using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LeatherLane_Atelier.Models
{
    public class ReturnRequest
    {
        [Key]
        public int ReturnId { get; set; }

        public int OrderId { get; set; }
        public Transaction Order { get; set; } = null!;

        public int CustomerId { get; set; }
        public User Customer { get; set; } = null!;

        public int OrderItemId { get; set; }
        public TransactionItem OrderItem { get; set; } = null!;

        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;

        public string Reason { get; set; } = string.Empty;
        public string? OtherReason { get; set; }

        public string Status { get; set; } = "Return Requested"; // Return Requested, Under Review, Approved, Pickup Scheduled, Picked Up, Warehouse Received, Inspection, Refund Approved, Refund Sent, Completed, Rejected, Cancelled

        public DateTime RequestDate { get; set; } = DateTime.UtcNow;
        public DateTime? ApprovalDate { get; set; }
        public DateTime? CompletionDate { get; set; }

        public string? RejectedReason { get; set; }
        public string? AdminRemarks { get; set; }

        public DateTime? PickupDate { get; set; }
        public DateTime? InspectionDate { get; set; }

        public string? TrackingNumber { get; set; }
        public string? CourierName { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal RefundAmount { get; set; }

        public bool CustomerConfirmed { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public List<ReturnImage> Images { get; set; } = new();
    }

    public class ReturnImage
    {
        [Key]
        public int ImageId { get; set; }
        
        public int ReturnId { get; set; }
        public ReturnRequest Return { get; set; } = null!;

        public string ImageUrl { get; set; } = string.Empty;
        public string ImageType { get; set; } = "General"; // "General", "Damage", "Packaging"
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    }
}
