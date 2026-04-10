using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using SklepMotoryzacyjny.Helpers;
using SklepMotoryzacyjny.Models;
using SklepMotoryzacyjny.Services;

namespace SklepMotoryzacyjny.ViewModels
{
    public class InvoicesViewModel : BaseViewModel
    {
        private readonly MainViewModel _mainVM;
        private Invoice? _selectedInvoice;
        private bool _isEditing;
        private bool _isNewInvoice;
        private string _searchText = string.Empty;
        private DateTime _filterFrom = DateTime.Today.AddMonths(-1);
        private DateTime _filterTo = DateTime.Today;
        private string _emailSendStatus = string.Empty;

        // Formularz faktury — dane nabywcy
        private string _editInvoiceNumber = string.Empty;
        private DateTime _editInvoiceDate = DateTime.Today;
        private DateTime? _editDueDate;
        private string _editBuyerName = string.Empty;
        private string _editBuyerNIP = string.Empty;
        private string _editBuyerAddress = string.Empty;
        private string _editBuyerCity = string.Empty;
        private string _editBuyerEmail = string.Empty;
        private string _editPaymentMethod = "Przelew";
        private string _editPaymentDays = "14";
        private string _editNotes = string.Empty;

        // Nowa pozycja do dodania
        private string _newItemName = string.Empty;
        private string _newItemUnit = "szt.";
        private string _newItemQuantity = "1";
        private string _newItemUnitPriceNet = "0,00";
        private string _newItemVatRate = "A";

        public ObservableCollection<Invoice> Invoices { get; } = new();
        public ObservableCollection<InvoiceItemViewModel> EditItems { get; } = new();

        public List<string> PaymentMethods { get; } = new() { "Przelew", "Gotówka", "Karta", "Kompensata" };
        public List<string> VatRates { get; } = new() { "A", "B", "C", "D" };
        public List<string> Units { get; } = new() { "szt.", "l", "kg", "m", "kpl.", "op.", "usł." };

        public Invoice? SelectedInvoice
        {
            get => _selectedInvoice;
            set { SetProperty(ref _selectedInvoice, value); OnPropertyChanged(nameof(CanEditSelected)); }
        }

        public bool IsEditing { get => _isEditing; set => SetProperty(ref _isEditing, value); }
        public bool IsNotEditing => !_isEditing;
        public bool CanEditSelected => SelectedInvoice != null;

        public string SearchText
        {
            get => _searchText;
            set { SetProperty(ref _searchText, value); RefreshInvoices(); }
        }

        public DateTime FilterFrom
        {
            get => _filterFrom;
            set { SetProperty(ref _filterFrom, value); RefreshInvoices(); }
        }

        public DateTime FilterTo
        {
            get => _filterTo;
            set { SetProperty(ref _filterTo, value); RefreshInvoices(); }
        }

        public string EmailSendStatus { get => _emailSendStatus; set => SetProperty(ref _emailSendStatus, value); }

        public string EditFormTitle => _isNewInvoice ? "NOWA FAKTURA" : $"EDYCJA — {_editInvoiceNumber}";

        // Formularz — właściwości
        public string EditInvoiceNumber { get => _editInvoiceNumber; set => SetProperty(ref _editInvoiceNumber, value); }
        public DateTime EditInvoiceDate { get => _editInvoiceDate; set { SetProperty(ref _editInvoiceDate, value); RecalcDueDate(); } }
        public DateTime? EditDueDate { get => _editDueDate; set => SetProperty(ref _editDueDate, value); }
        public string EditBuyerName { get => _editBuyerName; set => SetProperty(ref _editBuyerName, value); }
        public string EditBuyerNIP { get => _editBuyerNIP; set => SetProperty(ref _editBuyerNIP, value); }
        public string EditBuyerAddress { get => _editBuyerAddress; set => SetProperty(ref _editBuyerAddress, value); }
        public string EditBuyerCity { get => _editBuyerCity; set => SetProperty(ref _editBuyerCity, value); }
        public string EditBuyerEmail { get => _editBuyerEmail; set => SetProperty(ref _editBuyerEmail, value); }
        public string EditPaymentMethod { get => _editPaymentMethod; set => SetProperty(ref _editPaymentMethod, value); }
        public string EditPaymentDays { get => _editPaymentDays; set { SetProperty(ref _editPaymentDays, value); RecalcDueDate(); } }
        public string EditNotes { get => _editNotes; set => SetProperty(ref _editNotes, value); }

