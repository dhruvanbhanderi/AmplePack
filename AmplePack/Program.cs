using AmplePack.Data;
using AmplePack.Models;
using AmplePack.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;
using System.Text;

namespace AmplePack
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            // Ensure UTF-8 encoding for console output
            Console.OutputEncoding = Encoding.UTF8;

            // Enable QuestPDF debugging globally to capture layout diagnostics in published builds
            // This will cause QuestPDF to write debug files to the temp folder when a layout error occurs
            try
            {
                QuestPDF.Settings.EnableDebugging = true;
            }
            catch
            {
                // ignore if not available at runtime
            }

            var builder = WebApplication.CreateBuilder(args);

            // Configure QuestPDF license (already present)
            QuestPDF.Settings.License = LicenseType.Community;

            // Add services
            builder.Services.AddControllersWithViews();

            // Configure request localization for proper encoding
            builder.Services.Configure<RequestLocalizationOptions>(options =>
            {
                options.DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture("en-US");
                options.SupportedCultures = new[] { new System.Globalization.CultureInfo("en-US") };
                options.SupportedUICultures = new[] { new System.Globalization.CultureInfo("en-US") };
            });

            // Database
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") 
                    ?? "Data Source=AmplePack.sqlite"));

            // Services
            builder.Services.AddScoped<InvoiceService>();
            builder.Services.AddScoped<EnhancedReportService>();
            builder.Services.AddScoped<OrderManagementService>();
            builder.Services.AddScoped<BoxPriceCalculatorService>();

            // Identity
            builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = true;
                options.Password.RequiredLength = 6;
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedEmail = false;
                options.SignIn.RequireConfirmedPhoneNumber = false;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

            // Cookies
            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.Cookie.HttpOnly = true;
                options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
                options.LoginPath = "/Account/Login";
                options.AccessDeniedPath = "/Account/AccessDenied";
                options.SlidingExpiration = true;
            });

            var app = builder.Build();

            // Log QuestPDF temp path for diagnostics
            try
            {
                var tempPath = System.IO.Path.GetTempPath();
                Console.WriteLine($"QuestPDF debug temp path: {tempPath}");
            }
            catch { }

            // Database initialization
            using (var scope = app.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                await context.Database.EnsureCreatedAsync();
                DbSeeder.SeedData(context);
                await IdentitySeeder.SeedAsync(scope.ServiceProvider);
            }

            // Configure pipeline
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            
            // Use request localization
            app.UseRequestLocalization();
            
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            await app.RunAsync();
        }
    }
}
