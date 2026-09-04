using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LeatherLane_Atelier.Models;

namespace LeatherLane_Atelier.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public AdminApiController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // We bypass actual [Authorize(Roles="Admin")] for now to match the frontend mock auth flow
        // The frontend admin uses its own localStorage token for simplicity.

        [HttpPut("settings")]
        public async Task<IActionResult> UpdateSettings([FromBody] UpdateAdminSettingsRequest req)
        {
            var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.Role == "Admin");
            if (adminUser == null) return NotFound(new { message = "Admin not found" });

            if (!string.IsNullOrEmpty(req.Email))
            {
                adminUser.Email = req.Email;
            }

            if (!string.IsNullOrEmpty(req.NewPassword) && req.NewPassword.Length >= 6)
            {
                adminUser.Password = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Settings updated successfully" });
        }

        [HttpPost("site-image")]
        public async Task<IActionResult> UploadSiteImage(IFormFile imageFile, [FromForm] string target)
        {
            if (imageFile == null || imageFile.Length == 0) return BadRequest("No image provided");

            var currentDir = Directory.GetCurrentDirectory();
            var frontPath = currentDir.EndsWith("back", StringComparison.OrdinalIgnoreCase) 
                ? Path.Combine(currentDir, "..", "front", "images", "site") 
                : Path.Combine(currentDir, "front", "images", "site");

            Directory.CreateDirectory(frontPath);
            
            // Only allow specific targets for security
            if (target != "about-image.jpg") return BadRequest("Invalid target");

            var filePath = Path.Combine(frontPath, target);
            
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(stream);
            }

            return Ok(new { message = "Image uploaded successfully", url = $"/images/site/{target}?t={DateTime.UtcNow.Ticks}" });
        }

        [HttpPost("slider-image")]
        public async Task<IActionResult> UploadSliderImage(IFormFile imageFile)
        {
            if (imageFile == null || imageFile.Length == 0) return BadRequest("No image provided");

            var currentDir = Directory.GetCurrentDirectory();
            var frontPath = currentDir.EndsWith("back", StringComparison.OrdinalIgnoreCase) 
                ? Path.Combine(currentDir, "..", "front", "upload", "sliders") 
                : Path.Combine(currentDir, "front", "upload", "sliders");

            Directory.CreateDirectory(frontPath);
            
            var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
            var filePath = Path.Combine(frontPath, uniqueFileName);
            
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(stream);
            }

            return Ok(new { message = "Image uploaded successfully", url = "upload/sliders/" + uniqueFileName });
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var totalOrders = await _context.Transactions.CountAsync();
            var totalRevenue = await _context.Transactions.Where(t => t.Status != "Cancelled").SumAsync(t => t.TotalAmount);

            return Ok(new { totalOrders, totalRevenue });
        }

        [HttpGet("orders")]
        public async Task<IActionResult> GetOrders()
        {
            var transactions = await _context.Transactions.AsNoTracking()
                .Include(t => t.Items)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            var productIds = transactions.SelectMany(t => t.Items).Where(i => i.ProductId.HasValue).Select(i => i.ProductId!.Value).Distinct().ToList();
            var productsDict = await _context.Products.AsNoTracking()
                .Where(p => productIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id);

            var orders = transactions.Select(t => new {
                id = t.Id,
                orderId = t.OrderId ?? LeatherLane_Atelier.Services.IdGenerator.GenerateOrderId(t.CreatedAt),
                orderNumber = t.OrderId ?? LeatherLane_Atelier.Services.IdGenerator.GenerateOrderId(t.CreatedAt),
                date = t.CreatedAt,
                customer = t.ShippingName ?? $"User {t.UserId}",
                amount = t.TotalAmount,
                status = t.Status,
                items = t.Items.Select(i => {
                    var prod = i.ProductId.HasValue && productsDict.ContainsKey(i.ProductId.Value) ? productsDict[i.ProductId.Value] : null;
                    return new {
                        id = i.Id,
                        productId = prod != null ? (prod.ProductId ?? LeatherLane_Atelier.Services.IdGenerator.GenerateProductId(prod.Category)) : (i.ProductId.HasValue ? $"PRD-{i.ProductId.Value}" : "N/A"),
                        name = !string.IsNullOrEmpty(i.Name) ? i.Name : (prod != null ? prod.Name : "Product"),
                        quantity = i.Quantity,
                        price = i.Price,
                        thumbnail = prod != null ? prod.Thumbnail : null
                    };
                }).ToList()
            }).ToList();

            return Ok(orders);
        }

        [HttpPut("orders/{id}/status")]
        public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] UpdateStatusRequest req)
        {
            var transaction = await _context.Transactions
                .Include(t => t.Items)
                .FirstOrDefaultAsync(t => t.Id == id);
            if (transaction == null) return NotFound();

            if (transaction.Status == "Cancelled")
            {
                return BadRequest(new { message = "Cannot change the status of a cancelled order." });
            }

            if (req.Status == "Cancelled")
            {
                return BadRequest(new { message = "Admins are not permitted to manually cancel orders." });
            }

            if (transaction.Status == "Payment Verification Pending")
            {
                return BadRequest(new { message = "Cannot manually change the status. Please verify or reject the payment in the Payment Verification tab first." });
            }

            transaction.Status = req.Status;

            // Update timeline
            var previousCurrent = await _context.TimelineEvents
                .Where(t => t.ReferenceId == id && t.Type == "Order" && t.IsCurrent)
                .ToListAsync();

            foreach (var prev in previousCurrent)
            {
                prev.IsCurrent = false;
                prev.IsCompleted = true;
            }

            string desc = req.Status switch {
                "Cancelled" => $"The order was cancelled by Admin. Reason: {req.CancelReason}",
                "Order Confirmed" => "Your order has been verified and confirmed.",
                "Preparing Order" => "We are carefully crafting and preparing your items.",
                "Packed" => "Your order is packed and ready for dispatch.",
                "Handed to Courier" => "Your package has been handed over to our delivery partner.",
                "In Transit" => "Your package is on its way to your city.",
                "Out for Delivery" => "Our rider is out for delivery today.",
                "Delivered" => "The package was successfully delivered.",
                _ => "Status updated to " + req.Status
            };

            var newEvent = new TimelineEvent
            {
                ReferenceId = id,
                Type = "Order",
                Status = req.Status,
                Description = desc,
                EventDateTime = DateTime.UtcNow,
                CreatedBy = "Admin",
                IsCurrent = true,
                IsCompleted = req.Status == "Delivered" || req.Status == "Cancelled"
            };

            _context.TimelineEvents.Add(newEvent);

            // Customer notification and luxury email
            var customer = await _context.Users.FindAsync(transaction.UserId);
            if (customer != null)
            {
                var notifTitle = "Order Status Updated";
                var notifMsg = $"Your order {transaction.OrderId} status is now: {req.Status}.";
                
                if (req.Status == "Delivered")
                {
                    notifTitle = "Order Delivered";
                    notifMsg = $"Your order {transaction.OrderId} has been delivered successfully. We hope your bespoke leather pieces serve you with timeless elegance.";
                    transaction.DeliveredAt = DateTime.UtcNow;
                    transaction.ReviewReminderDay2Sent = false;
                    transaction.ReviewReminderDay4Sent = false;
                }
                else if (req.Status == "Handed to Courier" || req.Status == "Shipped" || req.Status == "In Transit")
                {
                    notifTitle = "Order Dispatched";
                    notifMsg = $"Your order {transaction.OrderId} has been handed to the courier and is on its way to your destination.";
                }
                else if (req.Status == "Preparing Order")
                {
                    notifTitle = "Preparing Order";
                    notifMsg = $"Your order {transaction.OrderId} is currently being handcrafted, polished, and packaged in our luxury atelier box.";
                }
                else if (req.Status == "Out for Delivery")
                {
                    notifTitle = "Out for Delivery";
                    notifMsg = $"Your LeatherLane Atelier parcel for Order {transaction.OrderId} is out for delivery today with our courier rider.";
                }

                _context.Notifications.Add(new Notification
                {
                    Title = notifTitle,
                    Message = notifMsg,
                    ActionUrl = $"order-tracking.html?id={transaction.OrderId}",
                    UserId = customer.Id
                });

                var emailSvc = HttpContext.RequestServices.GetService(typeof(LeatherLane_Atelier.Services.IEmailService)) as LeatherLane_Atelier.Services.IEmailService;
                if (emailSvc != null)
                {
                    var statusItemInfos = new List<LeatherLane_Atelier.Services.OrderItemInfo>();
                    foreach (var tItem in transaction.Items)
                    {
                        var prod = tItem.ProductId.HasValue ? await _context.Products.FindAsync(tItem.ProductId.Value) : null;
                        statusItemInfos.Add(new LeatherLane_Atelier.Services.OrderItemInfo
                        {
                            Name = tItem.Name,
                            ProductId = prod?.ProductId ?? ("LLA-PRD-" + tItem.ProductId),
                            Thumbnail = prod?.Image,
                            Quantity = tItem.Quantity,
                            Price = tItem.Price
                        });
                    }

                    string emailOpeningText = req.Status switch
                    {
                        "Preparing Order" => $"Your order <strong>{transaction.OrderId}</strong> is currently being handcrafted, polished, and packaged in our luxury atelier box.",
                        "Packed" => $"Your order <strong>{transaction.OrderId}</strong> has been carefully inspected and securely boxed for dispatch.",
                        "Handed to Courier" or "Shipped" => $"Your bespoke order <strong>{transaction.OrderId}</strong> has been handed over to our delivery courier partner and is en route to your shipping address.",
                        "In Transit" => $"Your bespoke order <strong>{transaction.OrderId}</strong> is currently in transit to your destination city.",
                        "Out for Delivery" => $"Great news! Your LeatherLane Atelier parcel for Order <strong>{transaction.OrderId}</strong> is out for doorstep delivery today with our courier rider.",
                        "Delivered" => $"Your order <strong>{transaction.OrderId}</strong> has been delivered. We hope your bespoke leather pieces serve you with timeless elegance and comfort.",
                        _ => $"Your order <strong>{transaction.OrderId}</strong> status has been updated to: <strong>{req.Status}</strong>."
                    };

                    string actionButtonText = req.Status == "Delivered" ? "Leave a Product Review" : "Track Your Order";
                    string actionBtnUrl = req.Status == "Delivered" ? $"/orders.html" : $"/order-tracking.html?id={transaction.OrderId}";

                    var emailHtml = LeatherLane_Atelier.Services.EmailTemplateBuilder.BuildOrderEmail(
                        notifTitle,
                        customer.Name,
                        transaction.OrderId,
                        emailOpeningText,
                        statusItemInfos,
                        0m,
                        transaction.TotalAmount,
                        req.Status,
                        transaction.PaymentMethod,
                        actionBtnUrl,
                        actionButtonText
                    );
                    _ = emailSvc.SendEmailAsync(customer.Email, $"Order {transaction.OrderId} - {req.Status} | LeatherLane Atelier", emailHtml);
                }
            }
            // ---> END INJECTED LOGIC <---


            await _context.SaveChangesAsync();

            return Ok(new { message = "Status updated successfully" });
        }

        [HttpDelete("products/{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound(new { message = "Product not found." });

            var reasons = new List<string>();

            // Check 1: In customer favorites / wishlist
            var favoritesCount = await _context.Favorites.CountAsync(f => f.ProductId == id);
            if (favoritesCount > 0)
            {
                reasons.Add($"This product is currently saved in {favoritesCount} customer favorite/wishlist item(s).");
            }

            // Check 2: Active orders in processing
            var activeOrders = await _context.Transactions
                .Where(t => t.Items.Any(i => i.ProductId == id))
                .Where(t => t.Status != "Delivered" && t.Status != "Cancelled" && t.Status != "Completed" && t.Status != "Payment Rejected")
                .Select(t => t.OrderId ?? ("#" + t.Id))
                .Distinct()
                .ToListAsync();

            var activeExchanges = await _context.ExchangeRequests
                .Where(e => (e.OriginalProductId == id || e.ReplacementProductId == id) && e.Status != "Completed" && e.Status != "Rejected")
                .Select(e => e.ExchangeCode ?? ("EXC-" + e.ExchangeId))
                .Distinct()
                .ToListAsync();

            var activeReturns = await _context.ReturnRequests
                .Where(r => r.ProductId == id && r.Status != "Approved" && r.Status != "Rejected" && r.Status != "Completed")
                .Select(r => r.ReturnCode ?? ("RET-" + r.ReturnId))
                .Distinct()
                .ToListAsync();

            var activeOrderDetails = new List<string>();
            if (activeOrders.Any())
            {
                activeOrderDetails.Add($"Order(s): {string.Join(", ", activeOrders)}");
            }
            if (activeExchanges.Any())
            {
                activeOrderDetails.Add($"Exchange(s): {string.Join(", ", activeExchanges)}");
            }
            if (activeReturns.Any())
            {
                activeOrderDetails.Add($"Return(s): {string.Join(", ", activeReturns)}");
            }

            if (activeOrderDetails.Any())
            {
                reasons.Add($"This product is currently part of active order/exchange/return in processing ({string.Join("; ", activeOrderDetails)}).");
            }

            // If any blocking reasons exist, reject deletion with clear reasons
            if (reasons.Any())
            {
                string formattedReason;
                if (reasons.Count == 1)
                {
                    formattedReason = $"Cannot delete product '{product.Name}':\n• {reasons[0]}";
                }
                else
                {
                    formattedReason = $"Cannot delete product '{product.Name}' due to the following reasons:\n1. {reasons[0]}\n2. {reasons[1]}";
                }

                return BadRequest(new { message = formattedReason });
            }

            // If safe to delete (no favorites, no active processing orders):
            try
            {
                // 1. Remove Cart Items
                var cartItems = await _context.CartItems.Where(c => c.ProductId == id).ToListAsync();
                if (cartItems.Any()) _context.CartItems.RemoveRange(cartItems);

                // 2. Remove Deals
                var deals = await _context.Deals.Where(d => d.ProductId == id).ToListAsync();
                if (deals.Any()) _context.Deals.RemoveRange(deals);

                // 3. Remove Reviews
                var reviews = await _context.Reviews.Where(r => r.ProductId == id).ToListAsync();
                if (reviews.Any()) _context.Reviews.RemoveRange(reviews);

                // 4. Remove Favorites (if any)
                var favs = await _context.Favorites.Where(f => f.ProductId == id).ToListAsync();
                if (favs.Any()) _context.Favorites.RemoveRange(favs);

                // 5. Clean up completed/closed Exchanges referencing this product
                var completedExchanges = await _context.ExchangeRequests
                    .Where(e => e.OriginalProductId == id || e.ReplacementProductId == id)
                    .ToListAsync();
                if (completedExchanges.Any())
                {
                    var exchIds = completedExchanges.Select(e => e.ExchangeId).ToList();
                    var exchImages = await _context.ExchangeImages.Where(ei => exchIds.Contains(ei.ExchangeId)).ToListAsync();
                    var exchHist = await _context.ExchangeStatusHistory.Where(eh => exchIds.Contains(eh.ExchangeId)).ToListAsync();
                    
                    _context.ExchangeImages.RemoveRange(exchImages);
                    _context.ExchangeStatusHistory.RemoveRange(exchHist);
                    _context.ExchangeRequests.RemoveRange(completedExchanges);
                }

                // 6. Clean up completed/closed Returns referencing this product
                var completedReturns = await _context.ReturnRequests.Where(r => r.ProductId == id).ToListAsync();
                if (completedReturns.Any())
                {
                    var retIds = completedReturns.Select(r => r.ReturnId).ToList();
                    var retImages = await _context.ReturnImages.Where(ri => retIds.Contains(ri.ReturnId)).ToListAsync();
                    _context.ReturnImages.RemoveRange(retImages);
                    _context.ReturnRequests.RemoveRange(completedReturns);
                }

                // 7. For TransactionItems, detach product reference
                var pastItems = await _context.Set<TransactionItem>().Where(ti => ti.ProductId == id).ToListAsync();
                foreach (var pi in pastItems)
                {
                    pi.ProductId = null;
                }

                // 8. Remove Product
                _context.Products.Remove(product);

                await _context.SaveChangesAsync();
                return Ok(new { message = $"Product '{product.Name}' deleted successfully." });
            }
            catch (Exception)
            {
                // Direct Raw SQL fallback if EF constraint execution encounters conflict
                try
                {
                    await _context.Database.ExecuteSqlInterpolatedAsync($@"
                        DELETE FROM [dbo].[CartItems] WHERE [ProductId] = {id};
                        DELETE FROM [dbo].[Deals] WHERE [ProductId] = {id};
                        DELETE FROM [dbo].[Reviews] WHERE [ProductId] = {id};
                        DELETE FROM [dbo].[Favorites] WHERE [ProductId] = {id};
                        
                        DELETE FROM [dbo].[ExchangeImages] WHERE [ExchangeId] IN (SELECT [ExchangeId] FROM [dbo].[ExchangeRequests] WHERE [OriginalProductId] = {id} OR [ReplacementProductId] = {id});
                        DELETE FROM [dbo].[ExchangeStatusHistories] WHERE [ExchangeId] IN (SELECT [ExchangeId] FROM [dbo].[ExchangeRequests] WHERE [OriginalProductId] = {id} OR [ReplacementProductId] = {id});
                        DELETE FROM [dbo].[ExchangeRequests] WHERE [OriginalProductId] = {id} OR [ReplacementProductId] = {id};

                        DELETE FROM [dbo].[ReturnImages] WHERE [ReturnId] IN (SELECT [ReturnId] FROM [dbo].[ReturnRequests] WHERE [ProductId] = {id});
                        DELETE FROM [dbo].[ReturnRequests] WHERE [ProductId] = {id};

                        UPDATE [dbo].[TransactionItems] SET [ProductId] = NULL WHERE [ProductId] = {id};
                        DELETE FROM [dbo].[Products] WHERE [Id] = {id};
                    ");

                    return Ok(new { message = $"Product '{product.Name}' deleted successfully." });
                }
                catch (Exception rawEx)
                {
                    var innerMsg = rawEx.InnerException?.InnerException?.Message 
                                   ?? rawEx.InnerException?.Message 
                                   ?? rawEx.Message;
                    return StatusCode(500, new { message = $"Error deleting product: {innerMsg}" });
                }
            }
        }

        [HttpGet("pending-payments")]
        public async Task<IActionResult> GetPendingPayments()
        {
            var payments = await _context.Transactions
                .Where(t => t.PaymentScreenshot != null && t.PaymentScreenshot != "")
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new {
                    id = t.Id,
                    orderId = t.OrderId,
                    orderNumber = t.OrderId,
                    date = t.CreatedAt,
                    customer = t.ShippingName ?? $"User {t.UserId}",
                    amount = t.TotalAmount,
                    status = t.Status,
                    paymentMethod = t.PaymentMethod,
                    paymentRefId = t.PaymentRefId,
                    senderName = t.SenderName,
                    senderMobile = t.SenderMobile,
                    paymentScreenshot = t.PaymentScreenshot,
                    rejectionReason = t.RejectionReason
                })
                .ToListAsync();

            return Ok(payments);
        }

        [HttpPut("orders/{id}/verify-payment")]
        public async Task<IActionResult> VerifyPayment(int id)
        {
            var transaction = await _context.Transactions
                .Include(t => t.Items)
                .FirstOrDefaultAsync(t => t.Id == id);
            if (transaction == null) return NotFound(new { message = "Order not found" });

            if (transaction.Status != "Payment Verification Pending")
            {
                return BadRequest(new { message = "Only pending payments can be verified." });
            }

            transaction.Status = "Order Confirmed";
            transaction.UpdatedAt = DateTime.UtcNow;

            var previousCurrent = await _context.TimelineEvents
                .Where(t => t.ReferenceId == id && t.Type == "Order" && t.IsCurrent)
                .ToListAsync();

            foreach (var prev in previousCurrent)
            {
                prev.IsCurrent = false;
                prev.IsCompleted = true;
            }

            var newEvent = new TimelineEvent
            {
                ReferenceId = id,
                Type = "Order",
                Status = "Order Confirmed",
                Description = "Your payment has been verified. Your order is now confirmed and will begin processing.",
                EventDateTime = DateTime.UtcNow,
                CreatedBy = "Admin",
                IsCurrent = true,
                IsCompleted = false
            };
            _context.TimelineEvents.Add(newEvent);

            var customer = await _context.Users.FindAsync(transaction.UserId);
            if (customer != null)
            {
                _context.Notifications.Add(new Notification
                {
                    Title = "Payment Verified",
                    Message = $"Your payment for Order {transaction.OrderId} has been verified. Your order is now confirmed.",
                    ActionUrl = $"transactions.html",
                    UserId = customer.Id
                });

                var emailSvc = HttpContext.RequestServices.GetService(typeof(LeatherLane_Atelier.Services.IEmailService)) as LeatherLane_Atelier.Services.IEmailService;
                if (emailSvc != null)
                {
                    var verifyItemInfos = new List<LeatherLane_Atelier.Services.OrderItemInfo>();
                    foreach (var tItem in transaction.Items)
                    {
                        var prod = tItem.ProductId.HasValue ? await _context.Products.FindAsync(tItem.ProductId.Value) : null;
                        verifyItemInfos.Add(new LeatherLane_Atelier.Services.OrderItemInfo
                        {
                            Name = tItem.Name,
                            ProductId = prod?.ProductId ?? ("LLA-PRD-" + tItem.ProductId),
                            Thumbnail = prod?.Image,
                            Quantity = tItem.Quantity,
                            Price = tItem.Price
                        });
                    }

                    string confirmMsg = $"We are delighted to confirm that your payment for Order <strong>{transaction.OrderId}</strong> has been successfully verified. Our atelier workshop has commenced processing your order.";
                    var emailHtml = LeatherLane_Atelier.Services.EmailTemplateBuilder.BuildOrderEmail(
                        "Payment Verified & Order Confirmed",
                        customer.Name,
                        transaction.OrderId,
                        confirmMsg,
                        verifyItemInfos,
                        0m,
                        transaction.TotalAmount,
                        "Order Confirmed",
                        transaction.PaymentMethod,
                        $"/order-tracking.html?id={transaction.OrderId}",
                        "Track Order Progress"
                    );
                    _ = emailSvc.SendEmailAsync(customer.Email, $"Payment Verified: Order {transaction.OrderId} Confirmed | LeatherLane Atelier", emailHtml);
                }
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Payment verified successfully" });
        }

        [HttpPut("orders/{id}/reject-payment")]
        public async Task<IActionResult> RejectPayment(int id, [FromBody] RejectPaymentRequest req)
        {
            var transaction = await _context.Transactions
                .Include(t => t.Items)
                .FirstOrDefaultAsync(t => t.Id == id);
            if (transaction == null) return NotFound(new { message = "Order not found" });

            if (transaction.Status != "Payment Verification Pending")
            {
                return BadRequest(new { message = "Only pending payments can be rejected." });
            }

            transaction.Status = "Payment Rejected";
            transaction.RejectionReason = string.IsNullOrEmpty(req.RejectionReason) ? "Invalid transaction details" : req.RejectionReason;
            transaction.UpdatedAt = DateTime.UtcNow;

            var customer = await _context.Users.FindAsync(transaction.UserId);
            if (customer != null)
            {
                _context.Notifications.Add(new Notification
                {
                    Title = "Payment Rejection Alert",
                    Message = $"Your payment for Order {transaction.OrderId} could not be verified. Reason: {transaction.RejectionReason}. Please upload a new payment proof.",
                    ActionUrl = $"order-tracking.html?id={transaction.OrderId}",
                    UserId = customer.Id
                });

                var emailSvc = HttpContext.RequestServices.GetService(typeof(LeatherLane_Atelier.Services.IEmailService)) as LeatherLane_Atelier.Services.IEmailService;
                if (emailSvc != null)
                {
                    var rejectItemInfos = new List<LeatherLane_Atelier.Services.OrderItemInfo>();
                    foreach (var tItem in transaction.Items)
                    {
                        var prod = tItem.ProductId.HasValue ? await _context.Products.FindAsync(tItem.ProductId.Value) : null;
                        rejectItemInfos.Add(new LeatherLane_Atelier.Services.OrderItemInfo
                        {
                            Name = tItem.Name,
                            ProductId = prod?.ProductId ?? ("LLA-PRD-" + tItem.ProductId),
                            Thumbnail = prod?.Image,
                            Quantity = tItem.Quantity,
                            Price = tItem.Price
                        });
                    }

                    string rejectMsg = $"We were unable to verify your payment proof for Order <strong>{transaction.OrderId}</strong>. Please review the reason below and submit a clear payment receipt.";
                    var emailHtml = LeatherLane_Atelier.Services.EmailTemplateBuilder.BuildOrderEmail(
                        "Payment Proof Rejected",
                        customer.Name,
                        transaction.OrderId,
                        rejectMsg,
                        rejectItemInfos,
                        0m,
                        transaction.TotalAmount,
                        "Payment Rejected",
                        transaction.PaymentMethod,
                        $"/order-tracking.html?id={transaction.OrderId}",
                        "Re-upload Payment Proof",
                        null,
                        null,
                        transaction.RejectionReason
                    );
                    _ = emailSvc.SendEmailAsync(customer.Email, $"Payment Proof Update - Order {transaction.OrderId} | LeatherLane Atelier", emailHtml);
                }
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Payment rejected successfully" });
        }

        [HttpGet("manual-payment-settings")]
        public async Task<IActionResult> GetManualPaymentSettings()
        {
            var settings = await _context.ManualPaymentSettings.ToListAsync();
            return Ok(settings);
        }

        [HttpPut("manual-payment-settings")]
        public async Task<IActionResult> UpdateManualPaymentSettings([FromBody] List<ManualPaymentSetting> settings)
        {
            foreach (var s in settings)
            {
                var existing = await _context.ManualPaymentSettings.FindAsync(s.Id);
                if (existing != null)
                {
                    existing.IsEnabled = s.IsEnabled;
                    existing.BankName = s.BankName;
                    existing.AccountTitle = s.AccountTitle;
                    existing.AccountNumber = s.AccountNumber;
                    existing.IBAN = s.IBAN;
                    existing.MobileNumber = s.MobileNumber;
                    existing.RaastId = s.RaastId;
                }
            }
            await _context.SaveChangesAsync();
            return Ok(new { message = "Manual payment settings updated successfully." });
        }

        
        [HttpPost("broadcast")]
        public async Task<IActionResult> SendBroadcast([FromBody] BroadcastRequest req)
        {
            var users = await _context.Users.ToListAsync();
            
            if (req.SendInApp)
            {
                var now = DateTime.UtcNow;
                var notifs = users.Select(u => new Notification
                {
                    Title = req.Title,
                    Message = req.Message,
                    ActionUrl = req.ActionUrl,
                    UserId = u.Id
                }).ToList();
                
                _context.Notifications.AddRange(notifs);
                await _context.SaveChangesAsync();
            }
            
            if (req.SendEmail)
            {
                var emailSvc = HttpContext.RequestServices.GetService(typeof(LeatherLane_Atelier.Services.IEmailService)) as LeatherLane_Atelier.Services.IEmailService;
                if (emailSvc != null)
                {
                    _ = Task.Run(async () =>
                    {
                        foreach (var u in users)
                        {
                            try {
                                
                                var emailHtml = LeatherLane_Atelier.Services.EmailTemplateBuilder.BuildStandardEmail(
                                    "📢 " + req.Title, 
                                    u.Name, 
                                    req.Message,
                                    req.ActionUrl, "Shop Now"
                                );
                                await emailSvc.SendEmailAsync(u.Email, req.Title, emailHtml);
                                await Task.Delay(500); // Small delay to prevent SMTP throttling
                            } catch {}
                        }
                    });
                }
            }
            
            return Ok(new { message = $"Broadcast successfully sent to {users.Count} customers." });
        }

        public class BroadcastRequest
        {
            public string Title { get; set; } = string.Empty;
            public string Message { get; set; } = string.Empty;
            public string? ActionUrl { get; set; }
            public bool SendEmail { get; set; }
            public bool SendInApp { get; set; }
        }


        // Category Management Endpoints
        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories()
        {
            var categories = await _context.ProductCategories
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name)
                .ToListAsync();
            return Ok(categories);
        }

        [HttpPost("categories")]
        public async Task<IActionResult> CreateCategory([FromBody] ProductCategory category)
        {
            if (string.IsNullOrEmpty(category.Name))
            {
                return BadRequest(new { message = "Category name is required." });
            }

            _context.ProductCategories.Add(category);
            await _context.SaveChangesAsync();
            return Ok(category);
        }

        [HttpPut("categories/{id}")]
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] ProductCategory dto)
        {
            var category = await _context.ProductCategories.FindAsync(id);
            if (category == null) return NotFound();

            if (string.IsNullOrEmpty(dto.Name))
            {
                return BadRequest(new { message = "Category name is required." });
            }

            var oldName = category.Name;
            category.Name = dto.Name;
            category.IsActive = dto.IsActive;
            category.DisplayOrder = dto.DisplayOrder;

            // If name changes, cascade name updates to all associated products' string Category
            if (oldName != dto.Name)
            {
                var products = await _context.Products.Where(p => p.CategoryId == id).ToListAsync();
                foreach (var p in products)
                {
                    p.Category = dto.Name;
                }
            }

            await _context.SaveChangesAsync();
            return Ok(category);
        }

        [HttpDelete("categories/{id}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _context.ProductCategories.FindAsync(id);
            if (category == null) return NotFound(new { message = "Category not found." });

            var reasons = new List<string>();

            // Check 1: Category is Active
            if (category.IsActive)
            {
                reasons.Add("The category is currently marked as Active (please deactivate it first from Edit Category).");
            }

            // Check 2: Contains associated products
            var associatedProductsCount = await _context.Products
                .CountAsync(p => p.CategoryId == id || p.Category == category.Name);
            if (associatedProductsCount > 0)
            {
                reasons.Add($"The category currently contains {associatedProductsCount} associated product(s) (please remove or reassign them first).");
            }

            // If any blocking reasons exist, reject deletion with clear reasons
            if (reasons.Any())
            {
                string formattedReason;
                if (reasons.Count == 1)
                {
                    formattedReason = $"Cannot delete category '{category.Name}':\n• {reasons[0]}";
                }
                else
                {
                    formattedReason = $"Cannot delete category '{category.Name}' due to the following reasons:\n1. {reasons[0]}\n2. {reasons[1]}";
                }

                return BadRequest(new { message = formattedReason });
            }

            _context.ProductCategories.Remove(category);
            await _context.SaveChangesAsync();
            return Ok(new { message = $"Category '{category.Name}' deleted successfully." });
        }
    }

    public class UpdateStatusRequest
    {
        public string Status { get; set; } = string.Empty;
        public string? CancelReason { get; set; }
    }

    public class UpdateAdminSettingsRequest
    {
        public string? Email { get; set; }
        public string? NewPassword { get; set; }
    }

    public class RejectPaymentRequest
    {
        public string RejectionReason { get; set; } = string.Empty;
    }
}
