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
                },
                new Customer
                {
                    Name = "Green Package Co",
                    Contact = "+1-555-0654",
                    Email = "orders@greenpackage.com",
                    Address = "555 Eco Ave, Portland, OR 97201"
                }
            };

            context.Customers.AddRange(customers);
            context.SaveChanges();

            // Seed Inventory
            var inventoryItems = new List<Inventory>
            {
                new Inventory
                {
                    ItemName = "Small Corrugated Box (12x8x6)",
                    AvailableQuantity = 500,
                    UnitPrice = 2.50m,
                    Unit = "pieces",
                    Category = "Packaging",
                    ReorderLevel = 100
                },
                new Inventory
                {
                    ItemName = "Medium Corrugated Box (16x12x8)",
                    AvailableQuantity = 300,
                    UnitPrice = 3.75m,
                    Unit = "pieces",
                    Category = "Packaging",
                    ReorderLevel = 75
                },
                new Inventory
                {
                    ItemName = "Large Corrugated Box (20x16x12)",
                    AvailableQuantity = 200,
                    UnitPrice = 5.25m,
                    Unit = "pieces",
                    Category = "Packaging",
                    ReorderLevel = 50
                },
                new Inventory
                {
                    ItemName = "Extra Large Corrugated Box (24x20x16)",
                    AvailableQuantity = 15, // Low stock to test alerts
                    UnitPrice = 8.50m,
                    Unit = "pieces",
                    Category = "Packaging",
                    ReorderLevel = 25
                },
                new Inventory
                {
                    ItemName = "Bubble Wrap Roll (50m)",
                    AvailableQuantity = 8, // Low stock to test alerts
                    UnitPrice = 15.99m,
                    Unit = "rolls",
                    Category = "Supplies",
                    ReorderLevel = 10
                },
                new Inventory
                {
                    ItemName = "Heavy Duty Packing Tape (48mm x 50m)",
                    AvailableQuantity = 150,
                    UnitPrice = 3.99m,
                    Unit = "rolls",
                    Category = "Supplies",
                    ReorderLevel = 20
                },
                new Inventory
                {
                    ItemName = "Premium Gift Box (20x15x10)",
                    AvailableQuantity = 75,
                    UnitPrice = 6.99m,
                    Unit = "pieces",
                    Category = "Premium",
                    ReorderLevel = 25
                },
                new Inventory
                {
                    ItemName = "Adhesive Shipping Labels",
                    AvailableQuantity = 5, // Low stock
                    UnitPrice = 12.50m,
                    Unit = "sheets",
                    Category = "Supplies",
                    ReorderLevel = 15
                }
            };

            context.Inventories.AddRange(inventoryItems);
            context.SaveChanges();

            // Seed Customer Products with UTC dates
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
                    CreatedDate = DateTime.UtcNow.AddMonths(-2)
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
                    CreatedDate = DateTime.UtcNow.AddMonths(-1)
                },
                new CustomerProduct
                {
                    CustomerId = customers[2].Id,
                    ProductName = "Logistics Box - Heavy Duty",
                    Description = "Reinforced box for heavy items and long distance shipping",
                    Length = 18.0m,
                    Width = 14.0m,
                    Height = 10.0m,
                    GSM = 350,
                    PaperType = "Corrugated",
                    PrintingType = "1 Color",
                    PricePerBox = 5.50m,
                    DefaultQuantity = 50,
                    Category = "Premium",
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow.AddDays(-20)
                },
                new CustomerProduct
                {
                    CustomerId = customers[3].Id,
                    ProductName = "Tech Product Box",
                    Description = "Anti-static box designed for electronic products",
                    Length = 12.0m,
                    Width = 10.0m,
                    Height = 8.0m,
                    GSM = 280,
                    PaperType = "Duplex Board",
                    PrintingType = "4 Color",
                    PricePerBox = 4.99m,
                    DefaultQuantity = 75,
                    Category = "Premium",
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow.AddDays(-10)
                }
            };

            context.CustomerProducts.AddRange(customerProducts);
            context.SaveChanges();

            // Seed Orders with varying dates to ensure good chart data
            var now = DateTime.UtcNow;
            var orders = new List<Order>
            {
                // Previous months orders
                new Order
                {
                    CustomerId = customers[0].Id,
                    Date = now.AddMonths(-3).AddDays(-5),
                    Status = "Completed",
                    TotalAmount = 1200.00m
                },
                new Order
                {
                    CustomerId = customers[1].Id,
                    Date = now.AddMonths(-2).AddDays(-10),
                    Status = "Completed",
                    TotalAmount = 850.00m
                },
                new Order
                {
                    CustomerId = customers[2].Id,
                    Date = now.AddMonths(-2).AddDays(-5),
                    Status = "Completed",
                    TotalAmount = 650.00m
                },
                new Order
                {
                    CustomerId = customers[3].Id,
                    Date = now.AddMonths(-1).AddDays(-15),
                    Status = "Completed",
                    TotalAmount = 425.00m
                },
                new Order
                {
                    CustomerId = customers[1].Id,
                    Date = now.AddMonths(-1).AddDays(-10),
                    Status = "Completed",
                    TotalAmount = 750.00m
                },
                new Order
                {
                    CustomerId = customers[4].Id,
                    Date = now.AddMonths(-1).AddDays(-5),
                    Status = "Completed",
                    TotalAmount = 320.00m
                },
                // Current month orders
                new Order
                {
                    CustomerId = customers[0].Id,
                    Date = now.AddDays(-25),
                    Status = "Completed",
                    TotalAmount = 950.00m
                },
                new Order
                {
                    CustomerId = customers[2].Id,
                    Date = now.AddDays(-20),
                    Status = "Completed",
                    TotalAmount = 275.00m
                },
                new Order
                {
                    CustomerId = customers[3].Id,
                    Date = now.AddDays(-15),
                    Status = "Completed",
                    TotalAmount = 850.00m
                },
                new Order
                {
                    CustomerId = customers[1].Id,
                    Date = now.AddDays(-10),
                    Status = "Completed",
                    TotalAmount = 374.25m
                },
                new Order
                {
                    CustomerId = customers[4].Id,
                    Date = now.AddDays(-8),
                    Status = "Completed",
                    TotalAmount = 580.00m
                },
                // Recent orders in various statuses
                new Order
                {
                    CustomerId = customers[2].Id,
                    Date = now.AddDays(-5),
                    Status = "Processing",
                    TotalAmount = 320.00m
                },
                new Order
                {
                    CustomerId = customers[3].Id,
                    Date = now.AddDays(-3),
                    Status = "Processing",
                    TotalAmount = 180.00m
                },
                new Order
                {
                    CustomerId = customers[4].Id,
                    Date = now.AddDays(-2),
                    Status = "Pending",
                    TotalAmount = 225.00m
                },
                new Order
                {
                    CustomerId = customers[0].Id,
                    Date = now.AddDays(-1),
                    Status = "Pending",
                    TotalAmount = 445.00m
                }
            };

            context.Orders.AddRange(orders);
            context.SaveChanges();

            // Seed Order Details with corresponding data
            var orderDetails = new List<OrderDetail>
            {
                // Order 1 Details
                new OrderDetail
                {
                    OrderId = orders[0].Id,
                    CustomerProductId = customerProducts[0].Id,
                    BoxType = "Custom Shipping Box",
                    Size = "15×12×8",
                    Quantity = 280,
                    PricePerBox = 4.25m,
                    Notes = "Include company logo"
                },
                // Order 2 Details
                new OrderDetail
                {
                    OrderId = orders[1].Id,
                    CustomerProductId = customerProducts[1].Id,
                    BoxType = "Product Packaging Box", 
                    Size = "10×8×6",
                    Quantity = 225,
                    PricePerBox = 3.75m,
                    Notes = "Eco-friendly materials required"
                },
                // Order 3 Details
                new OrderDetail
                {
                    OrderId = orders[2].Id,
                    CustomerProductId = customerProducts[2].Id,
                    BoxType = "Logistics Box - Heavy Duty",
                    Size = "18×14×10",
                    Quantity = 118,
                    PricePerBox = 5.50m,
                    Notes = "Extra reinforcement needed"
                },
                // Order 4 Details
                new OrderDetail
                {
                    OrderId = orders[3].Id,
                    CustomerProductId = customerProducts[0].Id,
                    BoxType = "Custom Shipping Box",
                    Size = "15×12×8",
                    Quantity = 100,
                    PricePerBox = 4.25m,
                    DeliveryDate = now.AddDays(7)
                },
                // Order 5 Details
                new OrderDetail
                {
                    OrderId = orders[4].Id,
                    CustomerProductId = customerProducts[1].Id,
                    BoxType = "Product Packaging Box",
                    Size = "10×8×6",
                    Quantity = 200,
                    PricePerBox = 3.75m,
                    Notes = "Priority order"
                },
                // Order 6 Details
                new OrderDetail
                {
                    OrderId = orders[5].Id,
                    BoxType = "Standard Box",
                    Size = "14×10×8",
                    Quantity = 80,
                    PricePerBox = 4.00m,
                    Notes = "Standard packaging"
                },
                // Current month orders
                new OrderDetail
                {
                    OrderId = orders[6].Id,
                    CustomerProductId = customerProducts[0].Id,
                    BoxType = "Custom Shipping Box",
                    Size = "15×12×8",
                    Quantity = 225,
                    PricePerBox = 4.25m,
                    Notes = "Current month order"
                },
                new OrderDetail
                {
                    OrderId = orders[7].Id,
                    CustomerProductId = customerProducts[2].Id,
                    BoxType = "Logistics Box - Heavy Duty",
                    Size = "18×14×10",
                    Quantity = 50,
                    PricePerBox = 5.50m,
                    Notes = "Heavy duty required"
                },
                new OrderDetail
                {
                    OrderId = orders[8].Id,
                    CustomerProductId = customerProducts[0].Id,
                    BoxType = "Custom Shipping Box",
                    Size = "15×12×8",
                    Quantity = 200,
                    PricePerBox = 4.25m,
                    DeliveryDate = now.AddDays(5)
                },
                new OrderDetail
                {
                    OrderId = orders[9].Id,
                    CustomerProductId = customerProducts[3].Id,
                    BoxType = "Tech Product Box",
                    Size = "12×10×8",
                    Quantity = 75,
                    PricePerBox = 4.99m,
                    Notes = "Anti-static coating required"
                },
                new OrderDetail
                {
                    OrderId = orders[10].Id,
                    BoxType = "Premium Box",
                    Size = "16×12×10",
                    Quantity = 145,
                    PricePerBox = 4.00m,
                    Notes = "Premium finish"
                },
                // Processing and Pending orders
                new OrderDetail
                {
                    OrderId = orders[11].Id,
                    BoxType = "Standard Box",
                    Size = "16×12×8",
                    Quantity = 80,
                    PricePerBox = 4.00m,
                    Notes = "Rush order"
                },
                new OrderDetail
                {
                    OrderId = orders[12].Id,
                    BoxType = "Small Box",
                    Size = "12×8×6",
                    Quantity = 60,
                    PricePerBox = 3.00m
                },
                new OrderDetail
                {
                    OrderId = orders[13].Id,
                    BoxType = "Medium Box",
                    Size = "14×10×8",
                    Quantity = 50,
                    PricePerBox = 4.50m,
                    DeliveryDate = now.AddDays(14)
                },
                new OrderDetail
                {
                    OrderId = orders[14].Id,
                    CustomerProductId = customerProducts[0].Id,
                    BoxType = "Custom Shipping Box",
                    Size = "15×12×8",
                    Quantity = 105,
                    PricePerBox = 4.25m,
                    Notes = "Pending approval"
                }
            };

            context.OrderDetails.AddRange(orderDetails);
            context.SaveChanges();
        }
    }
}