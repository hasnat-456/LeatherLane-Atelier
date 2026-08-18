using System;
using System.Collections.Generic;
using System.Linq;

namespace LeatherLane_Atelier.Models
{
    public class Deal
    {
        public int Id { get; set; }
        
        // "Product", "Category", "All"
        public string Scope { get; set; } = "All";
        
        public int? ProductId { get; set; }
        public int? CategoryId { get; set; }
        
        // "Percentage", "FixedAmount"
        public string DiscountType { get; set; } = "Percentage";
        
        public decimal DiscountValue { get; set; }
        
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public static void ApplyActiveDeals(Product product, IEnumerable<Deal> activeDeals)
        {
            var applicableDeals = activeDeals.Where(d => 
                (d.Scope == "Product" && d.ProductId == product.Id) ||
                (d.Scope == "Category" && d.CategoryId == product.CategoryId) ||
                (d.Scope == "All")
            ).ToList();
            
            if (applicableDeals.Any())
            {
                var bestDeal = applicableDeals.OrderBy(d => 
                    d.Scope == "Product" ? 1 : 
                    d.Scope == "Category" ? 2 : 3
                ).First();
                
                // If the product already has an OriginalPrice set and it's higher than Price, use it. Otherwise use Price.
                decimal originalPrice = (product.OriginalPrice.HasValue && product.OriginalPrice.Value > 0 && product.OriginalPrice.Value > product.Price) ? product.OriginalPrice.Value : product.Price;
                decimal finalPrice = originalPrice;
                
                if (bestDeal.DiscountType == "Percentage")
                {
                    finalPrice = originalPrice - (originalPrice * (bestDeal.DiscountValue / 100));
                }
                else if (bestDeal.DiscountType == "FixedAmount")
                {
                    finalPrice = originalPrice - bestDeal.DiscountValue;
                }
                
                if (finalPrice < 0) finalPrice = 0;
                
                if (finalPrice < originalPrice) 
                {
                    product.OriginalPrice = originalPrice;
                    product.Price = finalPrice;
                    
                    product.Discount = bestDeal.DiscountType == "Percentage" 
                        ? (int)bestDeal.DiscountValue 
                        : (int)Math.Round((originalPrice - finalPrice) / originalPrice * 100);
                }
            }
        }
    }
}
