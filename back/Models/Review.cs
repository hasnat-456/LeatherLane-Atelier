using System;

namespace LeatherLane_Atelier.Models
{
    public class Review
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;

        public int Rating { get; set; } // 1-5
        public string? Title { get; set; }
        public string Comment { get; set; } = string.Empty;
        
        public int Helpful { get; set; } = 0;
        public bool Approved { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
