using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SklepMotoryzacyjny.Helpers;
using SklepMotoryzacyjny.Models;
using SklepMotoryzacyjny.Services;

namespace SklepMotoryzacyjny.ViewModels
{
    /// <summary>
    /// ViewModel zarządzania produktami/magazynem JumpVolt.
    /// Obsługuje dynamiczne pola formularza w zależności od kategorii.
    /// </summary>
    public class ProductsViewModel : BaseViewModel
    {
        private readonly MainViewModel _mainVM;
        private string _searchText = string.Empty;
        private string _filterCategory = "Wszystkie";
        private Product? _selectedProduct;
        private bool _isEditing;
        private bool _isNewProduct;
        private bool _showLowStockOnly;

        // Pola formularza - wspólne
        private string _editName = string.Empty;
        private string _editBarcode = string.Empty;
        private string _editCatalogNumber = string.Empty;
        private string _editCategoryType = "Akumulator";
        private string _editBrand = string.Empty;
        private string _editPurchasePrice = "0,00";
        private string _editPurchasePriceGross = "0,00";
        private string _editSalePrice = "0,00";
        private string _editSalePriceNet = "0,00";
        private string _editVatRate = "A";
        private bool _suppressPriceRecalc = false;
        private string _editStockQuantity = "0";
        private string _editMinStockLevel = "2";
        private string _editUnit = "szt.";
        private string _editNotes = string.Empty;
        private string _editFiscalName = string.Empty;
        private string _editImagePath = string.Empty;  // ścieżka do podglądu
        private string _editImageFileName = string.Empty;  // nazwa pliku w bazie

        // Kolekcje
        public ObservableCollection<Product> Products { get; } = new();
        public ObservableCollection<string> FilterCategories { get; } = new();
        public ObservableCollection<string> Brands { get; } = new();
        public ObservableCollection<CategoryFieldViewModel> DynamicFields { get; } = new();

        // Opcje stałe
        public List<string> CategoryTypes => CategoryRegistry.AllCategoryNames;
        public List<string> VatRates { get; } = new() { "A", "B", "C", "D" };
        public List<string> Units { get; } = new() { "szt.", "l", "kg", "m", "kpl.", "op." };

        // Komendy
        public ICommand SearchCommand { get; }
        public ICommand AddNewCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelEditCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand ShowLowStockCommand { get; }
        public ICommand AddStockCommand { get; }
        public ICommand AddImageCommand { get; }
        public ICommand RemoveImageCommand { get; }
        public ICommand AddBrandCommand { get; }
        public ICommand DeleteBrandCommand { get; }

        // Właściwości filtru
        public string SearchText
        {
            get => _searchText;
            set { if (SetProperty(ref _searchText, value)) PerformSearch(); }
        }

        public string FilterCategory
        {
            get => _filterCategory;
            set { if (SetProperty(ref _filterCategory, value)) PerformSearch(); }
        }

        public Product? SelectedProduct
        {
            get => _selectedProduct;
            set => SetProperty(ref _selectedProduct, value);
        }

        public bool IsEditing
        {
            get => _isEditing;
            set => SetProperty(ref _isEditing, value);
        }

        public bool ShowLowStockOnly
        {
            get => _showLowStockOnly;
            set { if (SetProperty(ref _showLowStockOnly, value)) PerformSearch(); }
        }

        public string EditFormTitle => _isNewProduct ? "NOWY PRODUKT" : "EDYCJA PRODUKTU";

        // Pola formularza
        public string EditName { get => _editName; set => SetProperty(ref _editName, value); }
        public string EditBarcode { get => _editBarcode; set => SetProperty(ref _editBarcode, value); }
        public string EditCatalogNumber { get => _editCatalogNumber; set => SetProperty(ref _editCatalogNumber, value); }
        
        public string EditCategoryType
        {
            get => _editCategoryType;
            set
            {
                if (SetProperty(ref _editCategoryType, value))
                    BuildDynamicFields();
            }
        }

        public string EditBrand { get => _editBrand; set => SetProperty(ref _editBrand, value); }

        private decimal VatMultiplier => _editVatRate switch
        {
            "A" => 1.23m, "B" => 1.08m, "C" => 1.05m, "D" => 1.00m, _ => 1.23m
        };

        private static decimal? ParsePrice(string s)
        {
            if (decimal.TryParse(s.Replace(",", "."), System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out decimal v) && v >= 0)
                return v;
            return null;
        }

