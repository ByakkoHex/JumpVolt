using System.Collections.ObjectModel;

namespace SklepMotoryzacyjny.ViewModels
{
    /// <summary>
    /// ViewModel dla pojedynczego pola atrybutu kategorii.
    /// Używane w dynamicznym formularzu edycji produktu.
    /// </summary>
    public class CategoryFieldViewModel : BaseViewModel
    {
        private string _value = string.Empty;

        /// <summary>Klucz atrybutu (zapisywany do JSON)</summary>
        public string Key { get; set; } = string.Empty;
        
        /// <summary>Etykieta wyświetlana użytkownikowi</summary>
        public string Label { get; set; } = string.Empty;
        
        /// <summary>Typ pola: Text, Number, Dropdown, DropdownEditable</summary>
        public string FieldType { get; set; } = "Text";
        
        /// <summary>Opcje dla listy rozwijanej</summary>
        public ObservableCollection<string> Options { get; set; } = new();
        
        /// <summary>Sufiks (np. "Ah", "A", "mm")</summary>
        public string Suffix { get; set; } = string.Empty;
        
        /// <summary>Placeholder</summary>
        public string Placeholder { get; set; } = string.Empty;
        
        /// <summary>Czy pole jest wymagane</summary>
        public bool Required { get; set; }

        /// <summary>Aktualna wartość pola</summary>
        public string Value
        {
            get => _value;
            set => SetProperty(ref _value, value ?? string.Empty);
        }

        /// <summary>Czy to pole typu Dropdown (ComboBox nie edytowalny)</summary>
        public bool IsDropdown => FieldType == "Dropdown";
        
        /// <summary>Czy to pole typu DropdownEditable (ComboBox edytowalny)</summary>
        public bool IsDropdownEditable => FieldType == "DropdownEditable";
        
        /// <summary>Czy to pole tekstowe lub numeryczne</summary>
        public bool IsTextOrNumber => FieldType == "Text" || FieldType == "Number";

        /// <summary>Czy wyświetlić sufiks</summary>
        public bool HasSuffix => !string.IsNullOrEmpty(Suffix);

        /// <summary>Etykieta z sufiksem</summary>
        public string LabelWithSuffix => HasSuffix ? $"{Label} [{Suffix}]" : Label;
    }
}
