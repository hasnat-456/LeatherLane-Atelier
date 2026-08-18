using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using LeatherLane_Atelier.Models;

namespace LeatherLane_Atelier.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ExchangeController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly LeatherLane_Atelier.Services.IEmailService _emailService;
        private readonly IMemoryCache _cache;

        public ExchangeController(ApplicationDbContext context, LeatherLane_Atelier.Services.IEmailService emailService, IMemoryCache cache)
        {
            _context = context;
            _emailService = emailService;
            _cache = cache;
        }

        private int GetUserId()
        {
            var claim = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
            return claim != null ? int.Parse(claim) : 0;
        }

        [HttpGet("my-requests")]
        public async Task<IActionResult> GetMyRequests()
        {
            var userId = GetUserId();
            var requests = await _context.ExchangeRequests
                .Include(e => e.OriginalProduct)
                .Include(e => e.ReplacementProduct)
                .Where(e => e.CustomerId == userId)
                .OrderByDescending(e => e.CreatedAt)
                .Select(e => new
                {
                    e.ExchangeId,
                    e.OrderId,
                    e.Status,
                    e.RequestDate,
                    OriginalProductName = e.OriginalProduct.Name,
                    OriginalProductImage = e.OriginalProduct.Thumbnail,
                    ReplacementProductName = e.ReplacementProduct != null ? e.ReplacementProduct.Name : null,
                    e.ExchangeDeadline
                })
                .ToListAsync();

            return Ok(requests);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetRequestDetails(int id)
        {
            var userId = GetUserId();
            var request = await _context.ExchangeRequests
                .Include(e => e.OriginalProduct)
                .Include(e => e.ReplacementProduct)
                .Include(e => e.Images)
                .Include(e => e.StatusHistory)
                .FirstOrDefaultAsync(e => e.ExchangeId == id && e.CustomerId == userId);

            if (request == null)
                return NotFound(new { message = "Exchange request not found." });

            return Ok(request);
        }

        
        

        

        [HttpPost("request")]
        public async Task<IActionResult> SubmitRequest([FromForm] ExchangeRequestDto dto)
        {
            var userId = GetUserId();

            // Process and save images right now
            var savedImageUrls = new List<string>();
            if (dto.Images != null && dto.Images.Count > 0)
            {
                if (dto.Images.Count > 5)
                    return BadRequest(new { message = "Maximum 5 images allowed." });

                var currentDir = Directory.GetCurrentDirectory();
                var frontPath = currentDir.EndsWith("back", StringComparison.OrdinalIgnoreCase) 
                    ? Path.Combine(currentDir, "..", "front", "images", "exchanges") 
                    : Path.Combine(currentDir, "front", "images", "exchanges");
                
                if (!Directory.Exists(frontPath))
                    Directory.CreateDirectory(frontPath);

                foreach (var file in dto.Images)
                {
                    if (file.Length > 5 * 1024 * 1024)
                        return BadRequest(new { message = $"File {file.FileName} exceeds 5MB." });

                    var ext = Path.GetExtension(file.FileName).ToLower();
                    if (ext != ".jpg" && ext != ".jpeg" && ext != ".png")
                        return BadRequest(new { message = "Only JPG, JPEG, and PNG are allowed." });

                    var fileName = $"{Guid.NewGuid()}{ext}";
                    var filePath = Path.Combine(frontPath, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    savedImageUrls.Add($"/images/exchanges/{fileName}");
                }
            }

            var cachedDto = new CachedExchangeRequestDto
            {
                OrderId = dto.OrderId,
                OrderItemId = dto.OrderItemId,
                ReplacementProductId = dto.ReplacementProductId,
                Reason = dto.Reason,
                OtherReason = dto.OtherReason,
                CustomerConfirmed = dto.CustomerConfirmed,
                SavedImageUrls = savedImageUrls
            };
            return await ProcessExchangeRequest(cachedDto, userId);
        }

        private async Task<IActionResult> ProcessExchangeRequest(CachedExchangeRequestDto dto, int userId)
        {
            var order = await _context.Transactions
                .Include(t => t.Items)
                .FirstOrDefaultAsync(t => t.Id == dto.OrderId && t.UserId == userId);

            if (order == null || (order.Status.ToLower() != "delivered" && order.Status.ToLower() != "completed"))
                return BadRequest(new { message = "Order not found or not delivered yet." });

            var orderItem = order.Items.FirstOrDefault(i => i.Id == dto.OrderItemId);
            if (orderItem == null)
                return BadRequest(new { message = "Item not found in this order." });

            if (orderItem.HasBeenExchanged)
                return BadRequest(new { message = "This product has already been exchanged and is no longer eligible." });

            var deliveryDate = order.UpdatedAt; 
            var deadline = deliveryDate.AddDays(7);
            
            if (DateTime.UtcNow > deadline)
                return BadRequest(new { message = "Exchange period has expired. Requests must be made within 7 days of delivery." });

            if (!dto.CustomerConfirmed)
                return BadRequest(new { message = "You must confirm that the product is unused and includes all original tags and packaging." });

            var originalProduct = await _context.Products.FindAsync(orderItem.ProductId);
            Product? replacementProduct = null;
            if (dto.ReplacementProductId.HasValue)
            {
                replacementProduct = await _context.Products.FindAsync(dto.ReplacementProductId.Value);
            }

            decimal priceDifference = 0;
            if (originalProduct != null && replacementProduct != null)
            {

                priceDifference = replacementProduct.Price - originalProduct.Price;
            }

            var exchangeRequest = new ExchangeRequest
            {
                OrderId = order.Id,
                CustomerId = userId,
                OrderItemId = orderItem.Id,
                OriginalProductId = originalProduct?.Id ?? 0,
                ReplacementProductId = replacementProduct?.Id,
                Reason = dto.Reason,
                OtherReason = dto.OtherReason,
                ExchangeDeadline = deadline,
                PriceDifference = priceDifference,
                CustomerConfirmed = dto.CustomerConfirmed,
                Status = "Pending Review"
            };

            _context.ExchangeRequests.Add(exchangeRequest);
            await _context.SaveChangesAsync(); // Save to get the ID

            // Add Timeline Event
            _context.TimelineEvents.Add(new TimelineEvent
            {
                ReferenceId = exchangeRequest.ExchangeId,
                Type = "Exchange",
                Status = "Exchange Requested",
                Description = "Customer submitted exchange request.",
                EventDateTime = DateTime.UtcNow,
                CreatedBy = "Customer",
                IsCurrent = true,
                IsCompleted = false
            });

            // Handle Images
            if (dto.SavedImageUrls != null)
            {
                foreach (var url in dto.SavedImageUrls)
                {
                    _context.ExchangeImages.Add(new ExchangeImage
                    {
                        ExchangeId = exchangeRequest.ExchangeId,
                        ImageUrl = url,
                        ImageType = "General"
                    });
                }
            }

            // Update Order Item
            orderItem.HasBeenExchanged = true;
            orderItem.ExchangeRequestId = exchangeRequest.ExchangeId;
            orderItem.ExchangeDate = DateTime.UtcNow;

            // Add History
            _context.ExchangeStatusHistory.Add(new ExchangeStatusHistory
            {
                ExchangeId = exchangeRequest.ExchangeId,
                Status = "Pending Review",
                ChangedBy = "Customer",
                Remarks = "Exchange request submitted."
            });

            // Notification for Admin
            _context.Notifications.Add(new Notification
            {
                Title = "New Exchange Request",
                Message = $"Customer requested an exchange for Order #{order.Id}.",
                ActionUrl = $"admin-exchange.html?id={exchangeRequest.ExchangeId}",
                UserId = null // Admin
            });
            _ = _emailService.SendEmailAsync("muhammadbilalarifsheikh@gmail.com", "New Exchange Request", $"A new exchange request was submitted for Order #{order.Id}.");

            // Notification for Customer
            _context.Notifications.Add(new Notification
            {
                Title = "Exchange Request Submitted",
                Message = $"Your exchange request for Order #{order.Id} has been successfully submitted.",
                ActionUrl = "orders.html",
                UserId = userId
            });
            var userObj = await _context.Users.FindAsync(userId);
            if (userObj != null)
            {
                _ = _emailService.SendEmailAsync(userObj.Email, "Exchange Request Submitted", $"We have received your exchange request for Order #{order.Id}. Our team will review it shortly.");
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Exchange request submitted successfully.", exchangeId = exchangeRequest.ExchangeId });
        }

        [HttpPost("{id}/submit-tracking")]
        public async Task<IActionResult> SubmitTracking(int id, [FromBody] SubmitTrackingDto dto)
        {
            var userId = GetUserId();
            var request = await _context.ExchangeRequests.FindAsync(id);
            if (request == null || request.CustomerId != userId) return NotFound();

            if (request.Status != "Waiting for Customer Return") 
                return BadRequest(new { message = "Invalid state. Exchange must be waiting for return." });

            request.Status = "Return Shipped by Customer";
            request.CourierName = dto.CourierName;
            request.TrackingNumber = dto.TrackingNumber;
            
            // Add Timeline Event
            var prevs = await _context.TimelineEvents.Where(t => t.ReferenceId == id && t.Type == "Exchange" && t.IsCurrent).ToListAsync();
            foreach (var p in prevs) { p.IsCurrent = false; p.IsCompleted = true; }

            _context.TimelineEvents.Add(new TimelineEvent
            {
                ReferenceId = id,
                Type = "Exchange",
                Status = "Return Shipped by Customer",
                Description = $"Customer shipped original item back via {dto.CourierName}. Tracking: {dto.TrackingNumber}",
                EventDateTime = DateTime.UtcNow,
                CourierName = dto.CourierName,
                TrackingNumber = dto.TrackingNumber,
                CreatedBy = "Customer",
                IsCurrent = true,
                IsCompleted = false
            });

            // Add History
            _context.ExchangeStatusHistory.Add(new ExchangeStatusHistory
            {
                ExchangeId = id,
                Status = "Return Shipped by Customer",
                ChangedBy = "Customer",
                Remarks = $"Tracking submitted: {dto.CourierName} - {dto.TrackingNumber}"
            });

            // Notification for Admin
            _context.Notifications.Add(new Notification
            {
                Title = "Exchange Tracking Submitted",
                Message = $"Customer submitted tracking for Exchange #{id}.",
                ActionUrl = $"admin-exchange.html?id={id}",
                UserId = null
            });
            _ = _emailService.SendEmailAsync("muhammadbilalarifsheikh@gmail.com", "Exchange Tracking Submitted", $"Tracking for Exchange #{id} is: {dto.CourierName} {dto.TrackingNumber}");

            var userObj = await _context.Users.FindAsync(userId);
            if (userObj != null)
            {
                _context.Notifications.Add(new Notification
                {
                    Title = "Tracking Submitted",
                    Message = $"Your tracking details for Exchange #{id} have been successfully submitted.",
                    ActionUrl = $"exchange-tracking.html?id={id}",
                    UserId = userId
                });
                _ = _emailService.SendEmailAsync(userObj.Email, "Tracking Received", $"We have received your return tracking details: {dto.CourierName} {dto.TrackingNumber}. We will notify you once it passes inspection.");
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Tracking information submitted successfully." });
        }
    }

    
    public class CachedExchangeRequestDto
    {
        public int OrderId { get; set; }
        public int OrderItemId { get; set; }
        public int? ReplacementProductId { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string? OtherReason { get; set; }
        public bool CustomerConfirmed { get; set; }
        public List<string> SavedImageUrls { get; set; } = new List<string>();
    }

    public class ExchangeRequestDto
    {
        public int OrderId { get; set; }
        public int OrderItemId { get; set; }
        public int? ReplacementProductId { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string? OtherReason { get; set; }
        public bool CustomerConfirmed { get; set; }
        public List<IFormFile>? Images { get; set; }
    }

    public class SubmitTrackingDto
    {
        public string CourierName { get; set; } = string.Empty;
        public string TrackingNumber { get; set; } = string.Empty;
    }
}
