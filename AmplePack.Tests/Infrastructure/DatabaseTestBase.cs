using Microsoft.EntityFrameworkCore;
using AmplePack.Data;
using AmplePack.Models;

namespace AmplePack.Tests.Infrastructure
{
    /// <summary>
    /// Base class for tests that need a database context
    /// </summary>
    public abstract class DatabaseTestBase : IDisposable
    {
        protected AppDbContext Context { get; }

        protected DatabaseTestBase()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            Context = new AppDbContext(options);
            Context.Database.EnsureCreated();
            SeedDatabase();
        }

        protected virtual void SeedDatabase()
        {
            // Override in derived classes to add specific test data
        }

        protected Customer CreateTestCustomer(string name = "Test Customer", string email = "test@example.com")
        {
            return new Customer
            {
                Name = name,
                Email = email,
                Contact = "123-456-7890",
                Address = "123 Test St, Test City, TS 12345"
            };
        }

        protected Order CreateTestOrder(int customerId, decimal amount = 100.00m, string status = "Pending")
        {
            return new Order
            {
                CustomerId = customerId,
                Date = DateTime.UtcNow,
                TotalAmount = amount,
                Status = status
            };
        }

        protected CustomerProduct CreateTestCustomerProduct(int customerId, string productName = "Test Product")
        {
            return new CustomerProduct
            {
                CustomerId = customerId,
                ProductName = productName,
                Description = "Test product description",
                Length = 10,
                Width = 8,
                Height = 6,
                GSM = 150,
                PaperType = "Kraft",
                PrintingType = "Flexo",
                PricePerBox = 50.00m,
                IsActive = true
            };
        }

        public void Dispose()
        {
            Context.Dispose();
        }
    }
}