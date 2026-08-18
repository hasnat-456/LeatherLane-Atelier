using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LeatherLane_Atelier.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Email { get; set; } = string.Empty;

        public string? Phone { get; set; }

        [Required]
        public string Password { get; set; } = string.Empty; // Store hashed password

        public bool IsVerified { get; set; } = false;

        public string? VerificationOTP { get; set; }
        public DateTime? VerificationOTPExpires { get; set; }

        public string? ResetPasswordToken { get; set; }
        public DateTime? ResetPasswordExpiry { get; set; }
        
        public string Role { get; set; } = "User";
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public bool ReceivesMarketingEmails { get; set; } = false;
    }
}
