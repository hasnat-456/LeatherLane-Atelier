using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LeatherLane_Atelier.Controllers
{
    public class HomeController : Controller
    {
        private readonly LeatherLane_Atelier.Models.ApplicationDbContext _context;

        public HomeController(LeatherLane_Atelier.Models.ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Fetch top 6 featured products for the home page
            var products = await _context.Products.Take(6).ToListAsync();
            return View(products);
        }

        [HttpGet("about")]
        public IActionResult About()
        {
            return View();
        }
        
        [HttpGet("contact")]
        public IActionResult Contact()
        {
            return View();
        }
    }
}
