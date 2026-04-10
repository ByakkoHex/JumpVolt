namespace SklepMotoryzacyjny.Models
{
    /// <summary>
    /// Zwrot towaru — powiązany z oryginalną sprzedażą.
    /// </summary>
    public class Return
    {
        public int Id { get; set; }
        public int OriginalSaleId { get; set; }

        /// <summary>Numer paragonu oryginalnej sprzedaży</summary>
        public string OriginalReceiptNumber { get; set; } = string.Empty;

        /// <summary>Numer dokumentu zwrotu</summary>
        public string ReturnNumber { get; set; } = string.Empty;

        /// <summary>Data i czas zwrotu</summary>
        public DateTime ReturnDate { get; set; } = DateTime.Now;

        /// <summary>Łączna wartość zwrotu</summary>
        public decimal TotalAmount { get; set; }

        /// <summary>Forma zwrotu pieniędzy: Gotówka, Karta, Przelew</summary>
        public string RefundMethod { get; set; } = "Gotówka";

        /// <summary>Pozycje zwrotu</summary>
        public List<ReturnItem> Items { get; set; } = new();

        public string TotalDisplay => $"{TotalAmount:N2} zł";
        public string DateDisplay => ReturnDate.ToString("dd.MM.yyyy HH:mm");
        public int ItemCount => Items.Count;
    }

    /// <summary>
    /// Pozycja zwrotu.
    /// </summary>
    public class ReturnItem
    {
        public int Id { get; set; }
        public int ReturnId { get; set; }
        public int ProductId { get; set; }

        /// <summary>Nazwa produktu (kopia z momentu sprzedaży)</summary>
        public string ProductName { get; set; } = string.Empty;

        public string Barcode { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public string VatRate { get; set; } = "A";

        public decimal TotalPrice => UnitPrice * Quantity;
        public string TotalPriceDisplay => $"{TotalPrice:N2} zł";
        public string UnitPriceDisplay => $"{UnitPrice:N2} zł";
    }
}
