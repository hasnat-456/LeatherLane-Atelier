const fs = require('fs');

let authJs = fs.readFileSync('front/js/auth.js', 'utf8');

// 1. Remove the old broken fetch settings block
const oldFetchRegex = /\/\/ --- Dynamic Settings Injection ---[\s\S]*?\.catch\(e => console\.error\("Could not load dynamic settings", e\)\);/;
authJs = authJs.replace(oldFetchRegex, '');

// 2. Rewrite the WhatsApp floating button block to handle ALL social links perfectly
const waBlockRegex = /\/\/ --- WhatsApp Floating Button ---[\s\S]*?(?=\/\/ Inject CSS)/;

const newLogic = `// --- Dynamic Social & WhatsApp Settings ---
document.addEventListener('DOMContentLoaded', async function() {
    // 1. First fetch the settings from the server
    let settings = {};
    try {
        const res = await fetch('/api/settings');
        if (res.ok) {
            settings = await res.json();
        }
    } catch(e) {
        console.error("Failed to fetch admin settings", e);
    }

    // 2. Update all standard text fields (address, email, phone, hours)
    if (settings) {
        document.querySelectorAll('.dyn-address').forEach(el => el.textContent = settings.address || '');
        document.querySelectorAll('.dyn-email').forEach(el => el.textContent = settings.email || '');
        document.querySelectorAll('.dyn-phone').forEach(el => el.textContent = settings.phone || '');
        document.querySelectorAll('.dyn-hours').forEach(el => el.textContent = settings.businessHours || '');
    }

    // 3. Helper function to update footer social icons
    const updateIcon = (title, url) => {
        document.querySelectorAll(\`a[title='\${title}']\`).forEach(el => {
            if (url && url.trim() !== '') {
                el.href = url.trim();
                el.target = '_blank';
                el.style.display = 'inline-block';
            } else {
                // If the admin didn't provide a link, we hide the icon so it doesn't show a dead link
                el.style.display = 'none';
            }
        });
    };

    // 4. Apply Facebook, Instagram, and TikTok links
    updateIcon('Facebook', settings.facebookUrl);
    updateIcon('Instagram', settings.instagramUrl);
    updateIcon('TikTok', settings.tikTokUrl);

    // 5. Handle WhatsApp specifically (for both the footer icon and floating button)
    let waNumber = '923376306162'; // default fallback
    if (settings && settings.whatsAppUrl && settings.whatsAppUrl.trim() !== '') {
        waNumber = settings.whatsAppUrl.trim();
    }

    let waMessage = '';
    if (window.location.href.includes('/product-detail')) {
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

    // Apply the smart WhatsApp link to the footer icon
    updateIcon('WhatsApp', waUrl);

    // 6. Only create the floating button if we are NOT on the admin dashboard
    if (!document.getElementById('wa-floating-btn') && !window.location.href.includes('admin')) {
        // Inject CSS for floating button
`;

authJs = authJs.replace(waBlockRegex, newLogic);
fs.writeFileSync('front/js/auth.js', authJs, 'utf8');
console.log("Rewrote dynamic settings logic in auth.js");
