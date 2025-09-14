using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AmplePack.Data;
using AmplePack.Models;

namespace AmplePack.Pages_CustomerProducts
{
    public class IndexModel : PageModel
    {
        private readonly AmplePack.Data.AppDbContext _context;

        public IndexModel(AmplePack.Data.AppDbContext context)
        {
            _context = context;
        }

        public IList<CustomerProduct> CustomerProducts { get;set; } = default!;
        public Dictionary<int, string> CustomerNames { get; set; } = new();
        
        [BindProperty(SupportsGet = true)]
        public int? SelectedCustomerId { get; set; }
        
        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        public async Task OnGetAsync()
        {
            var query = _context.CustomerProducts
                .Include(cp => cp.Customer)
                .AsQueryable();

            // Filter by customer if selected
            if (SelectedCustomerId.HasValue)
            {
                query = query.Where(cp => cp.CustomerId == SelectedCustomerId.Value);
            }

            // Search functionality
            if (!string.IsNullOrEmpty(SearchTerm))
            {
                query = query.Where(cp => 
                    cp.ProductName.Contains(SearchTerm) ||
                    cp.Description.Contains(SearchTerm) ||
                    cp.Customer!.Name.Contains(SearchTerm));
            }

            CustomerProducts = await query
                .OrderBy(cp => cp.Customer!.Name)
                .ThenBy(cp => cp.ProductName)
                .ToListAsync();

            // Get customer names for dropdown
            CustomerNames = await _context.Customers
                .ToDictionaryAsync(c => c.Id, c => c.Name);
        }
    }
}