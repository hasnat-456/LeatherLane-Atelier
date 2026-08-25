const fs = require('fs');
function fix(file) {
    if(!fs.existsSync(file)) return;
    let content = fs.readFileSync(file, 'utf8');
    content = content.replace(/\(_emailService\(_context/g, '(_emailService, _context');
    content = content.replace(/\(emailService\(_context/g, '(emailService, _context');
    content = content.replace(/\(emailSvc\(_context/g, '(emailSvc, _context');
    fs.writeFileSync(file, content, 'utf8');
}
fix('back/Controllers/TransactionsController.cs');
fix('back/Controllers/ProductsController.cs');
fix('back/Controllers/ExchangeController.cs');
fix('back/Controllers/ReturnController.cs');
fix('back/Controllers/AdminApiController.cs');
