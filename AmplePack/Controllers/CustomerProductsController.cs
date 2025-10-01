using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AmplePack.Data;
using AmplePack.Models;

namespace AmplePack.Controllers
{
    [Authorize]
    public class CustomerProductsController : Controller
    {
        private readonly AppDbContext _context;

        public CustomerProductsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: CustomerProducts
        public async Task<IActionResult> Index(int? customerId, string? category, string? searchTerm)
        {
            var query = _context.CustomerProducts
                .Include(cp => cp.Customer)
                .AsQueryable();

            // Apply filters
            if (customerId.HasValue)
            {
                query = query.Where(cp => cp.CustomerId == customerId.Value);
            }

            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(cp => cp.Category == category);
            }

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(cp => 
                    cp.ProductName.Contains(searchTerm) ||
                    cp.Description.Contains(searchTerm) ||
                    (cp.Customer != null && cp.Customer.Name.Contains(searchTerm)));
            }

            var customerProducts = await query
                .OrderBy(cp => cp.Customer != null ? cp.Customer.Name : "")
                .ThenBy(cp => cp.ProductName)
                .ToListAsync();

            // Get statistics for dashboard
            var allProducts = await _context.CustomerProducts.Include(cp => cp.Customer).ToListAsync();
            ViewBag.TotalProducts = allProducts.Count;
            ViewBag.ActiveProducts = allProducts.Count(cp => cp.IsActive);
            ViewBag.TotalCustomers = await _context.Customers.CountAsync();
            ViewBag.UniqueCategories = allProducts
                .Where(cp => !string.IsNullOrEmpty(cp.Category))
                .Select(cp => cp.Category)
                .Distinct()
                .Count();

            // Get customers and categories for filter dropdowns
            ViewBag.Customers = await _context.Customers
                .OrderBy(c => c.Name)
                .ToListAsync();

            ViewBag.Categories = allProducts
                .Where(cp => !string.IsNullOrEmpty(cp.Category))
                .Select(cp => cp.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            // Pass filter values back to view
            ViewBag.CustomerFilter = customerId;
            ViewBag.CategoryFilter = category;
            ViewBag.SearchTerm = searchTerm;

            return View(customerProducts);
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
                customerProduct.CreatedDate = DateTime.UtcNow;
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