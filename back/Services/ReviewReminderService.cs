using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using LeatherLane_Atelier.Models;

namespace LeatherLane_Atelier.Services
{
    public class ReviewReminderService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ReviewReminderService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(30);

        public ReviewReminderService(IServiceProvider serviceProvider, ILogger<ReviewReminderService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ReviewReminderService started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckAndSendReviewRemindersAsync();
                }
                catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
                {
                    _logger.LogError(ex, "Error while checking and sending review reminders.");
                }

                try
                {
                    await Task.Delay(_checkInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // Graceful shutdown requested when stopping the app or debugging in Visual Studio
                    break;
                }
            }
        }

        public async Task CheckAndSendReviewRemindersAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

            var now = DateTime.UtcNow;

            // Fetch delivered transactions needing reminder checks
            var deliveredOrders = await db.Transactions
                .Include(t => t.User)
                .Include(t => t.Items)
                .Where(t => t.Status == "Delivered" && t.DeliveredAt != null && (!t.ReviewReminderDay2Sent || !t.ReviewReminderDay4Sent))
                .ToListAsync();

            if (!deliveredOrders.Any()) return;

            foreach (var order in deliveredOrders)
            {
                if (order.DeliveredAt == null) continue;

                var deliveredTime = order.DeliveredAt.Value;
                var productIds = order.Items.Where(i => i.ProductId.HasValue).Select(i => i.ProductId!.Value).ToList();

                // Check if customer already reviewed ANY product from this order
                bool hasReviewed = false;
                if (productIds.Any())
                {
                    hasReviewed = await db.Reviews.AnyAsync(r => r.UserId == order.UserId && productIds.Contains(r.ProductId));
                }

                // If customer already submitted a review, suppress all remaining reminders permanently
                if (hasReviewed)
                {
                    order.ReviewReminderDay2Sent = true;
                    order.ReviewReminderDay4Sent = true;
                    continue;
                }

                var itemsInfo = new List<OrderItemInfo>();
                foreach (var i in order.Items)
                {
                    var prod = i.ProductId.HasValue ? await db.Products.FindAsync(i.ProductId.Value) : null;
                    itemsInfo.Add(new OrderItemInfo
                    {
                        Name = i.Name,
                        ProductId = prod?.ProductId ?? ("LLA-PRD-" + i.ProductId),
                        Thumbnail = prod?.Image,
                        Quantity = i.Quantity,
                        Price = i.Price
                    });
                }

                // Day 2 (48h) Reminder Check
                if (deliveredTime <= now.AddDays(-2) && !order.ReviewReminderDay2Sent)
                {
                    // 1. Send Day 2 Email
                    if (order.User != null && !string.IsNullOrWhiteSpace(order.User.Email))
                    {
                        var emailHtml = EmailTemplateBuilder.BuildReviewReminderEmail(
                            order.User.Name ?? "Valued Customer",
                            order.OrderId,
                            2,
                            itemsInfo
                        );

                        _ = emailService.SendEmailAsync(
                            order.User.Email,
                            $"How was your experience with Order {order.OrderId}? | LeatherLane Atelier",
                            emailHtml
                        );
                    }

                    // 2. Create In-App Notification
                    db.Notifications.Add(new Notification
                    {
                        UserId = order.UserId,
                        Title = "Share Your Experience 🌟",
                        Message = $"How is your handcrafted piece performing? Please leave a review for Order {order.OrderId}.",
                        ActionUrl = $"/order-tracking.html?id={order.OrderId}",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    });

                    order.ReviewReminderDay2Sent = true;
                    _logger.LogInformation("Sent Day 2 review reminder for Order {OrderId}", order.OrderId);
                }

                // Day 4 (96h) Reminder Check
                if (deliveredTime <= now.AddDays(-4) && !order.ReviewReminderDay4Sent)
                {
                    // 1. Send Day 4 Email
                    if (order.User != null && !string.IsNullOrWhiteSpace(order.User.Email))
                    {
                        var emailHtml = EmailTemplateBuilder.BuildReviewReminderEmail(
                            order.User.Name ?? "Valued Customer",
                            order.OrderId,
                            4,
                            itemsInfo
                        );

                        _ = emailService.SendEmailAsync(
                            order.User.Email,
                            $"We value your feedback on Order {order.OrderId} | LeatherLane Atelier",
                            emailHtml
                        );
                    }

                    // 2. Create In-App Notification
                    db.Notifications.Add(new Notification
                    {
                        UserId = order.UserId,
                        Title = "We Value Your Feedback ⭐",
                        Message = $"Your review helps our master craftsmen! Please rate your items from Order {order.OrderId}.",
                        ActionUrl = $"/order-tracking.html?id={order.OrderId}",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    });

                    order.ReviewReminderDay4Sent = true;
                    _logger.LogInformation("Sent Day 4 review reminder for Order {OrderId}", order.OrderId);
                }
            }

            await db.SaveChangesAsync();
        }
    }
}
