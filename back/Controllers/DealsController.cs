using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LeatherLane_Atelier.Models;

namespace LeatherLane_Atelier.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DealsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DealsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Deals
        [HttpGet]
        public async Task<ActionResult<object>> GetDeals()
        {
            var now = DateTime.Now;
            
            // Fetch all deals
            var deals = await _context.Deals.ToListAsync();
            
            // We need to figure out which products have deals.
            var allProducts = await _context.Products.Where(p => p.Available).ToListAsync();
            
            var result = new List<object>();
            
            foreach (var product in allProducts)
            {
                // Find all applicable deals for this product
                var applicableDeals = deals.Where(d => 
                    (d.Scope == "Product" && d.ProductId == product.Id) ||
                    (d.Scope == "Category" && d.CategoryId == product.CategoryId) ||
                    (d.Scope == "All")
                ).ToList();
                
                if (applicableDeals.Any())
                {
                    // Sort deals by priority: Product > Category > All
                    var bestDeal = applicableDeals.OrderBy(d => 
                        d.Scope == "Product" ? 1 : 
                        d.Scope == "Category" ? 2 : 3
                    ).First();
                    
                    // Determine Status
                    string status = "Live";
                    if (now < bestDeal.StartTime) status = "Upcoming";
                    if (now > bestDeal.EndTime) status = "Ended";
                    
                    // Calculate Discount
                    decimal finalPrice = product.Price;
                    decimal originalPrice = product.Price;
                    
                    if (bestDeal.DiscountType == "Percentage")
                    {
                        finalPrice = originalPrice - (originalPrice * (bestDeal.DiscountValue / 100));
                    }
                    else if (bestDeal.DiscountType == "FixedAmount")
                    {
                        finalPrice = originalPrice - bestDeal.DiscountValue;
                    }
                    
                    if (finalPrice < 0) finalPrice = 0;
                    
                    result.Add(new
                    {
                        product = product,
                        deal = bestDeal,
                        finalPrice = finalPrice,
                        status = status
                    });
                }
            }
            
            return Ok(result);
        }

        // POST: api/Deals
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<Deal>> CreateDeal(Deal deal)
        {
            deal.CreatedAt = DateTime.UtcNow;
            
            _context.Deals.Add(deal);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetDeals), new { id = deal.Id }, deal);
        }
        
        // GET: api/Deals/AdminList
        [HttpGet("AdminList")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<Deal>>> GetAdminDealsList()
        {
            return await _context.Deals.OrderByDescending(d => d.CreatedAt).ToListAsync();
        }

        // DELETE: api/Deals/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteDeal(int id)
        {
            var deal = await _context.Deals.FindAsync(id);
            if (deal == null)
            {
                return NotFound();
            }

            _context.Deals.Remove(deal);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
