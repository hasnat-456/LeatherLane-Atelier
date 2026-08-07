using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace LeatherLane_Atelier.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string body);
    }

    public class EmailService : IEmailService
    {
        private readonly string _adminEmail = "muhammadbilalarifsheukh@gmail.com";
        private readonly string _smtpHost;
        private readonly int _smtpPort;
        private readonly string _smtpUser;
        private readonly string _smtpPass;

        public EmailService(IConfiguration configuration)
        {
            // You can configure these in appsettings.json, but defaulting to Gmail for now
            _smtpHost = configuration["Email:SmtpHost"] ?? "smtp.gmail.com";
            _smtpPort = int.TryParse(configuration["Email:SmtpPort"], out int port) ? port : 587;
            _smtpUser = configuration["Email:SmtpUser"] ?? _adminEmail;
            _smtpPass = configuration["Email:SmtpPass"] ?? "PLACEHOLDER_APP_PASSWORD"; // The user MUST fill this out
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            if (_smtpPass == "PLACEHOLDER_APP_PASSWORD")
            {
                // Fallback to console if password isn't set so it doesn't crash the server
                Console.WriteLine("-------------------------------------------------");
                Console.WriteLine($"[MOCK EMAIL] To: {toEmail}");
                Console.WriteLine($"[MOCK EMAIL] Subject: {subject}");
                Console.WriteLine($"[MOCK EMAIL] Body: {body}");
                Console.WriteLine("-------------------------------------------------");
                return;
            }

            try
            {
                using var client = new SmtpClient(_smtpHost, _smtpPort)
                {
                    Credentials = new NetworkCredential(_smtpUser, _smtpPass),
                    EnableSsl = true
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_smtpUser, "LeatherLane Atelier Notifications"),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };

                mailMessage.To.Add(toEmail);

                await client.SendMailAsync(mailMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send email to {toEmail}: {ex.Message}");
            }
        }
    }
}
