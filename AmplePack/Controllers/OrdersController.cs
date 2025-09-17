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
using AmplePack.ViewModels;
using Microsoft.AspNetCore.Authorization;
using AmplePack.Services;

namespace AmplePack.Controllers
{
    [Authorize]
    public class OrdersController : Controller
    {
        private readonly AppDbContext _context;
        private readonly InvoiceService _invoiceService;

        public OrdersController(AppDbContext context, InvoiceService invoiceService)
        {
            _context = context;
            _invoiceService = invoiceService;
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
        [Authorize(Roles = "Admin,Manager")]
        public IActionResult Create()
        {
            ViewData["CustomerId"] = new SelectList(_context.Customers, "Id", "Name");
            return View();
        }

        // POST: Orders/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager")]
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
        [Authorize(Roles = "Admin,Manager")]
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
                    if (product != null && product.CustomerId == customerId.Value)
                    {
                        model.CustomerProductId = productId.Value;
                        model.BoxType = product.ProductName;
                        model.Length = product.Length;
                        model.Width = product.Width;
                        model.Height = product.Height;
                        model.GSM = product.GSM;
                        model.PaperType = product.PaperType;
                        model.PrintingType = product.PrintingType;
                        model.Quantity = product.DefaultQuantity;
                        model.PricePerBox = product.PricePerBox;
                    }
                }
            }

            // Get customers for dropdown
            ViewBag.Customers = new SelectList(_context.Customers.OrderBy(c => c.Name), "Id", "Name", customerId);

            return View(model);
        }

        // POST: Orders/CreateWorkflow
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> CreateWorkflow(OrderDetailInput input)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Validate required fields
                    if (string.IsNullOrWhiteSpace(input.BoxType))
                    {
                        ModelState.AddModelError("BoxType", "Box type is required.");
                    }

                    if (!input.Length.HasValue || input.Length <= 0)
                    {
                        ModelState.AddModelError("Length", "Valid length is required.");
                    }

                    if (!input.Width.HasValue || input.Width <= 0)
                    {
                        ModelState.AddModelError("Width", "Valid width is required.");
                    }

                    if (!input.Height.HasValue || input.Height <= 0)
                    {
                        ModelState.AddModelError("Height", "Valid height is required.");
                    }

                    if (!ModelState.IsValid)
                    {
                        ViewBag.Customers = new SelectList(_context.Customers.OrderBy(c => c.Name), "Id", "Name", input.CustomerId);
                        return View(input);
                    }

                    // Create the order
                    var order = new Order
                    {
                        CustomerId = input.CustomerId,
                        Date = DateTime.UtcNow,
                        Status = "Pending",
                        TotalAmount = input.Quantity * input.PricePerBox
                    };

                    _context.Orders.Add(order);
                    await _context.SaveChangesAsync();

                    // Create order detail with current values (not just references)
                    var orderDetail = new OrderDetail
                    {
                        OrderId = order.Id,
                        CustomerProductId = input.CustomerProductId, // Reference to original product (optional)
                        BoxType = input.BoxType, // Store current box type
                        Size = $"{input.Length:F1}×{input.Width:F1}×{input.Height:F1}", // Store current size
                        Quantity = input.Quantity,
                        PricePerBox = input.PricePerBox, // Store current price (may differ from master product)
                        DeliveryDate = input.DeliveryDate?.ToUniversalTime(),
                        Notes = input.Notes ?? string.Empty
                    };

                    _context.OrderDetails.Add(orderDetail);
                    await _context.SaveChangesAsync();

                    // Update customer product statistics if linked to a product
                    if (input.CustomerProductId.HasValue)
                    {
                        var customerProduct = await _context.CustomerProducts.FindAsync(input.CustomerProductId.Value);
                        if (customerProduct != null)
                        {
                            customerProduct.TotalOrdersCount++;
                            customerProduct.LastOrderDate = DateTime.UtcNow;
                            _context.Update(customerProduct);
                            await _context.SaveChangesAsync();
                        }
                    }

                    TempData["SuccessMessage"] = "Order created successfully!";
                    return RedirectToAction(nameof(Details), new { id = order.Id });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "An error occurred while creating the order: " + ex.Message);
                }
            }

            // If we got this far, something failed, redisplay form
            ViewBag.Customers = new SelectList(_context.Customers.OrderBy(c => c.Name), "Id", "Name", input.CustomerId);
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

        // API endpoint for getting customer products (enhanced)
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
                    defaultQuantity = cp.DefaultQuantity,
                    length = cp.Length,
                    width = cp.Width,
                    height = cp.Height,
                    gsm = cp.GSM,
                    paperType = cp.PaperType,
                    printingType = cp.PrintingType,
                    description = cp.Description
                })
                .ToListAsync();

            return Json(products);
        }

        // API endpoint for getting product details (enhanced)
        [HttpGet]
        public async Task<IActionResult> GetProductDetails(int id)
        {
            var product = await _context.CustomerProducts.FindAsync(id);
            if (product == null)
            {
                return Json(new { success = false, message = "Product not found" });
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
                    gsm = product.GSM,
                    paperType = product.PaperType,
                    printingType = product.PrintingType,
                    pricePerBox = product.PricePerBox,
                    defaultQuantity = product.DefaultQuantity,
                    description = product.Description,
                    category = product.Category
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
        [Authorize(Roles = "Admin,Manager")]
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
        [Authorize(Roles = "Admin,Manager")]
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
        [Authorize(Roles = "Admin")]
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
        [Authorize(Roles = "Admin")]
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

        // GET: Orders/DownloadInvoice/5
        public async Task<IActionResult> DownloadInvoice(int? id)
        {
            if (id == null)
            {
                TempData["ErrorMessage"] = "Order ID is required.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var order = await _context.Orders
                    .Include(o => o.Customer)
                    .Include(o => o.OrderDetails)
                        .ThenInclude(od => od.CustomerProduct)
                    .FirstOrDefaultAsync(m => m.Id == id);

                if (order == null)
                {
                    TempData["ErrorMessage"] = "Order not found.";
                    return RedirectToAction(nameof(Index));
                }

                var pdfBytes = _invoiceService.GenerateInvoicePdf(order);
                var fileName = $"Invoice_Order_{order.Id}.pdf";

                // Set proper content type and headers for PDF download
                Response.Headers.Add("Content-Disposition", $"attachment; filename=\"{fileName}\"");
                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (QuestPDF.Drawing.Exceptions.DocumentLayoutException ex)
            {
                TempData["ErrorMessage"] = $"PDF layout error: {ex.Message}. Please contact support.";
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Failed to generate invoice PDF: {ex.Message}";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        private bool OrderExists(int id)
        {
            return _context.Orders.Any(e => e.Id == id);
        }
    }
}