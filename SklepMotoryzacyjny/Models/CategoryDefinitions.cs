namespace SklepMotoryzacyjny.Models
{
    /// <summary>
    /// Definicja pola atrybutu kategorii.
    /// </summary>
    public class CategoryField
    {
        public string Key { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        
        /// <summary>Text, Number, Dropdown, DropdownEditable</summary>
        public string FieldType { get; set; } = "Text";
        
        /// <summary>Opcje dla Dropdown / DropdownEditable</summary>
        public List<string> Options { get; set; } = new();
        
        /// <summary>Sufiks wyświetlany obok pola (np. "Ah", "A", "V")</summary>
        public string Suffix { get; set; } = string.Empty;
        
        /// <summary>Czy pole jest wymagane</summary>
        public bool Required { get; set; }
        
        /// <summary>Tekst podpowiedzi (placeholder)</summary>
        public string Placeholder { get; set; } = string.Empty;
    }

    /// <summary>
    /// Definicja kategorii produktu - jakie pola ma formularz.
    /// </summary>
    public class CategoryDefinition
    {
        public string Name { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public List<CategoryField> Fields { get; set; } = new();
    }

    /// <summary>
    /// Rejestr wszystkich kategorii i ich pól.
    /// </summary>
    public static class CategoryRegistry
    {
        public static readonly List<string> AllCategoryNames = new()
        {
            "Akumulator", "Prostownik", "Chemia", "Oleje", "Płyny", "Żarówki", "Akcesoria", "Inne"
        };

        public static readonly Dictionary<string, CategoryDefinition> Definitions = new()
        {
            // ============================================================
            // 🔋 AKUMULATORY
            // ============================================================
            ["Akumulator"] = new CategoryDefinition
            {
                Name = "Akumulator",
                Icon = "🔋",
                Fields = new()
                {
                    new CategoryField
                    {
                        Key = "Typ akumulatora", Label = "Typ akumulatora",
                        FieldType = "DropdownEditable",
                        Options = new() { "Kwasowo-ołowiowy", "AGM", "EFB", "Żelowy", "Litowo-jonowy" }
                    },
                    new CategoryField
                    {
                        Key = "Pojemność Ah", Label = "Pojemność",
                        FieldType = "Number", Suffix = "Ah", Placeholder = "np. 72"
                    },
                    new CategoryField
                    {
                        Key = "Prąd rozruchowy A", Label = "Prąd rozruchowy",
                        FieldType = "Number", Suffix = "A", Placeholder = "np. 680"
                    },
                    new CategoryField
                    {
                        Key = "Napięcie", Label = "Napięcie",
                        FieldType = "Dropdown",
                        Options = new() { "6V", "12V", "24V" }
                    },
                    new CategoryField
                    {
                        Key = "Polaryzacja", Label = "Polaryzacja",
                        FieldType = "Dropdown",
                        Options = new() { "Lewy plus (L+)", "Prawy plus (P+)" }
                    },
                    new CategoryField
                    {
                        Key = "Długość mm", Label = "Długość",
                        FieldType = "Number", Suffix = "mm", Placeholder = "opcjonalnie"
                    },
                    new CategoryField
                    {
                        Key = "Szerokość mm", Label = "Szerokość",
                        FieldType = "Number", Suffix = "mm", Placeholder = "opcjonalnie"
                    },
                    new CategoryField
                    {
                        Key = "Wysokość mm", Label = "Wysokość",
                        FieldType = "Number", Suffix = "mm", Placeholder = "opcjonalnie"
                    }
                }
            },

            // ============================================================
            // 🔌 PROSTOWNIKI
            // ============================================================
            ["Prostownik"] = new CategoryDefinition
            {
                Name = "Prostownik",
                Icon = "🔌",
                Fields = new()
                {
                    new CategoryField
                    {
                        Key = "Typ prostownika", Label = "Typ prostownika",
                        FieldType = "DropdownEditable",
                        Options = new() { "Automatyczny", "Mikroprocesorowy", "Transformatorowy", "Rozruchowy", "Rozruchowo-ładujący" }
                    },
                    new CategoryField
                    {
                        Key = "Napięcie obsługiwane", Label = "Napięcie obsługiwane",
                        FieldType = "DropdownEditable",
                        Options = new() { "6V", "12V", "6V/12V", "12V/24V", "6V/12V/24V" }
                    },
                    new CategoryField
                    {
                        Key = "Prąd ładowania A", Label = "Prąd ładowania",
                        FieldType = "Number", Suffix = "A", Placeholder = "np. 10"
                    },
                    new CategoryField
                    {
                        Key = "Pojemność obsługiwana od Ah", Label = "Akumulatory od",
                        FieldType = "Number", Suffix = "Ah", Placeholder = "np. 20"
                    },
                    new CategoryField
                    {
                        Key = "Pojemność obsługiwana do Ah", Label = "Akumulatory do",
                        FieldType = "Number", Suffix = "Ah", Placeholder = "np. 200"
                    }
                }
            },

            // ============================================================
            // 🧪 CHEMIA SAMOCHODOWA
            // ============================================================
            ["Chemia"] = new CategoryDefinition
            {
                Name = "Chemia samochodowa",
                Icon = "🧪",
                Fields = new()
                {
                    new CategoryField
                    {
                        Key = "Rodzaj", Label = "Rodzaj produktu",
                        FieldType = "DropdownEditable",
                        Options = new()
                        {
                            "Preparat do kokpitu", "Preparat do tapicerki", "Preparat do felg",
                            "Odmrażacz", "Smar techniczny", "Silikon", "Spray wielofunkcyjny",
                            "Preparat do szyb", "Preparat do plastików", "Środek do czyszczenia",
                            "Odtłuszczacz", "Konserwant podwozia", "Klej", "Uszczelniacz"
                        }
                    },
                    new CategoryField
                    {
                        Key = "Pojemność", Label = "Pojemność opakowania",
                        FieldType = "Text", Placeholder = "np. 500 ml, 1 L"
                    },
                    new CategoryField
                    {
                        Key = "Forma", Label = "Forma produktu",
                        FieldType = "Dropdown",
                        Options = new() { "Spray", "Płyn", "Pianka", "Żel", "Pasta", "Aerozol", "Chusteczki" }
                    }
                }
            },

            // ============================================================
            // 🛢️ OLEJE SILNIKOWE I SMARY
            // ============================================================
            ["Oleje"] = new CategoryDefinition
            {
                Name = "Oleje i smary",
                Icon = "🛢️",
                Fields = new()
                {
                    new CategoryField
                    {
                        Key = "Typ", Label = "Typ produktu",
                        FieldType = "DropdownEditable",
                        Options = new()
                        {
                            "Olej silnikowy", "Olej przekładniowy", "Olej hydrauliczny",
                            "Smar", "Olej do łańcuchów", "Olej do sprężarek",
                            "Olej do dwusuwów", "Olej do skrzyni biegów"
                        }
                    },
                    new CategoryField
                    {
                        Key = "Lepkość", Label = "Lepkość (klasa SAE)",
                        FieldType = "DropdownEditable",
                        Options = new()
                        {
                            "0W-20", "0W-30", "0W-40", "5W-20", "5W-30", "5W-40",
                            "10W-30", "10W-40", "15W-40", "20W-50", "75W-80", "75W-90", "80W-90"
                        },
                        Placeholder = "np. 5W-30"
                    },
                    new CategoryField
                    {
                        Key = "Specyfikacja", Label = "Specyfikacja",
                        FieldType = "Dropdown",
                        Options = new() { "Syntetyczny", "Półsyntetyczny", "Mineralny" }
                    },
                    new CategoryField
                    {
                        Key = "Pojemność", Label = "Pojemność",
                        FieldType = "DropdownEditable",
                        Options = new() { "0.5 L", "1 L", "2 L", "4 L", "5 L", "10 L", "20 L", "60 L", "200 L" },
                        Placeholder = "np. 5 L"
                    },
                    new CategoryField
                    {
                        Key = "Normy", Label = "Normy / aprobaty",
                        FieldType = "Text",
                        Placeholder = "np. ACEA C3, API SN, VW 504/507"
                    }
                }
            },

            // ============================================================
            // 💧 PŁYNY EKSPLOATACYJNE
            // ============================================================
            ["Płyny"] = new CategoryDefinition
            {
                Name = "Płyny eksploatacyjne",
                Icon = "💧",
                Fields = new()
                {
                    new CategoryField
                    {
                        Key = "Rodzaj", Label = "Rodzaj płynu",
                        FieldType = "DropdownEditable",
                        Options = new()
                        {
                            "Płyn do spryskiwaczy", "Płyn chłodniczy", "Płyn hamulcowy",
                            "Woda destylowana", "Elektrolit", "AdBlue"
                        }
                    },
                    new CategoryField
                    {
                        Key = "Pojemność", Label = "Pojemność",
                        FieldType = "DropdownEditable",
                        Options = new() { "0.5 L", "1 L", "2 L", "4 L", "5 L", "10 L", "20 L" },
                        Placeholder = "np. 5 L"
                    },
                    new CategoryField
                    {
                        Key = "Sezon", Label = "Sezon (spryskiwacze)",
                        FieldType = "Dropdown",
                        Options = new() { "", "Letni", "Zimowy", "Całoroczny" }
                    },
                    new CategoryField
                    {
                        Key = "Specyfikacja", Label = "Specyfikacja (hamulcowe)",
                        FieldType = "Dropdown",
                        Options = new() { "", "DOT-3", "DOT-4", "DOT-5", "DOT-5.1" }
                    },
                    new CategoryField
                    {
                        Key = "Kolor", Label = "Kolor (chłodnicze)",
                        FieldType = "Dropdown",
                        Options = new() { "", "Czerwony", "Zielony", "Niebieski", "Różowy", "Żółty", "Fioletowy" }
                    },
                    new CategoryField
                    {
                        Key = "Temperatura", Label = "Temperatura zamarzania",
                        FieldType = "Text",
                        Placeholder = "np. -22°C"
                    }
                }
            },

            // ============================================================
            // 💡 ŻARÓWKI SAMOCHODOWE
            // ============================================================
            ["Żarówki"] = new CategoryDefinition
            {
                Name = "Żarówki samochodowe",
                Icon = "💡",
                Fields = new()
                {
                    new CategoryField
                    {
                        Key = "Typ żarówki", Label = "Typ żarówki",
                        FieldType = "DropdownEditable",
                        Options = new()
                        {
                            "H1", "H3", "H4", "H7", "H8", "H9", "H11", "H15", "H16",
                            "HB3 (9005)", "HB4 (9006)", "HIR2",
                            "D1S", "D2S", "D3S", "D4S",
                            "LED H1", "LED H4", "LED H7", "LED H11",
                            "W5W", "W16W", "W21W", "W21/5W",
                            "P21W", "P21/5W", "PY21W",
                            "C5W", "C10W",
                            "T4W", "R5W", "R10W",
                            "H6W", "H21W"
                        }
                    },
                    new CategoryField
                    {
                        Key = "Napięcie", Label = "Napięcie",
                        FieldType = "Dropdown",
                        Options = new() { "12V", "24V" }
                    },
                    new CategoryField
                    {
                        Key = "Moc W", Label = "Moc",
                        FieldType = "Text", Suffix = "W", Placeholder = "np. 55"
                    },
                    new CategoryField
                    {
                        Key = "Trzonek", Label = "Typ trzonka / mocowania",
                        FieldType = "DropdownEditable",
                        Options = new()
                        {
                            "P14.5s", "PK22s", "P43t", "PX26d", "PGJ19-1", "PGJ19-2",
                            "W2.1x9.5d", "W3x16d", "W3x16q",
                            "BA15s", "BAY15d", "BAU15s",
                            "SV8.5-8", "BA9s"
                        },
                        Placeholder = "typ trzonka"
                    },
                    new CategoryField
                    {
                        Key = "Technologia", Label = "Technologia",
                        FieldType = "Dropdown",
                        Options = new() { "", "Halogenowa", "LED", "Ksenon (HID)", "Żarowa" }
                    }
                }
            },

            // ============================================================
            // 🧰 AKCESORIA SAMOCHODOWE
            // ============================================================
            ["Akcesoria"] = new CategoryDefinition
            {
                Name = "Akcesoria samochodowe",
                Icon = "🧰",
                Fields = new()
                {
                    new CategoryField
                    {
                        Key = "Rodzaj", Label = "Rodzaj akcesorium",
                        FieldType = "DropdownEditable",
                        Options = new()
                        {
                            "Klemy akumulatorowe", "Kable rozruchowe", "Wycieraczki",
                            "Mostki akumulatora", "Opaski zaciskowe", "Bezpieczniki",
                            "Złączki elektryczne", "Taśma izolacyjna", "Przewody elektryczne",
                            "Ładowarka USB", "Uchwyt na telefon", "Odświeżacz powietrza",
                            "Apteczka", "Trójkąt ostrzegawczy", "Gaśnica",
                            "Podnośnik", "Klucz do kół"
                        }
                    },
                    new CategoryField
                    {
                        Key = "Rozmiar", Label = "Rozmiar / wymiar",
                        FieldType = "Text", Placeholder = "np. 400 mm, 16 cali"
                    },
                    new CategoryField
                    {
                        Key = "Materiał", Label = "Materiał",
                        FieldType = "Text", Placeholder = "opcjonalnie"
                    },
                    new CategoryField
                    {
                        Key = "Szczegóły", Label = "Szczegóły techniczne",
                        FieldType = "Text", Placeholder = "np. 600A, 3m, przekrój 25mm²"
                    }
                }
            },

            // ============================================================
            // 📦 INNE
            // ============================================================
            ["Inne"] = new CategoryDefinition
            {
                Name = "Inne",
                Icon = "📦",
                Fields = new()
                {
                    new CategoryField
                    {
                        Key = "Rodzaj", Label = "Rodzaj produktu",
                        FieldType = "Text"
                    },
                    new CategoryField
                    {
                        Key = "Opis", Label = "Opis szczegółowy",
                        FieldType = "Text"
                    }
                }
            }
        };

        /// <summary>Pobiera definicję kategorii lub domyślną "Inne".</summary>
        public static CategoryDefinition GetDefinition(string categoryType)
        {
            return Definitions.TryGetValue(categoryType, out var def) ? def : Definitions["Inne"];
        }
    }
}
