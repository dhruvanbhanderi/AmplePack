using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AmplePack.Data;
using AmplePack.Models;

namespace AmplePack.Pages
{
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;

        public IndexModel(AppDbContext context)
        {
            _context = context;
        }

        public int TotalOrders { get; set; }
        public int TotalCustomers { get; set; }
        public int LowStockItems { get; set; }
        public decimal TotalRevenue { get; set; }
        public int CompletedOrders { get; set; }
        public int PendingOrders { get; set; }
        public List<Order> RecentOrders { get; set; } = new();
        public List<Inventory> LowStockInventory { get; set; } = new();

        public async Task OnGetAsync()
        {
            // Get dashboard statistics
            TotalOrders = await _context.Orders.CountAsync();
            TotalCustomers = await _context.Customers.CountAsync();
            LowStockItems = await _context.Inventories.CountAsync(i => i.Quantity <= 50);
            TotalRevenue = await _context.Orders
                .Where(o => o.Status == "Completed")
                .SumAsync(o => o.TotalAmount);

            CompletedOrders = await _context.Orders.CountAsync(o => o.Status == "Completed");
            PendingOrders = await _context.Orders.CountAsync(o => o.Status == "Pending");

            // Get recent orders with customer information
            RecentOrders = await _context.Orders
                .Include(o => o.Customer)
                .OrderByDescending(o => o.Date)
                .Take(5)
                .ToListAsync();

            // Get low stock inventory items
            LowStockInventory = await _context.Inventories
                .Where(i => i.Quantity <= 50)
                .OrderBy(i => i.Quantity)
                .Take(5)
                .ToListAsync();
        }
    }
}
