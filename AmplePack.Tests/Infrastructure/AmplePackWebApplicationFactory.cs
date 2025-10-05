using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AmplePack.Data;
using Microsoft.AspNetCore.Identity;
using AmplePack.Models;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Configuration;

namespace AmplePack.Tests.Infrastructure
{
    /// <summary>
    /// Custom WebApplicationFactory for integration testing
    /// </summary>
    public class AmplePackWebApplicationFactory<TStartup> : WebApplicationFactory<TStartup> where TStartup : class
    {
        private readonly string _databaseName = $"InMemoryTestDb_{Guid.NewGuid()}";

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                // Override configuration for testing
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = "Data Source=:memory:",
                    ["Environment"] = "Testing"
                });
            });

            builder.ConfigureServices(services =>
            {
                // Remove ALL database-related service registrations
                var descriptorsToRemove = services
                    .Where(d => d.ServiceType.Namespace?.Contains("EntityFramework") == true ||
                               d.ServiceType == typeof(DbContextOptions<AppDbContext>) ||
                               d.ServiceType == typeof(AppDbContext) ||
                               d.ImplementationType?.Name.Contains("Sqlite") == true)
                    .ToList();

                foreach (var descriptor in descriptorsToRemove)
                {
                    services.Remove(descriptor);
                }

                // Remove Identity-related services that depend on DbContext
                services.RemoveAll(typeof(UserManager<ApplicationUser>));
                services.RemoveAll(typeof(SignInManager<ApplicationUser>));
                services.RemoveAll(typeof(RoleManager<IdentityRole>));
                services.RemoveAll(typeof(IUserStore<ApplicationUser>));
                services.RemoveAll(typeof(IRoleStore<IdentityRole>));

                // Add InMemory database for testing
                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseInMemoryDatabase(_databaseName);
                    options.EnableSensitiveDataLogging();
                });

                // Re-add simplified Identity for testing without roles/auth requirements
                services.AddIdentityCore<ApplicationUser>()
                    .AddEntityFrameworkStores<AppDbContext>();

                // Build service provider to initialize the database
                var serviceProvider = services.BuildServiceProvider();
                using var scope = serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                
                try
                {
                    context.Database.EnsureCreated();
                    SeedTestData(context);
                }
                catch (Exception ex)
                {
                    var logger = scope.ServiceProvider.GetService<ILogger<AmplePackWebApplicationFactory<TStartup>>>();
                    logger?.LogError(ex, "An error occurred seeding the test database");
                    // Don't throw - let tests continue
                }
            });

            builder.UseEnvironment("Testing");
        }

        private static void SeedTestData(AppDbContext context)
        {
            // Clear existing data
            if (context.Orders.Any())
                context.Orders.RemoveRange(context.Orders);
            if (context.Customers.Any())
                context.Customers.RemoveRange(context.Customers);
            if (context.CustomerProducts.Any())
                context.CustomerProducts.RemoveRange(context.CustomerProducts);
            
            context.SaveChanges();

            // Add test customers
            var customers = new[]
            {
                new Customer 
                { 
                    Id = 1, 
                    Name = "Test Customer 1", 
                    Email = "test1@example.com", 
                    Contact = "123-456-7890",
                    Address = "123 Test St, Test City, TS 12345"
                },
                new Customer 
                { 
                    Id = 2, 
                    Name = "Test Customer 2", 
                    Email = "test2@example.com", 
                    Contact = "098-765-4321",
                    Address = "456 Test Ave, Test Town, TT 54321"
                }
            };

            context.Customers.AddRange(customers);
            context.SaveChanges();

            // Add test orders
            var orders = new[]
            {
                new Order
                {
                    Id = 1,
                    CustomerId = 1,
                    Date = DateTime.UtcNow,
                    TotalAmount = 100.00m,
                    Status = "Pending"
                },
                new Order
                {
                    Id = 2,
                    CustomerId = 2,
                    Date = DateTime.UtcNow.AddDays(-1),
                    TotalAmount = 250.00m,
                    Status = "Completed"
                }
            };

            context.Orders.AddRange(orders);
            context.SaveChanges();
        }

        public void SeedTestData()
        {
            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            SeedTestData(context);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    // Simple cleanup without database operations
                    // InMemory database will be automatically disposed
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
            base.Dispose(disposing);
        }
    }
}