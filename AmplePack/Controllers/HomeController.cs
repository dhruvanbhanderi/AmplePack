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
            // Get dashboard statistics
            ViewBag.TotalOrders = await _context.Orders.CountAsync();
            ViewBag.TotalCustomers = await _context.Customers.CountAsync();
            ViewBag.TotalRevenue = await _context.Orders
                .Where(o => o.Status == "Completed")
                .SumAsync(o => o.TotalAmount);
            ViewBag.LowStockCount = await _context.Inventories
                .CountAsync(i => i.AvailableQuantity <= i.ReorderLevel);

            // Order status counts
            ViewBag.PendingOrders = await _context.Orders.CountAsync(o => o.Status == "Pending");
            ViewBag.ProcessingOrders = await _context.Orders.CountAsync(o => o.Status == "Processing");
            ViewBag.CompletedOrders = await _context.Orders.CountAsync(o => o.Status == "Completed");
            ViewBag.CancelledOrders = await _context.Orders.CountAsync(o => o.Status == "Cancelled");

            // Recent orders (last 5)
            ViewBag.RecentOrders = await _context.Orders
                .Include(o => o.Customer)
                .OrderByDescending(o => o.Date)
                .Take(5)
                .ToListAsync();

            // Low stock items - Fixed: Order in memory to avoid SQLite decimal ordering issue
            var lowStockItems = await _context.Inventories
                .Where(i => i.AvailableQuantity <= i.ReorderLevel)
                .ToListAsync();
            
            ViewBag.LowStockItems = lowStockItems
                .OrderBy(i => i.AvailableQuantity)
                .ToList();

            return View();
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
