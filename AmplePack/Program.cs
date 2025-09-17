using AmplePack.Data;
using AmplePack.Models;
using AmplePack.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;

namespace AmplePack
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            try
            {
                // Configure QuestPDF to use the Community license
                QuestPDF.Settings.License = LicenseType.Community;
                
                var builder = WebApplication.CreateBuilder(args);

                // Add services to the container
                builder.Services.AddControllersWithViews();

                // Register AppDbContext with SQLite
                builder.Services.AddDbContext<AppDbContext>(options =>
                    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") 
                        ?? "Data Source=AmplePack.sqlite"));

                // Register services
                builder.Services.AddScoped<InvoiceService>();

                // Configure Identity
                builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
                {
                    // Password settings
                    options.Password.RequireDigit = true;
                    options.Password.RequireLowercase = true;
                    options.Password.RequireNonAlphanumeric = false;
                    options.Password.RequireUppercase = true;
                    options.Password.RequiredLength = 6;

                    // User settings
                    options.User.RequireUniqueEmail = true;

                    // Sign-in settings
                    options.SignIn.RequireConfirmedEmail = false;
                    options.SignIn.RequireConfirmedPhoneNumber = false;
                })
                .AddEntityFrameworkStores<AppDbContext>()
                .AddDefaultTokenProviders();

                // Configure cookie settings
                builder.Services.ConfigureApplicationCookie(options =>
                {
                    options.Cookie.HttpOnly = true;
                    options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
                    options.LoginPath = "/Account/Login";
                    options.AccessDeniedPath = "/Account/AccessDenied";
                    options.SlidingExpiration = true;
                });

                var app = builder.Build();

                // Ensure database is created and seed data
                using (var scope = app.Services.CreateScope())
                {
                    try
                    {
                        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                        
                        // Ensure database is created
                        await context.Database.EnsureCreatedAsync();
                        
                        // Seed the database
                        DbSeeder.SeedData(context);

                        // Seed Identity data
                        await IdentitySeeder.SeedAsync(scope.ServiceProvider);
                    }
                    catch (Exception ex)
                    {
                        // Log the error but don't crash the application
                        Console.WriteLine($"Database seeding error: {ex.Message}");
                        // Continue with app startup
                    }
                }

                // Configure the HTTP request pipeline
                if (!app.Environment.IsDevelopment())
                {
                    app.UseExceptionHandler("/Home/Error");
                    app.UseHsts();
                }

                app.UseHttpsRedirection();
                app.UseStaticFiles();
                app.UseRouting();

                app.UseAuthentication();
                app.UseAuthorization();

                app.MapStaticAssets();
                app.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}")
                    .WithStaticAssets();

                Console.WriteLine("AmplePack application started successfully!");
                app.Run();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Application startup failed: {ex.Message}");
                throw;
            }
        }
    }
}
