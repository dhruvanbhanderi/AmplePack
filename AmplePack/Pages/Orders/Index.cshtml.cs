using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AmplePack.Data;
using AmplePack.Models;

namespace AmplePack.Pages_Orders
{
    public class IndexModel : PageModel
    {
        private readonly AmplePack.Data.AppDbContext _context;

        public IndexModel(AmplePack.Data.AppDbContext context)
        {
            _context = context;
        }

        public IList<Order> Orders { get; set; } = default!;
        
        // Statistics for dashboard cards
        public int TotalOrders { get; set; }
        public int PendingOrders { get; set; }
        public int ProcessingOrders { get; set; }
        public int CompletedOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal MonthlyRevenue { get; set; }

        public async Task OnGetAsync()
        {
            // Load orders with customer and order details
            Orders = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.CustomerProduct)
                .OrderByDescending(o => o.Date)
                .ToListAsync();

            // Calculate statistics
            TotalOrders = Orders.Count;
            PendingOrders = Orders.Count(o => o.Status == "Pending");
            ProcessingOrders = Orders.Count(o => o.Status == "Processing");
            CompletedOrders = Orders.Count(o => o.Status == "Completed");
            TotalRevenue = Orders.Where(o => o.Status == "Completed").Sum(o => o.TotalAmount);
            
            var currentMonth = DateTime.Now.Month;
            var currentYear = DateTime.Now.Year;
            MonthlyRevenue = Orders
                .Where(o => o.Status == "Completed" && o.Date.Month == currentMonth && o.Date.Year == currentYear)
                .Sum(o => o.TotalAmount);
        }
    }
}
