namespace SklepMotoryzacyjny.Models
{
    /// <summary>
    /// Transakcja sprzedaży (paragon).
    /// </summary>
    public class Sale
    {
        public int Id { get; set; }
        
        /// <summary>Numer paragonu (wewnętrzny)</summary>
        public string ReceiptNumber { get; set; } = string.Empty;
        
        /// <summary>Data i czas sprzedaży</summary>
        public DateTime SaleDate { get; set; } = DateTime.Now;
        
        /// <summary>Suma brutto</summary>
        public decimal TotalAmount { get; set; }
        
        /// <summary>Metoda płatności: Gotówka, Karta, Przelew</summary>
        public string PaymentMethod { get; set; } = "Gotówka";
        
        /// <summary>Kwota zapłacona (przy gotówce)</summary>
        public decimal PaidAmount { get; set; }
        
        /// <summary>Reszta</summary>
        public decimal ChangeAmount { get; set; }
        
        /// <summary>Czy wydrukowano na kasie fiskalnej</summary>
        public bool IsFiscalPrinted { get; set; }
        
        /// <summary>Czy transakcja anulowana</summary>
        public bool IsCancelled { get; set; }

        /// <summary>Rabat procentowy (0–100)</summary>
        public decimal DiscountPercent { get; set; } = 0;

        /// <summary>Kwota rabatu</summary>
        public decimal DiscountAmount { get; set; } = 0;
        
        /// <summary>Pozycje na paragonie</summary>
        public List<SaleItem> Items { get; set; } = new();
        
        /// <summary>Sformatowana suma</summary>
        public string TotalDisplay => $"{TotalAmount:N2} zł";
        
        /// <summary>Sformatowana data</summary>
        public string DateDisplay => SaleDate.ToString("dd.MM.yyyy HH:mm");
        
        /// <summary>Liczba pozycji</summary>
        public int ItemCount => Items.Count;
        
        /// <summary>Status paragonu</summary>
        public string StatusDisplay => IsCancelled ? "ANULOWANY" : (IsFiscalPrinted ? "Wydrukowany" : "Bez paragonu");
    }

    /// <summary>
    /// Pozycja na paragonie.
    /// </summary>
    public class SaleItem
    {
        public int Id { get; set; }
        public int SaleId { get; set; }
        public int ProductId { get; set; }
        
        /// <summary>Nazwa produktu (kopia z momentu sprzedaży)</summary>
        public string ProductName { get; set; } = string.Empty;
        
        /// <summary>Kod kreskowy</summary>
        public string Barcode { get; set; } = string.Empty;
        
        /// <summary>Ilość sprzedana</summary>
        public int Quantity { get; set; } = 1;
        
        /// <summary>Cena jednostkowa brutto</summary>
        public decimal UnitPrice { get; set; }
        
        /// <summary>Stawka VAT</summary>
        public string VatRate { get; set; } = "A";
        
        /// <summary>Wartość pozycji brutto</summary>
        public decimal TotalPrice => UnitPrice * Quantity;
        
        /// <summary>Sformatowana wartość</summary>
        public string TotalPriceDisplay => $"{TotalPrice:N2} zł";
        
        /// <summary>Sformatowana cena jednostkowa</summary>
        public string UnitPriceDisplay => $"{UnitPrice:N2} zł";
    }
}
