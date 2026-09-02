const fs = require('fs');

function replaceInFile(filePath, replacements) {
    if (!fs.existsSync(filePath)) return;
    let content = fs.readFileSync(filePath, 'utf8');
    for (let r of replacements) {
        content = content.replace(r.search, r.replace);
    }
    fs.writeFileSync(filePath, content, 'utf8');
}

const replacements = [
    {
        search: /_emailService\.NotifyAdminsAsync/g,
        replace: `LeatherLane_Atelier.Services.EmailServiceExtensions.NotifyAdminsAsync(_emailService`
    },
    {
        search: /emailService\.NotifyAdminsAsync/g,
        replace: `LeatherLane_Atelier.Services.EmailServiceExtensions.NotifyAdminsAsync(emailService`
    },
    {
        search: /emailSvc\.NotifyAdminsAsync/g,
        replace: `LeatherLane_Atelier.Services.EmailServiceExtensions.NotifyAdminsAsync(emailSvc`
    }
];

replaceInFile('back/Controllers/TransactionsController.cs', replacements);
replaceInFile('back/Controllers/ProductsController.cs', replacements);
replaceInFile('back/Controllers/ExchangeController.cs', replacements);
replaceInFile('back/Controllers/ReturnController.cs', replacements);
replaceInFile('back/Controllers/AdminApiController.cs', replacements);
