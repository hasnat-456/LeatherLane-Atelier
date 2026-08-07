using System;
using System.Collections.Generic;

namespace LeatherLane_Atelier.Models
{
    public class Transaction
    {
        public int Id { get; set; }
        
        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public List<TransactionItem> Items { get; set; } = new();

        public decimal TotalAmount { get; set; }

        public string Status { get; set; } = "pending"; // pending, completed, cancelled

        public string? ShippingName { get; set; }
        public string? ShippingPhone { get; set; }
        public string? ShippingAddress { get; set; }

        public string PaymentMethod { get; set; } = string.Empty; // jazzcash, easypaisa, card

        public string? PaymentPhoneNumber { get; set; }
        public string? PaymentCardLast4 { get; set; }

        // Manual Payment Verification Fields
        public string? PaymentRefId { get; set; }
        public string? SenderName { get; set; }
        public string? SenderMobile { get; set; }
        public string? PaymentScreenshot { get; set; }
        public string? RejectionReason { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public class TransactionItem
    {
        public int Id { get; set; }
        public int? ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; }

        public int TransactionId { get; set; }
        
        [System.Text.Json.Serialization.JsonIgnore]
        public Transaction Transaction { get; set; } = null!;

        public bool HasBeenExchanged { get; set; } = false;
        public int? ExchangeRequestId { get; set; }
        public bool ExchangeCompleted { get; set; } = false;
        public DateTime? ExchangeDate { get; set; }

        public bool HasBeenReturned { get; set; } = false;
        public int? ReturnRequestId { get; set; }
        public bool ReturnCompleted { get; set; } = false;
        public DateTime? ReturnDate { get; set; }
    }
}
