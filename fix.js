const fs = require('fs');
let file = 'back/Controllers/TransactionsController.cs';
let content = fs.readFileSync(file, 'utf8');
content = content.replace('i.ProductName', 'i.Name');
fs.writeFileSync(file, content, 'utf8');
