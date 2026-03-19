using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MyAspNetApp.Data;
using MyAspNetApp.Models;
using System.Text.Json;

namespace MyAspNetApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IMemoryCache _cache;
        private readonly TimeSpan _cacheDuration;
        private readonly ILogger<ProductsController> _logger;
        private readonly IWebHostEnvironment _env;

        public ProductsController(AppDbContext db, IMemoryCache cache, IConfiguration config, ILogger<ProductsController> logger, IWebHostEnvironment env)
        {
            _db = db;
            _cache = cache;
            _logger = logger;
            _env = env;
            _cacheDuration = TimeSpan.FromMinutes(config.GetValue<int>("Cache:ProductCacheMinutes", 5));
        }

        private static Review MapDbReview(DbReview review, List<DbReviewImage> reviewImages)
        {
            return new Review
            {
                Id = review.Id,
                UserName = review.UserName,
                Rating = (double)review.Rating,
                ShortReview = review.ShortReview,
                Comment = review.Comment,
                Date = review.Date,
                VerifiedPurchase = review.VerifiedPurchase,
                Email = review.Email,
                Recommend = review.Recommend,
                Comfort = review.Comfort,
                Quality = review.Quality,
                SizeFit = review.SizeFit,
                WidthFit = review.WidthFit,
                SellerReply = review.SellerReply,
                SellerReplyDate = review.SellerReplyDate,
                ProductId = review.ProductId,
                Images = reviewImages
                    .Where(i => i.ReviewId == review.Id)
                    .Select(i => i.ImageUrl)
                    .ToList()
            };
        }

        private Product MapDbProduct(
            DbProduct p,
            List<DbProductVariant>? variants = null,
            Dictionary<int, (double Rating, int Count)>? reviewStats = null,
            Dictionary<int, List<Review>>? reviewsByProduct = null)
        {
            var variantsByColor = (variants ?? new List<DbProductVariant>())
                .GroupBy(v => v.ColorName)
                .ToDictionary(g => g.Key, g => g.ToList());

            var colorImageDict = variantsByColor.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value
                    .Select(v => v.ImagePath)
                    .Where(path => !string.IsNullOrWhiteSpace(path))
                    .Distinct()
                    .ToList());

            var availColors = variantsByColor.Keys.ToList();
            if (availColors.Count == 0)
                availColors = colorImageDict.Keys.ToList();

            var colorStockDict = new Dictionary<string, int>();
            var colorSizesDict = new Dictionary<string, List<string>>();

            foreach (var color in availColors)
            {
                if (variantsByColor.TryGetValue(color, out var vlist))
                {
                    colorStockDict[color] = vlist.Sum(v => v.Stock);
                    var sizes = vlist
                        .SelectMany(v => (v.Sizes ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                        .Where(s => !string.IsNullOrWhiteSpace(s))
                        .Distinct()
                        .ToList();
                    if (sizes.Any())
                        colorSizesDict[color] = sizes;
                }
                else
                {
                    colorStockDict[color] = p.Stock;
                }
            }

            var allSizes = (variants ?? new List<DbProductVariant>())
                .SelectMany(v => (v.Sizes ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct()
                .ToList();

            var rating = 0d;
            var reviewCount = 0;
            if (reviewStats != null && reviewStats.TryGetValue(p.ProductId, out var stats))
            {
                rating = stats.Rating;
                reviewCount = stats.Count;
            }

            var mappedReviews = new List<Review>();
            if (reviewsByProduct != null && reviewsByProduct.TryGetValue(p.ProductId, out var fullReviews))
                mappedReviews = fullReviews;

            // Prefer variant-level SKU/dimensions (normalized schema) but fall back to product-level if present.
            var productVariants = (variants ?? new List<DbProductVariant>())
                .Where(v => v.ProductId == p.ProductId)
                .ToList();
            var firstVariant = productVariants.FirstOrDefault();

            var sku = !string.IsNullOrWhiteSpace(p.SKU) ? p.SKU : firstVariant?.SKU ?? string.Empty;
            var weight = p.Weight ?? firstVariant?.Weight;
            var length = p.Length ?? firstVariant?.Length;
            var height = p.Height ?? firstVariant?.Height;
            var width = p.Width ?? firstVariant?.Width;

            // assemble image path: prefer first variant's image, then product fallback
            string imagePath = firstVariant?.ImagePath ?? p.ImagePath ?? string.Empty;

            // compute price using variant first if available
            // compute price entirely from variant. if none supplied, default to 0.
            decimal basePrice = 0m;
            decimal? baseOriginal = null;
            if (firstVariant != null && firstVariant.Price.HasValue)
            {
                basePrice = firstVariant.Price.Value;
            }
            // else leave zero (caller may interpret as not priced)

            // convert db variants to API variant models
            var variantModels = productVariants.Select(v => new ProductVariant
            {
                Id = v.Id,
                SKU = v.SKU,
                Size = v.Size,
                Style = v.Style,
                Quantity = v.Quantity,
                Availability = v.Availability,
                ImagePath = v.ImagePath,
                Price = v.Price,
                Weight = v.Weight,
                Length = v.Length,
                Height = v.Height,
                Width = v.Width
            }).ToList();

            return new Product
            {
                Variants = variantModels,
                Id = p.ProductId,
                Name = p.ProductName,
                Price = basePrice,
                OriginalPrice = baseOriginal,
                Image = imagePath,
                SKU = sku,
                Gender = p.Gender ?? "Unisex",
                Category = p.Category ?? "",
                // the database currently doesn't have a separate subcategory column;
                // we will leave this blank to avoid showing the same tag twice
                // (gender is now displayed separately on the UI).
                // TODO: add SubCategory column when needed and map appropriately.
                SubCategory = "",
                Brand = p.Brand ?? "",
                Weight = weight,
                Length = length,
                Height = height,
                Width = width,
                Description = p.Details ?? "",
                Rating = rating,
                ReviewCount = reviewCount,
                Reviews = mappedReviews,
                // compute total stock from variants since product.Stock no longer used
                Stock = productVariants.Sum(v => v.Quantity),
                Sizes = allSizes,
                AvailableColors = availColors,
                ColorImages = colorImageDict,
                ColorStocks = colorStockDict,
                ColorSizes = colorSizesDict
            };
        }

        // GET: api/products
        [HttpGet]
        public async Task<IActionResult> GetAllProducts()
        {
            try
            {
                var result = await _cache.GetOrCreateAsync("products:all", async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = _cacheDuration;

                    var dbProducts = await _db.Products
                        .AsNoTracking()
                        .Where(p => p.Status == "active")
                        .ToListAsync();

                    var productIds = dbProducts.Select(p => p.ProductId).ToList();
                    var dbReviews = await _db.Reviews
                        .AsNoTracking()
                        .Where(r => productIds.Contains(r.ProductId))
                        .ToListAsync();
                    var variants = await _db.ProductVariants
                        .AsNoTracking()
                        .Where(v => productIds.Contains(v.ProductId))
                        .ToListAsync();

                    var reviewStats = dbReviews
                        .GroupBy(r => r.ProductId)
                        .ToDictionary(g => g.Key, g => ((double)g.Average(x => x.Rating), g.Count()));

                    return dbProducts.Select(p => MapDbProduct(p, variants, reviewStats)).ToList();
                });

                if (result == null)
                {
                    return Ok(new List<Product>());
                }

                return Ok(result);
            }
            catch (Exception ex) when (IsTransientDatabaseException(ex))
            {
                _logger.LogError(ex, "Error fetching all products from the database.");
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new
                {
                    message = "Database is temporarily unavailable.",
                    detail = ex.Message
                });
            }
        }

        // GET: api/products/men
        [HttpGet("men")]
        public async Task<IActionResult> GetMenProducts()
        {
            try
            {
                var result = await _cache.GetOrCreateAsync("products:men", async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = _cacheDuration;

                    var products = await _db.Products
                        .AsNoTracking()
                        .Where(p => p.Status == "active" && p.Gender == "Men")
                        .ToListAsync();
                    var productIds = products.Select(p => p.ProductId).ToList();
                    var dbReviews = await _db.Reviews
                        .AsNoTracking()
                        .Where(r => productIds.Contains(r.ProductId))
                        .ToListAsync();

                    var variants = await _db.ProductVariants
                        .AsNoTracking()
                        .Where(v => productIds.Contains(v.ProductId))
                        .ToListAsync();

                    var reviewStats = dbReviews
                        .GroupBy(r => r.ProductId)
                        .ToDictionary(g => g.Key, g => ((double)g.Average(x => x.Rating), g.Count()));
                    return products.Select(p => MapDbProduct(p, variants, reviewStats)).ToList();
                });

                if (result == null)
                {
                    return Ok(new List<Product>());
                }

                return Ok(result);
            }
            catch (Exception ex) when (IsTransientDatabaseException(ex))
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Database is temporarily unavailable." });
            }
        }

        // GET: api/products/women
        [HttpGet("women")]
        public async Task<IActionResult> GetWomenProducts()
        {
            try
            {
                var result = await _cache.GetOrCreateAsync("products:women", async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = _cacheDuration;

                    var products = await _db.Products
                        .AsNoTracking()
                        .Where(p => p.Status == "active" && p.Gender == "Women")
                        .ToListAsync();
                    var productIds = products.Select(p => p.ProductId).ToList();
                    var dbReviews = await _db.Reviews
                        .AsNoTracking()
                        .Where(r => productIds.Contains(r.ProductId))
                        .ToListAsync();

                    var variants = await _db.ProductVariants
                        .AsNoTracking()
                        .Where(v => productIds.Contains(v.ProductId))
                        .ToListAsync();

                    var reviewStats = dbReviews
                        .GroupBy(r => r.ProductId)
                        .ToDictionary(g => g.Key, g => ((double)g.Average(x => x.Rating), g.Count()));
                    return products.Select(p => MapDbProduct(p, variants, reviewStats)).ToList();
                });

                if (result == null)
                {
                    return Ok(new List<Product>());
                }

                return Ok(result);
            }
            catch (Exception ex) when (IsTransientDatabaseException(ex))
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Database is temporarily unavailable." });
            }
        }

        // GET: api/products/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetProductById(int id)
        {
            try
            {
                var dbProduct = await _db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.ProductId == id);
                if (dbProduct != null)
                {
                    var dbReviews = await _db.Reviews
                        .AsNoTracking()
                        .Where(r => r.ProductId == id)
                        .OrderByDescending(r => r.Date)
                        .ToListAsync();
                    var reviewImages = await _db.ReviewImages
                        .AsNoTracking()
                        .Where(i => dbReviews.Select(r => r.Id).Contains(i.ReviewId))
                        .ToListAsync();

                    var mappedReviews = dbReviews
                        .GroupBy(r => r.ProductId)
                        .ToDictionary(
                            g => g.Key,
                            g => g.Select(r => MapDbReview(r, reviewImages)).ToList());
                    var variants = await _db.ProductVariants
                        .AsNoTracking()
                        .Where(v => v.ProductId == id)
                        .ToListAsync();

                    var reviewStats = dbReviews
                        .GroupBy(r => r.ProductId)
                        .ToDictionary(g => g.Key, g => ((double)g.Average(x => x.Rating), g.Count()));

                    var mappedProduct = MapDbProduct(dbProduct, variants, reviewStats, mappedReviews);

                    var dbSizeGuide = await _db.SizeGuides
                        .AsNoTracking()
                        .Include(g => g.Images)
                        .FirstOrDefaultAsync(g => g.ProductId == id);

                    if (dbSizeGuide != null)
                    {
                        var payload = ParseProductSizeGuidePayload(dbSizeGuide.TableJson, dbSizeGuide.Title);
                        var tables = payload.Tables;
                        var firstTable = tables.FirstOrDefault();
                        var effectiveCategory = dbSizeGuide.Category ?? dbProduct.Category ?? string.Empty;

                        mappedProduct.SizeGuide = new ProductSizeGuide
                        {
                            Title = firstTable?.Title ?? dbSizeGuide.Title ?? string.Empty,
                            MeasurementUnit = string.IsNullOrWhiteSpace(dbSizeGuide.MeasurementUnit) ? "in" : dbSizeGuide.MeasurementUnit!,
                            PhotoMeasurementUnit = string.IsNullOrWhiteSpace(payload.PhotoMeasurementUnit) ? "in" : payload.PhotoMeasurementUnit,
                            TableMeasurementUnit = string.IsNullOrWhiteSpace(payload.TableMeasurementUnit)
                                ? (string.IsNullOrWhiteSpace(dbSizeGuide.MeasurementUnit) ? "in" : dbSizeGuide.MeasurementUnit!)
                                : payload.TableMeasurementUnit,
                            PhotoGuideUnitsByUrl = payload.PhotoGuideUnitsByUrl,
                            Category = dbSizeGuide.Category ?? string.Empty,
                            FitTips = string.IsNullOrWhiteSpace(dbSizeGuide.FitTips) ? GetDefaultFitTips(effectiveCategory) : dbSizeGuide.FitTips,
                            HowToMeasure = string.IsNullOrWhiteSpace(dbSizeGuide.HowToMeasure) ? GetDefaultHowToMeasure(effectiveCategory) : dbSizeGuide.HowToMeasure,
                            AdditionalNotes = dbSizeGuide.AdditionalNotes ?? string.Empty,
                            TableData = firstTable?.Data ?? new List<List<string>>(),
                            Tables = tables,
                            ImageUrls = dbSizeGuide.Images
                                .OrderBy(i => i.SortOrder)
                                .Select(i => i.ImagePath)
                                .Where(p => !string.IsNullOrWhiteSpace(p))
                                .ToList()
                        };
                    }

                    return Ok(mappedProduct);
                }
                return NotFound();
            }
            catch (Exception ex) when (IsTransientDatabaseException(ex))
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Database is temporarily unavailable." });
            }
        }

        // GET: api/products/seller/{sellerId}
        [HttpGet("seller/{sellerId}")]
        public async Task<IActionResult> GetProductsBySeller(int sellerId)
        {
            try
            {
                var products = await _db.Products
                    .AsNoTracking()
                    .Where(p => p.Status == "active")
                    .ToListAsync();
                var productIds = products.Select(p => p.ProductId).ToList();
                var dbReviews = await _db.Reviews
                    .AsNoTracking()
                    .Where(r => productIds.Contains(r.ProductId))
                    .ToListAsync();
                var variants = await _db.ProductVariants
                    .AsNoTracking()
                    .Where(v => productIds.Contains(v.ProductId))
                    .ToListAsync();

                var reviewStats = dbReviews
                    .GroupBy(r => r.ProductId)
                    .ToDictionary(g => g.Key, g => ((double)g.Average(x => x.Rating), g.Count()));
                return Ok(products.Select(p => MapDbProduct(p, variants, reviewStats)).ToList());
            }
            catch (Exception ex) when (IsTransientDatabaseException(ex))
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Database is temporarily unavailable." });
            }
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

        private sealed class ProductSizeGuidePayload
        {
            public string PhotoMeasurementUnit { get; set; } = string.Empty;
            public string TableMeasurementUnit { get; set; } = string.Empty;
            public Dictionary<string, string> PhotoGuideUnitsByUrl { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            public List<ProductSizeGuideTable> Tables { get; set; } = new List<ProductSizeGuideTable>();
        }

        private static ProductSizeGuidePayload ParseProductSizeGuidePayload(string? rawJson, string? fallbackTitle)
        {
            var payload = new ProductSizeGuidePayload();
            if (string.IsNullOrWhiteSpace(rawJson))
            {
                return payload;
            }

            try
            {
                using var doc = JsonDocument.Parse(rawJson);
                if (doc.RootElement.ValueKind == JsonValueKind.Object)
                {
                    if (doc.RootElement.TryGetProperty("photoMeasurementUnit", out var photoUnitEl) && photoUnitEl.ValueKind == JsonValueKind.String)
                    {
                        payload.PhotoMeasurementUnit = (photoUnitEl.GetString() ?? string.Empty).Trim();
                    }
                    if (doc.RootElement.TryGetProperty("tableMeasurementUnit", out var tableUnitEl) && tableUnitEl.ValueKind == JsonValueKind.String)
                    {
                        payload.TableMeasurementUnit = (tableUnitEl.GetString() ?? string.Empty).Trim();
                    }
                    if (doc.RootElement.TryGetProperty("photoGuideUnitsByUrl", out var photoMapEl) && photoMapEl.ValueKind == JsonValueKind.Object)
                    {
                        payload.PhotoGuideUnitsByUrl = ParsePhotoGuideUnitsMap(photoMapEl);
                    }
                    if (doc.RootElement.TryGetProperty("tables", out var tablesEl) && tablesEl.ValueKind == JsonValueKind.Array)
                    {
                        payload.Tables = ParseProductSizeGuideTablesArray(tablesEl, fallbackTitle);
                    }
                    return payload;
                }

                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    payload.Tables = ParseProductSizeGuideTablesArray(doc.RootElement, fallbackTitle);
                }
            }
            catch
            {
                // Ignore malformed JSON and return empty payload.
            }

            return payload;
        }

        private static List<ProductSizeGuideTable> ParseProductSizeGuideTables(string? rawJson, string? fallbackTitle)
        {
            return ParseProductSizeGuidePayload(rawJson, fallbackTitle).Tables;
        }

        private static List<ProductSizeGuideTable> ParseProductSizeGuideTablesArray(JsonElement rootArray, string? fallbackTitle)
        {
            var tables = new List<ProductSizeGuideTable>();
            if (rootArray.ValueKind != JsonValueKind.Array)
            {
                return tables;
            }

            var items = rootArray.EnumerateArray().ToList();
            if (items.Count == 0)
            {
                return tables;
            }

            if (items[0].ValueKind == JsonValueKind.Array)
            {
                var data = ParseTableData(rootArray);
                if (data.Any(r => r.Any(c => !string.IsNullOrWhiteSpace(c))))
                {
                    tables.Add(new ProductSizeGuideTable
                    {
                        Title = string.IsNullOrWhiteSpace(fallbackTitle) ? string.Empty : fallbackTitle.Trim(),
                        Data = data
                    });
                }
                return tables;
            }

            foreach (var item in items)
            {
                if (item.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                var title = item.TryGetProperty("title", out var titleEl) && titleEl.ValueKind == JsonValueKind.String
                    ? (titleEl.GetString() ?? string.Empty).Trim()
                    : string.Empty;

                List<List<string>> data = new();
                if (item.TryGetProperty("data", out var dataEl) && dataEl.ValueKind == JsonValueKind.Array)
                {
                    data = ParseTableData(dataEl);
                }

                if (!data.Any(r => r.Any(c => !string.IsNullOrWhiteSpace(c))))
                {
                    continue;
                }

                tables.Add(new ProductSizeGuideTable
                {
                    Title = title,
                    MeasurementUnit = item.TryGetProperty("measurementUnit", out var unitEl) && unitEl.ValueKind == JsonValueKind.String
                        ? (unitEl.GetString() ?? string.Empty).Trim()
                        : string.Empty,
                    PhotoOrder = item.TryGetProperty("photoOrder", out var orderEl) && orderEl.ValueKind == JsonValueKind.Number
                        ? orderEl.GetInt32()
                        : 0,
                    Data = data,
                    ImageUrl = item.TryGetProperty("imageUrl", out var imageEl) && imageEl.ValueKind == JsonValueKind.String
                        ? (imageEl.GetString() ?? string.Empty).Trim()
                        : string.Empty
                });
            }

            return tables;
        }

        private static List<List<string>> ParseTableData(JsonElement tableElement)
        {
            var data = new List<List<string>>();
            if (tableElement.ValueKind != JsonValueKind.Array)
            {
                return data;
            }

            foreach (var rowEl in tableElement.EnumerateArray())
            {
                if (rowEl.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                var row = new List<string>();
                foreach (var cellEl in rowEl.EnumerateArray())
                {
                    row.Add((cellEl.ToString() ?? string.Empty).Trim());
                }
                data.Add(row);
            }

            return data;
        }

        private static Dictionary<string, string> ParsePhotoGuideUnitsMap(JsonElement mapElement)
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (mapElement.ValueKind != JsonValueKind.Object)
            {
                return map;
            }

            foreach (var prop in mapElement.EnumerateObject())
            {
                var key = (prop.Name ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(key))
                {
                    continue;
                }

                var unitRaw = prop.Value.ValueKind == JsonValueKind.String
                    ? (prop.Value.GetString() ?? string.Empty).Trim().ToLowerInvariant()
                    : string.Empty;
                map[key] = unitRaw == "cm" ? "cm" : "in";
            }

            return map;
        }

        private static string GetDefaultFitTips(string? category)
        {
            var key = (category ?? string.Empty).Trim().ToLowerInvariant();
            return key switch
            {
                "footwear" => "If you are between sizes, choose the larger size for better comfort, especially for socks or long wear.",
                "bottoms" => "If your waist and hips suggest different sizes, choose the size based on your hip measurement for better movement.",
                "outerwear" => "If you plan to layer underneath, consider going one size up for comfort.",
                "dresses & jumpsuits" => "If your bust and hips suggest different sizes, choose the size that fits your larger measurement.",
                "activewear" => "For compression feel choose your exact size, for a relaxed feel choose one size up.",
                _ => "If you are between two sizes, choose the smaller size for a tighter fit or the larger size for a looser fit."
            };
        }

        private static string GetDefaultHowToMeasure(string? category)
        {
            var key = (category ?? string.Empty).Trim().ToLowerInvariant();
            return key switch
            {
                "footwear" => "Foot Length: Stand on paper and mark heel to longest toe, then measure the distance.",
                "bottoms" => "Waist: Measure around your natural waistline. Hips: Measure around the fullest part of your hips.",
                "outerwear" => "Chest: Measure around the fullest part of your chest with the tape level and relaxed.",
                "dresses & jumpsuits" => "Bust: Measure around fullest bust. Waist: Measure natural waistline. Hips: Measure fullest hip area.",
                "activewear" => "Chest: Measure fullest chest. Waist: Measure natural waist. Keep tape snug but not tight.",
                _ => "Chest: Measure around the fullest part of your chest, keeping the measuring tape horizontal."
            };
        }
    }

    [ApiController]
    [Route("api/[controller]")]
    public class SellersController : ControllerBase
    {
        // GET: api/sellers/{id}
        [HttpGet("{id}")]
        public IActionResult GetSellerById(int id)
        {
            var seller = ProductData.Sellers.FirstOrDefault(s => s.Id == id);
            return seller != null ? Ok(seller) : NotFound();
        }

        // GET: api/sellers
        [HttpGet]
        public IActionResult GetAllSellers()
        {
            return Ok(ProductData.Sellers);
        }
    }
}
