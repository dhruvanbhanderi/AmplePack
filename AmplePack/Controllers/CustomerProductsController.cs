using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AmplePack.Data;
using AmplePack.Models;

namespace AmplePack.Controllers
{
    public class CustomerProductsController : Controller
    {
        private readonly AppDbContext _context;

        public CustomerProductsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: CustomerProducts
        public async Task<IActionResult> Index(int? customerId, string searchTerm)
        {
            // Get all customers with their product counts
            var customers = await _context.Customers
                .Include(c => c.CustomerProducts)
                .OrderBy(c => c.Name)
                .ToListAsync();

            ViewBag.Customers = customers;
            ViewBag.SelectedCustomerId = customerId;
            ViewBag.SearchTerm = searchTerm;

            // If a specific customer is selected, show their products
            if (customerId.HasValue)
            {
                var customerProducts = await _context.CustomerProducts
                    .Include(cp => cp.Customer)
                    .Where(cp => cp.CustomerId == customerId.Value)
                    .ToListAsync();

                // Filter by search term if provided
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    customerProducts = customerProducts.Where(cp => 
                        cp.ProductName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                        cp.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                ViewBag.CustomerProducts = customerProducts;
                ViewBag.SelectedCustomer = customers.FirstOrDefault(c => c.Id == customerId.Value);
                return View("CustomerProducts");
            }

            // Default view showing customers
            return View(customers);
        }

        // GET: CustomerProducts/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var customerProduct = await _context.CustomerProducts
                .Include(c => c.Customer)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (customerProduct == null)
            {
                return NotFound();
            }

            return View(customerProduct);
        }

        // GET: CustomerProducts/Create
        public async Task<IActionResult> Create(int? customerId)
        {
            ViewData["CustomerId"] = new SelectList(_context.Customers, "Id", "Name", customerId);
            
            var model = new CustomerProduct();
            if (customerId.HasValue)
            {
                model.CustomerId = customerId.Value;
                var customer = await _context.Customers.FindAsync(customerId.Value);
                ViewBag.CustomerName = customer?.Name;
            }
            
            return View(model);
        }

        // POST: CustomerProducts/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,CustomerId,ProductName,Description,Length,Width,Height,GSM,PaperType,PrintingType,PricePerBox,DefaultQuantity,Category")] CustomerProduct customerProduct)
        {
            if (ModelState.IsValid)
            {
                customerProduct.CreatedDate = DateTime.Now;
                customerProduct.IsActive = true;
                _context.Add(customerProduct);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index), new { customerId = customerProduct.CustomerId });
            }
            ViewData["CustomerId"] = new SelectList(_context.Customers, "Id", "Name", customerProduct.CustomerId);
            return View(customerProduct);
        }

        // GET: CustomerProducts/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var customerProduct = await _context.CustomerProducts.FindAsync(id);
            if (customerProduct == null)
            {
                return NotFound();
            }
            ViewData["CustomerId"] = new SelectList(_context.Customers, "Id", "Name", customerProduct.CustomerId);
            return View(customerProduct);
        }

        // POST: CustomerProducts/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,CustomerId,ProductName,Description,Length,Width,Height,GSM,PaperType,PrintingType,PricePerBox,DefaultQuantity,Category,IsActive")] CustomerProduct customerProduct)
        {
            if (id != customerProduct.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Preserve original created date
                    var originalProduct = await _context.CustomerProducts.AsNoTracking().FirstOrDefaultAsync(cp => cp.Id == id);
                    if (originalProduct != null)
                    {
                        customerProduct.CreatedDate = originalProduct.CreatedDate;
                    }
                    
                    _context.Update(customerProduct);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CustomerProductExists(customerProduct.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index), new { customerId = customerProduct.CustomerId });
            }
            ViewData["CustomerId"] = new SelectList(_context.Customers, "Id", "Name", customerProduct.CustomerId);
            return View(customerProduct);
        }

        // GET: CustomerProducts/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var customerProduct = await _context.CustomerProducts
                .Include(c => c.Customer)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (customerProduct == null)
            {
                return NotFound();
            }

            return View(customerProduct);
        }

        // POST: CustomerProducts/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var customerProduct = await _context.CustomerProducts.FindAsync(id);
            if (customerProduct != null)
            {
                var customerId = customerProduct.CustomerId;
                _context.CustomerProducts.Remove(customerProduct);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index), new { customerId = customerId });
            }
            return RedirectToAction(nameof(Index));
        }

        private bool CustomerProductExists(int id)
        {
            return _context.CustomerProducts.Any(e => e.Id == id);
        }
    }
}