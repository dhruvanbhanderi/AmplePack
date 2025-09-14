using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AmplePack.Data;
using AmplePack.Models;

namespace AmplePack.Pages_Reports
{
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;

        public IndexModel(AppDbContext context)
        {
            _context = context;
        }

        // Sales Reports
        public decimal TotalRevenue { get; set; }
        public decimal MonthlyRevenue { get; set; }
        public int TotalOrdersThisMonth { get; set; }
        public List<MonthlyRevenueData> MonthlyRevenueChart { get; set; } = new();

        // Customer Reports
        public int TotalCustomers { get; set; }
        public int NewCustomersThisMonth { get; set; }
        public List<Customer> TopCustomers { get; set; } = new();

        // Inventory Reports
        public int TotalInventoryItems { get; set; }
        public int LowStockCount { get; set; }
        public decimal TotalInventoryValue { get; set; }
        public List<Inventory> CriticalStockItems { get; set; } = new();

        // Order Status Reports
        public int PendingOrders { get; set; }
        public int ProcessingOrders { get; set; }
        public int CompletedOrders { get; set; }
        public int CancelledOrders { get; set; }

        public async Task OnGetAsync()
        {
            var currentMonth = DateTime.Now.Month;
            var currentYear = DateTime.Now.Year;

            // Sales Reports
            TotalRevenue = await _context.Orders
                .Where(o => o.Status == "Completed")
                .SumAsync(o => o.TotalAmount);

            MonthlyRevenue = await _context.Orders
                .Where(o => o.Status == "Completed" && o.Date.Month == currentMonth && o.Date.Year == currentYear)
                .SumAsync(o => o.TotalAmount);

            TotalOrdersThisMonth = await _context.Orders
                .CountAsync(o => o.Date.Month == currentMonth && o.Date.Year == currentYear);

            // Get monthly revenue for the last 12 months
            MonthlyRevenueChart = await GetMonthlyRevenueData();

            // Customer Reports
            TotalCustomers = await _context.Customers.CountAsync();
            
            NewCustomersThisMonth = await _context.Customers
                .CountAsync(c => c.Orders.Any(o => o.Date.Month == currentMonth && o.Date.Year == currentYear));

            TopCustomers = await _context.Customers
                .Include(c => c.Orders)
                .OrderByDescending(c => c.Orders.Where(o => o.Status == "Completed").Sum(o => o.TotalAmount))
                .Take(5)
                .ToListAsync();

            // Inventory Reports
            TotalInventoryItems = await _context.Inventories.CountAsync();
            LowStockCount = await _context.Inventories.CountAsync(i => i.Quantity <= i.ReorderLevel);
            
            CriticalStockItems = await _context.Inventories
                .Where(i => i.Quantity <= i.ReorderLevel)
                .OrderBy(i => i.Quantity)
                .Take(5)
                .ToListAsync();

            // Order Status Reports
            PendingOrders = await _context.Orders.CountAsync(o => o.Status == "Pending");
            ProcessingOrders = await _context.Orders.CountAsync(o => o.Status == "Processing");
            CompletedOrders = await _context.Orders.CountAsync(o => o.Status == "Completed");
            CancelledOrders = await _context.Orders.CountAsync(o => o.Status == "Cancelled");
        }

        private async Task<List<MonthlyRevenueData>> GetMonthlyRevenueData()
        {
            var result = new List<MonthlyRevenueData>();
            var startDate = DateTime.Now.AddMonths(-11).Date;

            for (int i = 0; i < 12; i++)
            {
                var monthStart = startDate.AddMonths(i);
                var monthEnd = monthStart.AddMonths(1).AddDays(-1);

                var revenue = await _context.Orders
                    .Where(o => o.Status == "Completed" && o.Date >= monthStart && o.Date <= monthEnd)
                    .SumAsync(o => o.TotalAmount);

                result.Add(new MonthlyRevenueData
                {
                    Month = monthStart.ToString("MMM yyyy"),
                    Revenue = revenue
                });
            }

            return result;
        }
    }

    public class MonthlyRevenueData
    {
        public string Month { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
    }
}