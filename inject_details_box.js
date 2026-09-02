const fs = require('fs');

const replacements = [
    {
        file: 'back/Controllers/AdminReturnController.cs',
        search: /_\s*=\s*_emailService\.SendEmailAsync\(customer\.Email,\s*"Return Approved",\s*\$?"Your return request for Order #\{req\.OrderId\} has been approved\."\);/g,
        replace: `var details = new System.Collections.Generic.Dictionary<string, string> {
                    { "Order No.", $"#{req.OrderId}" },
                    { "Status", "Return Approved" }
                };
                var htmlEmail = LeatherLane_Atelier.Services.EmailTemplateBuilder.BuildStandardEmail("Return Approved", customer.Name, "Your return request has been approved. A pickup will be scheduled soon.", $"/return-tracking?id={id}", "Track Return", details);
                _ = _emailService.SendEmailAsync(customer.Email, "Return Approved", htmlEmail);`
    },
    {
        file: 'back/Controllers/AdminReturnController.cs',
        search: /_\s*=\s*_emailService\.SendEmailAsync\(customer\.Email,\s*"Return Rejected",\s*\$?"Your return request for Order #\{req\.OrderId\} was rejected\. Reason: \{dto\.Reason\}"\);/g,
        replace: `var details = new System.Collections.Generic.Dictionary<string, string> {
                    { "Order No.", $"#{req.OrderId}" },
                    { "Reason", dto.Reason },
                    { "Status", "Rejected" }
                };
                var htmlEmail = LeatherLane_Atelier.Services.EmailTemplateBuilder.BuildStandardEmail("Return Rejected", customer.Name, "Your return request has been rejected.", $"/return-tracking?id={id}", "Track Return", details);
                _ = _emailService.SendEmailAsync(customer.Email, "Return Rejected", htmlEmail);`
    },
    {
        file: 'back/Controllers/AdminReturnController.cs',
        search: /_\s*=\s*_emailService\.SendEmailAsync\(customer\.Email,\s*"Return Inspection Failed",\s*\$?"Your returned item for Order #\{req\.OrderId\} failed inspection\. Reason: \{dto\.Reason\}"\);/g,
        replace: `var details = new System.Collections.Generic.Dictionary<string, string> {
                    { "Order No.", $"#{req.OrderId}" },
                    { "Reason", dto.Reason },
                    { "Status", "Inspection Failed" }
                };
                var htmlEmail = LeatherLane_Atelier.Services.EmailTemplateBuilder.BuildStandardEmail("Return Inspection Failed", customer.Name, "Your returned item failed inspection.", $"/return-tracking?id={id}", "Track Return", details);
                _ = _emailService.SendEmailAsync(customer.Email, "Return Inspection Failed", htmlEmail);`
    },
    {
        file: 'back/Controllers/AdminReturnController.cs',
        search: /_\s*=\s*_emailService\.SendEmailAsync\(customer\.Email,\s*"Refund Completed",\s*\$?"Your refund of \\\$\{req\.RefundAmount\} for Order #\{req\.OrderId\} has been successfully processed\."\);/g,
        replace: `var details = new System.Collections.Generic.Dictionary<string, string> {
                    { "Order No.", $"#{req.OrderId}" },
                    { "Refund Amount", $"Rs. {req.RefundAmount:N2}" },
                    { "Status", "Refund Processed" }
                };
                var htmlEmail = LeatherLane_Atelier.Services.EmailTemplateBuilder.BuildStandardEmail("Refund Completed", customer.Name, "Your refund has been successfully processed.", $"/return-tracking?id={id}", "View Return Details", details);
                _ = _emailService.SendEmailAsync(customer.Email, "Refund Completed", htmlEmail);`
    },
    {
        file: 'back/Controllers/AdminExchangeController.cs',
        search: /_\s*=\s*_emailService\.SendEmailAsync\(customer\.Email,\s*"Exchange Inspection Passed",\s*\$?"Good news! Your returned item for Order #\{request\.OrderId\} has arrived and passed inspection\. We are packing your replacement now\."\);/g,
        replace: `var details = new System.Collections.Generic.Dictionary<string, string> {
                    { "Order No.", $"#{request.OrderId}" },
                    { "Status", "Inspection Passed" }
                };
                var htmlEmail = LeatherLane_Atelier.Services.EmailTemplateBuilder.BuildStandardEmail("Exchange Inspection Passed", customer.Name, "Good news! Your returned item has arrived and passed inspection. We are packing your replacement now.", $"/exchange-tracking?id={id}", "Track Replacement", details);
                _ = _emailService.SendEmailAsync(customer.Email, "Exchange Inspection Passed", htmlEmail);`
    },
    {
        file: 'back/Controllers/AdminExchangeController.cs',
        search: /_\s*=\s*_emailService\.SendEmailAsync\(customer\.Email,\s*"Exchange Inspection Failed",\s*\$?"Your returned item for Order #\{request\.OrderId\} was received but failed inspection\. Reason: \{dto\.Reason\}"\);/g,
        replace: `var details = new System.Collections.Generic.Dictionary<string, string> {
                    { "Order No.", $"#{request.OrderId}" },
                    { "Reason", dto.Reason },
                    { "Status", "Inspection Failed" }
                };
                var htmlEmail = LeatherLane_Atelier.Services.EmailTemplateBuilder.BuildStandardEmail("Exchange Inspection Failed", customer.Name, "Your returned item was received but failed inspection.", $"/exchange-tracking?id={id}", "Track Status", details);
                _ = _emailService.SendEmailAsync(customer.Email, "Exchange Inspection Failed", htmlEmail);`
    },
    {
        file: 'back/Controllers/AdminExchangeController.cs',
        search: /_\s*=\s*_emailService\.SendEmailAsync\(customer\.Email,\s*"Replacement Shipped",\s*\$?"Your replacement item has shipped\. Tracking: \{dto\.TrackingNumber\}"\);/g,
        replace: `var details = new System.Collections.Generic.Dictionary<string, string> {
                    { "Tracking Number", dto.TrackingNumber },
                    { "Status", "Shipped" }
                };
                var htmlEmail = LeatherLane_Atelier.Services.EmailTemplateBuilder.BuildStandardEmail("Replacement Shipped", customer.Name, "Your replacement item has shipped.", $"/exchange-tracking?id={id}", "Track Package", details);
                _ = _emailService.SendEmailAsync(customer.Email, "Replacement Shipped", htmlEmail);`
    },
    {
        file: 'back/Controllers/AdminExchangeController.cs',
        search: /_\s*=\s*_emailService\.SendEmailAsync\(customer\.Email,\s*"Replacement Delivered",\s*\$?"Your replacement item for Order #\{request\.OrderId\} has been delivered\. Thank you for your patience\."\);/g,
        replace: `var details = new System.Collections.Generic.Dictionary<string, string> {
                    { "Order No.", $"#{request.OrderId}" },
                    { "Status", "Delivered" }
                };
                var htmlEmail = LeatherLane_Atelier.Services.EmailTemplateBuilder.BuildStandardEmail("Replacement Delivered", customer.Name, "Your replacement item has been delivered. Thank you for your patience.", $"/exchange-tracking?id={id}", "View Details", details);
                _ = _emailService.SendEmailAsync(customer.Email, "Replacement Delivered", htmlEmail);`
    },
    {
        file: 'back/Controllers/AdminOrderController.cs',
        search: /_\s*=\s*_emailService\.SendEmailAsync\(customer\.Email,\s*\$"Order \{dto\.Status\}",\s*message\);/g,
        replace: `var details = new System.Collections.Generic.Dictionary<string, string> {
                    { "Order No.", $"#{id}" },
                    { "Status", dto.Status }
                };
                var htmlEmail = LeatherLane_Atelier.Services.EmailTemplateBuilder.BuildStandardEmail($"Order {dto.Status}", customer.Name, message, $"/order-tracking?id={id}", "Track Order", details);
                _ = _emailService.SendEmailAsync(customer.Email, $"Order {dto.Status}", htmlEmail);`
    },
    {
        file: 'back/Controllers/AdminOrderController.cs',
        search: /_\s*=\s*_emailService\.SendEmailAsync\(customer\.Email,\s*"We'd Love Your Review!",\s*reviewMsg\);/g,
        replace: `var htmlEmail = LeatherLane_Atelier.Services.EmailTemplateBuilder.BuildStandardEmail("We'd Love Your Review!", customer.Name, reviewMsg, "/transactions", "Write a Review");
                        _ = _emailService.SendEmailAsync(customer.Email, "We'd Love Your Review!", htmlEmail);`
    },
    {
        file: 'back/Controllers/ExchangeController.cs',
        search: /_\s*=\s*_emailService\.SendEmailAsync\(userObj\.Email,\s*"Exchange Request Submitted",\s*\$?"We have received your exchange request for Order #\{order\.Id\}\. Our team will review it shortly\."\);/g,
        replace: `var details = new System.Collections.Generic.Dictionary<string, string> {
                    { "Order No.", $"#{order.Id}" },
                    { "Status", "Request Submitted" }
                };
                var htmlEmail = LeatherLane_Atelier.Services.EmailTemplateBuilder.BuildStandardEmail("Exchange Request Submitted", userObj.Name, "We have received your exchange request. Our team will review it shortly.", $"/exchange-tracking?id={exchangeRequest.Id}", "Track Status", details);
                _ = _emailService.SendEmailAsync(userObj.Email, "Exchange Request Submitted", htmlEmail);`
    },
    {
        file: 'back/Controllers/ExchangeController.cs',
        search: /_\s*=\s*_emailService\.SendEmailAsync\(userObj\.Email,\s*"Tracking Received",\s*\$?"We have received your return tracking details: \{dto\.CourierName\} \{dto\.TrackingNumber\}\. We will notify you once it passes inspection\."\);/g,
        replace: `var details = new System.Collections.Generic.Dictionary<string, string> {
                    { "Courier", dto.CourierName },
                    { "Tracking Number", dto.TrackingNumber },
                    { "Status", "Tracking Received" }
                };
                var htmlEmail = LeatherLane_Atelier.Services.EmailTemplateBuilder.BuildStandardEmail("Tracking Received", userObj.Name, "We have received your return tracking details. We will notify you once it passes inspection.", $"/exchange-tracking?id={id}", "Track Status", details);
                _ = _emailService.SendEmailAsync(userObj.Email, "Tracking Received", htmlEmail);`
    },
    {
        file: 'back/Controllers/TransactionsController.cs',
        search: /_\s*=\s*_emailService\.SendEmailAsync\(user\.Email,\s*"Order Placed - Awaiting Verification",\s*\$?"Your payment proof has been received and is awaiting verification\. Once verified, your order #\{transaction\.Id\} will automatically move to Order Confirmed\."\);/g,
        replace: `var details = new System.Collections.Generic.Dictionary<string, string> {
                    { "Order No.", $"#{transaction.Id}" },
                    { "Total Amount", $"Rs. {transaction.TotalAmount:N2}" },
                    { "Status", "Awaiting Verification" }
                };
                var htmlEmail = LeatherLane_Atelier.Services.EmailTemplateBuilder.BuildStandardEmail("Order Placed - Awaiting Verification", user.Name, "Your payment proof has been received and is awaiting verification.", $"/order-tracking?id={transaction.Id}", "Track Order", details);
                _ = _emailService.SendEmailAsync(user.Email, "Order Placed - Awaiting Verification", htmlEmail);`
    },
    {
        file: 'back/Controllers/TransactionsController.cs',
        search: /_\s*=\s*_emailService\.SendEmailAsync\(user\.Email,\s*"Payment Proof Resubmitted",\s*\$?"Your updated payment proof for Order #\{transaction\.Id\} was received and is awaiting verification\."\);/g,
        replace: `var details = new System.Collections.Generic.Dictionary<string, string> {
                    { "Order No.", $"#{transaction.Id}" },
                    { "Total Amount", $"Rs. {transaction.TotalAmount:N2}" },
                    { "Status", "Awaiting Verification" }
                };
                var htmlEmail = LeatherLane_Atelier.Services.EmailTemplateBuilder.BuildStandardEmail("Payment Proof Resubmitted", user.Name, "Your updated payment proof was received and is awaiting verification.", $"/order-tracking?id={transaction.Id}", "Track Order", details);
                _ = _emailService.SendEmailAsync(user.Email, "Payment Proof Resubmitted", htmlEmail);`
    },
    {
        file: 'back/Controllers/ProductsController.cs',
        search: /_\s*=\s*emailService\.SendEmailAsync\(userObj\.Email,\s*"Thank You For Your Review!",\s*\$?"We appreciate your \{dto\.Rating\}-star review on \{product\.Name\}\. Your feedback helps us maintain our timeless craftsmanship!"\);/g,
        replace: `var details = new System.Collections.Generic.Dictionary<string, string> {
                    { "Product", product.Name },
                    { "Rating", $"{dto.Rating} Stars" }
                };
                var htmlEmail = LeatherLane_Atelier.Services.EmailTemplateBuilder.BuildStandardEmail("Thank You For Your Review!", userObj.Name, "We appreciate your review. Your feedback helps us maintain our timeless craftsmanship!", "/products", "Shop New Arrivals", details);
                _ = emailService.SendEmailAsync(userObj.Email, "Thank You For Your Review!", htmlEmail);`
    },
    {
        file: 'back/Controllers/NewsletterController.cs',
        search: /await\s+_emailService\.SendEmailAsync\(req\.Email,\s*subject,\s*body\);/g,
        replace: `var htmlEmail = LeatherLane_Atelier.Services.EmailTemplateBuilder.BuildStandardEmail(subject, "", body, "/products", "Shop Now");
                await _emailService.SendEmailAsync(req.Email, subject, htmlEmail);`
    },
    {
        file: 'back/Controllers/NewsletterController.cs',
        search: /await\s+_emailService\.SendEmailAsync\(email,\s*req\.Subject,\s*req\.Body\);/g,
        replace: `var htmlEmail = LeatherLane_Atelier.Services.EmailTemplateBuilder.BuildStandardEmail(req.Subject, "", req.Body, "/products", "Shop Now");
                await _emailService.SendEmailAsync(email, req.Subject, htmlEmail);`
    }
];

replacements.forEach(r => {
    if (fs.existsSync(r.file)) {
        let content = fs.readFileSync(r.file, 'utf8');
        if (content.match(r.search)) {
            content = content.replace(r.search, r.replace);
            fs.writeFileSync(r.file, content, 'utf8');
            console.log("Updated: " + r.file);
        }
    }
});
