using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Microsoft.EntityFrameworkCore;

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
        private static string BaseHtml(string title, string customerName, string bodyContent, string actionButtonHtml = "", string customBoxHtml = "")
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <title>LeatherLane Atelier</title>
</head>
<body style=""margin: 0; padding: 0; font-family: 'Helvetica Neue', Helvetica, Arial, sans-serif; background-color: #F8F5F0; color: #333333;"">
    <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color: #F8F5F0; padding: 40px 20px;"">
        <tr>
            <td align=""center"">
                <!-- Main Email Card -->
                <table width=""100%"" max-width=""600"" cellpadding=""0"" cellspacing=""0"" style=""max-width: 600px; background-color: #ffffff; border-radius: 6px; overflow: hidden; box-shadow: 0 8px 25px rgba(0,0,0,0.04);"">
                    <!-- Premium Header -->
                    <tr>
                        <td align=""center"" style=""background-color: #4A1515; padding: 40px 20px; border-bottom: 2px solid #C79A52;"">
                            <h1 style=""color: #ffffff; margin: 0; font-size: 22px; letter-spacing: 4px; text-transform: uppercase; font-weight: 500;"">LeatherLane Atelier</h1>
                            <p style=""color: #C79A52; margin: 12px 0 0 0; font-size: 11px; letter-spacing: 3px; text-transform: uppercase;"">Premium Leather Footwear</p>
                        </td>
                    </tr>
                    <!-- Body Content -->
                    <tr>
                        <td style=""padding: 45px 40px;"">
                            <h2 style=""margin: 0 0 20px 0; font-size: 18px; color: #111111; font-weight: 600;"">Hello {(string.IsNullOrEmpty(customerName) ? "" : customerName + ",")}</h2>
                            <div style=""margin: 0; font-size: 15px; line-height: 1.6; color: #555555;"">
                                {bodyContent}
                            </div>
                            {customBoxHtml}
                            {actionButtonHtml}
                        </td>
                    </tr>
                    <!-- Dark Footer -->
                    <tr>
                        <td align=""center"" style=""background-color: #4A1515; padding: 35px 20px;"">
                            <p style=""margin: 0 0 10px 0; font-size: 11px; color: #D8C7A6; letter-spacing: 1px; text-transform: uppercase;"">
                                <strong>LeatherLane Atelier</strong> | Crafted in Pakistan
                            </p>
                            <p style=""margin: 0; font-size: 12px; color: #D8C7A6;"">
                                Need help? Contact us at <a href=""mailto:leatherlaneatelier@gmail.com"" style=""color: #ffffff; text-decoration: none; font-weight: bold;"">leatherlaneatelier@gmail.com</a>
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
        }

        public static string BuildOrderEmail(string title, string customerName, string orderNumber, string message, List<OrderItemInfo> items = null, decimal? deliveryFee = null, decimal? totalAmount = null, string actionUrl = null, string actionText = "Track Your Order")
        {
            string summaryHtml = "";
            if (items != null && items.Count > 0 && totalAmount.HasValue)
            {
                var sb = new StringBuilder();
                sb.Append($@"
                <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""margin: 35px 0; border: 1px solid #EAEAEA; border-radius: 4px; background-color: #FAFAFA;"">
                    <tr>
                        <td style=""padding: 30px;"">
                            <h3 style=""margin: 0 0 25px 0; font-size: 12px; color: #4A1515; text-transform: uppercase; letter-spacing: 2px; border-bottom: 1px solid #C79A52; padding-bottom: 10px; display: inline-block;"">ORDER SUMMARY (#{orderNumber})</h3>
                            <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""font-size: 14px;"">");
                foreach(var item in items)
                {
                    sb.Append($@"
                                <tr>
                                    <td style=""padding: 14px 0; border-bottom: 1px solid #F0F0F0; width: 70%;"">
                                        <div style=""color: #111111; font-weight: 500;"">{item.Name}</div>
                                        <div style=""color: #888888; font-size: 11px; text-transform: uppercase; letter-spacing: 1px; margin-top: 4px;"">QTY: {item.Quantity}</div>
                                    </td>
                                    <td style=""padding: 14px 0; border-bottom: 1px solid #F0F0F0; color: #111111; font-weight: 600; text-align: right; vertical-align: top;"">Rs. {item.Price:N0}</td>
                                </tr>");
                }
                if (deliveryFee.HasValue)
                {
                    sb.Append($@"
                                <tr>
                                    <td style=""padding: 14px 0; border-bottom: 1px solid #F0F0F0; color: #888888; font-size: 11px; text-transform: uppercase; letter-spacing: 1px;"">Delivery Fee</td>
                                    <td style=""padding: 14px 0; border-bottom: 1px solid #F0F0F0; color: #4A1515; font-weight: 600; text-align: right;"">Rs. {deliveryFee.Value:N0}</td>
                                </tr>");
                }
                sb.Append($@"
                                <tr>
                                    <td style=""padding-top: 14px; color: #111111; font-size: 12px; text-transform: uppercase; letter-spacing: 1px; font-weight: bold;"">TOTAL</td>
                                    <td style=""padding-top: 14px; color: #2E6B4D; font-weight: 600; text-align: right; font-size: 16px;"">Rs. {totalAmount.Value:N0}</td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                </table>");
                summaryHtml = sb.ToString();
            }

            string actionHtml = "";
            if (!string.IsNullOrEmpty(actionUrl))
            {
                if (actionUrl.StartsWith("/")) actionUrl = "https://leatherlaneatelier.store" + actionUrl;
                actionHtml = $@"
                <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""margin-top: 10px;"">
                    <tr>
                        <td align=""center"">
                            <a href=""{actionUrl}"" style=""display: inline-block; background-color: #111111; color: #ffffff; text-decoration: none; padding: 15px 35px; border-radius: 4px; font-weight: 600; font-size: 13px; letter-spacing: 2px; text-transform: uppercase;"">{actionText}</a>
                        </td>
                    </tr>
                </table>";
            }

            return BaseHtml(title, customerName, message, actionHtml, summaryHtml);
        }

        public static string BuildStandardEmail(string title, string customerName, string message, string actionUrl = null, string actionText = "View Details", Dictionary<string, string> details = null, string detailsTitle = "Transaction Details")
        {
            string detailsHtml = "";
            if (details != null && details.Count > 0)
            {
                var sb = new StringBuilder();
                sb.Append($@"
                <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""margin: 35px 0; border: 1px solid #EAEAEA; border-radius: 4px; background-color: #FAFAFA;"">
                    <tr>
                        <td style=""padding: 30px;"">
                            <h3 style=""margin: 0 0 25px 0; font-size: 12px; color: #4A1515; text-transform: uppercase; letter-spacing: 2px; border-bottom: 1px solid #C79A52; padding-bottom: 10px; display: inline-block;"">{detailsTitle}</h3>
                            <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""font-size: 14px;"">");
                
                int index = 0;
                foreach (var detail in details)
                {
                    bool isLast = (index == details.Count - 1);
                    string borderStyle = isLast ? "" : "border-bottom: 1px solid #F0F0F0;";
                    string padTop = index == 0 ? "padding: 0 0 14px 0;" : "padding: 14px 0;";
                    if (isLast) padTop = "padding-top: 14px;";

                    sb.Append($@"
                                <tr>
                                    <td style=""{padTop} {borderStyle} color: #888888; font-size: 11px; text-transform: uppercase; letter-spacing: 1px; width: 45%;"">{detail.Key}</td>
                                    <td style=""{padTop} {borderStyle} color: #111111; font-weight: 600; text-align: right;"">{detail.Value}</td>
                                </tr>");
                    index++;
                }

                sb.Append(@"
                            </table>
                        </td>
                    </tr>
                </table>");
                detailsHtml = sb.ToString();
            }

            string actionHtml = "";
            if (!string.IsNullOrEmpty(actionUrl))
            {
                if (actionUrl.StartsWith("/")) actionUrl = "https://leatherlaneatelier.store" + actionUrl;
                actionHtml = $@"
                <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""margin-top: 10px;"">
                    <tr>
                        <td align=""center"">
                            <a href=""{actionUrl}"" style=""display: inline-block; background-color: #111111; color: #ffffff; text-decoration: none; padding: 15px 35px; border-radius: 4px; font-weight: 600; font-size: 13px; letter-spacing: 2px; text-transform: uppercase;"">{actionText}</a>
                        </td>
                    </tr>
                </table>";
            }

            return BaseHtml(title, customerName, message, actionHtml, detailsHtml);
        }
    }

    public static class EmailServiceExtensions
    {
        public static async System.Threading.Tasks.Task NotifyAdminsAsync(this IEmailService emailService, LeatherLane_Atelier.Models.ApplicationDbContext context, string subject, string body, Dictionary<string, string> details = null, string detailsTitle = "Alert Details")
        {
            var adminEmails = await context.Users
                .Where(u => u.Role == "Admin")
                .Select(u => u.Email)
                .ToListAsync();
            
            var allAdminEmails = new HashSet<string>(adminEmails);
            allAdminEmails.Add("leatherlaneatelier@gmail.com");

            foreach (var email in allAdminEmails)
            {
                var htmlBody = EmailTemplateBuilder.BuildStandardEmail(subject, "Admin", body, "/admin.html", "Go to Dashboard", details, detailsTitle);
                _ = emailService.SendEmailAsync(email, subject, htmlBody);
            }
        }
    }
}
