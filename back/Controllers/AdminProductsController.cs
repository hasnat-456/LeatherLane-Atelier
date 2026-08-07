using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LeatherLane_Atelier.Models;
using Microsoft.AspNetCore.Http;
using System.IO;
using Microsoft.EntityFrameworkCore;

namespace LeatherLane_Atelier.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("admin/products")]
    public class AdminProductsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public AdminProductsController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [HttpGet("")]
        public IActionResult Index()
        {
            var products = _context.Products.ToList();
            return View(products);
        }

        [HttpGet("add")]
        public IActionResult AddProduct()
        {
            return View();
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddProduct(Product product, IFormFile imageFile)
        {
            if (imageFile != null && imageFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(_env.WebRootPath, "images", "products");
                Directory.CreateDirectory(uploadsFolder);
                
                var uniqueFileName = Guid.NewGuid().ToString() + "_" + imageFile.FileName;
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }
                
                product.Thumbnail = "/images/products/" + uniqueFileName;
            }

            product.Slug = product.Name.ToLower().Replace(" ", "-") + "-" + Guid.NewGuid().ToString().Substring(0, 5);
            
            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            // Background task to notify all users
            var serviceScopeFactory = HttpContext.RequestServices.GetService(typeof(Microsoft.Extensions.DependencyInjection.IServiceScopeFactory)) as Microsoft.Extensions.DependencyInjection.IServiceScopeFactory;
            if (serviceScopeFactory != null)
            {
                var productId = product.Id;
                var productName = product.Name;
                _ = Task.Run(async () =>
                {
                    using var scope = serviceScopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetService(typeof(ApplicationDbContext)) as ApplicationDbContext;
                    var emailSvc = scope.ServiceProvider.GetService(typeof(LeatherLane_Atelier.Services.IEmailService)) as LeatherLane_Atelier.Services.IEmailService;

                    if (db != null && emailSvc != null)
                    {
                        var users = await db.Users.ToListAsync();
                        foreach (var u in users)
                        {
                            db.Notifications.Add(new Notification
                            {
                                Title = "New Product Alert!",
                                Message = $"We just added {productName} to our collection! Check it out.",
                                ActionUrl = $"product-detail.html?id={productId}",
                                UserId = u.Id
                            });
                            _ = emailSvc.SendEmailAsync(u.Email, "New Product Alert!", $"Hi {u.Name},\n\nWe just added a new product to our store: {productName}. Visit our website to see more details!");
                        }
                        await db.SaveChangesAsync();
                    }
                });
            }

            return RedirectToAction("Index");
        }
    }
}
