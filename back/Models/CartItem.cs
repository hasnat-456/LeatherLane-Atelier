using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LeatherLane_Atelier.Models
{
    public class CartItem
    {
        public int Id { get; set; }
        
        [Required]
        public int UserId { get; set; }
        
        [Required]
        public int ProductId { get; set; }
        
        [Required]
        public int Quantity { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [JsonIgnore]
        public User User { get; set; } = null!;
        
        public Product Product { get; set; } = null!;
    }
}
