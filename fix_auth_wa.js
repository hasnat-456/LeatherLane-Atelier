const fs = require('fs');

let code = fs.readFileSync('front/js/auth.js', 'utf8');

// First, clean up the broken injection
const brokenInjectionRegex = /\/\/ Set up the footer WhatsApp icons to use the exact same link\s*document\.querySelectorAll\("a\[title='WhatsApp'\]"\)\.forEach\(el => \{\s*el\.href = waUrl;\s*el\.target = '_blank';\s*\}\);\s*\/\/ Inject CSS to ensure it always applies/g;

code = code.replace(brokenInjectionRegex, '// Inject CSS to ensure it always applies');

// Now, properly inject into the WhatsApp DOMContentLoaded block AFTER waUrl is defined
const targetPoint = "let waUrl = 'https://wa.me/' + waNumber + waMessage;";
const correctInjection = `
    let waUrl = 'https://wa.me/' + waNumber + waMessage;

    // Set up the footer WhatsApp icons to use the exact same link
    document.querySelectorAll("a[title='WhatsApp']").forEach(el => {
        el.href = waUrl;
        el.target = '_blank';
    });
`;

if (code.includes(targetPoint)) {
    code = code.replace(targetPoint, correctInjection);
    fs.writeFileSync('front/js/auth.js', code, 'utf8');
    console.log("Fixed auth.js WhatsApp injection!");
} else {
    console.log("Could not find the correct injection point.");
}
