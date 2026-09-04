using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using System.IO;
using System.Text;
using LeatherLane_Atelier.Models;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers(options => 
{
    options.Filters.Add<LeatherLaneAtelier.Filters.AntiXssFilter>();
}).AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});
builder.Services.AddMemoryCache();

var secretKey = builder.Configuration["JwtSettings:Secret"] ?? "super_secret_key_12345678901234567890";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true
        };
    });

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=app.db"));
}
else
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
}

builder.Services.AddOpenApi();
builder.Services.AddScoped<LeatherLane_Atelier.Services.IEmailService, LeatherLane_Atelier.Services.EmailService>();
builder.Services.Configure<LeatherLane_Atelier.Models.PayfastSettings>(builder.Configuration.GetSection("PayfastSettings"));
builder.Services.AddScoped<LeatherLane_Atelier.Services.IPayfastService, LeatherLane_Atelier.Services.PayfastService>();
builder.Services.AddHostedService<LeatherLane_Atelier.Services.ReviewReminderService>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseDeveloperExceptionPage();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();
// Dynamically locate the 'front' folder regardless of working directory
var currentDir = Directory.GetCurrentDirectory();
var frontPath = currentDir.EndsWith("back", StringComparison.OrdinalIgnoreCase) 
    ? Path.Combine(currentDir, "..", "front") 
    : Path.Combine(currentDir, "front");
frontPath = Path.GetFullPath(frontPath);

