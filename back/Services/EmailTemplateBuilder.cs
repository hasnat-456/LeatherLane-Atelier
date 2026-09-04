using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace LeatherLane_Atelier.Services
{
    public class OrderItemInfo
    {
        public string Name { get; set; } = string.Empty;
        public string? ProductId { get; set; }
        public string? Thumbnail { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }

    public static class EmailTemplateBuilder
    {
        private const string SiteBaseUrl = "https://leatherlaneatelier.store";

        private static string EnsureAbsoluteUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return "https://via.placeholder.com/120?text=LeatherLane";
            if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                return url;
            return SiteBaseUrl + (url.StartsWith("/") ? "" : "/") + url;
        }

        public static string FormatNonClickableText(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "N/A";
            var encoded = System.Net.WebUtility.HtmlEncode(text.Trim());
            var unlinked = encoded.Replace(" ", " &#8204;");
            return $"<span style=\"color: #111111 !important; text-decoration: none !important; pointer-events: none !important; cursor: default !important;\">{unlinked}</span>";
        }

        public static string BaseHtml(string title, string recipientName, string bodyContent, string actionButtonHtml = "", string customBoxHtml = "")
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>LeatherLane Atelier</title>
    <style>
        a[x-apple-data-detectors],
        a[href*=""maps.google.com""],
        a[href*=""google.com/maps""] {{
            color: inherit !important;
            text-decoration: none !important;
            font-size: inherit !important;
            font-family: inherit !important;
            font-weight: inherit !important;
            line-height: inherit !important;
            pointer-events: none !important;
            cursor: default !important;
        }}
    </style>
</head>
<body id=""body"" style=""margin: 0; padding: 0; font-family: 'Helvetica Neue', Helvetica, Arial, sans-serif; background-color: #F8F5F0; color: #333333;"">
    <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color: #F8F5F0; padding: 40px 15px;"">
        <tr>
            <td align=""center"">
                <!-- Main Email Card -->
                <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""max-width: 620px; background-color: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 10px 30px rgba(0,0,0,0.06); border: 1px solid #EAE2D5;"">
                    <!-- Luxury Header -->
                    <tr>
                        <td align=""center"" style=""background-color: #4A1515; padding: 35px 20px; border-bottom: 3px solid #C79A52;"">
                            <h1 style=""color: #ffffff; margin: 0; font-size: 22px; letter-spacing: 4px; text-transform: uppercase; font-weight: 500;"">LeatherLane Atelier</h1>
                            <p style=""color: #C79A52; margin: 8px 0 0 0; font-size: 11px; letter-spacing: 3px; text-transform: uppercase;"">Master Craftsmen &bull; Handcrafted Luxury</p>
                        </td>
                    </tr>
                    <!-- Body Content -->
                    <tr>
                        <td style=""padding: 40px 35px;"">
                            <h2 style=""margin: 0 0 18px 0; font-size: 18px; color: #111111; font-weight: 600;"">{(string.IsNullOrEmpty(recipientName) ? "Dear Valued Customer," : "Dear " + recipientName + ",")}</h2>
                            <div style=""margin: 0 0 25px 0; font-size: 15px; line-height: 1.65; color: #444444;"">
                                {bodyContent}
                            </div>
                            {customBoxHtml}
                            {actionButtonHtml}
                        </td>
                    </tr>
                    <!-- Luxury Footer -->
                    <tr>
                        <td align=""center"" style=""background-color: #4A1515; padding: 30px 20px; border-top: 1px solid rgba(199, 154, 82, 0.2);"">
                            <p style=""margin: 0 0 8px 0; font-size: 11px; color: #D8C7A6; letter-spacing: 2px; text-transform: uppercase;"">
                                <strong>LeatherLane Atelier</strong> &mdash; Crafted for a Lifetime
                            </p>
                            <p style=""margin: 0; font-size: 12px; color: #D8C7A6; line-height: 1.5;"">
                                Questions or Inquiries? Contact our Atelier Concierge at <a href=""mailto:leatherlaneatelier@gmail.com"" style=""color: #C79A52; text-decoration: none; font-weight: bold;"">leatherlaneatelier@gmail.com</a>
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

        // 1. New Product Alert
        public static string BuildNewProductAlertEmail(
            string recipientName, 
            string productName, 
            string? productId, 
            string category, 
            decimal price, 
            string? thumbnail, 
            string? sizes, 
            int productIdNumber)
        {
            var imgUrl = EnsureAbsoluteUrl(thumbnail);
            var prodIdText = !string.IsNullOrEmpty(productId) ? productId : "LLA-PRODUCT";

            var sb = new StringBuilder();
            sb.Append($@"
            <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""margin: 25px 0; border: 1.5px solid #EAE2D5; border-radius: 8px; background-color: #FDFBF8; overflow: hidden;"">
                <tr>
                    <td align=""center"" style=""padding: 25px 20px 15px 20px; background-color: #ffffff; border-bottom: 1px solid #EAE2D5;"">
                        <img src=""{imgUrl}"" alt=""{productName}"" style=""max-width: 260px; width: 100%; height: auto; border-radius: 6px; border: 1px solid #E2D7C5; object-fit: cover; display: block; margin: 0 auto;"">
                    </td>
                </tr>
                <tr>
                    <td style=""padding: 25px;"">
                        <div style=""font-size: 11px; color: #8C5E3C; font-weight: bold; text-transform: uppercase; letter-spacing: 2px; margin-bottom: 5px;"">{category}</div>
                        <h3 style=""margin: 0 0 10px 0; color: #111111; font-size: 20px; font-weight: 600;"">{productName}</h3>
                        <div style=""font-family: monospace; color: #8C5E3C; font-size: 12px; font-weight: bold; margin-bottom: 15px;"">Product ID: {prodIdText}</div>
                        <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""font-size: 14px; border-top: 1px solid #F0F0F0; padding-top: 12px;"">
                            <tr>
                                <td style=""padding: 6px 0; color: #666; font-size: 12px; text-transform: uppercase;"">Price</td>
                                <td style=""padding: 6px 0; color: #4A1515; font-size: 18px; font-weight: bold; text-align: right;"">Rs. {price:N0}</td>
                            </tr>
                            {(string.IsNullOrEmpty(sizes) ? "" : $@"
                            <tr>
                                <td style=""padding: 6px 0; color: #666; font-size: 12px; text-transform: uppercase;"">Available Sizes</td>
                                <td style=""padding: 6px 0; color: #111; font-weight: 600; text-align: right;"">{sizes}</td>
                            </tr>")}
                        </table>
                    </td>
                </tr>
            </table>");

            var actionHtml = $@"
            <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""margin-top: 20px;"">
                <tr>
                    <td align=""center"">
                        <a href=""{SiteBaseUrl}/product-detail.html?id={productIdNumber}"" target=""_blank"" style=""display: inline-block; background-color: #4A1515; color: #ffffff !important; text-decoration: none !important; padding: 14px 35px; border-radius: 4px; font-weight: 600; font-size: 13px; letter-spacing: 2px; text-transform: uppercase; border: 1px solid #C79A52;""><span style=""color: #ffffff !important; text-decoration: none !important; font-weight: 600;"">View &amp; Order Product</span></a>
                    </td>
                </tr>
            </table>";

            string message = $"We are proud to unveil our latest bespoke creation: <strong>{productName}</strong>. Handcrafted with pure genuine leather and refined artisan tailoring, explore its details below.";
            return BaseHtml("New Collection Arrival", recipientName, message, actionHtml, sb.ToString());
        }

        // 2. Order Confirmation / Status / Items Email
        public static string BuildOrderEmail(
            string title, 
            string customerName, 
            string orderNumber, 
            string message, 
            List<OrderItemInfo>? items = null, 
            decimal? deliveryFee = 0m, 
            decimal? totalAmount = null, 
            string? status = "Order Placed", 
            string? paymentMethod = "Bank Transfer", 
            string? actionUrl = null, 
            string actionText = "Track Your Order",
            string? courierName = null,
            string? trackingNumber = null,
            string? rejectionReason = null)
        {
            var sb = new StringBuilder();

            // Order ID ON TOP Prominent Badge Bar
            sb.Append($@"
            <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""margin-bottom: 25px; background-color: #FDFBF8; border: 1.5px solid #EAE2D5; border-radius: 6px; padding: 16px;"">
                <tr>
                    <td style=""padding: 6px 12px; vertical-align: middle;"">
                        <div style=""font-size: 11px; color: #888888; text-transform: uppercase; letter-spacing: 1.5px; font-weight: 600;"">ORDER ID</div>
                        <div style=""font-size: 16px; color: #8C5E3C; font-weight: bold; font-family: monospace; margin-top: 3px;"">{orderNumber}</div>
                    </td>
                    <td style=""padding: 6px 12px; border-left: 1px solid #EAE2D5; vertical-align: middle;"">
                        <div style=""font-size: 11px; color: #888888; text-transform: uppercase; letter-spacing: 1.5px; font-weight: 600;"">STATUS</div>
                        <div style=""font-size: 14px; color: #2E6B4D; font-weight: bold; margin-top: 3px;"">{status}</div>
                    </td>
                    <td style=""padding: 6px 12px; border-left: 1px solid #EAE2D5; vertical-align: middle;"">
                        <div style=""font-size: 11px; color: #888888; text-transform: uppercase; letter-spacing: 1.5px; font-weight: 600;"">PAYMENT</div>
                        <div style=""font-size: 13px; color: #111111; font-weight: bold; margin-top: 3px;"">{paymentMethod ?? "Bank Transfer"}</div>
                    </td>
                </tr>
            </table>");

            // Courier / Tracking Card (if shipped)
            if (!string.IsNullOrEmpty(courierName) || !string.IsNullOrEmpty(trackingNumber))
            {
                sb.Append($@"
                <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""margin-bottom: 25px; background-color: #F4F8F5; border: 1.5px solid #C8E0D1; border-radius: 6px; padding: 16px;"">
                    <tr>
                        <td style=""padding: 6px 12px;"">
                            <div style=""font-size: 11px; color: #2E6B4D; text-transform: uppercase; letter-spacing: 1.5px; font-weight: bold;"">COURIER SERVICE</div>
                            <div style=""font-size: 14px; color: #111111; font-weight: 600; margin-top: 3px;"">{courierName ?? "Designated Courier"}</div>
                        </td>
                        <td style=""padding: 6px 12px; border-left: 1px solid #C8E0D1;"">
                            <div style=""font-size: 11px; color: #2E6B4D; text-transform: uppercase; letter-spacing: 1.5px; font-weight: bold;"">TRACKING NUMBER</div>
                            <div style=""font-size: 14px; color: #8C5E3C; font-weight: bold; font-family: monospace; margin-top: 3px;"">{trackingNumber ?? "In Transit"}</div>
                        </td>
                    </tr>
                </table>");
            }

            // Rejection Reason Box (if payment rejected)
            if (!string.IsNullOrEmpty(rejectionReason))
            {
                sb.Append($@"
                <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""margin-bottom: 25px; background-color: #FFF5F5; border: 1.5px solid #F5C6CB; border-radius: 6px; padding: 16px;"">
                    <tr>
                        <td>
                            <div style=""font-size: 11px; color: #721C24; text-transform: uppercase; letter-spacing: 1.5px; font-weight: bold; margin-bottom: 5px;"">REASON FOR REJECTION</div>
                            <div style=""font-size: 14px; color: #721C24; line-height: 1.5;"">{rejectionReason}</div>
                        </td>
                    </tr>
                </table>");
            }

            // Itemized Products Summary
            if (items != null && items.Count > 0)
            {
                sb.Append($@"
                <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""margin: 20px 0 30px 0; border: 1px solid #EAEAEA; border-radius: 6px; background-color: #FAFAFA; overflow: hidden;"">
                    <tr>
                        <td style=""padding: 25px;"">
                            <h3 style=""margin: 0 0 20px 0; font-size: 12px; color: #4A1515; text-transform: uppercase; letter-spacing: 2px; border-bottom: 1.5px solid #C79A52; padding-bottom: 8px; display: inline-block;"">ORDERED ITEMS ({orderNumber})</h3>
                            <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""font-size: 14px;"">");

                foreach (var item in items)
                {
                    var imgUrl = EnsureAbsoluteUrl(item.Thumbnail);
                    var prodIdHtml = !string.IsNullOrEmpty(item.ProductId) 
                        ? $@"<div style=""color: #8C5E3C; font-family: monospace; font-size: 11px; font-weight: bold; margin-top: 3px;"">Product ID: {item.ProductId}</div>" 
                        : "";

                    sb.Append($@"
                                <tr>
                                    <td style=""padding: 14px 0; border-bottom: 1px solid #F0F0F0; width: 68px; vertical-align: middle;"">
                                        <img src=""{imgUrl}"" alt=""{item.Name}"" width=""56"" height=""56"" style=""object-fit: cover; border-radius: 4px; border: 1px solid #E2D7C5; display: block;"">
                                    </td>
                                    <td style=""padding: 14px 10px; border-bottom: 1px solid #F0F0F0; vertical-align: middle;"">
                                        <div style=""color: #111111; font-weight: 600; font-size: 14px;"">{item.Name}</div>
                                        {prodIdHtml}
                                        <div style=""color: #777777; font-size: 11px; text-transform: uppercase; letter-spacing: 1px; margin-top: 3px;"">QTY: {item.Quantity} &bull; Rs. {item.Price:N0} each</div>
                                    </td>
                                    <td style=""padding: 14px 0; border-bottom: 1px solid #F0F0F0; color: #111111; font-weight: bold; text-align: right; vertical-align: middle; white-space: nowrap;"">
                                        Rs. {(item.Price * item.Quantity):N0}
                                    </td>
                                </tr>");
                }

                // Delivery Fee (Always Rs. 0 Free Delivery)
                sb.Append($@"
                                <tr>
                                    <td colspan=""2"" style=""padding: 14px 0; border-bottom: 1px solid #F0F0F0; color: #666666; font-size: 12px; text-transform: uppercase; letter-spacing: 1px;"">Delivery Charges</td>
                                    <td style=""padding: 14px 0; border-bottom: 1px solid #F0F0F0; color: #2E6B4D; font-weight: bold; text-align: right;"">Rs. 0 (Free Delivery)</td>
                                </tr>");

                // Grand Total
                if (totalAmount.HasValue)
                {
                    sb.Append($@"
                                <tr>
                                    <td colspan=""2"" style=""padding-top: 16px; color: #111111; font-size: 13px; text-transform: uppercase; letter-spacing: 1.5px; font-weight: bold;"">TOTAL AMOUNT</td>
                                    <td style=""padding-top: 16px; color: #8C5E3C; font-weight: bold; text-align: right; font-size: 18px;"">Rs. {totalAmount.Value:N0}</td>
                                </tr>");
                }

                sb.Append(@"
                            </table>
                        </td>
                    </tr>
                </table>");
            }

            string actionHtml = "";
            if (!string.IsNullOrEmpty(actionUrl))
            {
                if (actionUrl.StartsWith("/")) actionUrl = SiteBaseUrl + actionUrl;
                actionHtml = $@"
                <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""margin-top: 15px;"">
                    <tr>
                        <td align=""center"">
                            <a href=""{actionUrl}"" target=""_blank"" style=""display: inline-block; background-color: #4A1515; color: #ffffff !important; text-decoration: none !important; padding: 14px 35px; border-radius: 4px; font-weight: 600; font-size: 13px; letter-spacing: 2px; text-transform: uppercase; border: 1px solid #C79A52;""><span style=""color: #ffffff !important; text-decoration: none !important; font-weight: 600;"">{actionText}</span></a>
                        </td>
                    </tr>
                </table>";
            }

            return BaseHtml(title, customerName, message, actionHtml, sb.ToString());
        }

        // 3. Admin New Order Dossier
        public static string BuildAdminOrderEmail(
            string orderNumber, 
            string customerName, 
            string customerEmail, 
            string? customerPhone, 
            string? shippingAddress, 
            string paymentMethod, 
            string status, 
            List<OrderItemInfo> items, 
            decimal totalAmount)
        {
            var sb = new StringBuilder();

            // Order ID on top banner
            sb.Append($@"
            <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""margin-bottom: 25px; background-color: #FDFBF8; border: 1.5px solid #EAE2D5; border-radius: 6px; padding: 18px;"">
                <tr>
                    <td style=""padding-bottom: 12px; border-bottom: 1px solid #EAE2D5;"" colspan=""2"">
                        <div style=""font-size: 11px; color: #8C5E3C; font-weight: bold; text-transform: uppercase; letter-spacing: 2px;"">ORDER ID: {orderNumber}</div>
                        <div style=""font-size: 15px; color: #4A1515; font-weight: bold; margin-top: 3px;"">Customer &amp; Shipping Dossier</div>
                    </td>
                </tr>
                <tr>
                    <td style=""padding: 10px 0; width: 45%; color: #666; font-size: 12px; text-transform: uppercase;"">Customer Name</td>
                    <td style=""padding: 10px 0; font-weight: bold; color: #111;"">{customerName}</td>
                </tr>
                <tr>
                    <td style=""padding: 8px 0; color: #666; font-size: 12px; text-transform: uppercase;"">Email Address</td>
                    <td style=""padding: 8px 0; font-weight: bold; color: #111;"">{customerEmail}</td>
                </tr>
                <tr>
                    <td style=""padding: 8px 0; color: #666; font-size: 12px; text-transform: uppercase;"">Phone Number</td>
                    <td style=""padding: 8px 0; font-weight: bold; color: #111;"">{FormatNonClickableText(customerPhone)}</td>
                </tr>
                <tr>
                    <td style=""padding: 8px 0; color: #666; font-size: 12px; text-transform: uppercase;"">Shipping Address</td>
                    <td style=""padding: 8px 0; font-weight: bold; color: #111; line-height: 1.4;"">{FormatNonClickableText(shippingAddress)}</td>
                </tr>
                <tr>
                    <td style=""padding: 8px 0; color: #666; font-size: 12px; text-transform: uppercase;"">Payment Method</td>
                    <td style=""padding: 8px 0; font-weight: bold; color: #8C5E3C;"">{paymentMethod}</td>
                </tr>
                <tr>
                    <td style=""padding: 8px 0; color: #666; font-size: 12px; text-transform: uppercase;"">Status</td>
                    <td style=""padding: 8px 0; font-weight: bold; color: #2E6B4D;"">{status}</td>
                </tr>
            </table>");

            // Ordered Products
            sb.Append($@"
            <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""margin: 20px 0 30px 0; border: 1px solid #EAEAEA; border-radius: 6px; background-color: #FAFAFA; overflow: hidden;"">
                <tr>
                    <td style=""padding: 25px;"">
                        <h3 style=""margin: 0 0 20px 0; font-size: 12px; color: #4A1515; text-transform: uppercase; letter-spacing: 2px; border-bottom: 1.5px solid #C79A52; padding-bottom: 8px; display: inline-block;"">ORDERED ITEMS ({orderNumber})</h3>
                        <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""font-size: 14px;"">");

            foreach (var item in items)
            {
                var imgUrl = EnsureAbsoluteUrl(item.Thumbnail);
                sb.Append($@"
                            <tr>
                                <td style=""padding: 12px 0; border-bottom: 1px solid #F0F0F0; width: 68px; vertical-align: middle;"">
                                    <img src=""{imgUrl}"" alt=""{item.Name}"" width=""56"" height=""56"" style=""object-fit: cover; border-radius: 4px; border: 1px solid #E2D7C5; display: block;"">
                                </td>
                                <td style=""padding: 12px 10px; border-bottom: 1px solid #F0F0F0; vertical-align: middle;"">
                                    <div style=""color: #111111; font-weight: 600; font-size: 14px;"">{item.Name}</div>
                                    <div style=""color: #8C5E3C; font-family: monospace; font-size: 11px; font-weight: bold; margin-top: 3px;"">Product ID: {item.ProductId ?? "N/A"}</div>
                                    <div style=""color: #777777; font-size: 11px; text-transform: uppercase; letter-spacing: 1px; margin-top: 3px;"">QTY: {item.Quantity} &bull; Rs. {item.Price:N0} each</div>
                                </td>
                                <td style=""padding: 12px 0; border-bottom: 1px solid #F0F0F0; color: #111111; font-weight: bold; text-align: right; vertical-align: middle; white-space: nowrap;"">
                                    Rs. {(item.Price * item.Quantity):N0}
                                </td>
                            </tr>");
            }

            sb.Append($@"
                            <tr>
                                <td colspan=""2"" style=""padding: 12px 0; border-bottom: 1px solid #F0F0F0; color: #666666; font-size: 12px; text-transform: uppercase; letter-spacing: 1px;"">Delivery Charges</td>
                                <td style=""padding: 12px 0; border-bottom: 1px solid #F0F0F0; color: #2E6B4D; font-weight: bold; text-align: right;"">Rs. 0 (Free Delivery)</td>
                            </tr>
                            <tr>
                                <td colspan=""2"" style=""padding-top: 14px; color: #111111; font-size: 13px; text-transform: uppercase; letter-spacing: 1.5px; font-weight: bold;"">TOTAL REVENUE</td>
                                <td style=""padding-top: 14px; color: #8C5E3C; font-weight: bold; text-align: right; font-size: 18px;"">Rs. {totalAmount:N0}</td>
                            </tr>
                        </table>
                    </td>
                </tr>
            </table>");

            var actionHtml = $@"
            <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""margin-top: 15px;"">
                <tr>
                    <td align=""center"">
                        <a href=""{SiteBaseUrl}/admin#orders"" target=""_blank"" style=""display: inline-block; background-color: #4A1515; color: #ffffff !important; text-decoration: none !important; padding: 14px 35px; border-radius: 4px; font-weight: 600; font-size: 13px; letter-spacing: 2px; text-transform: uppercase; border: 1px solid #C79A52;""><span style=""color: #ffffff !important; text-decoration: none !important; font-weight: 600;"">Manage Order in Dashboard</span></a>
                    </td>
                </tr>
            </table>";

            string message = $"A new order <strong>{orderNumber}</strong> has been placed by {customerName}. The full customer contact, shipping destination, and itemized product dossier are detailed below.";
            return BaseHtml("New Order Placed", "Admin", message, actionHtml, sb.ToString());
        }

        // 4. Review Confirmation (Thank You) Email
        public static string BuildCustomerReviewConfirmationEmail(
            string customerName, 
            string orderNumber, 
            string productName, 
            string? productId, 
            string? thumbnail, 
            int rating, 
            string? comment)
        {
            var imgUrl = EnsureAbsoluteUrl(thumbnail);
            var prodIdText = !string.IsNullOrEmpty(productId) ? productId : "LLA-PRODUCT";
            var starsHtml = new string('★', Math.Clamp(rating, 1, 5)) + new string('☆', 5 - Math.Clamp(rating, 1, 5));

            var sb = new StringBuilder();

            // Order ID on top
            sb.Append($@"
            <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""margin-bottom: 25px; background-color: #FDFBF8; border: 1.5px solid #EAE2D5; border-radius: 6px; padding: 16px;"">
                <tr>
                    <td>
                        <div style=""font-size: 11px; color: #888888; text-transform: uppercase; letter-spacing: 1.5px; font-weight: 600;"">ORDER ID</div>
                        <div style=""font-size: 15px; color: #8C5E3C; font-weight: bold; font-family: monospace; margin-top: 3px;"">{orderNumber}</div>
                    </td>
                </tr>
            </table>");

            // Product & Review Card
            sb.Append($@"
            <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""margin: 20px 0 30px 0; border: 1px solid #EAEAEA; border-radius: 6px; background-color: #FAFAFA; overflow: hidden;"">
                <tr>
                    <td style=""padding: 25px;"">
                        <table width=""100%"" cellpadding=""0"" cellspacing=""0"">
                            <tr>
                                <td style=""width: 70px; vertical-align: top;"">
                                    <img src=""{imgUrl}"" alt=""{productName}"" width=""60"" height=""60"" style=""object-fit: cover; border-radius: 4px; border: 1px solid #E2D7C5; display: block;"">
                                </td>
                                <td style=""padding-left: 15px; vertical-align: top;"">
                                    <div style=""color: #111111; font-weight: 600; font-size: 15px;"">{productName}</div>
                                    <div style=""color: #8C5E3C; font-family: monospace; font-size: 11px; font-weight: bold; margin-top: 3px;"">Product ID: {prodIdText}</div>
                                    <div style=""color: #C79A52; font-size: 18px; margin-top: 6px; letter-spacing: 2px;"">{starsHtml} <span style=""font-size: 13px; color: #666;"">({rating} / 5 Stars)</span></div>
                                </td>
                            </tr>
                        </table>
                        {(string.IsNullOrEmpty(comment) ? "" : $@"
                        <div style=""margin-top: 18px; padding: 15px; background: #FFFFFF; border-left: 3px solid #C79A52; border-radius: 4px; font-style: italic; color: #444; font-size: 14px; line-height: 1.5;"">
                            &ldquo;{comment}&rdquo;
                        </div>")}
                    </td>
                </tr>
            </table>");

            var actionHtml = $@"
            <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""margin-top: 15px;"">
                <tr>
                    <td align=""center"">
                        <a href=""{SiteBaseUrl}/products.html"" target=""_blank"" style=""display: inline-block; background-color: #4A1515; color: #ffffff !important; text-decoration: none !important; padding: 14px 35px; border-radius: 4px; font-weight: 600; font-size: 13px; letter-spacing: 2px; text-transform: uppercase; border: 1px solid #C79A52;""><span style=""color: #ffffff !important; text-decoration: none !important; font-weight: 600;"">Shop New Arrivals</span></a>
                    </td>
                </tr>
            </table>";

            string message = $"Thank you for reviewing your purchase from Order <strong>{orderNumber}</strong>. Your feedback inspires our artisans to maintain the highest standards of luxury leathercraft.";
            return BaseHtml("Thank You For Your Review", customerName, message, actionHtml, sb.ToString());
        }

        // 5. Admin Review Alert Email
        public static string BuildAdminReviewAlertEmail(
            string orderNumber, 
            string customerName, 
            string customerEmail, 
            string productName, 
            string? productId, 
            string? thumbnail, 
            int rating, 
            string? comment)
        {
            var imgUrl = EnsureAbsoluteUrl(thumbnail);
            var prodIdText = !string.IsNullOrEmpty(productId) ? productId : "LLA-PRODUCT";
            var starsHtml = new string('★', Math.Clamp(rating, 1, 5)) + new string('☆', 5 - Math.Clamp(rating, 1, 5));

            var sb = new StringBuilder();

            // Order ID on top banner
            sb.Append($@"
            <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""margin-bottom: 25px; background-color: #FDFBF8; border: 1.5px solid #EAE2D5; border-radius: 6px; padding: 16px;"">
                <tr>
                    <td>
                        <div style=""font-size: 11px; color: #888888; text-transform: uppercase; letter-spacing: 1.5px; font-weight: 600;"">ORDER ID</div>
                        <div style=""font-size: 15px; color: #8C5E3C; font-weight: bold; font-family: monospace; margin-top: 3px;"">{orderNumber}</div>
                    </td>
                </tr>
            </table>");

            // Review Details Box
            sb.Append($@"
            <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""margin: 20px 0 30px 0; border: 1px solid #EAEAEA; border-radius: 6px; background-color: #FAFAFA; overflow: hidden;"">
                <tr>
                    <td style=""padding: 25px;"">
                        <table width=""100%"" cellpadding=""0"" cellspacing=""0"">
                            <tr>
                                <td style=""width: 70px; vertical-align: top;"">
                                    <img src=""{imgUrl}"" alt=""{productName}"" width=""60"" height=""60"" style=""object-fit: cover; border-radius: 4px; border: 1px solid #E2D7C5; display: block;"">
                                </td>
                                <td style=""padding-left: 15px; vertical-align: top;"">
                                    <div style=""color: #111111; font-weight: 600; font-size: 15px;"">{productName}</div>
                                    <div style=""color: #8C5E3C; font-family: monospace; font-size: 11px; font-weight: bold; margin-top: 3px;"">Product ID: {prodIdText}</div>
                                    <div style=""color: #C79A52; font-size: 18px; margin-top: 6px; letter-spacing: 2px;"">{starsHtml} <span style=""font-size: 13px; color: #666;"">({rating} / 5 Stars)</span></div>
                                </td>
                            </tr>
                        </table>
                        <div style=""margin-top: 15px; font-size: 13px; color: #666;"">
                            <strong>Customer:</strong> {customerName} ({customerEmail})
                        </div>
                        {(string.IsNullOrEmpty(comment) ? "" : $@"
                        <div style=""margin-top: 15px; padding: 15px; background: #FFFFFF; border-left: 3px solid #C79A52; border-radius: 4px; font-style: italic; color: #333; font-size: 14px; line-height: 1.5;"">
                            &ldquo;{comment}&rdquo;
                        </div>")}
                    </td>
                </tr>
            </table>");

            var actionHtml = $@"
            <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""margin-top: 15px;"">
                <tr>
                    <td align=""center"">
                        <a href=""{SiteBaseUrl}/admin#reviews"" target=""_blank"" style=""display: inline-block; background-color: #4A1515; color: #ffffff !important; text-decoration: none !important; padding: 14px 35px; border-radius: 4px; font-weight: 600; font-size: 13px; letter-spacing: 2px; text-transform: uppercase; border: 1px solid #C79A52;""><span style=""color: #ffffff !important; text-decoration: none !important; font-weight: 600;"">View in Admin Dashboard</span></a>
                    </td>
                </tr>
            </table>";

            string message = $"A customer has submitted a new review for <strong>{productName}</strong> (Order <strong>{orderNumber}</strong>). Full review rating and feedback are detailed below.";
            return BaseHtml("New Product Review", "Admin", message, actionHtml, sb.ToString());
        }

        // 6. Review Reminder (Day 2 & Day 4)
        public static string BuildReviewReminderEmail(
            string customerName, 
            string orderNumber, 
            int dayNumber, 
            List<OrderItemInfo> items)
        {
            var sb = new StringBuilder();

            // Order ID on top banner
            sb.Append($@"
            <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""margin-bottom: 25px; background-color: #FDFBF8; border: 1.5px solid #EAE2D5; border-radius: 6px; padding: 16px;"">
                <tr>
                    <td>
                        <div style=""font-size: 11px; color: #888888; text-transform: uppercase; letter-spacing: 1.5px; font-weight: 600;"">ORDER ID</div>
                        <div style=""font-size: 15px; color: #8C5E3C; font-weight: bold; font-family: monospace; margin-top: 3px;"">{orderNumber}</div>
                    </td>
                </tr>
            </table>");

            string headline = dayNumber == 2 
                ? $"We hope you are delighted with your handcrafted piece for Order <strong>{orderNumber}</strong>. We would be deeply honored if you could take a brief moment to share your review." 
                : $"Your feedback guides our master craftsmen. Please take a brief moment to rate your handcrafted items for Order <strong>{orderNumber}</strong>.";

            sb.Append($@"
            <p style=""line-height: 1.65; color: #444444; font-size: 15px;"">
                {headline}
            </p>
            <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""margin: 25px 0; border: 1px solid #EAEAEA; border-radius: 6px; background-color: #FAFAFA; overflow: hidden;"">
                <tr>
                    <td style=""padding: 22px;"">
                        <h3 style=""margin: 0 0 15px 0; font-size: 12px; color: #4A1515; text-transform: uppercase; letter-spacing: 2px; border-bottom: 1.5px solid #C79A52; padding-bottom: 8px; display: inline-block;"">YOUR DELIVERED PIECES</h3>
                        <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""font-size: 14px;"">");

            foreach (var item in items)
            {
                var imgUrl = EnsureAbsoluteUrl(item.Thumbnail);
                sb.Append($@"
                            <tr>
                                <td style=""padding: 10px 0; border-bottom: 1px solid #F0F0F0; width: 60px; vertical-align: middle;"">
                                    <img src=""{imgUrl}"" alt=""{item.Name}"" width=""50"" height=""50"" style=""object-fit: cover; border-radius: 4px; border: 1px solid #E2D7C5; display: block;"">
                                </td>
                                <td style=""padding: 10px 10px; border-bottom: 1px solid #F0F0F0; vertical-align: middle;"">
                                    <div style=""color: #111111; font-weight: 600; font-size: 14px;"">{item.Name}</div>
                                    <div style=""color: #8C5E3C; font-family: monospace; font-size: 11px; font-weight: bold; margin-top: 2px;"">Product ID: {item.ProductId ?? "N/A"}</div>
                                </td>
                            </tr>");
            }

            sb.Append(@"
                        </table>
                    </td>
                </tr>
            </table>");

            var actionHtml = $@"
            <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""margin-top: 15px;"">
                <tr>
                    <td align=""center"">
                        <a href=""{SiteBaseUrl}/orders.html"" target=""_blank"" style=""display: inline-block; background-color: #C79A52; color: #111111 !important; text-decoration: none !important; padding: 14px 35px; border-radius: 4px; font-weight: bold; font-size: 13px; letter-spacing: 2px; text-transform: uppercase;""><span style=""color: #111111 !important; text-decoration: none !important; font-weight: bold;"">&starf;&starf;&starf;&starf;&starf; Write a Review &amp; Rate Product</span></a>
                    </td>
                </tr>
            </table>";

            return BaseHtml("Share Your Experience", customerName, $"How are your handcrafted pieces performing? (Order #{orderNumber})", actionHtml, sb.ToString());
        }

        // 7. Exchange Lifecycle Email (Submitted, Approved, Return Shipped, Replacement Shipped, Failed, Completed)
        public static string BuildExchangeEmail(
            string title, 
            string customerName, 
            string exchangeCode, 
            string orderNumber, 
            string status, 
            string message, 
            string originalProductName, 
            string? originalProductId, 
            string? originalThumbnail, 
            string? replacementProductName, 
            string? replacementProductId, 
            string? replacementThumbnail, 
            string reason, 
            string actionUrl, 
            string actionText, 
            string? courierName = null, 
            string? trackingNumber = null, 
            string? rejectedReason = null)
        {
            var sb = new StringBuilder();

            // Order ID & Exchange ID ON TOP
            sb.Append($@"
            <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""margin-bottom: 25px; background-color: #FDFBF8; border: 1.5px solid #EAE2D5; border-radius: 6px; padding: 16px;"">
                <tr>
                    <td style=""padding: 6px 12px; vertical-align: middle;"">
                        <div style=""font-size: 11px; color: #888888; text-transform: uppercase; letter-spacing: 1.5px; font-weight: 600;"">ORDER ID</div>
                        <div style=""font-size: 15px; color: #8C5E3C; font-weight: bold; font-family: monospace; margin-top: 3px;"">{orderNumber}</div>
                    </td>
                    <td style=""padding: 6px 12px; border-left: 1px solid #EAE2D5; vertical-align: middle;"">
                        <div style=""font-size: 11px; color: #888888; text-transform: uppercase; letter-spacing: 1.5px; font-weight: 600;"">EXCHANGE ID</div>
                        <div style=""font-size: 15px; color: #4A1515; font-weight: bold; font-family: monospace; margin-top: 3px;"">{exchangeCode}</div>
                    </td>
                    <td style=""padding: 6px 12px; border-left: 1px solid #EAE2D5; vertical-align: middle;"">
                        <div style=""font-size: 11px; color: #888888; text-transform: uppercase; letter-spacing: 1.5px; font-weight: 600;"">STATUS</div>
                        <div style=""font-size: 13px; color: #2E6B4D; font-weight: bold; margin-top: 3px;"">{status}</div>
                    </td>
                </tr>
            </table>");

            // Courier / Tracking Details (if available)
            if (!string.IsNullOrEmpty(courierName) || !string.IsNullOrEmpty(trackingNumber))
            {
                sb.Append($@"
                <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""margin-bottom: 25px; background-color: #F4F8F5; border: 1.5px solid #C8E0D1; border-radius: 6px; padding: 16px;"">
                    <tr>
                        <td style=""padding: 6px 12px;"">
                            <div style=""font-size: 11px; color: #2E6B4D; text-transform: uppercase; letter-spacing: 1.5px; font-weight: bold;"">COURIER SERVICE</div>
                            <div style=""font-size: 14px; color: #111111; font-weight: 600; margin-top: 3px;"">{courierName}</div>
                        </td>
                        <td style=""padding: 6px 12px; border-left: 1px solid #C8E0D1;"">
                            <div style=""font-size: 11px; color: #2E6B4D; text-transform: uppercase; letter-spacing: 1.5px; font-weight: bold;"">TRACKING NUMBER</div>
                            <div style=""font-size: 14px; color: #8C5E3C; font-weight: bold; font-family: monospace; margin-top: 3px;"">{trackingNumber}</div>
                        </td>
                    </tr>
                </table>");
            }

            // Reason / Rejection Details
            if (!string.IsNullOrEmpty(rejectedReason))
            {
                sb.Append($@"
                <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""margin-bottom: 25px; background-color: #FFF5F5; border: 1.5px solid #F5C6CB; border-radius: 6px; padding: 16px;"">
                    <tr>
                        <td>
                            <div style=""font-size: 11px; color: #721C24; text-transform: uppercase; letter-spacing: 1.5px; font-weight: bold; margin-bottom: 5px;"">INSPECTION / REJECTION REASON</div>
                            <div style=""font-size: 14px; color: #721C24; line-height: 1.5;"">{rejectedReason}</div>
                        </td>
                    </tr>
                </table>");
            }

            // Products Comparison Box
            var origImg = EnsureAbsoluteUrl(originalThumbnail);
            var repImg = EnsureAbsoluteUrl(replacementThumbnail);

            sb.Append($@"
            <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""margin: 20px 0 30px 0; border: 1px solid #EAEAEA; border-radius: 6px; background-color: #FAFAFA; overflow: hidden;"">
                <tr>
                    <td style=""padding: 25px;"">
                        <h3 style=""margin: 0 0 15px 0; font-size: 12px; color: #4A1515; text-transform: uppercase; letter-spacing: 2px; border-bottom: 1.5px solid #C79A52; padding-bottom: 8px; display: inline-block;"">EXCHANGE DOSSIER</h3>
                        <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""font-size: 14px;"">
                            <tr>
                                <td style=""padding: 12px 0; border-bottom: 1px solid #F0F0F0; width: 60px; vertical-align: middle;"">
                                    <img src=""{origImg}"" alt=""{originalProductName}"" width=""50"" height=""50"" style=""object-fit: cover; border-radius: 4px; border: 1px solid #E2D7C5; display: block;"">
                                </td>
                                <td style=""padding: 12px 10px; border-bottom: 1px solid #F0F0F0; vertical-align: middle;"">
                                    <div style=""color: #777; font-size: 11px; text-transform: uppercase; letter-spacing: 1px;"">ORIGINAL ITEM</div>
                                    <div style=""color: #111111; font-weight: 600; font-size: 14px;"">{originalProductName}</div>
                                    <div style=""color: #8C5E3C; font-family: monospace; font-size: 11px; font-weight: bold; margin-top: 2px;"">Product ID: {originalProductId ?? "N/A"}</div>
                                </td>
                            </tr>
                            <tr>
                                <td style=""padding: 12px 0; border-bottom: 1px solid #F0F0F0; width: 60px; vertical-align: middle;"">
                                    <img src=""{repImg}"" alt=""{replacementProductName ?? originalProductName}"" width=""50"" height=""50"" style=""object-fit: cover; border-radius: 4px; border: 1px solid #E2D7C5; display: block;"">
                                </td>
                                <td style=""padding: 12px 10px; border-bottom: 1px solid #F0F0F0; vertical-align: middle;"">
                                    <div style=""color: #777; font-size: 11px; text-transform: uppercase; letter-spacing: 1px;"">REPLACEMENT PIECE</div>
                                    <div style=""color: #111111; font-weight: 600; font-size: 14px;"">{replacementProductName ?? originalProductName}</div>
                                    <div style=""color: #8C5E3C; font-family: monospace; font-size: 11px; font-weight: bold; margin-top: 2px;"">Product ID: {replacementProductId ?? originalProductId ?? "N/A"}</div>
                                </td>
                            </tr>
                            <tr>
                                <td style=""padding: 12px 0; color: #666; font-size: 12px; text-transform: uppercase;"">Reason</td>
                                <td style=""padding: 12px 10px; color: #111; font-weight: 600; text-align: right;"">{reason}</td>
                            </tr>
                        </table>
                    </td>
                </tr>
            </table>");

            string actionBtnHtml = "";
            if (!string.IsNullOrEmpty(actionUrl))
            {
                if (actionUrl.StartsWith("/")) actionUrl = SiteBaseUrl + actionUrl;
                actionBtnHtml = $@"
                <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""margin-top: 15px;"">
                    <tr>
                        <td align=""center"">
                            <a href=""{actionUrl}"" target=""_blank"" style=""display: inline-block; background-color: #4A1515; color: #ffffff !important; text-decoration: none !important; padding: 14px 35px; border-radius: 4px; font-weight: 600; font-size: 13px; letter-spacing: 2px; text-transform: uppercase; border: 1px solid #C79A52;""><span style=""color: #ffffff !important; text-decoration: none !important; font-weight: 600;"">{actionText}</span></a>
                        </td>
                    </tr>
                </table>";
            }

            return BaseHtml(title, customerName, message, actionBtnHtml, sb.ToString());
        }

        // 8. Standard Fallback Template
        public static string BuildStandardEmail(
            string title, 
            string customerName, 
            string message, 
            string? actionUrl = null, 
            string actionText = "View Details", 
            Dictionary<string, string>? details = null, 
            string detailsTitle = "Transaction Details")
        {
            string detailsHtml = "";
            if (details != null && details.Count > 0)
            {
                var sb = new StringBuilder();
                sb.Append($@"
                <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""margin: 25px 0; border: 1px solid #EAEAEA; border-radius: 6px; background-color: #FAFAFA; overflow: hidden;"">
                    <tr>
                        <td style=""padding: 25px;"">
                            <h3 style=""margin: 0 0 20px 0; font-size: 12px; color: #4A1515; text-transform: uppercase; letter-spacing: 2px; border-bottom: 1.5px solid #C79A52; padding-bottom: 8px; display: inline-block;"">{detailsTitle}</h3>
                            <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""font-size: 14px;"">");
                
                int index = 0;
                foreach (var detail in details)
                {
                    bool isLast = (index == details.Count - 1);
                    string borderStyle = isLast ? "" : "border-bottom: 1px solid #F0F0F0;";
                    string padTop = index == 0 ? "padding: 0 0 12px 0;" : "padding: 12px 0;";
                    if (isLast) padTop = "padding-top: 12px;";

                    sb.Append($@"
                                <tr>
                                    <td style=""{padTop} {borderStyle} color: #777777; font-size: 11px; text-transform: uppercase; letter-spacing: 1px; width: 45%;"">{detail.Key}</td>
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
                if (actionUrl.StartsWith("/")) actionUrl = SiteBaseUrl + actionUrl;
                actionHtml = $@"
                <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""margin-top: 15px;"">
                    <tr>
                        <td align=""center"">
                            <a href=""{actionUrl}"" target=""_blank"" style=""display: inline-block; background-color: #4A1515; color: #ffffff !important; text-decoration: none !important; padding: 14px 35px; border-radius: 4px; font-weight: 600; font-size: 13px; letter-spacing: 2px; text-transform: uppercase; border: 1px solid #C79A52;""><span style=""color: #ffffff !important; text-decoration: none !important; font-weight: 600;"">{actionText}</span></a>
                        </td>
                    </tr>
                </table>";
            }

            return BaseHtml(title, customerName, message, actionHtml, detailsHtml);
        }

        // Password Reset Email
        public static string BuildPasswordResetEmail(string recipientName, string resetUrl)
        {
            var bodyContent = @"
                <p style=""margin: 0 0 15px 0; color: #333333; font-size: 15px; line-height: 1.6;"">
                    We received a request to reset the password associated with your <strong>LeatherLane Atelier</strong> account.
                </p>
                <p style=""margin: 0 0 20px 0; color: #555555; font-size: 14px; line-height: 1.6;"">
                    Click the secure button below to choose a new password and regain access to your account:
                </p>
            ";

            var customBoxHtml = @"
                <div style=""margin: 20px 0; padding: 15px 20px; background-color: #FFF9F0; border-left: 4px solid #C79A52; border-radius: 4px; font-size: 13px; color: #665544; line-height: 1.5;"">
                    <strong style=""color: #4A1515;"">Security Notice:</strong> This password reset link is valid for <strong>15 minutes</strong>. If you did not request a password reset, you can safely ignore this email &mdash; your password will remain unchanged.
                </div>
            ";

            var actionButtonHtml = $@"
                <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""margin-top: 25px; margin-bottom: 10px;"">
                    <tr>
                        <td align=""center"">
                            <a href=""{resetUrl}"" target=""_blank"" style=""display: inline-block; background-color: #4A1515; color: #ffffff !important; text-decoration: none !important; padding: 15px 40px; border-radius: 4px; font-weight: 600; font-size: 13px; letter-spacing: 2px; text-transform: uppercase; border: 1px solid #C79A52; box-shadow: 0 4px 12px rgba(74, 21, 21, 0.2);""><span style=""color: #ffffff !important; text-decoration: none !important; font-weight: 600; font-size: 13px; letter-spacing: 2px;"">Reset Your Password</span></a>
                        </td>
                    </tr>
                </table>
            ";

            return BaseHtml("Reset Your Password", recipientName, bodyContent, actionButtonHtml, customBoxHtml);
        }

        // Admin Exchange Notification Dossier
        public static string BuildAdminExchangeEmail(
            string title,
            string exchangeCode,
            string orderNumber,
            string customerName,
            string customerEmail,
            string status,
            string message,
            string originalProductName,
            string? originalProductId,
            string? originalThumbnail,
            string? replacementProductName,
            string? replacementProductId,
            string? replacementThumbnail,
            string reason,
            string actionUrl,
            string actionText = "Review Exchange in Dashboard",
            string? courierName = null,
            string? trackingNumber = null)
        {
            var sb = new StringBuilder();

            // Top Banner: Order ID, Exchange ID, Status
            sb.Append($@"
            <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""margin-bottom: 25px; background-color: #FDFBF8; border: 1.5px solid #EAE2D5; border-radius: 6px; padding: 16px;"">
                <tr>
                    <td style=""padding: 6px 12px; vertical-align: middle;"">
                        <div style=""font-size: 11px; color: #888888; text-transform: uppercase; letter-spacing: 1.5px; font-weight: 600;"">ORDER ID</div>
                        <div style=""font-size: 15px; color: #8C5E3C; font-weight: bold; font-family: monospace; margin-top: 3px;"">{orderNumber}</div>
                    </td>
                    <td style=""padding: 6px 12px; border-left: 1px solid #EAE2D5; vertical-align: middle;"">
                        <div style=""font-size: 11px; color: #888888; text-transform: uppercase; letter-spacing: 1.5px; font-weight: 600;"">EXCHANGE CODE</div>
                        <div style=""font-size: 15px; color: #4A1515; font-weight: bold; font-family: monospace; margin-top: 3px;"">{exchangeCode}</div>
                    </td>
                    <td style=""padding: 6px 12px; border-left: 1px solid #EAE2D5; vertical-align: middle;"">
                        <div style=""font-size: 11px; color: #888888; text-transform: uppercase; letter-spacing: 1.5px; font-weight: 600;"">STATUS</div>
                        <div style=""font-size: 13px; color: #2E6B4D; font-weight: bold; margin-top: 3px;"">{status}</div>
                    </td>
                </tr>
            </table>");

            // Customer Contact Card
            sb.Append($@"
            <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""margin-bottom: 25px; background-color: #FAFAFA; border: 1px solid #EAEAEA; border-radius: 6px; padding: 14px 18px;"">
                <tr>
                    <td style=""color: #666; font-size: 12px; text-transform: uppercase; width: 40%; padding: 4px 0;"">Customer Name:</td>
                    <td style=""color: #111; font-weight: bold; font-size: 13px; padding: 4px 0;"">{customerName}</td>
                </tr>
                <tr>
                    <td style=""color: #666; font-size: 12px; text-transform: uppercase; padding: 4px 0;"">Customer Email:</td>
                    <td style=""color: #111; font-weight: bold; font-size: 13px; padding: 4px 0;"">{customerEmail}</td>
                </tr>
            </table>");

            // Courier / Tracking Details (if available)
            if (!string.IsNullOrEmpty(courierName) || !string.IsNullOrEmpty(trackingNumber))
            {
                sb.Append($@"
                <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""margin-bottom: 25px; background-color: #F4F8F5; border: 1.5px solid #C8E0D1; border-radius: 6px; padding: 16px;"">
                    <tr>
                        <td style=""padding: 6px 12px;"">
                            <div style=""font-size: 11px; color: #2E6B4D; text-transform: uppercase; letter-spacing: 1.5px; font-weight: bold;"">DISPATCH COURIER</div>
                            <div style=""font-size: 14px; color: #111111; font-weight: 600; margin-top: 3px;"">{courierName}</div>
                        </td>
                        <td style=""padding: 6px 12px; border-left: 1px solid #C8E0D1;"">
                            <div style=""font-size: 11px; color: #2E6B4D; text-transform: uppercase; letter-spacing: 1.5px; font-weight: bold;"">TRACKING NUMBER</div>
                            <div style=""font-size: 14px; color: #8C5E3C; font-weight: bold; font-family: monospace; margin-top: 3px;"">{trackingNumber}</div>
                        </td>
                    </tr>
                </table>");
            }

            // Products Comparison Box
            var origImg = EnsureAbsoluteUrl(originalThumbnail);
            var repImg = EnsureAbsoluteUrl(replacementThumbnail);

            sb.Append($@"
            <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""margin: 20px 0 30px 0; border: 1px solid #EAEAEA; border-radius: 6px; background-color: #FAFAFA; overflow: hidden;"">
                <tr>
                    <td style=""padding: 25px;"">
                        <h3 style=""margin: 0 0 15px 0; font-size: 12px; color: #4A1515; text-transform: uppercase; letter-spacing: 2px; border-bottom: 1.5px solid #C79A52; padding-bottom: 8px; display: inline-block;"">EXCHANGE PRODUCT DETAILS</h3>
                        <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""font-size: 14px;"">
                            <tr>
                                <td style=""padding: 12px 0; border-bottom: 1px solid #F0F0F0; width: 60px; vertical-align: middle;"">
                                    <img src=""{origImg}"" alt=""{originalProductName}"" width=""50"" height=""50"" style=""object-fit: cover; border-radius: 4px; border: 1px solid #E2D7C5; display: block;"">
                                </td>
                                <td style=""padding: 12px 10px; border-bottom: 1px solid #F0F0F0; vertical-align: middle;"">
                                    <div style=""color: #777; font-size: 11px; text-transform: uppercase; letter-spacing: 1px;"">ORIGINAL ITEM TO RETURN</div>
                                    <div style=""color: #111111; font-weight: 600; font-size: 14px;"">{originalProductName}</div>
                                    <div style=""color: #8C5E3C; font-family: monospace; font-size: 11px; font-weight: bold; margin-top: 2px;"">Product ID: {originalProductId ?? "N/A"}</div>
                                </td>
                            </tr>
                            <tr>
                                <td style=""padding: 12px 0; border-bottom: 1px solid #F0F0F0; width: 60px; vertical-align: middle;"">
                                    <img src=""{repImg}"" alt=""{replacementProductName ?? originalProductName}"" width=""50"" height=""50"" style=""object-fit: cover; border-radius: 4px; border: 1px solid #E2D7C5; display: block;"">
                                </td>
                                <td style=""padding: 12px 10px; border-bottom: 1px solid #F0F0F0; vertical-align: middle;"">
                                    <div style=""color: #777; font-size: 11px; text-transform: uppercase; letter-spacing: 1px;"">REQUESTED REPLACEMENT PIECE</div>
                                    <div style=""color: #111111; font-weight: 600; font-size: 14px;"">{replacementProductName ?? originalProductName}</div>
                                    <div style=""color: #8C5E3C; font-family: monospace; font-size: 11px; font-weight: bold; margin-top: 2px;"">Product ID: {replacementProductId ?? originalProductId ?? "N/A"}</div>
                                </td>
                            </tr>
                            <tr>
                                <td style=""padding: 12px 0; color: #666; font-size: 12px; text-transform: uppercase;"">Customer Reason</td>
                                <td style=""padding: 12px 10px; color: #111; font-weight: 600; text-align: right;"">{reason}</td>
                            </tr>
                        </table>
                    </td>
                </tr>
            </table>");

            string actionBtnHtml = "";
            if (!string.IsNullOrEmpty(actionUrl))
            {
                if (actionUrl.StartsWith("/")) actionUrl = SiteBaseUrl + actionUrl;
                actionBtnHtml = $@"
                <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""margin-top: 15px;"">
                    <tr>
                        <td align=""center"">
                            <a href=""{actionUrl}"" target=""_blank"" style=""display: inline-block; background-color: #4A1515; color: #ffffff !important; text-decoration: none !important; padding: 14px 35px; border-radius: 4px; font-weight: 600; font-size: 13px; letter-spacing: 2px; text-transform: uppercase; border: 1px solid #C79A52;""><span style=""color: #ffffff !important; text-decoration: none !important; font-weight: 600;"">{actionText}</span></a>
                        </td>
                    </tr>
                </table>";
            }

            return BaseHtml(title, "Admin", message, actionBtnHtml, sb.ToString());
        }
    }

    public static class EmailServiceExtensions
    {
        public static async System.Threading.Tasks.Task NotifyAdminsHtmlAsync(this IEmailService emailService, LeatherLane_Atelier.Models.ApplicationDbContext context, string subject, string htmlBody)
        {
            var adminEmails = await context.Users
                .Where(u => u.Role == "Admin")
                .Select(u => u.Email)
                .ToListAsync();
            
            var allAdminEmails = new HashSet<string>(adminEmails, StringComparer.OrdinalIgnoreCase);

            var siteSettings = await context.SiteSettings.AsNoTracking().FirstOrDefaultAsync();
            if (siteSettings != null && !string.IsNullOrWhiteSpace(siteSettings.Email))
            {
                allAdminEmails.Add(siteSettings.Email.Trim());
            }

            allAdminEmails.Add("leatherlaneatelier@gmail.com");

            foreach (var email in allAdminEmails)
            {
                if (string.IsNullOrWhiteSpace(email)) continue;
                _ = emailService.SendEmailAsync(email, subject, htmlBody);
            }
        }

        public static async System.Threading.Tasks.Task NotifyAdminsAsync(this IEmailService emailService, LeatherLane_Atelier.Models.ApplicationDbContext context, string subject, string body, Dictionary<string, string>? details = null, string detailsTitle = "Alert Details")
        {
            var adminEmails = await context.Users
                .Where(u => u.Role == "Admin")
                .Select(u => u.Email)
                .ToListAsync();
            
            var allAdminEmails = new HashSet<string>(adminEmails, StringComparer.OrdinalIgnoreCase);

            var siteSettings = await context.SiteSettings.AsNoTracking().FirstOrDefaultAsync();
            if (siteSettings != null && !string.IsNullOrWhiteSpace(siteSettings.Email))
            {
                allAdminEmails.Add(siteSettings.Email.Trim());
            }

            allAdminEmails.Add("leatherlaneatelier@gmail.com");

            foreach (var email in allAdminEmails)
            {
                if (string.IsNullOrWhiteSpace(email)) continue;
                var htmlBody = EmailTemplateBuilder.BuildStandardEmail(subject, "Admin", body, "/admin.html", "Go to Dashboard", details, detailsTitle);
                _ = emailService.SendEmailAsync(email, subject, htmlBody);
            }
        }
    }
}
