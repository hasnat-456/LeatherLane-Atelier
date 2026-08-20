using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LeatherLane_Atelier.Models;

namespace LeatherLane_Atelier.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class FavoritesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public FavoritesController(ApplicationDbContext context)
        {
            _context = context;
        }

        private int GetUserId()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdStr, out int userId)) return userId;
            return 0;
        }

        // GET: api/Favorites
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetFavorites()
        {
            var userId = GetUserId();
            if (userId == 0) return Unauthorized();

            var favorites = await _context.Favorites
                .Where(f => f.UserId == userId)
                .Include(f => f.Product)
                .OrderByDescending(f => f.CreatedAt)
                .Select(f => new
                {
                    f.Id,
                    f.ProductId,
                    f.CreatedAt,
                    Product = new
                    {
                        f.Product.Id,
                        f.Product.Name,
                        f.Product.Slug,
                        f.Product.Price,
                        f.Product.OriginalPrice,
                        f.Product.Images,
                        f.Product.Discount
                    }
                })
                .ToListAsync();

            return Ok(favorites);
        }

        // POST: api/Favorites/5
        [HttpPost("{productId}")]
        public async Task<ActionResult> AddFavorite(int productId)
        {
            var userId = GetUserId();
            if (userId == 0) return Unauthorized();

            var existing = await _context.Favorites.FirstOrDefaultAsync(f => f.UserId == userId && f.ProductId == productId);
            if (existing != null)
            {
                return Ok(new { message = "Already in favorites" });
            }

            var favorite = new Favorite
            {
                UserId = userId,
                ProductId = productId,
                CreatedAt = DateTime.Now
            };

            _context.Favorites.Add(favorite);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Added to favorites" });
        }

        // DELETE: api/Favorites/5
        [HttpDelete("{productId}")]
        public async Task<IActionResult> RemoveFavorite(int productId)
        {
            var userId = GetUserId();
            if (userId == 0) return Unauthorized();

            var favorite = await _context.Favorites.FirstOrDefaultAsync(f => f.UserId == userId && f.ProductId == productId);
            if (favorite == null)
            {
                return NotFound(new { message = "Favorite not found" });
            }

            _context.Favorites.Remove(favorite);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Removed from favorites" });
        }
    }
}
