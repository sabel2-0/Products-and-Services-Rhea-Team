using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyAspNetApp.Data;
using MyAspNetApp.Models;

namespace MyAspNetApp.Controllers
{
    public class SellerController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IWebHostEnvironment _env;

        public SellerController(AppDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        // GET: /Seller - Seller Dashboard (loads products from DB)
        public async Task<IActionResult> Index()
        {
            var products = await _db.Products.ToListAsync();
            return View(products);
        }

        // GET: /Seller/CreateProduct
        public async Task<IActionResult> CreateProduct(string? mode, int? id)
        {
            ViewBag.Mode = mode ?? "create";
            ViewBag.ExistingColorImages = "{}";
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
                    return View(product);
                }
            }
            return View(new DbProduct());
        }

        // POST: /Seller/CreateProduct
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProduct(DbProduct model, IFormFile? imageFile, string? mode, string[]? colorNames)
        {
            ModelState.Remove("imageFile");
            ModelState.Remove("mode");
            ModelState.Remove("colorNames");

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

            // Derive Colors column from color entries
            var colorNameList = (colorNames ?? Array.Empty<string>())
                .Where(n => !string.IsNullOrWhiteSpace(n)).ToList();
            model.Colors = colorNameList.Count > 0
                ? string.Join(",", colorNameList)
                : model.Colors;

            int productId;

            if (mode == "edit" && model.ProductId > 0)
            {
                var existing = await _db.Products.FindAsync(model.ProductId);
                if (existing != null)
                {
                    existing.ProductName = model.ProductName;
                    existing.Price = model.Price;
                    existing.Discount = model.Discount;
                    existing.Category = model.Category;
                    existing.Details = model.Details;
                    existing.Brand = model.Brand;
                    existing.Stock = model.Stock;
                    existing.Colors = model.Colors;
                    existing.Sizes = model.Sizes;
                    existing.Status = model.Status ?? "active";
                    if (!string.IsNullOrEmpty(model.ImagePath))
                        existing.ImagePath = model.ImagePath;
                    await _db.SaveChangesAsync();
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
                    existing.Discount = model.Discount;
                    existing.Category = model.Category;
                    existing.Details = model.Details;
                    existing.Brand = model.Brand;
                    existing.Stock = model.Stock;
                    existing.Colors = model.Colors;
                    existing.Sizes = model.Sizes;
                    if (!string.IsNullOrEmpty(model.ImagePath))
                        existing.ImagePath = model.ImagePath;
                    await _db.SaveChangesAsync();
                }
                productId = model.ProductId;
            }
            else
            {
                model.Status = model.Status ?? "active";
                _db.Products.Add(model);
                await _db.SaveChangesAsync();
                productId = model.ProductId;
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
            }

            return RedirectToAction("Index");
        }

        // GET: /Seller/ViewProduct?id=1
        public async Task<IActionResult> ViewProduct(int? id, string? state)
        {
            ViewBag.State = state ?? "active";
            if (id.HasValue)
            {
                var product = await _db.Products.FindAsync(id.Value);
                if (product != null)
                    return View(product);
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
            }
            return RedirectToAction("Index");
        }
    }
}
