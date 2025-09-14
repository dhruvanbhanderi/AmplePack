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

namespace AmplePack.Pages_Orders
{
    public class CreateWorkflowModel : PageModel
    {
        private readonly AmplePack.Data.AppDbContext _context;
        private readonly ILogger<CreateWorkflowModel> _logger;

        public CreateWorkflowModel(AmplePack.Data.AppDbContext context, ILogger<CreateWorkflowModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            // Populate customer dropdown
            ViewData["CustomerId"] = new SelectList(
                await _context.Customers.OrderBy(c => c.Name).ToListAsync(), 
                "Id", "Name"
            );
            
            // Set default order date to today
            Order = new Order
            {
                Date = DateTime.Today,
                Status = "Pending"
            };
            
            return Page();
        }

        [BindProperty]
        public Order Order { get; set; } = default!;

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                // Repopulate dropdown on validation error
                ViewData["CustomerId"] = new SelectList(
                    await _context.Customers.OrderBy(c => c.Name).ToListAsync(), 
                    "Id", "Name"
                );
                return Page();
            }

            // Get order details from JSON
            if (Request.Form.ContainsKey("orderDetails"))
            {
                var orderDetailsJson = Request.Form["orderDetails"].ToString();
                if (!string.IsNullOrEmpty(orderDetailsJson))
                {
                    try
                    {
                        var orderDetailsList = System.Text.Json.JsonSerializer.Deserialize<List<OrderDetailInput>>(orderDetailsJson);
                        if (orderDetailsList?.Any() == true)
                        {
                            // Create the order
                            _context.Orders.Add(Order);
                            await _context.SaveChangesAsync();
                            _logger.LogInformation($"Created order with ID: {Order.Id}");

                            // Create order details with CustomerProduct links
                            foreach (var detail in orderDetailsList)
                            {
                                if (!string.IsNullOrEmpty(detail.BoxType) && detail.Quantity > 0 && detail.PricePerBox > 0)
                                {
                                    var orderDetail = new OrderDetail
                                    {
                                        OrderId = Order.Id,
                                        CustomerProductId = detail.CustomerProductId,
                                        BoxType = detail.BoxType,
                                        Size = detail.Size,
                                        Quantity = detail.Quantity,
                                        PricePerBox = detail.PricePerBox,
                                        DeliveryDate = detail.DeliveryDate,
                                        Notes = detail.Notes ?? ""
                                    };
                                    _context.OrderDetails.Add(orderDetail);

                                    // Update product pattern statistics if linked to a saved product
                                    if (detail.CustomerProductId.HasValue)
                                    {
                                        var productPattern = await _context.CustomerProducts.FindAsync(detail.CustomerProductId.Value);
                                        if (productPattern != null)
                                        {
                                            productPattern.TotalOrdersCount++;
                                            productPattern.LastOrderDate = DateTime.Now;
                                        }
                                    }
                                }
                            }
                            await _context.SaveChangesAsync();
                            _logger.LogInformation($"Added {orderDetailsList.Count} order details");

                            return RedirectToPage("./Index");
                        }
                    }
                    catch (System.Text.Json.JsonException ex)
                    {
                        _logger.LogError(ex, "Invalid order details JSON format");
                        ModelState.AddModelError("", "Invalid order details format");
                    }
                }
            }

            // If we get here, there was an error
            ModelState.AddModelError("", "Please add at least one product to the order");
            ViewData["CustomerId"] = new SelectList(
                await _context.Customers.OrderBy(c => c.Name).ToListAsync(), 
                "Id", "Name"
            );
            return Page();
        }

        // API endpoint to get customer products
        public async Task<IActionResult> OnGetCustomerProductsAsync(int customerId)
        {
            try
            {
                var products = await _context.CustomerProducts
                    .Where(cp => cp.CustomerId == customerId && cp.IsActive)
                    .Select(cp => new
                    {
                        Id = cp.Id,
                        ProductName = cp.ProductName,
                        SizeDisplay = $"{cp.Length}×{cp.Width}×{cp.Height}",
                        SpecificationDisplay = $"{cp.GSM} GSM, {cp.PaperType}, {cp.PrintingType}",
                        PricePerBox = cp.PricePerBox,
                        DefaultQuantity = cp.DefaultQuantity,
                        Category = cp.Category,
                        Length = cp.Length,
                        Width = cp.Width,
                        Height = cp.Height,
                        GSM = cp.GSM,
                        PaperType = cp.PaperType,
                        PrintingType = cp.PrintingType,
                        Description = cp.Description
                    })
                    .OrderBy(cp => cp.ProductName)
                    .ToListAsync();

                return new JsonResult(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load customer products for customer {CustomerId}", customerId);
                return new JsonResult(new { error = "Failed to load customer products" }) { StatusCode = 500 };
            }
        }

        // API endpoint to create new customer products during order creation
        public async Task<IActionResult> OnPostCreateCustomerProductAsync()
        {
            try
            {
                var form = Request.Form;
                var customerId = int.Parse(form["customerId"]);
                
                var customerProduct = new CustomerProduct
                {
                    CustomerId = customerId,
                    ProductName = form["productName"],
                    Description = form["description"],
                    Length = decimal.Parse(form["length"]),
                    Width = decimal.Parse(form["width"]),
                    Height = decimal.Parse(form["height"]),
                    GSM = int.Parse(form["gsm"]),
                    PaperType = form["paperType"],
                    PrintingType = form["printingType"],
                    PricePerBox = decimal.Parse(form["pricePerBox"]),
                    DefaultQuantity = 1,
                    Category = form["category"],
                    IsActive = true,
                    CreatedDate = DateTime.Now
                };

                _context.CustomerProducts.Add(customerProduct);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Created new customer product: {customerProduct.ProductName} for customer {customerId}");

                return new JsonResult(new { 
                    success = true, 
                    productId = customerProduct.Id,
                    message = "Product created successfully" 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create customer product");
                return new JsonResult(new { 
                    success = false, 
                    message = "Failed to create product: " + ex.Message 
                }) { StatusCode = 500 };
            }
        }
    }
}