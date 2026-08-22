const fs = require('fs');
const path = require('path');

const dir = 'front';
const files = fs.readdirSync(dir).filter(f => f.endsWith('.html'));

const exploreBtnRegex = /<button[^>]*>Explore Collection<\/button>\s*/g;
const igSectionRegex = /<!-- Instagram Section -->[\s\S]*?<\/section>/;
const homeTextRegex = />\s*HOME\s*</g;

for (const file of files) {
    const filePath = path.join(dir, file);
    let content = fs.readFileSync(filePath, 'utf8');
    
    // 1. Add Home Icon
    content = content.replace(homeTextRegex, '>🏠 HOME<');
    
    // 2. Remove Explore Collection button
    if (content.includes('Explore Collection')) {
        content = content.replace(exploreBtnRegex, '');
    }
    
    // 3. Remove Instagram Section
    if (content.includes('<!-- Instagram Section -->')) {
        content = content.replace(igSectionRegex, '');
    }
    
    fs.writeFileSync(filePath, content, 'utf8');
}

console.log("Fixed Navigation, removed Explore button, and removed Instagram grid sitewide.");
