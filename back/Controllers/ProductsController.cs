using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using LeatherLane_Atelier.Models;

namespace LeatherLane_Atelier.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ProductsController(ApplicationDbContext context)
        {
            _context = context;
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

            var products = await query.OrderByDescending(p => p.CreatedAt)
                .Select(p => new {
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
                    p.CreatedAt
                })
                .ToListAsync();
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
                _ = emailService.SendEmailAsync("muhammadbilalarifsheukh@gmail.com", "New Product Review", $"A customer left a {dto.Rating}-star review for {product.Name}:\n\n{dto.Comment}");
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Review added successfully" });
        }
    }

    public class ReviewDto
    {
        public int Rating { get; set; }
        public string? Title { get; set; }
        public string Comment { get; set; } = string.Empty;
    }
}
