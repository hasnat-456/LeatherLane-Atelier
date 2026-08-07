using System;
using System.ComponentModel.DataAnnotations;

namespace LeatherLane_Atelier.Models
{
    public class ExchangeStatusHistory
    {
        [Key]
        public int HistoryId { get; set; }

        public int ExchangeId { get; set; }
        public ExchangeRequest Exchange { get; set; } = null!;

        public string Status { get; set; } = string.Empty;
        public string ChangedBy { get; set; } = string.Empty; // e.g., "Customer", "Admin", "System"
        public string? Remarks { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
