using System;
using System.Collections.Generic;

namespace LeatherLane_Atelier.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal? OriginalPrice { get; set; }
        public string Category { get; set; } = string.Empty;
        public string? Subcategory { get; set; }
        
        public int? CategoryId { get; set; }
        public string AvailabilityStatus { get; set; } = "Available";
        
        public List<string> Images { get; set; } = new();
        public string? Thumbnail { get; set; }
        
        public string? LeatherType { get; set; }
        public List<string> Colors { get; set; } = new();
        public List<string> Sizes { get; set; } = new();
        public string? Material { get; set; }
        public string? Gender { get; set; }
        
        public int Stock { get; set; } = 0;
        public double Rating { get; set; } = 0;
        public int NumReviews { get; set; } = 0;
        
        public string? Sku { get; set; }
        public string? Weight { get; set; }
        public string? Dimensions { get; set; }
        public string? CareInstructions { get; set; }
        
        public bool IsNew { get; set; } = false;
        public bool IsBestseller { get; set; } = false;
        public bool IsFeatured { get; set; } = false;
        public decimal? Discount { get; set; }
        public bool Available { get; set; } = true;
        
        public List<string> Features { get; set; } = new();
        public List<ProductSpecification> Specifications { get; set; } = new();
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public class ProductSpecification
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        
        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;
    }
}