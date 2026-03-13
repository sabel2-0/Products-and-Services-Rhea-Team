using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MyAspNetApp.Data;
using MyAspNetApp.Models;
using MyAspNetApp.Models.ViewModels;
using System.Text.Json;

namespace MyAspNetApp.Controllers
{
    public class SellerController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IWebHostEnvironment _env;
        private readonly IMemoryCache _cache;
        private readonly ILogger<SellerController> _logger;

        public SellerController(AppDbContext db, IWebHostEnvironment env, IMemoryCache cache, ILogger<SellerController> logger)
        {
            _db = db;
            _env = env;
            _cache = cache;
            _logger = logger;
        }

        // GET: /Seller - Seller Dashboard (loads products from DB)
        public async Task<IActionResult> Index()
        {
            const string sellerIndexCacheKey = "seller:products:index";
            try
            {
                // load base products
                var products = await _db.Products.ToListAsync();

                // also fetch first variant image per product so UI can show something
                var variantImages = await _db.ProductVariants
                    .Where(v => !string.IsNullOrEmpty(v.ImagePath))
                    .GroupBy(v => v.ProductId)
                    .Select(g => new
                    {
                        ProductId = g.Key,
                        ImagePath = g.OrderBy(v => v.Id).Select(v => v.ImagePath).FirstOrDefault()
                    })
                    .ToListAsync();

                var imageMap = variantImages.ToDictionary(x => x.ProductId, x => x.ImagePath);
                foreach (var p in products)
                {
                    if (imageMap.TryGetValue(p.ProductId, out var img) && !string.IsNullOrEmpty(img))
                    {
                        p.ImagePath = img;
                    }
                }

                _cache.Set(sellerIndexCacheKey, products, TimeSpan.FromMinutes(5));
                return View(products);
            }
            catch (Exception ex) when (IsTransientDatabaseException(ex))
            {
                _logger.LogWarning(ex, "Database unreachable while loading seller product list.");

                if (_cache.TryGetValue(sellerIndexCacheKey, out List<DbProduct>? cachedProducts) && cachedProducts != null)
                {
                    TempData["SuccessMessage"] = "Database is temporarily unreachable. Showing last cached product list.";
                    return View(cachedProducts);
                }

                TempData["SuccessMessage"] = "Database is temporarily unreachable. Showing empty product list.";
                return View(new List<DbProduct>());
            }
        }

