const fs = require('fs');
let emailBuilderPath = 'back/Services/EmailTemplateBuilder.cs';
let content = fs.readFileSync(emailBuilderPath, 'utf8');

if (!content.includes("NotifyAdminsAsync")) {
    content = content.trim();
    if (content.endsWith("}")) {
        content = content.slice(0, -1); // remove the last bracket of the namespace
        
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
                _ = emailService.SendEmailAsync(email, subject, body);
            }
        }
    }
}
`;
        content += extensionMethod;
        fs.writeFileSync(emailBuilderPath, content, 'utf8');
    }
}
