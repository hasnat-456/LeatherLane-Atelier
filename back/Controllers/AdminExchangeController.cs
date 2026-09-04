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
                .Include(e => e.Order)
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
                ExchangeCode = e.ExchangeCode,
                e.OrderId,
                OrderNumber = e.Order.OrderId,
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

            var request = await _context.ExchangeRequests
                .Include(e => e.Order)
                .FirstOrDefaultAsync(e => e.ExchangeId == id);
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
                string orderDisplay = request.Order?.OrderId ?? request.OrderId.ToString();
                var origProd = await _context.Products.FindAsync(request.OriginalProductId);
                var repProd = request.ReplacementProductId.HasValue ? await _context.Products.FindAsync(request.ReplacementProductId.Value) : null;

                _context.Notifications.Add(new Notification
                {
                    Title = "Exchange Approved",
                    Message = $"Your exchange request ({request.ExchangeCode}) for Order {orderDisplay} has been approved. Please ship the item back via courier.",
                    ActionUrl = $"exchange-tracking.html?id={request.ExchangeCode}",
                    UserId = customer.Id
                });

                string approveMsg = $"Your exchange request <strong>{request.ExchangeCode}</strong> for Order <strong>{orderDisplay}</strong> has been approved. Please dispatch the original unworn product back to our workshop via any of our authorized couriers (<strong>TCS, M&amp;P, Leopards Courier, PostEx</strong>).";
                var emailHtml = LeatherLane_Atelier.Services.EmailTemplateBuilder.BuildExchangeEmail(
                    "Exchange Request Approved", 
                    customer.Name, 
                    request.ExchangeCode ?? ("EXC-" + request.ExchangeId),
                    orderDisplay,
                    "Approved - Ship Return",
                    approveMsg,
                    origProd?.Name ?? "Original Item",
                    origProd?.ProductId,
                    origProd?.Image,
                    repProd?.Name ?? origProd?.Name ?? "Same Item",
                    repProd?.ProductId ?? origProd?.ProductId,
                    repProd?.Image ?? origProd?.Image,
                    request.Reason,
                    $"/exchange-tracking.html?id={request.ExchangeCode}",
                    "Submit Return Tracking"
                );
                _ = _emailService.SendEmailAsync(customer.Email, $"Exchange Approved ({request.ExchangeCode}) | LeatherLane Atelier", emailHtml);
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Exchange approved." });
        }

        [HttpPost("{id}/reject")]
        public async Task<IActionResult> RejectExchange(int id, [FromBody] RejectDto dto)
        {
            if (!IsAdmin()) return Forbid();

            var request = await _context.ExchangeRequests
                .Include(e => e.Order)
                .Include(e => e.OriginalProduct)
                .Include(e => e.ReplacementProduct)
                .FirstOrDefaultAsync(e => e.ExchangeId == id);

            if (request == null) return NotFound();

            request.Status = "Rejected";
            request.RejectedReason = dto.Reason;

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

            await AddTimelineEvent(id, "Exchange Rejected", dto.Reason, null, null, null, true);

            var customer = await _context.Users.FindAsync(request.CustomerId);
            if (customer != null)
            {
                string orderDisplay = request.Order?.OrderId ?? ("#" + request.OrderId);
                var exchangeCode = request.ExchangeCode ?? ("EXC-" + id);
                var origProd = request.OriginalProduct ?? await _context.Products.FindAsync(request.OriginalProductId);
                var repProd = request.ReplacementProduct ?? (request.ReplacementProductId.HasValue ? await _context.Products.FindAsync(request.ReplacementProductId.Value) : null);

                _context.Notifications.Add(new Notification
                {
                    Title = "Exchange Rejected",
                    Message = $"Your exchange request for Order {orderDisplay} (Exchange {exchangeCode}) has been rejected. Reason: {dto.Reason}",
                    ActionUrl = $"exchange-tracking.html?id={exchangeCode}",
                    UserId = customer.Id
                });

                string rejectMsg = $"We have reviewed your exchange request for Order <strong>{orderDisplay}</strong> (Exchange <strong>{exchangeCode}</strong>). Unfortunately, your request could not be approved due to the reason specified below.";

                var emailHtml = LeatherLane_Atelier.Services.EmailTemplateBuilder.BuildExchangeEmail(
                    "Exchange Request Rejected",
                    customer.Name,
                    exchangeCode,
                    orderDisplay,
                    "Rejected",
                    rejectMsg,
                    origProd?.Name ?? "Original Item",
                    origProd?.ProductId,
                    origProd?.Image,
                    repProd?.Name ?? origProd?.Name ?? "Same Item",
                    repProd?.ProductId ?? origProd?.ProductId,
                    repProd?.Image ?? origProd?.Image,
                    request.Reason,
                    $"/exchange-tracking.html?id={exchangeCode}",
                    "View Exchange Details",
                    null,
                    null,
                    dto.Reason
                );

                _ = _emailService.SendEmailAsync(customer.Email, $"Exchange Rejected ({exchangeCode}) | LeatherLane Atelier", emailHtml);
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
                    string orderDisplay = request.Order?.OrderId ?? request.OrderId.ToString();
                    var origProd = await _context.Products.FindAsync(request.OriginalProductId);
                    var repProd = request.ReplacementProductId.HasValue ? await _context.Products.FindAsync(request.ReplacementProductId.Value) : null;
                    var exchangeCode = request.ExchangeCode ?? ("EXC-" + id);

                    _context.Notifications.Add(new Notification
                    {
                        Title = "Inspection Passed",
                        Message = $"Your returned item for Exchange ({exchangeCode}) passed inspection. We are packaging your unfaulted replacement piece now.",
                        ActionUrl = $"exchange-tracking.html?id={exchangeCode}",
                        UserId = customer.Id
                    });

                    string passMsg = $"Good news! Your returned product for Exchange <strong>{exchangeCode}</strong> has successfully passed our workshop inspection. We are packaging an unfaulted, brand-new piece of the same product and size for dispatch.";
                    var htmlEmail = LeatherLane_Atelier.Services.EmailTemplateBuilder.BuildExchangeEmail(
                        "Inspection Passed: Preparing Replacement",
                        customer.Name,
                        exchangeCode,
                        orderDisplay,
                        "Inspection Passed - Replacement Preparing",
                        passMsg,
                        origProd?.Name ?? "Original Item",
                        origProd?.ProductId,
                        origProd?.Image,
                        repProd?.Name ?? origProd?.Name ?? "Same Item",
                        repProd?.ProductId ?? origProd?.ProductId,
                        repProd?.Image ?? origProd?.Image,
                        request.Reason,
                        $"/exchange-tracking.html?id={exchangeCode}",
                        "Track Replacement Progress"
                    );
                    _ = _emailService.SendEmailAsync(customer.Email, $"Inspection Passed ({exchangeCode}) | LeatherLane Atelier", htmlEmail);
                }
            }
            else
            {
                request.Status = "Inspection Failed";
                request.RejectedReason = dto.Reason;

                await AddTimelineEvent(id, "Inspection Failed", "Inspection failed: " + dto.Reason + ". Original product will be shipped back.", null, null, null, false);

                if (customer != null)
                {
                    string orderDisplay = request.Order?.OrderId ?? request.OrderId.ToString();
                    var origProd = await _context.Products.FindAsync(request.OriginalProductId);
                    var exchangeCode = request.ExchangeCode ?? ("EXC-" + id);

                    _context.Notifications.Add(new Notification
                    {
                        Title = "Exchange Inspection Failed",
                        Message = $"Your returned item for Exchange ({exchangeCode}) did not pass inspection. Reason: {dto.Reason}. The original piece is being returned to you.",
                        ActionUrl = $"exchange-tracking.html?id={exchangeCode}",
                        UserId = customer.Id
                    });

                    string failMsg = $"Following physical inspection for Exchange <strong>{exchangeCode}</strong>, the exchange could not be approved due to the reason specified below. Your original item has been scheduled to ship back to your address.";
                    var htmlEmail = LeatherLane_Atelier.Services.EmailTemplateBuilder.BuildExchangeEmail(
                        "Exchange Inspection Failed",
                        customer.Name,
                        exchangeCode,
                        orderDisplay,
                        "Inspection Failed",
                        failMsg,
                        origProd?.Name ?? "Original Item",
                        origProd?.ProductId,
                        origProd?.Image,
                        origProd?.Name ?? "Original Item",
                        origProd?.ProductId,
                        origProd?.Image,
                        request.Reason,
                        $"/exchange-tracking.html?id={exchangeCode}",
                        "Track Exchange Portal",
                        null,
                        null,
                        dto.Reason
                    );
                    _ = _emailService.SendEmailAsync(customer.Email, $"Inspection Update ({exchangeCode}) | LeatherLane Atelier", htmlEmail);
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
            request.CourierName = dto.CourierName;
            request.TrackingNumber = dto.TrackingNumber;

            await AddTimelineEvent(id, "Replacement Shipped", $"Replacement shipped via {dto.CourierName}. Tracking: {dto.TrackingNumber}", dto.CourierName, dto.TrackingNumber);

            var customer = await _context.Users.FindAsync(request.CustomerId);
            if (customer != null)
            {
                string orderDisplay = request.Order?.OrderId ?? request.OrderId.ToString();
                var origProd = await _context.Products.FindAsync(request.OriginalProductId);
                var repProd = request.ReplacementProductId.HasValue ? await _context.Products.FindAsync(request.ReplacementProductId.Value) : null;
                var exchangeCode = request.ExchangeCode ?? ("EXC-" + id);

                _context.Notifications.Add(new Notification
                {
                    Title = "Replacement Shipped",
                    Message = $"Your replacement item for Exchange ({exchangeCode}) has been shipped via {dto.CourierName} (Tracking: {dto.TrackingNumber}).",
                    ActionUrl = $"exchange-tracking.html?id={exchangeCode}",
                    UserId = customer.Id
                });

                string repShippedMsg = $"Your returned product for Exchange <strong>{exchangeCode}</strong> passed physical inspection. We have dispatched a pristine, brand-new piece of the same product in the same size via <strong>{dto.CourierName}</strong>.";
                var htmlEmail = LeatherLane_Atelier.Services.EmailTemplateBuilder.BuildExchangeEmail(
                    "Replacement Dispatched",
                    customer.Name,
                    exchangeCode,
                    orderDisplay,
                    "Replacement Dispatched",
                    repShippedMsg,
                    origProd?.Name ?? "Original Item",
                    origProd?.ProductId,
                    origProd?.Image,
                    repProd?.Name ?? origProd?.Name ?? "Same Item",
                    repProd?.ProductId ?? origProd?.ProductId,
                    repProd?.Image ?? origProd?.Image,
                    request.Reason,
                    $"/exchange-tracking.html?id={exchangeCode}",
                    "Track Replacement Parcel",
                    dto.CourierName,
                    dto.TrackingNumber
                );
                _ = _emailService.SendEmailAsync(customer.Email, $"Replacement Shipped ({exchangeCode}) | LeatherLane Atelier", htmlEmail);
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

            var customer = await _context.Users.FindAsync(request.CustomerId);
            if (customer != null)
            {
                string orderDisplay = request.Order?.OrderId ?? request.OrderId.ToString();
                var origProd = await _context.Products.FindAsync(request.OriginalProductId);
                var exchangeCode = request.ExchangeCode ?? ("EXC-" + id);

                _context.Notifications.Add(new Notification
                {
                    Title = "Original Item Returned",
                    Message = $"Your original item for Exchange ({exchangeCode}) has been dispatched back to you via {dto.CourierName} (Tracking: {dto.TrackingNumber}).",
                    ActionUrl = $"exchange-tracking.html?id={exchangeCode}",
                    UserId = customer.Id
                });

                string origShippedMsg = $"Following physical inspection for Exchange <strong>{exchangeCode}</strong>, the exchange could not be approved due to the reason specified below. Your original item has been safely dispatched back to your address via <strong>{dto.CourierName}</strong>.";
                var htmlEmail = LeatherLane_Atelier.Services.EmailTemplateBuilder.BuildExchangeEmail(
                    "Original Item Returned",
                    customer.Name,
                    exchangeCode,
                    orderDisplay,
                    "Original Shipped Back",
                    origShippedMsg,
                    origProd?.Name ?? "Original Item",
                    origProd?.ProductId,
                    origProd?.Image,
                    origProd?.Name ?? "Original Item",
                    origProd?.ProductId,
                    origProd?.Image,
                    request.Reason,
                    $"/exchange-tracking.html?id={exchangeCode}",
                    "Track Parcel",
                    dto.CourierName,
                    dto.TrackingNumber,
                    request.RejectedReason
                );
                _ = _emailService.SendEmailAsync(customer.Email, $"Original Item Dispatched Back ({exchangeCode}) | LeatherLane Atelier", htmlEmail);
            }

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
                string orderDisplay = request.Order?.OrderId ?? request.OrderId.ToString();
                var origProd = await _context.Products.FindAsync(request.OriginalProductId);
                var repProd = request.ReplacementProductId.HasValue ? await _context.Products.FindAsync(request.ReplacementProductId.Value) : null;
                var exchangeCode = request.ExchangeCode ?? ("EXC-" + id);

                _context.Notifications.Add(new Notification
                {
                    Title = "Exchange Completed",
                    Message = $"Your exchange request ({exchangeCode}) for Order {orderDisplay} is now successfully completed.",
                    ActionUrl = $"exchange-tracking.html?id={exchangeCode}",
                    UserId = customer.Id
                });

                string completeMsg = $"Your exchange request <strong>{exchangeCode}</strong> for Order <strong>{orderDisplay}</strong> is now successfully completed. Thank you for your continued trust in LeatherLane Atelier.";
                var htmlEmail = LeatherLane_Atelier.Services.EmailTemplateBuilder.BuildExchangeEmail(
                    "Exchange Completed",
                    customer.Name,
                    exchangeCode,
                    orderDisplay,
                    "Exchange Completed",
                    completeMsg,
                    origProd?.Name ?? "Original Item",
                    origProd?.ProductId,
                    origProd?.Image,
                    repProd?.Name ?? origProd?.Name ?? "Same Item",
                    repProd?.ProductId ?? origProd?.ProductId,
                    repProd?.Image ?? origProd?.Image,
                    request.Reason,
                    $"/products.html",
                    "Explore Atelier Collection",
                    request.CourierName,
                    request.TrackingNumber
                );
                _ = _emailService.SendEmailAsync(customer.Email, $"Exchange Completed ({exchangeCode}) | LeatherLane Atelier", htmlEmail);
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