        // Cena zakupu NETTO (przechowywana w bazie)
        public string EditPurchasePrice
        {
            get => _editPurchasePrice;
            set
            {
                if (SetProperty(ref _editPurchasePrice, value) && !_suppressPriceRecalc)
                {
                    _suppressPriceRecalc = true;
                    var net = ParsePrice(value);
                    _editPurchasePriceGross = net.HasValue ? (net.Value * VatMultiplier).ToString("N2") : "0,00";
                    OnPropertyChanged(nameof(EditPurchasePriceGross));
                    _suppressPriceRecalc = false;
                }
            }
        }

        // Cena zakupu BRUTTO (wyliczona; edytowalna — aktualizuje netto)
        public string EditPurchasePriceGross
        {
            get => _editPurchasePriceGross;
            set
            {
                if (SetProperty(ref _editPurchasePriceGross, value) && !_suppressPriceRecalc)
                {
                    _suppressPriceRecalc = true;
                    var gross = ParsePrice(value);
                    _editPurchasePrice = gross.HasValue ? (gross.Value / VatMultiplier).ToString("N2") : "0,00";
                    OnPropertyChanged(nameof(EditPurchasePrice));
                    _suppressPriceRecalc = false;
                }
            }
        }

        // Cena sprzedaży BRUTTO (przechowywana w bazie)
        public string EditSalePrice
        {
            get => _editSalePrice;
            set
            {
                if (SetProperty(ref _editSalePrice, value) && !_suppressPriceRecalc)
                {
                    _suppressPriceRecalc = true;
                    var gross = ParsePrice(value);
                    _editSalePriceNet = gross.HasValue ? (gross.Value / VatMultiplier).ToString("N2") : "0,00";
                    OnPropertyChanged(nameof(EditSalePriceNet));
                    _suppressPriceRecalc = false;
                }
            }
        }

        // Cena sprzedaży NETTO (wyliczona; edytowalna — aktualizuje brutto)
        public string EditSalePriceNet
        {
            get => _editSalePriceNet;
            set
            {
                if (SetProperty(ref _editSalePriceNet, value) && !_suppressPriceRecalc)
                {
                    _suppressPriceRecalc = true;
                    var net = ParsePrice(value);
                    _editSalePrice = net.HasValue ? (net.Value * VatMultiplier).ToString("N2") : "0,00";
                    OnPropertyChanged(nameof(EditSalePrice));
                    _suppressPriceRecalc = false;
                }
            }
        }

        public string EditVatRate
        {
            get => _editVatRate;
            set
            {
                if (SetProperty(ref _editVatRate, value))
                    RecalcPricesOnVatChange();
            }
        }

        // Przy zmianie VAT: sprzedaż — zachowaj brutto, przelicz netto; zakup — zachowaj netto, przelicz brutto
        private void RecalcPricesOnVatChange()
        {
            _suppressPriceRecalc = true;
            var saleGross = ParsePrice(_editSalePrice);
            _editSalePriceNet = saleGross.HasValue ? (saleGross.Value / VatMultiplier).ToString("N2") : "0,00";
            var purchaseNet = ParsePrice(_editPurchasePrice);
            _editPurchasePriceGross = purchaseNet.HasValue ? (purchaseNet.Value * VatMultiplier).ToString("N2") : "0,00";
            _suppressPriceRecalc = false;
            OnPropertyChanged(nameof(EditSalePriceNet));
            OnPropertyChanged(nameof(EditPurchasePriceGross));
        }
        public string EditStockQuantity { get => _editStockQuantity; set => SetProperty(ref _editStockQuantity, value); }
        public string EditMinStockLevel { get => _editMinStockLevel; set => SetProperty(ref _editMinStockLevel, value); }
        public string EditUnit { get => _editUnit; set => SetProperty(ref _editUnit, value); }
        public string EditNotes { get => _editNotes; set => SetProperty(ref _editNotes, value); }
        public string EditFiscalName { get => _editFiscalName; set => SetProperty(ref _editFiscalName, value); }

        public string EditImagePath
        { 
            get => _editImagePath; 
            set { SetProperty(ref _editImagePath, value); OnPropertyChanged(nameof(HasEditImage)); } 
        }
        public string EditImageFileName { get => _editImageFileName; set => SetProperty(ref _editImageFileName, value); }
        public bool HasEditImage => !string.IsNullOrEmpty(EditImagePath) && System.IO.File.Exists(EditImagePath);

