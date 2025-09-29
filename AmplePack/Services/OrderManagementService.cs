using AmplePack.Data;
using AmplePack.Models;
using AmplePack.ViewModels;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using OfficeOpenXml;
using System.Text;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AmplePack.Services
{
    public class OrderManagementService
    {
        private readonly AppDbContext _context;

        public OrderManagementService(AppDbContext context)
        {
            _context = context;
            // Set EPPlus license context
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        public async Task<OrderListViewModel> GetFilteredOrdersAsync(OrderFilterViewModel filter)
        {
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
            query = ApplySorting(query, filter);

            // Apply pagination
            var orders = await query
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            // Get statistics
            var stats = await GetOrderStatsAsync(filter);

            // Get customers for filter dropdown
            var customers = await GetCustomersForFilterAsync();

            return new OrderListViewModel
            {
                Orders = orders.Select(MapToOrderSummaryDto).ToList(),
                Filter = filter,
                TotalCount = totalCount,
                PageCount = (int)Math.Ceiling((double)totalCount / filter.PageSize),
                Stats = stats,
                Customers = customers
            };
        }

        private IQueryable<Order> ApplyFilters(IQueryable<Order> query, OrderFilterViewModel filter)
        {
            // Search term filter
            if (!string.IsNullOrEmpty(filter.SearchTerm))
            {
                var searchTerm = filter.SearchTerm.ToLower();
                query = query.Where(o => 
                    o.Id.ToString().Contains(searchTerm) ||
                    o.Customer!.Name.ToLower().Contains(searchTerm) ||
                    o.Customer.Email.ToLower().Contains(searchTerm) ||
                    o.OrderDetails.Any(od => od.BoxType.ToLower().Contains(searchTerm)));
            }

            // Status filter
            if (!string.IsNullOrEmpty(filter.Status))
            {
                query = query.Where(o => o.Status == filter.Status);
            }

            // Customer filter
            if (filter.CustomerId.HasValue)
            {
                query = query.Where(o => o.CustomerId == filter.CustomerId);
            }

            // Date range filter
            if (filter.StartDate.HasValue)
            {
                query = query.Where(o => o.Date >= filter.StartDate.Value);
            }

            if (filter.EndDate.HasValue)
            {
                query = query.Where(o => o.Date <= filter.EndDate.Value);
            }

            // Date period filter
            if (!string.IsNullOrEmpty(filter.DatePeriod))
            {
                var (start, end) = GetDateRangeForPeriod(filter.DatePeriod);
                if (start.HasValue && end.HasValue)
                {
                    query = query.Where(o => o.Date >= start.Value && o.Date <= end.Value);
                }
            }

            // Quick filters
            if (filter.ShowOverdue)
            {
                query = query.Where(o => o.OrderDetails.Any(od => 
                    od.DeliveryDate.HasValue && od.DeliveryDate < DateTime.Today && o.Status != "Completed"));
            }

            return query;
        }

        private IQueryable<Order> ApplySorting(IQueryable<Order> query, OrderFilterViewModel filter)
        {
            // Primary sorting
            query = filter.SortBy?.ToLower() switch
            {
                "id" => filter.SortOrder == "asc" ? query.OrderBy(o => o.Id) : query.OrderByDescending(o => o.Id),
                "customer" => filter.SortOrder == "asc" ? query.OrderBy(o => o.Customer!.Name) : query.OrderByDescending(o => o.Customer!.Name),
                "date" => filter.SortOrder == "asc" ? query.OrderBy(o => o.Date) : query.OrderByDescending(o => o.Date),
                "status" => filter.SortOrder == "asc" ? query.OrderBy(o => o.Status) : query.OrderByDescending(o => o.Status),
                "totalamount" => filter.SortOrder == "asc" ? query.OrderBy(o => o.TotalAmount) : query.OrderByDescending(o => o.TotalAmount),
                _ => query.OrderByDescending(o => o.Date)
            };

            // Secondary sorting
            if (!string.IsNullOrEmpty(filter.SecondarySortBy))
            {
                var orderedQuery = (IOrderedQueryable<Order>)query;
                query = filter.SecondarySortBy?.ToLower() switch
                {
                    "id" => filter.SecondarySortOrder == "asc" ? orderedQuery.ThenBy(o => o.Id) : orderedQuery.ThenByDescending(o => o.Id),
                    "customer" => filter.SecondarySortOrder == "asc" ? orderedQuery.ThenBy(o => o.Customer!.Name) : orderedQuery.ThenByDescending(o => o.Customer!.Name),
                    "date" => filter.SecondarySortOrder == "asc" ? orderedQuery.ThenBy(o => o.Date) : orderedQuery.ThenByDescending(o => o.Date),
                    "status" => filter.SecondarySortOrder == "asc" ? orderedQuery.ThenBy(o => o.Status) : orderedQuery.ThenByDescending(o => o.Status),
                    "totalamount" => filter.SecondarySortOrder == "asc" ? orderedQuery.ThenBy(o => o.TotalAmount) : orderedQuery.ThenByDescending(o => o.TotalAmount),
                    _ => orderedQuery
                };
            }

            return query;
        }

        private async Task<OrderStatsDto> GetOrderStatsAsync(OrderFilterViewModel filter)
        {
            var baseQuery = _context.Orders.AsQueryable();
            var currentMonth = DateTime.Now;
            var currentYear = DateTime.Now.Year;

            // Apply date filters to base query if specified
            if (filter.StartDate.HasValue || filter.EndDate.HasValue || !string.IsNullOrEmpty(filter.DatePeriod))
            {
                baseQuery = ApplyFilters(baseQuery, filter);
            }

            var stats = new OrderStatsDto
            {
                TotalOrders = await baseQuery.CountAsync(),
                PendingOrders = await baseQuery.CountAsync(o => o.Status == "Pending"),
                ProcessingOrders = await baseQuery.CountAsync(o => o.Status == "Processing"),
                CompletedOrders = await baseQuery.CountAsync(o => o.Status == "Completed"),
                CancelledOrders = await baseQuery.CountAsync(o => o.Status == "Cancelled"),
                MonthlyRevenue = await baseQuery
                    .Where(o => o.Status == "Completed" && o.Date.Month == currentMonth.Month && o.Date.Year == currentMonth.Year)
                    .SumAsync(o => o.TotalAmount),
                YearlyRevenue = await baseQuery
                    .Where(o => o.Status == "Completed" && o.Date.Year == currentYear)
                    .SumAsync(o => o.TotalAmount)
            };

            var completedOrders = await baseQuery.Where(o => o.Status == "Completed").ToListAsync();
            stats.AverageOrderValue = completedOrders.Any() ? completedOrders.Average(o => o.TotalAmount) : 0;

            // Get order trends for the last 30 days
            var last30Days = DateTime.Today.AddDays(-30);
            stats.OrderTrends = await baseQuery
                .Where(o => o.Date >= last30Days)
                .GroupBy(o => o.Date.Date)
                .Select(g => new OrderTrendDto
                {
                    Date = g.Key,
                    OrderCount = g.Count(),
                    Revenue = g.Where(o => o.Status == "Completed").Sum(o => o.TotalAmount)
                })
                .OrderBy(t => t.Date)
                .ToListAsync();

            // Get top customers
            stats.TopCustomers = await baseQuery
                .Include(o => o.Customer)
                .Where(o => o.Status == "Completed")
                .GroupBy(o => new { o.CustomerId, o.Customer!.Name })
                .Select(g => new TopCustomerDto
                {
                    CustomerId = g.Key.CustomerId,
                    CustomerName = g.Key.Name,
                    OrderCount = g.Count(),
                    TotalValue = g.Sum(o => o.TotalAmount)
                })
                .OrderByDescending(c => c.TotalValue)
                .Take(5)
                .ToListAsync();

            return stats;
        }

        private async Task<List<CustomerSummaryDto>> GetCustomersForFilterAsync()
        {
            return await _context.Customers
                .Select(c => new CustomerSummaryDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Email = c.Email,
                    Contact = c.Contact
                })
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        private static OrderSummaryDto MapToOrderSummaryDto(Order order)
        {
            var itemCount = order.OrderDetails?.Count ?? 0;
            var itemsSummary = itemCount > 0 
                ? string.Join(", ", order.OrderDetails!.Take(2).Select(od => $"{od.BoxType} ({od.Quantity})")) +
                  (itemCount > 2 ? $" + {itemCount - 2} more" : "")
                : "No items";

            var nextDeliveryDate = order.OrderDetails?
                .Where(od => od.DeliveryDate.HasValue)
                .Min(od => od.DeliveryDate);

            return new OrderSummaryDto
            {
                Id = order.Id,
                CustomerName = order.Customer?.Name ?? "Unknown",
                CustomerEmail = order.Customer?.Email ?? "",
                Date = order.Date,
                Status = order.Status,
                TotalAmount = order.TotalAmount,
                ItemCount = itemCount,
                ItemsSummary = itemsSummary,
                DeliveryDate = nextDeliveryDate,
                IsOverdue = nextDeliveryDate.HasValue && 
                           nextDeliveryDate < DateTime.Today && 
                           order.Status != "Completed"
            };
        }

        private static (DateTime? start, DateTime? end) GetDateRangeForPeriod(string period)
        {
            var today = DateTime.Today;
            
            return period?.ToLower() switch
            {
                "thismonth" => (new DateTime(today.Year, today.Month, 1), 
                               new DateTime(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month))),
                "lastmonth" => (new DateTime(today.AddMonths(-1).Year, today.AddMonths(-1).Month, 1),
                               new DateTime(today.AddMonths(-1).Year, today.AddMonths(-1).Month, 
                                          DateTime.DaysInMonth(today.AddMonths(-1).Year, today.AddMonths(-1).Month))),
                "thisquarter" => GetQuarterRange(today),
                "thisyear" => (new DateTime(today.Year, 1, 1), new DateTime(today.Year, 12, 31)),
                "last30days" => (today.AddDays(-30), today),
                "last90days" => (today.AddDays(-90), today),
                _ => (null, null)
            };
        }

        private static (DateTime start, DateTime end) GetQuarterRange(DateTime date)
        {
            var quarter = (date.Month - 1) / 3 + 1;
            var startMonth = (quarter - 1) * 3 + 1;
            var endMonth = startMonth + 2;
            
            return (
                new DateTime(date.Year, startMonth, 1),
                new DateTime(date.Year, endMonth, DateTime.DaysInMonth(date.Year, endMonth))
            );
        }

        public async Task<byte[]> ExportOrdersAsync(OrderFilterViewModel filter, string format)
        {
            // Get all orders (no pagination for export)
            var originalPageSize = filter.PageSize;
            var originalPage = filter.Page;
            filter.PageSize = int.MaxValue;
            filter.Page = 1;

            var orderList = await GetFilteredOrdersAsync(filter);

            // Restore original pagination
            filter.PageSize = originalPageSize;
            filter.Page = originalPage;

            return format.ToLower() switch
            {
                "excel" => await ExportToExcelAsync(orderList.Orders, filter.SelectedColumns),
                "csv" => ExportToCsv(orderList.Orders, filter.SelectedColumns),
                "pdf" => await ExportToPdfAsync(orderList.Orders, filter.SelectedColumns),
                _ => throw new ArgumentException("Unsupported export format")
            };
        }

        private async Task<byte[]> ExportToExcelAsync(List<OrderSummaryDto> orders, string[]? selectedColumns)
        {
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Orders");

            var columns = GetExportColumns(selectedColumns);
            
            // Headers
            for (int i = 0; i < columns.Count; i++)
            {
                worksheet.Cells[1, i + 1].Value = columns[i].Header;
                worksheet.Cells[1, i + 1].Style.Font.Bold = true;
            }

            // Data
            for (int row = 0; row < orders.Count; row++)
            {
                for (int col = 0; col < columns.Count; col++)
                {
                    worksheet.Cells[row + 2, col + 1].Value = columns[col].GetValue(orders[row]);
                }
            }

            // Auto-fit columns
            worksheet.Cells.AutoFitColumns();

            return await package.GetAsByteArrayAsync();
        }

        private byte[] ExportToCsv(List<OrderSummaryDto> orders, string[]? selectedColumns)
        {
            var columns = GetExportColumns(selectedColumns);
            var csv = new StringBuilder();

            // Headers
            csv.AppendLine(string.Join(",", columns.Select(c => $"\"{c.Header}\"")));

            // Data
            foreach (var order in orders)
            {
                var values = columns.Select(c => $"\"{c.GetValue(order)}\"");
                csv.AppendLine(string.Join(",", values));
            }

            return Encoding.UTF8.GetBytes(csv.ToString());
        }

        private async Task<byte[]> ExportToPdfAsync(List<OrderSummaryDto> orders, string[]? selectedColumns)
        {
            var columns = GetExportColumns(selectedColumns);
            
            return await Task.Run(() =>
            {
                var document = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4.Landscape());
                        page.Margin(2, Unit.Centimetre);
                        page.PageColor(Colors.White);
                        page.DefaultTextStyle(x => x.FontSize(10));

                        page.Header()
                            .Text("Orders Export Report")
                            .SemiBold().FontSize(20).FontColor(Colors.Blue.Medium);

                        page.Content()
                            .PaddingVertical(1, Unit.Centimetre)
                            .Table(table =>
                            {
                                table.ColumnsDefinition(def =>
                                {
                                    foreach (var column in columns)
                                    {
                                        def.RelativeColumn();
                                    }
                                });

                                table.Header(header =>
                                {
                                    foreach (var column in columns)
                                    {
                                        header.Cell().Element(CellStyle).Text(column.Header);
                                    }

                                    static IContainer CellStyle(IContainer container)
                                    {
                                        return container.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Black);
                                    }
                                });

                                foreach (var order in orders)
                                {
                                    foreach (var column in columns)
                                    {
                                        table.Cell().Element(CellStyle).Text(column.GetValue(order)?.ToString() ?? "");
                                    }

                                    static IContainer CellStyle(IContainer container)
                                    {
                                        return container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5);
                                    }
                                }
                            });

                        page.Footer()
                            .AlignCenter()
                            .Text(x =>
                            {
                                x.Span("Generated on ");
                                x.Span(DateTime.Now.ToString("yyyy-MM-dd HH:mm")).SemiBold();
                            });
                    });
                });

                return document.GeneratePdf();
            });
        }

        private List<ExportColumn> GetExportColumns(string[]? selectedColumns)
        {
            var allColumns = new List<ExportColumn>
            {
                new("Order ID", o => o.Id),
                new("Customer", o => o.CustomerName),
                new("Email", o => o.CustomerEmail),
                new("Date", o => o.Date.ToString("yyyy-MM-dd")),
                new("Status", o => o.Status),
                new("Total Amount", o => o.TotalAmount.ToString("C")),
                new("Items", o => o.ItemsSummary),
                new("Delivery Date", o => o.DeliveryDate?.ToString("yyyy-MM-dd") ?? "")
            };

            if (selectedColumns?.Any() == true)
            {
                return allColumns.Where(c => selectedColumns.Contains(c.Header.Replace(" ", "").ToLower())).ToList();
            }

            return allColumns;
        }

        private class ExportColumn
        {
            public string Header { get; }
            public Func<OrderSummaryDto, object?> GetValue { get; }

            public ExportColumn(string header, Func<OrderSummaryDto, object?> getValue)
            {
                Header = header;
                GetValue = getValue;
            }
        }
    }
}