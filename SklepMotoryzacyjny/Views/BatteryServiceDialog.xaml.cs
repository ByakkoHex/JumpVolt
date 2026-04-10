using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace SklepMotoryzacyjny.Views
{
    public partial class BatteryServiceDialog : Window
    {
        /// <summary>Kod kreskowy wybranej usługi ("2" = Montaż, "3" = Dowóz i Montaż) lub pusty = brak.</summary>
        public string SelectedServiceBarcode { get; private set; } = string.Empty;

        /// <summary>Cena wpisana przez użytkownika dla wybranej usługi.</summary>
        public decimal SelectedPrice { get; private set; } = 0;

        public BatteryServiceDialog(string batteryName)
        {
            InitializeComponent();
            BatteryNameText.Text = batteryName;
        }

        private void OnMontaz(object sender, RoutedEventArgs e)
        {
            SelectedServiceBarcode = "2";
            SelectedPrice = ParsePrice(MontazPriceBox.Text);
            DialogResult = true;
        }

        private void OnDowozIMontaz(object sender, RoutedEventArgs e)
        {
            SelectedServiceBarcode = "3";
            SelectedPrice = ParsePrice(DowozPriceBox.Text);
            DialogResult = true;
        }

        private void OnBezUslugi(object sender, RoutedEventArgs e)
        {
            SelectedServiceBarcode = string.Empty;
            DialogResult = true;
        }

        // Zaznacz całą zawartość pola przy kliknięciu
        private void PriceBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.TextBox tb)
                tb.SelectAll();
        }

        // Zezwól tylko na cyfry i separator dziesiętny
        private void PriceBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, @"[\d,\.]");
        }

        private static decimal ParsePrice(string text)
        {
            string normalized = text.Replace(',', '.');
            return decimal.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal val)
                ? Math.Max(0, val)
                : 0;
        }
    }
}
