using Microsoft.EntityFrameworkCore;
using AmplePack.Data;
using AmplePack.Models;
using AmplePack.ViewModels;
using System.Linq.Expressions;
using System.Text;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AmplePack.Services
{
    public class OrderManagementService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<OrderManagementService> _logger;

        public OrderManagementService(AppDbContext context, ILogger<OrderManagementService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<OrderListViewModel> GetFilteredOrdersAsync(OrderFilterViewModel filter)
        {
            try
            {
                // Apply date range presets if specified
                filter.ApplyDateRangePreset();
                
                var query = _context.Orders
                    .Include(o => o.Customer)
                    .Include(o => o.OrderDetails)
                        .ThenInclude(od => od.CustomerProduct)
                    .AsQueryable();

                // Apply filters
                query = ApplyFilters(query, filter);

                // Get total count before pagination
                var totalCount = await query.CountAsync();

                // Apply sorting
                query = ApplySorting(query, filter.SortBy, filter.SortOrder);

                // Apply pagination
                var orders = await query
                    .Skip((filter.Page - 1) * filter.PageSize)
                    .Take(filter.PageSize)
                    .ToListAsync();

                // Calculate page count
                var pageCount = (int)Math.Ceiling((double)totalCount / filter.PageSize);

                // Get statistics
                var stats = await GetOrderStatisticsAsync(filter);

                return new OrderListViewModel
                {
                    Orders = orders,
                    Filter = filter,
                    Stats = stats,
                    TotalCount = totalCount,
                    PageCount = pageCount
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting filtered orders with filter: {@Filter}", filter);
                throw;
            }
        }

        private IQueryable<Order> ApplyFilters(IQueryable<Order> query, OrderFilterViewModel filter)
        {
            // Active orders only filter
            if (filter.ShowActiveOnly)
            {
                query = query.Where(o => o.Status == "Pending" || o.Status == "Processing");
            }

            // Customer filter
            if (!string.IsNullOrEmpty(filter.CustomerFilter) && int.TryParse(filter.CustomerFilter, out int customerId))
            {
                query = query.Where(o => o.CustomerId == customerId);
            }

            // Status filter
            if (!string.IsNullOrEmpty(filter.StatusFilter))
            {
                query = query.Where(o => o.Status == filter.StatusFilter);
            }

            // Date range filter
            if (filter.StartDate.HasValue)
            {
                query = query.Where(o => o.Date >= filter.StartDate.Value);
            }

            if (filter.EndDate.HasValue)
            {
                var endDate = filter.EndDate.Value.AddDays(1); // Include full end date
                query = query.Where(o => o.Date < endDate);
            }

            // Search term filter
            if (!string.IsNullOrEmpty(filter.SearchTerm))
            {
                var searchTerm = filter.SearchTerm.ToLower();
                query = query.Where(o => 
                    o.Id.ToString().Contains(searchTerm) ||
                    (o.Customer != null && o.Customer.Name.ToLower().Contains(searchTerm)) ||
                    o.OrderDetails.Any(od => od.BoxType.ToLower().Contains(searchTerm)));
            }

            // Overdue filter
            if (filter.ShowOverdue)
            {
                var today = DateTime.Today;
                query = query.Where(o => 
                    o.OrderDetails.Any(od => od.DeliveryDate.HasValue && od.DeliveryDate.Value < today) &&
                    (o.Status == "Pending" || o.Status == "Processing"));
            }

            return query;
        }

        private IQueryable<Order> ApplySorting(IQueryable<Order> query, string sortBy, string sortOrder)
        {
            var isDescending = sortOrder?.ToLower() == "desc";

            return sortBy?.ToLower() switch
            {
                "id" => isDescending ? query.OrderByDescending(o => o.Id) : query.OrderBy(o => o.Id),
                "customer" => isDescending ? query.OrderByDescending(o => o.Customer!.Name) : query.OrderBy(o => o.Customer!.Name),
                "status" => isDescending ? query.OrderByDescending(o => o.Status) : query.OrderBy(o => o.Status),
                "total" or "totalamount" => isDescending ? query.OrderByDescending(o => o.TotalAmount) : query.OrderBy(o => o.TotalAmount),
                "date" or _ => isDescending ? query.OrderByDescending(o => o.Date) : query.OrderBy(o => o.Date)
            };
        }

        public async Task<OrderStatisticsViewModel> GetOrderStatisticsAsync(OrderFilterViewModel? filter = null)
        {
            try
            {
                var stats = new OrderStatisticsViewModel();

                // Basic counts
                var allOrders = _context.Orders.AsQueryable();
                
                if (filter != null && filter.HasFilters())
                {
                    allOrders = ApplyFilters(allOrders, filter);
                }

                stats.TotalOrders = await allOrders.CountAsync();
                stats.PendingOrders = await allOrders.CountAsync(o => o.Status == "Pending");
                stats.ProcessingOrders = await allOrders.CountAsync(o => o.Status == "Processing");
                stats.CompletedOrders = await allOrders.CountAsync(o => o.Status == "Completed" || o.Status == "Delivered");
                stats.CancelledOrders = await allOrders.CountAsync(o => o.Status == "Cancelled");

                // Overdue orders
                var today = DateTime.Today;
                stats.OverdueOrders = await allOrders.CountAsync(o => 
                    o.OrderDetails.Any(od => od.DeliveryDate.HasValue && od.DeliveryDate.Value < today) &&
                    (o.Status == "Pending" || o.Status == "Processing"));

                // Financial metrics
                var completedOrders = allOrders.Where(o => o.Status == "Completed" || o.Status == "Delivered");
                stats.TotalRevenue = await completedOrders.SumAsync(o => o.TotalAmount);

                if (stats.TotalOrders > 0)
                {
                    stats.AverageOrderValue = stats.TotalRevenue / stats.TotalOrders;
                }

                // Monthly revenue
                var currentMonth = DateTime.Now.Month;
                var currentYear = DateTime.Now.Year;
                stats.MonthlyRevenue = await completedOrders
                    .Where(o => o.Date.Month == currentMonth && o.Date.Year == currentYear)
                    .SumAsync(o => o.TotalAmount);

                // Yearly revenue
                stats.YearlyRevenue = await completedOrders
                    .Where(o => o.Date.Year == currentYear)
                    .SumAsync(o => o.TotalAmount);

                // Completion rate
                var totalActiveOrders = await allOrders.CountAsync(o => o.Status != "Cancelled");
                if (totalActiveOrders > 0)
                {
                    stats.CompletionRate = (decimal)stats.CompletedOrders / totalActiveOrders * 100;
                }

                // Get trends and top customers
                stats.OrderTrends = await GetOrderTrendsAsync(filter);
                stats.TopCustomers = await GetTopCustomersAsync(filter);

                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting order statistics");
                throw;
            }
        }

        private async Task<List<OrderTrendItem>> GetOrderTrendsAsync(OrderFilterViewModel? filter = null)
        {
            try
            {
                var query = _context.Orders.AsQueryable();
                
                if (filter != null && filter.HasFilters())
                {
                    query = ApplyFilters(query, filter);
                }

                var last30Days = DateTime.Today.AddDays(-30);
                
                var trends = await query
                    .Where(o => o.Date >= last30Days)
                    .GroupBy(o => o.Date.Date)
                    .Select(g => new OrderTrendItem
                    {
                        Date = g.Key,
                        OrderCount = g.Count(),
                        Revenue = g.Where(o => o.Status == "Completed" || o.Status == "Delivered").Sum(o => o.TotalAmount),
                        Period = "daily"
                    })
                    .OrderBy(x => x.Date)
                    .ToListAsync();

                return trends;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting order trends");
                return new List<OrderTrendItem>();
            }
        }

        private async Task<List<CustomerOrderSummary>> GetTopCustomersAsync(OrderFilterViewModel? filter = null)
        {
            try
            {
                var query = _context.Orders
                    .Include(o => o.Customer)
                    .AsQueryable();
                
                if (filter != null && filter.HasFilters())
                {
                    query = ApplyFilters(query, filter);
                }

                // Fetch data first, then process in memory to avoid SQLite decimal ordering issues
                var ordersData = await query
                    .Where(o => o.Customer != null)
                    .Select(o => new { o.CustomerId, o.Customer!.Name, o.TotalAmount, o.Date })
                    .ToListAsync();

                // Group and calculate in memory
                var topCustomers = ordersData
                    .GroupBy(o => new { o.CustomerId, o.Name })
                    .Select(g => new CustomerOrderSummary
                    {
                        CustomerId = g.Key.CustomerId,
                        CustomerName = g.Key.Name,
                        OrderCount = g.Count(),
                        TotalValue = g.Sum(o => o.TotalAmount),
                        LastOrderDate = g.Max(o => o.Date)
                    })
                    .OrderByDescending(c => c.TotalValue) // This ordering happens in memory, not in SQLite
                    .Take(10)
                    .ToList();

                return topCustomers;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting top customers");
                return new List<CustomerOrderSummary>();
            }
        }

        public async Task<byte[]> ExportOrdersAsync(OrderFilterViewModel filter, string format)
        {
            try
            {
                _logger.LogInformation("Starting export with format: {Format}", format);
                
                var query = _context.Orders
                    .Include(o => o.Customer)
                    .Include(o => o.OrderDetails)
                        .ThenInclude(od => od.CustomerProduct)
                    .AsQueryable();

                query = ApplyFilters(query, filter);
                query = ApplySorting(query, filter.SortBy, filter.SortOrder);

                var orders = await query.ToListAsync();
                _logger.LogInformation("Retrieved {Count} orders for export", orders.Count);
                
                return format.ToLower() switch
                {
                    "csv" => await ExportToCsvAsync(orders),
                    "pdf" => await ExportToPdfAsync(orders),
                    _ => throw new ArgumentException($"Unsupported export format: {format}. Use 'csv' or 'pdf'.")
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical error in ExportOrdersAsync with format: {Format}. Filter: {@Filter}", format, filter);
                
                // If everything fails, try to return a simple CSV with basic order info
                try
                {
                    var fallbackOrders = await _context.Orders
                        .Include(o => o.Customer)
                        .Take(100) // Limit for safety
                        .ToListAsync();
                    
                    _logger.LogWarning("Attempting fallback CSV export with {Count} orders", fallbackOrders.Count);
                    return await ExportToCsvAsync(fallbackOrders);
                }
                catch (Exception fallbackEx)
                {
                    _logger.LogError(fallbackEx, "Even fallback export failed");
                    throw; // Re-throw original exception
                }
            }
        }

        private async Task<byte[]> ExportToCsvAsync(List<Order> orders)
        {
            await Task.CompletedTask; // Make async
            
            var csvContent = new StringBuilder();
            
            // Professional CSV Header with Company Info
            csvContent.AppendLine("# AMPLE PACKAGING - ORDER EXPORT REPORT");
            csvContent.AppendLine($"# Generated on: {DateTime.Now:dd-MMM-yyyy HH:mm} IST");
            csvContent.AppendLine($"# Total Orders: {orders.Count}");
            csvContent.AppendLine($"# Total Value: Rs.{orders.Sum(o => o.TotalAmount):N2}");
            csvContent.AppendLine("#");
            
            // Professional Column Headers (Standard Invoice Format)
            csvContent.AppendLine("Order Number,Customer Name,Contact Number,Order Date,Status,Quantity,Unit Price,Total Amount,Product Type,Size Specification,Delivery Date,Remarks");
            
            foreach (var order in orders)
            {
                if (order.OrderDetails?.Any() == true)
                {
                    foreach (var detail in order.OrderDetails)
                    {
                        // Professional data formatting with no special characters
                        var orderNumber = $"CP{order.Id:D3}";
                        var customerName = CleanCsvText(order.Customer?.Name ?? "N/A");
                        var contactNumber = CleanCsvText(order.Customer?.Contact ?? "N/A");
                        var orderDate = order.Date.ToString("dd-MMM-yyyy");
                        var status = GetProfessionalStatus(order.Status);
                        var quantity = detail.Quantity.ToString();
                        var unitPrice = $"Rs.{detail.PricePerBox:N2}";
                        var totalAmount = $"Rs.{order.TotalAmount:N2}";
                        var productType = CleanCsvText(detail.BoxType ?? "Standard Box");
                        var sizeSpec = CleanCsvText(detail.Size ?? "Standard");
                        var deliveryDate = detail.DeliveryDate?.ToString("dd-MMM-yyyy") ?? "TBD";
                        var remarks = CleanCsvText(detail.Notes ?? "Standard Order");

                        // Create professional CSV line
                        var line = $"{orderNumber},{customerName},{contactNumber},{orderDate},{status},{quantity},{unitPrice},{totalAmount},{productType},{sizeSpec},{deliveryDate},{remarks}";
                        csvContent.AppendLine(line);
                    }
                }
                else
                {
                    // Handle orders with no details professionally
                    var orderNumber = $"CP{order.Id:D3}";
                    var customerName = CleanCsvText(order.Customer?.Name ?? "N/A");
                    var contactNumber = CleanCsvText(order.Customer?.Contact ?? "N/A");
                    var orderDate = order.Date.ToString("dd-MMM-yyyy");
                    var status = GetProfessionalStatus(order.Status);
                    var totalAmount = $"Rs.{order.TotalAmount:N2}";
                    
                    var line = $"{orderNumber},{customerName},{contactNumber},{orderDate},{status},0,Rs.0.00,{totalAmount},No Items Specified,N/A,TBD,Order requires item specification";
                    csvContent.AppendLine(line);
                }
            }
            
            // Professional Summary Section
            csvContent.AppendLine("#");
            csvContent.AppendLine("# SUMMARY STATISTICS");
            csvContent.AppendLine($"# Total Orders Exported: {orders.Count}");
            var completedCount = orders.Count(o => o.Status == "Completed");
            csvContent.AppendLine($"# Completed Orders: {completedCount}");
            var pendingCount = orders.Count(o => o.Status == "Pending");
            var processingCount = orders.Count(o => o.Status == "Processing");
            csvContent.AppendLine($"# Pending Orders: {pendingCount}");
            csvContent.AppendLine($"# Processing Orders: {processingCount}");
            csvContent.AppendLine($"# Total Business Value: Rs.{orders.Sum(o => o.TotalAmount):N2}");
            csvContent.AppendLine($"# Average Order Value: Rs.{(orders.Any() ? orders.Average(o => o.TotalAmount) : 0):N2}");
            csvContent.AppendLine("#");
            csvContent.AppendLine("# Report generated by AmplePack Management System");
            csvContent.AppendLine($"# Export timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss} IST");
            
            return Encoding.UTF8.GetBytes(csvContent.ToString());
        }

        // Clean CSV text - removes problematic characters and ensures professional format
        private string CleanCsvText(string input)
        {
            if (string.IsNullOrEmpty(input)) return "N/A";
            
            // Remove problematic characters and escape properly
            var cleaned = input
                .Replace("\"", "'")     // Replace quotes with apostrophes
                .Replace(",", ";")      // Replace commas with semicolons
                .Replace("\r", " ")     // Remove carriage returns
                .Replace("\n", " ")     // Remove line feeds
                .Replace("?", "")       // Remove question marks
                .Replace("#", "No.")    // Replace hash with "No."
                .Trim();
            
            // Ensure no empty strings
            return string.IsNullOrEmpty(cleaned) ? "N/A" : cleaned;
        }

        // Professional status formatting
        private string GetProfessionalStatus(string status)
        {
            return status switch
            {
                "Pending" => "PENDING",
                "Processing" => "IN PROGRESS",
                "Completed" => "COMPLETED",
                "Delivered" => "DELIVERED",
                "Cancelled" => "CANCELLED",
                _ => CleanCsvText(status).ToUpper()
            };
        }

        private async Task<byte[]> ExportToPdfAsync(List<Order> orders)
        {
            await Task.CompletedTask; // Make async
            
            try
            {
                // Enable QuestPDF debugging for troubleshooting
                QuestPDF.Settings.EnableDebugging = true;
                _logger.LogInformation("Generating professional invoice-style PDF export");

                // Limit to reasonable amount for PDF stability
                var ordersToRender = orders.Take(25).ToList();
                _logger.LogInformation("Exporting {Count} orders to PDF (limited from {Total} for optimal layout)", ordersToRender.Count, orders.Count);

                var document = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        // Use A4 Portrait with professional margins
                        page.Size(PageSizes.A4);
                        page.Margin(15, Unit.Millimetre);
                        page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial"));

                        // Professional Header
                        page.Header()
                            .Height(60)
                            .Padding(8)
                            .Row(row =>
                            {
                                row.RelativeItem().Column(column =>
                                {
                                    column.Item().Text("AMPLE PACKAGING")
                                        .FontSize(16)
                                        .SemiBold()
                                        .FontColor(Colors.Blue.Darken2);
                                    
                                    column.Item().Text("Order Summary Report")
                                        .FontSize(12)
                                        .FontColor(Colors.Grey.Darken2);
                                    
                                    column.Item().Text($"Generated: {DateTime.Now:dd-MMM-yyyy HH:mm}")
                                        .FontSize(8)
                                        .FontColor(Colors.Grey.Medium);
                                });

                                row.ConstantItem(120).AlignRight().Column(column =>
                                {
                                    column.Item().Text($"Total Orders: {orders.Count}")
                                        .FontSize(10)
                                        .SemiBold();
                                    
                                    column.Item().Text($"Total Value: Rs.{orders.Sum(o => o.TotalAmount):N0}")
                                        .FontSize(10)
                                        .SemiBold()
                                        .FontColor(Colors.Green.Darken1);
                                    
                                    if (orders.Count > 25)
                                    {
                                        column.Item().Text($"Showing: {ordersToRender.Count} orders")
                                            .FontSize(8)
                                            .FontColor(Colors.Orange.Medium);
                                    }
                                });
                            });

                        // Professional Content with Essential Data Only
                        page.Content()
                            .PaddingVertical(5)
                            .Column(column =>
                            {
                                if (ordersToRender.Any())
                                {
                                    column.Item().Table(table =>
                                    {
                                        // Professional column widths - only essential data
                                        table.ColumnsDefinition(columns =>
                                        {
                                            columns.ConstantColumn(40);  // Order#
                                            columns.ConstantColumn(80);  // Customer
                                            columns.ConstantColumn(50);  // Date
                                            columns.ConstantColumn(45);  // Status
                                            columns.ConstantColumn(35);  // Qty
                                            columns.ConstantColumn(60);  // Amount
                                            columns.RelativeColumn(1);  // Product
                                        });

                                        // Professional Header
                                        table.Header(header =>
                                        {
                                            header.Cell().Element(ProfessionalHeaderCell).Text("Order#");
                                            header.Cell().Element(ProfessionalHeaderCell).Text("Customer");
                                            header.Cell().Element(ProfessionalHeaderCell).Text("Date");
                                            header.Cell().Element(ProfessionalHeaderCell).Text("Status");
                                            header.Cell().Element(ProfessionalHeaderCell).Text("Items");
                                            header.Cell().Element(ProfessionalHeaderCell).Text("Amount");
                                            header.Cell().Element(ProfessionalHeaderCell).Text("Product Type");
                                        });

                                        // Data rows with professional formatting
                                        foreach (var order in ordersToRender)
                                        {
                                            // Professional data formatting
                                            var orderNum = $"CP{order.Id:D3}";
                                            var customerName = CleanText(order.Customer?.Name ?? "N/A", 12);
                                            var orderDate = order.Date.ToString("dd-MMM");
                                            var status = GetStatusDisplay(order.Status);
                                            var itemCount = order.OrderDetails?.Count ?? 0;
                                            var amount = $"Rs.{order.TotalAmount:N0}";
                                            var productType = GetProductSummary(order.OrderDetails);

                                            table.Cell().Element(ProfessionalDataCell).Text(orderNum);
                                            table.Cell().Element(ProfessionalDataCell).Text(customerName);
                                            table.Cell().Element(ProfessionalDataCell).Text(orderDate);
                                            table.Cell().Element(ProfessionalStatusCell).Text(status);
                                            table.Cell().Element(ProfessionalDataCell).AlignCenter().Text(itemCount.ToString());
                                            table.Cell().Element(ProfessionalAmountCell).Text(amount);
                                            table.Cell().Element(ProfessionalDataCell).Text(productType);
                                        }
                                    });

                                    // Professional Summary Section
                                    if (orders.Count > 25)
                                    {
                                        column.Item().PaddingTop(10)
                                            .Background(Colors.Blue.Lighten5)
                                            .Padding(8)
                                            .Row(row =>
                                            {
                                                row.RelativeItem().Text("Note: This PDF shows the first 25 orders for optimal display. For complete data export, please use CSV format.")
                                                    .FontSize(8)
                                                    .FontColor(Colors.Blue.Darken1);
                                                
                                                row.ConstantItem(80).AlignRight().Text("CSV Recommended")
                                                    .FontSize(8)
                                                    .SemiBold()
                                                    .FontColor(Colors.Green.Darken1);
                                            });
                                    }

                                    // Professional Statistics
                                    column.Item().PaddingTop(15)
                                        .Row(row =>
                                        {
                                            var completedOrders = ordersToRender.Count(o => o.Status == "Completed");
                                            var pendingOrders = ordersToRender.Count(o => o.Status == "Pending");
                                            var totalValue = ordersToRender.Sum(o => o.TotalAmount);

                                            row.RelativeItem().Column(col =>
                                            {
                                                col.Item().Text("Summary Statistics")
                                                    .FontSize(10)
                                                    .SemiBold()
                                                    .FontColor(Colors.Blue.Darken2);
                                                
                                                col.Item().Text($"Completed: {completedOrders} | Pending: {pendingOrders}")
                                                    .FontSize(8);
                                                
                                                col.Item().Text($"Displayed Value: Rs.{totalValue:N0}")
                                                    .FontSize(8);
                                            });
                                        });
                                }
                                else
                                {
                                    column.Item().AlignCenter().PaddingVertical(50)
                                        .Text("No orders found for the selected criteria")
                                        .FontSize(14)
                                        .FontColor(Colors.Grey.Darken1);
                                }
                            });

                        // Professional Footer
                        page.Footer()
                            .Height(25)
                            .Padding(5)
                            .Row(row =>
                            {
                                row.RelativeItem().Text("AmplePack - Packaging Solutions")
                                    .FontSize(8)
                                    .FontColor(Colors.Grey.Medium);
                                
                                row.ConstantItem(100).AlignRight().Text(x =>
                                {
                                    x.Span("Page ");
                                    x.CurrentPageNumber();
                                    x.Span(" of ");
                                    x.TotalPages();
                                });
                            });
                    });
                });

                // Generate PDF with error handling
                var pdfBytes = document.GeneratePdf();
                _logger.LogInformation("Professional PDF generated successfully: {Size} bytes", pdfBytes.Length);
                return pdfBytes;
            }
            catch (Exception ex)
            {
                // Log error and fallback to CSV
                _logger.LogError(ex, "Professional PDF generation failed. Order count: {Count}, Error: {Message}", 
                    orders.Count, ex.Message);

                _logger.LogInformation("Falling back to CSV export due to PDF generation failure");
                return await ExportToCsvAsync(orders);
            }
        }

        // Professional Header Cell Styling
        private static IContainer ProfessionalHeaderCell(IContainer container)
        {
            return container
                .Background(Colors.Blue.Darken1)
                .Padding(4)
                .DefaultTextStyle(x => x.FontSize(8).SemiBold().FontColor(Colors.White));
        }

        // Professional Data Cell Styling
        private static IContainer ProfessionalDataCell(IContainer container)
        {
            return container
                .Padding(3)
                .DefaultTextStyle(x => x.FontSize(7));
        }

        // Professional Status Cell with Color Coding
        private static IContainer ProfessionalStatusCell(IContainer container)
        {
            return container
                .Padding(3)
                .DefaultTextStyle(x => x.FontSize(7).SemiBold());
        }

        // Professional Amount Cell (Right Aligned)
        private static IContainer ProfessionalAmountCell(IContainer container)
        {
            return container
                .Padding(3)
                .AlignRight()
                .DefaultTextStyle(x => x.FontSize(7).SemiBold().FontColor(Colors.Green.Darken1));
        }

        // Clean text helper - removes special characters and limits length
        private string CleanText(string input, int maxLength)
        {
            if (string.IsNullOrEmpty(input)) return "N/A";
            
            // Remove problematic characters
            var cleaned = input.Replace("?", "").Replace("#", "").Replace("\"", "").Trim();
            
            // Limit length professionally
            if (cleaned.Length > maxLength)
            {
                return cleaned.Substring(0, maxLength - 1).TrimEnd() + ".";
            }
            
            return cleaned;
        }

        // Professional status display
        private string GetStatusDisplay(string status)
        {
            return status switch
            {
                "Pending" => "PENDING",
                "Processing" => "PROCESS",
                "Completed" => "COMPLETE",
                "Delivered" => "DELIVERED",
                "Cancelled" => "CANCELLED",
                _ => CleanText(status, 8).ToUpper()
            };
        }

        // Professional product summary
        private string GetProductSummary(ICollection<OrderDetail>? orderDetails)
        {
            if (orderDetails == null || !orderDetails.Any())
                return "No Items";

            var firstItem = orderDetails.First();
            var boxType = CleanText(firstItem.BoxType ?? "Box", 15);
            
            if (orderDetails.Count == 1)
            {
                return $"{boxType} ({firstItem.Quantity})";
            }
            else
            {
                return $"{boxType} +{orderDetails.Count - 1}";
            }
        }

        public async Task<bool> UpdateOrderStatusAsync(int orderId, string newStatus)
        {
            try
            {
                var order = await _context.Orders.FindAsync(orderId);
                if (order == null)
                {
                    return false;
                }

                var oldStatus = order.Status;
                order.Status = newStatus;
                
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Order {OrderId} status changed from {OldStatus} to {NewStatus}", 
                    orderId, oldStatus, newStatus);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order {OrderId} status to {Status}", orderId, newStatus);
                return false;
            }
        }

        public async Task<Order?> DuplicateOrderAsync(int orderId)
        {
            try
            {
                var originalOrder = await _context.Orders
                    .Include(o => o.OrderDetails)
                    .FirstOrDefaultAsync(o => o.Id == orderId);

                if (originalOrder == null)
                {
                    return null;
                }

                var newOrder = new Order
                {
                    CustomerId = originalOrder.CustomerId,
                    Date = DateTime.UtcNow,
                    Status = "Pending",
                    TotalAmount = originalOrder.TotalAmount
                };

                _context.Orders.Add(newOrder);
                await _context.SaveChangesAsync();

                // Copy order details
                foreach (var detail in originalOrder.OrderDetails ?? new List<OrderDetail>())
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
                
                _logger.LogInformation("Order {OrderId} duplicated as {NewOrderId}", orderId, newOrder.Id);
                
                return newOrder;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error duplicating order {OrderId}", orderId);
                return null;
            }
        }
    }
}