const fs = require('fs');
let code = fs.readFileSync('back/Program.cs', 'utf8');

const injectionPoint = `app.UseDefaultFiles(`;
const middleware = `
    // Clean URL Rewrite Middleware
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
    });

    `;

if (code.includes(injectionPoint) && !code.includes("Clean URL Rewrite Middleware")) {
    code = code.replace(injectionPoint, middleware + injectionPoint);
    fs.writeFileSync('back/Program.cs', code, 'utf8');
    console.log("Injected URL rewrite middleware");
}
