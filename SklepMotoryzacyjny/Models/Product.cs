using System.Text.Json;

namespace SklepMotoryzacyjny.Models
{
    /// <summary>
    /// Produkt w magazynie sklepu JumpVolt.
    /// Atrybuty specyficzne dla kategorii przechowywane jako JSON.
    /// </summary>
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Barcode { get; set; } = string.Empty;
        public string CatalogNumber { get; set; } = string.Empty;
        
        /// <summary>Typ kategorii: Akumulator, Prostownik, Chemia, Oleje, Płyny, Żarówki, Akcesoria, Inne</summary>
        public string CategoryType { get; set; } = "Inne";
        
        /// <summary>Marka produktu</summary>
        public string Brand { get; set; } = string.Empty;
        
        public decimal PurchasePrice { get; set; }
        public decimal SalePrice { get; set; }
        public string VatRate { get; set; } = "A";
        public int StockQuantity { get; set; }
        public int MinStockLevel { get; set; } = 2;
        public string Unit { get; set; } = "szt.";
        public bool IsActive { get; set; } = true;
        public string Notes { get; set; } = string.Empty;
        
        /// <summary>JSON z atrybutami specyficznymi dla kategorii</summary>
        public string AttributesJson { get; set; } = "{}";
        
        /// <summary>Nazwa pliku zdjęcia (przechowywane w folderze Images)</summary>
        public string ImageFileName { get; set; } = string.Empty;

        /// <summary>Opcjonalna nazwa wysyłana do kasy fiskalnej (max 40 znaków). Jeśli puste — używana jest nazwa produktu.</summary>
        public string FiscalName { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        // ======= Właściwości obliczone =======
        
        public bool IsLowStock => StockQuantity <= MinStockLevel;
        public string VatRateDisplay => VatRate switch { "A" => "23%", "B" => "8%", "C" => "5%", "D" => "0%", _ => "23%" };
        public string SalePriceDisplay => $"{SalePrice:N2} zł";
        
        /// <summary>Pełna ścieżka do zdjęcia produktu</summary>
        public string? ImageFullPath => string.IsNullOrEmpty(ImageFileName) 
            ? null 
            : System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "JumpVolt", "Images", ImageFileName);

        /// <summary>Czy produkt ma zdjęcie</summary>
        public bool HasImage => !string.IsNullOrEmpty(ImageFileName) 
                                && ImageFullPath != null 
                                && System.IO.File.Exists(ImageFullPath);

        public string CategoryIcon => CategoryType switch
        {
            "Akumulator" => "🔋",
            "Prostownik" => "🔌",
            "Chemia" => "🧪",
            "Oleje" => "🛢️",
            "Płyny" => "💧",
            "Żarówki" => "💡",
            "Akcesoria" => "🧰",
            _ => "📦"
        };

        public string DisplayName => $"{CategoryIcon} {Name}";

        /// <summary>Krótki opis atrybutów do wyświetlenia w liście</summary>
        public string AttributesSummary
        {
            get
            {
                var a = GetAttributes();
                return CategoryType switch
                {
                    "Akumulator" => Join(a, "Pojemność Ah", "Prąd rozruchowy A", "Napięcie", "Polaryzacja"),
                    "Prostownik" => Join(a, "Typ prostownika", "Napięcie obsługiwane", "Prąd ładowania A"),
                    "Chemia" => Join(a, "Rodzaj", "Pojemność", "Forma"),
                    "Oleje" => Join(a, "Typ", "Lepkość", "Specyfikacja", "Pojemność"),
                    "Płyny" => Join(a, "Rodzaj", "Pojemność", "Sezon"),
                    "Żarówki" => Join(a, "Typ żarówki", "Moc W", "Napięcie"),
                    "Akcesoria" => Join(a, "Rodzaj"),
                    _ => ""
                };
            }
        }

        // ======= JSON helpers =======

        public Dictionary<string, string> GetAttributes()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(AttributesJson) || AttributesJson == "{}") return new();
                return JsonSerializer.Deserialize<Dictionary<string, string>>(AttributesJson) ?? new();
            }
            catch { return new(); }
        }

        public void SetAttributes(Dictionary<string, string> attrs)
            => AttributesJson = JsonSerializer.Serialize(attrs);

        public string GetAttr(string key)
        {
            var a = GetAttributes();
            return a.TryGetValue(key, out var v) ? v : string.Empty;
        }

        public void SetAttr(string key, string value)
        {
            var a = GetAttributes();
            a[key] = value;
            SetAttributes(a);
        }

        private static string Join(Dictionary<string, string> a, params string[] keys)
        {
            var parts = new List<string>();
            foreach (var k in keys)
                if (a.TryGetValue(k, out var v) && !string.IsNullOrWhiteSpace(v))
                    parts.Add(v);
            return string.Join(" │ ", parts);
        }
    }
}
