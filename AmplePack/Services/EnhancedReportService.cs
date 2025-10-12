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
        }

        #region Data Retrieval Methods

        public async Task<List<OrderReportData>> GetOrderReportDataAsync(ReportExportRequest request)
        {
            var query = _context.Orders
                .AsSplitQuery()
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

            var orders = await query.OrderByDescending(o => o.Date).ThenByDescending(o => o.Id).ToListAsync();

            return orders.Select(o => new OrderReportData
            {
                OrderId = o.Id,
                CustomerName = CleanText(o.Customer?.Name ?? "Unknown"),
                OrderDate = o.Date,
                Status = CleanText(o.Status),
                TotalAmount = o.TotalAmount,
                ItemCount = o.OrderDetails.Count,
                ProductSummary = GetCleanProductSummary(o.OrderDetails),
                CustomerEmail = CleanText(o.Customer?.Email ?? ""),
                CustomerContact = CleanText(o.Customer?.Contact ?? "")
            }).ToList();
        }

        public async Task<List<CustomerReportData>> GetCustomerReportDataAsync(ReportExportRequest request)
        {
            var query = _context.Customers
                .AsSplitQuery()
                .Include(c => c.Orders)
                .Include(c => c.CustomerProducts)
                .AsQueryable();

            var customers = await query.ToListAsync();

            // Process data in memory to avoid SQLite decimal ordering issues
            return customers.Select(c => new CustomerReportData
            {
                CustomerId = c.Id,
                Name = CleanText(c.Name),
                Email = CleanText(c.Email),
                Contact = CleanText(c.Contact),
                Address = CleanText(c.Address),
                TotalOrders = c.Orders.Count,
                TotalProducts = c.CustomerProducts?.Count ?? 0,
                TotalSpent = c.Orders.Where(o => o.Status == "Completed").Sum(o => o.TotalAmount),
                LastOrderDate = c.Orders.OrderByDescending(o => o.Date).FirstOrDefault()?.Date,
                Status = c.Orders.Any(o => o.Date >= DateTime.Now.AddDays(-30)) ? "Active" : "Inactive"
            })
            .OrderByDescending(c => c.TotalSpent) // Order in memory after calculation
            .ToList();
        }

        public async Task<List<InventoryReportData>> GetInventoryReportDataAsync(ReportExportRequest request)
        {
            var query = _context.Inventories.AsQueryable();

            // Apply filters
            if (request.Filters.ContainsKey("category") && request.Filters["category"] != null)
            {
                var category = (string)request.Filters["category"];
                if (!string.IsNullOrEmpty(category))
                    query = query.Where(i => i.Category == category);
            }

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

            var items = await query.OrderBy(i => i.ItemName).ToListAsync();

            var result = items.Select(i => new InventoryReportData
            {
                ItemId = i.Id,
                ItemName = CleanText(i.ItemName),
                Category = CleanText(i.Category ?? "Uncategorized"),
                AvailableQuantity = i.AvailableQuantity,
                Unit = CleanText(i.Unit),
                UnitPrice = i.UnitPrice,
                TotalValue = i.AvailableQuantity * i.UnitPrice,
                ReorderLevel = i.ReorderLevel,
                IsLowStock = i.AvailableQuantity <= i.ReorderLevel,
                StockStatus = GetCleanStockStatus(i.AvailableQuantity, i.ReorderLevel)
            }).ToList();

            // Apply minimum value filter
            if (request.Filters.ContainsKey("minValue") && request.Filters["minValue"] != null)
            {
                var minValue = (decimal)request.Filters["minValue"];
                result = result.Where(i => i.TotalValue >= minValue).ToList();
            }

            return result;
        }

        #endregion

        #region Export Methods

        public Task<byte[]> ExportToPdfAsync<T>(List<T> data, ReportExportRequest request, ReportSummary? summary = null)
        {
            try
            {
                // Professional invoice-style PDF with limited data for optimal layout
                var limitedData = data.Take(12).ToList(); 
                var istTime = GetISTTime();

                var pdfBytes = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        // Compact A4 layout with optimized margins
                        page.Size(PageSizes.A4);
                        page.Margin(10, Unit.Millimetre);
                        page.PageColor(Colors.White);
                        page.DefaultTextStyle(x => x.FontSize(6).FontFamily("Arial"));

                        // Professional compact header
                        page.Header().Height(35).Padding(5).Row(row =>
                        {
                            row.RelativeItem().Column(column =>
                            {
                                column.Item().Text("AMPLE PACKAGING")
                                    .FontSize(12).SemiBold().FontColor(Colors.Blue.Darken3);
                                column.Item().Text($"{GetReportTitle(request.ReportType)} Report")
                                    .FontSize(8).FontColor(Colors.Grey.Darken1);
                            });

                            row.ConstantItem(70).AlignRight().Column(column =>
                            {
                                column.Item().Text($"Total: {data.Count}")
                                    .FontSize(7).SemiBold();
                                column.Item().Text(istTime.ToString("dd/MM/yy"))
                                    .FontSize(6).FontColor(Colors.Grey.Medium);
                            });
                        });

                        page.Content().PaddingVertical(6).Column(column =>
                        {
                            if (limitedData.Any())
                            {
                                var properties = typeof(T).GetProperties();
                                var selectedProps = GetInvoiceColumns(request.ReportType, properties);

                                column.Item().Table(table =>
                                {
                                    // Optimized column definitions for invoice style
                                    table.ColumnsDefinition(columns =>
                                    {
                                        foreach (var prop in selectedProps)
                                        {
                                            var width = GetInvoiceColumnWidth(prop.Name);
                                            columns.ConstantColumn(width);
                                        }
                                    });

                                    // Professional compact header
                                    table.Header(header =>
                                    {
                                        foreach (var prop in selectedProps)
                                        {
                                            header.Cell().Element(InvoiceHeaderStyle)
                                                .Text(GetInvoiceDisplayName(prop.Name));
                                        }
                                    });

                                    // Data rows with alternating colors for readability
                                    for (int i = 0; i < limitedData.Count; i++)
                                    {
                                        var item = limitedData[i];
                                        var isOdd = i % 2 == 1;

                                        foreach (var prop in selectedProps)
                                        {
                                            var value = prop.GetValue(item);
                                            var displayValue = FormatValueForInvoice(value, prop.Name);
                                            var cellStyle = GetInvoiceCellStyle(prop.Name, isOdd);
                                            table.Cell().Element(cellStyle).Text(displayValue);
                                        }
                                    }
                                });

                                // Data limitation notice (compact)
                                if (data.Count > 12)
                                {
                                    column.Item().PaddingTop(6)
                                        .Background(Colors.Blue.Lighten5)
                                        .Padding(3)
                                        .Text($"Showing first 12 of {data.Count} records. Export to CSV for complete data.")
                                        .FontSize(5)
                                        .FontColor(Colors.Blue.Darken1);
                                }

                                // Professional compact summary
                                if (request.IncludeSummary && summary != null && summary.TotalAmount > 0)
                                {
                                    column.Item().PaddingTop(10)
                                        .Background(Colors.Grey.Lighten4)
                                        .Padding(4)
                                        .Row(row =>
                                        {
                                            row.RelativeItem().Text($"Records: {summary.TotalRecords:N0}")
                                                .FontSize(6).SemiBold();

                                            row.ConstantItem(100).AlignRight()
                                                .Text($"Total: {CurrencyHelper.FormatCurrency(summary.TotalAmount)}")
                                                .FontSize(7).SemiBold().FontColor(Colors.Green.Darken1);
                                        });
                                }
                            }
                            else
                            {
                                column.Item().AlignCenter().PaddingVertical(30)
                                    .Text("No data found")
                                    .FontSize(10).FontColor(Colors.Grey.Darken1);
                            }
                        });

                        // Minimal professional footer
                        page.Footer().Height(12).Padding(2).Row(row =>
                        {
                            row.RelativeItem().Text("AmplePack Report")
                                .FontSize(5).FontColor(Colors.Grey.Medium);

                            row.ConstantItem(40).AlignRight().Text(x =>
                            {
                                x.Span("Page ");
                                x.CurrentPageNumber();
                            });
                        });
                    });
                }).GeneratePdf();

                return Task.FromResult(pdfBytes);
            }
            catch (Exception ex)
            {
                var errorPdf = CreateErrorPdf(ex.Message, request.ReportType);
                return Task.FromResult(errorPdf);
            }
        }

        public Task<byte[]> ExportToCsvAsync<T>(List<T> data, ReportExportRequest request, ReportSummary? summary = null)
        {
            try
            {
                var csv = new StringBuilder();
                var istTime = GetISTTime();
                
                // Professional CSV header
                csv.AppendLine("# AMPLE PACKAGING REPORT");
                csv.AppendLine($"# Type: {GetReportTitle(request.ReportType)}");
                csv.AppendLine($"# Generated: {istTime:dd-MMM-yyyy HH:mm} IST");
                csv.AppendLine($"# Period: {GetPeriodDescription(request.StartDate, request.EndDate)}");
                csv.AppendLine($"# Records: {data.Count:N0}");
                csv.AppendLine("#");

                if (data.Any())
                {
                    var properties = typeof(T).GetProperties();
                    var selectedProps = request.SelectedColumns.Any() 
                        ? properties.Where(p => request.SelectedColumns.Contains(p.Name)).ToArray()
                        : properties;

                    // Professional CSV Headers
                    var headers = selectedProps.Select(p => GetInvoiceDisplayName(p.Name));
                    csv.AppendLine(string.Join(",", headers));

                    // Professional CSV Data
                    foreach (var item in data)
                    {
                        var values = selectedProps.Select(prop => 
                        {
                            var value = prop.GetValue(item);
                            return FormatValueForCsv(value, prop.Name);
                        });
                        csv.AppendLine(string.Join(",", values));
                    }

                    // Professional summary section
                    if (request.IncludeSummary && summary != null)
                    {
                        csv.AppendLine("#");
                        csv.AppendLine("# SUMMARY");
                        csv.AppendLine($"Total Records,{summary.TotalRecords:N0}");
                        
                        if (summary.TotalAmount > 0)
                        {
                            csv.AppendLine($"Total Value,{CurrencyHelper.FormatCurrency(summary.TotalAmount)}");
                        }

                        foreach (var metric in summary.AdditionalMetrics.Take(3))
                        {
                            csv.AppendLine($"{GetCleanMetricName(metric.Key)},{FormatMetricValue(metric.Value)}");
                        }
                    }
                }
                else
                {
                    csv.AppendLine("Status,No records found");
                }

                csv.AppendLine("#");
                csv.AppendLine($"Generated,{istTime:yyyy-MM-dd HH:mm:ss} IST");

                return Task.FromResult(Encoding.UTF8.GetBytes(csv.ToString()));
            }
            catch (Exception ex)
            {
                var errorCsv = new StringBuilder();
                errorCsv.AppendLine("# EXPORT ERROR");
                errorCsv.AppendLine($"Status,FAILED");
                errorCsv.AppendLine($"Error,{CleanTextForCsv(ex.Message)}");
                errorCsv.AppendLine($"Time,{DateTime.Now:yyyy-MM-dd HH:mm:ss} IST");
                
                return Task.FromResult(Encoding.UTF8.GetBytes(errorCsv.ToString()));
            }
        }

        #endregion

        #region PDF Styling Methods

        // Invoice-style column selection (max 4 columns for optimal fit)
        private System.Reflection.PropertyInfo[] GetInvoiceColumns(string reportType, System.Reflection.PropertyInfo[] allProperties)
        {
            var invoiceColumns = reportType.ToLower() switch
            {
                "orders" => new[] { "OrderId", "CustomerName", "OrderDate", "TotalAmount" },
                "customers" => new[] { "Name", "Contact", "TotalOrders", "TotalSpent" },
                "inventory" => new[] { "ItemName", "AvailableQuantity", "UnitPrice", "StockStatus" },
                _ => allProperties.Take(4).Select(p => p.Name).ToArray()
            };

            return allProperties.Where(p => invoiceColumns.Contains(p.Name)).Take(4).ToArray();
        }

        // Optimized column widths for invoice layout
        private int GetInvoiceColumnWidth(string propertyName)
        {
            return propertyName switch
            {
                "OrderId" or "CustomerId" or "ItemId" => 30,
                "OrderDate" or "LastOrderDate" => 40,
                "TotalAmount" or "UnitPrice" or "TotalSpent" => 45,
                "CustomerName" or "ItemName" or "Name" => 75,
                "Contact" or "Email" => 55,
                "AvailableQuantity" or "TotalOrders" => 30,
                "StockStatus" or "Status" => 35,
                _ => 45
            };
        }

        // Invoice header style with compact design
        private static IContainer InvoiceHeaderStyle(IContainer container)
        {
            return container
                .Background(Colors.Blue.Darken2)
                .Padding(1)
                .DefaultTextStyle(x => x.FontSize(5).SemiBold().FontColor(Colors.White));
        }

        // Invoice cell style with alternating rows and content-based alignment
        private static Func<IContainer, IContainer> GetInvoiceCellStyle(string propertyName, bool isOddRow)
        {
            var backgroundColor = isOddRow ? Colors.Grey.Lighten5 : Colors.White;

            if (propertyName.Contains("Amount") || propertyName.Contains("Price") || propertyName.Contains("Spent"))
            {
                return container => container
                    .Background(backgroundColor)
                    .Padding(1)
                    .AlignRight()
                    .DefaultTextStyle(x => x.FontSize(5).SemiBold().FontColor(Colors.Green.Darken2));
            }
            else if (propertyName.Contains("Status"))
            {
                return container => container
                    .Background(backgroundColor)
                    .Padding(1)
                    .AlignCenter()
                    .DefaultTextStyle(x => x.FontSize(5).SemiBold());
            }
            else
            {
                return container => container
                    .Background(backgroundColor)
                    .Padding(1)
                    .DefaultTextStyle(x => x.FontSize(5));
            }
        }

        // Invoice display names (short and professional)
        private string GetInvoiceDisplayName(string propertyName)
        {
            return propertyName switch
            {
                "OrderId" => "Order",
                "CustomerId" => "Cust",
                "CustomerName" => "Customer",
                "OrderDate" => "Date",
                "TotalAmount" => "Amount",
                "ItemCount" => "Items",
                "CustomerEmail" => "Email",
                "CustomerContact" => "Contact",
                "TotalOrders" => "Orders",
                "TotalSpent" => "Spent",
                "LastOrderDate" => "Last Order",
                "ItemId" => "Item",
                "ItemName" => "Product",
                "AvailableQuantity" => "Stock",
                "UnitPrice" => "Price",
                "TotalValue" => "Value",
                "StockStatus" => "Status",
                "Name" => "Name",
                "Email" => "Email",
                "Contact" => "Contact",
                "Category" => "Category",
                "Status" => "Status",
                _ => propertyName
            };
        }

        // Invoice value formatting (compact and clean)
        private string FormatValueForInvoice(object? value, string propertyName)
        {
            if (value == null) return "";

            try
            {
                // Currency fields with rupee formatting
                if (propertyName.Contains("Amount") || propertyName.Contains("Price") || propertyName.Contains("Value") || propertyName.Contains("Spent"))
                {
                    if (value is decimal decimalValue)
                        return $"Rs.{decimalValue:N0}";
                }

                // Dates with compact format
                if (value is DateTime dateValue)
                    return dateValue.ToString("dd/MM/yy");

                // Boolean with simple display
                if (value is bool boolValue)
                    return boolValue ? "Yes" : "No";

                // Numbers with clean formatting
                if (value is int or long)
                    return value.ToString() ?? "0";

                if (value is double or float)
                    return ((decimal)value).ToString("N0");

                // Text with conservative truncation to prevent overflow
                var text = CleanText(value.ToString() ?? "");
                
                var maxLength = propertyName switch
                {
                    "CustomerName" or "ItemName" or "Name" => 12,
                    "Contact" or "Email" => 10,
                    "Status" or "StockStatus" => 6,
                    "Category" => 6,
                    _ => 8
                };
                
                if (text.Length > maxLength)
                    return text.Substring(0, maxLength - 1) + ".";
                
                return text;
            }
            catch
            {
                return "";
            }
        }

        // Professional error PDF creation
        private byte[] CreateErrorPdf(string errorMessage, string reportType)
        {
            try
            {
                var istTime = GetISTTime();
                
                return Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(20, Unit.Millimetre);
                        page.PageColor(Colors.White);
                        
                        page.Content().Column(column =>
                        {
                            column.Item().Text("AMPLE PACKAGING")
                                .FontSize(14).SemiBold().FontColor(Colors.Blue.Darken2);
                            
                            column.Item().PaddingTop(10).Text("Export Notice")
                                .FontSize(12).SemiBold().FontColor(Colors.Orange.Darken1);
                                
                            column.Item().PaddingTop(8).Text($"Unable to generate {GetReportTitle(reportType)} PDF report.")
                                .FontSize(10);
                                
                            column.Item().PaddingTop(8).Text("Recommendations:")
                                .FontSize(9).SemiBold();
                                
                            column.Item().PaddingTop(4).Text("• Export as CSV for complete data")
                                .FontSize(9);
                                
                            column.Item().Text("• Apply date filters to reduce data size")
                                .FontSize(9);
                                
                            column.Item().PaddingTop(15).Text($"Generated: {istTime:dd-MMM-yyyy HH:mm} IST")
                                .FontSize(7).FontColor(Colors.Grey.Medium);
                        });
                    });
                }).GeneratePdf();
            }
            catch
            {
                return System.Text.Encoding.UTF8.GetBytes("PDF_EXPORT_UNAVAILABLE");
            }
        }

        #endregion

        #region Helper Methods

        // Clean text to remove all problematic characters
        private string CleanText(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "";

            return text
                .Replace("?", "")           // Remove question marks completely
                .Replace("\"", "'")         // Replace quotes with apostrophes
                .Replace("\r", " ")         // Remove carriage returns
                .Replace("\n", " ")         // Remove line feeds
                .Replace("\t", " ")         // Replace tabs with spaces
                .Trim();
        }

        // Get clean product summary without problematic characters
        private string GetCleanProductSummary(ICollection<OrderDetail> orderDetails)
        {
            if (orderDetails == null || !orderDetails.Any())
                return "No items";

            var first = orderDetails.First();
            var boxType = CleanText(first.BoxType);
            var quantity = first.Quantity;

            return orderDetails.Count == 1 
                ? $"{boxType} ({quantity})"
                : $"{boxType} +{orderDetails.Count - 1} more";
        }

        // Get clean stock status
        private string GetCleanStockStatus(decimal available, decimal reorderLevel)
        {
            if (available <= 0) return "Out of Stock";
            if (available <= reorderLevel) return "Low Stock";
            return "In Stock";
        }

        // Format value for CSV with professional standards
        private string FormatValueForCsv(object? value, string propertyName)
        {
            if (value == null) return "";

            try
            {
                // Currency fields with proper formatting
                if (propertyName.Contains("Amount") || propertyName.Contains("Price") || propertyName.Contains("Value") || propertyName.Contains("Spent"))
                {
                    if (value is decimal decimalValue)
                        return CurrencyHelper.FormatCurrency(decimalValue);
                }

                // Dates with readable format
                if (value is DateTime dateValue)
                    return dateValue.ToString("dd-MMM-yyyy");

                // Boolean with clear display
                if (value is bool boolValue)
                    return boolValue ? "YES" : "NO";

                // Numbers with proper formatting
                if (value is int or long or double or float)
                    return value.ToString() ?? "0";

                // Text with CSV cleaning
                return CleanTextForCsv(value.ToString() ?? "");
            }
            catch
            {
                return "";
            }
        }

        // Clean text specifically for CSV output
        private string CleanTextForCsv(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "";

            var cleaned = text
                .Replace("\"", "'")     // Replace quotes with apostrophes
                .Replace(",", ";")      // Replace commas with semicolons
                .Replace("\r", " ")     // Remove carriage returns
                .Replace("\n", " ")     // Remove line feeds
                .Replace("?", "")       // Remove question marks completely
                .Replace("#", "No.")    // Replace hash with professional alternative
                .Trim();

            return cleaned;
        }

        // Get clean metric name for summary
        private string GetCleanMetricName(string key)
        {
            return key switch
            {
                "CompletedOrders" => "Completed Orders",
                "PendingOrders" => "Pending Orders",
                "ActiveCustomers" => "Active Customers",
                "LowStockItems" => "Low Stock Items",
                "TotalItems" => "Total Items",
                "AverageOrderValue" => "Average Order Value",
                _ => CleanTextForCsv(key)
            };
        }

        // Format metric value with proper number formatting
        private string FormatMetricValue(object value)
        {
            return value switch
            {
                decimal d => d.ToString("N2"),
                double d => d.ToString("N2"),
                int i => i.ToString("N0"),
                _ => value?.ToString() ?? ""
            };
        }

        // Get IST time for timestamps
        private DateTime GetISTTime()
        {
            var utcNow = DateTime.UtcNow;
            var istOffset = TimeSpan.FromHours(5.5);
            return utcNow.Add(istOffset);
        }

        // Get professional report title
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

        // Get period description for date ranges
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
    }
}