        // GET: /Seller/CreateProduct
        public async Task<IActionResult> CreateProduct(string? mode, int? id)
        {
            ViewBag.Mode = mode ?? "create";
            ViewBag.ExistingColorImages = "{}";
            ViewBag.ExistingVariants = "[]";

            if (id.HasValue && (mode == "edit" || mode == "renew" || mode == "relist"))
            {
                var product = await _db.Products.FindAsync(id.Value);
                if (product != null)
                {
                    var colorImages = await _db.ProductColorImages
                        .Where(ci => ci.ProductId == product.ProductId)
                        .ToListAsync();
                    var grouped = colorImages
                        .GroupBy(ci => ci.ColorName)
                        .ToDictionary(g => g.Key, g => g.Select(ci => ci.ImagePath).ToList());
                    ViewBag.ExistingColorImages = System.Text.Json.JsonSerializer.Serialize(grouped);

                    var variants = await _db.ProductVariants
                        .Where(v => v.ProductId == product.ProductId)
                        .ToListAsync();
                    ViewBag.ExistingVariants = System.Text.Json.JsonSerializer.Serialize(variants);

                    return View(product);
                }
            }
            return View(new DbProduct());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProduct(
            DbProduct model,
            IFormFile? imageFile,
            string? mode,
            string[]? colorNames,
            string[]? colorStocks,
            string[]? colorSizes,
            string[]? variantSkus,
            string[]? variantAvailabilities,
            string[]? variantPrices,
            string[]? variantWeights,
            string[]? variantLengths,
            string[]? variantHeights,
            string[]? variantWidths)
        {
            try
            {
                _logger.LogInformation($"=== CREATE PRODUCT START ===");
                _logger.LogInformation($"ModelState.IsValid: {ModelState.IsValid}");
                
                foreach (var ms in ModelState.Values)
                {
                    foreach (var error in ms.Errors)
                    {
                        _logger.LogWarning($"ModelState Error: {error.ErrorMessage}");
                    }
                }

                ModelState.Remove("imageFile");
                ModelState.Remove("mode");
                ModelState.Remove("colorNames");
                ModelState.Remove("colorStocks");
                ModelState.Remove("colorSizes");
                ModelState.Remove("variantSkus");
                ModelState.Remove("variantAvailabilities");
                ModelState.Remove("variantPrices");
                ModelState.Remove("variantWeights");
                ModelState.Remove("variantLengths");
                ModelState.Remove("variantHeights");
                ModelState.Remove("variantWidths");

                _logger.LogInformation($"Mode: {mode}, ColorNames count: {colorNames?.Length ?? 0}");
                if (colorNames != null)
                {
                    for (int i = 0; i < colorNames.Length; i++)
                    {
                        var stockValue = (colorStocks != null && i < colorStocks.Length) ? colorStocks[i] : null;
                        var sizeValue = (colorSizes != null && i < colorSizes.Length) ? colorSizes[i] : null;
                        _logger.LogInformation($"  [{i}] Color: {colorNames[i]}, Stock: {stockValue}, Sizes: {sizeValue}");
                    }
                }

                // Fallback when browser sends multiple files and default binding doesn't populate imageFile.
                if ((imageFile == null || imageFile.Length == 0) && Request.Form.Files.Count > 0)
                {
                    imageFile = Request.Form.Files.FirstOrDefault(f => f.Name == "imageFile")
                                ?? Request.Form.Files.FirstOrDefault();
                }

                // Handle main product image
                if (imageFile != null && imageFile.Length > 0)
                {
                    var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "products");
                    Directory.CreateDirectory(uploadsDir);
                    var fileName = Guid.NewGuid().ToString("N") + Path.GetExtension(imageFile.FileName);
                    var filePath = Path.Combine(uploadsDir, fileName);
                    using var stream = new FileStream(filePath, FileMode.Create);
                    await imageFile.CopyToAsync(stream);
                    model.ImagePath = "/uploads/products/" + fileName;
                }

                // Build per-color variants for normalization (stored in ProductVariants table)
                var colorNameList = (colorNames ?? Array.Empty<string>())
                    .Select(n => n?.Trim())
                    .Where(n => !string.IsNullOrWhiteSpace(n))
                    .ToList();

                var colorStockList = (colorStocks ?? Array.Empty<string>())
                    .Select(s => int.TryParse(s, out var n) ? n : 0).ToList();

                var colorSizeList = (colorSizes ?? Array.Empty<string>())
                    .Select(s => s?.Trim() ?? "")
                    .ToList();

                var skuList = (variantSkus ?? Array.Empty<string>())
                    .Select(s => s?.Trim() ?? "")
                    .ToList();

                var availabilityList = (variantAvailabilities ?? Array.Empty<string>())
                    .Select(s => s?.Trim() ?? "")
                    .ToList();

var priceList = (variantPrices ?? Array.Empty<string>())
                    .Select(s => decimal.TryParse(s, out var n) ? n : (decimal?)null)
                    .ToList();

                var weightList = (variantWeights ?? Array.Empty<string>())
                    .Select(s => decimal.TryParse(s, out var n) ? n : (decimal?)null)
                    .ToList();

                var lengthList = (variantLengths ?? Array.Empty<string>())
                    .Select(s => decimal.TryParse(s, out var n) ? n : (decimal?)null)
                    .ToList();

                var heightList = (variantHeights ?? Array.Empty<string>())
                    .Select(s => decimal.TryParse(s, out var n) ? n : (decimal?)null)
                    .ToList();

                var widthList = (variantWidths ?? Array.Empty<string>())
                    .Select(s => decimal.TryParse(s, out var n) ? n : (decimal?)null)
                    .ToList();

                _logger.LogInformation($"Processed variants - Names: {colorNameList.Count}, Stocks: {colorStockList.Count}, Sizes: {colorSizeList.Count}");

                var variants = new List<DbProductVariant>();
                var usedSkus = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < colorNameList.Count; i++)
                {
                    var colorName = colorNameList[i]!;
                    var stock = (i < colorStockList.Count) ? colorStockList[i] : 0;
                    var sizes = (i < colorSizeList.Count) ? colorSizeList[i] : null;
                    var variantSkuInput = (i < skuList.Count) ? skuList[i] : null;
                    var variantAvailabilityInput = (i < availabilityList.Count) ? availabilityList[i] : null;
                    var variantWeight = (i < weightList.Count) ? weightList[i] : model.Weight;
                    var variantLength = (i < lengthList.Count) ? lengthList[i] : model.Length;
                    var variantHeight = (i < heightList.Count) ? heightList[i] : model.Height;
                    var variantWidth = (i < widthList.Count) ? widthList[i] : model.Width;
                    var variantSku = !string.IsNullOrWhiteSpace(variantSkuInput)
                        ? variantSkuInput!
                        : $"SKU-{DateTime.UtcNow:yyyyMMddHHmmss}-{i + 1}";

                    if (usedSkus.Contains(variantSku))
                    {
                        int suffix = 2;
                        var baseSku = variantSku;
                        while (usedSkus.Contains($"{baseSku}-{suffix}"))
                        {
                            suffix++;
                        }
                        variantSku = $"{baseSku}-{suffix}";
                    }
                    usedSkus.Add(variantSku);

                    variants.Add(new DbProductVariant
                    {
                        ProductId = model.ProductId,
                        SKU = variantSku,
                        Style = colorName,
                        Size = string.IsNullOrWhiteSpace(sizes) ? "N/A" : sizes!,
                        Quantity = stock,
                        Availability = !string.IsNullOrWhiteSpace(variantAvailabilityInput)
                            ? variantAvailabilityInput!
                            : (stock > 0 ? "In Stock" : "Pre-Order"),
                        ImagePath = model.ImagePath ?? string.Empty,
                        Price = (i < priceList.Count) ? priceList[i] : (decimal?)null,
                        Weight = variantWeight,
                        Length = variantLength,
                        Height = variantHeight,
                        Width = variantWidth
                    });
                    _logger.LogInformation($"Variant {i}: SKU={variantSku}, Style={colorName}, Qty={stock}, Size={sizes}, Price={(i < priceList.Count ? priceList[i]?.ToString() : "<none>")}, W/L/H/W={variantWeight}/{variantLength}/{variantHeight}/{variantWidth}");
                }

                // Keep product-level SKU as the first variant SKU for backward compatibility.
                if (variants.Count > 0)
                {
                    model.SKU = variants[0].SKU;
                }

                int productId;

                if (mode == "edit" && model.ProductId > 0)
                {
                    var existing = await _db.Products.FindAsync(model.ProductId);
                    if (existing != null)
                    {
                        existing.ProductName = model.ProductName;
                        existing.Price = model.Price;
                        // existing.Discount = model.Discount; // handled via variants now
                        existing.Category = model.Category;
                        existing.Details = model.Details;
                        existing.Brand = model.Brand;
                        existing.SKU = model.SKU;
                        existing.Weight = model.Weight;
                        existing.Length = model.Length;
                        existing.Height = model.Height;
                        existing.Width = model.Width;
                        // Stock/sizes moved to variants – do not keep on product
                        // existing.Stock = model.Stock;
                        // existing.Sizes = model.Sizes;
                        existing.Gender = model.Gender; // new field
                        existing.Status = model.Status ?? "active";
                        if (!string.IsNullOrEmpty(model.ImagePath))
                            existing.ImagePath = model.ImagePath;
                        await _db.SaveChangesAsync();
                        _logger.LogInformation($"Updated product {model.ProductId}");

                        // Sync variants (normalize color/size/stock data)
                        await SaveProductVariantsAsync(model.ProductId, variants);
                    }
                    productId = model.ProductId;
                }
                else if (mode == "relist" && model.ProductId > 0)
                {
                    var existing = await _db.Products.FindAsync(model.ProductId);
                    if (existing != null)
                    {
                        existing.Status = "active";
                        existing.ProductName = model.ProductName;
                        existing.Price = model.Price;
                        // existing.Discount = model.Discount;
                        existing.Category = model.Category;
                        existing.Details = model.Details;
                        existing.Brand = model.Brand;
                        existing.SKU = model.SKU;
                        existing.Weight = model.Weight;
                        existing.Length = model.Length;
                        existing.Height = model.Height;
                        existing.Width = model.Width;
                        //existing.Stock = model.Stock;
                        //existing.Sizes = model.Sizes;
                        existing.Gender = model.Gender;
                        if (!string.IsNullOrEmpty(model.ImagePath))
                            existing.ImagePath = model.ImagePath;
                        await _db.SaveChangesAsync();
                        _logger.LogInformation($"Relisted product {model.ProductId}");

                        // Sync variants (normalize color/size/stock data)
                        await SaveProductVariantsAsync(model.ProductId, variants);
                    }
                    productId = model.ProductId;
                }
                else
                {
                    model.Status = model.Status ?? "active";
                    _db.Products.Add(model);
                    await _db.SaveChangesAsync();
                    productId = model.ProductId;
                    _logger.LogInformation($"Created new product {productId}");
                    _logger.LogInformation($"About to save {variants.Count} variants");
                    await SaveProductVariantsAsync(productId, variants);
                    _logger.LogInformation($"Variants saved successfully");
                }

            // Save color images
            if (colorNameList.Count > 0)
            {
                // Remove old color images for this product
                var oldImages = _db.ProductColorImages.Where(ci => ci.ProductId == productId);
                _db.ProductColorImages.RemoveRange(oldImages);
                await _db.SaveChangesAsync();

                var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "products");
                Directory.CreateDirectory(uploadsDir);

                for (int i = 0; i < colorNameList.Count; i++)
                {
                    var colorName = colorNameList[i].Trim();

                    // Re-insert existing images that were kept
                    var existingPaths = Request.Form[$"existingColorPaths_{i}"].ToString();
                    if (!string.IsNullOrEmpty(existingPaths))
                    {
                        foreach (var path in existingPaths.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                        {
                            _db.ProductColorImages.Add(new DbProductColorImage
                            {
                                ProductId = productId,
                                ColorName = colorName,
                                ImagePath = path
                            });
                        }
                    }

                    // Save newly uploaded files for this color
                    var colorFiles = Request.Form.Files.GetFiles($"colorFiles_{i}");
                    foreach (var file in colorFiles)
                    {
                        if (file.Length > 0)
                        {
                            var fn = Guid.NewGuid().ToString("N") + Path.GetExtension(file.FileName);
                            var fp = Path.Combine(uploadsDir, fn);
                            using var fs = new FileStream(fp, FileMode.Create);
                            await file.CopyToAsync(fs);
                            _db.ProductColorImages.Add(new DbProductColorImage
                            {
                                ProductId = productId,
                                ColorName = colorName,
                                ImagePath = "/uploads/products/" + fn
                            });
                        }
                    }
                }
                await _db.SaveChangesAsync();

                // Sync ProductVariants.imagePath using first image per style/color.
                var styleImageLookup = await _db.ProductColorImages
                    .Where(ci => ci.ProductId == productId)
                    .GroupBy(ci => ci.ColorName)
                    .Select(g => new { Style = g.Key, ImagePath = g.OrderBy(x => x.Id).Select(x => x.ImagePath).FirstOrDefault() })
                    .ToListAsync();

                var productVariants = await _db.ProductVariants
                    .Where(v => v.ProductId == productId)
                    .ToListAsync();

                foreach (var variant in productVariants)
                {
                    var matched = styleImageLookup.FirstOrDefault(x => string.Equals(x.Style, variant.Style, StringComparison.OrdinalIgnoreCase));
                    if (!string.IsNullOrWhiteSpace(matched?.ImagePath))
                    {
                        variant.ImagePath = matched.ImagePath!;
                    }
                }

                await _db.SaveChangesAsync();

                // Set product main image from first color's first photo if not already set
                if (string.IsNullOrEmpty(model.ImagePath))
                {
                    var firstColorImage = await _db.ProductColorImages
                        .Where(ci => ci.ProductId == productId)
                        .OrderBy(ci => ci.Id)
                        .FirstOrDefaultAsync();
                    if (firstColorImage != null)
                    {
                        var product = await _db.Products.FindAsync(productId);
                        if (product != null)
                        {
                            product.ImagePath = firstColorImage.ImagePath;
                            await _db.SaveChangesAsync();
                        }
                    }
                }
            }

            if (mode == "edit")
                TempData["SuccessMessage"] = "Product updated successfully.";
            else if (mode == "relist")
                TempData["SuccessMessage"] = "Product relisted successfully.";
            else
                TempData["SuccessMessage"] = "Product added successfully.";

            InvalidateProductCaches();
            return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while creating/updating product");
                TempData["ErrorMessage"] = "Product save failed. Please review your variant fields and try again.";
                ViewBag.Mode = mode ?? "create";
                ViewBag.ExistingColorImages = "{}";
                ViewBag.ExistingVariants = "[]";
                return View(model);
            }
        }

        // GET: /Seller/ViewProduct?id=1
        public async Task<IActionResult> ViewProduct(int? id, string? state)
        {
            ViewBag.State = state ?? "active";
            if (id.HasValue)
            {
                var product = await _db.Products.FindAsync(id.Value);
                if (product != null)
                {
                    var colorImages = await _db.ProductColorImages
                        .Where(ci => ci.ProductId == id.Value)
                        .ToListAsync();

                    // keep flat list for main gallery, plus grouping by style for variant cards
                    ViewBag.AllColorImages = colorImages.Select(ci => ci.ImagePath).Distinct().ToList();
                    ViewBag.ImagesByStyle = colorImages
                        .GroupBy(ci => ci.ColorName)
                        .ToDictionary(g => g.Key, g => g.Select(ci => ci.ImagePath).ToList());

                    var variants = await _db.ProductVariants
                        .Where(v => v.ProductId == id.Value)
                        .ToListAsync();
                    ViewBag.ProductVariants = variants;

                    return View(product);
                }
            }
            return View(new DbProduct());
        }

        // POST: /Seller/DeleteProduct
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _db.Products.FindAsync(id);
            if (product != null)
            {
                product.Status = "relist";
                await _db.SaveChangesAsync();
                TempData["SuccessMessage"] = "Product removed.";
                InvalidateProductCaches();
            }
            return RedirectToAction("Index");
        }

        // POST: /Seller/UpdateStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            var product = await _db.Products.FindAsync(id);
            if (product != null)
            {
                product.Status = status;
                await _db.SaveChangesAsync();
                TempData["SuccessMessage"] = "Status updated.";
                InvalidateProductCaches();
            }
            return RedirectToAction("Index");
        }

        // GET: /Seller/SizeGuide
        public IActionResult SizeGuide()
        {
            return View();
        }

        // GET: /Seller/ViewSizeGuide?id=1
        [HttpGet]
        public async Task<IActionResult> ViewSizeGuide(int id)
        {
            var product = await _db.Products.FindAsync(id);
            var model = new MyAspNetApp.Models.ViewSizeGuideViewModel
            {
                ProductId = id,
                ProductTitle = product?.ProductName ?? "Product Size Guide",
                IsPhotoUpload = false,
                MeasurementUnit = "in",
                Category = product?.Category ?? "Tops",
                TableTitle = "Size Chart",
                FitTips = "If you're on the borderline between two sizes, order the smaller size for a tighter fit or the larger size for a looser fit.",
                HowToMeasure = "Chest: Measure around the fullest part of your chest, keeping the measuring tape horizontal.",
                TableData = new List<List<string>>
                {
                    new List<string> { "Size", "XXS", "XS", "S", "M", "L", "XL", "XXL" },
                    new List<string> { "Chest (in.)", "28.5–30", "30–32", "32–33.5", "33.5–35", "35–37.5", "37.5–40", "40–42.5" },
                    new List<string> { "Waist (in.)", "24.5–26", "26–27", "27–28", "28–29.5", "29.5–31.5", "31.5–33.5", "33.5–35.5" },
                    new List<string> { "Hip (in.)", "33–34", "34–35", "35–36.5", "36.5–38", "38–40", "40–42", "42–44" }
                }
            };
            return View(model);
        }

        // GET: /Seller/ViewRatings?id=1
        [HttpGet]
        public async Task<IActionResult> ViewRatings(int id)
        {
            var product = await _db.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            var reviews = await _db.Reviews
                .Where(r => r.ProductId == id)
                .OrderByDescending(r => r.Date)
                .ToListAsync();

            var reviewIds = reviews.Select(r => r.Id).ToList();
            var reviewImages = await _db.ReviewImages
                .Where(i => reviewIds.Contains(i.ReviewId))
                .ToListAsync();

            var model = new SellerRatingsViewModel
            {
                Product = product,
                Reviews = reviews.Select(r => new SellerRatingItem
                {
                    Review = r,
                    Images = reviewImages
                        .Where(i => i.ReviewId == r.Id)
                        .Select(i => i.ImageUrl)
                        .ToList()
                }).ToList()
            };

            return View(model);
        }

        private static bool IsTransientDatabaseException(Exception ex)
        {
            if (ex is TimeoutException || ex is SqlException)
            {
                return true;
            }

            if (ex is InvalidOperationException ioe &&
                ioe.Message.Contains("connection from the pool", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (ex.InnerException is TimeoutException || ex.InnerException is SqlException)
            {
                return true;
            }

            return ex.InnerException is InvalidOperationException innerIoe &&
                   innerIoe.Message.Contains("connection from the pool", StringComparison.OrdinalIgnoreCase);
        }

        private async Task SaveProductVariantsAsync(int productId, List<DbProductVariant> variants)
        {
            try
            {
                // Remove existing variants for this product and replace with the current set
                var existingVariants = await _db.ProductVariants
                    .Where(v => v.ProductId == productId)
                    .ToListAsync();
                
                if (existingVariants.Any())
                {
                    _db.ProductVariants.RemoveRange(existingVariants);
                    await _db.SaveChangesAsync();
                }

                // Add new variants
                if (variants != null && variants.Count > 0)
                {
                    foreach (var variant in variants)
                    {
                        // Create a new instance to avoid tracking issues.
                        var newVariant = new DbProductVariant
                        {
                            ProductId = productId,
                            SKU = string.IsNullOrWhiteSpace(variant.SKU) ? $"SKU-{productId}" : variant.SKU,
                            Style = string.IsNullOrWhiteSpace(variant.Style) ? "N/A" : variant.Style,
                            Size = string.IsNullOrWhiteSpace(variant.Size) ? "N/A" : variant.Size,
                            Quantity = variant.Quantity,
                            Availability = string.IsNullOrWhiteSpace(variant.Availability)
                                ? (variant.Quantity > 0 ? "In Stock" : "Pre-Order")
                                : variant.Availability,
                            ImagePath = variant.ImagePath ?? string.Empty,
                            Price = variant.Price,
                            Weight = variant.Weight,
                            Length = variant.Length,
                            Height = variant.Height,
                            Width = variant.Width
                        };
                        _db.ProductVariants.Add(newVariant);
                    }
                    await _db.SaveChangesAsync();
                    _logger.LogInformation($"Saved {variants.Count} variants for product {productId}");
                }
                else
                {
                    _logger.LogWarning($"No variants provided for product {productId}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error saving variants for product {productId}");
                throw;
            }
        }

        private void InvalidateProductCaches()
        {
            _cache.Remove("products:all");
            _cache.Remove("products:men");
            _cache.Remove("products:women");
            _cache.Remove("seller:products:index");
        }
    }
}
