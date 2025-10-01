using Microsoft.EntityFrameworkCore;
using AmplePack.Data;
using AmplePack.Models;
using AmplePack.ViewModels;
using System.Linq.Expressions;

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

                var topCustomers = await query
                    .Where(o => o.Customer != null)
                    .GroupBy(o => new { o.CustomerId, o.Customer!.Name })
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
                    .ToListAsync();

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
                var orders = await GetFilteredOrdersAsync(filter);
                
                return format.ToLower() switch
                {
                    "excel" => await ExportToExcelAsync(orders.Orders),
                    "csv" => await ExportToCsvAsync(orders.Orders),
                    "pdf" => await ExportToPdfAsync(orders.Orders),
                    _ => throw new ArgumentException($"Unsupported export format: {format}")
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting orders with format: {Format}", format);
                throw;
            }
        }

        private async Task<byte[]> ExportToExcelAsync(List<Order> orders)
        {
            // This would require a library like EPPlus or ClosedXML
            // For now, return CSV format as bytes
            var csv = await ExportToCsvAsync(orders);
            return csv;
        }

        private async Task<byte[]> ExportToCsvAsync(List<Order> orders)
        {
            await Task.CompletedTask; // Make async
            
            var csv = new System.Text.StringBuilder();
            csv.AppendLine("Order ID,Customer,Date,Status,Total Amount,Items");
            
            foreach (var order in orders)
            {
                var items = string.Join("; ", order.OrderDetails?.Select(od => $"{od.BoxType} ({od.Quantity})") ?? new string[0]);
                csv.AppendLine($"{order.Id},{order.Customer?.Name ?? "N/A"},{order.Date:yyyy-MM-dd},{order.Status},{order.TotalAmount:C},\"{items}\"");
            }
            
            return System.Text.Encoding.UTF8.GetBytes(csv.ToString());
        }

        private async Task<byte[]> ExportToPdfAsync(List<Order> orders)
        {
            await Task.CompletedTask; // Make async
            
            // This would require a PDF library like QuestPDF or iTextSharp
            // For now, return empty bytes
            return new byte[0];
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