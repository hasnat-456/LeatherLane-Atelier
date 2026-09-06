using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using LeatherLane_Atelier.Models;
using System.Security.Claims;

namespace LeatherLane_Atelier.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TrackingController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public TrackingController(ApplicationDbContext context)
        {
            _context = context;
        }

        private int GetUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return claim != null ? int.Parse(claim) : 0;
        }

        [HttpGet("order/{id}")]
        public async Task<IActionResult> GetOrderTracking(string id)
        {
            var userId = GetUserId();
            int.TryParse(id.TrimStart('#'), out var numericId);

            var order = await _context.Transactions
                .Include(t => t.Items)
                .FirstOrDefaultAsync(t => (t.OrderId == id || (numericId > 0 && t.Id == numericId)) && t.UserId == userId);

            if (order == null) return NotFound(new { message = "Order not found." });

            var timeline = await _context.TimelineEvents
                .Where(t => t.ReferenceId == order.Id && t.Type == "Order")
                .OrderBy(t => t.EventDateTime)
                .ToListAsync();

            return Ok(new
            {
                orderId = order.Id,
                orderNumber = order.OrderId,
                orderDate = order.CreatedAt,
                paymentStatus = order.Status,
                paymentMethod = order.PaymentMethod,
                shippingName = order.ShippingName,
                paymentRefId = order.PaymentRefId,
                senderName = order.SenderName,
                senderMobile = order.SenderMobile,
                paymentScreenshot = order.PaymentScreenshot,
                rejectionReason = order.RejectionReason,
                timeline = timeline
            });
        }

        [HttpGet("exchange/{id}")]
        public async Task<IActionResult> GetExchangeTracking(string id)
        {
            var userId = GetUserId();
            int.TryParse(id.TrimStart('#'), out var numericId);

            var exchange = await _context.ExchangeRequests
                .Include(e => e.Order)
                .Include(e => e.OriginalProduct)
                .Include(e => e.ReplacementProduct)
                .FirstOrDefaultAsync(e => (e.ExchangeCode == id || (numericId > 0 && e.ExchangeId == numericId)) && e.CustomerId == userId);

            if (exchange == null) return NotFound(new { message = "Exchange request not found." });

            var timeline = await _context.TimelineEvents
                .Where(t => t.ReferenceId == exchange.ExchangeId && t.Type == "Exchange")
                .OrderBy(t => t.EventDateTime)
                .ToListAsync();

            return Ok(new
            {
                exchangeId = exchange.ExchangeId,
                exchangeCode = exchange.ExchangeCode,
                orderId = exchange.OrderId,
                orderNumber = LeatherLane_Atelier.Services.IdGenerator.ResolveOrderDisplay(exchange.Order?.OrderId, exchange.OrderId),
                originalProduct = exchange.OriginalProduct?.Name,
                replacementProduct = exchange.ReplacementProduct?.Name,
                reason = exchange.Reason,
                status = exchange.Status,
                requestDate = exchange.RequestDate,
                timeline = timeline
            });
        }

        [HttpGet("return/{id}")]
        public async Task<IActionResult> GetReturnTracking(string id)
        {
            var userId = GetUserId();
            int.TryParse(id.TrimStart('#'), out var numericId);

            var ret = await _context.ReturnRequests
                .Include(r => r.Order)
                .Include(r => r.Product)
                .FirstOrDefaultAsync(r => (r.ReturnCode == id || (numericId > 0 && (r.ReturnId == numericId || r.OrderId == numericId))) && r.CustomerId == userId);

            if (ret == null) return NotFound(new { message = "Return request not found." });

            var timeline = await _context.TimelineEvents
                .Where(t => t.ReferenceId == ret.ReturnId && t.Type == "Return")
                .OrderBy(t => t.EventDateTime)
                .ToListAsync();

            return Ok(new
            {
                returnId = ret.ReturnId,
                returnCode = ret.ReturnCode,
                orderId = ret.OrderId,
                orderNumber = LeatherLane_Atelier.Services.IdGenerator.ResolveOrderDisplay(ret.Order?.OrderId, ret.OrderId),
                product = ret.Product?.Name,
                reason = ret.Reason,
                status = ret.Status,
                requestDate = ret.RequestDate,
                refundAmount = ret.RefundAmount,
                timeline = timeline
            });
        }
    }
}
