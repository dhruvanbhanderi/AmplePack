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
        public async Task<IActionResult> Index(int? selectedCustomerId, string searchTerm)
        {
            var query = _context.CustomerProducts
                .Include(cp => cp.Customer)
                .AsQueryable();

            // Filter by customer if selected
            if (selectedCustomerId.HasValue)
            {
                query = query.Where(cp => cp.CustomerId == selectedCustomerId.Value);
            }

            // Search functionality
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(cp => 
                    cp.ProductName.Contains(searchTerm) ||
                    cp.Description.Contains(searchTerm) ||
                    cp.Customer!.Name.Contains(searchTerm));
            }

            var customerProducts = await query
                .OrderBy(cp => cp.Customer!.Name)
                .ThenBy(cp => cp.ProductName)
                .ToListAsync();

            // Get customer names for dropdown
            ViewBag.CustomerNames = await _context.Customers
                .ToDictionaryAsync(c => c.Id, c => c.Name);
            ViewBag.SelectedCustomerId = selectedCustomerId;
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
        public IActionResult Create()
        {
            ViewData["CustomerId"] = new SelectList(_context.Customers, "Id", "Name");
            return View();
        }

        // POST: CustomerProducts/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,CustomerId,ProductName,Description,Length,Width,Height,GSM,PaperType,PrintingType,PricePerBox,DefaultQuantity,Category")] CustomerProduct customerProduct)
        {
            if (ModelState.IsValid)
            {
                customerProduct.CreatedDate = DateTime.Now;
                _context.Add(customerProduct);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
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
                return RedirectToAction(nameof(Index));
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
                _context.CustomerProducts.Remove(customerProduct);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CustomerProductExists(int id)
        {
            return _context.CustomerProducts.Any(e => e.Id == id);
        }
    }
}