        // Statystyki
        public int ProductCount => Products.Count;
        public int LowStockCount => Products.Count(p => p.IsLowStock);

        public string CategoryIcon => CategoryRegistry.GetDefinition(EditCategoryType).Icon;
        public string CategoryFullName => CategoryRegistry.GetDefinition(EditCategoryType).Name;

        public ProductsViewModel(MainViewModel mainVM)
        {
            _mainVM = mainVM;

            SearchCommand = new RelayCommand(() => PerformSearch());
            AddNewCommand = new RelayCommand(() => StartNewProduct());
            EditCommand = new RelayCommand(() => StartEditProduct(), () => SelectedProduct != null);
            SaveCommand = new RelayCommand(() => SaveProduct());
            CancelEditCommand = new RelayCommand(() => CancelEdit());
            DeleteCommand = new RelayCommand(() => DeleteProduct(), () => SelectedProduct != null);
            RefreshCommand = new RelayCommand(() => RefreshProducts());
            ShowLowStockCommand = new RelayCommand(() => ShowLowStockOnly = !ShowLowStockOnly);
            AddStockCommand = new RelayCommand(() => AddStock(), () => SelectedProduct != null);
            AddImageCommand = new RelayCommand(() => AddImage());
            RemoveImageCommand = new RelayCommand(() => RemoveImage());
            AddBrandCommand = new RelayCommand(() => AddNewBrand());
            DeleteBrandCommand = new RelayCommand(() => DeleteBrand());

            RefreshProducts();
        }

        public void RefreshProducts()
        {
            PerformSearch();
            LoadBrands();
            LoadFilterCategories();
            OnPropertyChanged(nameof(ProductCount));
            OnPropertyChanged(nameof(LowStockCount));
        }

        private void LoadBrands()
        {
            Brands.Clear();
            foreach (var b in DatabaseService.Instance.GetBrands())
                Brands.Add(b);
        }

        private void LoadFilterCategories()
        {
            var current = FilterCategory;
            FilterCategories.Clear();
            FilterCategories.Add("Wszystkie");
            foreach (var cat in CategoryRegistry.AllCategoryNames)
                FilterCategories.Add(cat);
            FilterCategory = current;
        }

        private void PerformSearch()
        {
            try
            {
                List<Product> results;
                if (ShowLowStockOnly)
                    results = DatabaseService.Instance.GetLowStockProducts();
                else if (!string.IsNullOrWhiteSpace(SearchText))
                    results = DatabaseService.Instance.SearchProducts(SearchText);
                else if (FilterCategory != "Wszystkie" && !string.IsNullOrEmpty(FilterCategory))
                    results = DatabaseService.Instance.GetProductsByCategory(FilterCategory);
                else
                    results = DatabaseService.Instance.GetAllProducts();

                // Dodatkowy filtr kategorii na wynikach wyszukiwania
                if (!string.IsNullOrWhiteSpace(SearchText) && FilterCategory != "Wszystkie" && !string.IsNullOrEmpty(FilterCategory))
                    results = results.Where(p => p.CategoryType == FilterCategory).ToList();

                Products.Clear();
                foreach (var p in results)
                    Products.Add(p);

                OnPropertyChanged(nameof(ProductCount));
                OnPropertyChanged(nameof(LowStockCount));
            }
            catch (Exception ex)
            {
                _mainVM.SetStatus($"Błąd: {ex.Message}");
            }
        }

        /// <summary>
        /// Buduje dynamiczne pola formularza na podstawie wybranej kategorii.
        /// </summary>
        private void BuildDynamicFields(Dictionary<string, string>? existingValues = null)
        {
            DynamicFields.Clear();
            var def = CategoryRegistry.GetDefinition(EditCategoryType);

            foreach (var field in def.Fields)
            {
                var vm = new CategoryFieldViewModel
                {
                    Key = field.Key,
                    Label = field.Label,
                    FieldType = field.FieldType,
                    Suffix = field.Suffix,
                    Placeholder = field.Placeholder,
                    Required = field.Required
                };

                foreach (var opt in field.Options)
                    vm.Options.Add(opt);

                // Ustaw istniejącą wartość
                if (existingValues != null && existingValues.TryGetValue(field.Key, out var val))
                    vm.Value = val;

                DynamicFields.Add(vm);
            }

            OnPropertyChanged(nameof(CategoryIcon));
            OnPropertyChanged(nameof(CategoryFullName));
        }

