const fs = require('fs');

let programCs = fs.readFileSync('back/Program.cs', 'utf8');

// 1. Add loading='lazy' to product SSR (Home & Products page)
programCs = programCs.replace(/<img src='\{img\}' class='product-img'/g, "<img src='{img}' class='product-img' loading='lazy'");

// 2. Add SSR logic for Stories to Program.cs
const storySsrLogic = `
                        // --- Story SSR ---
                        var recentStories = await dbContext.Blogs.AsNoTracking().OrderByDescending(b => b.CreatedAt).Take(3).ToListAsync();
                        if (recentStories.Any())
                        {
                            var storySb = new System.Text.StringBuilder();
                            foreach(var story in recentStories)
                            {
                                var sImg = string.IsNullOrEmpty(story.Image) ? "https://via.placeholder.com/400x250" : story.Image;
                                var sCat = string.IsNullOrEmpty(story.Category) ? "Journal" : story.Category;
                                storySb.Append($@"
                                <div style='background: #fff; border-radius: 8px; overflow: hidden; box-shadow: 0 4px 15px rgba(0,0,0,0.05); transition: transform 0.3s; cursor: pointer;' onmouseover=""this.style.transform='translateY(-5px)'"" onmouseout=""this.style.transform='translateY(0)'"" onclick=""window.location.href='/story?id={story.Id}'"">
                                    <img src='{sImg}' style='width: 100%; height: 250px; object-fit: cover;' loading='lazy'>
                                    <div style='padding: 1.5rem;'>
                                        <div style='color: var(--primary-gold); font-size: 0.8rem; font-weight: 600; text-transform: uppercase; margin-bottom: 0.5rem;'>{sCat}</div>
                                        <h4 style='font-family: var(--font-heading); color: var(--primary-bg); font-size: 1.4rem; margin: 0 0 1rem 0;'>{story.Title}</h4>
                                        <p style='color: #666; font-size: 0.95rem; line-height: 1.5; margin: 0;'>{story.Excerpt ?? ""}</p>
                                    </div>
                                </div>");
                            }
                            
                            var storyRegex = new System.Text.RegularExpressions.Regex(@"<div id=""dynamicJournalGrid""[^>]*>[\s\S]*?</div>");
                            htmlContent = storyRegex.Replace(htmlContent, @"<div id=""dynamicJournalGrid"" style=""display: grid; grid-template-columns: repeat(auto-fill, minmax(350px, 1fr)); gap: 2rem; max-width: 1200px; margin: 0 auto;"">" + storySb.ToString() + @"</div>");
                            
                            // Make sure the section is visible
                            htmlContent = htmlContent.Replace(@"id=""atelierJournalSection"" style=""background-color: #F8F5F2; padding: 4rem 3rem; text-align: left; display: none;""", @"id=""atelierJournalSection"" style=""background-color: #F8F5F2; padding: 4rem 3rem; text-align: left; display: block;""");
                        }
`;

// Insert the story SSR logic right after dynamicFeaturedCategories logic
programCs = programCs.replace(
    /htmlContent = regex\.Replace\(htmlContent, @"<div id=""dynamicFeaturedCategories"">" \+ sb\.ToString\(\) \+ @"<\/div>"\);/,
    'htmlContent = regex.Replace(htmlContent, @"<div id=""dynamicFeaturedCategories"">" + sb.ToString() + @"</div>");\n' + storySsrLogic
);

fs.writeFileSync('back/Program.cs', programCs, 'utf8');
console.log("Program.cs updated with Lazy Loading and Story SSR!");
