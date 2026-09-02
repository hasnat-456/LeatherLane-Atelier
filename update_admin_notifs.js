const fs = require('fs');

let code = fs.readFileSync('back/Controllers/AdminApiController.cs', 'utf8');

const injectionCode = `
            _context.TimelineEvents.Add(newEvent);

            // ---> INJECTED NOTIFICATION LOGIC <---
            var customer = await _context.Users.FindAsync(transaction.UserId);
            if (customer != null)
            {
                var notifTitle = "Order Status Updated";
                var notifMsg = $"Your order #{transaction.Id} status is now: {req.Status}.";
                
                if (req.Status == "Cancelled") 
                {
                    notifTitle = "Order Cancelled";
                    notifMsg = $"Your order #{transaction.Id} has been cancelled by Admin. Reason: {req.CancelReason}";
                }
                else if (req.Status == "Delivered")
                {
                    notifTitle = "Order Delivered";
                    notifMsg = $"Your order #{transaction.Id} has been delivered successfully. Thank you for shopping with us!";
                }
                else if (req.Status == "Handed to Courier" || req.Status == "Shipped")
                {
                    notifTitle = "Order Shipped";
                    notifMsg = $"Your order #{transaction.Id} has been handed to the courier and is on its way.";
                }

                _context.Notifications.Add(new Notification
                {
                    Title = notifTitle,
                    Message = notifMsg,
                    ActionUrl = $"order-tracking.html?id={transaction.Id}",
                    UserId = customer.Id
                });

                var emailSvc = HttpContext.RequestServices.GetService(typeof(LeatherLane_Atelier.Services.IEmailService)) as LeatherLane_Atelier.Services.IEmailService;
                if (emailSvc != null)
                {
                    _ = emailSvc.SendEmailAsync(customer.Email, notifTitle, notifMsg);
                }
            }
            // ---> END INJECTED LOGIC <---
`;

code = code.replace('_context.TimelineEvents.Add(newEvent);', injectionCode);
fs.writeFileSync('back/Controllers/AdminApiController.cs', code, 'utf8');
console.log("AdminApiController.cs updated successfully!");
