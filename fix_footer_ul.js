const fs = require('fs');
const path = require('path');

const dir = 'front';
const files = fs.readdirSync(dir);

let count = 0;
for (const file of files) {
    if (file.endsWith('.html')) {
        const filePath = path.join(dir, file);
        let content = fs.readFileSync(filePath, 'utf8');
        
        // Find <h5>Shop Men's</h5> followed by <ul>
        // Note: In some files it might be <h5 style="...">Shop Men's</h5>
        const regex = /(<h5[^>]*>Shop Men's<\/h5>\s*)<ul>/g;
        
        if (regex.test(content)) {
            content = content.replace(regex, '$1<ul id="footerCategoriesList">');
            fs.writeFileSync(filePath, content, 'utf8');
            count++;
        }
    }
}
console.log("Updated " + count + " HTML files.");
