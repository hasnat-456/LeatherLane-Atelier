const fs = require('fs');
const path = require('path');

const dir = 'front';
const files = fs.readdirSync(dir);

for (const file of files) {
    if (!file.endsWith('.html')) continue;
    
    const filePath = path.join(dir, file);
    let content = fs.readFileSync(filePath, 'utf8');
    
    // Task 2: Fix domain mismatch
    content = content.replace(/leatherlaneatelier\.com/g, 'leatherlaneatelier.store');
    
    // Task 3: Update Twitter meta description
    const oldTwitterDesc = '<meta name="twitter:description" content="Discover premium handcrafted leather bags, wallets, and accessories">';
    const newTwitterDesc = '<meta name="twitter:description" content="Discover premium handmade Peshawari chappals and handcrafted leather footwear for men, made with passion and precision.">';
    content = content.replace(new RegExp(escapeRegExp(oldTwitterDesc), 'g'), newTwitterDesc);
    
    // Task 4 & 5: home.html specific
    if (file === 'home.html') {
        // Fix canonical
        content = content.replace('<link rel="canonical" href="https://leatherlaneatelier.store/home">', '<link rel="canonical" href="https://leatherlaneatelier.store/">');
        
        // Optimize H1
        const newH1Section = `<h1 style="font-family: var(--font-heading); font-size: 3.5rem; text-transform: uppercase; letter-spacing: 2px;">Handmade Peshawari Chappals & Leather Footwear, Crafted in Pakistan</h1>
            <h2 style="font-size: 1.5rem; font-weight: normal; margin-top: 1rem;">Timeless Craftsmanship - Handcrafted leather goods made with passion, precision, and the finest materials</h2>`;
        
        content = content.replace(/<h2 style="[^"]*">Timeless Craftsmanship<\/h2>\s*<p>Handcrafted leather goods made with passion, precision, and the finest materials<\/p>/g, newH1Section);
    }
    
    // Task 6: Canonical tags for category filters
    if (file === 'products.html') {
        // Ensure there is a canonical tag. If missing, add it before </head>
        if (!content.includes('rel="canonical"')) {
            content = content.replace('</head>', '    <link rel="canonical" href="https://leatherlaneatelier.store/products">\n</head>');
        }
    }
    
    fs.writeFileSync(filePath, content, 'utf8');
}

function escapeRegExp(string) {
  return string.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
}

console.log('Finished updating HTML metadata and tags.');
