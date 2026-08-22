const fs = require('fs');
const path = require('path');

const dir = 'front';
const files = fs.readdirSync(dir).filter(f => f.endsWith('.html'));

const globalKeywordsRegex = /<meta name="keywords" content="[^"]*">/g;
const newKeywords = `<meta name="keywords" content="handmade Peshawari chappal, leather chappal Pakistan, handcrafted men's footwear, leather sandals Pakistan, LeatherLane Atelier">`;

const canonicalRegex = /<link rel="canonical" href="[^"]*">/g;
const ogUrlRegex = /<meta property="og:url" content="[^"]*">/g;
const ogSiteNameRegex = /<meta property="og:site_name" content="[^"]*">/g;

const pageMeta = {
    'home.html': {
        title: "Handmade Peshawari Chappal & Leather Footwear Online | LeatherLane Atelier",
        desc: "Shop handcrafted men's Peshawari Chappal, Chappals, Shoes & Sandals made from premium full-grain leather in Pakistan. Lifetime warranty. Order online today."
    },
    'products.html': {
        title: "Buy Men's Leather Footwear Online in Pakistan | LeatherLane Atelier",
        desc: "Browse our full collection of handmade Chappals, Peshawari Chappals, Shoes & Sandals. Premium leather, artisan craftsmanship, delivered across Pakistan."
    },
    'about.html': {
        title: "Our Story | Handmade Leather Footwear Since 1985 | LeatherLane Atelier",
        desc: "Discover the story behind LeatherLane Atelier — Pakistani artisans handcrafting premium leather Chappals and footwear with decades of tradition and skill."
    },
    'deals.html': {
        title: "Today's Deal on Handmade Leather Chappals | LeatherLane Atelier",
        desc: "Limited-time offer on premium handcrafted leather footwear. Shop today's deal before it's gone — genuine leather, artisan made, fast delivery in Pakistan."
    },
    'contact.html': {
        title: "Contact LeatherLane Atelier | Jhang, Punjab, Pakistan",
        desc: "Get in touch with LeatherLane Atelier for orders, custom sizing, or support. Based in Jhang City, Punjab — Mon-Sat, 10 AM to 7 PM."
    }
};

for (const file of files) {
    const filePath = path.join(dir, file);
    let content = fs.readFileSync(filePath, 'utf8');
    
    // Global replacements
    content = content.replace(globalKeywordsRegex, newKeywords);
    
    const pageName = file.replace('.html', '');
    const cleanUrl = `https://leatherlaneatelier.store/${pageName}`;
    
    if (content.includes('<link rel="canonical"')) {
        content = content.replace(canonicalRegex, `<link rel="canonical" href="${cleanUrl}">`);
    } else {
        content = content.replace('</head>', `    <link rel="canonical" href="${cleanUrl}">\n</head>`);
    }
    
    content = content.replace(ogUrlRegex, `<meta property="og:url" content="${cleanUrl}">`);
    
    // Specific page meta
    if (pageMeta[file]) {
        const titleRegex = /<title>.*<\/title>/;
        content = content.replace(titleRegex, `<title>${pageMeta[file].title}</title>`);
        
        const descRegex = /<meta name="description" content="[^"]*">/;
        const newDesc = `<meta name="description" content="${pageMeta[file].desc}">`;
        if (content.includes('<meta name="description"')) {
            content = content.replace(descRegex, newDesc);
        } else {
            content = content.replace('</head>', `    ${newDesc}\n</head>`);
        }
        
        const ogTitleRegex = /<meta property="og:title" content="[^"]*">/;
        if (content.includes('<meta property="og:title"')) {
            content = content.replace(ogTitleRegex, `<meta property="og:title" content="${pageMeta[file].title}">`);
        }
        
        const ogDescRegex = /<meta property="og:description" content="[^"]*">/;
        if (content.includes('<meta property="og:description"')) {
            content = content.replace(ogDescRegex, `<meta property="og:description" content="${pageMeta[file].desc}">`);
        }
    }
    
    // Nav replacements sitewide
    // Replace "cart.html" with "Cart" and add an icon (e.g., 🛒 or a nice SVG/emoji)
    // Actually, let's use standard unicode or simple HTML entity to avoid breaking encoding, or 🛒
    content = content.replace(/>\s*cart\.html\s*</g, '>🛒 Cart<');
    content = content.replace(/>\s*Home\s*</g, '>🏠 Home<');
    
    // Replace hrefs
    // Simple regex for href="page.html" to href="/page"
    // except external links or api links
    content = content.replace(/href="([a-zA-Z0-9_-]+)\.html"/g, 'href="/$1"');
    
    fs.writeFileSync(filePath, content, 'utf8');
}
console.log("SEO and Nav Links updated successfully.");
