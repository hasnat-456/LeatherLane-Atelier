const fs = require('fs');
let code = fs.readFileSync('front/js/security.js', 'utf8');

// Replace the overly aggressive general regex
const oldRegex = /\/\[\^a-zA-Z0-9\\s\\-\\\.,!\?'"\(\)&\\r\\n#:\]\/g/;
const newRegex = "/[^a-zA-Z0-9\\s\\-\\.,!?'\"()&\\r\\n#:\\/=@%+_]/g";

// We also need to add a specific check for URLs to be safe.
const urlFix = `
                        // 3.5 URLs
                        const isUrl = input.type === 'url' || name.includes('url') || id.includes('url') || name.includes('link') || id.includes('link');
                        if (isUrl) {
                            // Allow valid URL characters
                            val = val.replace(/[^a-zA-Z0-9\\-\\.\\_~:/?#\\[\\]@!$&'()*+,;=%]/g, '');
                        }
                        else if (isNameOrCity) {
`;

code = code.replace(/else\s*{\s*\/\/\s*3\.\s*Name\s*fields/, "else {\n" + urlFix.replace(/\n/g, '\n                            ') + "            // 3. Name fields");
code = code.replace(oldRegex, newRegex);

fs.writeFileSync('front/js/security.js', code, 'utf8');
console.log("Fixed security.js to allow URLs and standard characters");
