using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using LeatherLane_Atelier.Models;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using System.Text.RegularExpressions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using Microsoft.Extensions.Caching.Memory;

namespace LeatherLane_Atelier.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IMemoryCache _cache;

        public ProductsController(ApplicationDbContext context, IWebHostEnvironment env, IMemoryCache cache)
        {
            _context = context;
            _env = env;
            _cache = cache;
        }

        [HttpGet]
        public async Task<IActionResult> GetProducts([FromQuery] string? category, [FromQuery] string? search, [FromQuery] bool? isFeatured)
        {
            var query = _context.Products.AsNoTracking().Where(p => p.AvailabilityStatus == null || p.AvailabilityStatus != "Discontinued").AsQueryable();

            if (!string.IsNullOrEmpty(category))
                query = query.Where(p => p.Category == category);

            if (!string.IsNullOrEmpty(search))
                query = query.Where(p => p.Name.Contains(search) || p.Description.Contains(search) || (p.ProductId != null && p.ProductId.Contains(search)) || p.Category.Contains(search));

            if (isFeatured.HasValue && isFeatured.Value)
                query = query.Where(p => p.IsFeatured);

            var productsEntities = await query.OrderByDescending(p => p.CreatedAt).ToListAsync();
            
            // Cache active deals in memory for 30s to keep API responses under 2ms
            var activeDeals = await _cache.GetOrCreateAsync("ActiveDealsCacheKey", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30);
                var now = DateTime.Now;
                return await _context.Deals.AsNoTracking().Where(d => d.StartTime <= now && d.EndTime >= now).ToListAsync();
            }) ?? new List<Deal>();
            
            foreach(var p in productsEntities)
            {
                Deal.ApplyActiveDeals(p, activeDeals);
            }
            
            var products = productsEntities.Select(p => new {
                    p.Id,
                    ProductId = p.ProductId ?? LeatherLane_Atelier.Services.IdGenerator.GenerateProductId(p.Category),
                    p.Name,
                    p.Slug,
                    p.Price,
                    p.OriginalPrice,
                    p.Category,
                    p.CategoryId,
                    AvailabilityStatus = p.AvailabilityStatus ?? "Available",
                    p.Subcategory,
                    p.Thumbnail,
                    p.Images,
                    p.Description,
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

            if (string.IsNullOrEmpty(product.ProductId))
            {
                product.ProductId = LeatherLane_Atelier.Services.IdGenerator.GenerateProductId(product.Category);
                await _context.SaveChangesAsync();
            }

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
            else if (!string.IsNullOrEmpty(product.Category))
            {
                var category = await _context.ProductCategories.FirstOrDefaultAsync(c => c.Name == product.Category);
                if (category != null)
                {
                    product.CategoryId = category.Id;
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

            if (string.IsNullOrEmpty(product.ProductId))
            {
                string candidateId;
                do
                {
                    candidateId = LeatherLane_Atelier.Services.IdGenerator.GenerateProductId(product.Category);
                } while (await _context.Products.AnyAsync(p => p.ProductId == candidateId));
                product.ProductId = candidateId;
            }

            var currentDir = Directory.GetCurrentDirectory();
            var frontPath = currentDir.EndsWith("back", StringComparison.OrdinalIgnoreCase) 
                ? Path.Combine(currentDir, "..", "front") 
                : Path.Combine(currentDir, "front");
            var uploadsFolder = Path.Combine(frontPath, "images", "products");
            Directory.CreateDirectory(uploadsFolder);

            // Clean any base64 images to real files
            if (!string.IsNullOrEmpty(product.Thumbnail) && product.Thumbnail.StartsWith("data:image"))
            {
                product.Thumbnail = SaveBase64ToDisk(product.Thumbnail, uploadsFolder);
            }
            if (product.Images != null)
            {
                for (int i = 0; i < product.Images.Count; i++)
                {
                    if (!string.IsNullOrEmpty(product.Images[i]) && product.Images[i].StartsWith("data:image"))
                    {
                        product.Images[i] = SaveBase64ToDisk(product.Images[i], uploadsFolder);
                    }
                }
            }
            if (string.IsNullOrEmpty(product.Thumbnail) && product.Images != null && product.Images.Count > 0)
            {
                product.Thumbnail = product.Images[0];
            }

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            // Background task to notify all users
            var serviceScopeFactory = HttpContext.RequestServices.GetService(typeof(Microsoft.Extensions.DependencyInjection.IServiceScopeFactory)) as Microsoft.Extensions.DependencyInjection.IServiceScopeFactory;
            if (serviceScopeFactory != null)
            {
                var productId = product.Id;
                var productName = product.Name;
                var productCode = product.ProductId;
                var productCategory = product.Category ?? "Artisan Footwear";
                var productPrice = product.Price;
                var productThumbnail = product.Thumbnail;
                var productSizes = product.Sizes;

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
                            var htmlEmail = LeatherLane_Atelier.Services.EmailTemplateBuilder.BuildNewProductAlertEmail(
                                u.Name, 
                                productName, 
                                productCode, 
                                productCategory, 
                                productPrice, 
                                productThumbnail, 
                                (productSizes != null && productSizes.Count > 0 ? string.Join(", ", productSizes) : null), 
                                productId
                            );
                            _ = emailSvc.SendEmailAsync(u.Email, $"New Arrival: {productName} | LeatherLane Atelier", htmlEmail);
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

            var currentDir = Directory.GetCurrentDirectory();
            var frontPath = currentDir.EndsWith("back", StringComparison.OrdinalIgnoreCase) 
                ? Path.Combine(currentDir, "..", "front") 
                : Path.Combine(currentDir, "front");
            var uploadsFolder = Path.Combine(frontPath, "images", "products");
            Directory.CreateDirectory(uploadsFolder);

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
                if (product.Thumbnail.StartsWith("data:image"))
                {
                    existingProduct.Thumbnail = SaveBase64ToDisk(product.Thumbnail, uploadsFolder);
                }
                else
                {
                    existingProduct.Thumbnail = product.Thumbnail;
                }
            }

            if (product.Images != null && product.Images.Count > 0)
            {
                var cleanedImages = new List<string>();
                foreach(var img in product.Images)
                {
                    if (!string.IsNullOrEmpty(img) && img.StartsWith("data:image"))
                    {
                        cleanedImages.Add(SaveBase64ToDisk(img, uploadsFolder));
                    }
                    else if (!string.IsNullOrEmpty(img))
                    {
                        cleanedImages.Add(img);
                    }
                }
                existingProduct.Images = cleanedImages;
                if (string.IsNullOrEmpty(existingProduct.Thumbnail))
                {
                    existingProduct.Thumbnail = cleanedImages[0];
                }
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

            // Find user's latest order containing this product to get Order ID
            var latestOrder = await _context.Transactions
                .Include(t => t.Items)
                .Where(t => t.UserId == userId && t.Items.Any(i => i.ProductId == id))
                .OrderByDescending(t => t.CreatedAt)
                .FirstOrDefaultAsync();

            string orderIdDisplay = latestOrder?.OrderId ?? ("LLA-PROD-" + product.Id);

            var emailService = HttpContext.RequestServices.GetService(typeof(LeatherLane_Atelier.Services.IEmailService)) as LeatherLane_Atelier.Services.IEmailService;
            var userObj = await _context.Users.FindAsync(userId);

            if (emailService != null)
            {
                var adminEmailHtml = LeatherLane_Atelier.Services.EmailTemplateBuilder.BuildAdminReviewAlertEmail(
                    orderIdDisplay, 
                    userObj?.Name ?? "Customer", 
                    userObj?.Email ?? "customer@example.com", 
                    product.Name, 
                    product.ProductId, 
                    product.Image, 
                    dto.Rating, 
                    dto.Comment
                );

                var adminEmails = await _context.Users.Where(u => u.Role == "Admin").Select(u => u.Email).ToListAsync();
                adminEmails.Add("leatherlaneatelier@gmail.com");
                foreach (var aEmail in adminEmails.Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    _ = emailService.SendEmailAsync(aEmail, $"New Customer Review: {product.Name} (Order {orderIdDisplay})", adminEmailHtml);
                }
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
                
                if (userObj != null && emailService != null)
                {
                    var htmlEmail = LeatherLane_Atelier.Services.EmailTemplateBuilder.BuildCustomerReviewConfirmationEmail(
                        userObj.Name, 
                        orderIdDisplay, 
                        product.Name, 
                        product.ProductId, 
                        product.Image, 
                        dto.Rating, 
                        dto.Comment
                    );
                    _ = emailService.SendEmailAsync(userObj.Email, $"Thank You For Reviewing {product.Name} | LeatherLane Atelier", htmlEmail);
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Review added successfully" });
        }
    
        [HttpPost("upload-images")]
        [DisableRequestSizeLimit]
        public async Task<IActionResult> UploadImages()
        {
            var uploadedUrls = new List<string>();
            var formFiles = Request.Form.Files;
            if (formFiles == null || formFiles.Count == 0) return BadRequest(new { message = "No images received." });
            
            var currentDir = Directory.GetCurrentDirectory();
            var frontPath = currentDir.EndsWith("back", StringComparison.OrdinalIgnoreCase) 
                ? Path.Combine(currentDir, "..", "front") 
                : Path.Combine(currentDir, "front");
            frontPath = Path.GetFullPath(frontPath);
            var uploadsFolder = Path.Combine(frontPath, "images", "products");
            Directory.CreateDirectory(uploadsFolder);

            var parentFront = Path.Combine(currentDir, "..", "front", "images", "products");
            bool syncToParent = Directory.Exists(Path.Combine(currentDir, "..", "front"));
            if (syncToParent) Directory.CreateDirectory(parentFront);

            var hdFiles = formFiles.GetFiles("images");
            var thumbFiles = formFiles.GetFiles("thumbnails");

            // If files were sent with specific keys (images / thumbnails)
            if (hdFiles.Count > 0)
            {
                for (int i = 0; i < hdFiles.Count; i++)
                {
                    var hdFile = hdFiles[i];
                    if (hdFile.Length > 0)
                    {
                        var baseName = Guid.NewGuid().ToString("N");
                        var hdFileName = baseName + ".jpg";
                        var thumbFileName = "thumb_" + baseName + ".jpg";
                        var hdPath = Path.Combine(uploadsFolder, hdFileName);
                        var thumbPath = Path.Combine(uploadsFolder, thumbFileName);

                        // 1. Save HD file directly
                        using (var outStream = new FileStream(hdPath, FileMode.Create))
                        {
                            await hdFile.CopyToAsync(outStream);
                        }

                        // 2. Save thumbnail file directly if provided, or generate fast fallback
                        if (i < thumbFiles.Count && thumbFiles[i].Length > 0)
                        {
                            using (var thumbOut = new FileStream(thumbPath, FileMode.Create))
                            {
                                await thumbFiles[i].CopyToAsync(thumbOut);
                            }
                        }
                        else
                        {
                            try
                            {
                                using var stream = hdFile.OpenReadStream();
                                using var image = await SixLabors.ImageSharp.Image.LoadAsync(stream);
                                using var thumb = image.Clone(x => x.Resize(new ResizeOptions
                                {
                                    Size = new SixLabors.ImageSharp.Size(180, 180),
                                    Mode = ResizeMode.Crop
                                }));
                                await thumb.SaveAsync(thumbPath, new JpegEncoder { Quality = 75 });
                            }
                            catch {}
                        }

                        if (syncToParent)
                        {
                            try 
                            {
                                System.IO.File.Copy(hdPath, Path.Combine(parentFront, hdFileName), true);
                                if (System.IO.File.Exists(thumbPath))
                                    System.IO.File.Copy(thumbPath, Path.Combine(parentFront, thumbFileName), true);
                            } catch {}
                        }

                        uploadedUrls.Add("/images/products/" + hdFileName);
                    }
                }
            }
            else
            {
                // Fallback for general multipart files
                foreach (var file in formFiles)
                {
                    if (file.Length > 0)
                    {
                        var baseName = Guid.NewGuid().ToString("N");
                        var hdFileName = baseName + ".jpg";
                        var thumbFileName = "thumb_" + baseName + ".jpg";
                        var hdPath = Path.Combine(uploadsFolder, hdFileName);
                        var thumbPath = Path.Combine(uploadsFolder, thumbFileName);

                        using (var outStream = new FileStream(hdPath, FileMode.Create))
                        {
                            await file.CopyToAsync(outStream);
                        }

                        try
                        {
                            using var stream = file.OpenReadStream();
                            using var image = await SixLabors.ImageSharp.Image.LoadAsync(stream);
                            using var thumb = image.Clone(x => x.Resize(new ResizeOptions
                            {
                                Size = new SixLabors.ImageSharp.Size(180, 180),
                                Mode = ResizeMode.Crop
                            }));
                            await thumb.SaveAsync(thumbPath, new JpegEncoder { Quality = 75 });
                        }
                        catch {}

                        if (syncToParent)
                        {
                            try 
                            {
                                System.IO.File.Copy(hdPath, Path.Combine(parentFront, hdFileName), true);
                                if (System.IO.File.Exists(thumbPath))
                                    System.IO.File.Copy(thumbPath, Path.Combine(parentFront, thumbFileName), true);
                            } catch {}
                        }

                        uploadedUrls.Add("/images/products/" + hdFileName);
                    }
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
        
        public static string SaveBase64ToDisk(string base64String, string uploadsFolder)
        {
            try 
            {
                var match = Regex.Match(base64String, @"data:image/(?<type>.+?);base64,(?<data>.+)");
                if (!match.Success) return base64String;
                
                string ext = "jpg";
                string base64Data = match.Groups["data"].Value;
                byte[] bytes = Convert.FromBase64String(base64Data);
                
                string baseName = Guid.NewGuid().ToString("N");
                string fileName = baseName + "." + ext;
                string thumbName = "thumb_" + baseName + "." + ext;
                string filePath = Path.Combine(uploadsFolder, fileName);
                string thumbPath = Path.Combine(uploadsFolder, thumbName);
                
                using var ms = new MemoryStream(bytes);
                using var image = SixLabors.ImageSharp.Image.Load(ms);
                if (image.Width > 1200 || image.Height > 1200)
                {
                    image.Mutate(x => x.Resize(new ResizeOptions
                    {
                        Size = new SixLabors.ImageSharp.Size(1200, 1200),
                        Mode = ResizeMode.Max
                    }));
                }
                var encoder = new JpegEncoder { Quality = 82 };
                image.Save(filePath, encoder);

                using var thumb = image.Clone(x => x.Resize(new ResizeOptions
                {
                    Size = new SixLabors.ImageSharp.Size(180, 180),
                    Mode = ResizeMode.Crop
                }));
                thumb.Save(thumbPath, new JpegEncoder { Quality = 75 });
                
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
