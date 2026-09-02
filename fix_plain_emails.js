const fs = require('fs');

// 1. Fix NotifyAdminsAsync in EmailTemplateBuilder.cs to wrap the body in HTML
let builderPath = 'back/Services/EmailTemplateBuilder.cs';
if (fs.existsSync(builderPath)) {
    let builderContent = fs.readFileSync(builderPath, 'utf8');
    builderContent = builderContent.replace(
        /_ = emailService\.SendEmailAsync\(email, subject, body\);/g,
        `var htmlBody = EmailTemplateBuilder.BuildStandardEmail(subject, "Admin", body, "/admin.html", "Go to Dashboard");\n                _ = emailService.SendEmailAsync(email, subject, htmlBody);`
    );
    fs.writeFileSync(builderPath, builderContent, 'utf8');
}

// 2. Fix New Product Alerts in AdminProductsController and ProductsController
function replaceNewProductAlert(filePath) {
    if (!fs.existsSync(filePath)) return;
    let content = fs.readFileSync(filePath, 'utf8');
    
    // Replace standard format
    content = content.replace(
        /_ = emailSvc\.SendEmailAsync\(u\.Email, "New Product Alert!", \$\"Hi \{u\.Name\},\\n\\nWe just added a new product to our store: \{productName\}\. Visit our website to see more details!\"\);/g,
        `var htmlEmail = LeatherLane_Atelier.Services.EmailTemplateBuilder.BuildStandardEmail("📢 New Product Alert!", u.Name, $"We just added a new product to our store: <b>{productName}</b>. Visit our website to see more details!", "/products.html", "Shop Now");
                            _ = emailSvc.SendEmailAsync(u.Email, "New Product Alert!", htmlEmail);`
    );

    // Replace context variation if it exists
    content = content.replace(
        /_ = _emailService\.SendEmailAsync\(u\.Email, "New Product Alert!", \$\"Hi \{u\.Name\},\\n\\nWe just added a new product to our store: \{productName\}\. Visit our website to see more details!\"\);/g,
        `var htmlEmail = LeatherLane_Atelier.Services.EmailTemplateBuilder.BuildStandardEmail("📢 New Product Alert!", u.Name, $"We just added a new product to our store: <b>{productName}</b>. Visit our website to see more details!", "/products.html", "Shop Now");
                            _ = _emailService.SendEmailAsync(u.Email, "New Product Alert!", htmlEmail);`
    );
    
    fs.writeFileSync(filePath, content, 'utf8');
}

replaceNewProductAlert('back/Controllers/AdminProductsController.cs');
replaceNewProductAlert('back/Controllers/ProductsController.cs');

console.log("Fixed missing HTML wrappers!");
