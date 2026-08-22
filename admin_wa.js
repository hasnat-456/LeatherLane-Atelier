const fs = require('fs');
let code = fs.readFileSync('front/js/auth.js', 'utf8');

const regex = /\/\/ --- WhatsApp Floating Button ---[\s\S]*?(?=\/\/ Inject CSS)/;

const newLogic = `// --- WhatsApp Floating Button ---
document.addEventListener('DOMContentLoaded', async function() {
    if (document.getElementById('wa-floating-btn') || window.location.href.includes('admin')) return;

    let waNumber = '923376306162';
    
    // Fetch Admin Settings dynamically
    try {
        const res = await fetch('/api/settings');
        if (res.ok) {
            const settings = await res.json();
            if (settings && settings.whatsAppUrl && settings.whatsAppUrl.trim() !== '') {
                waNumber = settings.whatsAppUrl.trim();
            }
        }
    } catch(e) {
        console.error("Failed to fetch WhatsApp admin settings", e);
    }

    let waMessage = '';
    if (window.location.href.includes('product-detail.html')) {
        let currentUrl = window.location.href;
        waMessage = '?text=' + encodeURIComponent('Hi, I need help with this: ' + currentUrl);
    }
    
    let waUrl = waNumber;
    if (!waUrl.startsWith('http')) {
        waUrl = 'https://wa.me/' + waNumber.replace(/[^\\d\\+]/g, '') + waMessage;
    } else {
        if (!waUrl.includes('?')) {
            waUrl += waMessage;
        } else {
            waUrl += waMessage.replace('?', '&');
        }
    }

    // Set up the footer WhatsApp icons to use the exact same link
    document.querySelectorAll("a[title='WhatsApp']").forEach(el => {
        el.href = waUrl;
        el.target = '_blank';
    });

    `;

code = code.replace(regex, newLogic);
fs.writeFileSync('front/js/auth.js', code, 'utf8');
console.log("Updated WhatsApp logic to pull from Admin settings.");
