using System.Diagnostics;
using AmplePack.Models;
using AmplePack.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AmplePack.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AppDbContext _context;

        public HomeController(ILogger<HomeController> logger, AppDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Dashboard statistics
            ViewBag.TotalOrders = await _context.Orders.CountAsync();
            ViewBag.TotalCustomers = await _context.Customers.CountAsync();
            ViewBag.TotalInventoryItems = await _context.Inventories.CountAsync();
            ViewBag.MonthlyRevenue = await _context.Orders
                .Where(o => o.Status == "Completed" && o.Date.Month == DateTime.Now.Month)
                .SumAsync(o => o.TotalAmount);

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
