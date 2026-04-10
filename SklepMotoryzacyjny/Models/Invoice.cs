namespace SklepMotoryzacyjny.Models
{
    public class Invoice
    {
        public int Id { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateTime InvoiceDate { get; set; } = DateTime.Now;
        public DateTime? DueDate { get; set; }
        public int? SaleId { get; set; }

        // Nabywca
        public string BuyerName { get; set; } = string.Empty;
        public string BuyerNIP { get; set; } = string.Empty;
        public string BuyerAddress { get; set; } = string.Empty;
        public string BuyerCity { get; set; } = string.Empty;
        public string BuyerEmail { get; set; } = string.Empty;

        // Płatność
        public string PaymentMethod { get; set; } = "Przelew";
        public int PaymentDays { get; set; } = 14;

        // Kwoty
        public decimal TotalNet { get; set; }
        public decimal TotalVat { get; set; }
        public decimal TotalGross { get; set; }

        // Status
        public bool IsPaid { get; set; }
        public bool IsEmailSent { get; set; }
        public string Notes { get; set; } = string.Empty;

        public List<InvoiceItem> Items { get; set; } = new();

        // Computed
        public string TotalGrossDisplay => $"{TotalGross:N2} zł";
        public string DateDisplay => InvoiceDate.ToString("dd.MM.yyyy");
        public string DueDateDisplay => DueDate?.ToString("dd.MM.yyyy") ?? "-";
        public string StatusDisplay
        {
            get
            {
                if (IsEmailSent) return "Wysłana";
                if (IsPaid) return "Opłacona";
                return "Nowa";
            }
        }
        public int ItemCount => Items.Count;
    }

    public class InvoiceItem
    {
        public int Id { get; set; }
        public int InvoiceId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Unit { get; set; } = "szt.";
        public decimal Quantity { get; set; } = 1;
        public decimal UnitPriceNet { get; set; }
        public string VatRate { get; set; } = "A";

        // Computed
        public decimal VatPercent => VatRate switch { "A" => 23m, "B" => 8m, "C" => 5m, "D" => 0m, _ => 23m };
        public decimal TotalNet => Math.Round(UnitPriceNet * Quantity, 2);
        public decimal TotalVat => Math.Round(TotalNet * VatPercent / 100m, 2);
        public decimal TotalGross => TotalNet + TotalVat;
        public decimal UnitPriceGross => Math.Round(UnitPriceNet * (1 + VatPercent / 100m), 2);
        public string VatDisplay => VatRate switch { "A" => "23%", "B" => "8%", "C" => "5%", "D" => "0%", _ => "23%" };
    }
}
