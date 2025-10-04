using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AmplePack.Data;
using AmplePack.Models;
using AmplePack.Services;
using System.Text.Json;

namespace AmplePack.Controllers
{
    [Authorize]
    public class ReportsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly EnhancedReportService _reportService;
        private readonly ILogger<ReportsController> _logger;

        public ReportsController(AppDbContext context, EnhancedReportService reportService, ILogger<ReportsController> logger)
        {
            _context = context;
            _reportService = reportService;
            _logger = logger;
        }

        // Keep existing Index method unchanged
        public async Task<IActionResult> Index()
        {
            var currentMonth = DateTime.UtcNow.Month;
            var currentYear = DateTime.UtcNow.Year;

            var viewModel = new ReportsViewModel();

            // Sales Reports
            viewModel.TotalRevenue = await _context.Orders
                .Where(o => o.Status == "Completed")
                .SumAsync(o => o.TotalAmount);

            viewModel.MonthlyRevenue = await _context.Orders
                .Where(o => o.Status == "Completed" && o.Date.Month == currentMonth && o.Date.Year == currentYear)
                .SumAsync(o => o.TotalAmount);

            viewModel.TotalOrdersThisMonth = await _context.Orders
                .CountAsync(o => o.Date.Month == currentMonth && o.Date.Year == currentYear);

            // Get monthly revenue for the last 12 months
            viewModel.MonthlyRevenueChart = await GetMonthlyRevenueData();

            // Customer Reports
            viewModel.TotalCustomers = await _context.Customers.CountAsync();
            
            viewModel.NewCustomersThisMonth = await _context.Customers
                .CountAsync(c => c.Orders.Any(o => o.Date.Month == currentMonth && o.Date.Year == currentYear));

            // Fixed: Get top customers by loading data first, then ordering in memory
            var customersWithOrders = await _context.Customers
                .Include(c => c.Orders)
                .ToListAsync();
            
            viewModel.TopCustomers = customersWithOrders
                .OrderByDescending(c => c.Orders?.Where(o => o.Status == "Completed").Sum(o => o.TotalAmount) ?? 0)
                .Take(5)
                .ToList();

            // Inventory Reports
            viewModel.TotalInventoryItems = await _context.Inventories.CountAsync();
            viewModel.LowStockCount = await _context.Inventories.CountAsync(i => i.AvailableQuantity <= i.ReorderLevel);
            
            // Calculate total inventory value
            viewModel.TotalInventoryValue = await _context.Inventories
                .SumAsync(i => i.AvailableQuantity * i.UnitPrice);
            
            // Fixed: Get critical stock items by loading data first, then ordering in memory
            var criticalStockItems = await _context.Inventories
                .Where(i => i.AvailableQuantity <= i.ReorderLevel)
                .ToListAsync();
            
            viewModel.CriticalStockItems = criticalStockItems
                .OrderBy(i => i.AvailableQuantity)
                .Take(5)
                .ToList();

            // Order Status Reports
            viewModel.PendingOrders = await _context.Orders.CountAsync(o => o.Status == "Pending");
            viewModel.ProcessingOrders = await _context.Orders.CountAsync(o => o.Status == "Processing");
            viewModel.CompletedOrders = await _context.Orders.CountAsync(o => o.Status == "Completed");
            viewModel.CancelledOrders = await _context.Orders.CountAsync(o => o.Status == "Cancelled");

            return View(viewModel);
        }

