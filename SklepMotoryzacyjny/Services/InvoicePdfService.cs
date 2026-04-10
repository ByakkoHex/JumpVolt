using System.IO;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SklepMotoryzacyjny.Models;

namespace SklepMotoryzacyjny.Services
{
    public static class InvoicePdfService
    {
        static InvoicePdfService()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public static byte[] GeneratePdf(Invoice invoice, AppSettings settings)
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);
                    page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(9));

                    page.Header().Element(c => ComposeHeader(c, invoice, settings));
                    page.Content().Element(c => ComposeContent(c, invoice, settings));
                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Strona ").FontSize(8).FontColor(Colors.Grey.Medium);
                        x.CurrentPageNumber().FontSize(8).FontColor(Colors.Grey.Medium);
                        x.Span(" z ").FontSize(8).FontColor(Colors.Grey.Medium);
                        x.TotalPages().FontSize(8).FontColor(Colors.Grey.Medium);
                    });
                });
            }).GeneratePdf();
        }

        private static void ComposeHeader(IContainer container, Invoice invoice, AppSettings settings)
        {
            container.Column(col =>
            {
                col.Item().Row(row =>
                {
                    // Lewa strona — sprzedawca
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text("SPRZEDAWCA").Bold().FontSize(8).FontColor(Colors.Grey.Medium);
                        c.Item().Text(settings.CompanyName).Bold().FontSize(13);
                        if (!string.IsNullOrEmpty(settings.CompanyNIP))
                            c.Item().Text($"NIP: {settings.CompanyNIP}").FontSize(9);
                        if (!string.IsNullOrEmpty(settings.CompanyAddress))
                            c.Item().Text(settings.CompanyAddress).FontSize(9);
                        if (!string.IsNullOrEmpty(settings.CompanyCity))
                            c.Item().Text(settings.CompanyCity).FontSize(9);
                        if (!string.IsNullOrEmpty(settings.CompanyPhone))
                            c.Item().Text($"Tel: {settings.CompanyPhone}").FontSize(9);
                    });

                    // Prawa strona — numer i data faktury
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().AlignRight().Text("FAKTURA VAT").Bold().FontSize(16).FontColor(Color.FromHex("#1565C0"));
                        c.Item().AlignRight().Text(invoice.InvoiceNumber).Bold().FontSize(14);
                        c.Item().AlignRight().Text($"Data wystawienia: {invoice.DateDisplay}").FontSize(9);
                        if (invoice.DueDate.HasValue)
                            c.Item().AlignRight().Text($"Termin płatności: {invoice.DueDateDisplay}").FontSize(9);
                        c.Item().AlignRight().Text($"Metoda płatności: {invoice.PaymentMethod}").FontSize(9);
                    });
                });

                col.Item().PaddingTop(16).Row(row =>
                {
                    row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(c =>
                    {
                        c.Item().Text("NABYWCA").Bold().FontSize(8).FontColor(Colors.Grey.Medium);
                        c.Item().Text(invoice.BuyerName).Bold().FontSize(11);
                        if (!string.IsNullOrEmpty(invoice.BuyerNIP))
                            c.Item().Text($"NIP: {invoice.BuyerNIP}").FontSize(9);
                        if (!string.IsNullOrEmpty(invoice.BuyerAddress))
                            c.Item().Text(invoice.BuyerAddress).FontSize(9);
                        if (!string.IsNullOrEmpty(invoice.BuyerCity))
                            c.Item().Text(invoice.BuyerCity).FontSize(9);
                        if (!string.IsNullOrEmpty(invoice.BuyerEmail))
                            c.Item().Text(invoice.BuyerEmail).FontSize(9);
                    });
                    row.ConstantItem(12);
                    row.RelativeItem();
                });

                col.Item().PaddingTop(4).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
            });
        }

        private static void ComposeContent(IContainer container, Invoice invoice, AppSettings settings)
        {
            container.Column(col =>
            {
                col.Spacing(12);

                // Tabela pozycji
                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.ConstantColumn(25);   // Lp.
                        cols.RelativeColumn(4);    // Nazwa
                        cols.ConstantColumn(35);   // Jedn.
                        cols.ConstantColumn(45);   // Ilość
                        cols.ConstantColumn(65);   // Cena netto
                        cols.ConstantColumn(35);   // VAT
                        cols.ConstantColumn(65);   // Net razem
                        cols.ConstantColumn(60);   // VAT razem
                        cols.ConstantColumn(70);   // Brutto razem
                    });

                    table.Header(header =>
                    {
                        header.Cell().Background(Color.FromHex("#1565C0")).Padding(5).AlignCenter().Text("Lp.").Bold().FontSize(8).FontColor(Colors.White);
                        header.Cell().Background(Color.FromHex("#1565C0")).Padding(5).AlignLeft().Text("Nazwa towaru/usługi").Bold().FontSize(8).FontColor(Colors.White);
                        header.Cell().Background(Color.FromHex("#1565C0")).Padding(5).AlignCenter().Text("Jedn.").Bold().FontSize(8).FontColor(Colors.White);
                        header.Cell().Background(Color.FromHex("#1565C0")).Padding(5).AlignCenter().Text("Ilość").Bold().FontSize(8).FontColor(Colors.White);
                        header.Cell().Background(Color.FromHex("#1565C0")).Padding(5).AlignCenter().Text("Cena netto").Bold().FontSize(8).FontColor(Colors.White);
                        header.Cell().Background(Color.FromHex("#1565C0")).Padding(5).AlignCenter().Text("VAT").Bold().FontSize(8).FontColor(Colors.White);
                        header.Cell().Background(Color.FromHex("#1565C0")).Padding(5).AlignCenter().Text("Wartość netto").Bold().FontSize(8).FontColor(Colors.White);
                        header.Cell().Background(Color.FromHex("#1565C0")).Padding(5).AlignCenter().Text("Kwota VAT").Bold().FontSize(8).FontColor(Colors.White);
                        header.Cell().Background(Color.FromHex("#1565C0")).Padding(5).AlignCenter().Text("Wartość brutto").Bold().FontSize(8).FontColor(Colors.White);
                    });

                    var items = invoice.Items ?? new List<InvoiceItem>();
                    for (int i = 0; i < items.Count; i++)
                    {
                        var item = items[i];
                        var isOdd = i % 2 == 0;
                        var bg = isOdd ? "#FFFFFF" : "#F5F5F5";

                        table.Cell().Background(Color.FromHex(bg)).Padding(4).AlignCenter().Text((i + 1).ToString()).FontSize(8);
                        table.Cell().Background(Color.FromHex(bg)).Padding(4).AlignLeft().Text(item.Name).FontSize(8);
                        table.Cell().Background(Color.FromHex(bg)).Padding(4).AlignCenter().Text(item.Unit).FontSize(8);
                        table.Cell().Background(Color.FromHex(bg)).Padding(4).AlignRight().Text(item.Quantity.ToString("N2")).FontSize(8);
                        table.Cell().Background(Color.FromHex(bg)).Padding(4).AlignRight().Text($"{item.UnitPriceNet:N2}").FontSize(8);
                        table.Cell().Background(Color.FromHex(bg)).Padding(4).AlignCenter().Text(item.VatDisplay).FontSize(8);
                        table.Cell().Background(Color.FromHex(bg)).Padding(4).AlignRight().Text($"{item.TotalNet:N2}").FontSize(8);
                        table.Cell().Background(Color.FromHex(bg)).Padding(4).AlignRight().Text($"{item.TotalVat:N2}").FontSize(8);
                        table.Cell().Background(Color.FromHex(bg)).Padding(4).AlignRight().Text($"{item.TotalGross:N2}").Bold().FontSize(8);
                    }
                });

                // Podsumowanie per stawka VAT + razem
                col.Item().AlignRight().Width(280).Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.RelativeColumn(2);
                        cols.RelativeColumn();
                        cols.RelativeColumn();
                        cols.RelativeColumn();
                    });

                    table.Header(header =>
                    {
                        header.Cell().Background(Color.FromHex("#ECEFF1")).Padding(4).Text("Stawka VAT").Bold().FontSize(8);
                        header.Cell().Background(Color.FromHex("#ECEFF1")).Padding(4).AlignRight().Text("Netto").Bold().FontSize(8);
                        header.Cell().Background(Color.FromHex("#ECEFF1")).Padding(4).AlignRight().Text("VAT").Bold().FontSize(8);
                        header.Cell().Background(Color.FromHex("#ECEFF1")).Padding(4).AlignRight().Text("Brutto").Bold().FontSize(8);
                    });

                    var items = invoice.Items ?? new List<InvoiceItem>();
                    var groups = items.GroupBy(x => x.VatRate).OrderBy(x => x.Key);
                    foreach (var g in groups)
                    {
                        var net = g.Sum(x => x.TotalNet);
                        var vat = g.Sum(x => x.TotalVat);
                        var gross = g.Sum(x => x.TotalGross);
                        var first = g.First();
                        table.Cell().Padding(4).Text(first.VatDisplay).FontSize(8);
                        table.Cell().Padding(4).AlignRight().Text($"{net:N2}").FontSize(8);
                        table.Cell().Padding(4).AlignRight().Text($"{vat:N2}").FontSize(8);
                        table.Cell().Padding(4).AlignRight().Text($"{gross:N2}").FontSize(8);
                    }

                    // Razem
                    table.Cell().Background(Color.FromHex("#1565C0")).Padding(4).Text("RAZEM").Bold().FontSize(8).FontColor(Colors.White);
                    table.Cell().Background(Color.FromHex("#1565C0")).Padding(4).AlignRight().Text($"{invoice.TotalNet:N2}").Bold().FontSize(8).FontColor(Colors.White);
                    table.Cell().Background(Color.FromHex("#1565C0")).Padding(4).AlignRight().Text($"{invoice.TotalVat:N2}").Bold().FontSize(8).FontColor(Colors.White);
                    table.Cell().Background(Color.FromHex("#1565C0")).Padding(4).AlignRight().Text($"{invoice.TotalGross:N2}").Bold().FontSize(9).FontColor(Colors.White);
                });

                // Kwota słownie + uwagi
                col.Item().PaddingTop(8).Row(row =>
                {
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text($"Do zapłaty: {invoice.TotalGross:N2} PLN").Bold().FontSize(11);
                        if (!string.IsNullOrEmpty(invoice.Notes))
                            c.Item().PaddingTop(8).Text($"Uwagi: {invoice.Notes}").FontSize(8).FontColor(Colors.Grey.Medium);
                    });
                });

                // Podpisy
                col.Item().PaddingTop(24).Row(row =>
                {
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                        c.Item().AlignCenter().Text("Wystawił").FontSize(8).FontColor(Colors.Grey.Medium);
                    });
                    row.ConstantItem(40);
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                        c.Item().AlignCenter().Text("Odebrał").FontSize(8).FontColor(Colors.Grey.Medium);
                    });
                });
            });
        }

        public static string SaveToFile(Invoice invoice, AppSettings settings)
        {
            var folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "JumpVolt", "Faktury");
            Directory.CreateDirectory(folder);
            var safeName = invoice.InvoiceNumber.Replace("/", "-").Replace("\\", "-");
            var fileName = $"Faktura_{safeName}_{invoice.InvoiceDate:yyyyMMdd}.pdf";
            var path = Path.Combine(folder, fileName);
            var bytes = GeneratePdf(invoice, settings);
            File.WriteAllBytes(path, bytes);
            return path;
        }
    }
}
