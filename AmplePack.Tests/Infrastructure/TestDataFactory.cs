using AmplePack.Models;
using Bogus;

namespace AmplePack.Tests.Infrastructure
{
    /// <summary>
    /// Test data factory using Bogus for generating realistic test data
    /// </summary>
    public static class TestDataFactory
    {
        private static readonly Faker<Customer> CustomerFaker = new Faker<Customer>()
            .RuleFor(c => c.Id, f => f.Random.Int(1, 1000))
            .RuleFor(c => c.Name, f => f.Company.CompanyName())
            .RuleFor(c => c.Contact, f => f.Name.FullName())
            .RuleFor(c => c.Email, f => f.Internet.Email())
            .RuleFor(c => c.Address, f => f.Address.FullAddress());

        private static readonly Faker<Order> OrderFaker = new Faker<Order>()
            .RuleFor(o => o.Id, f => f.Random.Int(1, 1000))
            .RuleFor(o => o.CustomerId, f => f.Random.Int(1, 100))
            .RuleFor(o => o.Status, f => f.PickRandom(new[] { "Pending", "Processing", "Completed", "Cancelled" }))
            .RuleFor(o => o.Date, f => f.Date.Recent(30))
            .RuleFor(o => o.TotalAmount, f => f.Random.Decimal(1000, 50000));

        private static readonly Faker<CustomerProduct> CustomerProductFaker = new Faker<CustomerProduct>()
            .RuleFor(cp => cp.Id, f => f.Random.Int(1, 1000))
            .RuleFor(cp => cp.CustomerId, f => f.Random.Int(1, 100))
            .RuleFor(cp => cp.ProductName, f => f.Commerce.ProductName())
            .RuleFor(cp => cp.Description, f => f.Commerce.ProductDescription())
            .RuleFor(cp => cp.Length, f => f.Random.Decimal(5, 50))
            .RuleFor(cp => cp.Width, f => f.Random.Decimal(5, 50))
            .RuleFor(cp => cp.Height, f => f.Random.Decimal(2, 30))
            .RuleFor(cp => cp.GSM, f => f.Random.Int(80, 500))
            .RuleFor(cp => cp.PaperType, f => f.PickRandom(new[] { "Duplex", "Kraft", "Art Card" }))
            .RuleFor(cp => cp.PrintingType, f => f.PickRandom(new[] { "1 Color", "2 Color", "Full Color" }))
            .RuleFor(cp => cp.PricePerBox, f => f.Random.Decimal(5, 100))
            .RuleFor(cp => cp.Category, f => f.PickRandom(new[] { "Standard", "Premium", "Special" }))
            .RuleFor(cp => cp.CreatedDate, f => f.Date.Recent(90));

        private static readonly Faker<Inventory> InventoryFaker = new Faker<Inventory>()
            .RuleFor(i => i.Id, f => f.Random.Int(1, 1000))
            .RuleFor(i => i.ItemName, f => f.Commerce.ProductName())
            .RuleFor(i => i.Category, f => f.PickRandom(new[] { "Raw Material", "Finished Goods", "Tools", "Consumables" }))
            .RuleFor(i => i.AvailableQuantity, f => f.Random.Decimal(0, 1000))
            .RuleFor(i => i.Unit, f => f.PickRandom(new[] { "Pieces", "Kg", "Meters", "Sheets" }))
            .RuleFor(i => i.UnitPrice, f => f.Random.Decimal(1, 1000))
            .RuleFor(i => i.ReorderLevel, f => f.Random.Decimal(10, 100));

        // Factory methods
        public static Customer CreateCustomer() => CustomerFaker.Generate();
        public static List<Customer> CreateCustomers(int count) => CustomerFaker.Generate(count);

        public static Order CreateOrder() => OrderFaker.Generate();
        public static List<Order> CreateOrders(int count) => OrderFaker.Generate(count);

        public static CustomerProduct CreateCustomerProduct() => CustomerProductFaker.Generate();
        public static List<CustomerProduct> CreateCustomerProducts(int count) => CustomerProductFaker.Generate(count);

        public static Inventory CreateInventory() => InventoryFaker.Generate();
        public static List<Inventory> CreateInventories(int count) => InventoryFaker.Generate(count);

        public static Customer CreateValidCustomer()
        {
            var customer = CreateCustomer();
            customer.Name = "Test Company Ltd.";
            customer.Email = "test@testcompany.com";
            customer.Contact = "John Doe";
            return customer;
        }

        public static Order CreateValidOrder()
        {
            var order = CreateOrder();
            order.Status = "Pending";
            order.TotalAmount = 25000;
            return order;
        }

        public static CustomerProduct CreateValidCustomerProduct()
        {
            var product = CreateCustomerProduct();
            product.Length = 12;
            product.Width = 8;
            product.Height = 6;
            return product;
        }
    }
}