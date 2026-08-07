using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LeatherLane_Atelier.Models
{
    public class TimelineEvent
    {
        [Key]
        public int EventId { get; set; }

        public int ReferenceId { get; set; }
        public string Type { get; set; } = string.Empty; // "Order", "Exchange", "Return"
        
        public string Status { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        
        public DateTime EventDateTime { get; set; } = DateTime.UtcNow;
        
        public string? CourierName { get; set; }
        public string? TrackingNumber { get; set; }
        public string? Notes { get; set; }
        
        public string CreatedBy { get; set; } = string.Empty; // "System", "Admin", "Customer"
        
        public bool IsCurrent { get; set; } = false;
        public bool IsCompleted { get; set; } = false;
    }
}