// Only add static file serving if the folder exists to prevent crashes
if (Directory.Exists(frontPath))
{
    var fileProvider = new PhysicalFileProvider(frontPath);

    
    // Clean URL Rewrite Middleware (Strict)
    app.Use(async (context, next) =>
    {
        var path = context.Request.Path.Value;
        
        // 1. Force remove .html from browser URL if requested explicitly
        if (!string.IsNullOrEmpty(path) && path.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
        {
            var cleanPath = path.Substring(0, path.Length - 5);
            if (cleanPath.Equals("/index", StringComparison.OrdinalIgnoreCase))
                cleanPath = "/";
            
            var qs = context.Request.QueryString.HasValue ? context.Request.QueryString.Value : "";
            context.Response.Redirect(cleanPath + qs, permanent: true);
            return;
        }

        // 2. Secretly append .html on the server side so static files work
        if (!string.IsNullOrEmpty(path) && !path.StartsWith("/api") && !path.StartsWith("/images") && !System.IO.Path.HasExtension(path))
        {
            var htmlPath = (path == "/" || path.Equals("/splash", StringComparison.OrdinalIgnoreCase) || path.Equals("/index", StringComparison.OrdinalIgnoreCase) ? "/index" : path) + ".html";
            var physicalPath = System.IO.Path.Combine(frontPath, htmlPath.TrimStart('/'));
            if (System.IO.File.Exists(physicalPath))
            {
                context.Request.Path = htmlPath;
            }
        }
        await next();
    });

    
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
                            sb.Append("<div class='section-header-row'>");
                            sb.Append("<h3 class='section-header-title'>New Arrivals</h3>");
                            if (allProducts.Count > 4) sb.Append("<a href='/products' class='section-view-all-link'>Shop All &rarr;</a>");
                            sb.Append("</div><div class='product-grid' style='margin-bottom: 2rem;'>");
                            
                            foreach(var p in allProducts.Take(4))
                            {
                                var img = string.IsNullOrEmpty(p.Thumbnail) ? "https://via.placeholder.com/300x250" : p.Thumbnail;
                                sb.Append($"<div class='product-card'><a href='/product-detail?id={p.Id}' style='text-decoration: none; color: inherit;'><img src='{img}' class='product-img' loading='lazy' style='width: 100%; height: 250px; object-fit: cover;'><div class='product-info'><div class='product-category'>{p.Category}</div><h3 class='product-title'>{p.Name}</h3><div class='product-price'>Rs {p.Price:N0}</div></div></a></div>");
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
                            sb.Append("<div class='section-header-row'>");
                            sb.Append($"<h3 class='section-header-title'>{cat.Name}</h3>");
                            if (items.Count > 4) sb.Append($"<a href='/products?category={System.Uri.EscapeDataString(cat.Name)}' class='section-view-all-link'>See More &rarr;</a>");
                            sb.Append("</div><div class='product-grid' style='margin-bottom: 2rem;'>");
                            
                            foreach(var p in items.Take(4))
                            {
                                var img = string.IsNullOrEmpty(p.Thumbnail) ? "https://via.placeholder.com/300x250" : p.Thumbnail;
                                sb.Append($"<div class='product-card'><a href='/product-detail?id={p.Id}' style='text-decoration: none; color: inherit;'><img src='{img}' class='product-img' loading='lazy' style='width: 100%; height: 250px; object-fit: cover;'><div class='product-info'><div class='product-category'>{p.Category}</div><h3 class='product-title'>{p.Name}</h3><div class='product-price'>Rs {p.Price:N0}</div></div></a></div>");
                            }
                            sb.Append("</div></section>");
                        }
                        
                        var regex = new System.Text.RegularExpressions.Regex(@"<div id=""dynamicFeaturedCategories"">[\s\S]*?Loading Products\.\.\.[\s\S]*?</div>\s*</div>\s*</div>");
                        htmlContent = regex.Replace(htmlContent, @"<div id=""dynamicFeaturedCategories"">" + sb.ToString() + @"</div>");

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

                    }
                    else if (path.Equals("/products.html", StringComparison.OrdinalIgnoreCase))
                    {
                        foreach(var p in allProducts)
                        {
                            var img = string.IsNullOrEmpty(p.Thumbnail) ? "https://via.placeholder.com/300x250" : p.Thumbnail;
                            sb.Append($"<div class='product-card' data-category='{p.Category?.ToLower()}' data-price='{p.Price}'><a href='/product-detail?id={p.Id}' style='text-decoration: none; color: inherit;'><img src='{img}' class='product-img' loading='lazy' style='width: 100%; height: 250px; object-fit: cover;'><div class='product-info'><div class='product-category'>{p.Category}</div><h3 class='product-title'>{p.Name}</h3><div class='product-price'>Rs {p.Price:N0}</div></div></a></div>");
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

    app.UseDefaultFiles(new DefaultFilesOptions
    {
        FileProvider = fileProvider
    });

    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = fileProvider,
        RequestPath = "",
        OnPrepareResponse = ctx =>
        {
            var path = ctx.File.PhysicalPath;
            if (path != null && (path.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) || 
                                 path.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                                 path.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                                 path.EndsWith(".webp", StringComparison.OrdinalIgnoreCase) ||
                                 path.EndsWith(".css", StringComparison.OrdinalIgnoreCase) ||
                                 path.EndsWith(".js", StringComparison.OrdinalIgnoreCase)))
            {
                ctx.Context.Response.Headers.Append("Cache-Control", "public,max-age=604800"); // Cache for 7 days
            }
            else
            {
                ctx.Context.Response.Headers.Append("Cache-Control", "no-cache, no-store, must-revalidate");
                ctx.Context.Response.Headers.Append("Pragma", "no-cache");
                ctx.Context.Response.Headers.Append("Expires", "0");
            }
        }
    });
}

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Wrap ALL startup DB work in try-catch so app doesn't crash if SQL Server is slow/unreachable
try
{
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    
    try { context.Database.EnsureCreated(); }
    catch (Exception ex) { Console.WriteLine($"EnsureCreated failed (non-fatal): {ex.Message}"); }
    
    // SQLite Manual Migrations
    if (app.Environment.IsDevelopment())
    {
        try
        {
            context.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS ManualPaymentSettings (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    MethodName TEXT NOT NULL,
                    IsEnabled INTEGER NOT NULL,
                    BankName TEXT NULL,
                    AccountNumber TEXT NULL,
                    IBAN TEXT NULL,
                    AccountTitle TEXT NULL,
                    MobileNumber TEXT NULL,
                    RaastId TEXT NULL
                );
            ");
            context.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS SiteSettings (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Address TEXT NOT NULL,
                    Email TEXT NOT NULL,
                    Phone TEXT NOT NULL,
                    BusinessHours TEXT NOT NULL,
                    FacebookUrl TEXT NOT NULL,
                    InstagramUrl TEXT NOT NULL,
                    WhatsAppUrl TEXT NOT NULL,
                    TikTokUrl TEXT NOT NULL
                );
            ");
        }
        catch (System.Exception ex)
        {
            System.Console.WriteLine($"Error creating ManualPaymentSettings table: {ex.Message}");
        }

        var newColumns = new[]
        {
            "PaymentRefId TEXT NULL",
            "SenderName TEXT NULL",
            "SenderMobile TEXT NULL",
            "PaymentScreenshot TEXT NULL",
            "RejectionReason TEXT NULL"
        };

        var conn = context.Database.GetDbConnection();
        bool opened = false;
        if (conn.State != System.Data.ConnectionState.Open) { conn.Open(); opened = true; }

        foreach (var colDef in newColumns)
        {
            try
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = $"ALTER TABLE Transactions ADD COLUMN {colDef};";
                cmd.ExecuteNonQuery();
            }
            catch (System.Exception)
            {
                // Column probably already exists
            }
        }

        // Category Migration & Column Updates
        try
        {
            context.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS ProductCategories (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL,
                    IsActive INTEGER NOT NULL DEFAULT 1,
                    DisplayOrder INTEGER NOT NULL DEFAULT 0
                );
            ");
        }
        catch (System.Exception ex)
        {
            System.Console.WriteLine($"Error creating ProductCategories table: {ex.Message}");
        }

        var newProductColumns = new[]
        {
            "CategoryId INTEGER NULL",
            "AvailabilityStatus TEXT NULL",
            "ProductId TEXT NULL"
        };

        foreach (var colDef in newProductColumns)
        {
            try
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = $"ALTER TABLE Products ADD COLUMN {colDef};";
                cmd.ExecuteNonQuery();
            }
            catch (System.Exception)
            {
                // Column probably already exists
            }
        }

        var additionalTableColumns = new (string Table, string Column)[]
        {
            ("Transactions", "OrderId TEXT NULL"),
            ("Transactions", "DeliveredAt TEXT NULL"),
            ("Transactions", "ReviewReminderDay2Sent INTEGER NOT NULL DEFAULT 0"),
            ("Transactions", "ReviewReminderDay4Sent INTEGER NOT NULL DEFAULT 0"),
            ("ExchangeRequests", "ExchangeCode TEXT NULL"),
            ("ReturnRequests", "ReturnCode TEXT NULL")
        };

        foreach (var (tbl, colDef) in additionalTableColumns)
        {
            try
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = $"ALTER TABLE {tbl} ADD COLUMN {colDef};";
                cmd.ExecuteNonQuery();
            }
            catch (System.Exception)
            {
                // Column probably already exists
            }
        }
        
        if (opened) { conn.Close(); }

        // Set default values for AvailabilityStatus where null
        try
        {
            context.Database.ExecuteSqlRaw("UPDATE Products SET AvailabilityStatus = 'Available' WHERE AvailabilityStatus IS NULL;");
        }
        catch (System.Exception) { }

        // Backfill Professional Identifiers
        try
        {
            var unassignedProducts = context.Products.Where(p => string.IsNullOrEmpty(p.ProductId)).ToList();
            foreach (var prod in unassignedProducts)
            {
                prod.ProductId = LeatherLane_Atelier.Services.IdGenerator.GenerateProductId(prod.Category);
            }

            var unassignedTransactions = context.Transactions.Where(t => string.IsNullOrEmpty(t.OrderId)).ToList();
            foreach (var tx in unassignedTransactions)
            {
                tx.OrderId = LeatherLane_Atelier.Services.IdGenerator.GenerateOrderId(tx.CreatedAt);
            }

            var unassignedExchanges = context.ExchangeRequests.Where(e => string.IsNullOrEmpty(e.ExchangeCode)).ToList();
            foreach (var ex in unassignedExchanges)
            {
                ex.ExchangeCode = LeatherLane_Atelier.Services.IdGenerator.GenerateExchangeId();
            }

            var unassignedReturns = context.ReturnRequests.Where(r => string.IsNullOrEmpty(r.ReturnCode)).ToList();
            foreach (var ret in unassignedReturns)
            {
                ret.ReturnCode = LeatherLane_Atelier.Services.IdGenerator.GenerateReturnId();
            }

            if (unassignedProducts.Any() || unassignedTransactions.Any() || unassignedExchanges.Any() || unassignedReturns.Any())
            {
                context.SaveChanges();
            }
        }
        catch (System.Exception ex)
        {
            System.Console.WriteLine($"Error backfilling professional IDs: {ex.Message}");
        }
    }

    // Seed default categories for both Dev and Production
    try
    {
        if (!context.ProductCategories.Any())
        {
            context.ProductCategories.AddRange(new System.Collections.Generic.List<ProductCategory>
            {
                new ProductCategory { Name = "Chappal", IsActive = true, DisplayOrder = 1 },
                new ProductCategory { Name = "Peshawari Chappal", IsActive = true, DisplayOrder = 2 },
                new ProductCategory { Name = "Shoes", IsActive = true, DisplayOrder = 3 },
                new ProductCategory { Name = "Sandals", IsActive = true, DisplayOrder = 4 }
            });
            context.SaveChanges();
        }
    }
    catch (System.Exception ex)
    {
        System.Console.WriteLine($"Seeding categories error: {ex.Message}");
    }

    // Backfill CategoryId on existing Products
    try
    {
        var unlinkedProducts = context.Products.Where(p => p.CategoryId == null).ToList();
        if (unlinkedProducts.Any())
        {
            var categories = context.ProductCategories.ToList();
            foreach (var p in unlinkedProducts)
            {
                var cat = categories.FirstOrDefault(c => c.Name.Equals(p.Category, System.StringComparison.OrdinalIgnoreCase));
                if (cat != null)
                {
                    p.CategoryId = cat.Id;
                }
                else
                {
                    var fallbackCat = categories.FirstOrDefault(c => c.Name.Equals("Shoes", System.StringComparison.OrdinalIgnoreCase)) ?? categories.FirstOrDefault();
                    if (fallbackCat != null)
                    {
                        p.CategoryId = fallbackCat.Id;
                        p.Category = fallbackCat.Name;
                    }
                }
            }
            context.SaveChanges();
        }
    }
    catch (System.Exception ex)
    {
        System.Console.WriteLine($"Backfilling categories error: {ex.Message}");
    }

    // Seed Default Manual Payment Settings
    try
    {
        if (!context.ManualPaymentSettings.Any())
        {
            context.ManualPaymentSettings.AddRange(new System.Collections.Generic.List<ManualPaymentSetting>
            {
                new ManualPaymentSetting {
                    MethodName = "Bank Transfer",
                    IsEnabled = true,
                    BankName = "Meezan Bank",
                    AccountTitle = "LeatherLane Atelier",
                    AccountNumber = "1234567890",
                    IBAN = "PK00MEZN0000001234567890"
                },
                new ManualPaymentSetting {
                    MethodName = "JazzCash",
                    IsEnabled = true,
                    AccountTitle = "LeatherLane Atelier",
                    MobileNumber = "03001234567"
                },
                new ManualPaymentSetting {
                    MethodName = "Easypaisa",
                    IsEnabled = true,
                    AccountTitle = "LeatherLane Atelier",
                    MobileNumber = "03121234567"
                },
                new ManualPaymentSetting {
                    MethodName = "Raast ID",
                    IsEnabled = true,
                    AccountTitle = "LeatherLane Atelier",
                    RaastId = "leatherlane@raast"
                }
            });
            context.SaveChanges();
        }
    }
    catch (System.Exception ex)
    {
        System.Console.WriteLine($"Seeding payment settings error: {ex.Message}");
    }
    
    // Seed and Ensure Admin Accounts
    try
    {
        var adminList = new[]
        {
            new { Email = "leatherlaneatelier@gmail.com", Name = "Muhammad Bilal" }
        };

        // Remove deprecated secondary admin account if present in DB
        var oldAdmin = context.Users.FirstOrDefault(u => u.Email.ToLower() == "admin@leatherlaneatelier.store");
        if (oldAdmin != null)
        {
            context.Users.Remove(oldAdmin);
            context.SaveChanges();
        }

        foreach (var adminInfo in adminList)
        {
            var adminUser = context.Users.FirstOrDefault(u => u.Email.ToLower() == adminInfo.Email.ToLower());
            if (adminUser == null)
            {
                context.Users.Add(new User
                {
                    Name = adminInfo.Name,
                    Email = adminInfo.Email,
                    Password = BCrypt.Net.BCrypt.HashPassword("Bilal123@@@"),
                    Role = "Admin",
                    IsVerified = true
                });
            }
            else
            {
                adminUser.Role = "Admin";
                adminUser.IsVerified = true;
                adminUser.Password = BCrypt.Net.BCrypt.HashPassword("Bilal123@@@");
            }
        }
        context.SaveChanges();
    }
    catch (System.Exception ex)
    {
        System.Console.WriteLine($"Seeding admin error: {ex.Message}");
    }

    try
    {
        var siteSetting = context.SiteSettings.FirstOrDefault();
        if (siteSetting == null)
        {
            context.SiteSettings.Add(new SiteSettings());
            context.SaveChanges();
        }
        else
        {
            if (siteSetting.FacebookUrl == "#") siteSetting.FacebookUrl = "";
            if (siteSetting.TikTokUrl == "#") siteSetting.TikTokUrl = "";
            if (siteSetting.WhatsAppUrl == "#") siteSetting.WhatsAppUrl = "03376306162";
            context.SaveChanges();
        }
    }
    catch (System.Exception ex)
    {
        System.Console.WriteLine($"Seeding site settings error: {ex.Message}");
    }
}
}
catch (Exception ex)
{
    Console.WriteLine($"STARTUP DB ERROR (app will still run): {ex.Message}");
}

app.Run();