        // Nowa pozycja
        public string NewItemName { get => _newItemName; set => SetProperty(ref _newItemName, value); }
        public string NewItemUnit { get => _newItemUnit; set => SetProperty(ref _newItemUnit, value); }
        public string NewItemQuantity { get => _newItemQuantity; set => SetProperty(ref _newItemQuantity, value); }
        public string NewItemUnitPriceNet { get => _newItemUnitPriceNet; set => SetProperty(ref _newItemUnitPriceNet, value); }
        public string NewItemVatRate { get => _newItemVatRate; set => SetProperty(ref _newItemVatRate, value); }

        // Sumy
        public decimal TotalNet => EditItems.Sum(x => x.TotalNet);
        public decimal TotalVat => EditItems.Sum(x => x.TotalVat);
        public decimal TotalGross => EditItems.Sum(x => x.TotalGross);
        public string TotalNetDisplay => $"{TotalNet:N2} zł";
        public string TotalVatDisplay => $"{TotalVat:N2} zł";
        public string TotalGrossDisplay => $"{TotalGross:N2} zł";

        // Komendy
        public ICommand AddNewCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand AddItemCommand { get; }
        public ICommand RemoveItemCommand { get; }
        public ICommand GeneratePdfCommand { get; }
        public ICommand SendEmailCommand { get; }
        public ICommand RefreshCommand { get; }

        public InvoicesViewModel(MainViewModel mainVM)
        {
            _mainVM = mainVM;

            AddNewCommand = new RelayCommand(() => StartNew());
            EditCommand = new RelayCommand(() => StartEdit(), () => SelectedInvoice != null);
            SaveCommand = new RelayCommand(() => SaveInvoice());
            CancelCommand = new RelayCommand(() => CancelEdit());
            DeleteCommand = new RelayCommand(() => DeleteInvoice(), () => SelectedInvoice != null);
            AddItemCommand = new RelayCommand(() => AddItem());
            RemoveItemCommand = new RelayCommand<InvoiceItemViewModel>(item => RemoveItem(item));
            GeneratePdfCommand = new RelayCommand(() => GeneratePdf(), () => SelectedInvoice != null);
            SendEmailCommand = new RelayCommand(async () => await SendEmail(), () => SelectedInvoice != null);
            RefreshCommand = new RelayCommand(() => RefreshInvoices());

            RefreshInvoices();
        }

        public void RefreshInvoices()
        {
            try
            {
                var list = DatabaseService.Instance.GetInvoices(FilterFrom, FilterTo,
                    string.IsNullOrWhiteSpace(SearchText) ? null : SearchText);
                Invoices.Clear();
                foreach (var inv in list) Invoices.Add(inv);
                OnPropertyChanged(nameof(CanEditSelected));
            }
            catch (Exception ex) { _mainVM.SetStatus($"Błąd: {ex.Message}"); }
        }

        private void RecalcDueDate()
        {
            if (int.TryParse(EditPaymentDays, out int days))
                EditDueDate = EditInvoiceDate.AddDays(days);
        }

        private void StartNew()
        {
            _isNewInvoice = true;
            var settings = DatabaseService.Instance.LoadSettings();
            EditInvoiceNumber = DatabaseService.Instance.GenerateInvoiceNumber();
            EditInvoiceDate = DateTime.Today;
            EditPaymentDays = settings.InvoicePaymentDays.ToString();
            EditPaymentMethod = "Przelew";
            EditBuyerName = string.Empty;
            EditBuyerNIP = string.Empty;
            EditBuyerAddress = string.Empty;
            EditBuyerCity = string.Empty;
            EditBuyerEmail = string.Empty;
            EditNotes = string.Empty;
            EditItems.Clear();
            RecalcDueDate();
            RecalcTotals();
            IsEditing = true;
            OnPropertyChanged(nameof(IsNotEditing));
            OnPropertyChanged(nameof(EditFormTitle));
        }