        // New enhanced export page
        public async Task<IActionResult> Export()
        {
            var viewModel = new EnhancedReportsViewModel();

            // Load customers for filter dropdown
            viewModel.Customers = await _context.Customers
                .OrderBy(c => c.Name)
                .ToListAsync();

            // Set consistent available statuses
            viewModel.AvailableStatuses = new List<string> { "Pending", "Processing", "Completed", "Delivered", "Cancelled" };

            // Get unique product types
            viewModel.ProductTypes = await _context.OrderDetails
                .Select(od => od.BoxType)
                .Distinct()
                .Where(bt => !string.IsNullOrEmpty(bt))
                .OrderBy(bt => bt)
                .ToListAsync();

            // Get unique categories
            viewModel.Categories = await _context.Inventories
                .Select(i => i.Category)
                .Where(c => !string.IsNullOrEmpty(c))
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            // Set default filter values
            viewModel.Filter.DateFrom = DateTime.Now.AddMonths(-1);
            viewModel.Filter.DateTo = DateTime.Now;
            viewModel.Filter.QuickDateRange = "thismonth";

            // Get quick stats for current period
            var currentMonth = DateTime.UtcNow.Month;
            var currentYear = DateTime.UtcNow.Year;
            
            viewModel.CurrentPeriodRevenue = await _context.Orders
                .Where(o => o.Status == "Completed" && o.Date.Month == currentMonth && o.Date.Year == currentYear)
                .SumAsync(o => o.TotalAmount);
                
            viewModel.CurrentPeriodOrders = await _context.Orders
                .CountAsync(o => o.Date.Month == currentMonth && o.Date.Year == currentYear);
                
            viewModel.NewCustomers = await _context.Customers
                .CountAsync(c => c.Orders.Any(o => o.Date.Month == currentMonth && o.Date.Year == currentYear));
                
            viewModel.LowStockItems = await _context.Inventories
                .CountAsync(i => i.AvailableQuantity <= i.ReorderLevel);

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> ExportData([FromBody] ExportFilterViewModel filter)
        {
            try
            {
                // Validate export format - only allow PDF
                if (string.IsNullOrEmpty(filter.ExportFormat) || filter.ExportFormat.ToLower() != "pdf")
                {
                    return BadRequest(new { success = false, message = "Invalid export format. Only PDF is supported." });
                }

                var request = new ReportExportRequest
                {
                    ReportType = filter.ExportType,
                    ExportFormat = "pdf", // Force PDF only
                    StartDate = GetDateFromFilter(filter),
                    EndDate = GetDateToFilter(filter),
                    SelectedColumns = filter.SelectedColumns,
                    IncludeSummary = filter.IncludeSummary
                };

                // Apply filters properly
                if (filter.OrderStatus?.Length > 0)
                    request.Filters["status"] = filter.OrderStatus;

                if (filter.CustomerId.HasValue)
                    request.Filters["customerId"] = filter.CustomerId.Value;

                if (!string.IsNullOrEmpty(filter.ProductType))
                    request.Filters["productType"] = filter.ProductType;

                if (!string.IsNullOrEmpty(filter.Category))
                    request.Filters["category"] = filter.Category;

                if (!string.IsNullOrEmpty(filter.StockStatus))
                    request.Filters["stockStatus"] = filter.StockStatus;

                if (filter.MinValue.HasValue)
                    request.Filters["minValue"] = filter.MinValue.Value;

                byte[] fileData;
                string fileName;
                string contentType = "application/pdf";
                string fileExtension = ".pdf";
                ReportSummary? summary = null;

                // Generate summary if requested
                if (filter.IncludeSummary)
                {
                    summary = await _reportService.GenerateSummaryAsync(filter.ExportType, request, 0);
                }

                switch (filter.ExportType.ToLower())
                {
                    case "orders":
                        var orderData = await _reportService.GetOrderReportDataAsync(request);
                        if (summary != null) summary.TotalRecords = orderData.Count;
                        fileData = await _reportService.ExportToPdfAsync(orderData, request, summary);
                        fileName = $"Orders_Report_{DateTime.Now:yyyyMMdd_HHmmss}{fileExtension}";
                        break;
                    case "customers":
                        var customerData = await _reportService.GetCustomerReportDataAsync(request);
                        if (summary != null) summary.TotalRecords = customerData.Count;
                        fileData = await _reportService.ExportToPdfAsync(customerData, request, summary);
                        fileName = $"Customers_Report_{DateTime.Now:yyyyMMdd_HHmmss}{fileExtension}";
                        break;
                    case "inventory":
                        var inventoryData = await _reportService.GetInventoryReportDataAsync(request);
                        if (summary != null) summary.TotalRecords = inventoryData.Count;
                        fileData = await _reportService.ExportToPdfAsync(inventoryData, request, summary);
                        fileName = $"Inventory_Report_{DateTime.Now:yyyyMMdd_HHmmss}{fileExtension}";
                        break;
                    default:
                        return BadRequest(new { success = false, message = "Invalid report type." });
                }

                return File(fileData, contentType, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting data: {@Filter}", filter);
                return StatusCode(500, new { success = false, message = "Export failed: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetReportPreview([FromQuery] string exportType, [FromQuery] string? dateFrom, [FromQuery] string? dateTo, [FromQuery] string? status, [FromQuery] int? customerId, [FromQuery] string? category, [FromQuery] string? stockStatus)
        {
            try
            {
                var request = new ReportExportRequest
                {
                    ReportType = exportType,
                    StartDate = !string.IsNullOrEmpty(dateFrom) ? DateTime.Parse(dateFrom) : null,
                    EndDate = !string.IsNullOrEmpty(dateTo) ? DateTime.Parse(dateTo) : null
                };

                // Apply filters based on report type
                if (!string.IsNullOrEmpty(status))
                    request.Filters["status"] = status.Split(',');

                if (customerId.HasValue)
                    request.Filters["customerId"] = customerId.Value;

                if (!string.IsNullOrEmpty(category))
                    request.Filters["category"] = category;

                if (!string.IsNullOrEmpty(stockStatus))
                    request.Filters["stockStatus"] = stockStatus;

                object data;
                switch (exportType.ToLower())
                {
                    case "orders":
                        data = (await _reportService.GetOrderReportDataAsync(request)).Take(10);
                        break;
                    case "customers":
                        data = (await _reportService.GetCustomerReportDataAsync(request)).Take(10);
                        break;
                    case "inventory":
                        data = (await _reportService.GetInventoryReportDataAsync(request)).Take(10);
                        break;
                    default:
                        return BadRequest("Invalid export type");
                }

                return Json(data);
            }
            catch (Exception ex)
            {
                return BadRequest($"Preview failed: {ex.Message}");
            }
        }

        // Helper method to get default columns for each report type
        private List<string> GetDefaultColumnsForReportType(string reportType)
        {
            return reportType.ToLower() switch
            {
                "orders" => new List<string> { "OrderId", "CustomerName", "OrderDate", "Status", "TotalAmount" },
                "customers" => new List<string> { "CustomerId", "Name", "Email", "Contact", "TotalOrders", "TotalProducts", "TotalSpent"},
                "inventory" => new List<string> { "ItemId", "ItemName", "Category", "AvailableQuantity", "Unit", "UnitPrice" },
                _ => new List<string>()
            };
        }

        // API: Get chart data for reports
        [HttpGet]
        public async Task<IActionResult> GetChartData(string chartType)
        {
            try
            {
                var result = chartType.ToLower() switch
                {
                    "monthlyrevenue" => await GetMonthlyRevenueChartData(),
                    "orderstatus" => await GetOrderStatusChartData(),
                    "customergrowth" => await GetCustomerGrowthChartData(),
                    "inventoryvalue" => await GetInventoryValueChartData(),
                    "topcustomers" => await GetTopCustomersChartData(),
                    "productperformance" => await GetProductPerformanceChartData(),
                    _ => null
                };

                if (result == null)
                    return BadRequest("Invalid chart type");

                return Json(result);
            }
            catch (Exception ex)
            {
                return BadRequest($"Failed to get chart data: {ex.Message}");
            }
        }

        private async Task<object> GetMonthlyRevenueChartData()
        {
            var result = new List<object>();
            var now = DateTime.UtcNow;
            
            for (int i = 11; i >= 0; i--)
            {
                var monthStart = new DateTime(now.Year, now.Month, 1).AddMonths(-i);
                var monthEnd = monthStart.AddMonths(1);

                var revenue = await _context.Orders
                    .Where(o => o.Status == "Completed" && o.Date >= monthStart && o.Date < monthEnd)
                    .SumAsync(o => o.TotalAmount);

                var orderCount = await _context.Orders
                    .CountAsync(o => o.Date >= monthStart && o.Date < monthEnd);

                result.Add(new
                {
                    month = monthStart.ToString("MMM yyyy"),
                    revenue = revenue,
                    orderCount = orderCount
                });
            }

            return new { data = result };
        }

        private async Task<object> GetOrderStatusChartData()
        {
            var statusCounts = await _context.Orders
                .GroupBy(o => o.Status)
                .Select(g => new { status = g.Key, count = g.Count() })
                .ToListAsync();

            return new
            {
                labels = statusCounts.Select(s => s.status).ToArray(),
                data = statusCounts.Select(s => s.count).ToArray()
            };
        }

        private async Task<object> GetCustomerGrowthChartData()
        {
            var result = new List<object>();
            var now = DateTime.UtcNow;
            
            for (int i = 11; i >= 0; i--)
            {
                var monthStart = new DateTime(now.Year, now.Month, 1).AddMonths(-i);
                var monthEnd = monthStart.AddMonths(1);

                var newCustomers = await _context.Customers
                    .CountAsync(c => c.Orders.Any(o => o.Date >= monthStart && o.Date < monthEnd));

                result.Add(new
                {
                    month = monthStart.ToString("MMM yyyy"),
                    newCustomers = newCustomers
                });
            }

            return new { data = result };
        }

        private async Task<object> GetInventoryValueChartData()
        {
            var categories = await _context.Inventories
                .Where(i => !string.IsNullOrEmpty(i.Category))
                .GroupBy(i => i.Category)
                .Select(g => new
                {
                    category = g.Key,
                    value = g.Sum(i => i.AvailableQuantity * i.UnitPrice),
                    count = g.Count()
                })
                .ToListAsync();

            return new
            {
                labels = categories.Select(c => c.category).ToArray(),
                data = categories.Select(c => c.value).ToArray(),
                counts = categories.Select(c => c.count).ToArray()
            };
        }

        private async Task<object> GetTopCustomersChartData()
        {
            var topCustomers = await _context.Customers
                .Include(c => c.Orders)
                .ToListAsync();

            var customerData = topCustomers
                .Select(c => new
                {
                    name = c.Name,
                    totalSpent = c.Orders.Where(o => o.Status == "Completed").Sum(o => o.TotalAmount),
                    orderCount = c.Orders.Count
                })
                .OrderByDescending(c => c.totalSpent)
                .Take(10)
                .ToList();

            return new
            {
                labels = customerData.Select(c => c.name).ToArray(),
                data = customerData.Select(c => c.totalSpent).ToArray(),
                orderCounts = customerData.Select(c => c.orderCount).ToArray()
            };
        }

        private async Task<object> GetProductPerformanceChartData()
        {
            var productData = await _context.OrderDetails
                .Include(od => od.Order)
                .Where(od => od.Order.Status == "Completed")
                .GroupBy(od => od.BoxType)
                .Select(g => new
                {
                    product = g.Key,
                    totalQuantity = g.Sum(od => od.Quantity),
                    totalRevenue = g.Sum(od => od.Quantity * od.PricePerBox),
                    orderCount = g.Count()
                })
                .OrderByDescending(p => p.totalRevenue)
                .Take(10)
                .ToListAsync();

            return new
            {
                labels = productData.Select(p => p.product).ToArray(),
                revenue = productData.Select(p => p.totalRevenue).ToArray(),
                quantity = productData.Select(p => p.totalQuantity).ToArray(),
                orders = productData.Select(p => p.orderCount).ToArray()
            };
        }

        private DateTime? GetDateFromFilter(ExportFilterViewModel filter)
        {
            return filter.QuickDateRange switch
            {
                "thismonth" => new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                "lastmonth" => new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(-1),
                "thisquarter" => GetQuarterStart(DateTime.Now),
                "custom" => filter.DateFrom,
                _ => filter.DateFrom
            };
        }

        private DateTime? GetDateToFilter(ExportFilterViewModel filter)
        {
            return filter.QuickDateRange switch
            {
                "thismonth" => new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(1).AddDays(-1),
                "lastmonth" => new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddDays(-1),
                "thisquarter" => GetQuarterEnd(DateTime.Now),
                "custom" => filter.DateTo,
                _ => filter.DateTo
            };
        }

        private DateTime GetQuarterStart(DateTime date)
        {
            var quarterNumber = (date.Month - 1) / 3 + 1;
            return new DateTime(date.Year, (quarterNumber - 1) * 3 + 1, 1);
        }

        private DateTime GetQuarterEnd(DateTime date)
        {
            var quarterNumber = (date.Month - 1) / 3 + 1;
            return new DateTime(date.Year, quarterNumber * 3, 1).AddMonths(1).AddDays(-1);
        }

        private void SetAvailableColumns(ExportFilterViewModel filter)
        {
            filter.AvailableColumns = filter.ExportType.ToLower() switch
            {
                "orders" => new Dictionary<string, string>
                {
                    { "OrderId", "Order ID" },
                    { "CustomerName", "Customer Name" },
                    { "OrderDate", "Order Date" },
                    { "Status", "Status" },
                    { "TotalAmount", "Total Amount" },
                    { "ItemCount", "Item Count" },
                    { "ProductSummary", "Product Summary" },
                    { "CustomerEmail", "Customer Email" },
                    { "CustomerContact", "Customer Contact" }
                },
                "customers" => new Dictionary<string, string>
                {
                    { "CustomerId", "Customer ID" },
                    { "Name", "Name" },
                    { "Email", "Email" },
                    { "Contact", "Contact" },
                    { "Address", "Address" },
                    { "TotalOrders", "Total Orders" },
                    { "TotalSpent", "Total Spent" },
                    { "LastOrderDate", "Last Order Date" },
                    { "Status", "Status" }
                },
                "inventory" => new Dictionary<string, string>
                {
                    { "ItemId", "Item ID" },
                    { "ItemName", "Item Name" },
                    { "Category", "Category" },
                    { "AvailableQuantity", "Available Quantity" },
                    { "Unit", "Unit" },
                    { "UnitPrice", "Unit Price" },
                    { "TotalValue", "Total Value" },
                    { "ReorderLevel", "Reorder Level" },
                    { "StockStatus", "Stock Status" }
                },
                _ => new Dictionary<string, string>()
            };

            // Set default selected columns
            filter.SelectedColumns = filter.AvailableColumns.Keys.ToList();
        }

        private async Task<List<MonthlyRevenueData>> GetMonthlyRevenueData()
        {
            var result = new List<MonthlyRevenueData>();
            var now = DateTime.UtcNow;
            
            // Get the last 12 months
            for (int i = 11; i >= 0; i--)
            {
                var monthStart = new DateTime(now.Year, now.Month, 1).AddMonths(-i);
                var monthEnd = monthStart.AddMonths(1);

                var revenue = await _context.Orders
                    .Where(o => o.Status == "Completed" && o.Date >= monthStart && o.Date < monthEnd)
                    .SumAsync(o => o.TotalAmount);

                result.Add(new MonthlyRevenueData
                {
                    Month = monthStart.ToString("MMM yyyy"),
                    Revenue = revenue
                });
            }

            return result;
        }
    }
}