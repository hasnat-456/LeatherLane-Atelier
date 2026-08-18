using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using LeatherLane_Atelier.Models;
using System.Collections.Generic;

namespace LeatherLane_Atelier.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ReturnController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly LeatherLane_Atelier.Services.IEmailService _emailService;

        public ReturnController(ApplicationDbContext context, LeatherLane_Atelier.Services.IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        private int GetUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return claim != null ? int.Parse(claim) : 0;
        }

        [HttpPost("request")]
        public async Task<IActionResult> RequestReturn([FromBody] ReturnRequestDto dto)
        {
            var userId = GetUserId();

            var order = await _context.Transactions
                .Include(t => t.Items)
                .FirstOrDefaultAsync(t => t.Id == dto.OrderId && t.UserId == userId);

            if (order == null || (order.Status.ToLower() != "delivered" && order.Status.ToLower() != "completed"))
                return BadRequest(new { message = "Order not found or not delivered yet." });

            var orderItem = order.Items.FirstOrDefault(i => i.Id == dto.OrderItemId);
            if (orderItem == null) return BadRequest(new { message = "Item not found in this order." });

            if (orderItem.HasBeenExchanged || orderItem.HasBeenReturned)
                return BadRequest(new { message = "This item has already been exchanged or returned." });

            var returnRequest = new ReturnRequest
            {
                OrderId = dto.OrderId,
                CustomerId = userId,
                OrderItemId = dto.OrderItemId,
                ProductId = orderItem.ProductId.Value,
                Reason = dto.Reason,
                OtherReason = dto.OtherReason,
                Status = "Return Requested",
                RequestDate = DateTime.UtcNow,
                RefundAmount = orderItem.Price * orderItem.Quantity
            };

            _context.ReturnRequests.Add(returnRequest);
            await _context.SaveChangesAsync();

            // Add Timeline Event
            _context.TimelineEvents.Add(new TimelineEvent
            {
                ReferenceId = returnRequest.ReturnId,
                Type = "Return",
                Status = "Return Requested",
                Description = "Customer submitted return request.",
                EventDateTime = DateTime.UtcNow,
                CreatedBy = "Customer",
                IsCurrent = true,
                IsCompleted = false
            });

            // Handle Images
            if (dto.Images != null && dto.Images.Count > 0)
            {
                if (dto.Images.Count > 5)
                    return BadRequest(new { message = "Maximum 5 images allowed." });

                foreach (var imgBase64 in dto.Images)
                {
                    _context.ReturnImages.Add(new ReturnImage
                    {
                        ReturnId = returnRequest.ReturnId,
                        ImageUrl = imgBase64
                    });
                }
            }

            orderItem.HasBeenReturned = true;
            orderItem.ReturnRequestId = returnRequest.ReturnId;

            // Notification for Admin
            _context.Notifications.Add(new Notification
            {
                Title = "New Return Request",
                Message = $"Customer requested a return for Order #{dto.OrderId}.",
                ActionUrl = $"admin-return.html",
                UserId = null // Admin
            });
            _ = _emailService.SendEmailAsync("muhammadbilalarifsheikh@gmail.com", "New Return Request", $"A new return request was submitted for Order #{dto.OrderId}.");

            // Notification for Customer
            _context.Notifications.Add(new Notification
            {
                Title = "Return Request Submitted",
                Message = $"Your return request for Order #{dto.OrderId} has been successfully submitted.",
                ActionUrl = "orders.html",
                UserId = userId
            });
            var userObj = await _context.Users.FindAsync(userId);
            if (userObj != null)
            {
                _ = _emailService.SendEmailAsync(userObj.Email, "Return Request Submitted", $"We have received your return request for Order #{dto.OrderId}. Our team will review it shortly.");
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Return request submitted successfully.", returnId = returnRequest.ReturnId });
        }
    }

    public class ReturnRequestDto
    {
        public int OrderId { get; set; }
        public int OrderItemId { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string? OtherReason { get; set; }
        public List<string>? Images { get; set; }
    }
}
