using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AmplePack.Data;
using AmplePack.Models;

namespace AmplePack.Pages
{
    public class TestDbModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly ILogger<TestDbModel> _logger;

        public TestDbModel(AppDbContext context, ILogger<TestDbModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public string ConnectionStatus { get; set; } = "";
        public List<Customer> Customers { get; set; } = new();
        public List<CustomerProduct> CustomerProducts { get; set; } = new();
        public List<Order> Orders { get; set; } = new();
        public int CustomerCount { get; set; }
        public int ProductPatternsCount { get; set; }
        public int OrdersCount { get; set; }

        public async Task OnGetAsync()
        {
            try
            {
                // Test database connection
                await _context.Database.CanConnectAsync();
                ConnectionStatus = "? Database connection successful";

                // Get counts
                CustomerCount = await _context.Customers.CountAsync();
                ProductPatternsCount = await _context.CustomerProducts.CountAsync();
                OrdersCount = await _context.Orders.CountAsync();

                // Get all customers
                Customers = await _context.Customers.ToListAsync();

                // Get customer products with customer info
                CustomerProducts = await _context.CustomerProducts
                    .Include(cp => cp.Customer)
                    .OrderBy(cp => cp.Customer!.Name)
                    .ThenBy(cp => cp.ProductName)
                    .Take(5)
                    .ToListAsync();

                // Get recent orders
                Orders = await _context.Orders
                    .Include(o => o.Customer)
                    .Include(o => o.OrderDetails)
                    .OrderByDescending(o => o.Date)
                    .Take(5)
                    .ToListAsync();

                _logger.LogInformation($"Database test successful. Found {CustomerCount} customers, {ProductPatternsCount} product patterns, and {OrdersCount} orders.");
            }
            catch (Exception ex)
            {
                ConnectionStatus = $"? Database connection failed: {ex.Message}";
                _logger.LogError(ex, "Database connection test failed");
            }
        }

        public async Task<IActionResult> OnPostCreateTestCustomerAsync()
        {
            try
            {
                var testCustomer = new Customer
                {
                    Name = $"Test Customer {DateTime.Now:yyyyMMdd-HHmmss}",
                    Email = $"test{DateTime.Now:yyyyMMddHHmmss}@example.com",
                    Contact = "+1-555-0199",
                    Address = "123 Test Street, Test City, TC 12345"
                };

                _context.Customers.Add(testCustomer);
                var result = await _context.SaveChangesAsync();

                _logger.LogInformation($"Test customer created successfully. SaveChanges returned: {result}");
                
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create test customer");
                return RedirectToPage();
            }
        }

        public async Task<IActionResult> OnPostCreateTestProductAsync()
        {
            try
            {
                // Get the first customer, or create one if none exists
                var customer = await _context.Customers.FirstOrDefaultAsync();
                if (customer == null)
                {
                    customer = new Customer
                    {
                        Name = "Test Customer for Product",
                        Email = "testproduct@example.com",
                        Contact = "+1-555-0100",
                        Address = "Product Test Address"
                    };
                    _context.Customers.Add(customer);
                    await _context.SaveChangesAsync();
                }

                var testProduct = new CustomerProduct
                {
                    CustomerId = customer.Id,
                    ProductName = $"Test Product {DateTime.Now:HHmmss}",
                    Description = "Test product pattern created automatically",
                    Length = 12.5m,
                    Width = 10.0m,
                    Height = 8.0m,
                    GSM = 180,
                    PaperType = "Corrugated",
                    PrintingType = "2 Color",
                    PricePerBox = 3.25m,
                    DefaultQuantity = 100,
                    Category = "Standard",
                    IsActive = true,
                    CreatedDate = DateTime.Now
                };

                _context.CustomerProducts.Add(testProduct);
                var result = await _context.SaveChangesAsync();

                _logger.LogInformation($"Test product pattern created successfully with ID: {testProduct.Id}");
                
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create test product pattern");
                return RedirectToPage();
            }
        }

        public async Task<IActionResult> OnPostCreateTestOrderAsync()
        {
            try
            {
                // Get the first customer, or create one if none exists
                var customer = await _context.Customers.FirstOrDefaultAsync();
                if (customer == null)
                {
                    customer = new Customer
                    {
                        Name = "Test Customer for Order",
                        Email = "testorder@example.com",
                        Contact = "+1-555-0100",
                        Address = "Order Test Address"
                    };
                    _context.Customers.Add(customer);
                    await _context.SaveChangesAsync();
                }

                var testOrder = new Order
                {
                    CustomerId = customer.Id,
                    Date = DateTime.Today,
                    Status = "Pending",
                    TotalAmount = 45.75m
                };

                _context.Orders.Add(testOrder);
                await _context.SaveChangesAsync();

                // Add order details
                var orderDetails = new List<OrderDetail>
                {
                    new OrderDetail
                    {
                        OrderId = testOrder.Id,
                        BoxType = "Medium",
                        Size = "16.0×12.0×8.0",
                        Quantity = 5,
                        PricePerBox = 4.25m
                    },
                    new OrderDetail
                    {
                        OrderId = testOrder.Id,
                        BoxType = "Small",
                        Size = "12.0×8.0×6.0",
                        Quantity = 10,
                        PricePerBox = 2.50m
                    }
                };

                _context.OrderDetails.AddRange(orderDetails);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Test order created successfully with ID: {testOrder.Id}");
                
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create test order");
                return RedirectToPage();
            }
        }

        public async Task<IActionResult> OnPostCreateTestCompleteOrderAsync()
        {
            try
            {
                // Get the first customer, or create one if none exists
                var customer = await _context.Customers.FirstOrDefaultAsync();
                if (customer == null)
                {
                    customer = new Customer
                    {
                        Name = "Test Customer for Complete Order",
                        Email = "testcompleteorder@example.com",
                        Contact = "+1-555-0100",
                        Address = "Complete Order Test Address"
                    };
                    _context.Customers.Add(customer);
                    await _context.SaveChangesAsync();
                }

                // Create a complete order with realistic data
                var testOrder = new Order
                {
                    CustomerId = customer.Id,
                    Date = DateTime.Today,
                    Status = "Pending",
                    TotalAmount = 0 // Will be calculated from details
                };

                _context.Orders.Add(testOrder);
                await _context.SaveChangesAsync();

                // Add order details based on realistic patterns
                var orderDetails = new List<OrderDetail>
                {
                    new OrderDetail
                    {
                        OrderId = testOrder.Id,
                        BoxType = "Small",
                        Size = "12.0×8.0×6.0",
                        Quantity = 50,
                        PricePerBox = 2.50m
                    },
                    new OrderDetail
                    {
                        OrderId = testOrder.Id,
                        BoxType = "Medium",
                        Size = "16.0×12.0×8.0",
                        Quantity = 25,
                        PricePerBox = 4.00m
                    },
                    new OrderDetail
                    {
                        OrderId = testOrder.Id,
                        BoxType = "Large",
                        Size = "20.0×16.0×12.0",
                        Quantity = 10,
                        PricePerBox = 6.50m
                    }
                };

                _context.OrderDetails.AddRange(orderDetails);

                // Calculate and update total amount
                testOrder.TotalAmount = orderDetails.Sum(od => od.Quantity * od.PricePerBox);
                
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Complete test order created successfully with ID: {testOrder.Id}, Total: ${testOrder.TotalAmount}");
                
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create complete test order");
                return RedirectToPage();
            }
        }

        public async Task<IActionResult> OnPostTestOrderDetailsAsync()
        {
            try
            {
                _logger.LogInformation("Starting order details test...");

                // Test if we can create and retrieve orders with BoxType
                var customer = await _context.Customers.FirstOrDefaultAsync();
                if (customer == null)
                {
                    customer = new Customer
                    {
                        Name = "Test Customer for Details",
                        Email = "testdetails@example.com",
                        Contact = "+1-555-0101",
                        Address = "Details Test Address"
                    };
                    _context.Customers.Add(customer);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"Created test customer with ID: {customer.Id}");
                }

                // Create order with comprehensive details
                var testOrder = new Order
                {
                    CustomerId = customer.Id,
                    Date = DateTime.Today,
                    Status = "Pending",
                    TotalAmount = 0
                };

                _context.Orders.Add(testOrder);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Created test order with ID: {testOrder.Id}");

                // Create detailed order items with different box types
                var orderDetails = new List<OrderDetail>
                {
                    new OrderDetail
                    {
                        OrderId = testOrder.Id,
                        BoxType = "Small Box",
                        Size = "10.0×8.0×5.0",
                        Quantity = 20,
                        PricePerBox = 1.75m
                    },
                    new OrderDetail
                    {
                        OrderId = testOrder.Id,
                        BoxType = "Medium Box",
                        Size = "15.0×12.0×8.0",
                        Quantity = 15,
                        PricePerBox = 3.25m
                    },
                    new OrderDetail
                    {
                        OrderId = testOrder.Id,
                        BoxType = "Large Custom",
                        Size = "25.0×20.0×15.0",
                        Quantity = 8,
                        PricePerBox = 7.50m
                    },
                    new OrderDetail
                    {
                        OrderId = testOrder.Id,
                        BoxType = "Extra Large Premium",
                        Size = "30.0×25.0×20.0",
                        Quantity = 5,
                        PricePerBox = 12.00m
                    }
                };

                _context.OrderDetails.AddRange(orderDetails);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Added {orderDetails.Count} order details");

                // Calculate total
                testOrder.TotalAmount = orderDetails.Sum(od => od.Quantity * od.PricePerBox);
                await _context.SaveChangesAsync();

                // Test retrieval with includes
                var retrievedOrder = await _context.Orders
                    .Include(o => o.Customer)
                    .Include(o => o.OrderDetails)
                    .FirstOrDefaultAsync(o => o.Id == testOrder.Id);

                if (retrievedOrder?.OrderDetails?.Any() == true)
                {
                    _logger.LogInformation($"Successfully retrieved order with {retrievedOrder.OrderDetails.Count} details");
                    foreach (var detail in retrievedOrder.OrderDetails)
                    {
                        _logger.LogInformation($"Detail: {detail.BoxType} - {detail.Size} - Qty: {detail.Quantity} - Price: ${detail.PricePerBox}");
                    }
                }

                _logger.LogInformation($"Order details test completed successfully. Order ID: {testOrder.Id}, Total: ${testOrder.TotalAmount}");
                
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create detailed test order");
                return RedirectToPage();
            }
        }

