using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using SklepMotoryzacyjny.Helpers;
using SklepMotoryzacyjny.Models;
using SklepMotoryzacyjny.Services;
using SklepMotoryzacyjny.Views;

namespace SklepMotoryzacyjny.ViewModels
{
    public class SalesViewModel : BaseViewModel
    {
        private readonly MainViewModel _mainVM;
        private string _searchText = string.Empty;
        private string _barcodeText = string.Empty;
        private decimal _paidAmount;
        private string _selectedPayment = "Gotówka";
        private Product? _selectedProduct;
        private SaleItem? _selectedCartItem;
        private int _addQuantity = 1;
        private string _filterCategory = "Wszystkie";
        private decimal _discountPercent = 0;

        public ObservableCollection<SaleItem> CartItems { get; } = new();
        public ObservableCollection<Product> SearchResults { get; } = new();
        public List<string> PaymentMethods { get; } = new() { "Gotówka", "Karta", "Przelew" };
        public List<string> CategoryFilters { get; }

        public ICommand SearchCommand { get; }
        public ICommand ScanBarcodeCommand { get; }
        public ICommand AddToCartCommand { get; }
        public ICommand RemoveFromCartCommand { get; }
        public ICommand ClearCartCommand { get; }
        public ICommand FinalizeSaleCommand { get; }
        public ICommand QuickAddCommand { get; }

        public string SearchText
        {
            get => _searchText;
            set { if (SetProperty(ref _searchText, value)) PerformSearch(); }
        }

        public string BarcodeText
        {
            get => _barcodeText;
            set => SetProperty(ref _barcodeText, value);
        }

        public string FilterCategory
        {
            get => _filterCategory;
            set { if (SetProperty(ref _filterCategory, value)) PerformSearch(); }
        }

        public decimal PaidAmount
        {
            get => _paidAmount;
            set { SetProperty(ref _paidAmount, value); UpdateTotals(); }
        }

        public string SelectedPayment
        {
            get => _selectedPayment;
            set { SetProperty(ref _selectedPayment, value); UpdateTotals(); }
        }

        public Product? SelectedProduct { get => _selectedProduct; set => SetProperty(ref _selectedProduct, value); }
        public SaleItem? SelectedCartItem { get => _selectedCartItem; set => SetProperty(ref _selectedCartItem, value); }
        public int AddQuantity { get => _addQuantity; set => SetProperty(ref _addQuantity, Math.Max(1, value)); }

        public decimal TotalAmount => CartTotal - DiscountAmount;
        public string TotalDisplay => $"{TotalAmount:N2} zł";
        public int TotalItems => CartItems.Sum(i => i.Quantity);
        public decimal ChangeAmount => SelectedPayment == "Gotówka" ? Math.Max(0, PaidAmount - TotalAmount) : 0;
        public string ChangeDisplay => $"{ChangeAmount:N2} zł";
        public bool IsPaymentCash => SelectedPayment == "Gotówka";
        public bool IsPaymentCard => SelectedPayment == "Karta";
        public bool CanFinalize => CartItems.Count > 0 && (SelectedPayment != "Gotówka" || PaidAmount >= TotalAmount);

        public decimal DiscountPercent
        {
            get => _discountPercent;
            set { SetProperty(ref _discountPercent, Math.Clamp(value, 0, 100)); UpdateTotals(); }
        }

        public decimal CartTotal => CartItems.Sum(i => i.TotalPrice);
        public decimal DiscountAmount => Math.Round(CartTotal * DiscountPercent / 100m, 2);
        public string DiscountAmountDisplay => $"-{DiscountAmount:N2} zł";
        public bool HasDiscount => DiscountPercent > 0;

        public SalesViewModel(MainViewModel mainVM)
        {
            _mainVM = mainVM;
            CategoryFilters = new List<string> { "Wszystkie" };
            CategoryFilters.AddRange(CategoryRegistry.AllCategoryNames);

            SearchCommand = new RelayCommand(() => PerformSearch());
            ScanBarcodeCommand = new RelayCommand(() => ProcessBarcode());
            AddToCartCommand = new RelayCommand(() => AddSelectedToCart(), () => SelectedProduct != null);
            RemoveFromCartCommand = new RelayCommand(() => RemoveSelectedFromCart(), () => SelectedCartItem != null);
            ClearCartCommand = new RelayCommand(() => ClearCart(), () => CartItems.Count > 0);
            FinalizeSaleCommand = new RelayCommand(() => FinalizeSale(), () => CanFinalize);
            QuickAddCommand = new RelayCommand(o => { if (o is Product p) AddProductToCart(p, 1); });

            RefreshProducts();
        }

        public void RefreshProducts() => PerformSearch();

