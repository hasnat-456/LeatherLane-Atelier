using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Net;
using System.Net.Mail;
using LeatherLane_Atelier.Models;

namespace LeatherLane_Atelier.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _config;

        public AuthController(ApplicationDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        private string GenerateToken(int userId, string role)
        {
            var secret = _config["JwtSettings:Secret"] ?? "super_secret_key_12345678901234567890";
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim("role", role)
            };

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.Now.AddDays(30),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string GenerateOTP()
        {
            return new Random().Next(100000, 999999).ToString();
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest req)
        {
            if (await _context.Users.AnyAsync(u => u.Email == req.Email))
            {
                return BadRequest(new { message = "User already exists" });
            }

            var user = new User
            {
                Name = req.Name,
                Email = req.Email,
                Phone = req.Phone,
                Password = BCrypt.Net.BCrypt.HashPassword(req.Password),
                IsVerified = true, // Automatically verified
                VerificationOTP = null,
                VerificationOTPExpires = null,
                Role = "User"
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Created("", new
            {
                _id = user.Id,
                name = user.Name,
                email = user.Email,
                phone = user.Phone,
                isVerified = user.IsVerified,
                token = GenerateToken(user.Id, user.Role),
                message = "Registration successful"
            });
        }

        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest req)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == req.Email && u.VerificationOTP == req.Otp && u.VerificationOTPExpires > DateTime.UtcNow);

            if (user == null)
            {
                return BadRequest(new { message = "Invalid or expired OTP" });
            }

            user.IsVerified = true;
            user.VerificationOTP = null;
            user.VerificationOTPExpires = null;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                _id = user.Id,
                name = user.Name,
                email = user.Email,
                isVerified = user.IsVerified,
                token = GenerateToken(user.Id, user.Role)
            });
        }

        [HttpPost("resend-otp")]
        public async Task<IActionResult> ResendOtp([FromBody] ResendOtpRequest req)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == req.Email);

            if (user == null)
                return NotFound(new { message = "User not found" });

            if (user.IsVerified)
                return BadRequest(new { message = "User already verified" });

            var otp = GenerateOTP();
            user.VerificationOTP = otp;
            user.VerificationOTPExpires = DateTime.UtcNow.AddMinutes(10);
            await _context.SaveChangesAsync();

            Console.WriteLine($"Resent OTP for {user.Email}: {otp}");

            return Ok(new { message = "OTP resent" });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
            {
                return Unauthorized(new { message = "Invalid email or password" });
            }

            var inputEmail = req.Email.Trim().ToLower();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == inputEmail);

            bool isPasswordValid = false;
            if (user != null)
            {
                try
                {
                    isPasswordValid = BCrypt.Net.BCrypt.Verify(req.Password, user.Password);
                }
                catch (Exception)
                {
                    // Fallback for older accounts that might have plaintext passwords stored
                    isPasswordValid = (req.Password == user.Password);
                    
                    // If it was valid plaintext, automatically upgrade them to a hash for next time
                    if (isPasswordValid)
                    {
                        user.Password = BCrypt.Net.BCrypt.HashPassword(req.Password);
                        await _context.SaveChangesAsync();
                    }
                }
            }

            if (user == null || !isPasswordValid)
            {
                return Unauthorized(new { message = "Invalid email or password" });
            }

            // Ensure Admin users are always verified
            if (user.Role == "Admin" && !user.IsVerified)
            {
                user.IsVerified = true;
                await _context.SaveChangesAsync();
            }

            if (!user.IsVerified)
            {
                var otp = GenerateOTP();
                user.VerificationOTP = otp;
                user.VerificationOTPExpires = DateTime.UtcNow.AddMinutes(10);
                await _context.SaveChangesAsync();
                
                Console.WriteLine($"OTP for {user.Email}: {otp}");

                return Ok(new
                {
                    _id = user.Id,
                    name = user.Name,
                    email = user.Email,
                    isVerified = user.IsVerified,
                    message = "OTP sent to email"
                });
            }

            return Ok(new
            {
                _id = user.Id,
                name = user.Name,
                email = user.Email,
                role = user.Role,
                token = GenerateToken(user.Id, user.Role)
            });
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest req)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == req.Email);
            
            if (user == null)
            {
                // Always return Ok to prevent email enumeration
                return Ok(new { message = "If an account with that email exists, a reset link has been sent." });
            }

            var token = Guid.NewGuid().ToString("N");
            user.ResetPasswordToken = token;
            user.ResetPasswordExpiry = DateTime.UtcNow.AddMinutes(15);
            await _context.SaveChangesAsync();

            var smtpHost = _config["Smtp:Host"];
            var smtpPort = int.TryParse(_config["Smtp:Port"], out int port) ? port : 587;
            var smtpEmail = _config["Smtp:Email"];
            var smtpPass = _config["Smtp:Password"];

            var resetLink = $"{Request.Scheme}://{Request.Host}/reset-password.html?token={token}";

            try 
            {
                using var client = new SmtpClient(smtpHost, smtpPort)
                {
                    Credentials = new NetworkCredential(smtpEmail, smtpPass),
                    EnableSsl = true
                };
                var mailMessage = new MailMessage
                {
                    From = new MailAddress(smtpEmail, "LeatherLane Atelier"),
                    Subject = "Reset Your Password",
                    Body = $"Hello {user.Name},\n\nPlease click the following link to reset your password. This link will expire in 15 minutes.\n\n{resetLink}\n\nIf you did not request this, please ignore this email.",
                    IsBodyHtml = false,
                };
                mailMessage.To.Add(user.Email);
                await client.SendMailAsync(mailMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error sending email: " + ex.Message);
                Console.WriteLine($"RESET LINK FOR {user.Email}: {resetLink}");
            }

            return Ok(new { message = "If an account with that email exists, a reset link has been sent." });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest req)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.ResetPasswordToken == req.Token);
            if (user == null)
            {
                Console.WriteLine($"Failed to find user with token: {req.Token}");
                return BadRequest(new { message = "Invalid reset token. It may have already been used." });
            }
            
            if (user.ResetPasswordExpiry < DateTime.UtcNow)
            {
                Console.WriteLine($"Token expired. Expiry: {user.ResetPasswordExpiry}, Now: {DateTime.UtcNow}");
                return BadRequest(new { message = "Your reset link has expired. Please request a new one." });
            }

            user.Password = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);
            user.ResetPasswordToken = null;
            user.ResetPasswordExpiry = null;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Password has been reset successfully." });
        }

        [HttpGet("profile")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> GetProfile()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                return Unauthorized(new { message = "Invalid token" });

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound(new { message = "User not found" });

            var nameParts = user.Name.Split(' ', 2);
            var firstName = nameParts.Length > 0 ? nameParts[0] : "";
            var lastName = nameParts.Length > 1 ? nameParts[1] : "";

            return Ok(new
            {
                firstName = firstName,
                lastName = lastName,
                email = user.Email,
                phone = user.Phone,
                receivesMarketingEmails = user.ReceivesMarketingEmails
            });
        }

        [HttpPut("profile")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest req)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                return Unauthorized(new { message = "Invalid token" });

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound(new { message = "User not found" });

            user.Name = $"{req.FirstName} {req.LastName}".Trim();
            if (!string.IsNullOrEmpty(req.Email)) {
                user.Email = req.Email;
            }
            user.Phone = req.Phone;
            user.ReceivesMarketingEmails = req.ReceivesMarketingEmails;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Profile updated successfully" });
        }

        [HttpPut("change-password")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest req)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                return Unauthorized(new { message = "Invalid token" });

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound(new { message = "User not found" });

            bool isCurrentValid = false;
            try
            {
                isCurrentValid = BCrypt.Net.BCrypt.Verify(req.CurrentPassword, user.Password);
            }
            catch (BCrypt.Net.SaltParseException)
            {
                isCurrentValid = (req.CurrentPassword == user.Password);
            }

            if (!isCurrentValid)
                return BadRequest(new { message = "Incorrect current password" });

            user.Password = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Password changed successfully" });
        }
    }

    public class RegisterRequest { public string Name { get; set; } = string.Empty; public string Email { get; set; } = string.Empty; public string Password { get; set; } = string.Empty; public string? Phone { get; set; } }
    public class LoginRequest { public string Email { get; set; } = string.Empty; public string Password { get; set; } = string.Empty; }
    public class VerifyOtpRequest { public string Email { get; set; } = string.Empty; public string Otp { get; set; } = string.Empty; }
    public class ResendOtpRequest { public string Email { get; set; } = string.Empty; }
    public class ForgotPasswordRequest { public string Email { get; set; } = string.Empty; }
    public class ResetPasswordRequest { public string Token { get; set; } = string.Empty; public string NewPassword { get; set; } = string.Empty; }
    public class UpdateProfileRequest { public string FirstName { get; set; } = string.Empty; public string LastName { get; set; } = string.Empty; public string Email { get; set; } = string.Empty; public string? Phone { get; set; } public bool ReceivesMarketingEmails { get; set; } }
    public class ChangePasswordRequest { public string CurrentPassword { get; set; } = string.Empty; public string NewPassword { get; set; } = string.Empty; }
}