        public async Task<IActionResult> OnPostCheckDatabaseSchemaAsync()
        {
            try
            {
                _logger.LogInformation("Checking database schema...");

                // Check if OrderDetails table exists and has BoxType column
                var hasOrderDetails = await _context.OrderDetails.AnyAsync();
                _logger.LogInformation($"OrderDetails table accessible: {hasOrderDetails}");

                // Try to query OrderDetails with BoxType
                var orderDetailsWithBoxType = await _context.OrderDetails
                    .Select(od => new { od.Id, od.BoxType, od.Size, od.Quantity })
                    .Take(5)
                    .ToListAsync();

                _logger.LogInformation($"Successfully queried {orderDetailsWithBoxType.Count} order details with BoxType");
                
                foreach (var detail in orderDetailsWithBoxType)
                {
                    _logger.LogInformation($"OrderDetail ID: {detail.Id}, BoxType: '{detail.BoxType}', Size: '{detail.Size}', Quantity: {detail.Quantity}");
                }

                // Check Orders with OrderDetails relationship
                var ordersWithDetails = await _context.Orders
                    .Include(o => o.OrderDetails)
                    .Where(o => o.OrderDetails.Any())
                    .Take(3)
                    .ToListAsync();

                _logger.LogInformation($"Found {ordersWithDetails.Count} orders with details");
                
                foreach (var order in ordersWithDetails)
                {
                    _logger.LogInformation($"Order {order.Id} has {order.OrderDetails.Count} details");
                    foreach (var detail in order.OrderDetails.Take(2))
                    {
                        _logger.LogInformation($"  - {detail.BoxType}: {detail.Size} x{detail.Quantity} @ ${detail.PricePerBox}");
                    }
                }

                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database schema check failed");
                return RedirectToPage();
            }
        }