        private void PerformSearch()
        {
            try
            {
                List<Product> results;
                if (!string.IsNullOrWhiteSpace(SearchText))
                    results = DatabaseService.Instance.SearchProducts(SearchText);
                else if (FilterCategory != "Wszystkie")
                    results = DatabaseService.Instance.GetProductsByCategory(FilterCategory);
                else
                    results = DatabaseService.Instance.GetAllProducts();

                if (!string.IsNullOrWhiteSpace(SearchText) && FilterCategory != "Wszystkie")
                    results = results.Where(p => p.CategoryType == FilterCategory).ToList();

                SearchResults.Clear();
                foreach (var p in results) SearchResults.Add(p);
            }
            catch (Exception ex) { _mainVM.SetStatus($"Błąd: {ex.Message}"); }
        }

        private void ProcessBarcode()
        {
            if (string.IsNullOrWhiteSpace(BarcodeText)) return;
            try
            {
                var product = DatabaseService.Instance.GetProductByBarcode(BarcodeText.Trim());
                if (product != null)
                {
                    AddProductToCart(product, 1);
                    _mainVM.SetStatus($"Dodano: {product.DisplayName}");
                }
                else
                {
                    _mainVM.SetStatus($"Nie znaleziono: {BarcodeText}");
                    MessageBox.Show($"Nie znaleziono produktu o kodzie:\n{BarcodeText}",
                        "Nie znaleziono", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            finally { BarcodeText = string.Empty; }
        }

        private void AddSelectedToCart()
        {
            if (SelectedProduct == null) return;
            AddProductToCart(SelectedProduct, AddQuantity);
            AddQuantity = 1;
        }

        private void AddProductToCart(Product product, int quantity)
        {
            var existing = CartItems.FirstOrDefault(i => i.ProductId == product.Id);
            int inCart = existing?.Quantity ?? 0;

            if (inCart + quantity > product.StockQuantity)
            {
                MessageBox.Show(
                    $"Brak wystarczającej ilości!\n\n{product.DisplayName}\nNa magazynie: {product.StockQuantity} {product.Unit}\nW koszyku: {inCart}",
                    "Brak na stanie", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (existing != null)
            {
                existing.Quantity += quantity;
                int idx = CartItems.IndexOf(existing);
                CartItems.RemoveAt(idx);
                CartItems.Insert(idx, existing);
            }
            else
            {
                CartItems.Add(new SaleItem
                {
                    ProductId = product.Id,
                    ProductName = !string.IsNullOrWhiteSpace(product.FiscalName)
                        ? product.FiscalName
                        : product.Name,
                    Barcode = product.Barcode,
                    Quantity = quantity,
                    UnitPrice = product.SalePrice,
                    VatRate = product.VatRate
                });
            }

            UpdateTotals();
            _mainVM.SetStatus($"Dodano: {product.DisplayName} ×{quantity}");

            // Po dodaniu akumulatora zapytaj o usługę montażu
            if (product.CategoryType == "Akumulator")
                PromptBatteryService(product);
        }

        private void PromptBatteryService(Product battery)
        {
            var dialog = new BatteryServiceDialog(battery.Name)
            {
                Owner = Application.Current.MainWindow
            };

            if (dialog.ShowDialog() != true || string.IsNullOrEmpty(dialog.SelectedServiceBarcode))
                return;

            var service = DatabaseService.Instance.GetProductByBarcode(dialog.SelectedServiceBarcode);
            if (service == null)
            {
                _mainVM.SetStatus($"Nie znaleziono usługi (kod: {dialog.SelectedServiceBarcode}) — dodaj ją najpierw w Magazynie");
                MessageBox.Show(
                    $"Nie znaleziono usługi o kodzie \"{dialog.SelectedServiceBarcode}\" w bazie danych.\n\n" +
                    "Dodaj produkt usługowy (Montaż / Dowóz i Montaż u klienta) w module Magazyn z odpowiednim kodem kreskowym.",
                    "Brak usługi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Usługi dodajemy bez sprawdzania stanu magazynowego, z ceną podaną przez użytkownika
            AddServiceToCart(service, dialog.SelectedPrice);
        }

        private void AddServiceToCart(Product service, decimal customPrice)
        {
            // Każda usługa to osobna pozycja — cena może być inna dla każdego akumulatora
            CartItems.Add(new SaleItem
            {
                ProductId = service.Id,
                ProductName = service.Name,
                Barcode = service.Barcode,
                Quantity = 1,
                UnitPrice = customPrice,
                VatRate = service.VatRate
            });

            UpdateTotals();
            _mainVM.SetStatus($"Dodano usługę: {service.DisplayName} — {customPrice:N2} zł");
        }

        private void RemoveSelectedFromCart()
        {
            if (SelectedCartItem == null) return;
            if (MessageBox.Show($"Usunąć \"{SelectedCartItem.ProductName}\"?",
                "Potwierdzenie", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                CartItems.Remove(SelectedCartItem);
                UpdateTotals();
            }
        }

        private void ClearCart()
        {
            if (MessageBox.Show("Wyczyścić cały paragon?", "Potwierdzenie",
                MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                CartItems.Clear();
                PaidAmount = 0;
                DiscountPercent = 0;
                UpdateTotals();
            }
        }

        private void FinalizeSale()
        {
            if (CartItems.Count == 0) return;

            string payInfo = IsPaymentCash
                ? $"Zapłacono: {PaidAmount:N2} zł\nReszta: {ChangeDisplay}\n"
                : IsPaymentCard ? "Płatność przez terminal kartowy\n" : "";

            if (MessageBox.Show(
                $"Sfinalizować sprzedaż?\n\nSuma: {TotalDisplay}\nPłatność: {SelectedPayment}\n" +
                payInfo + $"Pozycji: {CartItems.Count}",
                "Potwierdzenie", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            try
            {
                bool fiscalPrinted = false;
                var settings = DatabaseService.Instance.LoadSettings();

                if (settings.AutoPrintReceipt)
                    fiscalPrinted = TryPrintFiscalReceipt(settings);

                var sale = new Sale
                {
                    ReceiptNumber = DatabaseService.Instance.GenerateReceiptNumber(),
                    SaleDate = DateTime.Now,
                    TotalAmount = TotalAmount,
                    PaymentMethod = SelectedPayment,
                    PaidAmount = IsPaymentCash ? PaidAmount : TotalAmount,
                    ChangeAmount = ChangeAmount,
                    IsFiscalPrinted = fiscalPrinted,
                    Items = CartItems.ToList(),
                    DiscountPercent = DiscountPercent,
                    DiscountAmount = DiscountAmount,
                };

                DatabaseService.Instance.SaveSale(sale);

                string msg = $"Sprzedaż zapisana!\n\nParagon: {sale.ReceiptNumber}\nSuma: {TotalDisplay}";
                if (IsPaymentCash) msg += $"\nReszta: {ChangeDisplay}";
                if (fiscalPrinted) msg += "\n\n✓ Paragon fiskalny wydrukowany";
                else if (settings.AutoPrintReceipt) msg += "\n\n⚠ Paragon fiskalny NIE wydrukowany!";

                MessageBox.Show(msg, "Sprzedaż zakończona", MessageBoxButton.OK, MessageBoxImage.Information);

                CartItems.Clear();
                PaidAmount = 0;
                DiscountPercent = 0;
                UpdateTotals();
                RefreshProducts();
                _mainVM.SetStatus($"Sprzedaż {sale.ReceiptNumber} — {TotalDisplay}");
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool TryPrintFiscalReceipt(AppSettings settings)
        {
            using var fiscal = new NovitusFiscalService();
            if (!fiscal.ConnectFromSettings(settings))
            {
                if (MessageBox.Show($"Brak połączenia z kasą.\n{fiscal.LastError}\n\nKontynuować BEZ paragonu?",
                    "Kasa fiskalna", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
                    throw new OperationCanceledException();
                return false;
            }

            var itemsToPrint = DiscountPercent > 0
                ? CartItems.Select(i => new SaleItem
                {
                    ProductId = i.ProductId, ProductName = i.ProductName, Barcode = i.Barcode,
                    Quantity = i.Quantity, UnitPrice = Math.Round(i.UnitPrice * (1 - DiscountPercent / 100m), 2),
                    VatRate = i.VatRate
                }).ToList()
                : CartItems.ToList();

            if (!fiscal.PrintReceipt(itemsToPrint, SelectedPayment, PaidAmount))
            {
                if (MessageBox.Show($"Błąd druku: {fiscal.LastError}\n\nKontynuować BEZ paragonu?",
                    "Kasa fiskalna", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
                    throw new OperationCanceledException();
                return false;
            }

            return true;
        }

        private void UpdateTotals()
        {
            OnPropertyChanged(nameof(CartTotal));
            OnPropertyChanged(nameof(DiscountAmount));
            OnPropertyChanged(nameof(DiscountAmountDisplay));
            OnPropertyChanged(nameof(HasDiscount));
            OnPropertyChanged(nameof(TotalAmount));
            OnPropertyChanged(nameof(TotalDisplay));
            OnPropertyChanged(nameof(TotalItems));
            OnPropertyChanged(nameof(ChangeAmount));
            OnPropertyChanged(nameof(ChangeDisplay));
            OnPropertyChanged(nameof(CanFinalize));
            OnPropertyChanged(nameof(IsPaymentCash));
            OnPropertyChanged(nameof(IsPaymentCard));
        }
    }
}
