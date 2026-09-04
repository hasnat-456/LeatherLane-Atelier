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

            var order = await _context.Transactions
                .Include(t => t.Items)
                .FirstOrDefaultAsync(t => t.Id == id);
            if (order == null) return NotFound(new { message = "Order not found" });

            if (order.Status == "Cancelled")
            {
                return BadRequest(new { message = "Cannot change the status of a cancelled order." });
            }

            if (order.Status == "Delivered")
            {
                return BadRequest(new { message = "Cannot change the status of an already delivered order." });
            }

            if (dto.Status == "Cancelled")
            {
                return BadRequest(new { message = "Admins are not permitted to manually cancel orders." });
            }

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

            if (dto.Status == "Packed" || dto.Status == "Shipped" || dto.Status == "Handed to Courier" || dto.Status == "In Transit" || dto.Status == "Out for Delivery" || dto.Status == "Delivered")
            {
                var customer = await _context.Users.FindAsync(order.UserId);
                if (customer != null)
                {
                    string message = "";
                    if (dto.Status == "Packed") message = $"Your order {order.OrderId} has been packed with bespoke care and is ready to ship.";
                    else if (dto.Status == "Shipped" || dto.Status == "Handed to Courier" || dto.Status == "In Transit") message = $"Your handcrafted order {order.OrderId} is on its way via {dto.CourierName ?? "Courier"}. Tracking Number: {dto.TrackingNumber ?? "In Transit"}";
                    else if (dto.Status == "Out for Delivery") message = $"Your order {order.OrderId} is out for delivery today with our courier rider.";
                    else if (dto.Status == "Delivered")
                    {
                        message = $"Your order {order.OrderId} has been delivered successfully. We hope your handcrafted leather items serve you with timeless elegance.";
                        order.DeliveredAt = DateTime.UtcNow;
                        order.ReviewReminderDay2Sent = false;
                        order.ReviewReminderDay4Sent = false;
                    }

                    _context.Notifications.Add(new Notification
                    {
                        Title = $"Order {dto.Status}",
                        Message = message,
                        ActionUrl = $"order-tracking.html?id={order.OrderId}",
                        UserId = customer.Id
                    });

                    var orderItemInfos = new List<LeatherLane_Atelier.Services.OrderItemInfo>();
                    foreach (var tItem in order.Items)
                    {
                        var prod = tItem.ProductId.HasValue ? await _context.Products.FindAsync(tItem.ProductId.Value) : null;
                        orderItemInfos.Add(new LeatherLane_Atelier.Services.OrderItemInfo
                        {
                            Name = tItem.Name,
                            ProductId = prod?.ProductId ?? ("LLA-PRD-" + tItem.ProductId),
                            Thumbnail = prod?.Image,
                            Quantity = tItem.Quantity,
                            Price = tItem.Price
                        });
                    }

                    string emailOpeningText = dto.Status switch
                    {
                        "Packed" => $"Your order <strong>{order.OrderId}</strong> has been packed with bespoke care and is ready for dispatch.",
                        "Shipped" or "Handed to Courier" or "In Transit" => $"Your bespoke order <strong>{order.OrderId}</strong> has been securely packed and handed over to <strong>{dto.CourierName ?? "our courier partner"}</strong>. Your tracking details are provided below.",
                        "Out for Delivery" => $"Great news! Your LeatherLane Atelier parcel for Order <strong>{order.OrderId}</strong> is out for doorstep delivery today with our courier rider.",
                        "Delivered" => $"Your order <strong>{order.OrderId}</strong> has been delivered. We hope your bespoke leather pieces serve you with timeless elegance and comfort.",
                        _ => $"Your order <strong>{order.OrderId}</strong> status is now: <strong>{dto.Status}</strong>."
                    };

                    string actionButtonText = dto.Status == "Delivered" ? "Leave a Product Review" : "Track Your Order";
                    string actionBtnUrl = dto.Status == "Delivered" ? $"/orders.html" : $"/order-tracking.html?id={order.OrderId}";

                    var htmlEmail = LeatherLane_Atelier.Services.EmailTemplateBuilder.BuildOrderEmail(
                        $"Order {dto.Status}",
                        customer.Name,
                        order.OrderId,
                        emailOpeningText,
                        orderItemInfos,
                        0m,
                        order.TotalAmount,
                        dto.Status,
                        order.PaymentMethod,
                        actionBtnUrl,
                        actionButtonText,
                        dto.CourierName,
                        dto.TrackingNumber
                    );
                    _ = _emailService.SendEmailAsync(customer.Email, $"Order {order.OrderId} - {dto.Status} | LeatherLane Atelier", htmlEmail);
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
