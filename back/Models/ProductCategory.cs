using System;

namespace LeatherLane_Atelier.Models
{
    public class ProductCategory
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Audience { get; set; } = "Men";
        public bool IsActive { get; set; } = true;
        public int DisplayOrder { get; set; } = 0;
    }
}
