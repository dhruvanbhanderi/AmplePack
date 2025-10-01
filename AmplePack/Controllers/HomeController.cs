using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AmplePack.Models;
using AmplePack.Data;
using AmplePack.Services;
using AmplePack.ViewModels;

namespace AmplePack.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<HomeController> _logger;
        private readonly OrderManagementService _orderManagementService;

        public HomeController(AppDbContext context, ILogger<HomeController> logger, OrderManagementService orderManagementService)
        {
            _context = context;
            _logger = logger;
            _orderManagementService = orderManagementService;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                // Get comprehensive dashboard statistics
                var filter = new OrderFilterViewModel(); // No filters for full stats
                var orderData = await _orderManagementService.GetFilteredOrdersAsync(filter);
                
                // Get dashboard statistics
                ViewBag.TotalOrders = orderData.Stats.TotalOrders;
                ViewBag.TotalCustomers = await _context.Customers.CountAsync();
                ViewBag.TotalRevenue = orderData.Stats.YearlyRevenue;
                ViewBag.MonthlyRevenue = orderData.Stats.MonthlyRevenue;
                ViewBag.LowStockCount = await _context.Inventories
                    .CountAsync(i => i.AvailableQuantity <= i.ReorderLevel);

                // Order status counts
                ViewBag.PendingOrders = orderData.Stats.PendingOrders;
                ViewBag.ProcessingOrders = orderData.Stats.ProcessingOrders;
                ViewBag.CompletedOrders = orderData.Stats.CompletedOrders;
                ViewBag.CancelledOrders = orderData.Stats.CancelledOrders;
                ViewBag.OverdueOrders = orderData.Stats.OverdueOrders;

                // Additional KPIs
                ViewBag.AverageOrderValue = orderData.Stats.AverageOrderValue;
                ViewBag.OrderTrends = orderData.Stats.OrderTrends;
                ViewBag.TopCustomers = orderData.Stats.TopCustomers;

                // Recent orders (last 10)
                ViewBag.RecentOrders = await _context.Orders
                    .Include(o => o.Customer)
                    .Include(o => o.OrderDetails)
                    .OrderByDescending(o => o.Date)
                    .Take(10)
                    .ToListAsync();

                // Low stock items - Fixed: Order in memory to avoid SQLite decimal ordering issue
                var lowStockItems = await _context.Inventories
                    .Where(i => i.AvailableQuantity <= i.ReorderLevel)
                    .ToListAsync();
                
                ViewBag.LowStockItems = lowStockItems
                    .OrderBy(i => i.AvailableQuantity)
                    .Take(10)
                    .ToList();

                // Top products by order frequency
                ViewBag.TopProducts = await _context.CustomerProducts
                    .Where(cp => cp.IsActive)
                    .OrderByDescending(cp => cp.TotalOrdersCount)
                    .Take(5)
                    .ToListAsync();

                // Monthly order and revenue trends for charts
                var last12Months = DateTime.Now.AddMonths(-12);
                var monthlyData = await _context.Orders
                    .Where(o => o.Date >= last12Months)
                    .GroupBy(o => new { o.Date.Year, o.Date.Month })
                    .Select(g => new {
                        Year = g.Key.Year,
                        Month = g.Key.Month,
                        OrderCount = g.Count(),
                        Revenue = g.Where(o => o.Status == "Completed").Sum(o => o.TotalAmount)
                    })
                    .OrderBy(x => x.Year).ThenBy(x => x.Month)
                    .ToListAsync();

                ViewBag.MonthlyData = monthlyData;

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

        // API endpoint for dashboard charts
        [HttpGet]
        public async Task<IActionResult> GetDashboardChartData()
        {
            try
            {
                // Last 30 days order trends
                var last30Days = DateTime.Today.AddDays(-30);
                var dailyTrends = await _context.Orders
                    .Where(o => o.Date >= last30Days)
                    .GroupBy(o => o.Date.Date)
                    .Select(g => new
                    {
                        date = g.Key.ToString("yyyy-MM-dd"),
                        orders = g.Count(),
                        revenue = g.Where(o => o.Status == "Completed").Sum(o => o.TotalAmount)
                    })
                    .OrderBy(x => x.date)
                    .ToListAsync();

                // Order status distribution
                var statusDistribution = await _context.Orders
                    .GroupBy(o => o.Status)
                    .Select(g => new
                    {
                        status = g.Key,
                        count = g.Count()
                    })
                    .ToListAsync();

                // Monthly revenue for the last 12 months
                var last12Months = DateTime.Today.AddMonths(-12);
                var monthlyRevenue = await _context.Orders
                    .Where(o => o.Date >= last12Months && o.Status == "Completed")
                    .GroupBy(o => new { o.Date.Year, o.Date.Month })
                    .Select(g => new
                    {
                        period = $"{g.Key.Year}-{g.Key.Month:D2}",
                        revenue = g.Sum(o => o.TotalAmount),
                        orders = g.Count()
                    })
                    .OrderBy(x => x.period)
                    .ToListAsync();

                return Json(new
                {
                    success = true,
                    dailyTrends,
                    statusDistribution,
                    monthlyRevenue
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
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
