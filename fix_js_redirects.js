const fs = require('fs');
const path = require('path');

const dir = 'front';
const files = fs.readdirSync(dir).filter(f => f.endsWith('.html') || f.endsWith('.js'));

// Fix javascript redirects in html files
for (const file of files) {
    const filePath = path.join(dir, file);
    let content = fs.readFileSync(filePath, 'utf8');
    
    // Replace window.location.href = 'cart.html' with '/cart'
    content = content.replace(/'cart\.html'/g, "'/cart'");
    content = content.replace(/'login\.html'/g, "'/login'");
    content = content.replace(/'checkout\.html'/g, "'/checkout'");
    content = content.replace(/'home\.html'/g, "'/home'");
    content = content.replace(/'products\.html'/g, "'/products'");
    
    fs.writeFileSync(filePath, content, 'utf8');
}
console.log("Fixed JS redirects to use clean URLs");
