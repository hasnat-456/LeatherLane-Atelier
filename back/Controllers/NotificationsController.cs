using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LeatherLane_Atelier.Models;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;

namespace LeatherLane_Atelier.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public NotificationsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetNotifications()
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            IQueryable<Notification> query = _context.Notifications;

            if (role == "Admin")
            {
                // Admin gets all notifications where UserId is null (meaning Admin-wide notifications)
                query = query.Where(n => n.UserId == null);
            }
            else
            {
                // Customer gets their own notifications
                if (int.TryParse(userIdStr, out int userId))
                {
                    query = query.Where(n => n.UserId == userId);
                }
                else
                {
                    return Unauthorized();
                }
            }

            var notifications = await query
                .OrderByDescending(n => n.CreatedAt)
                .Take(50) // limit to 50 for performance
                .ToListAsync();

            return Ok(notifications);
        }

        [HttpPut("{id}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification == null) return NotFound();

            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (role != "Admin")
            {
                if (!int.TryParse(userIdStr, out int userId) || notification.UserId != userId)
                {
                    return Unauthorized();
                }
            }
            else if (notification.UserId != null)
            {
                // Admin trying to read a customer's notification
                return Unauthorized();
            }

            notification.IsRead = true;
            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpPut("read-all")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            IQueryable<Notification> query = _context.Notifications.Where(n => !n.IsRead);

            if (role == "Admin")
            {
                query = query.Where(n => n.UserId == null);
            }
            else
            {
                if (int.TryParse(userIdStr, out int userId))
                {
                    query = query.Where(n => n.UserId == userId);
                }
                else
                {
                    return Unauthorized();
                }
            }

            var unread = await query.ToListAsync();
            foreach (var n in unread)
            {
                n.IsRead = true;
            }

            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}
