const fs = require('fs');

// 1. Add extension method to EmailTemplateBuilder.cs
let emailBuilderPath = 'back/Services/EmailTemplateBuilder.cs';
let emailBuilder = fs.readFileSync(emailBuilderPath, 'utf8');

const extensionMethod = `
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
                // Fire and forget
                _ = emailService.SendEmailAsync(email, subject, body);
            }
        }
    }
}
`;

if (!emailBuilder.includes("NotifyAdminsAsync")) {
    emailBuilder = emailBuilder.replace(/}\s*}$/, extensionMethod);
    fs.writeFileSync(emailBuilderPath, emailBuilder, 'utf8');
}

// 2. Replace all hardcoded admin emails in the controllers with NotifyAdminsAsync

function replaceInFile(filePath, replacements) {
    if (!fs.existsSync(filePath)) return;
    let content = fs.readFileSync(filePath, 'utf8');
    for (let r of replacements) {
        content = content.replace(r.search, r.replace);
    }
    fs.writeFileSync(filePath, content, 'utf8');
}

replaceInFile('back/Controllers/TransactionsController.cs', [
    {
        search: `_ = _emailService.SendEmailAsync("leatherlaneatelier@gmail.com", "New Order Received", $"Order #{transaction.Id} was placed.");`,
        replace: `_ = _emailService.NotifyAdminsAsync(_context, "New Order Received", $"Order #{transaction.Id} was placed.");`
    },
    {
        search: `_ = _emailService.SendEmailAsync("leatherlaneatelier@gmail.com", "Order Cancelled", $"Order #{transaction.Id} was cancelled by the customer. Reason: {req.Reason}");`,
        replace: `_ = _emailService.NotifyAdminsAsync(_context, "Order Cancelled", $"Order #{transaction.Id} was cancelled by the customer. Reason: {req.Reason}");`
    },
    {
        search: `_ = _emailService.SendEmailAsync("leatherlaneatelier@gmail.com", \r\n                    "New Payment Verification Pending", \r\n                    $"Order #{transaction.Id} requires payment verification. Transaction ID: {transaction.PaymentRefId}. Amount: Rs. {transaction.TotalAmount}.");`,
        replace: `_ = _emailService.NotifyAdminsAsync(_context, "New Payment Verification Pending", $"Order #{transaction.Id} requires payment verification. Transaction ID: {transaction.PaymentRefId}. Amount: Rs. {transaction.TotalAmount}.");`
    },
    {
        search: `_ = _emailService.SendEmailAsync("leatherlaneatelier@gmail.com", \n                    "New Payment Verification Pending", \n                    $"Order #{transaction.Id} requires payment verification. Transaction ID: {transaction.PaymentRefId}. Amount: Rs. {transaction.TotalAmount}.");`,
        replace: `_ = _emailService.NotifyAdminsAsync(_context, "New Payment Verification Pending", $"Order #{transaction.Id} requires payment verification. Transaction ID: {transaction.PaymentRefId}. Amount: Rs. {transaction.TotalAmount}.");`
    },
    {
        search: `_ = _emailService.SendEmailAsync("leatherlaneatelier@gmail.com", \r\n                "Resubmitted Payment Proof", \r\n                $"Order #{transaction.Id} payment proof was resubmitted. Reference ID: {transaction.PaymentRefId}.");`,
        replace: `_ = _emailService.NotifyAdminsAsync(_context, "Resubmitted Payment Proof", $"Order #{transaction.Id} payment proof was resubmitted. Reference ID: {transaction.PaymentRefId}.");`
    },
    {
        search: `_ = _emailService.SendEmailAsync("leatherlaneatelier@gmail.com", \n                "Resubmitted Payment Proof", \n                $"Order #{transaction.Id} payment proof was resubmitted. Reference ID: {transaction.PaymentRefId}.");`,
        replace: `_ = _emailService.NotifyAdminsAsync(_context, "Resubmitted Payment Proof", $"Order #{transaction.Id} payment proof was resubmitted. Reference ID: {transaction.PaymentRefId}.");`
    }
]);

replaceInFile('back/Controllers/ProductsController.cs', [
    {
        search: `_ = emailService.SendEmailAsync("leatherlaneatelier@gmail.com", "New Product Review", $"A customer left a {dto.Rating}-star review for {product.Name}:\\n\\n{dto.Comment}");`,
        replace: `_ = emailService.NotifyAdminsAsync(_context, "New Product Review", $"A customer left a {dto.Rating}-star review for {product.Name}:\\n\\n{dto.Comment}");`
    }
]);

replaceInFile('back/Controllers/ExchangeController.cs', [
    {
        search: `_ = _emailService.SendEmailAsync("leatherlaneatelier@gmail.com", "New Exchange Request", $"A new exchange request was submitted for Order #{order.Id}.");`,
        replace: `_ = _emailService.NotifyAdminsAsync(_context, "New Exchange Request", $"A new exchange request was submitted for Order #{order.Id}.");`
    },
    {
        search: `_ = _emailService.SendEmailAsync("leatherlaneatelier@gmail.com", "Exchange Tracking Submitted", $"Tracking for Exchange #{id} is: {dto.CourierName} {dto.TrackingNumber}");`,
        replace: `_ = _emailService.NotifyAdminsAsync(_context, "Exchange Tracking Submitted", $"Tracking for Exchange #{id} is: {dto.CourierName} {dto.TrackingNumber}");`
    }
]);

replaceInFile('back/Controllers/ReturnController.cs', [
    {
        search: `_ = _emailService.SendEmailAsync("leatherlaneatelier@gmail.com", "New Return Request", $"A new return request was submitted for Order #{dto.OrderId}.");`,
        replace: `_ = _emailService.NotifyAdminsAsync(_context, "New Return Request", $"A new return request was submitted for Order #{dto.OrderId}.");`
    }
]);

console.log("Admin notification dynamically updated across controllers!");
