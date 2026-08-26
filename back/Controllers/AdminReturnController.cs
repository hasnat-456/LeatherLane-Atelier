using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using LeatherLane_Atelier.Models;

namespace LeatherLane_Atelier.Controllers
{
    [Route("api/admin/returns")]
    [ApiController]
    [Authorize]
    public class AdminReturnController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly LeatherLane_Atelier.Services.IEmailService _emailService;

        public AdminReturnController(ApplicationDbContext context, LeatherLane_Atelier.Services.IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        private bool IsAdmin()
        {
            return User.Claims.Any(c => (c.Type == ClaimTypes.Role || c.Type == "role") && c.Value == "Admin");
        }

        private async Task AddTimelineEvent(int returnId, string status, string description, string? courier = null, string? tracking = null, string? notes = null, bool isCompleted = false)
        {
            var prevs = await _context.TimelineEvents.Where(t => t.ReferenceId == returnId && t.Type == "Return" && t.IsCurrent).ToListAsync();
            foreach (var p in prevs) { p.IsCurrent = false; p.IsCompleted = true; }

            _context.TimelineEvents.Add(new TimelineEvent
            {
                ReferenceId = returnId,
                Type = "Return",
                Status = status,
                Description = description,
                EventDateTime = DateTime.UtcNow,
                CourierName = courier,
                TrackingNumber = tracking,
                Notes = notes,
                CreatedBy = "Admin",
                IsCurrent = true,
                IsCompleted = isCompleted
            });
        }

        [HttpPost("{id}/approve")]
        public async Task<IActionResult> ApproveReturn(int id)
        {
            if (!IsAdmin()) return Forbid();

            var req = await _context.ReturnRequests.FindAsync(id);
            if (req == null) return NotFound();

            req.Status = "Approved";
            req.ApprovalDate = DateTime.UtcNow;

            await AddTimelineEvent(id, "Approved", "Return request approved. Pickup will be scheduled.");

            var customer = await _context.Users.FindAsync(req.CustomerId);
            if (customer != null)
            {
                _context.Notifications.Add(new Notification
                {
                    Title = "Return Approved",
                    Message = $"Your return request for Order #{req.OrderId} has been approved. A pickup will be scheduled soon.",
                    ActionUrl = "return-tracking.html?id=" + id,
                    UserId = customer.Id
                });
                var details = new System.Collections.Generic.Dictionary<string, string> {
                    { "Order No.", $"#{req.OrderId}" },
                    { "Status", "Return Approved" }
                };
                var htmlEmail = LeatherLane_Atelier.Services.EmailTemplateBuilder.BuildStandardEmail("Return Approved", customer.Name, "Your return request has been approved. A pickup will be scheduled soon.", $"/return-tracking?id={id}", "Track Return", details);
                _ = _emailService.SendEmailAsync(customer.Email, "Return Approved", htmlEmail);
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Return approved." });
        }

        [HttpPost("{id}/reject")]
        public async Task<IActionResult> RejectReturn(int id, [FromBody] RejectDto dto)
        {
            if (!IsAdmin()) return Forbid();

            var req = await _context.ReturnRequests.FindAsync(id);
            if (req == null) return NotFound();

            req.Status = "Rejected";
            req.RejectedReason = dto.Reason;

            var order = await _context.Transactions.Include(t => t.Items).FirstOrDefaultAsync(t => t.Id == req.OrderId);
            if (order != null)
            {
                var item = order.Items.FirstOrDefault(i => i.Id == req.OrderItemId);
                if (item != null)
                {
                    item.HasBeenReturned = false;
                    item.ReturnRequestId = null;
                }
            }

            await AddTimelineEvent(id, "Rejected", dto.Reason, null, null, null, true);

            var customer = await _context.Users.FindAsync(req.CustomerId);
            if (customer != null)
            {
                _context.Notifications.Add(new Notification
                {
                    Title = "Return Rejected",
                    Message = $"Your return request for Order #{req.OrderId} has been rejected. Reason: {dto.Reason}",
                    ActionUrl = "return-tracking.html?id=" + id,
                    UserId = customer.Id
                });
                var details = new System.Collections.Generic.Dictionary<string, string> {
                    { "Order No.", $"#{req.OrderId}" },
                    { "Reason", dto.Reason },
                    { "Status", "Rejected" }
                };
                var htmlEmail = LeatherLane_Atelier.Services.EmailTemplateBuilder.BuildStandardEmail("Return Rejected", customer.Name, "Your return request has been rejected.", $"/return-tracking?id={id}", "Track Return", details);
                _ = _emailService.SendEmailAsync(customer.Email, "Return Rejected", htmlEmail);
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Return rejected." });
        }

        [HttpPost("{id}/schedule-pickup")]
        public async Task<IActionResult> SchedulePickup(int id, [FromBody] PickupDto dto)
        {
            if (!IsAdmin()) return Forbid();

            var req = await _context.ReturnRequests.FindAsync(id);
            if (req == null || req.Status != "Approved") return BadRequest("Invalid state.");

            req.Status = "Pickup Scheduled";
            req.CourierName = dto.CourierName;
            req.PickupDate = dto.PickupDate;
            req.TrackingNumber = dto.TrackingNumber;

            await AddTimelineEvent(id, "Pickup Scheduled", $"Pickup Date: {dto.PickupDate.ToShortDateString()}", dto.CourierName, dto.TrackingNumber);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Pickup scheduled." });
        }

        [HttpPost("{id}/inspection")]
        public async Task<IActionResult> ProcessInspection(int id, [FromBody] InspectionDto dto)
        {
            if (!IsAdmin()) return Forbid();

            var req = await _context.ReturnRequests.FindAsync(id);
            if (req == null || req.Status != "Pickup Scheduled") return BadRequest("Invalid state.");

            req.InspectionDate = DateTime.UtcNow;

            if (dto.IsApproved)
            {
                req.Status = "Refund Approved";
                var product = await _context.Products.FindAsync(req.ProductId);
                if (product != null) product.Stock += 1;

                await AddTimelineEvent(id, "Refund Approved", "Inspection passed. Refund will be processed.");
            }
            else
            {
                req.Status = "Rejected";
                req.RejectedReason = dto.Reason;
                
                var order = await _context.Transactions.Include(t => t.Items).FirstOrDefaultAsync(t => t.Id == req.OrderId);
                if (order != null)
                {
                    var item = order.Items.FirstOrDefault(i => i.Id == req.OrderItemId);
                    if (item != null)
                    {
                        item.HasBeenReturned = false;
                        item.ReturnRequestId = null;
                    }
                }

                await AddTimelineEvent(id, "Rejected", "Inspection failed: " + dto.Reason, null, null, null, true);

                var customer = await _context.Users.FindAsync(req.CustomerId);
                if (customer != null)
                {
                    _context.Notifications.Add(new Notification
                    {
                        Title = "Return Inspection Failed",
                        Message = $"Your returned item for Order #{req.OrderId} failed inspection. Reason: {dto.Reason}",
                        ActionUrl = "return-tracking.html?id=" + id,
                        UserId = customer.Id
                    });
                    var details = new System.Collections.Generic.Dictionary<string, string> {
                    { "Order No.", $"#{req.OrderId}" },
                    { "Reason", dto.Reason },
                    { "Status", "Inspection Failed" }
                };
                var htmlEmail = LeatherLane_Atelier.Services.EmailTemplateBuilder.BuildStandardEmail("Return Inspection Failed", customer.Name, "Your returned item failed inspection.", $"/return-tracking?id={id}", "Track Return", details);
                _ = _emailService.SendEmailAsync(customer.Email, "Return Inspection Failed", htmlEmail);
                }
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Inspection processed." });
        }

        [HttpPost("{id}/complete")]
        public async Task<IActionResult> CompleteRefund(int id)
        {
            if (!IsAdmin()) return Forbid();

            var req = await _context.ReturnRequests.FindAsync(id);
            if (req == null || req.Status != "Refund Approved") return BadRequest("Invalid state.");

            req.Status = "Completed";
            req.CompletionDate = DateTime.UtcNow;

            var order = await _context.Transactions.Include(t => t.Items).FirstOrDefaultAsync(t => t.Id == req.OrderId);
            if (order != null)
            {
                var item = order.Items.FirstOrDefault(i => i.Id == req.OrderItemId);
                if (item != null)
                {
                    item.ReturnCompleted = true;
                }
            }

            await AddTimelineEvent(id, "Completed", "Refund processed and return workflow completed.", null, null, null, true);

            var customer = await _context.Users.FindAsync(req.CustomerId);
            if (customer != null)
            {
                _context.Notifications.Add(new Notification
                {
                    Title = "Refund Completed",
                    Message = $"Your refund of ${req.RefundAmount} for Order #{req.OrderId} has been successfully processed.",
                    ActionUrl = "return-tracking.html?id=" + id,
                    UserId = customer.Id
                });
                var details = new System.Collections.Generic.Dictionary<string, string> { { "Order No.", $"#{req.OrderId}" }, { "Refund Amount", $"Rs. {req.RefundAmount:N2}" }, { "Status", "Refund Processed" } };
                var htmlEmail = LeatherLane_Atelier.Services.EmailTemplateBuilder.BuildStandardEmail("Refund Completed", customer.Name, "Your refund has been successfully processed.", $"/return-tracking?id={id}", "View Return Details", details);
                _ = _emailService.SendEmailAsync(customer.Email, "Refund Completed", htmlEmail);
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Return completed." });
        }
    }
}
