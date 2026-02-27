using MyAspNetApp.Models;

namespace MyAspNetApp.Data;

public static class ProductData
{
    public static List<Product> Products { get; } = new()
    {
        // Men's Running Products
        new Product
        {
            Id = 1,
            Name = "Horizon Elite Road Runner",
            Description = "Premium carbon-plated running shoes engineered for speed and endurance. Features advanced cushioning technology and breathable mesh upper for maximum performance.",
            Price = 8999.00m,
            Image = "https://images.pexels.com/photos/2529148/pexels-photo-2529148.jpeg?auto=compress&cs=tinysrgb&w=600",
            Images = new List<string> {
                "https://images.pexels.com/photos/2529148/pexels-photo-2529148.jpeg?auto=compress&cs=tinysrgb&w=600",
                "https://images.pexels.com/photos/1598508/pexels-photo-1598508.jpeg?auto=compress&cs=tinysrgb&w=600",
                "https://images.pexels.com/photos/2385477/pexels-photo-2385477.jpeg?auto=compress&cs=tinysrgb&w=600",
                "https://images.pexels.com/photos/1456706/pexels-photo-1456706.jpeg?auto=compress&cs=tinysrgb&w=600"
            },
            Sizes = new List<string> { "US 7", "US 8", "US 9", "US 10", "US 11", "US 12" },
            Category = "Men",
            SubCategory = "Running Shoes",
            Brand = "Nike",
            AvailableColors = new List<string> { "Red", "Black", "Blue" },
            Rating = 4.8,
            ReviewCount = 245,
            Reviews = new List<Review>
            {
                new Review { Id = 1, UserName = "Carlos M.", Rating = 5, Comment = "Best running shoes for marathon training! Worth every peso.", Date = DateTime.Now.AddDays(-5) },
                new Review { Id = 2, UserName = "Rafael S.", Rating = 5, Comment = "Lightweight and responsive. Perfect for long runs.", Date = DateTime.Now.AddDays(-10) },
                new Review { Id = 3, UserName = "Jose P.", Rating = 5, Comment = "Excellent cushioning! My knees feel great after long runs.", Date = DateTime.Now.AddDays(-15) },
                new Review { Id = 4, UserName = "Mark T.", Rating = 4, Comment = "Great shoes, slight break-in period but worth it.", Date = DateTime.Now.AddDays(-20) },
                new Review { Id = 5, UserName = "David L.", Rating = 5, Comment = "Perfect for road running. Very durable and comfortable.", Date = DateTime.Now.AddDays(-25) },
                new Review { Id = 6, UserName = "Rico G.", Rating = 5, Comment = "These shoes helped me achieve my personal best in a half marathon!", Date = DateTime.Now.AddDays(-30) },
                new Review { Id = 7, UserName = "Anthony V.", Rating = 4, Comment = "Good quality, runs true to size. Highly recommend.", Date = DateTime.Now.AddDays(-35) }
            }
        },
        new Product
        {
            Id = 2,
            Name = "Sprint Pro Training Shoes",
            Description = "Versatile training shoes with responsive cushioning perfect for speed work and interval training. Durable outsole for all-terrain running.",
            Price = 6499.00m,
            Image = "https://images.pexels.com/photos/1598505/pexels-photo-1598505.jpeg?auto=compress&cs=tinysrgb&w=600",
            Images = new List<string> {
                "https://images.pexels.com/photos/1598505/pexels-photo-1598505.jpeg?auto=compress&cs=tinysrgb&w=600",
                "https://images.pexels.com/photos/2529147/pexels-photo-2529147.jpeg?auto=compress&cs=tinysrgb&w=600",
                "https://images.pexels.com/photos/1464625/pexels-photo-1464625.jpeg?auto=compress&cs=tinysrgb&w=600",
                "https://images.pexels.com/photos/1102776/pexels-photo-1102776.jpeg?auto=compress&cs=tinysrgb&w=600"
            },
            Sizes = new List<string> { "US 7", "US 8", "US 9", "US 10", "US 11", "US 12" },
            Category = "Men",
            SubCategory = "Running Shoes",
            Brand = "Adidas",
            AvailableColors = new List<string> { "White", "Grey", "Black" },
            Rating = 4.6,
            ReviewCount = 198,
            Reviews = new List<Review>
            {
                new Review { Id = 3, UserName = "Miguel P.", Rating = 5, Comment = "Great for tempo runs and track workouts!", Date = DateTime.Now.AddDays(-3) }
            }
        },
        new Product
        {
            Id = 3,
            Name = "Men's Performance Running Shorts",
            Description = "Lightweight 2-in-1 running shorts with moisture-wicking fabric and secure phone pocket. Built-in compression liner for support.",
            Price = 2499.00m,
            Image = "https://images.pexels.com/photos/5067731/pexels-photo-5067731.jpeg?auto=compress&cs=tinysrgb&w=600",
            Sizes = new List<string> { "S", "M", "L", "XL", "XXL" },
            Category = "Men",
            SubCategory = "Apparel",
            Brand = "Nike",
            AvailableColors = new List<string> { "Black", "Navy", "Charcoal" },
            Rating = 4.7,
            ReviewCount = 156,
            Reviews = new List<Review>()
        },
        new Product
        {
            Id = 4,
            Name = "Trail Runner XC",
            Description = "Rugged trail running shoes with aggressive grip and protective rock plate. Waterproof upper for all-weather adventures.",
            Price = 7499.00m,
            Image = "https://images.pexels.com/photos/1456705/pexels-photo-1456705.jpeg?auto=compress&cs=tinysrgb&w=600",
            Sizes = new List<string> { "US 7", "US 8", "US 9", "US 10", "US 11", "US 12" },
            Category = "Men",
            SubCategory = "Running Shoes",
            Brand = "Adidas",
            AvailableColors = new List<string> { "Brown", "Green", "Black" },
            Rating = 4.9,
            ReviewCount = 203,
            Reviews = new List<Review>()
        },
        new Product
        {
            Id = 5,
            Name = "Men's Tech Running Singlet",
            Description = "Ultra-lightweight running singlet with seamless construction and reflective details for visibility during early morning or evening runs.",
            Price = 1899.00m,
            Image = "https://images.pexels.com/photos/8612988/pexels-photo-8612988.jpeg?auto=compress&cs=tinysrgb&w=600",
            Sizes = new List<string> { "S", "M", "L", "XL", "XXL" },
            Category = "Men",
            SubCategory = "Apparel",
            Brand = "Nike",
            AvailableColors = new List<string> { "White", "Black", "Red" },
            Rating = 4.5,
            ReviewCount = 134,
            Reviews = new List<Review>()
        },
        
        // Women's Running Products
        new Product
        {
            Id = 6,
            Name = "Horizon Elite Women's Runner",
            Description = "Precision-engineered women's running shoes with adaptive fit technology. Provides exceptional energy return and plush cushioning for distance running.",
            Price = 8799.00m,
            Image = "https://images.pexels.com/photos/2529147/pexels-photo-2529147.jpeg?auto=compress&cs=tinysrgb&w=600",
            Sizes = new List<string> { "US 5", "US 6", "US 7", "US 8", "US 9", "US 10" },
            Category = "Women",
            SubCategory = "Running Shoes",
            Brand = "Nike",
            AvailableColors = new List<string> { "Pink", "White", "Purple" },
            Rating = 4.9,
            ReviewCount = 287,
            Reviews = new List<Review>
            {
                new Review { Id = 4, UserName = "Maria C.", Rating = 5, Comment = "Perfect for my marathon training! Super comfortable even on 30km runs.", Date = DateTime.Now.AddDays(-2) },
                new Review { Id = 5, UserName = "Anna S.", Rating = 5, Comment = "Love the fit and cushioning. Best investment for serious runners.", Date = DateTime.Now.AddDays(-7) },
                new Review { Id = 6, UserName = "Gina R.", Rating = 5, Comment = "Amazing support and stability. My go-to running shoes now!", Date = DateTime.Now.AddDays(-12) },
                new Review { Id = 7, UserName = "Patricia L.", Rating = 4, Comment = "Very comfortable, great for long distance. Slight sizing issue but overall excellent.", Date = DateTime.Now.AddDays(-18) },
                new Review { Id = 8, UserName = "Michelle A.", Rating = 5, Comment = "These shoes are a game changer! No more foot fatigue.", Date = DateTime.Now.AddDays(-22) },
                new Review { Id = 9, UserName = "Sarah D.", Rating = 5, Comment = "Worth every peso! Best running shoes I've ever owned.", Date = DateTime.Now.AddDays(-28) }
            }
        },
        new Product
        {
            Id = 7,
            Name = "Women's Velocity Racer",
            Description = "Lightweight racing flats designed for speed. Minimalist design with responsive foam for PR attempts and race day performance.",
            Price = 5999.00m,
            Image = "https://images.pexels.com/photos/2385477/pexels-photo-2385477.jpeg?auto=compress&cs=tinysrgb&w=600",
            Sizes = new List<string> { "US 5", "US 6", "US 7", "US 8", "US 9", "US 10" },
            Category = "Women",
            SubCategory = "Running Shoes",
            Brand = "Adidas",
            AvailableColors = new List<string> { "Yellow", "Black", "White" },
            Rating = 4.7,
            ReviewCount = 167,
            Reviews = new List<Review>
            {
                new Review { Id = 6, UserName = "Sofia R.", Rating = 5, Comment = "Shaved 2 minutes off my 10K time! Highly recommend.", Date = DateTime.Now.AddDays(-1) }
            }
        },
        new Product
        {
            Id = 8,
            Name = "Women's High-Impact Running Bra",
            Description = "Maximum support sports bra engineered for runners. Moisture-wicking fabric with adjustable straps and breathable mesh panels.",
            Price = 2899.00m,
            Image = "https://images.pexels.com/photos/6454243/pexels-photo-6454243.jpeg?auto=compress&cs=tinysrgb&w=600",
            Sizes = new List<string> { "XS", "S", "M", "L", "XL" },
            Category = "Women",
            SubCategory = "Apparel",
            Brand = "Nike",
            AvailableColors = new List<string> { "Black", "Grey", "Navy" },
            Rating = 4.8,
            ReviewCount = 234,
            Reviews = new List<Review>()
        },
        new Product
        {
            Id = 9,
            Name = "Women's Running Tights",
            Description = "Premium compression running tights with reflective elements. Features secure zip pocket and flatlock seams to prevent chafing.",
            Price = 3299.00m,
            Image = "https://images.pexels.com/photos/4056535/pexels-photo-4056535.jpeg?auto=compress&cs=tinysrgb&w=600",
            Sizes = new List<string> { "XS", "S", "M", "L", "XL" },
            Category = "Women",
            SubCategory = "Apparel",
            Brand = "Adidas",
            AvailableColors = new List<string> { "Black", "Purple", "Teal" },
            Rating = 4.6,
            ReviewCount = 192,
            Reviews = new List<Review>()
        },
        new Product
        {
            Id = 10,
            Name = "Women's Distance Runner Tank",
            Description = "Breathable running tank with mesh back panel. Lightweight, quick-drying fabric keeps you cool during long runs.",
            Price = 1699.00m,
            Image = "https://images.pexels.com/photos/5069432/pexels-photo-5069432.jpeg?auto=compress&cs=tinysrgb&w=600",
            Sizes = new List<string> { "XS", "S", "M", "L", "XL" },
            Category = "Women",
            SubCategory = "Apparel",
            Brand = "Nike",
            AvailableColors = new List<string> { "White", "Pink", "Light Blue" },
            Rating = 4.5,
            ReviewCount = 145,
            Reviews = new List<Review>()
        }
    };
    
    public static List<CartItem> Cart { get; } = new();
    public static List<WishlistItem> Wishlist { get; } = new();
    
    // Simulated purchase records - products that customer has received
    public static List<PurchaseRecord> PurchaseRecords { get; } = new()
    {
        new PurchaseRecord 
        { 
            ProductId = 1, 
            PurchaseDate = DateTime.Now.AddDays(-30),
            DeliveryDate = DateTime.Now.AddDays(-20)
        },
        new PurchaseRecord 
        { 
            ProductId = 6, 
            PurchaseDate = DateTime.Now.AddDays(-25),
            DeliveryDate = DateTime.Now.AddDays(-15)
        },
        new PurchaseRecord 
        { 
            ProductId = 3, 
            PurchaseDate = DateTime.Now.AddDays(-10),
            DeliveryDate = DateTime.Now.AddDays(-3)
        }
    };
}
