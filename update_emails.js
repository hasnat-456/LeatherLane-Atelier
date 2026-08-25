const fs = require('fs');
const path = require('path');

function replaceInFile(filePath, replacements) {
    let content = fs.readFileSync(filePath, 'utf8');
    for (let r of replacements) {
        content = content.replace(r.search, r.replace);
    }
    fs.writeFileSync(filePath, content, 'utf8');
}

// 1. TransactionsController.cs
replaceInFile('back/Controllers/TransactionsController.cs', [
    {
        search: `_ = _emailService.SendEmailAsync(userObj.Email, "Order Confirmation", $"Your order #{transaction.Id} is confirmed.");`,
        replace: `
                var orderItems = transaction.Items.Where(i => i.ProductId.HasValue).Select(i => new LeatherLane_Atelier.Services.OrderItemInfo { Name = i.ProductName, Quantity = i.Quantity, Price = i.Price }).ToList();
                var emailHtml = LeatherLane_Atelier.Services.EmailTemplateBuilder.BuildOrderEmail(
                    "🛍️ Order Received", 
                    userObj.Name, 
                    transaction.Id.ToString(), 
                    $"Thank you for shopping with LeatherLane Atelier. Your order #{transaction.Id} has been received and is awaiting payment/processing.",
                    orderItems,
                    250m, // Delivery fee mockup, ideally fetched from transaction
                    transaction.TotalAmount,
                    $"/order-tracking.html?id={transaction.Id}"
                );
                _ = _emailService.SendEmailAsync(userObj.Email, "Your Order has been received! (#" + transaction.Id + ")", emailHtml);`
    },
    {
        search: `_ = _emailService.SendEmailAsync(userObj.Email, "Order Cancelled", $"Your order #{transaction.Id} has been cancelled.");`,
        replace: `
                var emailHtml = LeatherLane_Atelier.Services.EmailTemplateBuilder.BuildStandardEmail(
                    "❌ Order Cancelled", 
                    userObj.Name, 
                    $"Your order #{transaction.Id} has been successfully cancelled as requested.",
                    $"/transactions.html"
                );
                _ = _emailService.SendEmailAsync(userObj.Email, "Order Cancelled (#" + transaction.Id + ")", emailHtml);`
    },
    {
        search: `_ = _emailService.SendEmailAsync(user.Email, \r\n                    "Payment Proof Received", \r\n                    $"We have received your payment proof for Order #{transaction.Id}. Our team will review it shortly.");`,
        replace: `var emailHtml = LeatherLane_Atelier.Services.EmailTemplateBuilder.BuildStandardEmail(
                    "💳 Payment Received", 
                    user.Name, 
                    $"We have received your payment proof for Order #{transaction.Id}. Our team will review and verify it shortly.",
                    $"/order-tracking.html?id={transaction.Id}", "Track Order"
                );
                _ = _emailService.SendEmailAsync(user.Email, "Payment Proof Received (#" + transaction.Id + ")", emailHtml);`
    },
    {
        // Try Unix newlines too if above fails
        search: `_ = _emailService.SendEmailAsync(user.Email, \n                    "Payment Proof Received", \n                    $"We have received your payment proof for Order #{transaction.Id}. Our team will review it shortly.");`,
        replace: `var emailHtml = LeatherLane_Atelier.Services.EmailTemplateBuilder.BuildStandardEmail(
                    "💳 Payment Received", 
                    user.Name, 
                    $"We have received your payment proof for Order #{transaction.Id}. Our team will review and verify it shortly.",
                    $"/order-tracking.html?id={transaction.Id}", "Track Order"
                );
                _ = _emailService.SendEmailAsync(user.Email, "Payment Proof Received (#" + transaction.Id + ")", emailHtml);`
    },
    {
        search: `_ = _emailService.SendEmailAsync(user.Email, \r\n                "Resubmitted Payment Proof Received", \r\n                $"We have received your new payment proof for Order #{transaction.Id}. Our team will review it shortly.");`,
        replace: `var emailHtml = LeatherLane_Atelier.Services.EmailTemplateBuilder.BuildStandardEmail(
                    "💳 Payment Resubmitted", 
                    user.Name, 
                    $"We have received your new payment proof for Order #{transaction.Id}. Our team will review and verify it shortly.",
                    $"/order-tracking.html?id={transaction.Id}", "Track Order"
                );
                _ = _emailService.SendEmailAsync(user.Email, "Payment Proof Resubmitted (#" + transaction.Id + ")", emailHtml);`
    },
    {
        // Unix newlines fallback
        search: `_ = _emailService.SendEmailAsync(user.Email, \n                "Resubmitted Payment Proof Received", \n                $"We have received your new payment proof for Order #{transaction.Id}. Our team will review it shortly.");`,
        replace: `var emailHtml = LeatherLane_Atelier.Services.EmailTemplateBuilder.BuildStandardEmail(
                    "💳 Payment Resubmitted", 
                    user.Name, 
                    $"We have received your new payment proof for Order #{transaction.Id}. Our team will review and verify it shortly.",
                    $"/order-tracking.html?id={transaction.Id}", "Track Order"
                );
                _ = _emailService.SendEmailAsync(user.Email, "Payment Proof Resubmitted (#" + transaction.Id + ")", emailHtml);`
    }
]);

