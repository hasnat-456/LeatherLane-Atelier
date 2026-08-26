using System;
using System.Collections.Generic;
using System.Text;

namespace LeatherLane_Atelier.Services
{
    public class OrderItemInfo
    {
        public string Name { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }

    public static class EmailTemplateBuilder
    {
        private static string BaseHtml(string title, string customerName, string bodyContent, string actionButtonHtml = "", string orderSummaryHtml = "")
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: 'Helvetica Neue', Helvetica, Arial, sans-serif; background-color: #f7f7f7; color: #333; margin: 0; padding: 0; }}
        .email-wrapper {{ width: 100%; max-width: 600px; margin: 0 auto; background-color: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 4px 10px rgba(0,0,0,0.05); margin-top: 20px; margin-bottom: 20px; }}
        .header {{ background-color: #4A1515; padding: 30px 20px; text-align: center; color: #fff; }}
        .header h1 {{ margin: 0; font-size: 24px; font-weight: 600; letter-spacing: 2px; text-transform: uppercase; }}
        .header-status {{ font-size: 14px; font-weight: 400; color: #D4AF37; margin-top: 10px; text-transform: uppercase; letter-spacing: 1px; }}
        .content {{ padding: 30px; }}
        .greeting {{ font-size: 18px; font-weight: 600; margin-bottom: 20px; color: #222; }}
        .message-body {{ font-size: 15px; line-height: 1.6; color: #555; margin-bottom: 30px; }}
        .order-summary-box {{ border-top: 1px solid #eee; border-bottom: 1px solid #eee; padding: 20px 0; margin-bottom: 30px; }}
        .order-summary-title {{ font-size: 12px; font-weight: bold; color: #888; text-transform: uppercase; letter-spacing: 1px; margin-bottom: 15px; }}
        .item-row {{ display: flex; justify-content: space-between; margin-bottom: 10px; font-size: 14px; }}
        .item-name {{ color: #333; }}
        .item-qty {{ color: #888; font-size: 13px; }}
        .item-price {{ font-weight: 600; color: #222; }}
        .totals-row {{ display: flex; justify-content: space-between; margin-top: 15px; font-size: 14px; padding-top: 15px; border-top: 1px dashed #eee; }}
        .grand-total {{ display: flex; justify-content: space-between; margin-top: 10px; font-size: 16px; font-weight: bold; padding-top: 10px; color: #4A1515; }}
        .action-area {{ text-align: center; margin: 30px 0; }}
        .btn {{ display: inline-block; background-color: #D4AF37; color: #fff; text-decoration: none; padding: 12px 30px; border-radius: 4px; font-weight: bold; font-size: 14px; letter-spacing: 1px; text-transform: uppercase; }}
        .footer {{ background-color: #f7f7f7; text-align: center; padding: 20px; font-size: 12px; color: #888; border-top: 1px solid #eaeaea; }}
        .footer a {{ color: #4A1515; text-decoration: none; }}
    </style>
</head>
<body>
    <div class='email-wrapper'>
        <div class='header'>
            <h1>LEATHERLANE ATELIER</h1>
            <div class='header-status'>{title}</div>
        </div>
        <div class='content'>
            <div class='greeting'>Hello {(string.IsNullOrEmpty(customerName) ? "" : customerName + ",") }</div>
            <div class='message-body'>
                {bodyContent}
            </div>
            
            {orderSummaryHtml}
            
            {actionButtonHtml}
            
        </div>
        <div class='footer'>
            <p>LeatherLane Atelier | Premium Leather Footwear<br>Crafted in Pakistan</p>
            <p>Need help? Contact us at <a href='mailto:leatherlaneatelier@gmail.com'>leatherlaneatelier@gmail.com</a></p>
        </div>
    </div>
</body>
</html>";
        }

        public static string BuildOrderEmail(string title, string customerName, string orderNumber, string message, List<OrderItemInfo> items = null, decimal? deliveryFee = null, decimal? totalAmount = null, string actionUrl = null, string actionText = "Track Your Order")
        {
            string summaryHtml = "";
            
            if (items != null && items.Count > 0 && totalAmount.HasValue)
            {
                var sb = new StringBuilder();
                sb.Append("<div class='order-summary-box'>");
                sb.Append("<div class='order-summary-title'>ORDER SUMMARY (ORDER #" + orderNumber + ")</div>");
                
                foreach(var item in items)
                {
                    sb.Append($@"
                    <div class='item-row'>
                        <div>
                            <div class='item-name'>{item.Name}</div>
                            <div class='item-qty'>Quantity: {item.Quantity}</div>
                        </div>
                        <div class='item-price'>Rs. {item.Price:N0}</div>
                    </div>");
                }
                
                if (deliveryFee.HasValue)
                {
                    sb.Append($@"
                    <div class='totals-row'>
                        <div>Delivery</div>
                        <div>Rs. {deliveryFee.Value:N0}</div>
                    </div>");
                }
                
                sb.Append($@"
                <div class='grand-total'>
                    <div>TOTAL</div>
                    <div>Rs. {totalAmount.Value:N0}</div>
                </div>");
                
                sb.Append("</div>");
                summaryHtml = sb.ToString();
            }

            string actionHtml = "";
            if (!string.IsNullOrEmpty(actionUrl))
            {
                // Ensure URL is absolute for emails if it's relative
                if (actionUrl.StartsWith("/"))
                {
                    actionUrl = "https://leatherlaneatelier.store" + actionUrl;
                }
                actionHtml = $@"
                <div class='action-area'>
                    <a href='{actionUrl}' class='btn'>{actionText}</a>
                </div>";
            }

            return BaseHtml(title, customerName, message, actionHtml, summaryHtml);
        }

        public static string BuildStandardEmail(string title, string customerName, string message, string actionUrl = null, string actionText = "View Details")
        {
            string actionHtml = "";
            if (!string.IsNullOrEmpty(actionUrl))
            {
                if (actionUrl.StartsWith("/"))
                {
                    actionUrl = "https://leatherlaneatelier.store" + actionUrl;
                }
                actionHtml = $@"
                <div class='action-area'>
                    <a href='{actionUrl}' class='btn'>{actionText}</a>
                </div>";
            }

            return BaseHtml(title, customerName, message, actionHtml, "");
        }
    }

    public static class EmailServiceExtensions
    {
        public static async System.Threading.Tasks.Task NotifyAdminsAsync(this IEmailService emailService, LeatherLane_Atelier.Models.ApplicationDbContext context, string subject, string body)
        {
            var adminEmails = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
                System.Linq.Queryable.Select(
                    System.Linq.Queryable.Where(context.Users, u => u.Role == "Admin"), 
                    u => u.Email
                )
            );
            
            var allAdminEmails = new System.Collections.Generic.HashSet<string>(adminEmails);
            allAdminEmails.Add("leatherlaneatelier@gmail.com");

            foreach (var email in allAdminEmails)
            {
                var htmlBody = EmailTemplateBuilder.BuildStandardEmail(subject, "Admin", body, "/admin.html", "Go to Dashboard");
                _ = emailService.SendEmailAsync(email, subject, htmlBody);
            }
        }
    }
}
