const fs = require('fs');

let programCs = fs.readFileSync('back/Program.cs', 'utf8');

const preloadSsrLogic = `
                        // --- Preload Hero Image SSR ---
                        var settings = await dbContext.SiteSettings.AsNoTracking().FirstOrDefaultAsync();
                        if (settings != null && !string.IsNullOrEmpty(settings.HeroSliderImages))
                        {
                            try {
                                var images = System.Text.Json.JsonSerializer.Deserialize<List<string>>(settings.HeroSliderImages);
                                if (images != null && images.Any())
                                {
                                    var firstHero = images.First();
                                    if (!firstHero.StartsWith("/")) firstHero = "/" + firstHero;
                                    var preloadTag = $"<link rel=\\"preload\\" as=\\"image\\" href=\\"{firstHero}\\">\\n</head>";
                                    htmlContent = htmlContent.Replace("</head>", preloadTag);
                                }
                            } catch { }
                        }
`;

// Insert the preload logic before the target string replacement
programCs = programCs.replace(
    /var targetString = @"<div style=""padding: 40px; text-align: center; color:#666; \r?\ngrid-column: 1\/-1;"">Loading collection\.\.\.<\/div>";/,
    preloadSsrLogic + '\n                          var targetString = @"<div style=\\"padding: 40px; text-align: center; color:#666; \\ngrid-column: 1/-1;\\">Loading collection...</div>";'
);

fs.writeFileSync('back/Program.cs', programCs, 'utf8');
console.log("Program.cs updated with Preload SSR!");
