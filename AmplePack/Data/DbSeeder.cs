using AmplePack.Models;
using Microsoft.EntityFrameworkCore;

namespace AmplePack.Data
{
    public static class DbSeeder
    {
        public static void SeedData(AppDbContext context)
        {
            // Ensure database is created
            context.Database.EnsureCreated();

            // Check if data already exists
            if (context.Customers.Any())
                return; // Database has been seeded

            // Seed Customers
            var customers = new List<Customer>
            {
                new Customer
                {
                    Name = "John Smith",
                    Email = "john.smith@email.com",
                    Contact = "+1-555-0101",
                    Address = "123 Main St, New York, NY 10001"
                },
                new Customer
                {
                    Name = "Sarah Johnson",
                    Email = "sarah.johnson@email.com",
                    Contact = "+1-555-0102",
                    Address = "456 Oak Ave, Los Angeles, CA 90210"
                },
                new Customer
                {
                    Name = "Michael Brown",
                    Email = "michael.brown@email.com",
                    Contact = "+1-555-0103",
                    Address = "789 Pine St, Chicago, IL 60601"
                },
                new Customer
                {
                    Name = "Emily Davis",
                    Email = "emily.davis@email.com",
                    Contact = "+1-555-0104",
                    Address = "321 Elm St, Houston, TX 77001"
                },
                new Customer
                {
                    Name = "David Wilson",
                    Email = "david.wilson@email.com",
                    Contact = "+1-555-0105",
                    Address = "654 Cedar Ave, Phoenix, AZ 85001"
                }
            };

            context.Customers.AddRange(customers);
            context.SaveChanges();

            // Seed Inventory
            var inventoryItems = new List<Inventory>
            {
                new Inventory
                {
                    ItemName = "Small Box (12x8x6)",
                    Quantity = 150,
                    Unit = "pieces",
                    ReorderLevel = 20
                },
                new Inventory
                {
                    ItemName = "Medium Box (16x12x8)",
                    Quantity = 120,
                    Unit = "pieces",
                    ReorderLevel = 15
                },
                new Inventory
                {
                    ItemName = "Large Box (20x16x12)",
                    Quantity = 85,
                    Unit = "pieces",
                    ReorderLevel = 10
                },
                new Inventory
                {
                    ItemName = "Extra Large Box (24x20x16)",
                    Quantity = 45,
                    Unit = "pieces",
                    ReorderLevel = 8
                },
                new Inventory
                {
                    ItemName = "Bubble Wrap Roll",
                    Quantity = 25,
                    Unit = "rolls",
                    ReorderLevel = 5
                },
                new Inventory
                {
                    ItemName = "Packing Tape",
                    Quantity = 75,
                    Unit = "rolls",
                    ReorderLevel = 12
                }
            };

            context.Inventories.AddRange(inventoryItems);
            context.SaveChanges();

            // Seed Orders
            var orders = new List<Order>
            {
                new Order
                {
                    CustomerId = customers[0].Id,
                    Date = DateTime.Now.AddDays(-10),
                    Status = "Completed",
                    TotalAmount = 45.50m
                },
                new Order
                {
                    CustomerId = customers[1].Id,
                    Date = DateTime.Now.AddDays(-8),
                    Status = "Pending",
                    TotalAmount = 32.75m
                },
                new Order
                {
                    CustomerId = customers[2].Id,
                    Date = DateTime.Now.AddDays(-5),
                    Status = "Processing",
                    TotalAmount = 67.25m
                },
                new Order
                {
                    CustomerId = customers[3].Id,
                    Date = DateTime.Now.AddDays(-3),
                    Status = "Completed",
                    TotalAmount = 28.00m
                },
                new Order
                {
                    CustomerId = customers[4].Id,
                    Date = DateTime.Now.AddDays(-1),
                    Status = "Pending",
                    TotalAmount = 55.75m
                }
            };

            context.Orders.AddRange(orders);
            context.SaveChanges();

            // Seed Box Pricing
            var boxPricing = new List<BoxPricing>
            {
                new BoxPricing
                {
                    L = 12.0m,
                    W = 8.0m,
                    H = 6.0m,
                    GSM = 300,
                    PrintingType = "Standard",
                    Quantity = 100,
                    Cost = 250.00m
                },
                new BoxPricing
                {
                    L = 16.0m,
                    W = 12.0m,
                    H = 8.0m,
                    GSM = 350,
                    PrintingType = "Premium",
                    Quantity = 50,
                    Cost = 200.00m
                },
                new BoxPricing
                {
                    L = 20.0m,
                    W = 16.0m,
                    H = 12.0m,
                    GSM = 400,
                    PrintingType = "Deluxe",
                    Quantity = 25,
                    Cost = 162.50m
                },
                new BoxPricing
                {
                    L = 24.0m,
                    W = 20.0m,
                    H = 16.0m,
                    GSM = 450,
                    PrintingType = "Premium",
                    Quantity = 10,
                    Cost = 90.00m
                }
            };

            context.BoxPricings.AddRange(boxPricing);
            context.SaveChanges();

            // Seed Customer Products (Product Patterns)
            var customerProducts = new List<CustomerProduct>
            {
                // John Smith's Products
                new CustomerProduct
                {
                    CustomerId = customers[0].Id,
                    ProductName = "Standard Shipping Box",
                    Description = "Regular shipping box for general products",
                    Length = 12.0m,
                    Width = 8.0m,
                    Height = 6.0m,
                    GSM = 180,
                    PaperType = "Corrugated",
                    PrintingType = "1 Color",
                    PricePerBox = 2.50m,
                    DefaultQuantity = 100,
                    Category = "Standard",
                    IsActive = true,
                    CreatedDate = DateTime.Now.AddDays(-30),
                    TotalOrdersCount = 3
                },
                new CustomerProduct
                {
                    CustomerId = customers[0].Id,
                    ProductName = "Premium Display Box",
                    Description = "High-quality display box with premium finish",
                    Length = 15.0m,
                    Width = 12.0m,
                    Height = 8.0m,
                    GSM = 300,
                    PaperType = "Art Paper",
                    PrintingType = "4 Color",
                    PricePerBox = 5.75m,
                    DefaultQuantity = 50,
                    Category = "Premium",
                    IsActive = true,
                    CreatedDate = DateTime.Now.AddDays(-25),
                    TotalOrdersCount = 2
                },

                // Sarah Johnson's Products
                new CustomerProduct
                {
                    CustomerId = customers[1].Id,
                    ProductName = "Eco-Friendly Box A",
                    Description = "Environmentally friendly packaging solution",
                    Length = 10.0m,
                    Width = 8.0m,
                    Height = 5.0m,
                    GSM = 150,
                    PaperType = "Kraft",
                    PrintingType = "2 Color",
                    PricePerBox = 1.85m,
                    DefaultQuantity = 200,
                    Category = "Economy",
                    IsActive = true,
                    CreatedDate = DateTime.Now.AddDays(-20),
                    TotalOrdersCount = 5
                },
                new CustomerProduct
                {
                    CustomerId = customers[1].Id,
                    ProductName = "Gift Box Deluxe",
                    Description = "Luxury gift box with special finish",
                    Length = 20.0m,
                    Width = 15.0m,
                    Height = 10.0m,
                    GSM = 250,
                    PaperType = "Duplex",
                    PrintingType = "Spot Color",
                    PricePerBox = 8.25m,
                    DefaultQuantity = 25,
                    Category = "Premium",
                    IsActive = true,
                    CreatedDate = DateTime.Now.AddDays(-15),
                    TotalOrdersCount = 1
                },

                // Michael Brown's Products
                new CustomerProduct
                {
                    CustomerId = customers[2].Id,
                    ProductName = "Industrial Storage Box",
                    Description = "Heavy-duty box for industrial use",
                    Length = 25.0m,
                    Width = 20.0m,
                    Height = 15.0m,
                    GSM = 200,
                    PaperType = "Corrugated",
                    PrintingType = "No Printing",
                    PricePerBox = 3.50m,
                    DefaultQuantity = 75,
                    Category = "Standard",
                    IsActive = true,
                    CreatedDate = DateTime.Now.AddDays(-12),
                    TotalOrdersCount = 4
                },

                // Emily Davis's Products
                new CustomerProduct
                {
                    CustomerId = customers[3].Id,
                    ProductName = "Jewelry Box Small",
                    Description = "Compact box for jewelry items",
                    Length = 8.0m,
                    Width = 6.0m,
                    Height = 3.0m,
                    GSM = 300,
                    PaperType = "Ivory",
                    PrintingType = "Digital",
                    PricePerBox = 4.25m,
                    DefaultQuantity = 100,
                    Category = "Premium",
                    IsActive = true,
                    CreatedDate = DateTime.Now.AddDays(-8),
                    TotalOrdersCount = 2
                },

                // David Wilson's Products
                new CustomerProduct
                {
                    CustomerId = customers[4].Id,
                    ProductName = "Food Package Box",
                    Description = "Food-safe packaging box",
                    Length = 14.0m,
                    Width = 10.0m,
                    Height = 7.0m,
                    GSM = 180,
                    PaperType = "Kraft",
                    PrintingType = "2 Color",
                    PricePerBox = 2.95m,
                    DefaultQuantity = 150,
                    Category = "Standard",
                    IsActive = true,
                    CreatedDate = DateTime.Now.AddDays(-5),
                    TotalOrdersCount = 3
                }
            };

            context.CustomerProducts.AddRange(customerProducts);
            context.SaveChanges();
        }
    }
}