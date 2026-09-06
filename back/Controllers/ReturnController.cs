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

            if (string.IsNullOrEmpty(returnRequest.ReturnCode))
            {
                string candidateCode;
                do
                {
                    candidateCode = LeatherLane_Atelier.Services.IdGenerator.GenerateReturnId();
                } while (await _context.ReturnRequests.AnyAsync(r => r.ReturnCode == candidateCode));
                returnRequest.ReturnCode = candidateCode;
            }

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

            string orderDisplay = LeatherLane_Atelier.Services.IdGenerator.ResolveOrderDisplay(order.OrderId, order.Id);

            // Notification for Admin
            _context.Notifications.Add(new Notification
            {
                Title = "New Return Request",
                Message = $"Customer requested a return for Order {orderDisplay} (Return ID: {returnRequest.ReturnCode}).",
                ActionUrl = $"admin-return.html",
                UserId = null // Admin
            });
            await LeatherLane_Atelier.Services.EmailServiceExtensions.NotifyAdminsAsync(_emailService, _context, "New Return Request", $"A new return request ({returnRequest.ReturnCode}) was submitted for Order {orderDisplay}.");

            // Notification for Customer
            _context.Notifications.Add(new Notification
            {
                Title = "Return Request Submitted",
                Message = $"Your return request ({returnRequest.ReturnCode}) for Order {orderDisplay} has been successfully submitted.",
                ActionUrl = "orders.html",
                UserId = userId
            });
            var userObj = await _context.Users.FindAsync(userId);
            if (userObj != null)
            {
                var details = new System.Collections.Generic.Dictionary<string, string> {
                    { "Return ID", returnRequest.ReturnCode },
                    { "Order ID", orderDisplay },
                    { "Refund Amount", $"Rs. {returnRequest.RefundAmount:N2}" }
                };
                var emailHtml = LeatherLane_Atelier.Services.EmailTemplateBuilder.BuildStandardEmail("🔄 Return Request Received", userObj.Name, $"We have received your return request ({returnRequest.ReturnCode}) for Order {orderDisplay}. Our team will review it shortly.", "/transactions.html", "View Orders", details);
                _ = _emailService.SendEmailAsync(userObj.Email, $"Return Request Submitted ({returnRequest.ReturnCode})", emailHtml);
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Return request submitted successfully.", returnId = returnRequest.ReturnId, returnCode = returnRequest.ReturnCode });
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
