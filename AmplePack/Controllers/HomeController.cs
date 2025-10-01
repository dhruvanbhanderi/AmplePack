using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AmplePack.Models;
using AmplePack.Data;

namespace AmplePack.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<HomeController> _logger;

        public HomeController(AppDbContext context, ILogger<HomeController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                // Get dashboard statistics with real data
                ViewBag.TotalOrders = await _context.Orders.CountAsync();
                ViewBag.TotalCustomers = await _context.Customers.CountAsync();
                ViewBag.TotalRevenue = await _context.Orders
                    .Where(o => o.Status == "Completed")
                    .SumAsync(o => o.TotalAmount);
                ViewBag.LowStockCount = await _context.Inventories
                    .CountAsync(i => i.AvailableQuantity <= i.ReorderLevel);

                // Order status counts for the charts
                ViewBag.PendingOrders = await _context.Orders.CountAsync(o => o.Status == "Pending");
                ViewBag.ProcessingOrders = await _context.Orders.CountAsync(o => o.Status == "Processing");
                ViewBag.CompletedOrders = await _context.Orders.CountAsync(o => o.Status == "Completed");
                ViewBag.CancelledOrders = await _context.Orders.CountAsync(o => o.Status == "Cancelled");

                // Recent orders (last 5) with customer information
                ViewBag.RecentOrders = await _context.Orders
                    .Include(o => o.Customer)
                    .OrderByDescending(o => o.Date)
                    .Take(5)
                    .ToListAsync();

                // Low stock items with proper ordering
                var allInventory = await _context.Inventories.ToListAsync();
                var lowStockItems = allInventory
                    .Where(i => i.AvailableQuantity <= i.ReorderLevel)
                    .OrderBy(i => i.AvailableQuantity / Math.Max(i.ReorderLevel, 1)) // Order by percentage of reorder level
                    .Take(5)
                    .ToList();
                
                ViewBag.LowStockItems = lowStockItems;

                // Current month statistics for comparison
                var currentMonth = DateTime.Now.Month;
                var currentYear = DateTime.Now.Year;
                ViewBag.ThisMonthOrders = await _context.Orders
                    .CountAsync(o => o.Date.Month == currentMonth && o.Date.Year == currentYear);
                ViewBag.ThisMonthRevenue = await _context.Orders
                    .Where(o => o.Status == "Completed" && o.Date.Month == currentMonth && o.Date.Year == currentYear)
                    .SumAsync(o => o.TotalAmount);
                ViewBag.ThisMonthNewCustomers = await _context.Customers
                    .CountAsync(c => c.Orders.Any(o => o.Date.Month == currentMonth && o.Date.Year == currentYear));

                // Check if this is a first-time user (for welcome message)
                var hasData = ViewBag.TotalOrders > 0 || ViewBag.TotalCustomers > 0;
                ViewBag.ShowWelcome = !hasData;

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard data");
                
                // Provide fallback data
                ViewBag.TotalOrders = 0;
                ViewBag.TotalCustomers = 0;
                ViewBag.TotalRevenue = 0;
                ViewBag.LowStockCount = 0;
                ViewBag.PendingOrders = 0;
                ViewBag.ProcessingOrders = 0;
                ViewBag.CompletedOrders = 0;
                ViewBag.CancelledOrders = 0;
                ViewBag.RecentOrders = new List<Order>();
                ViewBag.LowStockItems = new List<Inventory>();
                ViewBag.ShowWelcome = true;
                
                TempData["ErrorMessage"] = "Unable to load some dashboard data. Please refresh the page.";
                return View();
            }
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