// 2. AdminApiController.cs
replaceInFile('back/Controllers/AdminApiController.cs', [
    {
        search: `_ = emailSvc.SendEmailAsync(customer.Email, "Payment Verified - Order Confirmed", \r\n                        $"Great news! Your payment for Order #{transaction.Id} has been verified and your order is now confirmed.");`,
        replace: `var emailHtml = LeatherLane_Atelier.Services.EmailTemplateBuilder.BuildStandardEmail(
                        "✅ Payment Confirmed", 
                        customer.Name, 
                        $"Great news! Your payment for Order #{transaction.Id} has been verified and your order is now confirmed.",
                        $"/order-tracking.html?id={transaction.Id}", "Track Order"
                    );
                    _ = emailSvc.SendEmailAsync(customer.Email, "Payment Verified & Order Confirmed (#" + transaction.Id + ")", emailHtml);`
    },
    {
        search: `_ = emailSvc.SendEmailAsync(customer.Email, "Payment Verified - Order Confirmed", \n                        $"Great news! Your payment for Order #{transaction.Id} has been verified and your order is now confirmed.");`,
        replace: `var emailHtml = LeatherLane_Atelier.Services.EmailTemplateBuilder.BuildStandardEmail(
                        "✅ Payment Confirmed", 
                        customer.Name, 
                        $"Great news! Your payment for Order #{transaction.Id} has been verified and your order is now confirmed.",
                        $"/order-tracking.html?id={transaction.Id}", "Track Order"
                    );
                    _ = emailSvc.SendEmailAsync(customer.Email, "Payment Verified & Order Confirmed (#" + transaction.Id + ")", emailHtml);`
    },
    {
        search: `_ = emailSvc.SendEmailAsync(customer.Email, notifTitle, notifMsg);`,
        replace: `
                    var fancyTitle = notifTitle;
                    if (req.Status == "Preparing Order") fancyTitle = "🔄 Order Being Prepared";
                    else if (req.Status == "Packed") fancyTitle = "📦 Order Packed";
                    else if (req.Status == "Shipped" || req.Status == "Handed to Courier") fancyTitle = "🚚 Order Shipped";
                    else if (req.Status == "In Transit") fancyTitle = "🚚 Order In Transit";
                    else if (req.Status == "Out for Delivery") fancyTitle = "📍 Out for Delivery";
                    else if (req.Status == "Delivered") fancyTitle = "🎉 Order Delivered";
                    else if (req.Status == "Cancelled") fancyTitle = "❌ Order Cancelled";
                    else fancyTitle = "📋 Order Update";

                    var emailHtml = LeatherLane_Atelier.Services.EmailTemplateBuilder.BuildStandardEmail(
                        fancyTitle, 
                        customer.Name, 
                        notifMsg,
                        $"/order-tracking.html?id={transaction.Id}", "Track Order"
                    );
                    _ = emailSvc.SendEmailAsync(customer.Email, "Order Status Update (#" + transaction.Id + ")", emailHtml);`
    },
    {
        search: `await emailSvc.SendEmailAsync(u.Email, req.Title, req.Message);`,
        replace: `
                                var emailHtml = LeatherLane_Atelier.Services.EmailTemplateBuilder.BuildStandardEmail(
                                    "📢 " + req.Title, 
                                    u.Name, 
                                    req.Message,
                                    req.ActionUrl, "Shop Now"
                                );
                                await emailSvc.SendEmailAsync(u.Email, req.Title, emailHtml);`
    }
]);

// 3. AdminExchangeController.cs
if (fs.existsSync('back/Controllers/AdminExchangeController.cs')) {
    replaceInFile('back/Controllers/AdminExchangeController.cs', [
        {
            search: `_ = _emailService.SendEmailAsync(customer.Email, "Exchange Approved", $"Your exchange request for Order #{request.OrderId} was approved.");`,
            replace: `var emailHtml = LeatherLane_Atelier.Services.EmailTemplateBuilder.BuildStandardEmail("✅ Exchange Approved", customer.Name, $"Your exchange request for Order #{request.OrderId} was approved.", "/transactions.html");
                _ = _emailService.SendEmailAsync(customer.Email, "Exchange Approved (#" + request.OrderId + ")", emailHtml);`
        },
        {
            search: `_ = _emailService.SendEmailAsync(customer.Email, "Exchange Rejected", $"Your exchange request for Order #{request.OrderId} was rejected. Reason: {dto.Reason}");`,
            replace: `var emailHtml = LeatherLane_Atelier.Services.EmailTemplateBuilder.BuildStandardEmail("❌ Exchange Request Update", customer.Name, $"Your exchange request for Order #{request.OrderId} was rejected. Reason: {dto.Reason}", "/transactions.html");
                _ = _emailService.SendEmailAsync(customer.Email, "Exchange Rejected (#" + request.OrderId + ")", emailHtml);`
        }
    ]);
}

// 4. ReturnController.cs
replaceInFile('back/Controllers/ReturnController.cs', [
    {
        search: `_ = _emailService.SendEmailAsync(userObj.Email, "Return Request Submitted", $"We have received your return request for Order #{dto.OrderId}. Our team will review it shortly.");`,
        replace: `var emailHtml = LeatherLane_Atelier.Services.EmailTemplateBuilder.BuildStandardEmail("🔄 Return Request Received", userObj.Name, $"We have received your return request for Order #{dto.OrderId}. Our team will review it shortly.", "/transactions.html");
                _ = _emailService.SendEmailAsync(userObj.Email, "Return Request Submitted (#" + dto.OrderId + ")", emailHtml);`
    }
]);

console.log("Email template integration complete!");
