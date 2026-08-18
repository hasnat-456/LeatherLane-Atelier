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
builder.Services.AddControllers().AddJsonOptions(options =>
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
            "AvailabilityStatus TEXT NULL"
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
        
        if (opened) { conn.Close(); }

        // Set default values for AvailabilityStatus where null
        try
        {
            context.Database.ExecuteSqlRaw("UPDATE Products SET AvailabilityStatus = 'Available' WHERE AvailabilityStatus IS NULL;");
        }
        catch (System.Exception) { }
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
            new { Email = "muhammadbilalarifsheikh@gmail.com", Name = "Muhammad Bilal" },
            new { Email = "admin@leatherlaneatelier.store", Name = "Admin" }
        };

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
        if (!context.SiteSettings.Any())
        {
            context.SiteSettings.Add(new SiteSettings());
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
