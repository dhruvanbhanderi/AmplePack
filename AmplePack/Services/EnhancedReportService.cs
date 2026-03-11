using AmplePack.Data;
using AmplePack.Models;
using AmplePack.Helpers;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Text;

namespace AmplePack.Services
{
    public class EnhancedReportService
    {
        private readonly AppDbContext _context;

        public EnhancedReportService(AppDbContext context)
        {
            _context = context;
            // Ensure QuestPDF license is set
            QuestPDF.Settings.License = LicenseType.Community;
        }

        #region Data Retrieval Methods

        public async Task<List<OrderReportData>> GetOrderReportDataAsync(ReportExportRequest request)
        {
            var query = _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderDetails)
                .AsQueryable();

            // Apply date filter
            if (request.StartDate.HasValue)
                query = query.Where(o => o.Date >= request.StartDate.Value);

            if (request.EndDate.HasValue)
                query = query.Where(o => o.Date <= request.EndDate.Value);

            // Apply other filters
            if (request.Filters.ContainsKey("status") && request.Filters["status"] != null)
            {
                var statuses = (string[])request.Filters["status"];
                if (statuses.Length > 0)
                    query = query.Where(o => statuses.Contains(o.Status));
            }

            if (request.Filters.ContainsKey("customerId") && request.Filters["customerId"] != null)
            {
                var customerId = (int)request.Filters["customerId"];
                query = query.Where(o => o.CustomerId == customerId);
            }

            // ? CRITICAL FIX: Add pagination to prevent memory exhaustion
            // Limit to maximum 5000 records per export (can be made configurable)
            const int MAX_EXPORT_RECORDS = 5000;
            
            // Default sort by Date descending
            var orders = await query
                .OrderByDescending(o => o.Date)
                .Take(MAX_EXPORT_RECORDS) // Safety limit
                .ToListAsync();

            return orders.Select(o => new OrderReportData
            {
                OrderId = o.Id,
                CustomerName = o.Customer?.Name ?? "Unknown",
                OrderDate = o.Date,
                Status = o.Status,
                TotalAmount = o.TotalAmount,
                ItemCount = o.OrderDetails.Count,
                ProductSummary = o.OrderDetails.Count switch
                {
                    0 => "No items",
                    1 => $"{o.OrderDetails.First().BoxType} ({o.OrderDetails.First().Quantity})",
                    _ => $"{o.OrderDetails.First().BoxType} +{o.OrderDetails.Count - 1} more"
                },
                CustomerEmail = o.Customer?.Email ?? "",
                CustomerContact = o.Customer?.Contact ?? ""
            }).ToList();
        }

        public async Task<List<CustomerReportData>> GetCustomerReportDataAsync(ReportExportRequest request)
        {
            var query = _context.Customers
                .Include(c => c.Orders)
                .Include(c => c.CustomerProducts) // Include customer products
                .AsQueryable();

            // ? CRITICAL FIX: Add pagination limit
            const int MAX_EXPORT_RECORDS = 5000;
            
            var customers = await query
                .Take(MAX_EXPORT_RECORDS)
                .ToListAsync();

            return customers.Select(c => new CustomerReportData
            {
                CustomerId = c.Id,
                Name = c.Name,
                Email = c.Email,
                Contact = c.Contact,
                Address = c.Address,
                TotalOrders = c.Orders.Count,
                TotalProducts = c.CustomerProducts?.Count ?? 0, // Count customer products
                TotalSpent = c.Orders.Where(o => o.Status == "Completed").Sum(o => o.TotalAmount),
                LastOrderDate = c.Orders.OrderByDescending(o => o.Date).FirstOrDefault()?.Date,
                Status = c.Orders.Any(o => o.Date >= DateTime.Now.AddDays(-30)) ? "Active" : "Inactive"
            }).ToList();
        }

