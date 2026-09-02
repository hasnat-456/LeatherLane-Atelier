const fs = require('fs');

function replaceInFile(filePath) {
    if (!fs.existsSync(filePath)) return;
    let content = fs.readFileSync(filePath, 'utf8');
    content = content.replace(/_\s*=\s*LeatherLane_Atelier\.Services\.EmailServiceExtensions\.NotifyAdminsAsync/g, 'await LeatherLane_Atelier.Services.EmailServiceExtensions.NotifyAdminsAsync');
    fs.writeFileSync(filePath, content, 'utf8');
}

replaceInFile('back/Controllers/TransactionsController.cs');
replaceInFile('back/Controllers/ProductsController.cs');
replaceInFile('back/Controllers/ExchangeController.cs');
replaceInFile('back/Controllers/ReturnController.cs');
replaceInFile('back/Controllers/AdminApiController.cs');

console.log("Fixed EF Core concurrency issue!");
