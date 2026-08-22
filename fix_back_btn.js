const fs = require('fs');

let authJs = fs.readFileSync('front/js/auth.js', 'utf8');

const oldCondition = "if (!path.includes('/home') && !path.includes('/login') && !path.includes('/signup') && !path.includes('/index')) {";
const newCondition = "if (path !== '/' && path !== '' && !path.includes('/home') && !path.includes('/login') && !path.includes('/signup') && !path.includes('/index')) {";

authJs = authJs.replace(oldCondition, newCondition);

fs.writeFileSync('front/js/auth.js', authJs, 'utf8');
console.log("Fixed back button condition for root path.");