        private void StartNewProduct()
        {
            _isNewProduct = true;
            ClearEditForm();

            var settings = DatabaseService.Instance.LoadSettings();
            EditVatRate = settings.DefaultVatRate;
            EditCategoryType = "Akumulator";
            BuildDynamicFields();

            IsEditing = true;
            OnPropertyChanged(nameof(EditFormTitle));
        }

        private void StartEditProduct()
        {
            if (SelectedProduct == null) return;
            _isNewProduct = false;
            var p = SelectedProduct;

            EditName = p.Name;
            EditBarcode = p.Barcode;
            EditCatalogNumber = p.CatalogNumber;
            EditCategoryType = p.CategoryType;
            EditBrand = p.Brand;
            EditPurchasePrice = p.PurchasePrice.ToString("N2");
            EditSalePrice = p.SalePrice.ToString("N2");
            EditVatRate = p.VatRate;
            EditStockQuantity = p.StockQuantity.ToString();
            EditMinStockLevel = p.MinStockLevel.ToString();
            EditUnit = p.Unit;
            EditNotes = p.Notes;
            EditFiscalName = p.FiscalName;
            EditImageFileName = p.ImageFileName;
            EditImagePath = p.HasImage ? p.ImageFullPath! : string.Empty;

            BuildDynamicFields(p.GetAttributes());

            IsEditing = true;
            OnPropertyChanged(nameof(EditFormTitle));
        }

