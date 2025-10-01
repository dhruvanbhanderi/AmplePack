using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AmplePack.Data;
using AmplePack.Models;

namespace AmplePack.Controllers
{
    [Authorize]
    public class InventoryController : Controller
    {
        private readonly AppDbContext _context;

        public InventoryController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Inventory
        public async Task<IActionResult> Index(string? category, string? stockStatus, string? searchTerm)
        {
            var query = _context.Inventories.AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(i => i.Category == category);
            }

            if (!string.IsNullOrEmpty(stockStatus))
            {
                switch (stockStatus.ToLower())
                {
                    case "instock":
                        query = query.Where(i => i.AvailableQuantity > i.ReorderLevel);
                        break;
                    case "lowstock":
                        query = query.Where(i => i.AvailableQuantity <= i.ReorderLevel && i.AvailableQuantity > 0);
                        break;
                    case "outofstock":
                        query = query.Where(i => i.AvailableQuantity <= 0);
                        break;
                }
            }

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(i => i.ItemName.Contains(searchTerm) || 
                                        (i.Category != null && i.Category.Contains(searchTerm)));
            }

            var inventoryItems = await query.OrderBy(i => i.ItemName).ToListAsync();

            // Calculate real statistics
            var allItems = await _context.Inventories.ToListAsync();
            ViewBag.TotalItems = allItems.Count;
            ViewBag.InStockItems = allItems.Count(i => i.AvailableQuantity > i.ReorderLevel);
            ViewBag.LowStockItems = allItems.Count(i => i.AvailableQuantity <= i.ReorderLevel && i.AvailableQuantity > 0);
            ViewBag.OutOfStockItems = allItems.Count(i => i.AvailableQuantity <= 0);
            ViewBag.TotalInventoryValue = allItems.Sum(i => i.AvailableQuantity * i.UnitPrice);

            // Get categories for filter dropdown
            ViewBag.Categories = await _context.Inventories
                .Where(i => !string.IsNullOrEmpty(i.Category))
                .Select(i => i.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            // Pass filter values back to view
            ViewBag.CategoryFilter = category;
            ViewBag.StockStatusFilter = stockStatus;
            ViewBag.SearchTerm = searchTerm;

            return View(inventoryItems);
        }

        // GET: Inventory/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var inventory = await _context.Inventories
                .FirstOrDefaultAsync(m => m.Id == id);
            if (inventory == null)
            {
                return NotFound();
            }

            return View(inventory);
        }

        // GET: Inventory/Create
        [Authorize(Roles = "Admin,Manager")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Inventory/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Create([Bind("Id,ProductName,ItemName,AvailableQuantity,UnitPrice,Unit,Category,ReorderLevel")] Inventory inventory)
        {
            if (ModelState.IsValid)
            {
                _context.Add(inventory);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(inventory);
        }

        // GET: Inventory/Edit/5
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var inventory = await _context.Inventories.FindAsync(id);
            if (inventory == null)
            {
                return NotFound();
            }
            return View(inventory);
        }

        // POST: Inventory/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,ProductName,ItemName,AvailableQuantity,UnitPrice,Unit,Category,ReorderLevel")] Inventory inventory)
        {
            if (id != inventory.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(inventory);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!InventoryExists(inventory.Id))
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
            return View(inventory);
        }

        // GET: Inventory/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var inventory = await _context.Inventories
                .FirstOrDefaultAsync(m => m.Id == id);
            if (inventory == null)
            {
                return NotFound();
            }

            return View(inventory);
        }

        // POST: Inventory/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var inventory = await _context.Inventories.FindAsync(id);
            if (inventory != null)
            {
                _context.Inventories.Remove(inventory);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // POST: Inventory/AdjustStock
        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> AdjustStock([FromBody] StockAdjustmentRequest request)
        {
            try
            {
                var inventory = await _context.Inventories.FindAsync(request.ItemId);
                if (inventory == null)
                {
                    return Json(new { success = false, message = "Inventory item not found" });
                }

                var oldQuantity = inventory.AvailableQuantity;
                
                if (request.Action.ToLower() == "add")
                {
                    inventory.AvailableQuantity += request.Quantity;
                }
                else if (request.Action.ToLower() == "remove")
                {
                    if (inventory.AvailableQuantity < request.Quantity)
                    {
                        return Json(new { success = false, message = "Insufficient stock available" });
                    }
                    inventory.AvailableQuantity -= request.Quantity;
                }
                else
                {
                    return Json(new { success = false, message = "Invalid action" });
                }

                _context.Update(inventory);
                await _context.SaveChangesAsync();

                // Determine stock status
                string stockStatus = inventory.AvailableQuantity <= 0 ? "Out of Stock" :
                                   inventory.AvailableQuantity <= inventory.ReorderLevel ? "Low Stock" : "In Stock";

                // Log the adjustment
                var auditLog = new AuditLog
                {
                    EntityType = "Inventory",
                    EntityId = request.ItemId,
                    Action = "STOCK_" + request.Action.ToUpper(),
                    Field = "AvailableQuantity",
                    OldValue = oldQuantity.ToString(),
                    NewValue = inventory.AvailableQuantity.ToString(),
                    Details = $"Reason: {request.Reason}",
                    ChangedBy = User?.Identity?.Name ?? "System",
                    Timestamp = DateTime.UtcNow,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = HttpContext.Request.Headers["User-Agent"].ToString()
                };

                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();

                return Json(new { 
                    success = true, 
                    message = $"Stock {request.Action}ed successfully",
                    newQuantity = $"{inventory.AvailableQuantity:F2} {inventory.Unit}",
                    stockStatus = stockStatus
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error adjusting stock: " + ex.Message });
            }
        }

        // GET: API endpoint for current stock data
        [HttpGet]
        [Route("api/inventory/current-stock")]
        public async Task<IActionResult> GetCurrentStock()
        {
            try
            {
                var inventoryData = await _context.Inventories
                    .Select(i => new
                    {
                        id = i.Id,
                        availableQuantity = i.AvailableQuantity,
                        unit = i.Unit,
                        stockStatus = i.AvailableQuantity <= 0 ? "Out of Stock" :
                                     i.AvailableQuantity <= i.ReorderLevel ? "Low Stock" : "In Stock"
                    })
                    .ToListAsync();

                return Json(new { success = true, data = inventoryData });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        private bool InventoryExists(int id)
        {
            return _context.Inventories.Any(e => e.Id == id);
        }
    }

    // DTO for stock adjustment requests
    public class StockAdjustmentRequest
    {
        public int ItemId { get; set; }
        public decimal Quantity { get; set; }
        public string Action { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }
}