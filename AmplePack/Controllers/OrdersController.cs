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
            // Load orders with customer and order details
            var orders = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.CustomerProduct)
                .OrderByDescending(o => o.Date)
                .ToListAsync();

            // Calculate statistics
            ViewBag.TotalOrders = orders.Count;
            ViewBag.PendingOrders = orders.Count(o => o.Status == "Pending");
            ViewBag.ProcessingOrders = orders.Count(o => o.Status == "Processing");
            ViewBag.CompletedOrders = orders.Count(o => o.Status == "Completed");
            ViewBag.TotalRevenue = orders.Where(o => o.Status == "Completed").Sum(o => o.TotalAmount);
            
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
            return RedirectToAction("CreateWorkflow");
        }

        // GET: Orders/CreateWorkflow
        public async Task<IActionResult> CreateWorkflow()
        {
            ViewBag.Customers = await _context.Customers.ToListAsync();
            return View();
        }

        // POST: Orders/CreateWorkflow
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateWorkflow(int customerId, string orderDetails)
        {
            if (customerId == 0 || string.IsNullOrEmpty(orderDetails))
            {
                ViewBag.Customers = await _context.Customers.ToListAsync();
                ViewBag.ErrorMessage = "Please select a customer and add at least one product.";
                return View();
            }

            try
            {
                var orderDetailsList = System.Text.Json.JsonSerializer.Deserialize<List<OrderDetailInput>>(orderDetails);
                
                if (orderDetailsList == null || !orderDetailsList.Any())
                {
                    ViewBag.Customers = await _context.Customers.ToListAsync();
                    ViewBag.ErrorMessage = "Please add at least one product to the order.";
                    return View();
                }

                var order = new Order
                {
                    CustomerId = customerId,
                    Date = DateTime.Now,
                    Status = "Pending",
                    OrderDetails = new List<OrderDetail>()
                };

                decimal totalAmount = 0;

                foreach (var detail in orderDetailsList)
                {
                    if (detail.Quantity > 0)
                    {
                        var orderDetail = new OrderDetail
                        {
                            CustomerProductId = detail.CustomerProductId,
                            BoxType = "Custom Box",
                            Size = "Custom",
                            Quantity = detail.Quantity,
                            PricePerBox = detail.PricePerBox
                        };

                        // If CustomerProductId is provided, get the product details
                        if (detail.CustomerProductId.HasValue)
                        {
                            var customerProduct = await _context.CustomerProducts
                                .FirstOrDefaultAsync(cp => cp.Id == detail.CustomerProductId.Value);

                            if (customerProduct != null)
                            {
                                orderDetail.BoxType = customerProduct.ProductName;
                                orderDetail.Size = customerProduct.SizeDisplay;
                                orderDetail.PricePerBox = customerProduct.PricePerBox;
                            }
                        }

                        order.OrderDetails.Add(orderDetail);
                        totalAmount += orderDetail.Quantity * orderDetail.PricePerBox;
                    }
                }

                order.TotalAmount = totalAmount;

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            catch (Exception)
            {
                ViewBag.Customers = await _context.Customers.ToListAsync();
                ViewBag.ErrorMessage = "An error occurred while creating the order.";
                return View();
            }
        }

        // GET: Orders/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.CustomerProduct)
                .FirstOrDefaultAsync(o => o.Id == id);

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
        public async Task<IActionResult> Edit(int id, Order order)
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

        // GET: Orders/UpdateStatus/5
        public async Task<IActionResult> UpdateStatus(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Orders
                .Include(o => o.Customer)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // POST: Orders/UpdateStatus/5
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
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        private bool OrderExists(int id)
        {
            return _context.Orders.Any(e => e.Id == id);
        }
    }
}