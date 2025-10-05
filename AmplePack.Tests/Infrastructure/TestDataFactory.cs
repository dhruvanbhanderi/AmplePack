using AmplePack.Models;

namespace AmplePack.Tests.Infrastructure
{
    /// <summary>
    /// Test data factory using Bogus for generating realistic test data
    /// </summary>
    public static class TestDataFactory
    {
        private static readonly Faker<Customer> CustomerFaker = new Faker<Customer>()
            .RuleFor(c => c.Name, f => f.Company.CompanyName())
            .RuleFor(c => c.Email, f => f.Internet.Email())
            .RuleFor(c => c.Contact, f => f.Phone.PhoneNumber())
            .RuleFor(c => c.Address, f => f.Address.FullAddress());

        private static readonly Faker<Order> OrderFaker = new Faker<Order>()
            .RuleFor(o => o.Date, f => f.Date.Recent(30))
            .RuleFor(o => o.TotalAmount, f => f.Random.Decimal(10, 1000))
            .RuleFor(o => o.Status, f => f.PickRandom("Pending", "Processing", "Completed", "Cancelled"));

        private static readonly Faker<CustomerProduct> CustomerProductFaker = new Faker<CustomerProduct>()
            .RuleFor(cp => cp.ProductName, f => f.Commerce.ProductName())
            .RuleFor(cp => cp.Description, f => f.Commerce.ProductDescription())
            .RuleFor(cp => cp.Length, f => f.Random.Decimal(5, 50))
            .RuleFor(cp => cp.Width, f => f.Random.Decimal(3, 40))
            .RuleFor(cp => cp.Height, f => f.Random.Decimal(2, 30))
            .RuleFor(cp => cp.GSM, f => f.Random.Int(120, 400))
            .RuleFor(cp => cp.PaperType, f => f.PickRandom("Kraft", "Test Liner", "White Top"))
            .RuleFor(cp => cp.PrintingType, f => f.PickRandom("Flexo", "Offset", "Digital"))
            .RuleFor(cp => cp.PricePerBox, f => f.Random.Decimal(5, 500))
            .RuleFor(cp => cp.IsActive, f => f.Random.Bool(0.8f));

        private static readonly Faker<BoxCalculatorInput> BoxCalculatorInputFaker = new Faker<BoxCalculatorInput>()
            .RuleFor(b => b.Length, f => f.Random.Decimal(1, 50))
            .RuleFor(b => b.Width, f => f.Random.Decimal(1, 50))
            .RuleFor(b => b.Height, f => f.Random.Decimal(1, 50))
            .RuleFor(b => b.BoardGSM, f => f.Random.Int(120, 1000))
            .RuleFor(b => b.BoardType, f => f.PickRandom("Single Wall", "Double Wall", "Triple Wall"))
            .RuleFor(b => b.CompressionRatio, f => f.Random.Decimal(0.8m, 1.2m))
            .RuleFor(b => b.Quantity, f => f.Random.Int(1, 10000))
            .RuleFor(b => b.SheetLength, f => f.Random.Decimal(10, 100))
            .RuleFor(b => b.SheetWidth, f => f.Random.Decimal(10, 100))
            .RuleFor(b => b.OverheadPercentage, f => f.Random.Decimal(5, 25))
            .RuleFor(b => b.ProfitMarginPercentage, f => f.Random.Decimal(10, 30))
            .RuleFor(b => b.DiscountPercentage, f => f.Random.Decimal(0, 20))
            .RuleFor(b => b.IncludeGST, f => f.Random.Bool());

        public static Customer CreateCustomer() => CustomerFaker.Generate();
        public static List<Customer> CreateCustomers(int count) => CustomerFaker.Generate(count);

        public static Order CreateOrder(int customerId) 
        {
            var order = OrderFaker.Generate();
            order.CustomerId = customerId;
            return order;
        }
        
        public static List<Order> CreateOrders(int customerId, int count)
        {
            return OrderFaker.Generate(count).Select(o => { o.CustomerId = customerId; return o; }).ToList();
        }

        public static CustomerProduct CreateCustomerProduct(int customerId)
        {
            var product = CustomerProductFaker.Generate();
            product.CustomerId = customerId;
            return product;
        }

        public static List<CustomerProduct> CreateCustomerProducts(int customerId, int count)
        {
            return CustomerProductFaker.Generate(count).Select(cp => { cp.CustomerId = customerId; return cp; }).ToList();
        }

        public static BoxCalculatorInput CreateBoxCalculatorInput() => BoxCalculatorInputFaker.Generate();

        public static BoxCalculatorInput CreateValidBoxCalculatorInput()
        {
            return new BoxCalculatorInput
            {
                Length = 10,
                Width = 8,
                Height = 6,
                BoardGSM = 150,
                BoardType = "Single Wall",
                CompressionRatio = 1.0m,
                Quantity = 1000,
                SheetLength = 40,
                SheetWidth = 30,
                OverheadPercentage = 15,
                ProfitMarginPercentage = 20,
                DiscountPercentage = 0,
                IncludeGST = true,
                BoardRatePerSqM = 50.0m,
                PrintingCostPerSqM = 15.0m,
                DieCuttingCostPerSqM = 8.0m,
                LaborCostPerBox = 2.0m,
                WastePercentage = 5.0m,
                ShippingCostPerOrder = 100.0m
            };
        }
    }
}