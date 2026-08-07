using System;
using System.ComponentModel.DataAnnotations;

namespace LeatherLane_Atelier.Models
{
    public class ExchangeImage
    {
        [Key]
        public int ImageId { get; set; }

        public int ExchangeId { get; set; }
        public ExchangeRequest Exchange { get; set; } = null!;

        public string ImageUrl { get; set; } = string.Empty;
        public string ImageType { get; set; } = string.Empty; // Front, Back, Packaging

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
