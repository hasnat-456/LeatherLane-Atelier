using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LeatherLane_Atelier.Models
{
    public class ExchangeRequest
    {
        [Key]
        public int ExchangeId { get; set; }

        public int OrderId { get; set; }
        public Transaction Order { get; set; } = null!;

        public int CustomerId { get; set; }
        public User Customer { get; set; } = null!;

        public int OrderItemId { get; set; }
        public TransactionItem OrderItem { get; set; } = null!;

        public int OriginalProductId { get; set; }
        public Product OriginalProduct { get; set; } = null!;

        public int? ReplacementProductId { get; set; }
        public Product? ReplacementProduct { get; set; }

        public string Reason { get; set; } = string.Empty;
        public string? OtherReason { get; set; }

        public string Status { get; set; } = "Pending Review"; // Pending Review, Approved, Rejected, Pickup Scheduled, Inspection, Replacement Preparing, Replacement Shipped, Completed, Cancelled

        public DateTime RequestDate { get; set; } = DateTime.UtcNow;
        public DateTime? ApprovalDate { get; set; }
        public DateTime? CompletionDate { get; set; }

        public string? RejectedReason { get; set; }
        public string? AdminRemarks { get; set; }

        public DateTime ExchangeDeadline { get; set; }

        public DateTime? PickupDate { get; set; }
        public DateTime? InspectionDate { get; set; }
        public DateTime? ReplacementShipmentDate { get; set; }

        public string? TrackingNumber { get; set; }
        public string? CourierName { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal PriceDifference { get; set; }

        public bool CustomerConfirmed { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public List<ExchangeImage> Images { get; set; } = new();
        public List<ExchangeStatusHistory> StatusHistory { get; set; } = new();
    }
}
