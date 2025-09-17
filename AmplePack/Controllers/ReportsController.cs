using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AmplePack.Data;
using AmplePack.Models;

namespace AmplePack.Controllers
{
    [Authorize]
    public class ReportsController : Controller
    {
        private readonly AppDbContext _context;

        public ReportsController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var currentMonth = DateTime.UtcNow.Month;
            var currentYear = DateTime.UtcNow.Year;

            var viewModel = new ReportsViewModel();

            // Sales Reports
            viewModel.TotalRevenue = await _context.Orders
                .Where(o => o.Status == "Completed")
                .SumAsync(o => o.TotalAmount);

            viewModel.MonthlyRevenue = await _context.Orders
                .Where(o => o.Status == "Completed" && o.Date.Month == currentMonth && o.Date.Year == currentYear)
                .SumAsync(o => o.TotalAmount);

            viewModel.TotalOrdersThisMonth = await _context.Orders
                .CountAsync(o => o.Date.Month == currentMonth && o.Date.Year == currentYear);

            // Get monthly revenue for the last 12 months
            viewModel.MonthlyRevenueChart = await GetMonthlyRevenueData();

            // Customer Reports
            viewModel.TotalCustomers = await _context.Customers.CountAsync();
            
            viewModel.NewCustomersThisMonth = await _context.Customers
                .CountAsync(c => c.Orders.Any(o => o.Date.Month == currentMonth && o.Date.Year == currentYear));

            // Fixed: Get top customers by loading data first, then ordering in memory
            var customersWithOrders = await _context.Customers
                .Include(c => c.Orders)
                .ToListAsync();
            
            viewModel.TopCustomers = customersWithOrders
                .OrderByDescending(c => c.Orders.Where(o => o.Status == "Completed").Sum(o => o.TotalAmount))
                .Take(5)
                .ToList();

            // Inventory Reports
            viewModel.TotalInventoryItems = await _context.Inventories.CountAsync();
            viewModel.LowStockCount = await _context.Inventories.CountAsync(i => i.AvailableQuantity <= i.ReorderLevel);
            
            // Calculate total inventory value
            viewModel.TotalInventoryValue = await _context.Inventories
                .SumAsync(i => i.AvailableQuantity * i.UnitPrice);
            
            // Fixed: Get critical stock items by loading data first, then ordering in memory
            var criticalStockItems = await _context.Inventories
                .Where(i => i.AvailableQuantity <= i.ReorderLevel)
                .ToListAsync();
            
            viewModel.CriticalStockItems = criticalStockItems
                .OrderBy(i => i.AvailableQuantity)
                .Take(5)
                .ToList();

            // Order Status Reports
            viewModel.PendingOrders = await _context.Orders.CountAsync(o => o.Status == "Pending");
            viewModel.ProcessingOrders = await _context.Orders.CountAsync(o => o.Status == "Processing");
            viewModel.CompletedOrders = await _context.Orders.CountAsync(o => o.Status == "Completed");
            viewModel.CancelledOrders = await _context.Orders.CountAsync(o => o.Status == "Cancelled");

            return View(viewModel);
        }

        private async Task<List<MonthlyRevenueData>> GetMonthlyRevenueData()
        {
            var result = new List<MonthlyRevenueData>();
            var now = DateTime.UtcNow;
            
            // Get the last 12 months
            for (int i = 11; i >= 0; i--)
            {
                var monthStart = new DateTime(now.Year, now.Month, 1).AddMonths(-i);
                var monthEnd = monthStart.AddMonths(1);

                var revenue = await _context.Orders
                    .Where(o => o.Status == "Completed" && o.Date >= monthStart && o.Date < monthEnd)
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

    public class ReportsViewModel
    {
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
    }

    public class MonthlyRevenueData
    {
        public string Month { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
    }
}