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
            
            var builder = WebApplication.CreateBuilder(args);

            // Configure QuestPDF
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

            // Only configure database and identity for non-testing environments
            if (builder.Environment.EnvironmentName != "Testing")
            {
                // Database
                builder.Services.AddDbContext<AppDbContext>(options =>
                    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") 
                        ?? "Data Source=AmplePack.sqlite"));

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
            }
            else
            {
                // For testing environment, add minimal auth services to support [Authorize] attributes
                builder.Services.AddAuthentication("Test")
                    .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, TestAuthenticationHandler>(
                        "Test", options => { });
                
                builder.Services.AddAuthorization(options =>
                {
                    options.DefaultPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
                        .RequireAssertion(_ => true) // Always allow for testing
                        .Build();
                });
            }

            // Services (always needed)
            builder.Services.AddScoped<InvoiceService>();
            builder.Services.AddScoped<EnhancedReportService>();
            builder.Services.AddScoped<OrderManagementService>();
            builder.Services.AddScoped<BoxPriceCalculatorService>();

            var app = builder.Build();

            // Database initialization - only for non-testing environments
            if (app.Environment.EnvironmentName != "Testing")
            {
                using (var scope = app.Services.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    await context.Database.EnsureCreatedAsync();
                    DbSeeder.SeedData(context);
                    await IdentitySeeder.SeedAsync(scope.ServiceProvider);
                }
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

    // Test authentication handler that always succeeds
    public class TestAuthenticationHandler : Microsoft.AspNetCore.Authentication.AuthenticationHandler<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions>
    {
        public TestAuthenticationHandler(Microsoft.Extensions.Options.IOptionsMonitor<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions> options,
            Microsoft.Extensions.Logging.ILoggerFactory logger, System.Text.Encodings.Web.UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        protected override Task<Microsoft.AspNetCore.Authentication.AuthenticateResult> HandleAuthenticateAsync()
        {
            var claims = new[]
            {
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, "TestUser"),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, "1"),
            };

            var identity = new System.Security.Claims.ClaimsIdentity(claims, "Test");
            var principal = new System.Security.Claims.ClaimsPrincipal(identity);
            var ticket = new Microsoft.AspNetCore.Authentication.AuthenticationTicket(principal, "Test");

            return Task.FromResult(Microsoft.AspNetCore.Authentication.AuthenticateResult.Success(ticket));
        }
    }
}
