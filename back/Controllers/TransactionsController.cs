using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;
using LeatherLane_Atelier.Models;

namespace LeatherLane_Atelier.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TransactionsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly LeatherLane_Atelier.Services.IEmailService _emailService;
        private readonly LeatherLane_Atelier.Services.IPayfastService _payfastService;
        private readonly LeatherLane_Atelier.Models.PayfastSettings _payfastSettings;

        public TransactionsController(
            ApplicationDbContext context, 
            IMemoryCache cache, 
            LeatherLane_Atelier.Services.IEmailService emailService,
            LeatherLane_Atelier.Services.IPayfastService payfastService,
            Microsoft.Extensions.Options.IOptions<LeatherLane_Atelier.Models.PayfastSettings> payfastSettings)
        {
            _context = context;
            _cache = cache;
            _emailService = emailService;
            _payfastService = payfastService;
            _payfastSettings = payfastSettings.Value;
        }

        private int GetUserId() 
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return claim != null ? int.Parse(claim) : 0; 
        }

        [HttpGet]
        public async Task<IActionResult> GetTransactions()
        {
            var userId = GetUserId();
            var transactions = await _context.Transactions
                .Include(t => t.Items)
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
            return Ok(transactions);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetTransaction(int id)
        {
            var userId = GetUserId();
            var transaction = await _context.Transactions
                .Include(t => t.Items)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (transaction != null && transaction.UserId == userId)
            {
                return Ok(transaction);
            }
            return NotFound(new { message = "Transaction not found" });
        }

        [HttpPost("initiate-payment")]
        public IActionResult InitiatePayment([FromBody] InitiatePaymentRequest req)
        {
            var userId = GetUserId();
            var user = _context.Users.FirstOrDefault(u => u.Id == userId);
            
            if (user == null)
            {
                return BadRequest(new { message = "User account not found. Cannot send OTP." });
            }

            var otp = new Random().Next(100000, 999999).ToString();
            
            // In a production environment, you would use an SMTP Client here:
            // using var smtp = new System.Net.Mail.SmtpClient("smtp.gmail.com", 587) { Credentials = ... };
            // smtp.Send("noreply@leatherlane.com", user.Email, "Payment Verification", $"Your OTP is: {otp}");

            // Log OTP to terminal simulating the email dispatch
            Console.WriteLine($"\n=======================================================");
            Console.WriteLine($"[EMAIL DISPATCH SIMULATOR]");
            Console.WriteLine($"To: {user.Email} (Registered Account Email)");
            Console.WriteLine($"Subject: LeatherLane Payment Verification");
            Console.WriteLine($"Body: Your secure payment OTP is: {otp}");
            Console.WriteLine($"=======================================================\n");

            // Cache options: expire after 10 minutes
            var cacheEntryOptions = new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(10));
            
            _cache.Set($"TempOTP_{userId}", otp, cacheEntryOptions);
            _cache.Set($"TempTransaction_{userId}", req, cacheEntryOptions);

            return Ok(new { message = $"OTP sent to your registered email ({user.Email})" });
        }

        [HttpPost("verify-payment")]
        public async Task<IActionResult> VerifyPayment([FromBody] VerifyPaymentRequest req)
        {
            var userId = GetUserId();

            if (!_cache.TryGetValue($"TempOTP_{userId}", out string? storedOtp) || storedOtp != req.Otp)
            {
                return BadRequest(new { message = "Invalid or expired OTP" });
            }

            if (!_cache.TryGetValue($"TempTransaction_{userId}", out InitiatePaymentRequest? tempTx) || tempTx == null)
            {
                return BadRequest(new { message = "No pending payment found" });
            }

            var transaction = new Transaction
            {
                UserId = userId,
                TotalAmount = tempTx.TotalAmount,
                ShippingName = tempTx.ShippingName,
                ShippingPhone = tempTx.ShippingPhone,
                ShippingAddress = tempTx.ShippingAddress,
                PaymentMethod = tempTx.PaymentMethod,
                PaymentPhoneNumber = tempTx.PaymentDetails?.PhoneNumber,
                PaymentCardLast4 = tempTx.PaymentDetails?.CardLast4,
                Status = "Order Placed"
            };

            foreach (var item in tempTx.Items)
            {
                transaction.Items.Add(new TransactionItem
                {
                    ProductId = item.ProductId,
                    Name = item.Name,
                    Price = item.Price,
                    Quantity = item.Quantity
                });

                if (item.ProductId.HasValue)
                {
                    var product = await _context.Products.FindAsync(item.ProductId.Value);
                    if (product != null)
                    {
                        if (product.AvailabilityStatus != "Available")
                        {
                            return BadRequest(new { message = $"Product {product.Name} is currently unavailable." });
                        }
                    }
                }
            }

            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync(); // Save to generate Transaction ID

            // Generate Timeline Event for Order Placed
            _context.TimelineEvents.Add(new TimelineEvent
            {
                ReferenceId = transaction.Id,
                Type = "Order",
                Status = "Order Placed",
                Description = "Your order has been successfully placed and is awaiting confirmation.",
                EventDateTime = DateTime.UtcNow,
                CreatedBy = "System",
                IsCurrent = true,
                IsCompleted = false
            });

            // Notification for Admin
            _context.Notifications.Add(new Notification
            {
                Title = "New Order Received",
                Message = $"Order #{transaction.Id} was just placed for ${transaction.TotalAmount}.",
                ActionUrl = "admin.html#orders",
                UserId = null // Admin
            });

            var userObj = await _context.Users.FindAsync(userId);
            if (userObj != null)
            {
                // In-app for customer
                _context.Notifications.Add(new Notification
                {
                    Title = "Order Placed Successfully",
                    Message = $"Your order #{transaction.Id} has been confirmed.",
                    ActionUrl = "transactions.html",
                    UserId = userId
                });

                // Emails
                _ = _emailService.SendEmailAsync("leatherlaneatelier@gmail.com", "New Order Received", $"Order #{transaction.Id} was placed.");
                _ = _emailService.SendEmailAsync(userObj.Email, "Order Confirmation", $"Your order #{transaction.Id} is confirmed.");
            }

            // Clear cart from DB
            var cartItems = await _context.CartItems.Where(c => c.UserId == userId).ToListAsync();
            if (cartItems.Any())
            {
                _context.CartItems.RemoveRange(cartItems);
            }

            await _context.SaveChangesAsync();

            // Clear cache
            _cache.Remove($"TempOTP_{userId}");
            _cache.Remove($"TempTransaction_{userId}");

            return CreatedAtAction(nameof(GetTransaction), new { id = transaction.Id }, transaction);
        }

        [HttpPut("{id}/cancel")]
        public async Task<IActionResult> CancelTransaction(int id, [FromBody] CancelOrderRequest req)
        {
            var userId = GetUserId();
            var transaction = await _context.Transactions
                .Include(t => t.Items)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (transaction == null || transaction.UserId != userId)
            {
                return NotFound(new { message = "Transaction not found" });
            }

            var allowedStatuses = new[] { "Order Placed", "Order Confirmed", "Preparing Order", "Packed", "Payment Verification Pending", "Payment Rejected" };
            if (!allowedStatuses.Contains(transaction.Status))
            {
                return BadRequest(new { message = "Order cannot be cancelled at this stage." });
            }

            transaction.Status = "Cancelled";
            transaction.UpdatedAt = DateTime.UtcNow;

            // Restock items
            foreach(var item in transaction.Items)
            {
                if (item.ProductId.HasValue)
                {
                    var product = await _context.Products.FindAsync(item.ProductId.Value);
                    if (product != null)
                    {
                        product.Stock += item.Quantity;
                        product.Available = true;
                    }
                }
            }

            // Update timeline
            var previousCurrent = await _context.TimelineEvents
                .Where(t => t.ReferenceId == id && t.Type == "Order" && t.IsCurrent)
                .ToListAsync();

            foreach (var prev in previousCurrent)
            {
                prev.IsCurrent = false;
                prev.IsCompleted = true;
            }

            _context.TimelineEvents.Add(new TimelineEvent
            {
                ReferenceId = transaction.Id,
                Type = "Order",
                Status = "Cancelled",
                Description = "The order was cancelled by the customer.",
                EventDateTime = DateTime.UtcNow,
                CreatedBy = "Customer",
                IsCurrent = true,
                IsCompleted = true
            });

            // Notify Admin
            _context.Notifications.Add(new Notification
            {
                Title = "Order Cancelled",
                Message = $"Customer cancelled Order #{transaction.Id}.",
                ActionUrl = "admin.html#orders",
                UserId = null // Admin
            });
            _ = _emailService.SendEmailAsync("leatherlaneatelier@gmail.com", "Order Cancelled", $"Order #{transaction.Id} was cancelled by the customer. Reason: {req.Reason}");

            // Notify Customer
            var userObj = await _context.Users.FindAsync(userId);
            if (userObj != null)
            {
                _context.Notifications.Add(new Notification
                {
                    Title = "Order Cancelled",
                    Message = $"Your order #{transaction.Id} has been successfully cancelled.",
                    ActionUrl = "transactions.html",
                    UserId = userId
                });
                _ = _emailService.SendEmailAsync(userObj.Email, "Order Cancelled", $"Your order #{transaction.Id} has been cancelled.");
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Order cancelled successfully." });
        }

        [HttpPost("payfast-initiate")]
        public IActionResult PayfastInitiate([FromBody] InitiatePaymentRequest req)
        {
            var userId = GetUserId();
            var user = _context.Users.FirstOrDefault(u => u.Id == userId);
            
            if (user == null)
            {
                return BadRequest(new { message = "User account not found." });
            }

            var transactionRef = Guid.NewGuid().ToString("N").Substring(0, 10);
            
            // Cache transaction for when notify callback arrives
            var cacheEntryOptions = new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromHours(2));
            _cache.Set($"PayfastTx_{transactionRef}", new { Request = req, UserId = userId }, cacheEntryOptions);

            var data = new Dictionary<string, string>
            {
                { "merchant_id", _payfastSettings.MerchantId },
                { "merchant_key", _payfastSettings.MerchantKey },
                { "return_url", _payfastSettings.ReturnUrl },
                { "cancel_url", _payfastSettings.CancelUrl },
                { "notify_url", _payfastSettings.NotifyUrl },
                { "name_first", user.Name },
                { "email_address", user.Email },
                { "m_payment_id", transactionRef },
                { "amount", req.TotalAmount.ToString("F2") },
                { "item_name", $"Order {transactionRef}" }
            };

            var signature = _payfastService.GenerateSignature(data);
            data.Add("signature", signature);
            
            return Ok(new 
            { 
                processUrl = _payfastSettings.ProcessUrl,
                paymentData = data 
            });
        }

        [AllowAnonymous]
        [HttpPost("payfast-notify")]
        public async Task<IActionResult> PayfastNotify([FromForm] Microsoft.AspNetCore.Http.IFormCollection form)
        {
            var data = new Dictionary<string, string>();
            foreach (var key in form.Keys)
            {
                data.Add(key, form[key].ToString());
            }

            if (!_payfastService.ValidateSignature(data))
            {
                return BadRequest("Invalid Signature");
            }

            if (data.TryGetValue("payment_status", out string? paymentStatus) && paymentStatus == "COMPLETE")
            {
                if (data.TryGetValue("m_payment_id", out string? transactionRef))
                {
                    // Look up cached order details
                    if (_cache.TryGetValue($"PayfastTx_{transactionRef}", out dynamic? txData) && txData != null)
                    {
                        var req = (InitiatePaymentRequest)txData.Request;
                        int userId = txData.UserId;

                        var transaction = new Transaction
                        {
                            UserId = userId,
                            TotalAmount = req.TotalAmount,
                            ShippingName = req.ShippingName,
                            ShippingPhone = req.ShippingPhone,
                            ShippingAddress = req.ShippingAddress,
                            PaymentMethod = "Payfast",
                            Status = "Order Placed"
                        };

                        foreach (var item in req.Items)
                        {
                            transaction.Items.Add(new TransactionItem
                            {
                                ProductId = item.ProductId,
                                Name = item.Name,
                                Price = item.Price,
                                Quantity = item.Quantity
                            });
                        }

                        _context.Transactions.Add(transaction);
                        await _context.SaveChangesAsync();
                        _cache.Remove($"PayfastTx_{transactionRef}");

                        // Add timeline/notification...
                        // (simplified for the snippet, normally same as VerifyPayment)
                    }
                }
            }

            return Ok();
        }

        [HttpGet("manual-payment-settings")]
        public async Task<IActionResult> GetManualPaymentSettings()
        {
            var settings = await _context.ManualPaymentSettings
                .Where(s => s.IsEnabled)
                .ToListAsync();
            return Ok(settings);
        }

        [HttpPost("upload-screenshot")]
        public async Task<IActionResult> UploadScreenshot(IFormFile imageFile)
        {
            if (imageFile == null || imageFile.Length == 0)
            {
                return BadRequest(new { message = "No file provided" });
            }

            var extension = Path.GetExtension(imageFile.FileName).ToLower();
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".pdf" };

            if (!allowedExtensions.Contains(extension))
            {
                return BadRequest(new { message = "Invalid file type. Only JPG, JPEG, PNG, and PDF are accepted." });
            }

            // Save file in wwwroot/uploads/payments/
            var currentDir = Directory.GetCurrentDirectory();
            var frontPath = currentDir.EndsWith("back", StringComparison.OrdinalIgnoreCase) 
                ? Path.Combine(currentDir, "..", "front", "uploads", "payments") 
                : Path.Combine(currentDir, "front", "uploads", "payments");

            Directory.CreateDirectory(frontPath);

            var uniqueFileName = Guid.NewGuid().ToString() + extension;
            var filePath = Path.Combine(frontPath, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(stream);
            }

            // Return relative URL for front-end to access
            var relativeUrl = "/uploads/payments/" + uniqueFileName;
            return Ok(new { url = relativeUrl });
        }

        [HttpPost("place-order")]
        public async Task<IActionResult> PlaceOrder([FromBody] PlaceOrderRequest req)
        {
            try
            {
                var userId = GetUserId();
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return BadRequest(new { message = "User not found." });
                }

                // Input Validation: Empty Checks
                if (string.IsNullOrWhiteSpace(req.ShippingName) ||
                    string.IsNullOrWhiteSpace(req.ShippingPhone) ||
                    string.IsNullOrWhiteSpace(req.ShippingAddress))
                {
                    return BadRequest(new { message = "All shipping fields are required." });
                }

                if (req.Items == null || !req.Items.Any())
                {
                    return BadRequest(new { message = "Cart is empty." });
                }

                // Price Integrity Check
                decimal calculatedSubtotal = 0;
                var transactionItems = new List<TransactionItem>();

                foreach (var item in req.Items)
                {
                    if (!item.ProductId.HasValue) 
                        return BadRequest(new { message = "Invalid product in cart." });

                    var product = await _context.Products.FindAsync(item.ProductId.Value);
                    if (product == null)
                    {
                        return BadRequest(new { message = $"Product not found." });
                    }
                    if (product.AvailabilityStatus != "Available")
                    {
                        return BadRequest(new { message = $"Product {product.Name} is currently unavailable." });
                    }

                    // Use the database price, ignore the frontend price
                    calculatedSubtotal += product.Price * item.Quantity;

                    transactionItems.Add(new TransactionItem
                    {
                        ProductId = product.Id,
                        Name = product.Name,
                        Price = product.Price,
                        Quantity = item.Quantity
                    });
                }

                // Tax calculation (5% based on frontend logic)
                decimal calculatedTax = calculatedSubtotal * 0.05m;
                decimal trueTotal = calculatedSubtotal + calculatedTax;

                var transaction = new Transaction
                {
                    UserId = userId,
                    TotalAmount = trueTotal, // Use server-calculated total
                    ShippingName = req.ShippingName,
                    ShippingPhone = req.ShippingPhone,
                    ShippingAddress = req.ShippingAddress,
                    PaymentMethod = req.PaymentMethod,
                    PaymentRefId = req.PaymentRefId,
                    SenderName = req.SenderName,
                    SenderMobile = req.SenderMobile,
                    PaymentScreenshot = req.PaymentScreenshot,
                    Status = "Payment Verification Pending",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                foreach (var tItem in transactionItems)
                {
                    transaction.Items.Add(tItem);
                }

                _context.Transactions.Add(transaction);
                await _context.SaveChangesAsync();

                // Notify Admin
                _context.Notifications.Add(new Notification
                {
                    Title = "New Payment Verification Pending",
                    Message = $"Order #{transaction.Id} was placed for Rs. {transaction.TotalAmount} and is awaiting payment verification.",
                    ActionUrl = "admin.html#payment-verification",
                    UserId = null // Admin
                });

                // Notify Customer
                _context.Notifications.Add(new Notification
                {
                    Title = "Payment Verification Pending",
                    Message = $"Your payment proof for Order #{transaction.Id} has been received and is awaiting verification.",
                    ActionUrl = $"order-tracking.html?id={transaction.Id}",
                    UserId = userId
                });

                // Send Emails
                _ = _emailService.SendEmailAsync("leatherlaneatelier@gmail.com", 
                    "New Payment Verification Pending", 
                    $"Order #{transaction.Id} requires payment verification. Transaction ID: {transaction.PaymentRefId}. Amount: Rs. {transaction.TotalAmount}.");

                _ = _emailService.SendEmailAsync(user.Email, 
                    "Order Placed - Awaiting Verification", 
                    $"Your payment proof has been received and is awaiting verification. Once verified, your order #{transaction.Id} will automatically move to Order Confirmed.");

                // Clear Cart
                var cartItems = await _context.CartItems.Where(c => c.UserId == userId).ToListAsync();
                if (cartItems.Any())
                {
                    _context.CartItems.RemoveRange(cartItems);
                }

                await _context.SaveChangesAsync();

                return Ok(new { message = "Order submitted successfully.", orderId = transaction.Id });
            }
            catch (Exception ex)
            {
                // Log exception here in a real scenario
                Console.WriteLine($"[Error] PlaceOrder failed: {ex.Message}");
                return StatusCode(500, new { message = "An internal server error occurred while processing your order. Please try again." });
            }
        }

        [HttpPost("{id}/resubmit-payment")]
        public async Task<IActionResult> ResubmitPayment(int id, [FromBody] ResubmitPaymentRequest req)
        {
            var userId = GetUserId();
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return BadRequest(new { message = "User not found." });

            var transaction = await _context.Transactions
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (transaction == null)
            {
                return NotFound(new { message = "Order not found." });
            }

            if (transaction.Status != "Payment Rejected")
            {
                return BadRequest(new { message = "Payment can only be resubmitted for rejected orders." });
            }

            // Update details
            transaction.PaymentRefId = req.PaymentRefId;
            transaction.SenderName = req.SenderName;
            transaction.SenderMobile = req.SenderMobile;
            transaction.PaymentScreenshot = req.PaymentScreenshot;
            transaction.Status = "Payment Verification Pending";
            transaction.RejectionReason = null; // Clear rejection reason
            transaction.UpdatedAt = DateTime.UtcNow;

            // Notify Admin
            _context.Notifications.Add(new Notification
            {
                Title = "Resubmitted Payment Proof",
                Message = $"Customer resubmitted payment proof for Order #{transaction.Id}.",
                ActionUrl = "admin.html#payment-verification",
                UserId = null // Admin
            });

            // Notify Customer
            _context.Notifications.Add(new Notification
            {
                Title = "Payment Verification Pending",
                Message = $"Your updated payment proof for Order #{transaction.Id} is awaiting verification.",
                ActionUrl = $"order-tracking.html?id={transaction.Id}",
                UserId = userId
            });

            // Emails
            _ = _emailService.SendEmailAsync("leatherlaneatelier@gmail.com", 
                "Resubmitted Payment Proof", 
                $"Order #{transaction.Id} payment proof was resubmitted. Reference ID: {transaction.PaymentRefId}.");

            _ = _emailService.SendEmailAsync(user.Email, 
                "Payment Proof Resubmitted", 
                $"Your updated payment proof for Order #{transaction.Id} was received and is awaiting verification.");

            await _context.SaveChangesAsync();

            return Ok(new { message = "Payment proof resubmitted successfully." });
        }
    }

    public class CancelOrderRequest
    {
        public string Reason { get; set; } = string.Empty;
    }

    public class InitiatePaymentRequest 
    { 
        public List<CartItemRequest> Items { get; set; } = new();
        public decimal TotalAmount { get; set; } 
        
        public string? ShippingName { get; set; }
        public string? ShippingPhone { get; set; }
        public string? ShippingAddress { get; set; }

        public string PaymentMethod { get; set; } = string.Empty; 
        public PaymentDetailsRequest? PaymentDetails { get; set; }
    }

    public class PaymentDetailsRequest
    {
        public string? PhoneNumber { get; set; }
        public string? CardLast4 { get; set; }
    }

    public class CartItemRequest
    {
        public int? ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
    }

    public class VerifyPaymentRequest 
    { 
        public string Otp { get; set; } = string.Empty; 
    }

    public class PlaceOrderRequest
    {
        public List<CartItemRequest> Items { get; set; } = new();
        public decimal TotalAmount { get; set; } 
        
        public string ShippingName { get; set; } = string.Empty;
        public string ShippingPhone { get; set; } = string.Empty;
        public string ShippingAddress { get; set; } = string.Empty;

        public string PaymentMethod { get; set; } = string.Empty;
        public string PaymentRefId { get; set; } = string.Empty;
        public string SenderName { get; set; } = string.Empty;
        public string SenderMobile { get; set; } = string.Empty;
        public string PaymentScreenshot { get; set; } = string.Empty;
    }

    public class ResubmitPaymentRequest
    {
        public string PaymentRefId { get; set; } = string.Empty;
        public string SenderName { get; set; } = string.Empty;
        public string SenderMobile { get; set; } = string.Empty;
        public string PaymentScreenshot { get; set; } = string.Empty;
    }
}
