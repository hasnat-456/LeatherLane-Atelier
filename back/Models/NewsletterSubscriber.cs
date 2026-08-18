using System;
using System.ComponentModel.DataAnnotations;

namespace LeatherLane_Atelier.Models
{
    public class NewsletterSubscriber
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        public DateTime SubscribedAt { get; set; } = DateTime.UtcNow;
    }
}