        private void StartEdit()
        {
            if (SelectedInvoice == null) return;
            _isNewInvoice = false;
            var inv = SelectedInvoice;

            EditInvoiceNumber = inv.InvoiceNumber;
            EditInvoiceDate = inv.InvoiceDate;
            EditDueDate = inv.DueDate;
            EditBuyerName = inv.BuyerName;
            EditBuyerNIP = inv.BuyerNIP;
            EditBuyerAddress = inv.BuyerAddress;
            EditBuyerCity = inv.BuyerCity;
            EditBuyerEmail = inv.BuyerEmail;
            EditPaymentMethod = inv.PaymentMethod;
            EditPaymentDays = inv.PaymentDays.ToString();
            EditNotes = inv.Notes;

            EditItems.Clear();
            var items = DatabaseService.Instance.GetInvoiceItems(inv.Id);
            foreach (var item in items)
                EditItems.Add(new InvoiceItemViewModel(item, RecalcTotals));

            RecalcTotals();
            IsEditing = true;
            OnPropertyChanged(nameof(IsNotEditing));
            OnPropertyChanged(nameof(EditFormTitle));
        }

        private void AddItem()
        {
            if (string.IsNullOrWhiteSpace(NewItemName)) return;

            if (!decimal.TryParse(NewItemQuantity.Replace(",", "."),
                System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture,
                out decimal qty) || qty <= 0) qty = 1;
            if (!decimal.TryParse(NewItemUnitPriceNet.Replace(",", "."),
                System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture,
                out decimal price)) price = 0;

            var item = new InvoiceItem
            {
                Name = NewItemName.Trim(),
                Unit = NewItemUnit,
                Quantity = qty,
                UnitPriceNet = price,
                VatRate = NewItemVatRate
            };
            EditItems.Add(new InvoiceItemViewModel(item, RecalcTotals));
            RecalcTotals();

            // Reset pól
            NewItemName = string.Empty;
            NewItemQuantity = "1";
            NewItemUnitPriceNet = "0,00";
        }

        private void RemoveItem(InvoiceItemViewModel? item)
        {
            if (item == null) return;
            EditItems.Remove(item);
            RecalcTotals();
        }

        private void RecalcTotals()
        {
            OnPropertyChanged(nameof(TotalNet));
            OnPropertyChanged(nameof(TotalVat));
            OnPropertyChanged(nameof(TotalGross));
            OnPropertyChanged(nameof(TotalNetDisplay));
            OnPropertyChanged(nameof(TotalVatDisplay));
            OnPropertyChanged(nameof(TotalGrossDisplay));
        }

