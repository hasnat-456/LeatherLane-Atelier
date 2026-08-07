using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LeatherLane_Atelier.Models;

namespace LeatherLane_Atelier.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BlogsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public BlogsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetBlogs([FromQuery] string? category, [FromQuery] string? search, [FromQuery] bool? isFeatured)
        {
            var query = _context.Blogs.AsQueryable();

            if (!string.IsNullOrEmpty(category))
                query = query.Where(b => b.Category == category);

            if (!string.IsNullOrEmpty(search))
                query = query.Where(b => b.Title.Contains(search) || b.Content.Contains(search));

            if (isFeatured.HasValue && isFeatured.Value)
                query = query.Where(b => b.IsFeatured);

            var blogs = await query.OrderByDescending(b => b.CreatedAt).ToListAsync();
            return Ok(blogs);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetBlog(int id)
        {
            var blog = await _context.Blogs.FindAsync(id);
            if (blog == null) return NotFound(new { message = "Blog not found" });

            blog.Views += 1;
            await _context.SaveChangesAsync();

            return Ok(blog);
        }

        [HttpPost]
        public async Task<IActionResult> CreateBlog([FromBody] Blog blog)
        {
            _context.Blogs.Add(blog);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetBlog), new { id = blog.Id }, blog);
        }
    }
}
