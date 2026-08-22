const fs = require('fs');
const path = require('path');

const dir = 'front';
const files = fs.readdirSync(dir).filter(f => f.endsWith('.html'));

// The exact corrupted string might vary slightly depending on how it was read,
// but we can target the whole line or just use a regex for <a href="/home">.*HOME</a>
const homeLinkRegex = /<a href="\/home">[^<]*HOME<\/a>/g;
const cleanHomeLink = `<a href="/home" style="display:inline-flex; align-items:center; gap:5px;"><svg viewBox="0 0 24 24" width="1.2em" height="1.2em" stroke="currentColor" stroke-width="2" fill="none" stroke-linecap="round" stroke-linejoin="round" style="vertical-align: text-bottom;"><path d="M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"></path><polyline points="9 22 9 12 15 12 15 22"></polyline></svg> HOME</a>`;

for (const file of files) {
    const filePath = path.join(dir, file);
    let content = fs.readFileSync(filePath, 'utf8');
    
    // Replace the corrupted home link with the clean SVG + text version
    content = content.replace(homeLinkRegex, cleanHomeLink);
    
    fs.writeFileSync(filePath, content, 'utf8');
}

console.log("Fixed corrupted HOME text and added professional SVG icon sitewide.");
