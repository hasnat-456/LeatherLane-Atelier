const fs = require('fs');
const path = require('path');

// 1. UPDATE PROGRAM.CS (Backend)
let programCs = fs.readFileSync('back/Program.cs', 'utf8');

const oldMiddleware = `    // Clean URL Rewrite Middleware
    app.Use(async (context, next) =>
    {
        var path = context.Request.Path.Value;
        if (!string.IsNullOrEmpty(path) && !path.StartsWith("/api") && !path.StartsWith("/images") && !System.IO.Path.HasExtension(path))
        {
            var htmlPath = path + ".html";
            var physicalPath = System.IO.Path.Combine(frontPath, htmlPath.TrimStart('/'));
            if (System.IO.File.Exists(physicalPath))
            {
                context.Request.Path = htmlPath;
            }
        }
        await next();
    });`;

const newMiddleware = `    // Clean URL Rewrite Middleware (Strict)
    app.Use(async (context, next) =>
    {
        var path = context.Request.Path.Value;
        
        // 1. Force remove .html from browser URL if requested explicitly
        if (!string.IsNullOrEmpty(path) && path.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
        {
            var cleanPath = path.Substring(0, path.Length - 5);
            if (cleanPath.Equals("/index", StringComparison.OrdinalIgnoreCase))
                cleanPath = "/home";
            
            var qs = context.Request.QueryString.HasValue ? context.Request.QueryString.Value : "";
            context.Response.Redirect(cleanPath + qs, permanent: true);
            return;
        }

        // 2. Secretly append .html on the server side so static files work
        if (!string.IsNullOrEmpty(path) && !path.StartsWith("/api") && !path.StartsWith("/images") && !System.IO.Path.HasExtension(path))
        {
            var htmlPath = (path == "/" ? "/home" : path) + ".html";
            var physicalPath = System.IO.Path.Combine(frontPath, htmlPath.TrimStart('/'));
            if (System.IO.File.Exists(physicalPath))
            {
                context.Request.Path = htmlPath;
            }
        }
        await next();
    });`;

if (programCs.includes('// Clean URL Rewrite Middleware')) {
    programCs = programCs.replace(oldMiddleware, newMiddleware);
    fs.writeFileSync('back/Program.cs', programCs, 'utf8');
    console.log("Updated Program.cs with strict 301 redirects.");
} else {
    console.log("Could not find middleware to replace in Program.cs!");
}

// 2. CLEAN FRONTEND FILES (HTML & JS)
function cleanUrlsInDirectory(dir) {
    const files = fs.readdirSync(dir);
    for (const file of files) {
        const filePath = path.join(dir, file);
        const stat = fs.statSync(filePath);
        if (stat.isDirectory()) {
            cleanUrlsInDirectory(filePath);
        } else if (file.endsWith('.html') || file.endsWith('.js')) {
            let content = fs.readFileSync(filePath, 'utf8');
            
            // Regex to replace href="xxx.html" with href="/xxx"
            // Be careful not to break external links or .html text
            // We mainly want to target local links
            
            // Replaces href="deals.html" -> href="/deals"
            // Replaces href="home.html" -> href="/home"
            const linkRegex = /href="([a-zA-Z0-9_-]+)\.html([^"]*)"/g;
            content = content.replace(linkRegex, 'href="/$1$2"');

            // Replaces href='deals.html' -> href='/deals'
            const linkRegexSingle = /href='([a-zA-Z0-9_-]+)\.html([^']*)'/g;
            content = content.replace(linkRegexSingle, "href='/$1$2'");

            // Replaces window.location.href = 'deals.html' -> '/deals'
            const jsLocationRegex = /window\.location\.href\s*=\s*'([a-zA-Z0-9_-]+)\.html([^']*)'/g;
            content = content.replace(jsLocationRegex, "window.location.href = '/$1$2'");
            
            // Double quotes for window.location
            const jsLocationRegexDouble = /window\.location\.href\s*=\s*"([a-zA-Z0-9_-]+)\.html([^"]*)"/g;
            content = content.replace(jsLocationRegexDouble, 'window.location.href = "/$1$2"');

            // Special case in auth.js where they build HTML strings: `<a href="deals.html"`
            const innerHtmlRegex = /<a href="([a-zA-Z0-9_-]+)\.html([^"]*)"/g;
            content = content.replace(innerHtmlRegex, '<a href="/$1$2"');

            fs.writeFileSync(filePath, content, 'utf8');
        }
    }
}

cleanUrlsInDirectory('front');
console.log("Cleaned .html links from frontend files.");
