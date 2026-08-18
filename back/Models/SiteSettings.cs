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
        
        public string FacebookUrl { get; set; } = "#";
        public string InstagramUrl { get; set; } = "https://www.instagram.com/leatherlane_atelier";
        public string WhatsAppUrl { get; set; } = "#";
        public string TikTokUrl { get; set; } = "#";
        
        public string AboutUsText { get; set; } = "";
        
        public string SizeGuideData { get; set; } = "[]";
        
        public string HeroSliderImages { get; set; } = "[\"images/1.jpeg\",\"images/2.jpeg\",\"images/3.jpeg\",\"images/4.jpeg\"]";
        public string CraftSliderImages { get; set; } = "[\"upload/iiiii.mp4\",\"upload/2.webp\",\"upload/3.jpg\",\"upload/4.avif\",\"upload/5.jpg\",\"upload/6.jpg\",\"upload/7.avif\"]";
    }
}
