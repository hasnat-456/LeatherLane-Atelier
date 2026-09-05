using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Text.RegularExpressions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using LeatherLane_Atelier.Models;

namespace LeatherLane_Atelier.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BlogsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public BlogsController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [HttpGet]
        public async Task<IActionResult> GetBlogs([FromQuery] string? category, [FromQuery] string? search, [FromQuery] bool? isFeatured)
        {
            var query = _context.Blogs.AsNoTracking().AsQueryable();

            if (!string.IsNullOrEmpty(category))
                query = query.Where(b => b.Category == category);

            if (!string.IsNullOrEmpty(search))
                query = query.Where(b => b.Title.Contains(search) || (b.Excerpt != null && b.Excerpt.Contains(search)));

            if (isFeatured.HasValue && isFeatured.Value)
                query = query.Where(b => b.IsFeatured);

            // Return lightweight summary for fast listing (omit huge HTML Content)
            var blogs = await query.OrderByDescending(b => b.CreatedAt)
                .Select(b => new {
                    b.Id,
                    b.Title,
                    b.Slug,
                    b.Excerpt,
                    b.Image,
                    b.Category,
                    b.Author,
                    b.Views,
                    b.IsFeatured,
                    b.CreatedAt,
                    b.UpdatedAt,
                    // If in admin mode or specifically needed, content is fetched via GetBlog(id)
                    Content = b.Content != null && b.Content.Length < 1000 ? b.Content : b.Excerpt
                })
                .ToListAsync();

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

        [HttpPost("upload-image")]
        public async Task<IActionResult> UploadImage(IFormFile imageFile)
        {
            if (imageFile == null || imageFile.Length == 0)
                return BadRequest("No image provided.");

            var currentDir = Directory.GetCurrentDirectory();
            var frontPath = currentDir.EndsWith("back", StringComparison.OrdinalIgnoreCase) 
                ? Path.Combine(currentDir, "..", "front") 
                : Path.Combine(currentDir, "front");
            var uploadsFolder = Path.Combine(frontPath, "uploads", "blogs");
            Directory.CreateDirectory(uploadsFolder);

            var parentUploads = Path.Combine(currentDir, "..", "front", "uploads", "blogs");
            bool syncToParent = Directory.Exists(Path.Combine(currentDir, "..", "front"));
            if (syncToParent) Directory.CreateDirectory(parentUploads);

            var uniqueFileName = Guid.NewGuid().ToString("N") + ".jpg";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            // Fast stream directly to disk (client already pre-compresses)
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(fileStream);
            }

            if (syncToParent)
            {
                try { System.IO.File.Copy(filePath, Path.Combine(parentUploads, uniqueFileName), true); } catch {}
            }

            var relativeUrl = "/uploads/blogs/" + uniqueFileName;
            return Ok(new { url = relativeUrl });
        }

        [HttpPost]
        public async Task<IActionResult> CreateBlog([FromBody] Blog blog)
        {
            var currentDir = Directory.GetCurrentDirectory();
            var frontPath = currentDir.EndsWith("back", StringComparison.OrdinalIgnoreCase) 
                ? Path.Combine(currentDir, "..", "front") 
                : Path.Combine(currentDir, "front");
            var uploadsFolder = Path.Combine(frontPath, "uploads", "blogs");
            Directory.CreateDirectory(uploadsFolder);

            if (!string.IsNullOrEmpty(blog.Image) && blog.Image.StartsWith("data:image"))
            {
                blog.Image = SaveBase64Image(blog.Image, uploadsFolder);
            }

            if (!string.IsNullOrEmpty(blog.Content))
            {
                blog.Content = CleanBlogContentBase64(blog.Content, uploadsFolder);
            }

            blog.CreatedAt = DateTime.UtcNow;
            blog.UpdatedAt = DateTime.UtcNow;
            _context.Blogs.Add(blog);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetBlog), new { id = blog.Id }, blog);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateBlog(int id, [FromBody] Blog blog)
        {
            var existing = await _context.Blogs.FindAsync(id);
            if (existing == null) return NotFound();

            var currentDir = Directory.GetCurrentDirectory();
            var frontPath = currentDir.EndsWith("back", StringComparison.OrdinalIgnoreCase) 
                ? Path.Combine(currentDir, "..", "front") 
                : Path.Combine(currentDir, "front");
            var uploadsFolder = Path.Combine(frontPath, "uploads", "blogs");
            Directory.CreateDirectory(uploadsFolder);

            existing.Title = blog.Title;
            existing.Slug = blog.Slug;
            existing.Excerpt = blog.Excerpt;
            
            if (!string.IsNullOrEmpty(blog.Content))
            {
                existing.Content = CleanBlogContentBase64(blog.Content, uploadsFolder);
            }
            else
            {
                existing.Content = blog.Content;
            }

            existing.Category = blog.Category;
            
            if (!string.IsNullOrEmpty(blog.Image))
            {
                if (blog.Image.StartsWith("data:image"))
                {
                    existing.Image = SaveBase64Image(blog.Image, uploadsFolder);
                }
                else
                {
                    existing.Image = blog.Image;
                }
            }

            existing.Author = blog.Author;
            existing.Tags = blog.Tags;
            existing.IsFeatured = blog.IsFeatured;
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(existing);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBlog(int id)
        {
            var blog = await _context.Blogs.FindAsync(id);
            if (blog == null) return NotFound();

            _context.Blogs.Remove(blog);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Blog deleted successfully" });
        }

        public static string SaveBase64Image(string base64String, string uploadsFolder)
        {
            try 
            {
                var match = Regex.Match(base64String, @"data:image/(?<type>.+?);base64,(?<data>.+)");
                if (!match.Success) return base64String;
                
                string ext = "jpg";
                string base64Data = match.Groups["data"].Value;
                byte[] bytes = Convert.FromBase64String(base64Data);
                
                string fileName = Guid.NewGuid().ToString("N") + "." + ext;
                string filePath = Path.Combine(uploadsFolder, fileName);
                
                using var ms = new MemoryStream(bytes);
                using var image = SixLabors.ImageSharp.Image.Load(ms);
                if (image.Width > 1200 || image.Height > 1200)
                {
                    image.Mutate(x => x.Resize(new ResizeOptions
                    {
                        Size = new SixLabors.ImageSharp.Size(1200, 1200),
                        Mode = ResizeMode.Max
                    }));
                }
                var encoder = new JpegEncoder { Quality = 82 };
                image.Save(filePath, encoder);
                
                return "/uploads/blogs/" + fileName;
            } 
            catch 
            {
                return base64String;
            }
        }

        public static string CleanBlogContentBase64(string content, string uploadsFolder)
        {
            if (string.IsNullOrEmpty(content)) return content;
            
            return Regex.Replace(content, @"src=[""']data:image/(?<type>[a-zA-Z0-9+]+);base64,(?<data>[^""']+)[""']", match =>
            {
                try
                {
                    string base64Data = match.Groups["data"].Value;
                    byte[] bytes = Convert.FromBase64String(base64Data);
                    string fileName = Guid.NewGuid().ToString("N") + ".jpg";
                    string filePath = Path.Combine(uploadsFolder, fileName);
                    
                    using var ms = new MemoryStream(bytes);
                    using var image = SixLabors.ImageSharp.Image.Load(ms);
                    if (image.Width > 1200 || image.Height > 1200)
                    {
                        image.Mutate(x => x.Resize(new ResizeOptions
                        {
                            Size = new SixLabors.ImageSharp.Size(1200, 1200),
                            Mode = ResizeMode.Max
                        }));
                    }
                    var encoder = new JpegEncoder { Quality = 82 };
                    image.Save(filePath, encoder);
                    
                    return $"src=\"/uploads/blogs/{fileName}\"";
                }
                catch
                {
                    return match.Value;
                }
            });
        }
    }
}