        public async Task<List<InventoryReportData>> GetInventoryReportDataAsync(ReportExportRequest request)
        {
            var query = _context.Inventories.AsQueryable();

            // Apply category filter
            if (request.Filters.ContainsKey("category") && request.Filters["category"] != null)
            {
                var category = (string)request.Filters["category"];
                if (!string.IsNullOrEmpty(category))
                    query = query.Where(i => i.Category == category);
            }

            // Apply stock status filter
            if (request.Filters.ContainsKey("stockStatus") && request.Filters["stockStatus"] != null)
            {
                var stockStatus = (string)request.Filters["stockStatus"];
                if (!string.IsNullOrEmpty(stockStatus))
                {
                    switch (stockStatus.ToLower())
                    {
                        case "instock":
                            query = query.Where(i => i.AvailableQuantity > i.ReorderLevel);
                            break;
                        case "lowstock":
                            query = query.Where(i => i.AvailableQuantity <= i.ReorderLevel && i.AvailableQuantity > 0);
                            break;
                        case "outofstock":
                            query = query.Where(i => i.AvailableQuantity <= 0);
                            break;
                    }
                }
            }

            // Apply minimum value filter
            if (request.Filters.ContainsKey("minValue") && request.Filters["minValue"] != null)
            {
                var minValue = (decimal)request.Filters["minValue"];
                query = query.Where(i => i.AvailableQuantity * i.UnitPrice >= minValue);
            }

            // ? CRITICAL FIX: Add pagination limit
            const int MAX_EXPORT_RECORDS = 5000;
            
            var items = await query
                .OrderBy(i => i.ItemName)
                .Take(MAX_EXPORT_RECORDS)
                .ToListAsync();

            return items.Select(i => new InventoryReportData
            {
                ItemId = i.Id,
                ItemName = i.ItemName,
                Category = i.Category ?? "Uncategorized",
                AvailableQuantity = i.AvailableQuantity,
                Unit = i.Unit,
                UnitPrice = i.UnitPrice,
                TotalValue = i.AvailableQuantity * i.UnitPrice,
                ReorderLevel = i.ReorderLevel,
                IsLowStock = i.AvailableQuantity <= i.ReorderLevel,
                StockStatus = i.AvailableQuantity <= 0 ? "Out of Stock" : 
                           i.AvailableQuantity <= i.ReorderLevel ? "Low Stock" : "In Stock"
            }).ToList();
        }

        #endregion

        #region Export Methods

