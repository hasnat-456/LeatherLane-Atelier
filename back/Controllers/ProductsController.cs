using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using LeatherLane_Atelier.Models;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using System.Text.RegularExpressions;

namespace LeatherLane_Atelier.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public ProductsController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [HttpGet]
        public async Task<IActionResult> GetProducts([FromQuery] string? category, [FromQuery] string? search, [FromQuery] bool? isFeatured)
        {
            var query = _context.Products.Where(p => p.AvailabilityStatus != "Discontinued").AsQueryable();

            if (!string.IsNullOrEmpty(category))
                query = query.Where(p => p.Category == category);

            if (!string.IsNullOrEmpty(search))
                query = query.Where(p => p.Name.Contains(search) || p.Description.Contains(search));

            if (isFeatured.HasValue && isFeatured.Value)
                query = query.Where(p => p.IsFeatured);

            var productsEntities = await query.OrderByDescending(p => p.CreatedAt).ToListAsync();
            
            var now = DateTime.Now;
            var activeDeals = await _context.Deals.Where(d => d.StartTime <= now && d.EndTime >= now).ToListAsync();
            
            foreach(var p in productsEntities)
            {
                Deal.ApplyActiveDeals(p, activeDeals);
            }
            
            var products = productsEntities.Select(p => new {
                    p.Id,
                    p.Name,
                    p.Slug,
                    p.Price,
                    p.OriginalPrice,
                    p.Category,
                    p.CategoryId,
                    p.AvailabilityStatus,
                    p.Subcategory,
                    p.Thumbnail,
                    p.Rating,
                    p.NumReviews,
                    p.IsNew,
                    p.IsBestseller,
                    p.IsFeatured,
                    p.Discount,
                    p.Available,
                    p.Sizes,
                    p.CreatedAt
            }).ToList();
            return Ok(products);
        }

        [HttpGet("categories")]
        public async Task<IActionResult> GetActiveCategories()
        {
            var categories = await _context.ProductCategories
                .Where(c => c.IsActive)
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name)
                .ToListAsync();
            return Ok(categories);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetProduct(int id)
        {
            var product = await _context.Products
                .Include(p => p.Specifications)
                .FirstOrDefaultAsync(p => p.Id == id);
                
            if (product == null)
                return NotFound(new { message = "Product not found" });

            var now = DateTime.Now;
            var activeDeals = await _context.Deals.Where(d => d.StartTime <= now && d.EndTime >= now).ToListAsync();
            Deal.ApplyActiveDeals(product, activeDeals);

            return Ok(product);
        }

        [HttpPost]
        public async Task<IActionResult> CreateProduct([FromBody] Product product)
        {
            if (product.CategoryId.HasValue)
            {
                var category = await _context.ProductCategories.FindAsync(product.CategoryId.Value);
                if (category != null)
                {
                    product.Category = category.Name;
                }
            }

            if (string.IsNullOrEmpty(product.AvailabilityStatus))
            {
                product.AvailabilityStatus = "Available";
            }

            if (product.AvailabilityStatus == "Available")
            {
                product.Available = true;
                product.Stock = 9999;
            }
            else
            {
                product.Available = false;
                product.Stock = 0;
            }

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

            return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] Product product)
        {
            var existingProduct = await _context.Products.FindAsync(id);
            if (existingProduct == null) return NotFound();

            existingProduct.Name = product.Name;
            existingProduct.CategoryId = product.CategoryId;
            existingProduct.IsFeatured = product.IsFeatured;
            
            if (product.CategoryId.HasValue)
            {
                var category = await _context.ProductCategories.FindAsync(product.CategoryId.Value);
                if (category != null)
                {
                    existingProduct.Category = category.Name;
                }
            }
            else
            {
                existingProduct.Category = product.Category;
            }

            existingProduct.Price = product.Price;
            existingProduct.Description = product.Description;
            
            existingProduct.AvailabilityStatus = string.IsNullOrEmpty(product.AvailabilityStatus) 
                ? "Available" 
                : product.AvailabilityStatus;

            if (existingProduct.AvailabilityStatus == "Available")
            {
                existingProduct.Available = true;
                existingProduct.Stock = 9999;
            }
            else
            {
                existingProduct.Available = false;
                existingProduct.Stock = 0;
            }

            existingProduct.Slug = product.Slug;
            existingProduct.Sizes = product.Sizes;
            
            if (!string.IsNullOrEmpty(product.Thumbnail))
            {
                existingProduct.Thumbnail = product.Thumbnail;
                existingProduct.Images = product.Images;
            }

            await _context.SaveChangesAsync();
            return Ok(existingProduct);
        }
        [HttpGet("{id}/reviews")]
        public async Task<IActionResult> GetProductReviews(int id)
        {
            var reviews = await _context.Reviews
                .Include(r => r.User)
                .Where(r => r.ProductId == id && r.Approved)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new {
                    id = r.Id,
                    rating = r.Rating,
                    title = r.Title,
                    comment = r.Comment,
                    userName = r.User.Name,
                    createdAt = r.CreatedAt
                })
                .ToListAsync();

            return Ok(reviews);
        }

        [Authorize]
        [HttpPost("{id}/reviews")]
        public async Task<IActionResult> AddReview(int id, [FromBody] ReviewDto dto)
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                return Unauthorized();

            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            var review = new Review
            {
                ProductId = id,
                UserId = userId,
                Rating = dto.Rating,
                Title = dto.Title,
                Comment = dto.Comment,
                Approved = true, // Auto-approve for now
                CreatedAt = DateTime.UtcNow
            };

            _context.Reviews.Add(review);

            // Update product average rating
            product.NumReviews += 1;
            // new average = old average + (new value - old average) / n
            product.Rating = product.Rating + (dto.Rating - product.Rating) / product.NumReviews;

            _context.Notifications.Add(new Notification
            {
                Title = "New Product Review",
                Message = $"A customer just left a {dto.Rating}-star review for {product.Name}.",
                ActionUrl = $"product-detail.html?id={id}",
                UserId = null // Admin
            });

            // The IEmailService would normally be injected here, but to avoid changing constructor
            // I will use HttpContext.RequestServices to get it directly
            var emailService = HttpContext.RequestServices.GetService(typeof(LeatherLane_Atelier.Services.IEmailService)) as LeatherLane_Atelier.Services.IEmailService;
            if (emailService != null)
            {
                _ = emailService.SendEmailAsync("leatherlaneatelier@gmail.com", "New Product Review", $"A customer left a {dto.Rating}-star review for {product.Name}:\n\n{dto.Comment}");
            }

            if (userId > 0)
            {
                // Notification for Customer
                _context.Notifications.Add(new Notification
                {
                    Title = "Review Submitted",
                    Message = $"Thank you for reviewing {product.Name}! Your {dto.Rating}-star review is now visible.",
                    ActionUrl = $"product-detail.html?id={id}",
                    UserId = userId
                });
                
                var userObj = await _context.Users.FindAsync(userId);
                if (userObj != null && emailService != null)
                {
                    _ = emailService.SendEmailAsync(userObj.Email, "Thank You For Your Review!", $"We appreciate your {dto.Rating}-star review on {product.Name}. Your feedback helps us maintain our timeless craftsmanship!");
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Review added successfully" });
        }
    
        [HttpPost("upload-images")]
        public async Task<IActionResult> UploadImages([FromForm] List<IFormFile> images)
        {
            var uploadedUrls = new List<string>();
            if (images == null || images.Count == 0) return BadRequest("No images received.");
            
            var currentDir = Directory.GetCurrentDirectory();
            var frontPath = currentDir.EndsWith("back", StringComparison.OrdinalIgnoreCase) 
                ? Path.Combine(currentDir, "..", "front") 
                : Path.Combine(currentDir, "front");
            var uploadsFolder = Path.Combine(frontPath, "images", "products");
            Directory.CreateDirectory(uploadsFolder);
            
            foreach (var file in images)
            {
                if (file.Length > 0)
                {
                    var uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName.Replace(" ", "_");
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }
                    uploadedUrls.Add("/images/products/" + uniqueFileName);
                }
            }
            
            return Ok(uploadedUrls);
        }

        [HttpGet("migrate-base64")]
        public async Task<IActionResult> MigrateBase64Images()
        {
            var products = await _context.Products.ToListAsync();
            int migratedCount = 0;
            
            var currentDir = Directory.GetCurrentDirectory();
            var frontPath = currentDir.EndsWith("back", StringComparison.OrdinalIgnoreCase) 
                ? Path.Combine(currentDir, "..", "front") 
                : Path.Combine(currentDir, "front");
            var uploadsFolder = Path.Combine(frontPath, "images", "products");
            Directory.CreateDirectory(uploadsFolder);

            foreach (var p in products)
            {
                bool modified = false;
                
                // Migrate Thumbnail
                if (!string.IsNullOrEmpty(p.Thumbnail) && p.Thumbnail.StartsWith("data:image"))
                {
                    p.Thumbnail = SaveBase64ToDisk(p.Thumbnail, uploadsFolder);
                    modified = true;
                }
                
                // Migrate Images array
                if (p.Images != null && p.Images.Count > 0)
                {
                    for (int i = 0; i < p.Images.Count; i++)
                    {
                        if (!string.IsNullOrEmpty(p.Images[i]) && p.Images[i].StartsWith("data:image"))
                        {
                            p.Images[i] = SaveBase64ToDisk(p.Images[i], uploadsFolder);
                            modified = true;
                        }
                    }
                }
                
                if (modified)
                {
                    migratedCount++;
                }
            }
            
            await _context.SaveChangesAsync();
            return Ok(new { message = $"Successfully migrated {migratedCount} products." });
        }
        
        private string SaveBase64ToDisk(string base64String, string uploadsFolder)
        {
            try 
            {
                var match = Regex.Match(base64String, @"data:image/(?<type>.+?);base64,(?<data>.+)");
                if (!match.Success) return base64String;
                
                string ext = match.Groups["type"].Value.Split(';')[0]; // handle cases like jpeg;charset=utf-8
                if (ext == "jpeg") ext = "jpg";
                
                string base64Data = match.Groups["data"].Value;
                byte[] bytes = Convert.FromBase64String(base64Data);
                
                string fileName = Guid.NewGuid().ToString() + "." + ext;
                string filePath = Path.Combine(uploadsFolder, fileName);
                
                System.IO.File.WriteAllBytes(filePath, bytes);
                return "/images/products/" + fileName;
            } 
            catch 
            {
                return base64String; // fallback
            }
        }

    }

    public class ReviewDto
    {
        public int Rating { get; set; }
        public string? Title { get; set; }
        public string Comment { get; set; } = string.Empty;
    
        }
}