        public async Task<IActionResult> OnPostTestWorkflowOrderAsync()
        {
            try
            {
                _logger.LogInformation("Testing workflow-based order creation...");

                // Get or create a customer
                var customer = await _context.Customers.FirstOrDefaultAsync();
                if (customer == null)
                {
                    customer = new Customer
                    {
                        Name = "Workflow Test Customer",
                        Email = "workflow@example.com",
                        Contact = "+1-555-WORK",
                        Address = "Workflow Test Address"
                    };
                    _context.Customers.Add(customer);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"Created test customer: {customer.Name}");
                }

                // Get or create customer products
                var customerProducts = await _context.CustomerProducts
                    .Where(cp => cp.CustomerId == customer.Id)
                    .ToListAsync();

                if (!customerProducts.Any())
                {
                    var products = new List<CustomerProduct>
                    {
                        new CustomerProduct
                        {
                            CustomerId = customer.Id,
                            ProductName = "Workflow Box A",
                            Description = "Standard workflow box",
                            Length = 15.0m,
                            Width = 10.0m,
                            Height = 8.0m,
                            GSM = 180,
                            PaperType = "Corrugated",
                            PrintingType = "2 Color",
                            PricePerBox = 3.50m,
                            DefaultQuantity = 50,
                            Category = "Standard",
                            IsActive = true,
                            CreatedDate = DateTime.Now
                        },
                        new CustomerProduct
                        {
                            CustomerId = customer.Id,
                            ProductName = "Workflow Box B",
                            Description = "Premium workflow box",
                            Length = 20.0m,
                            Width = 15.0m,
                            Height = 12.0m,
                            GSM = 250,
                            PaperType = "Art Paper",
                            PrintingType = "4 Color",
                            PricePerBox = 6.75m,
                            DefaultQuantity = 25,
                            Category = "Premium",
                            IsActive = true,
                            CreatedDate = DateTime.Now
                        }
                    };

                    _context.CustomerProducts.AddRange(products);
                    await _context.SaveChangesAsync();
                    customerProducts = products;
                    _logger.LogInformation($"Created {products.Count} test products for customer");
                }

                // Create workflow order
                var workflowOrder = new Order
                {
                    CustomerId = customer.Id,
                    Date = DateTime.Today,
                    Status = "Pending",
                    TotalAmount = 0
                };

                _context.Orders.Add(workflowOrder);
                await _context.SaveChangesAsync();

                // Add order details linked to customer products
                var orderDetails = new List<OrderDetail>();
                foreach (var product in customerProducts.Take(2))
                {
                    var orderDetail = new OrderDetail
                    {
                        OrderId = workflowOrder.Id,
                        CustomerProductId = product.Id, // Link to customer product
                        BoxType = product.ProductName,
                        Size = $"{product.Length}×{product.Width}×{product.Height}",
                        Quantity = product.DefaultQuantity,
                        PricePerBox = product.PricePerBox,
                        DeliveryDate = DateTime.Today.AddDays(7),
                        Notes = "Workflow test order"
                    };
                    orderDetails.Add(orderDetail);

                    // Update product statistics
                    product.TotalOrdersCount++;
                    product.LastOrderDate = DateTime.Now;
                }

                _context.OrderDetails.AddRange(orderDetails);

                // Calculate total
                workflowOrder.TotalAmount = orderDetails.Sum(od => od.Quantity * od.PricePerBox);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Workflow order created successfully: Order ID {workflowOrder.Id}, Total: ${workflowOrder.TotalAmount}");

                // Verify the relationships
                var verifyOrder = await _context.Orders
                    .Include(o => o.Customer)
                    .Include(o => o.OrderDetails)
                        .ThenInclude(od => od.CustomerProduct)
                    .FirstOrDefaultAsync(o => o.Id == workflowOrder.Id);

                if (verifyOrder != null)
                {
                    _logger.LogInformation($"Verification: Order for {verifyOrder.Customer?.Name} with {verifyOrder.OrderDetails.Count} items");
                    foreach (var detail in verifyOrder.OrderDetails)
                    {
                        _logger.LogInformation($"  - Product: {detail.CustomerProduct?.ProductName ?? detail.BoxType} | Qty: {detail.Quantity} | Price: ${detail.PricePerBox}");
                    }
                }

                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create workflow test order");
                return RedirectToPage();
            }
        }

        public async Task<IActionResult> OnPostTestCompleteWorkflowAsync()
        {
            try
            {
                _logger.LogInformation("Testing complete workflow order creation...");

                // Create customer with realistic products
                var customer = new Customer
                {
                    Name = "Mobile Test Customer",
                    Email = "mobile@test.com",
                    Contact = "+1-555-MOBILE",
                    Address = "Mobile Test Address"
                };
                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();

                // Create realistic product patterns
                var products = new List<CustomerProduct>
                {
                    new CustomerProduct
                    {
                        CustomerId = customer.Id,
                        ProductName = "Small Standard Box",
                        Description = "Most common small box",
                        Length = 12.0m,
                        Width = 8.0m,
                        Height = 6.0m,
                        GSM = 180,
                        PaperType = "Corrugated",
                        PrintingType = "2 Color",
                        PricePerBox = 2.25m,
                        DefaultQuantity = 100,
                        Category = "Standard",
                        IsActive = true,
                        CreatedDate = DateTime.Now
                    },
                    new CustomerProduct
                    {
                        CustomerId = customer.Id,
                        ProductName = "Medium Premium Box",
                        Description = "Premium medium box with 4-color printing",
                        Length = 16.0m,
                        Width = 12.0m,
                        Height = 10.0m,
                        GSM = 250,
                        PaperType = "Art Paper",
                        PrintingType = "4 Color",
                        PricePerBox = 5.75m,
                        DefaultQuantity = 50,
                        Category = "Premium",
                        IsActive = true,
                        CreatedDate = DateTime.Now
                    },
                    new CustomerProduct
                    {
                        CustomerId = customer.Id,
                        ProductName = "Large Economy Box",
                        Description = "Cost-effective large box",
                        Length = 24.0m,
                        Width = 18.0m,
                        Height = 15.0m,
                        GSM = 180,
                        PaperType = "Kraft",
                        PrintingType = "No Printing",
                        PricePerBox = 4.50m,
                        DefaultQuantity = 25,
                        Category = "Economy",
                        IsActive = true,
                        CreatedDate = DateTime.Now
                    }
                };

                _context.CustomerProducts.AddRange(products);
                await _context.SaveChangesAsync();

                // Create workflow order using the products
                var workflowOrder = new Order
                {
                    CustomerId = customer.Id,
                    Date = DateTime.Today,
                    Status = "Pending",
                    TotalAmount = 0
                };

                _context.Orders.Add(workflowOrder);
                await _context.SaveChangesAsync();

                // Add order details with different quantities
                var orderDetails = new List<OrderDetail>
                {
                    new OrderDetail
                    {
                        OrderId = workflowOrder.Id,
                        CustomerProductId = products[0].Id,
                        BoxType = products[0].ProductName,
                        Size = $"{products[0].Length}×{products[0].Width}×{products[0].Height}",
                        Quantity = 150, // Modified quantity
                        PricePerBox = products[0].PricePerBox,
                        DeliveryDate = DateTime.Today.AddDays(5),
                        Notes = "Workflow test - mobile optimized"
                    },
                    new OrderDetail
                    {
                        OrderId = workflowOrder.Id,
                        CustomerProductId = products[1].Id,
                        BoxType = products[1].ProductName,
                        Size = $"{products[1].Length}×{products[1].Width}×{products[1].Height}",
                        Quantity = 75, // Modified quantity
                        PricePerBox = products[1].PricePerBox,
                        DeliveryDate = DateTime.Today.AddDays(7),
                        Notes = "Premium order with custom quantities"
                    }
                };

                _context.OrderDetails.AddRange(orderDetails);

                // Update product statistics
                foreach (var product in products.Take(2))
                {
                    product.TotalOrdersCount++;
                    product.LastOrderDate = DateTime.Now;
                }

                // Calculate total
                workflowOrder.TotalAmount = orderDetails.Sum(od => od.Quantity * od.PricePerBox);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Complete workflow test successful: Customer '{customer.Name}', {products.Count} products, Order ${workflowOrder.TotalAmount}");

                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create complete workflow test");
                return RedirectToPage();
            }
        }
    }
}