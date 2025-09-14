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
    public class OrdersController : Controller
    {
        private readonly AppDbContext _context;

        public OrdersController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Orders
        public async Task<IActionResult> Index()
        {
            var orders = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.CustomerProduct)
                .OrderByDescending(o => o.Date)
                .ToListAsync();

            // Get statistics for dashboard
            ViewBag.TotalOrders = orders.Count;
            ViewBag.PendingOrders = orders.Count(o => o.Status == "Pending");
            ViewBag.ProcessingOrders = orders.Count(o => o.Status == "Processing");
            ViewBag.CompletedOrders = orders.Count(o => o.Status == "Completed");
            ViewBag.CancelledOrders = orders.Count(o => o.Status == "Cancelled");
            
            var currentMonth = DateTime.Now.Month;
            var currentYear = DateTime.Now.Year;
            ViewBag.MonthlyRevenue = orders
                .Where(o => o.Status == "Completed" && o.Date.Month == currentMonth && o.Date.Year == currentYear)
                .Sum(o => o.TotalAmount);

            return View(orders);
        }

        // GET: Orders/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.CustomerProduct)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // GET: Orders/Create
        public IActionResult Create()
        {
            ViewData["CustomerId"] = new SelectList(_context.Customers, "Id", "Name");
            return View();
        }

        // POST: Orders/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,CustomerId,Date,Status,TotalAmount")] Order order)
        {
            if (ModelState.IsValid)
            {
                _context.Add(order);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["CustomerId"] = new SelectList(_context.Customers, "Id", "Name", order.CustomerId);
            return View(order);
        }

        // GET: Orders/CreateWorkflow
        public async Task<IActionResult> CreateWorkflow(int? customerId, int? productId)
        {
            var model = new OrderDetailInput();

            // Pre-fill customer if provided
            if (customerId.HasValue)
            {
                model.CustomerId = customerId.Value;
                
                // Pre-fill product if provided
                if (productId.HasValue)
                {
                    var product = await _context.CustomerProducts.FindAsync(productId.Value);
                    if (product != null)
                    {
                        model.CustomerProductId = productId.Value;
                        model.Quantity = product.DefaultQuantity;
                        model.PricePerBox = product.PricePerBox;
                    }
                }
            }

            // Get customers for dropdown
            ViewBag.Customers = new SelectList(_context.Customers, "Id", "Name", customerId);

            return View(model);
        }

        // POST: Orders/CreateWorkflow
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateWorkflow(OrderDetailInput input)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Create the order
                    var order = new Order
                    {
                        CustomerId = input.CustomerId,
                        Date = DateTime.Now,
                        Status = "Pending",
                        TotalAmount = input.Quantity * input.PricePerBox
                    };

                    _context.Orders.Add(order);
                    await _context.SaveChangesAsync();

                    // Create order detail
                    var orderDetail = new OrderDetail
                    {
                        OrderId = order.Id,
                        CustomerProductId = input.CustomerProductId,
                        Quantity = input.Quantity,
                        PricePerBox = input.PricePerBox,
                        DeliveryDate = input.DeliveryDate,
                        Notes = input.Notes ?? string.Empty
                    };

                    // Set box details based on source
                    if (input.CustomerProductId.HasValue)
                    {
                        var product = await _context.CustomerProducts.FindAsync(input.CustomerProductId.Value);
                        if (product != null)
                        {
                            orderDetail.BoxType = product.ProductName;
                            orderDetail.Size = $"{product.Length}×{product.Width}×{product.Height}";
                        }
                    }
                    else
                    {
                        // Manual entry
                        orderDetail.BoxType = input.BoxType ?? "Custom Box";
                        orderDetail.Size = $"{input.Length}×{input.Width}×{input.Height}";
                    }

                    _context.OrderDetails.Add(orderDetail);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Order created successfully!";
                    return RedirectToAction(nameof(Details), new { id = order.Id });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "An error occurred while creating the order: " + ex.Message);
                }
            }

            // If we got this far, something failed, redisplay form
            ViewBag.Customers = new SelectList(_context.Customers, "Id", "Name", input.CustomerId);
            return View(input);
        }

        // API endpoint for getting customer details
        [HttpGet]
        public async Task<IActionResult> GetCustomerDetails(int id)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null)
            {
                return Json(new { success = false });
            }

            return Json(new 
            { 
                success = true, 
                customer = new 
                { 
                    id = customer.Id,
                    name = customer.Name,
                    email = customer.Email,
                    contact = customer.Contact
                }
            });
        }

        // API endpoint for getting customer products
        [HttpGet]
        public async Task<IActionResult> GetCustomerProducts(int customerId)
        {
            var products = await _context.CustomerProducts
                .Where(cp => cp.CustomerId == customerId && cp.IsActive)
                .Select(cp => new 
                {
                    id = cp.Id,
                    productName = cp.ProductName,
                    pricePerBox = cp.PricePerBox,
                    defaultQuantity = cp.DefaultQuantity
                })
                .ToListAsync();

            return Json(products);
        }

        // API endpoint for getting product details
        [HttpGet]
        public async Task<IActionResult> GetProductDetails(int id)
        {
            var product = await _context.CustomerProducts.FindAsync(id);
            if (product == null)
            {
                return Json(new { success = false });
            }

            return Json(new 
            { 
                success = true, 
                product = new 
                { 
                    id = product.Id,
                    productName = product.ProductName,
                    length = product.Length,
                    width = product.Width,
                    height = product.Height,
                    pricePerBox = product.PricePerBox,
                    defaultQuantity = product.DefaultQuantity
                }
            });
        }

        // POST: Orders/UpdateStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            order.Status = status;
            _context.Update(order);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Order #{id} status updated to {status}";
            return RedirectToAction(nameof(Index));
        }

        // GET: Orders/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }
            
            ViewBag.Customers = new SelectList(_context.Customers, "Id", "Name", order.CustomerId);
            return View(order);
        }

        // POST: Orders/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,CustomerId,Date,Status,TotalAmount")] Order order)
        {
            if (id != order.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(order);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!OrderExists(order.Id))
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
            
            ViewBag.Customers = new SelectList(_context.Customers, "Id", "Name", order.CustomerId);
            return View(order);
        }

        // GET: Orders/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // POST: Orders/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order != null)
            {
                _context.Orders.Remove(order);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool OrderExists(int id)
        {
            return _context.Orders.Any(e => e.Id == id);
        }
    }
}