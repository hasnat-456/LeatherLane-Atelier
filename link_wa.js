const fs = require('fs');

let code = fs.readFileSync('front/js/auth.js', 'utf8');

const injection = `
    // Set up the footer WhatsApp icons to use the exact same link
    document.querySelectorAll("a[title='WhatsApp']").forEach(el => {
        el.href = waUrl;
        el.target = '_blank';
    });

    // Inject CSS`;

if (code.includes('// Inject CSS')) {
    code = code.replace('// Inject CSS', injection);
    fs.writeFileSync('front/js/auth.js', code, 'utf8');
    console.log("Updated auth.js to link footer WhatsApp to the floating WhatsApp URL.");
} else {
    console.log("Could not find injection point.");
}
