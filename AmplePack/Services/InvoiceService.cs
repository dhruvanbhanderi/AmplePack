using AmplePack.Models;
using AmplePack.Helpers;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AmplePack.Services
{
    public class InvoiceService
    {
        public byte[] GenerateInvoicePdf(Order order)
        {
            try
            {
                // Professional QuestPDF template implementation
                return Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        // Standard A4 professional layout
                        page.Size(PageSizes.A4);
                        page.Margin(20, Unit.Millimetre);
                        page.PageColor(Colors.White);
                        page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Calibri"));

                        // Professional Header Section
                        page.Header().Element(container => ComposeHeader(container, order));

                        // Main Content using QuestPDF table layout
                        page.Content().Element(container => ComposeContent(container, order));

                        // Professional Footer
                        page.Footer().Element(ComposeFooter);
                    });
                }).GeneratePdf();
            }
            catch (Exception ex)
            {
                return CreateSimpleInvoice(order, ex.Message);
            }
        }

        // Professional Header Component
        void ComposeHeader(IContainer container, Order order)
        {
            container.Background(Colors.Blue.Lighten4).Padding(15).Row(row =>
            {
                // Company Info Section
                row.RelativeItem().Column(column =>
                {
                    column.Item().Text("AMPLE PACKAGING")
                        .FontSize(20)
                        .SemiBold()
                        .FontColor(Colors.Blue.Darken3);

                    column.Item().Text("Professional Packaging Solutions")
                        .FontSize(11)
                        .FontColor(Colors.Blue.Darken1);

                    column.Item().PaddingTop(5).Text("Industrial Area, Rajkot - 360001, Gujarat")
                        .FontSize(9)
                        .FontColor(Colors.Grey.Darken2);

                    column.Item().Text("Phone: +91 98765 43210 | Email: info@amplepackaging.com")
                        .FontSize(9)
                        .FontColor(Colors.Grey.Darken2);
                });

                // Invoice Info Box
                row.ConstantItem(140).Background(Colors.White).Padding(10).Column(column =>
                {
                    column.Item().Text("INVOICE")
                        .FontSize(14)
                        .SemiBold()
                        .FontColor(Colors.Blue.Darken3);

                    column.Item().PaddingTop(3).Text($"#{order.Id:D4}")
                        .FontSize(12)
                        .SemiBold();

                    column.Item().Text($"Date: {order.Date:dd-MMM-yyyy}")
                        .FontSize(9);

                    column.Item().Text($"Status: {SafeText(order.Status)}")
                        .FontSize(9)
                        .FontColor(GetStatusColor(order.Status));
                });
            });
        }

        // Main Content with Professional Layout
        void ComposeContent(IContainer container, Order order)
        {
            container.PaddingTop(20).Column(column =>
            {
                // Bill To Section with Box Layout
                column.Item().Element(container => ComposeBillToSection(container, order));

                // Spacer
                column.Item().PaddingTop(20);

                // Order Items Table
                column.Item().Element(container => ComposeOrderItemsTable(container, order));

                // Spacer
                column.Item().PaddingTop(15);

                // Totals Section
                column.Item().Element(container => ComposeTotalsSection(container, order));
            });
        }

        // Bill To Information Box
        void ComposeBillToSection(IContainer container, Order order)
        {
            container.Background(Colors.Grey.Lighten5).Padding(15).Row(row =>
            {
                row.RelativeItem().Column(column =>
                {
                    column.Item().Text("BILL TO")
                        .FontSize(11)
                        .SemiBold()
                        .FontColor(Colors.Blue.Darken2);

                    if (order.Customer != null)
                    {
                        column.Item().PaddingTop(5).Text(SafeText(order.Customer.Name))
                            .FontSize(12)
                            .SemiBold();

                        if (!string.IsNullOrEmpty(order.Customer.Address))
                        {
                            column.Item().Text(SafeText(order.Customer.Address))
                                .FontSize(10)
                                .FontColor(Colors.Grey.Darken2);
                        }

                        if (!string.IsNullOrEmpty(order.Customer.Contact))
                        {
                            column.Item().Text($"Phone: {SafeText(order.Customer.Contact)}")
                                .FontSize(10)
                                .FontColor(Colors.Grey.Darken2);
                        }

                        if (!string.IsNullOrEmpty(order.Customer.Email))
                        {
                            column.Item().Text($"Email: {SafeText(order.Customer.Email)}")
                                .FontSize(10)
                                .FontColor(Colors.Grey.Darken2);
                        }
                    }
                });
            });
        }

        // Professional Order Items Table
        void ComposeOrderItemsTable(IContainer container, Order order)
        {
            container.Column(column =>
            {
                column.Item().Text("ORDER DETAILS")
                    .FontSize(12)
                    .SemiBold()
                    .FontColor(Colors.Blue.Darken2);

                if (order.OrderDetails?.Any() == true)
                {
                    // Professional table with borders
                    column.Item().PaddingTop(10).Table(table =>
                    {
                        // Define columns with proper spacing
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(4);    // Product
                            columns.RelativeColumn(2);    // Size
                            columns.RelativeColumn(1);    // Qty
                            columns.RelativeColumn(1.5f); // Price
                            columns.RelativeColumn(1.5f); // Total
                        });

                        // Professional table header
                        table.Header(header =>
                        {
                            header.Cell().Element(HeaderStyle).Text("Product Name");
                            header.Cell().Element(HeaderStyle).Text("Size");
                            header.Cell().Element(HeaderStyle).AlignCenter().Text("Qty");
                            header.Cell().Element(HeaderStyle).AlignRight().Text("Price");
                            header.Cell().Element(HeaderStyle).AlignRight().Text("Total");
                        });

                        // Data rows - limit to 10 items for clean layout
                        var displayItems = order.OrderDetails.Take(10).ToList();
                        
                        foreach (var detail in displayItems)
                        {
                            table.Cell().Element(CellStyle).Text(SafeText(detail.BoxType));
                            table.Cell().Element(CellStyle).Text(SafeText(detail.Size));
                            table.Cell().Element(CellStyle).AlignCenter().Text(detail.Quantity.ToString());
                            table.Cell().Element(CellStyle).AlignRight().Text($"Rs. {detail.PricePerBox:N2}");
                            table.Cell().Element(CellStyle).AlignRight().Text($"Rs. {detail.Quantity * detail.PricePerBox:N2}");
                        }
                    });

                    // Note for truncated items
                    if (order.OrderDetails.Count > 10)
                    {
                        column.Item().PaddingTop(5)
                            .Background(Colors.Blue.Lighten5)
                            .Padding(8)
                            .Text($"Note: Showing first 10 items of {order.OrderDetails.Count} total items for optimal layout.")
                            .FontSize(8)
                            .FontColor(Colors.Blue.Darken1);
                    }
                }
                else
                {
                    column.Item().PaddingTop(10)
                        .Background(Colors.Grey.Lighten4)
                        .Padding(15)
                        .AlignCenter()
                        .Text("No items found in this order")
                        .FontSize(11)
                        .FontColor(Colors.Grey.Darken2);
                }
            });
        }

        // Professional Totals Section
        void ComposeTotalsSection(IContainer container, Order order)
        {
            var subtotal = order.OrderDetails?.Sum(d => d.Quantity * d.PricePerBox) ?? 0;
            var gstRate = 0.18m;
            var gstAmount = subtotal * gstRate;
            var grandTotal = subtotal + gstAmount;

            container.AlignRight().Column(column =>
            {
                column.Item().Background(Colors.Grey.Lighten4).Padding(12).Column(totalsColumn =>
                {
                    // Subtotal row
                    totalsColumn.Item().Row(row =>
                    {
                        row.ConstantItem(80).Text("Subtotal:").FontSize(10);
                        row.ConstantItem(80).AlignRight().Text($"Rs. {subtotal:N2}").FontSize(10);
                    });

                    // GST row
                    totalsColumn.Item().PaddingTop(3).Row(row =>
                    {
                        row.ConstantItem(80).Text("GST (18%):").FontSize(10);
                        row.ConstantItem(80).AlignRight().Text($"Rs. {gstAmount:N2}").FontSize(10);
                    });

                    // Separator line
                    totalsColumn.Item().PaddingTop(5).BorderTop(1).BorderColor(Colors.Grey.Medium);

                    // Grand total row
                    totalsColumn.Item().PaddingTop(5).Row(row =>
                    {
                        row.ConstantItem(80).Text("Grand Total:")
                            .FontSize(12)
                            .SemiBold()
                            .FontColor(Colors.Blue.Darken2);
                        
                        row.ConstantItem(80).AlignRight().Text($"Rs. {grandTotal:N2}")
                            .FontSize(12)
                            .SemiBold()
                            .FontColor(Colors.Blue.Darken2);
                    });
                });
            });
        }

        // Professional Footer
        void ComposeFooter(IContainer container)
        {
            container.Background(Colors.Blue.Lighten4).Padding(10).Row(row =>
            {
                row.RelativeItem().Column(column =>
                {
                    column.Item().Text("Thank you for choosing Ample Packaging!")
                        .FontSize(11)
                        .SemiBold()
                        .FontColor(Colors.Blue.Darken2);

                    column.Item().Text("Your trusted partner for quality packaging solutions")
                        .FontSize(9)
                        .FontColor(Colors.Grey.Darken2);
                });

                row.ConstantItem(120).AlignRight().Text($"Generated: {DateTime.Now:dd-MMM-yyyy HH:mm}")
                    .FontSize(8)
                    .FontColor(Colors.Grey.Darken1);
            });
        }

        // Styling Methods
        static IContainer HeaderStyle(IContainer container)
        {
            return container
                .Background(Colors.Blue.Darken1)
                .Padding(8)
                .DefaultTextStyle(x => x.FontSize(10).SemiBold().FontColor(Colors.White));
        }

        static IContainer CellStyle(IContainer container)
        {
            return container
                .Border(0.5f)
                .BorderColor(Colors.Grey.Lighten1)
                .Padding(6)
                .DefaultTextStyle(x => x.FontSize(9));
        }

        // Helper Methods
        private string SafeText(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "N/A";
            
            return input
                .Replace("?", "")
                .Replace("\"", "'")
                .Replace("\r", " ")
                .Replace("\n", " ")
                .Trim();
        }

        private string GetStatusColor(string status)
        {
            return status?.ToLower() switch
            {
                "completed" => Colors.Green.Darken1,
                "pending" => Colors.Orange.Darken1,
                "processing" => Colors.Blue.Darken1,
                "cancelled" => Colors.Red.Darken1,
                _ => Colors.Grey.Darken1
            };
        }

        // Simple fallback invoice
        private byte[] CreateSimpleInvoice(Order order, string errorMessage)
        {
            try
            {
                return Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(25, Unit.Millimetre);
                        page.PageColor(Colors.White);

                        page.Content().Column(column =>
                        {
                            column.Item().Text("AMPLE PACKAGING")
                                .FontSize(18)
                                .SemiBold()
                                .FontColor(Colors.Blue.Darken2);

                            column.Item().PaddingTop(20).Text("INVOICE")
                                .FontSize(16)
                                .SemiBold();

                            column.Item().PaddingTop(10).Text($"Order ID: #{order.Id}")
                                .FontSize(12);

                            column.Item().Text($"Customer: {SafeText(order.Customer?.Name ?? "Unknown")}")
                                .FontSize(12);

                            column.Item().Text($"Date: {order.Date:dd-MMM-yyyy}")
                                .FontSize(12);

                            column.Item().Text($"Total Amount: Rs. {order.TotalAmount:N2}")
                                .FontSize(14)
                                .SemiBold();

                            column.Item().PaddingTop(30).Text("For detailed invoice, please contact support.")
                                .FontSize(10)
                                .FontColor(Colors.Grey.Darken1);
                        });
                    });
                }).GeneratePdf();
            }
            catch
            {
                return System.Text.Encoding.UTF8.GetBytes("INVOICE_GENERATION_ERROR");
            }
        }
    }
}