using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using LeatherLane_Atelier.Models;

namespace LeatherLane_Atelier.Controllers
{
    [Route("api/admin/exchanges")]
    [ApiController]
    [Authorize]
    public class AdminExchangeController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly LeatherLane_Atelier.Services.IEmailService _emailService;

        public AdminExchangeController(ApplicationDbContext context, LeatherLane_Atelier.Services.IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        private bool IsAdmin()
        {
            return User.Claims.Any(c => (c.Type == System.Security.Claims.ClaimTypes.Role || c.Type == "role") && c.Value == "Admin");
        }

        private async Task AddTimelineEvent(int exchangeId, string status, string description, string? courier = null, string? tracking = null, string? notes = null, bool isCompleted = false)
        {
            var prevs = await _context.TimelineEvents.Where(t => t.ReferenceId == exchangeId && t.Type == "Exchange" && t.IsCurrent).ToListAsync();
            foreach (var p in prevs) { p.IsCurrent = false; p.IsCompleted = true; }

            _context.TimelineEvents.Add(new TimelineEvent
            {
                ReferenceId = exchangeId,
                Type = "Exchange",
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

        [HttpGet]
        public async Task<IActionResult> GetAllExchanges([FromQuery] string? status)
        {
            if (!IsAdmin()) return Forbid();

            var query = _context.ExchangeRequests
                .Include(e => e.Customer)
                .Include(e => e.OriginalProduct)
                .Include(e => e.ReplacementProduct)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(e => e.Status == status);
            }

            var exchanges = await query.OrderByDescending(e => e.CreatedAt).Select(e => new
            {
                e.ExchangeId,
                e.OrderId,
                CustomerName = e.Customer.Name,
                OriginalProductName = e.OriginalProduct.Name,
                ReplacementProductName = e.ReplacementProduct != null ? e.ReplacementProduct.Name : null,
                e.Reason,
                e.Status,
                e.RequestDate
            }).ToListAsync();

            return Ok(exchanges);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetExchangeDetails(int id)
        {
            if (!IsAdmin()) return Forbid();

            var request = await _context.ExchangeRequests
                .Include(e => e.Customer)
                .Include(e => e.Order)
                .Include(e => e.OriginalProduct)
                .Include(e => e.ReplacementProduct)
                .Include(e => e.Images)
                .Include(e => e.StatusHistory)
                .FirstOrDefaultAsync(e => e.ExchangeId == id);

            if (request == null) return NotFound();

            return Ok(request);
        }

        [HttpPost("{id}/approve")]
        public async Task<IActionResult> ApproveExchange(int id)
        {
            if (!IsAdmin()) return Forbid();

            var request = await _context.ExchangeRequests.FindAsync(id);
            if (request == null) return NotFound();

            if (request.Status != "Pending Review")
                return BadRequest("Only pending requests can be approved.");

            // Check stock if there's a replacement
            if (request.ReplacementProductId.HasValue)
            {
                var replacement = await _context.Products.FindAsync(request.ReplacementProductId.Value);
                if (replacement == null || replacement.Stock <= 0)
                    return BadRequest("Currently Out of Stock. Cannot approve.");
            }

            request.Status = "Waiting for Customer Return";
            request.ApprovalDate = DateTime.UtcNow;

            await AddTimelineEvent(id, "Waiting for Customer Return", "Request approved. Please ship the item back and provide tracking details.");

            var customer = await _context.Users.FindAsync(request.CustomerId);
            if (customer != null)
            {
                _context.Notifications.Add(new Notification
                {
                    Title = "Exchange Approved",
                    Message = $"Your exchange request for Order #{request.OrderId} has been approved. Please ship the item.",
                    ActionUrl = $"exchange-tracking.html?id={id}",
                    UserId = customer.Id
                });
                _ = _emailService.SendEmailAsync(customer.Email, "Exchange Approved", $"Your exchange request for Order #{request.OrderId} was approved.");
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Exchange approved." });
        }

        [HttpPost("{id}/reject")]
        public async Task<IActionResult> RejectExchange(int id, [FromBody] RejectDto dto)
        {
            if (!IsAdmin()) return Forbid();

            var request = await _context.ExchangeRequests.FindAsync(id);
            if (request == null) return NotFound();

            request.Status = "Rejected";
            request.RejectedReason = dto.Reason;

            // Revert Order Item flags since it was rejected
            var orderItem = await _context.CartItems.FirstOrDefaultAsync(i => i.Id == request.OrderItemId);
            // Wait, we used TransactionItem, not CartItem. 
            // We need to fetch via Transaction
            var order = await _context.Transactions.Include(t => t.Items).FirstOrDefaultAsync(t => t.Id == request.OrderId);
            if (order != null)
            {
                var item = order.Items.FirstOrDefault(i => i.Id == request.OrderItemId);
                if (item != null)
                {
                    item.HasBeenExchanged = false;
                    item.ExchangeRequestId = null;
                }
            }

            await AddTimelineEvent(id, "Inspection Failed", dto.Reason, null, null, null, true);

            var customer = await _context.Users.FindAsync(request.CustomerId);
            if (customer != null)
            {
                _context.Notifications.Add(new Notification
                {
                    Title = "Exchange Rejected",
                    Message = $"Your exchange request for Order #{request.OrderId} has been rejected. Reason: {dto.Reason}",
                    ActionUrl = $"exchange-tracking.html?id={id}",
                    UserId = customer.Id
                });
                _ = _emailService.SendEmailAsync(customer.Email, "Exchange Rejected", $"Your exchange request for Order #{request.OrderId} was rejected. Reason: {dto.Reason}");
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Exchange rejected." });
        }



        [HttpPost("{id}/inspection")]
        public async Task<IActionResult> ProcessInspection(int id, [FromBody] InspectionDto dto)
        {
            if (!IsAdmin()) return Forbid();

            var request = await _context.ExchangeRequests.FindAsync(id);
            if (request == null || request.Status != "Return Shipped by Customer") return BadRequest("Invalid state.");

            request.InspectionDate = DateTime.UtcNow;

            var customer = await _context.Users.FindAsync(request.CustomerId);

            if (dto.IsApproved)
            {
                request.Status = "Replacement Preparing";

                // Inventory Update
                var originalProduct = await _context.Products.FindAsync(request.OriginalProductId);
                if (originalProduct != null) originalProduct.Stock += 1;

                if (request.ReplacementProductId.HasValue)
                {
                    var replacement = await _context.Products.FindAsync(request.ReplacementProductId.Value);
                    if (replacement != null) replacement.Stock -= 1;
                }

                await AddTimelineEvent(id, "Replacement Preparing", "Inspection passed. Replacement preparing.");
                
                if (customer != null)
                {
                    _context.Notifications.Add(new Notification
                    {
                        Title = "Inspection Passed",
                        Message = $"Your returned item for Exchange #{id} passed inspection. We are packing your replacement now.",
                        ActionUrl = $"exchange-tracking.html?id={id}",
                        UserId = customer.Id
                    });
                    _ = _emailService.SendEmailAsync(customer.Email, "Exchange Inspection Passed", $"Good news! Your returned item for Order #{request.OrderId} has arrived and passed inspection. We are packing your replacement now.");
                }
            }
            else
            {
                request.Status = "Inspection Failed";
                request.RejectedReason = dto.Reason;
                
                // We DO NOT revert the Order Item HasBeenExchanged flag yet, 
                // because the workflow is still active until the original is shipped back.
                // It will be reverted when the return is completed.

                await AddTimelineEvent(id, "Inspection Failed", "Inspection failed: " + dto.Reason + ". Original product will be shipped back.", null, null, null, false);

                if (customer != null)
                {
                    _context.Notifications.Add(new Notification
                    {
                        Title = "Exchange Inspection Failed",
                        Message = $"Your exchange item for Order #{request.OrderId} failed inspection. Reason: {dto.Reason}. It will be shipped back to you.",
                        ActionUrl = $"exchange-tracking.html?id={id}",
                        UserId = customer.Id
                    });
                    _ = _emailService.SendEmailAsync(customer.Email, "Exchange Inspection Failed", $"Your returned item for Order #{request.OrderId} was received but failed inspection. Reason: {dto.Reason}");
                }
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Inspection processed." });
        }

        [HttpPost("{id}/ship-replacement")]
        public async Task<IActionResult> ShipReplacement(int id, [FromBody] ShipmentDto dto)
        {
            if (!IsAdmin()) return Forbid();

            var request = await _context.ExchangeRequests.FindAsync(id);
            if (request == null || request.Status != "Replacement Preparing") return BadRequest("Invalid state.");

            request.Status = "Replacement Shipped";
            request.ReplacementShipmentDate = DateTime.UtcNow;
            // Update tracking
            request.CourierName = dto.CourierName;
            request.TrackingNumber = dto.TrackingNumber;

            await AddTimelineEvent(id, "Replacement Shipped", $"Replacement shipped via {dto.CourierName}. Tracking: {dto.TrackingNumber}", dto.CourierName, dto.TrackingNumber);

            var customer = await _context.Users.FindAsync(request.CustomerId);
            if (customer != null)
            {
                _context.Notifications.Add(new Notification
                {
                    Title = "Replacement Shipped",
                    Message = $"Your replacement item for Exchange #{id} has been shipped.",
                    ActionUrl = $"exchange-tracking.html?id={id}",
                    UserId = customer.Id
                });
                _ = _emailService.SendEmailAsync(customer.Email, "Replacement Shipped", $"Your replacement item has shipped. Tracking: {dto.TrackingNumber}");
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Replacement shipped." });
        }

        [HttpPost("{id}/return-original")]
        public async Task<IActionResult> ReturnOriginal(int id, [FromBody] ShipmentDto dto)
        {
            if (!IsAdmin()) return Forbid();

            var request = await _context.ExchangeRequests.FindAsync(id);
            if (request == null || request.Status != "Inspection Failed") return BadRequest("Invalid state.");

            request.Status = "Original Shipped Back";
            request.CompletionDate = DateTime.UtcNow;
            request.CourierName = dto.CourierName;
            request.TrackingNumber = dto.TrackingNumber;

            // Revert Order Item since the exchange officially failed and is closed
            var order = await _context.Transactions.Include(t => t.Items).FirstOrDefaultAsync(t => t.Id == request.OrderId);
            if (order != null)
            {
                var item = order.Items.FirstOrDefault(i => i.Id == request.OrderItemId);
                if (item != null)
                {
                    item.HasBeenExchanged = false;
                    item.ExchangeRequestId = null;
                }
            }

            await AddTimelineEvent(id, "Original Shipped Back", $"Original product shipped back via {dto.CourierName}. Tracking: {dto.TrackingNumber}", dto.CourierName, dto.TrackingNumber, null, true);

            await _context.SaveChangesAsync();
            return Ok(new { message = "Original product shipped back." });
        }

        [HttpPost("{id}/complete")]
        public async Task<IActionResult> CompleteExchange(int id)
        {
            if (!IsAdmin()) return Forbid();

            var request = await _context.ExchangeRequests.FindAsync(id);
            if (request == null || request.Status != "Replacement Shipped") return BadRequest("Invalid state.");

            request.Status = "Completed";
            request.CompletionDate = DateTime.UtcNow;

            var order = await _context.Transactions.Include(t => t.Items).FirstOrDefaultAsync(t => t.Id == request.OrderId);
            if (order != null)
            {
                var item = order.Items.FirstOrDefault(i => i.Id == request.OrderItemId);
                if (item != null)
                {
                    item.ExchangeCompleted = true;
                }
            }

            await AddTimelineEvent(id, "Completed", "Exchange workflow completed.", null, null, null, true);

            var customer = await _context.Users.FindAsync(request.CustomerId);
            if (customer != null)
            {
                _context.Notifications.Add(new Notification
                {
                    Title = "Replacement Delivered",
                    Message = $"Your replacement item for Exchange #{id} has been delivered. Enjoy!",
                    ActionUrl = $"exchange-tracking.html?id={id}",
                    UserId = customer.Id
                });
                _ = _emailService.SendEmailAsync(customer.Email, "Replacement Delivered", $"Your replacement item for Order #{request.OrderId} has been delivered. Thank you for your patience.");
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Exchange completed." });
        }
    }

    public class RejectDto { public string Reason { get; set; } = string.Empty; }
    public class PickupDto { public string CourierName { get; set; } = string.Empty; public DateTime PickupDate { get; set; } public string TrackingNumber { get; set; } = string.Empty; }
    public class InspectionDto { public bool IsApproved { get; set; } public string Reason { get; set; } = string.Empty; }
    public class ShipmentDto { public string CourierName { get; set; } = string.Empty; public string TrackingNumber { get; set; } = string.Empty; }
}
