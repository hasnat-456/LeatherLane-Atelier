using System.ComponentModel.DataAnnotations;

namespace LeatherLane_Atelier.Models
{
    public class SiteSettings
    {
        [Key]
        public int Id { get; set; }
        
        public string Address { get; set; } = "Ward no 7, street 14, house 143, Mohallah Ansarian Jhang City, Punjab, 35200";
        public string Email { get; set; } = "leatherlaneatelier@gmail.com";
        public string Phone { get; set; } = "03376306162";
        public string BusinessHours { get; set; } = "Mon-Sat: 10 AM - 7 PM";
        
        public string FacebookUrl { get; set; } = "";
        public string InstagramUrl { get; set; } = "https://www.instagram.com/leatherlane_atelier";
        public string WhatsAppUrl { get; set; } = "03376306162";
        public string TikTokUrl { get; set; } = "";
        
        public string AboutUsText { get; set; } = "";
        
        public string SizeGuideData { get; set; } = "[]";
        
        public string HeroSliderImages { get; set; } = "[\"images/1.jpeg\",\"images/2.jpeg\",\"images/3.jpeg\",\"images/4.jpeg\"]";
        public string CraftSliderImages { get; set; } = "[\"upload/iiiii.mp4\",\"upload/2.webp\",\"upload/3.jpg\",\"upload/4.avif\",\"upload/5.jpg\",\"upload/6.jpg\",\"upload/7.avif\"]";
        
        public string? MenCollectionImage { get; set; } = "https://images.unsplash.com/photo-1617137968427-85924c800a22?ixlib=rb-4.0.3&auto=format&fit=crop&w=600&q=80";
        public string? WomenCollectionImage { get; set; } = "https://images.unsplash.com/photo-1543163521-1bf539c55dd2?ixlib=rb-4.0.3&auto=format&fit=crop&w=600&q=80";
        public string? ChildrenCollectionImage { get; set; } = "https://images.unsplash.com/photo-1514090458221-65bb69cf63e6?ixlib=rb-4.0.3&auto=format&fit=crop&w=600&q=80";
    }
}
