namespace MyAspNetApp.Models;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Image { get; set; } = string.Empty;
    public List<string> Images { get; set; } = new(); // Multiple product images
    public List<string> Sizes { get; set; } = new();
    public string Category { get; set; } = string.Empty; // "Men" or "Women"
    public string SubCategory { get; set; } = string.Empty; // "Running Shoes", "Apparel", "Accessories"
    public string Brand { get; set; } = string.Empty; // "Nike", "Adidas", etc.
    public List<string> AvailableColors { get; set; } = new(); // Color options
    public Dictionary<string, List<string>> ColorImages { get; set; } = new(); // color name → image gallery
    public double Rating { get; set; }
    public int ReviewCount { get; set; }
    public List<Review> Reviews { get; set; } = new();
    public int Stock { get; set; } = 50;
    public bool IsPreOrder { get; set; } = false;
    public string ExpectedReleaseDate { get; set; } = string.Empty; // e.g. "March 2026"
    public string PreOrderNote { get; set; } = string.Empty; // e.g. "Ships in 4–6 weeks"
    // Existing product sold out but available for pre-order restock
    public bool IsRestockPreOrder { get; set; } = false;
    public string RestockDate { get; set; } = string.Empty; // e.g. "Mid-March 2026"
    public string RestockNote { get; set; } = string.Empty; // e.g. "Limited restock — reserve yours now"
}

public class Review
{
    public int Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public double Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public bool VerifiedPurchase { get; set; } = true;
    public List<string> Images { get; set; } = new(); // Optional customer photos
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
