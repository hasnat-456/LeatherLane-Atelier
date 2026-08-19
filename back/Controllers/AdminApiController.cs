using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LeatherLane_Atelier.Models;

namespace LeatherLane_Atelier.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public AdminApiController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // We bypass actual [Authorize(Roles="Admin")] for now to match the frontend mock auth flow
        // The frontend admin uses its own localStorage token for simplicity.

        [HttpPut("settings")]
        public async Task<IActionResult> UpdateSettings([FromBody] UpdateAdminSettingsRequest req)
        {
            var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.Role == "Admin");
            if (adminUser == null) return NotFound(new { message = "Admin not found" });

            if (!string.IsNullOrEmpty(req.Email))
            {
                adminUser.Email = req.Email;
            }

            if (!string.IsNullOrEmpty(req.NewPassword) && req.NewPassword.Length >= 6)
            {
                adminUser.Password = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Settings updated successfully" });
        }

        [HttpPost("site-image")]
        public async Task<IActionResult> UploadSiteImage(IFormFile imageFile, [FromForm] string target)
        {
            if (imageFile == null || imageFile.Length == 0) return BadRequest("No image provided");

            var currentDir = Directory.GetCurrentDirectory();
            var frontPath = currentDir.EndsWith("back", StringComparison.OrdinalIgnoreCase) 
                ? Path.Combine(currentDir, "..", "front", "images", "site") 
                : Path.Combine(currentDir, "front", "images", "site");

            Directory.CreateDirectory(frontPath);
            
            // Only allow specific targets for security
            if (target != "about-image.jpg") return BadRequest("Invalid target");

            var filePath = Path.Combine(frontPath, target);
            
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(stream);
            }

            return Ok(new { message = "Image uploaded successfully", url = $"/images/site/{target}?t={DateTime.UtcNow.Ticks}" });
        }

        [HttpPost("slider-image")]
        public async Task<IActionResult> UploadSliderImage(IFormFile imageFile)
        {
            if (imageFile == null || imageFile.Length == 0) return BadRequest("No image provided");

            var currentDir = Directory.GetCurrentDirectory();
            var frontPath = currentDir.EndsWith("back", StringComparison.OrdinalIgnoreCase) 
                ? Path.Combine(currentDir, "..", "front", "upload", "sliders") 
                : Path.Combine(currentDir, "front", "upload", "sliders");

            Directory.CreateDirectory(frontPath);
            
            var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
            var filePath = Path.Combine(frontPath, uniqueFileName);
            
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(stream);
            }

            return Ok(new { message = "Image uploaded successfully", url = "upload/sliders/" + uniqueFileName });
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var totalOrders = await _context.Transactions.CountAsync();
            var totalRevenue = await _context.Transactions.Where(t => t.Status != "Cancelled").SumAsync(t => t.TotalAmount);

            return Ok(new { totalOrders, totalRevenue });
        }

        [HttpGet("orders")]
        public async Task<IActionResult> GetOrders()
        {
            var orders = await _context.Transactions
                .Include(t => t.Items)
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new {
                    id = t.Id,
                    date = t.CreatedAt,
                    customer = t.ShippingName ?? $"User {t.UserId}",
                    amount = t.TotalAmount,
                    status = t.Status
                })
                .ToListAsync();

            return Ok(orders);
        }

        [HttpPut("orders/{id}/status")]
        public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] UpdateStatusRequest req)
        {
            var transaction = await _context.Transactions.FindAsync(id);
            if (transaction == null) return NotFound();

            if (transaction.Status == "Cancelled")
            {
                return BadRequest(new { message = "Cannot change the status of a cancelled order." });
            }

            if (transaction.Status == "Payment Verification Pending")
            {
                return BadRequest(new { message = "Cannot manually change the status. Please verify or reject the payment in the Payment Verification tab first." });
            }

            transaction.Status = req.Status;

            // Update timeline
            var previousCurrent = await _context.TimelineEvents
                .Where(t => t.ReferenceId == id && t.Type == "Order" && t.IsCurrent)
                .ToListAsync();

            foreach (var prev in previousCurrent)
            {
                prev.IsCurrent = false;
                prev.IsCompleted = true;
            }

            string desc = req.Status switch {
                "Order Confirmed" => "Your order has been verified and confirmed.",
                "Preparing Order" => "We are carefully crafting and preparing your items.",
                "Packed" => "Your order is packed and ready for dispatch.",
                "Handed to Courier" => "Your package has been handed over to our delivery partner.",
                "In Transit" => "Your package is on its way to your city.",
                "Out for Delivery" => "Our rider is out for delivery today.",
                "Delivered" => "The package was successfully delivered.",
                _ => "Status updated to " + req.Status
            };

            var newEvent = new TimelineEvent
            {
                ReferenceId = id,
                Type = "Order",
                Status = req.Status,
                Description = desc,
                EventDateTime = DateTime.UtcNow,
                CreatedBy = "Admin",
                IsCurrent = true,
                IsCompleted = req.Status == "Delivered" || req.Status == "Cancelled"
            };

            _context.TimelineEvents.Add(newEvent);

            await _context.SaveChangesAsync();

            return Ok(new { message = "Status updated successfully" });
        }

        [HttpDelete("products/{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Product deleted successfully" });
        }

        [HttpGet("pending-payments")]
        public async Task<IActionResult> GetPendingPayments()
        {
            var payments = await _context.Transactions
                .Where(t => t.PaymentScreenshot != null && t.PaymentScreenshot != "")
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new {
                    id = t.Id,
                    date = t.CreatedAt,
                    customer = t.ShippingName ?? $"User {t.UserId}",
                    amount = t.TotalAmount,
                    status = t.Status,
                    paymentMethod = t.PaymentMethod,
                    paymentRefId = t.PaymentRefId,
                    senderName = t.SenderName,
                    senderMobile = t.SenderMobile,
                    paymentScreenshot = t.PaymentScreenshot,
                    rejectionReason = t.RejectionReason
                })
                .ToListAsync();

            return Ok(payments);
        }

        [HttpPut("orders/{id}/verify-payment")]
        public async Task<IActionResult> VerifyPayment(int id)
        {
            var transaction = await _context.Transactions.FindAsync(id);
            if (transaction == null) return NotFound(new { message = "Order not found" });

            if (transaction.Status != "Payment Verification Pending")
            {
                return BadRequest(new { message = "Only pending payments can be verified." });
            }

            transaction.Status = "Order Confirmed";
            transaction.UpdatedAt = DateTime.UtcNow;

            var previousCurrent = await _context.TimelineEvents
                .Where(t => t.ReferenceId == id && t.Type == "Order" && t.IsCurrent)
                .ToListAsync();

            foreach (var prev in previousCurrent)
            {
                prev.IsCurrent = false;
                prev.IsCompleted = true;
            }

            var newEvent = new TimelineEvent
            {
                ReferenceId = id,
                Type = "Order",
                Status = "Order Confirmed",
                Description = "Your payment has been verified. Your order is now confirmed and will begin processing.",
                EventDateTime = DateTime.UtcNow,
                CreatedBy = "Admin",
                IsCurrent = true,
                IsCompleted = false
            };
            _context.TimelineEvents.Add(newEvent);

            var customer = await _context.Users.FindAsync(transaction.UserId);
            if (customer != null)
            {
                _context.Notifications.Add(new Notification
                {
                    Title = "Payment Verified",
                    Message = "Your payment has been verified. Your order is now confirmed and will begin processing.",
                    ActionUrl = $"transactions.html",
                    UserId = customer.Id
                });

                var emailSvc = HttpContext.RequestServices.GetService(typeof(LeatherLane_Atelier.Services.IEmailService)) as LeatherLane_Atelier.Services.IEmailService;
                if (emailSvc != null)
                {
                    _ = emailSvc.SendEmailAsync(customer.Email, "Payment Verified - Order Confirmed", 
                        $"Your payment for Order #{transaction.Id} has been verified. Your order is now confirmed and will begin processing.");
                }
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Payment verified successfully" });
        }

        [HttpPut("orders/{id}/reject-payment")]
        public async Task<IActionResult> RejectPayment(int id, [FromBody] RejectPaymentRequest req)
        {
            var transaction = await _context.Transactions.FindAsync(id);
            if (transaction == null) return NotFound(new { message = "Order not found" });

            if (transaction.Status != "Payment Verification Pending")
            {
                return BadRequest(new { message = "Only pending payments can be rejected." });
            }

            transaction.Status = "Payment Rejected";
            transaction.RejectionReason = string.IsNullOrEmpty(req.RejectionReason) ? "Invalid transaction details" : req.RejectionReason;
            transaction.UpdatedAt = DateTime.UtcNow;

            var customer = await _context.Users.FindAsync(transaction.UserId);
            if (customer != null)
            {
                _context.Notifications.Add(new Notification
                {
                    Title = "Payment Rejection Alert",
                    Message = $"Your payment could not be verified. Reason: {transaction.RejectionReason}. Please upload a new payment proof.",
                    ActionUrl = $"order-tracking.html?id={transaction.Id}",
                    UserId = customer.Id
                });

                var emailSvc = HttpContext.RequestServices.GetService(typeof(LeatherLane_Atelier.Services.IEmailService)) as LeatherLane_Atelier.Services.IEmailService;
                if (emailSvc != null)
                {
                    _ = emailSvc.SendEmailAsync(customer.Email, "Payment Rejection Alert", 
                        $"Your payment for Order #{transaction.Id} could not be verified. Reason: {transaction.RejectionReason}. Please review the reason and upload a new payment proof.");
                }
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Payment rejected successfully" });
        }

        [HttpGet("manual-payment-settings")]
        public async Task<IActionResult> GetManualPaymentSettings()
        {
            var settings = await _context.ManualPaymentSettings.ToListAsync();
            return Ok(settings);
        }

        [HttpPut("manual-payment-settings")]
        public async Task<IActionResult> UpdateManualPaymentSettings([FromBody] List<ManualPaymentSetting> settings)
        {
            foreach (var s in settings)
            {
                var existing = await _context.ManualPaymentSettings.FindAsync(s.Id);
                if (existing != null)
                {
                    existing.IsEnabled = s.IsEnabled;
                    existing.BankName = s.BankName;
                    existing.AccountTitle = s.AccountTitle;
                    existing.AccountNumber = s.AccountNumber;
                    existing.IBAN = s.IBAN;
                    existing.MobileNumber = s.MobileNumber;
                    existing.RaastId = s.RaastId;
                }
            }
            await _context.SaveChangesAsync();
            return Ok(new { message = "Manual payment settings updated successfully." });
        }

        // Category Management Endpoints
        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories()
        {
            var categories = await _context.ProductCategories
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name)
                .ToListAsync();
            return Ok(categories);
        }

        [HttpPost("categories")]
        public async Task<IActionResult> CreateCategory([FromBody] ProductCategory category)
        {
            if (string.IsNullOrEmpty(category.Name))
            {
                return BadRequest(new { message = "Category name is required." });
            }

            _context.ProductCategories.Add(category);
            await _context.SaveChangesAsync();
            return Ok(category);
        }

        [HttpPut("categories/{id}")]
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] ProductCategory dto)
        {
            var category = await _context.ProductCategories.FindAsync(id);
            if (category == null) return NotFound();

            if (string.IsNullOrEmpty(dto.Name))
            {
                return BadRequest(new { message = "Category name is required." });
            }

            var oldName = category.Name;
            category.Name = dto.Name;
            category.IsActive = dto.IsActive;
            category.DisplayOrder = dto.DisplayOrder;

            // If name changes, cascade name updates to all associated products' string Category
            if (oldName != dto.Name)
            {
                var products = await _context.Products.Where(p => p.CategoryId == id).ToListAsync();
                foreach (var p in products)
                {
                    p.Category = dto.Name;
                }
            }

            await _context.SaveChangesAsync();
            return Ok(category);
        }

        [HttpDelete("categories/{id}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _context.ProductCategories.FindAsync(id);
            if (category == null) return NotFound();

            // Prevent deletion if associated products exist
            var hasProducts = await _context.Products.AnyAsync(p => p.CategoryId == id);
            if (hasProducts)
            {
                return BadRequest(new { message = "Cannot delete category because it contains active products. Reassign them first." });
            }

            _context.ProductCategories.Remove(category);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Category deleted successfully." });
        }
    }

    public class UpdateStatusRequest
    {
        public string Status { get; set; } = string.Empty;
    }

    public class UpdateAdminSettingsRequest
    {
        public string? Email { get; set; }
        public string? NewPassword { get; set; }
    }

    public class RejectPaymentRequest
    {
        public string RejectionReason { get; set; } = string.Empty;
    }
}