        private void SaveProduct()
        {
            if (string.IsNullOrWhiteSpace(EditName))
            {
                MessageBox.Show("Podaj nazwę produktu!", "Brak nazwy", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(EditSalePrice.Replace(",", "."), System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out decimal salePrice) || salePrice <= 0)
            {
                MessageBox.Show("Podaj prawidłową cenę sprzedaży!", "Błąd ceny", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            decimal.TryParse(EditPurchasePrice.Replace(",", "."), System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out decimal purchasePrice);
            int.TryParse(EditStockQuantity, out int stockQty);
            int.TryParse(EditMinStockLevel, out int minStock);

            // Zbierz atrybuty dynamiczne
            var attrs = new Dictionary<string, string>();
            foreach (var field in DynamicFields)
            {
                if (!string.IsNullOrWhiteSpace(field.Value))
                    attrs[field.Key] = field.Value.Trim();
            }

            try
            {
                var product = new Product
                {
                    Name = EditName.Trim(),
                    Barcode = EditBarcode?.Trim() ?? string.Empty,
                    CatalogNumber = EditCatalogNumber?.Trim() ?? string.Empty,
                    CategoryType = EditCategoryType,
                    Brand = EditBrand?.Trim() ?? string.Empty,
                    PurchasePrice = purchasePrice,
                    SalePrice = salePrice,
                    VatRate = EditVatRate,
                    StockQuantity = stockQty,
                    MinStockLevel = minStock,
                    Unit = EditUnit,
                    Notes = EditNotes?.Trim() ?? string.Empty,
                    FiscalName = EditFiscalName?.Trim() ?? string.Empty,
                    ImageFileName = EditImageFileName,
                    IsActive = true
                };
                product.SetAttributes(attrs);

                if (_isNewProduct)
                {
                    product.Id = DatabaseService.Instance.AddProduct(product);
                    _mainVM.SetStatus($"Dodano: {product.CategoryIcon} {product.Name}");
                }
                else
                {
                    product.Id = SelectedProduct!.Id;
                    DatabaseService.Instance.UpdateProduct(product);
                    _mainVM.SetStatus($"Zaktualizowano: {product.CategoryIcon} {product.Name}");
                }

                IsEditing = false;
                RefreshProducts();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd zapisu: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelEdit()
        {
            IsEditing = false;
            ClearEditForm();
        }

        private void DeleteProduct()
        {
            if (SelectedProduct == null) return;
            var result = MessageBox.Show(
                $"Czy na pewno usunąć produkt?\n\n{SelectedProduct.DisplayName}\n\nProdukt zostanie dezaktywowany.",
                "Potwierdzenie", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                DatabaseService.Instance.DeactivateProduct(SelectedProduct.Id);
                _mainVM.SetStatus($"Usunięto: {SelectedProduct.DisplayName}");
                RefreshProducts();
            }
        }

        private void AddStock()
        {
            if (SelectedProduct == null) return;

            var dialog = new Window
            {
                Title = "Przyjęcie towaru",
                Width = 480, SizeToContent = SizeToContent.Height,
                MinHeight = 200,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Application.Current.MainWindow,
                ResizeMode = ResizeMode.NoResize
            };

            var panel = new StackPanel { Margin = new Thickness(20) };
            panel.Children.Add(new TextBlock
            {
                Text = $"{SelectedProduct.DisplayName}\nMarka: {SelectedProduct.Brand}\nAktualny stan: {SelectedProduct.StockQuantity} {SelectedProduct.Unit}",
                FontSize = 16, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 16)
            });
            panel.Children.Add(new TextBlock { Text = "Ilość do przyjęcia:", FontSize = 16, FontWeight = FontWeights.SemiBold });
            var inputBox = new TextBox { Text = "1", FontSize = 22, Padding = new Thickness(8), Margin = new Thickness(0, 4, 0, 16) };
            panel.Children.Add(inputBox);

            var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
            var btnOk = new Button { Content = "📥 Przyjmij", Width = 150, Height = 46, FontSize = 18, FontWeight = FontWeights.SemiBold, Margin = new Thickness(4) };
            var btnCancel = new Button { Content = "Anuluj", Width = 120, Height = 46, FontSize = 16, Margin = new Thickness(4) };
            btnOk.Click += (s, e) => { dialog.DialogResult = true; dialog.Close(); };
            btnCancel.Click += (s, e) => { dialog.DialogResult = false; dialog.Close(); };
            btnPanel.Children.Add(btnOk);
            btnPanel.Children.Add(btnCancel);
            panel.Children.Add(btnPanel);
            dialog.Content = panel;

            inputBox.SelectAll();
            inputBox.Focus();

            if (dialog.ShowDialog() == true && int.TryParse(inputBox.Text, out int qty) && qty > 0)
            {
                var product = SelectedProduct;
                product.StockQuantity += qty;
                DatabaseService.Instance.UpdateProduct(product);
                _mainVM.SetStatus($"Przyjęto {qty} {product.Unit} — {product.DisplayName}");
                RefreshProducts();
            }
        }

        private void AddNewBrand()
        {
            var primaryStyle = (Style)System.Windows.Application.Current.Resources["PrimaryButton"];
            var dangerStyle = (Style)System.Windows.Application.Current.Resources["DangerButton"];

            var dialog = new Window
            {
                Title = "Dodaj nową markę",
                Width = 440, SizeToContent = SizeToContent.Height,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Application.Current.MainWindow,
                ResizeMode = ResizeMode.NoResize,
                Background = System.Windows.Media.Brushes.White
            };

            var panel = new StackPanel { Margin = new Thickness(24) };
            panel.Children.Add(new TextBlock
            {
                Text = "Nazwa nowej marki:",
                FontSize = 16, FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 8)
            });

            var inputBox = new TextBox
            {
                FontSize = 20, Padding = new Thickness(10, 8, 10, 8),
                Margin = new Thickness(0, 0, 0, 16),
                BorderThickness = new Thickness(2),
                BorderBrush = System.Windows.Media.Brushes.Gray
            };
            panel.Children.Add(inputBox);

            var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
            var btnOk = new Button
            {
                Content = "✓  Dodaj markę",
                Style = primaryStyle,
                MinWidth = 160, Height = 46
            };
            var btnCancel = new Button
            {
                Content = "Anuluj",
                Style = dangerStyle,
                MinWidth = 110, Height = 46
            };
            btnOk.Click += (s, e) => { dialog.DialogResult = true; };
            btnCancel.Click += (s, e) => { dialog.DialogResult = false; };
            btnPanel.Children.Add(btnOk);
            btnPanel.Children.Add(btnCancel);
            panel.Children.Add(btnPanel);
            dialog.Content = panel;

            // Enter potwierdza
            inputBox.KeyDown += (s, e) => { if (e.Key == System.Windows.Input.Key.Return) dialog.DialogResult = true; };
            inputBox.Focus();

            if (dialog.ShowDialog() == true)
            {
                string name = inputBox.Text.Trim();
                if (string.IsNullOrWhiteSpace(name)) return;

                DatabaseService.Instance.AddBrand(name);
                LoadBrands();
                EditBrand = name;
                _mainVM.SetStatus($"Dodano markę: {name}");
            }
        }

        private void DeleteBrand()
        {
            var primaryStyle = (Style)System.Windows.Application.Current.Resources["PrimaryButton"];
            var dangerStyle = (Style)System.Windows.Application.Current.Resources["DangerButton"];

            var dialog = new Window
            {
                Title = "Usuń markę",
                Width = 500, SizeToContent = SizeToContent.Height,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Application.Current.MainWindow,
                ResizeMode = ResizeMode.NoResize,
                Background = System.Windows.Media.Brushes.White
            };

            var panel = new StackPanel { Margin = new Thickness(24) };
            panel.Children.Add(new TextBlock
            {
                Text = "Wybierz markę do usunięcia:",
                FontSize = 16, FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 8)
            });

            var brandCombo = new ComboBox
            {
                FontSize = 18, Padding = new Thickness(10, 8, 10, 8),
                Margin = new Thickness(0, 0, 0, 8),
                MinHeight = 44
            };
            foreach (var b in Brands)
                brandCombo.Items.Add(b);
            if (!string.IsNullOrEmpty(EditBrand) && Brands.Contains(EditBrand))
                brandCombo.SelectedItem = EditBrand;
            else if (brandCombo.Items.Count > 0)
                brandCombo.SelectedIndex = 0;
            panel.Children.Add(brandCombo);

            panel.Children.Add(new TextBlock
            {
                Text = "⚠️ Uwaga: marka zostanie usunięta tylko z listy.\nProdukty z tą marką nie zostaną zmienione.",
                FontSize = 13, Foreground = System.Windows.Media.Brushes.DarkOrange,
                TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 16)
            });

            var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
            var btnOk = new Button
            {
                Content = "🗑️  Usuń markę",
                Style = dangerStyle,
                MinWidth = 160, Height = 46
            };
            var btnCancel = new Button
            {
                Content = "Anuluj",
                Style = primaryStyle,
                MinWidth = 110, Height = 46
            };
            btnOk.Click += (s, e) => { dialog.DialogResult = true; };
            btnCancel.Click += (s, e) => { dialog.DialogResult = false; };
            btnPanel.Children.Add(btnOk);
            btnPanel.Children.Add(btnCancel);
            panel.Children.Add(btnPanel);
            dialog.Content = panel;

            if (dialog.ShowDialog() == true)
            {
                string? selected = brandCombo.SelectedItem as string;
                if (string.IsNullOrWhiteSpace(selected)) return;

                DatabaseService.Instance.DeleteBrand(selected);
                LoadBrands();
                if (EditBrand == selected) EditBrand = string.Empty;
                _mainVM.SetStatus($"Usunięto markę: {selected}");
            }
        }

        private void ClearEditForm()
        {
            EditName = string.Empty;
            EditBarcode = string.Empty;
            EditCatalogNumber = string.Empty;
            EditBrand = string.Empty;
            EditPurchasePrice = "0,00";
            EditSalePrice = "0,00";
            EditVatRate = "A";
            EditStockQuantity = "0";
            EditMinStockLevel = "2";
            EditUnit = "szt.";
            EditNotes = string.Empty;
            EditFiscalName = string.Empty;
            EditImagePath = string.Empty;
            EditImageFileName = string.Empty;
            DynamicFields.Clear();
        }

        /// <summary>Otwiera okno wyboru zdjęcia i kopiuje do folderu aplikacji.</summary>
        private void AddImage()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Wybierz zdjęcie produktu",
                Filter = "Zdjęcia|*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.webp|Wszystkie pliki|*.*",
                Multiselect = false
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    // Usuń stare zdjęcie jeśli istnieje
                    if (!string.IsNullOrEmpty(EditImageFileName))
                        ImageService.DeleteImage(EditImageFileName);

                    // Zapisz nowe
                    string fileName = ImageService.SaveImageTemp(dialog.FileName);
                    EditImageFileName = fileName;
                    EditImagePath = ImageService.GetFullPath(fileName);

                    _mainVM.SetStatus($"Dodano zdjęcie: {System.IO.Path.GetFileName(dialog.FileName)}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Błąd dodawania zdjęcia:\n{ex.Message}", "Błąd", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>Usuwa zdjęcie produktu.</summary>
        private void RemoveImage()
        {
            if (string.IsNullOrEmpty(EditImageFileName)) return;

            if (MessageBox.Show("Czy usunąć zdjęcie produktu?", "Potwierdzenie",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                ImageService.DeleteImage(EditImageFileName);
                EditImageFileName = string.Empty;
                EditImagePath = string.Empty;
                _mainVM.SetStatus("Zdjęcie usunięte");
            }
        }
    }
}
