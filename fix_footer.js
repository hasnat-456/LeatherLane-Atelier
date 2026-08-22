const fs = require('fs');
let code = fs.readFileSync('front/about.html', 'utf8');

const regex = /products\.html<svg[^>]*>category=/g;
code = code.replace(regex, '/products?category=');

fs.writeFileSync('front/about.html', code, 'utf8');
console.log("Fixed corrupted footer links in about.html");
