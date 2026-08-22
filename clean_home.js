const fs = require('fs');
const path = require('path');

const dir = 'front';
const files = fs.readdirSync(dir).filter(f => f.endsWith('.html'));

for (const file of files) {
    const filePath = path.join(dir, file);
    let content = fs.readFileSync(filePath, 'utf8');
    
    // Catch-all for any corrupted home link
    content = content.replace(/<a href="\/home">[^<]*Home<\/a>/gi, (match) => {
        // preserve the case of 'Home' or 'HOME' that was there, wait, just look at the last character
        if (match.includes('HOME')) {
            return '<a href="/home">HOME</a>';
        } else {
            return '<a href="/home">HOME</a>'; // actually the user wants it to match, let's just use what they had, wait, their screenshot of home.html shows uppercase. Their screenshot of about.html shows uppercase! 
        }
    });
    
    // I'll just forcefully make all nav links uppercase to be consistent if they want,
    // but better yet, just replace the exact tags
    content = content.replace(/<a href="\/home">.*?HOME<\/a>/g, '<a href="/home">HOME</a>');
    content = content.replace(/<a href="\/home">.*?Home<\/a>/g, '<a href="/home">HOME</a>'); // Screenshot 1 shows uppercase HOME SHOP ABOUT
    
    fs.writeFileSync(filePath, content, 'utf8');
}
console.log("Force cleaned Home text.");
