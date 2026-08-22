const fs = require('fs');
const path = require('path');

const dir = 'front';
const files = fs.readdirSync(dir).filter(f => f.endsWith('.html'));

// Regex to find the SVG Home icon we added
const homeSvgRegex = /<a href="\/home"[^>]*><svg[^>]*>.*?<\/svg>\s*HOME<\/a>/g;
const simpleHomeLink = `<a href="/home">HOME</a>`;

// Regex for the broken footer links
const brokenFooterRegex = /category=([^"]*)">([^<]*)<\/a>/g;

// Regex for the specific address string
const addressRegex = /Ward no 7, street 14, house 143, /g;

for (const file of files) {
    const filePath = path.join(dir, file);
    let content = fs.readFileSync(filePath, 'utf8');
    
    // 1. Remove the home SVG icon and just leave HOME text
    content = content.replace(homeSvgRegex, simpleHomeLink);
    
    // 2. Fix the broken footer links
    // Right now it looks like: <a href="/products?category=Chappal">Chappal</a>
    // Wait, let's see exactly what's broken in the previous output.
    // The previous output showed: <li><a href="/products?category=Chappal">Chappal</a></li>
    // Why did the screenshot show "category=Chappal">Chappal" in the text?
    // Because the SVG string was literally part of the text or the tag closed incorrectly.
    // Let's just forcefully replace the entire list items for Shop Men's footer.
    const badFooterShopRegex = /<a href="\/products\?category=([^"]*)">[^<]*<\/a>/g;
    // Actually, I'll just replace the exact broken text if it exists.
    content = content.replace(/<a href="\/products\?category=([^"]*)">([^<]*)<\/a>/g, '<a href="/products?category=$1">$2</a>');
    
    // Let's just find the exact block and replace it since it's cleaner.
    const footerBlockRegex = /<h5[^>]*>Shop Men's<\/h5>\s*<ul>\s*<li><a href="[^"]*">Chappal<\/a><\/li>\s*<li><a href="[^"]*">Peshawari Chappal<\/a><\/li>\s*<li><a href="[^"]*">Shoes<\/a><\/li>\s*<li><a href="[^"]*">Sandals<\/a><\/li>\s*<\/ul>/g;
    
    // Wait, the HTML in the screenshot literally renders `category=Chappal">Chappal`.
    // That means the HTML looks like: `<a href="/products?category=Chappal"category=Chappal">Chappal</a>` or something.
    // Let's do a catch-all for these specific links to perfectly reset them.
    content = content.replace(/<li><a href="[^"]*".*?Chappal<\/a><\/li>/, '<li><a href="/products?category=Chappal">Chappal</a></li>');
    content = content.replace(/<li><a href="[^"]*".*?Peshawari Chappal<\/a><\/li>/, '<li><a href="/products?category=Peshawari Chappal">Peshawari Chappal</a></li>');
    content = content.replace(/<li><a href="[^"]*".*?Shoes<\/a><\/li>/, '<li><a href="/products?category=Shoes">Shoes</a></li>');
    content = content.replace(/<li><a href="[^"]*".*?Sandals<\/a><\/li>/, '<li><a href="/products?category=Sandals">Sandals</a></li>');
    
    // 3. Remove the specific address
    content = content.replace(addressRegex, '');
    
    fs.writeFileSync(filePath, content, 'utf8');
}
console.log("Reverted Home icon, fixed footer links, and removed specific address.");
