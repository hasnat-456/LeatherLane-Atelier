const fs = require('fs');

let authJs = fs.readFileSync('front/js/auth.js', 'utf8');

// The block ends with:
//     document.body.appendChild(container);
// });
// We need it to be:
//     document.body.appendChild(container);
//     }
// });

authJs = authJs.replace(/document\.body\.appendChild\(container\);\s*\n\s*}\);/, "document.body.appendChild(container);\n    }\n});");

fs.writeFileSync('front/js/auth.js', authJs, 'utf8');
console.log("Fixed syntax error in auth.js");
