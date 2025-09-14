using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AmplePack.Data;
using AmplePack.Models;

namespace AmplePack.Pages_CustomerProducts
{
    public class CreateModel : PageModel
    {
        private readonly AmplePack.Data.AppDbContext _context;

        public CreateModel(AmplePack.Data.AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> OnGetAsync(int? customerId)
        {
            ViewData["CustomerId"] = new SelectList(await _context.Customers.OrderBy(c => c.Name).ToListAsync(), "Id", "Name");
            
            CustomerProduct = new CustomerProduct();
            if (customerId.HasValue)
            {
                CustomerProduct.CustomerId = customerId.Value;
            }
            
            return Page();
        }

        [BindProperty]
        public CustomerProduct CustomerProduct { get; set; } = default!;

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                ViewData["CustomerId"] = new SelectList(await _context.Customers.OrderBy(c => c.Name).ToListAsync(), "Id", "Name");
                return Page();
            }

            // Check for duplicate product name for the same customer
            var existingProduct = await _context.CustomerProducts
                .FirstOrDefaultAsync(cp => cp.CustomerId == CustomerProduct.CustomerId && 
                                         cp.ProductName == CustomerProduct.ProductName);

            if (existingProduct != null)
            {
                ModelState.AddModelError("CustomerProduct.ProductName", "A product with this name already exists for the selected customer.");
                ViewData["CustomerId"] = new SelectList(await _context.Customers.OrderBy(c => c.Name).ToListAsync(), "Id", "Name");
                return Page();
            }

            CustomerProduct.CreatedDate = DateTime.Now;
            CustomerProduct.IsActive = true;
            CustomerProduct.TotalOrdersCount = 0;

            _context.CustomerProducts.Add(CustomerProduct);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}