        private void SaveInvoice()
        {
            if (string.IsNullOrWhiteSpace(EditBuyerName))
            {
                MessageBox.Show("Podaj nazwę nabywcy!", "Brak danych", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (EditItems.Count == 0)
            {
                MessageBox.Show("Dodaj co najmniej jedną pozycję!", "Puste pozycje", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int.TryParse(EditPaymentDays, out int payDays);

            var invoice = new Invoice
            {
                Id = _isNewInvoice ? 0 : (SelectedInvoice?.Id ?? 0),
                InvoiceNumber = EditInvoiceNumber,
                InvoiceDate = EditInvoiceDate,
                DueDate = EditDueDate,
                BuyerName = EditBuyerName.Trim(),
                BuyerNIP = EditBuyerNIP.Trim(),
                BuyerAddress = EditBuyerAddress.Trim(),
                BuyerCity = EditBuyerCity.Trim(),
                BuyerEmail = EditBuyerEmail.Trim(),
                PaymentMethod = EditPaymentMethod,
                PaymentDays = payDays > 0 ? payDays : 14,
                TotalNet = TotalNet,
                TotalVat = TotalVat,
                TotalGross = TotalGross,
                Notes = EditNotes.Trim(),
                Items = EditItems.Select(x => x.ToModel()).ToList()
            };

            try
            {
                int id = DatabaseService.Instance.SaveInvoice(invoice);
                _mainVM.SetStatus($"Faktura {invoice.InvoiceNumber} zapisana");
                IsEditing = false;
                OnPropertyChanged(nameof(IsNotEditing));
                RefreshInvoices();
                SelectedInvoice = Invoices.FirstOrDefault(x => x.Id == id);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd zapisu: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelEdit()
        {
            IsEditing = false;
            OnPropertyChanged(nameof(IsNotEditing));
            EditItems.Clear();
        }

        private void DeleteInvoice()
        {
            if (SelectedInvoice == null) return;
            if (MessageBox.Show($"Usunąć fakturę {SelectedInvoice.InvoiceNumber}?",
                "Potwierdzenie", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
            DatabaseService.Instance.DeleteInvoice(SelectedInvoice.Id);
            _mainVM.SetStatus($"Usunięto fakturę {SelectedInvoice.InvoiceNumber}");
            RefreshInvoices();
        }

        private void GeneratePdf()
        {
            if (SelectedInvoice == null) return;
            try
            {
                var inv = SelectedInvoice;
                inv.Items = DatabaseService.Instance.GetInvoiceItems(inv.Id);
                var settings = DatabaseService.Instance.LoadSettings();
                var path = InvoicePdfService.SaveToFile(inv, settings);
                _mainVM.SetStatus($"PDF zapisany: {System.IO.Path.GetFileName(path)}");
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd generowania PDF:\n{ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async System.Threading.Tasks.Task SendEmail()
        {
            if (SelectedInvoice == null) return;

            var inv = SelectedInvoice;
            if (string.IsNullOrWhiteSpace(inv.BuyerEmail))
            {
                MessageBox.Show("Nabywca nie ma podanego adresu e-mail.\nEdytuj fakturę i uzupełnij e-mail.",
                    "Brak e-mail", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            EmailSendStatus = "Wysyłanie...";
            try
            {
                inv.Items = DatabaseService.Instance.GetInvoiceItems(inv.Id);
                var settings = DatabaseService.Instance.LoadSettings();
                var pdfBytes = InvoicePdfService.GeneratePdf(inv, settings);
                await InvoiceEmailService.SendInvoiceAsync(inv, pdfBytes, settings);
                DatabaseService.Instance.MarkInvoiceEmailSent(inv.Id);
                EmailSendStatus = $"Wyslano na {inv.BuyerEmail}";
                _mainVM.SetStatus($"Faktura wysłana na {inv.BuyerEmail}");
                RefreshInvoices();
            }
            catch (Exception ex)
            {
                EmailSendStatus = $"Blad: {ex.Message}";
                MessageBox.Show($"Błąd wysyłania:\n{ex.Message}", "Błąd wysyłania", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    public class InvoiceItemViewModel : BaseViewModel
    {
        private readonly Action _onChanged;
        private string _name;
        private string _unit;
        private decimal _quantity;
        private decimal _unitPriceNet;
        private string _vatRate;

        public InvoiceItemViewModel(InvoiceItem item, Action onChanged)
        {
            _onChanged = onChanged;
            _name = item.Name;
            _unit = item.Unit;
            _quantity = item.Quantity;
            _unitPriceNet = item.UnitPriceNet;
            _vatRate = item.VatRate;
        }

        public string Name
        {
            get => _name;
            set { SetProperty(ref _name, value); _onChanged(); }
        }

        public string Unit
        {
            get => _unit;
            set { SetProperty(ref _unit, value); _onChanged(); }
        }

        public decimal Quantity
        {
            get => _quantity;
            set { SetProperty(ref _quantity, value); OnPropertyChanged(nameof(TotalNet)); OnPropertyChanged(nameof(TotalVat)); OnPropertyChanged(nameof(TotalGross)); _onChanged(); }
        }

        public decimal UnitPriceNet
        {
            get => _unitPriceNet;
            set { SetProperty(ref _unitPriceNet, value); OnPropertyChanged(nameof(TotalNet)); OnPropertyChanged(nameof(TotalVat)); OnPropertyChanged(nameof(TotalGross)); _onChanged(); }
        }

        public string VatRate
        {
            get => _vatRate;
            set { SetProperty(ref _vatRate, value); OnPropertyChanged(nameof(VatDisplay)); OnPropertyChanged(nameof(TotalVat)); OnPropertyChanged(nameof(TotalGross)); _onChanged(); }
        }

        public decimal VatPercent => VatRate switch { "A" => 23m, "B" => 8m, "C" => 5m, "D" => 0m, _ => 23m };
        public string VatDisplay => VatRate switch { "A" => "23%", "B" => "8%", "C" => "5%", "D" => "0%", _ => "23%" };
        public decimal TotalNet => Math.Round(UnitPriceNet * Quantity, 2);
        public decimal TotalVat => Math.Round(TotalNet * VatPercent / 100m, 2);
        public decimal TotalGross => TotalNet + TotalVat;

        public InvoiceItem ToModel() => new()
        {
            Name = Name,
            Unit = Unit,
            Quantity = Quantity,
            UnitPriceNet = UnitPriceNet,
            VatRate = VatRate
        };
    }
}
