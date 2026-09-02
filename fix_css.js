const fs = require('fs');
let html = fs.readFileSync('front/home.html', 'utf8');

html = html.replace('.hero h2 {', '.hero h1 {');
html = html.replace('.hero p {', '.hero h2 {');

fs.writeFileSync('front/home.html', html, 'utf8');
console.log("Updated CSS selectors in home.html");
