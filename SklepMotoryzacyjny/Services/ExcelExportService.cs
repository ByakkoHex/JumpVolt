using ClosedXML.Excel;
using SklepMotoryzacyjny.Models;

namespace SklepMotoryzacyjny.Services
{
    public static class ExcelExportService
    {
        public static string ExportSales(List<Sale> sales, string filePath)
        {
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Sprzedaz");

            // Header row
            var headers = new[] { "Nr paragonu", "Data", "Kwota", "Rabat %", "Metoda platnosci", "Fisk.", "Pozycji" };
            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cell(1, i + 1).Value = headers[i];
                ws.Cell(1, i + 1).Style.Font.Bold = true;
                ws.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#FFD600");
            }

            int row = 2;
            foreach (var s in sales)
            {
                ws.Cell(row, 1).Value = s.ReceiptNumber;
                ws.Cell(row, 2).Value = s.SaleDate.ToString("dd.MM.yyyy HH:mm");
                ws.Cell(row, 3).Value = (double)s.TotalAmount;
                ws.Cell(row, 3).Style.NumberFormat.Format = "#,##0.00";
                ws.Cell(row, 4).Value = (double)s.DiscountPercent;
                ws.Cell(row, 5).Value = s.PaymentMethod;
                ws.Cell(row, 6).Value = s.IsFiscalPrinted ? "Tak" : "Nie";
                ws.Cell(row, 7).Value = s.Items?.Count ?? 0;
                row++;
            }

            ws.Columns().AdjustToContents();
            wb.SaveAs(filePath);
            return filePath;
        }

        public static string ExportProducts(List<Product> products, string filePath)
        {
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Stan magazynu");

            var headers = new[] { "Nazwa", "Marka", "Kategoria", "Kod kreskowy", "Nr katalogowy", "Cena zakupu", "Cena sprzedazy", "VAT", "Stan", "Min. stan", "Jednostka" };
            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cell(1, i + 1).Value = headers[i];
                ws.Cell(1, i + 1).Style.Font.Bold = true;
                ws.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#FFD600");
            }

            int row = 2;
            foreach (var p in products)
            {
                ws.Cell(row, 1).Value = p.Name;
                ws.Cell(row, 2).Value = p.Brand;
                ws.Cell(row, 3).Value = p.CategoryType;
                ws.Cell(row, 4).Value = p.Barcode;
                ws.Cell(row, 5).Value = p.CatalogNumber;
                ws.Cell(row, 6).Value = (double)p.PurchasePrice;
                ws.Cell(row, 6).Style.NumberFormat.Format = "#,##0.00";
                ws.Cell(row, 7).Value = (double)p.SalePrice;
                ws.Cell(row, 7).Style.NumberFormat.Format = "#,##0.00";
                ws.Cell(row, 8).Value = p.VatRateDisplay;
                ws.Cell(row, 9).Value = p.StockQuantity;
                ws.Cell(row, 10).Value = p.MinStockLevel;
                ws.Cell(row, 11).Value = p.Unit;
                if (p.IsLowStock) ws.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#FFEBEE");
                row++;
            }

            ws.Columns().AdjustToContents();
            wb.SaveAs(filePath);
            return filePath;
        }
    }
}
