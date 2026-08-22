const fs = require('fs');

let about = fs.readFileSync('front/about.html', 'utf8');
about = about.replace(/<script src="js\/auth\.js[^"]*"><\/script>/g, '<script src="js/auth.js?v=999999"></script>');
fs.writeFileSync('front/about.html', about, 'utf8');

let fav = fs.readFileSync('front/favorites.html', 'utf8');
fav = fav.replace(/<script src="js\/auth\.js"><\/script>/g, '<script src="js/auth.js?v=999999"></script>');
fs.writeFileSync('front/favorites.html', fav, 'utf8');

console.log("Fixed script tags");
