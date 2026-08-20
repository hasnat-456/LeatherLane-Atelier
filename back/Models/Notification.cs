using System;

namespace LeatherLane_Atelier.Models
{
    public class Notification
    {
        public int Id { get; set; }
        
        // Null means it's an Admin notification. Otherwise, it belongs to a specific user.
        public int? UserId { get; set; }
        public User User { get; set; }

        public string Title { get; set; }
        public string Message { get; set; }
        public string ActionUrl { get; set; }
        
        public bool IsRead { get; set; } = false;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
