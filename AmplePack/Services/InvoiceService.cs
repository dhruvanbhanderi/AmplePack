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
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header()
                        .Height(100)
                        .Background(Colors.Grey.Lighten3)
                        .Padding(15)
                        .Row(row =>
                        {
                            row.RelativeItem().Column(column =>
                            {
                                column.Item().Text("AMPLE PACKAGING")
                                    .FontSize(20)
                                    .SemiBold()
                                    .FontColor(Colors.Blue.Darken2);

                                column.Item().Text("Packaging Solutions Expert")
                                    .FontSize(10)
                                    .FontColor(Colors.Grey.Darken2);
                                
                                column.Item().Text("Rajkot, Gujarat, India")
                                    .FontSize(9)
                                    .FontColor(Colors.Grey.Darken1);
                                
                                column.Item().Text("Phone: +91 98765 43210")
                                    .FontSize(9)
                                    .FontColor(Colors.Grey.Darken1);
                                
                                column.Item().Text("Email: info@amplepackaging.com")
                                    .FontSize(9)
                                    .FontColor(Colors.Grey.Darken1);
                            });

                            row.ConstantItem(120).AlignRight().Column(column =>
                            {
                                column.Item().Text("INVOICE")
                                    .FontSize(16)
                                    .SemiBold()
                                    .FontColor(Colors.Blue.Darken2);

                                column.Item().Text($"Order ID: #{order.Id}")
                                    .FontSize(10)
                                    .SemiBold();

                                column.Item().Text($"Date: {order.Date:MMM dd, yyyy}")
                                    .FontSize(9);
                                
                                column.Item().Text($"Status: {order.Status}")
                                    .FontSize(9);
                            });
                        });

                    page.Content()
                        .PaddingVertical(15)
                        .Column(column =>
                        {
                            // Customer Information Section
                            column.Item().Text("BILL TO:")
                                .FontSize(11)
                                .SemiBold()
                                .FontColor(Colors.Blue.Darken2);

                            if (order.Customer != null)
                            {
                                column.Item().PaddingTop(5).Text(order.Customer.Name)
                                    .FontSize(10)
                                    .SemiBold();

                                if (!string.IsNullOrEmpty(order.Customer.Address))
                                {
                                    column.Item().Text(order.Customer.Address)
                                        .FontSize(9)
                                        .FontColor(Colors.Grey.Darken1);
                                }

                                if (!string.IsNullOrEmpty(order.Customer.Contact))
                                {
                                    column.Item().Text($"Phone: {order.Customer.Contact}")
                                        .FontSize(9)
                                        .FontColor(Colors.Grey.Darken1);
                                }

                                if (!string.IsNullOrEmpty(order.Customer.Email))
                                {
                                    column.Item().Text($"Email: {order.Customer.Email}")
                                        .FontSize(9)
                                        .FontColor(Colors.Grey.Darken1);
                                }
                            }

                            column.Item().PaddingTop(15).Text("ORDER DETAILS")
                                .FontSize(11)
                                .SemiBold()
                                .FontColor(Colors.Blue.Darken2);

                            // Order Items Table
                            if (order.OrderDetails?.Any() == true)
                            {
                                column.Item().PaddingTop(10).Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(3); // Product Name
                                        columns.RelativeColumn(2); // Size/Specifications
                                        columns.RelativeColumn(1); // Quantity
                                        columns.RelativeColumn(1.5f); // Unit Price
                                        columns.RelativeColumn(1.5f); // Total
                                    });

                                    table.Header(header =>
                                    {
                                        header.Cell().Element(CellStyle).Text("Product Name");
                                        header.Cell().Element(CellStyle).Text("Size/Specifications");
                                        header.Cell().Element(CellStyle).AlignCenter().Text("Quantity");
                                        header.Cell().Element(CellStyle).AlignRight().Text("Unit Price");
                                        header.Cell().Element(CellStyle).AlignRight().Text("Total");

                                        static IContainer CellStyle(IContainer container)
                                        {
                                            return container
                                                .Background(Colors.Blue.Lighten4)
                                                .BorderColor(Colors.Blue.Lighten2)
                                                .Border(1)
                                                .Padding(5)
                                                .DefaultTextStyle(x => x.SemiBold().FontSize(9));
                                        }
                                    });

                                    foreach (var detail in order.OrderDetails)
                                    {
                                        table.Cell().Element(RowCellStyle).Text(detail.BoxType).FontSize(9);
                                        table.Cell().Element(RowCellStyle).Text(detail.Size).FontSize(9);
                                        table.Cell().Element(RowCellStyle).AlignCenter().Text(detail.Quantity.ToString()).FontSize(9);
                                        table.Cell().Element(RowCellStyle).AlignRight().Text(CurrencyHelper.FormatCurrency(detail.PricePerBox)).FontSize(9);
                                        table.Cell().Element(RowCellStyle).AlignRight().Text(CurrencyHelper.FormatCurrency(detail.Quantity * detail.PricePerBox)).FontSize(9);

                                        static IContainer RowCellStyle(IContainer container)
                                        {
                                            return container
                                                .BorderColor(Colors.Grey.Lighten2)
                                                .Border(1)
                                                .Padding(5);
                                        }
                                    }
                                });
                            }
                            else
                            {
                                column.Item().PaddingTop(10).Text("No items in this order")
                                    .FontSize(10)
                                    .FontColor(Colors.Grey.Darken1);
                            }

                            // Totals Section
                            column.Item().PaddingTop(15).AlignRight().Column(totalsColumn =>
                            {
                                var subtotal = order.OrderDetails?.Sum(d => d.Quantity * d.PricePerBox) ?? 0;
                                
                                totalsColumn.Item().Row(row =>
                                {
                                    row.ConstantItem(80).Text("Subtotal:").FontSize(9);
                                    row.ConstantItem(70).AlignRight().Text(CurrencyHelper.FormatCurrency(subtotal)).FontSize(9);
                                });

                                // Add tax calculation (assuming 18% GST)
                                var taxRate = 0.18m;
                                var taxAmount = subtotal * taxRate;
                                
                                totalsColumn.Item().Row(row =>
                                {
                                    row.ConstantItem(80).Text("GST (18%):").FontSize(9);
                                    row.ConstantItem(70).AlignRight().Text(CurrencyHelper.FormatCurrency(taxAmount)).FontSize(9);
                                });

                                var grandTotal = subtotal + taxAmount;

                                totalsColumn.Item().PaddingTop(5).Row(row =>
                                {
                                    row.ConstantItem(80).Text("Grand Total:")
                                        .FontSize(10)
                                        .SemiBold();
                                    row.ConstantItem(70).AlignRight().Text(CurrencyHelper.FormatCurrency(grandTotal))
                                        .FontSize(10)
                                        .SemiBold()
                                        .FontColor(Colors.Blue.Darken2);
                                });
                            });
                        });

                    page.Footer()
                        .Height(50)
                        .Background(Colors.Grey.Lighten4)
                        .Padding(10)
                        .AlignCenter()
                        .Column(column =>
                        {
                            column.Item().Text("Thank you for your business!")
                                .FontSize(10)
                                .SemiBold()
                                .FontColor(Colors.Blue.Darken2);

                            column.Item().Text("Ample Packaging - Your trusted partner for quality packaging solutions")
                                .FontSize(8)
                                .FontColor(Colors.Grey.Darken1);

                            column.Item().Text("Address: Industrial Area, Rajkot - 360001, Gujarat, India")
                                .FontSize(7)
                                .FontColor(Colors.Grey.Darken1);
                        });
                });
            }).GeneratePdf();
        }
    }
}