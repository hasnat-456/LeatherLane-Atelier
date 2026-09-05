using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LeatherLane_Atelier.Models;
using LeatherLane_Atelier.Services;
using System.Threading.Tasks;
using System.Linq;

namespace LeatherLane_Atelier.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NewsletterController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;

        public NewsletterController(ApplicationDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        public class SubscribeRequest
        {
            public string Email { get; set; }
        }

        [HttpPost("subscribe")]
        public async Task<IActionResult> Subscribe([FromBody] SubscribeRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Email)) return BadRequest(new { message = "Email is required" });
            
            var exists = await _context.NewsletterSubscribers.AnyAsync(s => s.Email == req.Email);
            if (!exists)
            {
                _context.NewsletterSubscribers.Add(new NewsletterSubscriber { Email = req.Email });
                await _context.SaveChangesAsync();
                
                // Send welcome email upon newsletter subscription (no discounts)
                string subject = "Welcome to LeatherLane Atelier!";
                string body = @"
                    <div style='font-family: Arial, sans-serif; color: #333; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #eaeaea; border-radius: 8px;'>
                        <h2 style='color: #4A1515; text-align: center;'>Welcome to the LeatherLane Family!</h2>
                        <p>Thank you for subscribing to our newsletter. You are now part of our exclusive circle to receive updates on bespoke handcrafted collections, artisan journal stories, and new releases.</p>
                        <div style='background-color: #F5EFE7; padding: 18px; border-radius: 6px; text-align: center; margin: 20px 0; border-left: 3.5px solid #C79A52;'>
                            <h3 style='margin-top: 0; color: #4A1515; font-size: 1.15rem;'>Bespoke Craftsmanship & Lifetime Quality</h3>
                            <p style='margin: 0; color: #555; font-size: 0.95rem; line-height: 1.5;'>Every pair of footwear and leather accessory is meticulously handcrafted with pure, genuine leather by master artisans.</p>
                        </div>
                        <p>We invite you to explore our handcrafted collections and experience timeless elegance.</p>
                        <br>
                        <p>Warm regards,<br><strong>LeatherLane Atelier Team</strong></p>
                    </div>";
                var htmlEmail = LeatherLane_Atelier.Services.EmailTemplateBuilder.BuildStandardEmail(subject, "", body, "/products", "Shop Now");
                await _emailService.SendEmailAsync(req.Email, subject, htmlEmail);
            }
            return Ok(new { message = "Subscribed successfully" });
        }

        [HttpGet("subscribers")]
        public async Task<IActionResult> GetSubscribers()
        {
            var subs = await _context.NewsletterSubscribers.OrderByDescending(s => s.SubscribedAt).ToListAsync();
            return Ok(subs);
        }

        public class BulkEmailRequest
        {
            public string Subject { get; set; }
            public string Body { get; set; }
            public System.Collections.Generic.List<string> Emails { get; set; }
        }

        [HttpPost("send-bulk")]
        public async Task<IActionResult> SendBulkEmail([FromBody] BulkEmailRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Subject) || string.IsNullOrWhiteSpace(req.Body))
            {
                return BadRequest(new { message = "Subject and body are required." });
            }

            System.Collections.Generic.List<string> emailsToSend;

            if (req.Emails != null && req.Emails.Count > 0)
            {
                emailsToSend = req.Emails;
            }
            else
            {
                emailsToSend = await _context.NewsletterSubscribers.Select(s => s.Email).ToListAsync();
            }
            
            foreach (var email in emailsToSend)
            {
                var htmlEmail = LeatherLane_Atelier.Services.EmailTemplateBuilder.BuildStandardEmail(req.Subject, "", req.Body, "/products", "Shop Now");
                await _emailService.SendEmailAsync(email, req.Subject, htmlEmail);
            }

            return Ok(new { message = $"Sent emails to {emailsToSend.Count} subscribers." });
        }
    }
}
