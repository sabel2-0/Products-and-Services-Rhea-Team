namespace MyAspNetApp.Models;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Image { get; set; } = string.Empty;
    public List<string> Sizes { get; set; } = new();
    public string Category { get; set; } = string.Empty; // "Men" or "Women"
    public string SubCategory { get; set; } = string.Empty; // "Running Shoes", "Apparel", "Accessories"
    public string Brand { get; set; } = string.Empty; // "Nike", "Adidas", etc.
    public List<string> AvailableColors { get; set; } = new(); // Color options
    public double Rating { get; set; }
    public int ReviewCount { get; set; }
    public List<Review> Reviews { get; set; } = new();
}

public class Review
{
    public int Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public double Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public bool VerifiedPurchase { get; set; } = true;
}

public class CartItem
{
    public int ProductId { get; set; }
    public string Size { get; set; } = string.Empty;
    public int Quantity { get; set; }
}

public class WishlistItem
{
    public int ProductId { get; set; }
    public DateTime AddedDate { get; set; }
}

public class PurchaseRecord
{
    public int ProductId { get; set; }
    public DateTime PurchaseDate { get; set; }
    public DateTime? DeliveryDate { get; set; }
    public bool IsDelivered => DeliveryDate.HasValue;
}
