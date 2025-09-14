using AmplePack.Models;

namespace AmplePack.Data
{
    public static class DbSeeder
    {
        public static void SeedData(AppDbContext context)
        {
            // Ensure database is created
            context.Database.EnsureCreated();

            // Check if we already have data
            if (context.Customers.Any())
            {
                return; // Data already seeded
            }

            // Seed Customers
            var customers = new List<Customer>
            {
                new Customer
                {
                    Name = "ABC Corporation",
                    Contact = "+1-555-0123",
                    Email = "orders@abccorp.com",
                    Address = "123 Business St, New York, NY 10001"
                },
                new Customer
                {
                    Name = "XYZ Enterprises",
                    Contact = "+1-555-0456",
                    Email = "procurement@xyzent.com",
                    Address = "456 Commerce Ave, Los Angeles, CA 90210"
                },
                new Customer
                {
                    Name = "Global Logistics Ltd",
                    Contact = "+1-555-0789",
                    Email = "supplies@globallogistics.com",
                    Address = "789 Industrial Blvd, Chicago, IL 60601"
                },
                new Customer
                {
                    Name = "Tech Solutions Inc",
                    Contact = "+1-555-0321",
                    Email = "shipping@techsolutions.com",
                    Address = "321 Innovation Dr, San Francisco, CA 94105"
                }
            };

            context.Customers.AddRange(customers);
            context.SaveChanges();

            // Seed Inventory
            var inventoryItems = new List<Inventory>
            {
                new Inventory
                {
                    ProductName = "Corrugated Box - Small",
                    ItemName = "Small Box (12x8x6)",
                    AvailableQuantity = 500,
                    Quantity = 500,
                    UnitPrice = 2.50m,
                    Unit = "pieces",
                    Category = "Packaging",
                    ReorderLevel = 100
                },
                new Inventory
                {
                    ProductName = "Corrugated Box - Medium", 
                    ItemName = "Medium Box (16x12x8)",
                    AvailableQuantity = 300,
                    Quantity = 300,
                    UnitPrice = 3.75m,
                    Unit = "pieces",
                    Category = "Packaging",
                    ReorderLevel = 75
                },
                new Inventory
                {
                    ProductName = "Corrugated Box - Large",
                    ItemName = "Large Box (20x16x12)",
                    AvailableQuantity = 200,
                    Quantity = 200,
                    UnitPrice = 5.25m,
                    Unit = "pieces",
                    Category = "Packaging",
                    ReorderLevel = 50
                },
                new Inventory
                {
                    ProductName = "Corrugated Box - Extra Large",
                    ItemName = "Extra Large Box (24x20x16)",
                    AvailableQuantity = 15, // Low stock to test alerts
                    Quantity = 15,
                    UnitPrice = 8.50m,
                    Unit = "pieces",
                    Category = "Packaging",
                    ReorderLevel = 25
                },
                new Inventory
                {
                    ProductName = "Bubble Wrap",
                    ItemName = "Bubble Wrap Roll",
                    AvailableQuantity = 8, // Low stock to test alerts
                    Quantity = 8,
                    UnitPrice = 15.99m,
                    Unit = "rolls",
                    Category = "Supplies",
                    ReorderLevel = 10
                },
                new Inventory
                {
                    ProductName = "Packing Tape",
                    ItemName = "Packing Tape",
                    AvailableQuantity = 150,
                    Quantity = 150,
                    UnitPrice = 3.99m,
                    Unit = "rolls",
                    Category = "Supplies",
                    ReorderLevel = 20
                }
            };

            context.Inventories.AddRange(inventoryItems);
            context.SaveChanges();

            // Seed Customer Products
            var customerProducts = new List<CustomerProduct>
            {
                new CustomerProduct
                {
                    CustomerId = customers[0].Id,
                    ProductName = "Custom Shipping Box",
                    Description = "Custom white corrugated shipping box with company logo",
                    Length = 15.0m,
                    Width = 12.0m,
                    Height = 8.0m,
                    GSM = 250,
                    PaperType = "Corrugated",
                    PrintingType = "2 Color",
                    PricePerBox = 4.25m,
                    DefaultQuantity = 100,
                    Category = "Premium",
                    IsActive = true,
                    CreatedDate = DateTime.Now.AddMonths(-2)
                },
                new CustomerProduct
                {
                    CustomerId = customers[1].Id,
                    ProductName = "Product Packaging Box",
                    Description = "Eco-friendly kraft paper box for retail products",
                    Length = 10.0m,
                    Width = 8.0m,
                    Height = 6.0m,
                    GSM = 300,
                    PaperType = "Kraft Paper",
                    PrintingType = "Full Color",
                    PricePerBox = 3.75m,
                    DefaultQuantity = 200,
                    Category = "Standard",
                    IsActive = true,
                    CreatedDate = DateTime.Now.AddMonths(-1)
                }
            };

            context.CustomerProducts.AddRange(customerProducts);
            context.SaveChanges();

            // Seed Orders
            var orders = new List<Order>
            {
                new Order
                {
                    CustomerId = customers[0].Id,
                    Date = DateTime.Now.AddDays(-30),
                    Status = "Completed",
                    TotalAmount = 425.00m
                },
                new Order
                {
                    CustomerId = customers[1].Id,
                    Date = DateTime.Now.AddDays(-15),
                    Status = "Completed",
                    TotalAmount = 750.00m
                },
                new Order
                {
                    CustomerId = customers[2].Id,
                    Date = DateTime.Now.AddDays(-5),
                    Status = "Processing",
                    TotalAmount = 320.00m
                },
                new Order
                {
                    CustomerId = customers[3].Id,
                    Date = DateTime.Now.AddDays(-2),
                    Status = "Pending",
                    TotalAmount = 180.00m
                }
            };

            context.Orders.AddRange(orders);
            context.SaveChanges();

            // Seed Order Details
            var orderDetails = new List<OrderDetail>
            {
                // Order 1 Details
                new OrderDetail
                {
                    OrderId = orders[0].Id,
                    CustomerProductId = customerProducts[0].Id,
                    BoxType = "Custom Shipping Box",
                    Size = "15×12×8",
                    Quantity = 100,
                    PricePerBox = 4.25m
                },
                // Order 2 Details
                new OrderDetail
                {
                    OrderId = orders[1].Id,
                    CustomerProductId = customerProducts[1].Id,
                    BoxType = "Product Packaging Box", 
                    Size = "10×8×6",
                    Quantity = 200,
                    PricePerBox = 3.75m
                },
                // Order 3 Details
                new OrderDetail
                {
                    OrderId = orders[2].Id,
                    BoxType = "Standard Box",
                    Size = "16×12×8",
                    Quantity = 80,
                    PricePerBox = 4.00m
                },
                // Order 4 Details
                new OrderDetail
                {
                    OrderId = orders[3].Id,
                    BoxType = "Small Box",
                    Size = "12×8×6",
                    Quantity = 60,
                    PricePerBox = 3.00m
                }
            };

            context.OrderDetails.AddRange(orderDetails);
            context.SaveChanges();
        }
    }
}