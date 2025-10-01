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
        public async Task<IActionResult> Index(string? customerFilter, string? statusFilter, DateTime? startDate, DateTime? endDate, string? searchTerm)
        {
            var query = _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.CustomerProduct)
                .Where(o => o.Status == "Pending" || o.Status == "Processing") // Only show active orders
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(customerFilter) && int.TryParse(customerFilter, out int customerId))
            {
                query = query.Where(o => o.CustomerId == customerId);
            }

            if (!string.IsNullOrEmpty(statusFilter))
            {
                // Only allow filtering by active statuses
                if (statusFilter == "Pending" || statusFilter == "Processing")
                {
                    query = query.Where(o => o.Status == statusFilter);
                }
            }

            if (startDate.HasValue)
            {
                query = query.Where(o => o.Date >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(o => o.Date <= endDate.Value.AddDays(1));
            }

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(o => 
                    o.Id.ToString().Contains(searchTerm) ||
                    (o.Customer != null && o.Customer.Name.Contains(searchTerm)) ||
                    o.OrderDetails.Any(od => od.BoxType.Contains(searchTerm)));
            }

            var orders = await query.OrderByDescending(o => o.Date).ToListAsync();

            // Get statistics for active orders only
            ViewBag.TotalOrders = orders.Count;
            ViewBag.PendingOrders = orders.Count(o => o.Status == "Pending");
            ViewBag.ProcessingOrders = orders.Count(o => o.Status == "Processing");
            ViewBag.CompletedOrders = await _context.Orders.CountAsync(o => o.Status == "Completed" || o.Status == "Delivered");
            ViewBag.CancelledOrders = await _context.Orders.CountAsync(o => o.Status == "Cancelled");
            
            var currentMonth = DateTime.Now.Month;
            var currentYear = DateTime.Now.Year;
            ViewBag.MonthlyRevenue = await _context.Orders
                .Where(o => o.Status == "Completed" && o.Date.Month == currentMonth && o.Date.Year == currentYear)
                .SumAsync(o => o.TotalAmount);

            // Get customers for filter dropdown
            ViewBag.Customers = await _context.Customers
                .OrderBy(c => c.Name)
                .ToListAsync();

            // Pass filter values back to view
            ViewBag.CustomerFilter = customerFilter;
            ViewBag.StatusFilter = statusFilter;
            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");
            ViewBag.SearchTerm = searchTerm;

            return View(orders);
        }

        // GET: Orders/Completed
        public async Task<IActionResult> Completed(string? customerFilter, DateTime? startDate, DateTime? endDate, string? searchTerm)
        {
            var query = _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.CustomerProduct)
                .Where(o => o.Status == "Completed" || o.Status == "Delivered")
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(customerFilter) && int.TryParse(customerFilter, out int customerId))
            {
                query = query.Where(o => o.CustomerId == customerId);
            }

            if (startDate.HasValue)
            {
                query = query.Where(o => o.Date >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(o => o.Date <= endDate.Value.AddDays(1));
            }

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(o => 
                    o.Id.ToString().Contains(searchTerm) ||
                    (o.Customer != null && o.Customer.Name.Contains(searchTerm)) ||
                    o.OrderDetails.Any(od => od.BoxType.Contains(searchTerm)));
            }

            var completedOrders = await query
                .OrderByDescending(o => o.Date)
                .ToListAsync();

            // Statistics for completed orders
            ViewBag.TotalCompletedOrders = completedOrders.Count;
            ViewBag.TotalRevenue = completedOrders.Sum(o => o.TotalAmount);
            ViewBag.AverageOrderValue = completedOrders.Any() ? completedOrders.Average(o => o.TotalAmount) : 0;
            
            var currentMonth = DateTime.Now.Month;
            var currentYear = DateTime.Now.Year;
            ViewBag.ThisMonthCompleted = completedOrders.Count(o => o.Date.Month == currentMonth && o.Date.Year == currentYear);
            ViewBag.ThisMonthRevenue = completedOrders
                .Where(o => o.Date.Month == currentMonth && o.Date.Year == currentYear)
                .Sum(o => o.TotalAmount);

            // Get customers for filter dropdown
            ViewBag.Customers = await _context.Customers
                .OrderBy(c => c.Name)
                .ToListAsync();

            // Pass filter values back to view
            ViewBag.CustomerFilter = customerFilter;
            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");
            ViewBag.SearchTerm = searchTerm;

            return View(completedOrders);
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

            var oldStatus = order.Status;
            order.Status = status;
            _context.Update(order);
            await _context.SaveChangesAsync();

            // Log status change for audit
            await LogOrderStatusChange(id, oldStatus, status);

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
                // Set content type header
                if (Response.Headers.ContainsKey("Content-Type"))
                    Response.Headers["Content-Type"] = "application/pdf";
                else
                    Response.Headers.Append("Content-Type", "application/pdf");
                    
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

        // POST: Orders/ChangeStatus
        [HttpPost]
        public async Task<IActionResult> ChangeStatus(int id, [FromBody] ChangeStatusRequest request)
        {
            try
            {
                var order = await _context.Orders.FindAsync(id);
                if (order == null)
                {
                    return Json(new { success = false, message = "Order not found" });
                }

                // Validate status
                var validStatuses = new[] { "Pending", "Processing", "Completed", "Delivered", "Cancelled" };
                if (!validStatuses.Contains(request.Status))
                {
                    return Json(new { success = false, message = "Invalid status" });
                }

                var oldStatus = order.Status;
                order.Status = request.Status;
                await _context.SaveChangesAsync();

                // Log the status change
                await LogOrderStatusChange(id, oldStatus, request.Status);

                return Json(new { 
                    success = true, 
                    message = $"Order status changed from {oldStatus} to {request.Status}",
                    newStatus = request.Status
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error updating status: " + ex.Message });
            }
        }

        // POST: Orders/Reorder
        [HttpPost]
        public async Task<IActionResult> Reorder(int id)
        {
            try
            {
                var originalOrder = await _context.Orders
                    .Include(o => o.OrderDetails)
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (originalOrder == null)
                {
                    return Json(new { success = false, message = "Original order not found" });
                }

                // Create new order based on the original
                var newOrder = new Order
                {
                    CustomerId = originalOrder.CustomerId,
                    Date = DateTime.Now,
                    Status = "Pending",
                    TotalAmount = originalOrder.TotalAmount
                };

                _context.Orders.Add(newOrder);
                await _context.SaveChangesAsync();

                // Copy order details
                foreach (var detail in originalOrder.OrderDetails)
                {
                    var newDetail = new OrderDetail
                    {
                        OrderId = newOrder.Id,
                        CustomerProductId = detail.CustomerProductId,
                        BoxType = detail.BoxType,
                        Size = detail.Size,
                        Quantity = detail.Quantity,
                        PricePerBox = detail.PricePerBox,
                        DeliveryDate = detail.DeliveryDate,
                        Notes = detail.Notes
                    };
                    _context.OrderDetails.Add(newDetail);
                }

                await _context.SaveChangesAsync();

                return Json(new { 
                    success = true, 
                    message = "Reorder created successfully",
                    newOrderId = newOrder.Id
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error creating reorder: " + ex.Message });
            }
        }

        // Helper method to log order status changes
        private async Task LogOrderStatusChange(int orderId, string fromStatus, string toStatus, string? changedBy = null)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.Customer)
                    .FirstOrDefaultAsync(o => o.Id == orderId);

                var details = new
                {
                    OrderId = orderId,
                    CustomerName = order?.Customer?.Name ?? "Unknown",
                    FromStatus = fromStatus,
                    ToStatus = toStatus
                };

                var auditLog = new AuditLog
                {
                    EntityType = "Order",
                    EntityId = orderId,
                    Action = "STATUS_CHANGE",
                    Field = "Status",
                    OldValue = fromStatus,
                    NewValue = toStatus,
                    Details = System.Text.Json.JsonSerializer.Serialize(details),
                    ChangedBy = changedBy ?? User?.Identity?.Name ?? "System",
                    Timestamp = DateTime.UtcNow,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = HttpContext.Request.Headers["User-Agent"].ToString()
                };

                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Log the error but don't fail the main operation
                System.Diagnostics.Debug.WriteLine($"Failed to create audit log: {ex.Message}");
            }
        }

        private bool OrderExists(int id)
        {
            return _context.Orders.Any(e => e.Id == id);
        }
    }

    // DTO for order status change logging
    public class OrderStatusChangeLog
    {
        public int OrderId { get; set; }
        public string FromStatus { get; set; } = string.Empty;
        public string ToStatus { get; set; } = string.Empty;
        public string ChangedBy { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }

    // Request DTO for changing order status
    public class ChangeStatusRequest
    {
        public string Status { get; set; } = string.Empty;
    }
}