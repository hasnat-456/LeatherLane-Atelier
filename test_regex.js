const fs = require('fs');
const content = fs.readFileSync('front/home.html', 'utf8');
const regex = /<div id="dynamicFeaturedCategories">[\s\S]*?Loading Products\.\.\.[\s\S]*?<\/div>\s*<\/div>\s*<\/div>/;
const match = content.match(regex);
if (match) {
    console.log("MATCHED STRING LENGTH: ", match[0].length);
    console.log("ENDS WITH:\n", match[0].substring(match[0].length - 200));
} else {
    console.log("NO MATCH");
}
