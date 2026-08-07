using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using LeatherLane_Atelier.Models;
using System.Security.Claims;

namespace LeatherLane_Atelier.Controllers
{
    [Route("api/admin/orders")]
    [ApiController]
    [Authorize]
    public class AdminOrderController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly LeatherLane_Atelier.Services.IEmailService _emailService;

        public AdminOrderController(ApplicationDbContext context, LeatherLane_Atelier.Services.IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        private bool IsAdmin()
        {
            return User.Claims.Any(c => (c.Type == ClaimTypes.Role || c.Type == "role") && c.Value == "Admin");
        }

        [HttpPost("{id}/timeline")]
        public async Task<IActionResult> AddTimelineEvent(int id, [FromBody] TimelineEventDto dto)
        {
            if (!IsAdmin()) return Forbid();

            var order = await _context.Transactions.FindAsync(id);
            if (order == null) return NotFound(new { message = "Order not found" });

            // Mark previous current events as not current
            var previousCurrent = await _context.TimelineEvents
                .Where(t => t.ReferenceId == id && t.Type == "Order" && t.IsCurrent)
                .ToListAsync();

            foreach (var prev in previousCurrent)
            {
                prev.IsCurrent = false;
                prev.IsCompleted = true; // Mark as completed when moving to next step
            }

            var newEvent = new TimelineEvent
            {
                ReferenceId = id,
                Type = "Order",
                Status = dto.Status,
                Description = dto.Description,
                EventDateTime = DateTime.UtcNow,
                CourierName = dto.CourierName,
                TrackingNumber = dto.TrackingNumber,
                Notes = dto.Notes,
                CreatedBy = "Admin",
                IsCurrent = true,
                IsCompleted = dto.Status == "Delivered" || dto.Status == "Cancelled"
            };

            // Also update the main order status
            order.Status = dto.Status;
            order.UpdatedAt = DateTime.UtcNow;

            _context.TimelineEvents.Add(newEvent);

            if (dto.Status == "Packed" || dto.Status == "Shipped" || dto.Status == "Delivered")
            {
                var customer = await _context.Users.FindAsync(order.UserId);
                if (customer != null)
                {
                    string message = "";
                    if (dto.Status == "Packed") message = $"Your order #{id} has been packed and is ready to ship.";
                    else if (dto.Status == "Shipped") message = $"Your order #{id} has been shipped via {dto.CourierName ?? "Courier"}. Tracking: {dto.TrackingNumber ?? "N/A"}";
                    else if (dto.Status == "Delivered") message = $"Your order #{id} has been delivered! Enjoy your purchase.";

                    _context.Notifications.Add(new Notification
                    {
                        Title = $"Order {dto.Status}",
                        Message = message,
                        ActionUrl = $"transactions.html",
                        UserId = customer.Id
                    });

                    _ = _emailService.SendEmailAsync(customer.Email, $"Order {dto.Status}", message);

                    if (dto.Status == "Delivered")
                    {
                        string reviewMsg = $"How did we do? Please log in and leave a review for your items from Order #{id}. Your feedback helps us improve!";
                        _context.Notifications.Add(new Notification
                        {
                            Title = "Please Leave a Review",
                            Message = reviewMsg,
                            ActionUrl = $"transactions.html",
                            UserId = customer.Id
                        });
                        _ = _emailService.SendEmailAsync(customer.Email, "We'd Love Your Review!", reviewMsg);
                    }
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Timeline event added successfully", @event = newEvent });
        }
    }

    public class TimelineEventDto
    {
        public string Status { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? CourierName { get; set; }
        public string? TrackingNumber { get; set; }
        public string? Notes { get; set; }
    }
}
