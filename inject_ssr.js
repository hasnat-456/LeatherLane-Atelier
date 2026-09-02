const fs = require('fs');

let program = fs.readFileSync('back/Program.cs', 'utf8');

const middlewareCode = `
    // SSR Middleware for Products (SEO)
    app.Use(async (context, next) =>
    {
        var path = context.Request.Path.Value;
        
        if (path != null && (path.Equals("/home.html", StringComparison.OrdinalIgnoreCase) || path.Equals("/products.html", StringComparison.OrdinalIgnoreCase)))
        {
            var physicalPath = System.IO.Path.Combine(frontPath, path.TrimStart('/'));
            if (System.IO.File.Exists(physicalPath))
            {
                var htmlContent = await System.IO.File.ReadAllTextAsync(physicalPath);
                
                try 
                {
                    using var scope = app.Services.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    
                    var categories = await dbContext.ProductCategories
                        .Where(c => c.IsActive)
                        .OrderBy(c => c.DisplayOrder)
                        .ThenBy(c => c.Name)
                        .ToListAsync();
                            
                    var allProducts = await dbContext.Products
                        .Where(p => p.Available)
                        .OrderByDescending(p => p.CreatedAt)
                        .ToListAsync();
                    
                    var sb = new System.Text.StringBuilder();
                    
                    if (path.Equals("/home.html", StringComparison.OrdinalIgnoreCase))
                    {
                        if (allProducts.Any())
                        {
                            sb.Append("<section class='products-section' style='padding: 4rem 2rem; background-color: #fff; border-bottom: 1px solid #eaeaea;'>");
                            sb.Append("<div style='display: flex; justify-content: space-between; align-items: flex-end; margin-bottom: 2rem; max-width: 1200px; margin-left: auto; margin-right: auto; padding: 0 1rem;'>");
                            sb.Append("<h3 style='font-family: var(--font-heading); color: var(--primary-bg); font-size: 2.2rem; text-transform: uppercase; margin: 0; border-bottom: 2px solid var(--primary-gold); padding-bottom: 5px;'>New Arrivals</h3>");
                            if (allProducts.Count > 4) sb.Append("<a href='/products' style='color: var(--primary-gold); font-weight: 600; text-decoration: none; font-size: 1rem; transition: opacity 0.2s;'>Shop All New Arrivals &rarr;</a>");
                            sb.Append("</div><div class='product-grid' style='margin-bottom: 2rem;'>");
                            
                            foreach(var p in allProducts.Take(4))
                            {
                                var img = string.IsNullOrEmpty(p.Thumbnail) ? "https://via.placeholder.com/300x250" : p.Thumbnail;
                                sb.Append($"<div class='product-card'><a href='/product-detail?id={p.Id}' style='text-decoration: none; color: inherit;'><img src='{img}' class='product-img' style='width: 100%; height: 250px; object-fit: cover;'><div class='product-info'><div class='product-category'>{p.Category}</div><h3 class='product-title'>{p.Name}</h3><div class='product-price'>Rs {p.Price:N0}</div></div></a></div>");
                            }
                            sb.Append("</div></section>");
                        }
                        
                        int catIndex = 0;
                        foreach(var cat in categories)
                        {
                            var items = allProducts.Where(p => string.Equals(p.Category, cat.Name, StringComparison.OrdinalIgnoreCase)).ToList();
                            if (!items.Any()) continue;
                            
                            string bgColor = catIndex % 2 == 0 ? "var(--light-bg)" : "#fff";
                            catIndex++;
                            
                            sb.Append($"<section class='products-section' style='padding: 4rem 2rem; background-color: {bgColor}; border-bottom: 1px solid #eaeaea;'>");
                            sb.Append("<div style='display: flex; justify-content: space-between; align-items: flex-end; margin-bottom: 2rem; max-width: 1200px; margin-left: auto; margin-right: auto; padding: 0 1rem;'>");
                            sb.Append($"<h3 style='font-family: var(--font-heading); color: var(--primary-bg); font-size: 2.2rem; text-transform: uppercase; margin: 0; border-bottom: 2px solid var(--primary-gold); padding-bottom: 5px;'>{cat.Name}</h3>");
                            if (items.Count > 4) sb.Append($"<a href='/products?category={System.Uri.EscapeDataString(cat.Name)}' style='color: var(--primary-gold); font-weight: 600; text-decoration: none; font-size: 1rem; transition: opacity 0.2s;'>See More in {cat.Name} &rarr;</a>");
                            sb.Append("</div><div class='product-grid' style='margin-bottom: 2rem;'>");
                            
                            foreach(var p in items.Take(4))
                            {
                                var img = string.IsNullOrEmpty(p.Thumbnail) ? "https://via.placeholder.com/300x250" : p.Thumbnail;
                                sb.Append($"<div class='product-card'><a href='/product-detail?id={p.Id}' style='text-decoration: none; color: inherit;'><img src='{img}' class='product-img' style='width: 100%; height: 250px; object-fit: cover;'><div class='product-info'><div class='product-category'>{p.Category}</div><h3 class='product-title'>{p.Name}</h3><div class='product-price'>Rs {p.Price:N0}</div></div></a></div>");
                            }
                            sb.Append("</div></section>");
                        }
                        
                        var regex = new System.Text.RegularExpressions.Regex(@"<div id=""dynamicFeaturedCategories"">[\\s\\S]*?Loading Products\\.\\.\\.[\\s\\S]*?</div>\\s*</div>\\s*</div>");
                        htmlContent = regex.Replace(htmlContent, @"<div id=""dynamicFeaturedCategories"">" + sb.ToString() + @"</div>");
                    }
                    else if (path.Equals("/products.html", StringComparison.OrdinalIgnoreCase))
                    {
                        foreach(var p in allProducts)
                        {
                            var img = string.IsNullOrEmpty(p.Thumbnail) ? "https://via.placeholder.com/300x250" : p.Thumbnail;
                            sb.Append($"<div class='product-card' data-category='{p.Category?.ToLower()}' data-price='{p.Price}'><a href='/product-detail?id={p.Id}' style='text-decoration: none; color: inherit;'><img src='{img}' class='product-img' style='width: 100%; height: 250px; object-fit: cover;'><div class='product-info'><div class='product-category'>{p.Category}</div><h3 class='product-title'>{p.Name}</h3><div class='product-price'>Rs {p.Price:N0}</div></div></a></div>");
                        }
                        
                        var targetString = @"<div style=""padding: 40px; text-align: center; color:#666; grid-column: 1/-1;"">Loading collection...</div>";
                        htmlContent = htmlContent.Replace(targetString, sb.ToString());
                    }
                }
                catch (System.Exception ex)
                {
                    System.Console.WriteLine("SSR Failed: " + ex.Message);
                }

                context.Response.ContentType = "text/html; charset=utf-8";
                await context.Response.WriteAsync(htmlContent);
                return;
            }
        }
        await next();
    });

    app.UseDefaultFiles`;

// Only inject if not already injected
if (!program.includes('// SSR Middleware for Products (SEO)')) {
    program = program.replace('app.UseDefaultFiles', middlewareCode);
    fs.writeFileSync('back/Program.cs', program, 'utf8');
    console.log("Injected SSR middleware into Program.cs");
} else {
    console.log("Already injected.");
}
