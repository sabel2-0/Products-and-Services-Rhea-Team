using Microsoft.EntityFrameworkCore;
using MyAspNetApp.Models;

namespace MyAspNetApp.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // Core Product Tables
        public DbSet<DbProduct> Products { get; set; }
        public DbSet<DbProductVariant> ProductVariants { get; set; }
        
        // Image Tables (Retained from AppDbContext (1).cs)
        public DbSet<DbProductColorImage> ProductColorImages { get; set; }
        
        // Size Guide Tables (Integrated from AppDbContext.cs)
        public DbSet<DbSizeGuide> SizeGuides { get; set; }
        public DbSet<DbSizeGuideImage> SizeGuideImages { get; set; }
        
        // Review Tables
        public DbSet<DbReview> Reviews { get; set; }
        public DbSet<DbReviewImage> ReviewImages { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            // Suppress the pending model changes warning to allow migrations that drop columns
            optionsBuilder.ConfigureWarnings(w => 
                w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        }
    }
}