const fs = require('fs');

let authJs = fs.readFileSync('front/js/auth.js', 'utf8');

authJs = authJs.replace(/includes\('([a-zA-Z0-9_-]+)\.html'\)/g, "includes('/$1')");

fs.writeFileSync('front/js/auth.js', authJs, 'utf8');
console.log("Fixed includes logic in auth.js");
