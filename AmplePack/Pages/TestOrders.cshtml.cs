using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AmplePack.Data;
using AmplePack.Models;

namespace AmplePack.Pages
{
    public class TestOrdersModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly ILogger<TestOrdersModel> _logger;

        public TestOrdersModel(AppDbContext context, ILogger<TestOrdersModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public string Status { get; set; } = "";
        public List<Order> Orders { get; set; } = new();
        public List<Customer> Customers { get; set; } = new();

        public async Task OnGetAsync()
        {
            try
            {
                // Get all orders with customers
                Orders = await _context.Orders.Include(o => o.Customer).ToListAsync();
                Customers = await _context.Customers.ToListAsync();
                
                Status = $"? Found {Orders.Count} orders and {Customers.Count} customers";
            }
            catch (Exception ex)
            {
                Status = $"? Error: {ex.Message}";
                _logger.LogError(ex, "Error loading test data");
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
                    TotalAmount = 25.50m
                };

                _context.Orders.Add(testOrder);
                var result = await _context.SaveChangesAsync();

                // Add order details
                var orderDetail = new OrderDetail
                {
                    OrderId = testOrder.Id,
                    BoxType = "Small",
                    Size = "12x8x6",
                    Quantity = 10,
                    PricePerBox = 2.55m
                };

                _context.OrderDetails.Add(orderDetail);
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
    }
}