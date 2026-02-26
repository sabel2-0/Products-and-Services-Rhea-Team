using MyAspNetApp.Data;
using MyAspNetApp.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();
app.UseDefaultFiles();
app.UseStaticFiles();

// API Endpoints

// Get all products
app.MapGet("/api/products", () => 
{
    return Results.Ok(ProductData.Products);
})
.WithName("GetAllProducts");

// Get men's products
app.MapGet("/api/products/men", () => 
{
    var menProducts = ProductData.Products.Where(p => p.Category == "Men").ToList();
    return Results.Ok(menProducts);
})
.WithName("GetMenProducts");

// Get women's products
app.MapGet("/api/products/women", () => 
{
    var womenProducts = ProductData.Products.Where(p => p.Category == "Women").ToList();
    return Results.Ok(womenProducts);
})
.WithName("GetWomenProducts");

// Get product by ID
app.MapGet("/api/products/{id}", (int id) => 
{
    var product = ProductData.Products.FirstOrDefault(p => p.Id == id);
    return product != null ? Results.Ok(product) : Results.NotFound();
})
.WithName("GetProductById");

// Add to cart
app.MapPost("/api/cart", (CartItem item) => 
{
    ProductData.Cart.Add(item);
    return Results.Ok(new { message = "Added to cart", cart = ProductData.Cart });
})
.WithName("AddToCart");

// Get cart
app.MapGet("/api/cart", () => 
{
    var cartWithProducts = ProductData.Cart.Select(ci => new
    {
        CartItem = ci,
        Product = ProductData.Products.FirstOrDefault(p => p.Id == ci.ProductId)
    }).ToList();
    return Results.Ok(cartWithProducts);
})
.WithName("GetCart");

// Remove from cart
app.MapDelete("/api/cart/{productId}", (int productId) => 
{
    var item = ProductData.Cart.FirstOrDefault(c => c.ProductId == productId);
    if (item != null)
    {
        ProductData.Cart.Remove(item);
        return Results.Ok(new { message = "Removed from cart" });
    }
    return Results.NotFound();
})
.WithName("RemoveFromCart");

// Add to wishlist
app.MapPost("/api/wishlist/{productId}", (int productId) => 
{
    if (!ProductData.Wishlist.Any(w => w.ProductId == productId))
    {
        ProductData.Wishlist.Add(new WishlistItem 
        { 
            ProductId = productId, 
            AddedDate = DateTime.Now 
        });
        return Results.Ok(new { message = "Added to wishlist" });
    }
    return Results.Ok(new { message = "Already in wishlist" });
})
.WithName("AddToWishlist");

// Get wishlist
app.MapGet("/api/wishlist", () => 
{
    var wishlistWithProducts = ProductData.Wishlist.Select(wi => new
    {
        WishlistItem = wi,
        Product = ProductData.Products.FirstOrDefault(p => p.Id == wi.ProductId)
    }).ToList();
    return Results.Ok(wishlistWithProducts);
})
.WithName("GetWishlist");

// Remove from wishlist
app.MapDelete("/api/wishlist/{productId}", (int productId) => 
{
    var item = ProductData.Wishlist.FirstOrDefault(w => w.ProductId == productId);
    if (item != null)
    {
        ProductData.Wishlist.Remove(item);
        return Results.Ok(new { message = "Removed from wishlist" });
    }
    return Results.NotFound();
})
.WithName("RemoveFromWishlist");

// Add review
app.MapPost("/api/products/{id}/reviews", (int id, Review review) => 
{
    var product = ProductData.Products.FirstOrDefault(p => p.Id == id);
    if (product != null)
    {
        review.Id = product.Reviews.Count + 1;
        review.Date = DateTime.Now;
        review.VerifiedPurchase = true;
        product.Reviews.Add(review);
        
        // Update rating
        product.Rating = product.Reviews.Average(r => r.Rating);
        product.ReviewCount = product.Reviews.Count;
        
        return Results.Ok(new { message = "Review added", product });
    }
    return Results.NotFound();
})
.WithName("AddReview");

// Check if product has been purchased and delivered
app.MapGet("/api/purchases/{productId}/delivered", (int productId) => 
{
    var purchase = ProductData.PurchaseRecords.FirstOrDefault(p => p.ProductId == productId);
    return Results.Ok(new { 
        hasPurchased = purchase != null,
        isDelivered = purchase?.IsDelivered ?? false,
        deliveryDate = purchase?.DeliveryDate
    });
})
.WithName("CheckPurchaseStatus");

app.Run();
