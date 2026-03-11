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

                // ? SQLITE FIX: Load data first, then sort in memory to avoid decimal ordering issues
                var ordersData = await query
                    .Where(o => o.Customer != null)
                    .Select(o => new { o.CustomerId, o.Customer!.Name, o.TotalAmount, o.Date })
                    .ToListAsync();

                // Group and calculate in memory
                var customerSummaries = ordersData
                    .GroupBy(o => new { o.CustomerId, o.Name })
                    .Select(g => new CustomerOrderSummary
                    {
                        CustomerId = g.Key.CustomerId,
                        CustomerName = g.Key.Name,
                        OrderCount = g.Count(),
                        TotalValue = g.Sum(o => o.TotalAmount),
                        LastOrderDate = g.Max(o => o.Date)
                    })
                    .OrderByDescending(c => c.TotalValue)
                    .Take(10)
                    .ToList();

                return customerSummaries;
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
                var query = _context.Orders
                    .Include(o => o.Customer)
                    .Include(o => o.OrderDetails)
                        .ThenInclude(od => od.CustomerProduct)
                    .AsQueryable();

                query = ApplyFilters(query, filter);
                query = ApplySorting(query, filter.SortBy, filter.SortOrder);

                var orders = await query.ToListAsync();
                
                return format.ToLower() switch
                {
                    "csv" => await ExportToCsvAsync(orders),
                    "pdf" => await ExportToPdfAsync(orders),
                    _ => throw new ArgumentException($"Unsupported export format: {format}. Use 'csv' or 'pdf'.")
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting orders with format: {Format}", format);
                throw;
            }
        }

        private async Task<byte[]> ExportToCsvAsync(List<Order> orders)
        {
            await Task.CompletedTask; // Make async
            
            var csvContent = new StringBuilder();
            csvContent.AppendLine("Order ID,Customer Name,Customer Contact,Order Date,Status,Total Amount (Rs.),Box Type,Size,Quantity,Price per Box (Rs.),Delivery Date,Notes");
            
            foreach (var order in orders)
            {
                if (order.OrderDetails?.Any() == true)
                {
                    foreach (var detail in order.OrderDetails)
                    {
                        var line = string.Format(
                            "CP{0:D3},\"{1}\",\"{2}\",{3},{4},Rs.{5:F2},\"{6}\",\"{7}\",{8},Rs.{9:F2},\"{10}\",\"{11}\"",
                            order.Id,
                            EscapeCsvField(order.Customer?.Name ?? "N/A"),
                            EscapeCsvField(order.Customer?.Contact ?? "N/A"),
                            order.Date.ToString("dd-MM-yyyy"), // Indian date format
                            order.Status,
                            order.TotalAmount,
                            EscapeCsvField(detail.BoxType ?? ""),
                            EscapeCsvField(detail.Size ?? ""),
                            detail.Quantity,
                            detail.PricePerBox,
                            detail.DeliveryDate?.ToString("dd-MM-yyyy") ?? "N/A",
                            EscapeCsvField(detail.Notes ?? "")
                        );
                        csvContent.AppendLine(line);
                    }
                }
                else
                {
                    var line = string.Format(
                        "CP{0:D3},\"{1}\",\"{2}\",{3},{4},Rs.{5:F2},\"No items\",\"\",\"\",\"\",\"\",\"\"",
                        order.Id,
                        EscapeCsvField(order.Customer?.Name ?? "N/A"),
                        EscapeCsvField(order.Customer?.Contact ?? "N/A"),
                        order.Date.ToString("dd-MM-yyyy"),
                        order.Status,
                        order.TotalAmount
                    );
                    csvContent.AppendLine(line);
                }
            }
            
            // Add summary
            csvContent.AppendLine("");
            csvContent.AppendLine("Summary:");
            csvContent.AppendLine($"Total Orders:,{orders.Count}");
            csvContent.AppendLine($"Total Value:,Rs.{orders.Sum(o => o.TotalAmount):F2}");
            csvContent.AppendLine($"Export Date:,{DateTime.Now:dd-MM-yyyy HH:mm}");
            
            return Encoding.UTF8.GetBytes(csvContent.ToString());
        }

        private string EscapeCsvField(string field)
        {
            if (string.IsNullOrEmpty(field))
                return "";
                
            // Escape quotes and handle special characters
            return field.Replace("\"", "\"\"");
        }

        private async Task<byte[]> ExportToPdfAsync(List<Order> orders)
        {
            await Task.CompletedTask; // Make async
            
            try
            {
                var document = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4.Landscape());
                        page.Margin(2, Unit.Centimetre);
                        page.DefaultTextStyle(x => x.FontSize(10));

                        page.Header()
                            .AlignCenter()
                            .Text("AmplePack - Orders Report")
                            .SemiBold().FontSize(16).FontColor(Colors.Blue.Medium);

                        page.Content()
                            .PaddingVertical(1, Unit.Centimetre)
                            .Table(table =>
                            {
                                // Define columns
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(1); // Order ID
                                    columns.RelativeColumn(2); // Customer
                                    columns.RelativeColumn(1.5f); // Date
                                    columns.RelativeColumn(1); // Status
                                    columns.RelativeColumn(1.5f); // Amount
                                    columns.RelativeColumn(2); // Items
                                });

                                // Header
                                table.Header(header =>
                                {
                                    header.Cell().Element(HeaderStyle).Text("Order ID");
                                    header.Cell().Element(HeaderStyle).Text("Customer");
                                    header.Cell().Element(HeaderStyle).Text("Date");
                                    header.Cell().Element(HeaderStyle).Text("Status");
                                    header.Cell().Element(HeaderStyle).Text("Amount (Rs.)");
                                    header.Cell().Element(HeaderStyle).Text("Items");
                                });

                                // Data rows
                                foreach (var order in orders.Take(100)) // Limit for PDF performance
                                {
                                    var items = order.OrderDetails?.Any() == true
                                        ? string.Join(", ", order.OrderDetails.Select(od => $"{od.BoxType} ({od.Quantity})"))
                                        : "No items";

                                    table.Cell().Element(CellStyle).Text($"CP{order.Id:D3}");
                                    table.Cell().Element(CellStyle).Text(order.Customer?.Name ?? "N/A");
                                    table.Cell().Element(CellStyle).Text(order.Date.ToString("dd-MM-yyyy"));
                                    table.Cell().Element(CellStyle).Text(order.Status);
                                    table.Cell().Element(CellStyle).Text($"Rs.{order.TotalAmount:F2}");
                                    table.Cell().Element(CellStyle).Text(items);
                                }

                                static IContainer HeaderStyle(IContainer container)
                                {
                                    return container
                                        .DefaultTextStyle(x => x.SemiBold().FontSize(9))
                                        .PaddingVertical(8)
                                        .BorderBottom(1)
                                        .BorderColor(Colors.Black)
                                        .AlignCenter();
                                }

                                static IContainer CellStyle(IContainer container)
                                {
                                    return container
                                        .BorderBottom(1)
                                        .BorderColor(Colors.Grey.Lighten2)
                                        .PaddingVertical(5)
                                        .PaddingHorizontal(3);
                                }
                            });

                        page.Footer()
                            .AlignCenter()
                            .Text(x =>
                            {
                                x.Span("Generated: ");
                                x.Span(DateTime.Now.ToString("dd-MM-yyyy HH:mm")).SemiBold();
                                x.Span(" | Total Orders: ");
                                x.Span(orders.Count.ToString()).SemiBold();
                                x.Span(" | Total: Rs.");
                                x.Span(orders.Sum(o => o.TotalAmount).ToString("F2")).SemiBold();
                            });
                    });
                });

                return document.GeneratePdf();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating PDF export");
                // Fallback to CSV if PDF fails
                return await ExportToCsvAsync(orders);
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