        public Task<byte[]> ExportToPdfAsync<T>(List<T> data, ReportExportRequest request, ReportSummary? summary = null)
        {
            try
            {
                // Get IST time
                var istTime = GetISTTime();

                var pdfBytes = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4.Landscape());
                        page.Margin(1, Unit.Centimetre);
                        page.PageColor(Colors.White);
                        page.DefaultTextStyle(x => x.FontSize(9).FontFamily(Fonts.Arial));

                        page.Header()
                            .Height(80)
                            .Background(Colors.Grey.Lighten3)
                            .Padding(10)
                            .Row(row =>
                            {
                                row.RelativeItem().Column(column =>
                                {
                                    column.Item().Text("AMPLE PACKAGING")
                                        .FontSize(18)
                                        .SemiBold()
                                        .FontColor(Colors.Blue.Darken2);

                                    column.Item().Text($"{GetReportTitle(request.ReportType)} Report")
                                        .FontSize(12)
                                        .FontColor(Colors.Grey.Darken2);
                                });

                                row.ConstantItem(150).AlignRight().Column(column =>
                                {
                                    column.Item().Text($"Generated: {istTime:dd-MM-yyyy HH:mm} IST")
                                        .FontSize(8);
                                    
                                    if (summary != null)
                                    {
                                        column.Item().Text($"Period: {summary.PeriodDescription}")
                                            .FontSize(8);
                                    }
                                });
                            });

                        page.Content()
                            .PaddingVertical(10)
                            .Column(column =>
                            {
                                if (data.Any())
                                {
                                    var properties = typeof(T).GetProperties();
                                    var selectedProps = request.SelectedColumns.Any() 
                                        ? properties.Where(p => request.SelectedColumns.Contains(p.Name)).Take(8).ToArray()
                                        : properties.Take(8).ToArray(); // Limit columns for PDF

                                    column.Item().Table(table =>
                                    {
                                        table.ColumnsDefinition(columns =>
                                        {
                                            for (int i = 0; i < selectedProps.Length; i++)
                                            {
                                                columns.RelativeColumn();
                                            }
                                        });

                                        table.Header(header =>
                                        {
                                            foreach (var prop in selectedProps)
                                            {
                                                header.Cell().Element(HeaderCellStyle).Text(GetDisplayName(prop.Name));
                                            }

                                            static IContainer HeaderCellStyle(IContainer container)
                                            {
                                                return container
                                                    .Background(Colors.Blue.Lighten4)
                                                    .BorderColor(Colors.Blue.Lighten2)
                                                    .Border(1)
                                                    .Padding(3)
                                                    .DefaultTextStyle(x => x.SemiBold().FontSize(8).FontFamily(Fonts.Arial));
                                            }
                                        });

                                        foreach (var item in data.Take(100)) // Increased limit for better reports
                                        {
                                            foreach (var prop in selectedProps)
                                            {
                                                var value = prop.GetValue(item);
                                                var displayValue = FormatValueForPdf(value, prop.Name);
                                                table.Cell().Element(RowCellStyle).Text(displayValue);
                                            }

                                            static IContainer RowCellStyle(IContainer container)
                                            {
                                                return container
                                                    .BorderColor(Colors.Grey.Lighten2)
                                                    .Border(1)
                                                    .Padding(3)
                                                    .DefaultTextStyle(x => x.FontSize(7).FontFamily(Fonts.Arial));
                                            }
                                        }
                                    });

                                    if (data.Count > 100)
                                    {
                                        column.Item().PaddingTop(10).Text($"Note: Showing first 100 records of {data.Count} total records")
                                            .FontSize(8)
                                            .FontColor(Colors.Grey.Darken1);
                                    }
                                }
                                else
                                {
                                    column.Item().AlignCenter().Text("No data available for the selected criteria")
                                        .FontSize(12)
                                        .FontColor(Colors.Grey.Darken1);
                                }

                                // Add summary
                                if (request.IncludeSummary && summary != null)
                                {
                                    column.Item().PaddingTop(20).Column(summaryColumn =>
                                    {
                                        summaryColumn.Item().Text("SUMMARY")
                                            .FontSize(12)
                                            .SemiBold()
                                            .FontColor(Colors.Blue.Darken2);

                                        summaryColumn.Item().PaddingTop(5).Row(row =>
                                        {
                                            row.ConstantItem(100).Text("Total Records:");
                                            row.ConstantItem(80).Text(summary.TotalRecords.ToString());
                                            row.ConstantItem(100).Text("Total Amount:");
                                            row.RelativeItem().Text(CurrencyHelper.FormatCurrency(summary.TotalAmount));
                                        });
                                    });
                                }
                            });

                        page.Footer()
                            .Height(30)
                            .Background(Colors.Grey.Lighten4)
                            .Padding(10)
                            .AlignCenter()
                            .Text("AmplePack - Generated on " + istTime.ToString("dd-MM-yyyy HH:mm") + " IST")
                            .FontSize(8);
                    });
                }).GeneratePdf();

                // Validate PDF was created properly
                if (pdfBytes == null || pdfBytes.Length == 0)
                {
                    throw new InvalidOperationException("PDF generation failed - empty result");
                }

                return Task.FromResult(pdfBytes);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"PDF generation failed: {ex.Message}", ex);
            }
        }

        #endregion

        #region Summary Generation

        public async Task<ReportSummary> GenerateSummaryAsync(string reportType, ReportExportRequest request, int recordCount)
        {
            var istTime = GetISTTime();
            
            var summary = new ReportSummary
            {
                TotalRecords = recordCount,
                PeriodDescription = GetPeriodDescription(request.StartDate, request.EndDate),
                GeneratedOn = istTime,
                GeneratedBy = "System"
            };

            switch (reportType.ToLower())
            {
                case "orders":
                    var orderData = await GetOrderReportDataAsync(request);
                    summary.TotalAmount = orderData.Sum(o => o.TotalAmount);
                    summary.AdditionalMetrics["CompletedOrders"] = orderData.Count(o => o.Status == "Completed");
                    summary.AdditionalMetrics["PendingOrders"] = orderData.Count(o => o.Status == "Pending");
                    break;

                case "customers":
                    var customerData = await GetCustomerReportDataAsync(request);
                    summary.TotalAmount = customerData.Sum(c => c.TotalSpent);
                    summary.AdditionalMetrics["ActiveCustomers"] = customerData.Count(c => c.Status == "Active");
                    summary.AdditionalMetrics["AverageOrderValue"] = customerData.Any() ? customerData.Average(c => c.TotalSpent) : 0;
                    break;

                case "inventory":
                    var inventoryData = await GetInventoryReportDataAsync(request);
                    summary.TotalAmount = inventoryData.Sum(i => i.TotalValue);
                    summary.AdditionalMetrics["LowStockItems"] = inventoryData.Count(i => i.IsLowStock);
                    summary.AdditionalMetrics["TotalItems"] = inventoryData.Count;
                    break;
            }

            return summary;
        }

        #endregion

        #region Helper Methods

        private DateTime GetISTTime()
        {
            // Convert UTC to IST (UTC + 5:30)
            var utcNow = DateTime.UtcNow;
            var istOffset = TimeSpan.FromHours(5.5);
            return utcNow.Add(istOffset);
        }

        private string GetDisplayName(string propertyName)
        {
            return propertyName switch
            {
                "OrderId" => "Order ID",
                "CustomerId" => "Customer ID",
                "CustomerName" => "Customer Name",
                "OrderDate" => "Order Date",
                "TotalAmount" => "Total Amount",
                "ItemCount" => "Item Count",
                "ProductSummary" => "Product Summary",
                "CustomerEmail" => "Customer Email",
                "CustomerContact" => "Customer Contact",
                "TotalOrders" => "Total Orders",
                "TotalProducts" => "Total Products",
                "TotalSpent" => "Total Spent",
                "LastOrderDate" => "Last Order Date",
                "ItemId" => "Item ID",
                "ItemName" => "Item Name",
                "AvailableQuantity" => "Available Quantity",
                "UnitPrice" => "Unit Price",
                "TotalValue" => "Total Value",
                "ReorderLevel" => "Reorder Level",
                "IsLowStock" => "Low Stock",
                "StockStatus" => "Stock Status",
                _ => propertyName
            };
        }

        private string GetReportTitle(string reportType)
        {
            return reportType.ToLower() switch
            {
                "orders" => "Orders",
                "customers" => "Customers",
                "inventory" => "Inventory",
                _ => "Data"
            };
        }

        private string GetPeriodDescription(DateTime? startDate, DateTime? endDate)
        {
            if (startDate.HasValue && endDate.HasValue)
                return $"{startDate.Value:dd-MM-yyyy} to {endDate.Value:dd-MM-yyyy}";
            else if (startDate.HasValue)
                return $"From {startDate.Value:dd-MM-yyyy}";
            else if (endDate.HasValue)
                return $"Until {endDate.Value:dd-MM-yyyy}";
            else
                return "All Time";
        }

        private string FormatValueForPdf(object? value, string propertyName)
        {
            if (value == null) return "";

            if (propertyName.Contains("Amount") || propertyName.Contains("Price") || propertyName.Contains("Value"))
            {
                if (value is decimal decimalValue)
                    return CurrencyHelper.FormatCurrency(decimalValue);
            }

            if (value is DateTime dateValue)
                return dateValue.ToString("dd-MM-yyyy");

            if (value is bool boolValue)
                return boolValue ? "Yes" : "No";

            // Truncate long text fields for better PDF layout
            if (propertyName == "ProductSummary" || propertyName.Contains("Summary"))
            {
                var text = value.ToString() ?? "";
                return text.Length > 40 ? text.Substring(0, 37) + "..." : text;
            }

            // Truncate other text fields if too long
            if (value is string stringValue && stringValue.Length > 30)
            {
                return stringValue.Substring(0, 27) + "...";
            }

            return value.ToString() ?? "";
        }

        // Helper method to clean text for reports - removes problematic characters
        private string CleanText(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return "";

            return input
                .Replace("?", "")
                .Replace("\"", "'")
                .Replace("\r", " ")
                .Replace("\n", " ")
                .Trim();
        }

        // Helper method to get clean product summary from order details
        private string GetCleanProductSummary(ICollection<OrderDetail>? orderDetails)
        {
            if (orderDetails == null || !orderDetails.Any())
                return "No Items";

            var summary = string.Join(", ", orderDetails
                .Take(3) // Limit to first 3 items
                .Select(od => $"{CleanText(od.BoxType)} ({od.Quantity})"));

            if (orderDetails.Count > 3)
                summary += $" +{orderDetails.Count - 3} more";

            return summary;
        }

        // Helper method to get clean stock status
        private string GetCleanStockStatus(decimal availableQuantity, decimal reorderLevel)
        {
            if (availableQuantity <= 0)
                return "Out of Stock";
            else if (availableQuantity <= reorderLevel)
                return "Low Stock";
            else
                return "In Stock";
        }

        #endregion
    }
}