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
                
                // Send early subscription offer / welcome email
                string subject = "Welcome to LeatherLane Atelier - Exclusive Offers Inside!";
                string body = @"
                    <div style='font-family: Arial, sans-serif; color: #333; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #eaeaea; border-radius: 8px;'>
                        <h2 style='color: #470C0E; text-align: center;'>Welcome to the LeatherLane Family!</h2>
                        <p>Thank you for subscribing to our newsletter. You're now on the VIP list to receive our latest updates, early access to new collections, and exclusive subscriber-only offers.</p>
                        <div style='background-color: #F5EFE7; padding: 15px; border-radius: 6px; text-align: center; margin: 20px 0;'>
                            <h3 style='margin-top: 0; color: #C79A52;'>Your Early Subscription Offer</h3>
                            <p style='font-size: 1.1rem; font-weight: bold;'>Enjoy 10% off your first purchase!</p>
                            <p>Use code: <strong>WELCOME10</strong> at checkout.</p>
                        </div>
                        <p>We craft our leather goods with passion, precision, and the finest materials. We can't wait for you to experience our timeless craftsmanship.</p>
                        <br>
                        <p>Best regards,<br><strong>LeatherLane Atelier Team</strong></p>
                    </div>";
                await _emailService.SendEmailAsync(req.Email, subject, body);
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
                await _emailService.SendEmailAsync(email, req.Subject, req.Body);
            }

            return Ok(new { message = $"Sent emails to {emailsToSend.Count} subscribers." });
        }
    }
}
