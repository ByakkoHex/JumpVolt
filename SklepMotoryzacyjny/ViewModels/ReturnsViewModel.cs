using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using SklepMotoryzacyjny.Helpers;
using SklepMotoryzacyjny.Models;
using SklepMotoryzacyjny.Services;

namespace SklepMotoryzacyjny.ViewModels
{
    /// <summary>
    /// ViewModel pojedynczej pozycji zwrotu — pozwala zaznaczać i ustalać ilość do zwrotu.
    /// </summary>
    public class ReturnSaleItemVM : BaseViewModel
    {
        private bool _isSelected;
        private int _returnQuantity;

        public SaleItem OriginalItem { get; }

        public int ProductId => OriginalItem.ProductId;
        public string ProductName => OriginalItem.ProductName;
        public string Barcode => OriginalItem.Barcode;
        public int OriginalQuantity => OriginalItem.Quantity;
        public decimal UnitPrice => OriginalItem.UnitPrice;
        public string VatRate => OriginalItem.VatRate;
        public string UnitPriceDisplay => OriginalItem.UnitPriceDisplay;

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (SetProperty(ref _isSelected, value))
                {
                    OnPropertyChanged(nameof(ReturnValue));
                    OnPropertyChanged(nameof(ReturnValueDisplay));
                    NotifyChanged?.Invoke();
                }
            }
        }

        public int ReturnQuantity
        {
            get => _returnQuantity;
            set
            {
                int clamped = Math.Max(1, Math.Min(OriginalQuantity, value));
                if (SetProperty(ref _returnQuantity, clamped))
                {
                    OnPropertyChanged(nameof(ReturnValue));
                    OnPropertyChanged(nameof(ReturnValueDisplay));
                    NotifyChanged?.Invoke();
                }
            }
        }

        public decimal ReturnValue => IsSelected ? UnitPrice * ReturnQuantity : 0;
        public string ReturnValueDisplay => IsSelected ? $"{ReturnValue:N2} zł" : "—";

        /// <summary>Callback wywoływany przy zmianie IsSelected lub ReturnQuantity.</summary>
        public Action? NotifyChanged { get; set; }

        public ReturnSaleItemVM(SaleItem item)
        {
            OriginalItem = item;
            _returnQuantity = item.Quantity;
        }
    }

    /// <summary>
    /// ViewModel zakładki Zwroty.
    /// </summary>
    public class ReturnsViewModel : BaseViewModel
    {
        private readonly MainViewModel _mainVM;
        private string _searchText = string.Empty;
        private DateTime _dateFrom;
        private DateTime _dateTo;
        private Sale? _selectedSale;
        private Return? _selectedReturn;
        private string _selectedRefundMethod = "Gotówka";

        public ObservableCollection<Sale> Sales { get; } = new();
        public ObservableCollection<ReturnSaleItemVM> ReturnItems { get; } = new();
        public ObservableCollection<Return> RecentReturns { get; } = new();

        public List<string> RefundMethods { get; } = new() { "Gotówka", "Karta", "Przelew" };

        public ICommand SearchSalesCommand { get; }
        public ICommand TodayCommand { get; }
        public ICommand ThisWeekCommand { get; }
        public ICommand ThisMonthCommand { get; }
        public ICommand ProcessReturnCommand { get; }
        public ICommand SelectAllCommand { get; }
        public ICommand DeselectAllCommand { get; }

        public string SearchText
        {
            get => _searchText;
            set => SetProperty(ref _searchText, value);
        }

        public DateTime DateFrom
        {
            get => _dateFrom;
            set { if (SetProperty(ref _dateFrom, value)) RefreshSales(); }
        }

        public DateTime DateTo
        {
            get => _dateTo;
            set { if (SetProperty(ref _dateTo, value)) RefreshSales(); }
        }

        public Sale? SelectedSale
        {
            get => _selectedSale;
            set
            {
                if (SetProperty(ref _selectedSale, value))
                    LoadSaleItems();
            }
        }

        public Return? SelectedReturn
        {
            get => _selectedReturn;
            set => SetProperty(ref _selectedReturn, value);
        }

        public string SelectedRefundMethod
        {
            get => _selectedRefundMethod;
            set => SetProperty(ref _selectedRefundMethod, value);
        }

        public decimal ReturnTotal => ReturnItems.Where(i => i.IsSelected).Sum(i => i.ReturnValue);
        public string ReturnTotalDisplay => $"{ReturnTotal:N2} zł";
        public bool HasSelectedItems => ReturnItems.Any(i => i.IsSelected);
        public bool CanProcessReturn => SelectedSale != null && HasSelectedItems;

        // Statystyki zwrotów
        public string RecentReturnsTotalDisplay => $"{RecentReturns.Sum(r => r.TotalAmount):N2} zł";
        public int RecentReturnsCount => RecentReturns.Count;

        public ReturnsViewModel(MainViewModel mainVM)
        {
            _mainVM = mainVM;
            _dateFrom = DateTime.Today;
            _dateTo = DateTime.Today;

            SearchSalesCommand = new RelayCommand(() => RefreshSales());
            TodayCommand = new RelayCommand(() => { DateFrom = DateTime.Today; DateTo = DateTime.Today; });
            ThisWeekCommand = new RelayCommand(() =>
            {
                var today = DateTime.Today;
                int diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
                DateFrom = today.AddDays(-diff);
                DateTo = today;
            });
            ThisMonthCommand = new RelayCommand(() =>
            {
                DateFrom = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                DateTo = DateTime.Today;
            });
            ProcessReturnCommand = new RelayCommand(() => ProcessReturn(), () => CanProcessReturn);
            SelectAllCommand = new RelayCommand(() => { foreach (var i in ReturnItems) i.IsSelected = true; UpdateTotals(); });
            DeselectAllCommand = new RelayCommand(() => { foreach (var i in ReturnItems) i.IsSelected = false; UpdateTotals(); });
        }

        public void RefreshAll()
        {
            RefreshSales();
            LoadRecentReturns();
        }

        public void RefreshSales()
        {
            try
            {
                var results = DatabaseService.Instance.GetSales(
                    DateFrom, DateTo,
                    string.IsNullOrWhiteSpace(SearchText) ? null : SearchText);

                // Pokaż tylko niezanulowane sprzedaże
                results = results.Where(s => !s.IsCancelled).ToList();

                Sales.Clear();
                foreach (var s in results) Sales.Add(s);

                SelectedSale = null;
            }
            catch (Exception ex) { _mainVM.SetStatus($"Błąd ładowania sprzedaży: {ex.Message}"); }
        }

        private void LoadSaleItems()
        {
            ReturnItems.Clear();
            if (SelectedSale == null) { UpdateTotals(); return; }

            foreach (var item in SelectedSale.Items)
            {
                var vm = new ReturnSaleItemVM(item) { NotifyChanged = UpdateTotals };
                ReturnItems.Add(vm);
            }

            UpdateTotals();
        }

        private void ProcessReturn()
        {
            if (SelectedSale == null) return;

            var selected = ReturnItems.Where(i => i.IsSelected).ToList();
            if (selected.Count == 0)
            {
                MessageBox.Show("Zaznacz co najmniej jeden produkt do zwrotu.",
                    "Brak zaznaczenia", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string itemList = string.Join("\n", selected.Select(i =>
                $"  • {i.ProductName}  ×{i.ReturnQuantity}  = {i.ReturnValueDisplay}"));

            if (MessageBox.Show(
                $"Potwierdzenie zwrotu towaru\n\n" +
                $"Paragon: {SelectedSale.ReceiptNumber}\n\n" +
                $"Zwracane pozycje:\n{itemList}\n\n" +
                $"Łączna kwota zwrotu:  {ReturnTotalDisplay}\n" +
                $"Forma zwrotu:  {SelectedRefundMethod}\n\n" +
                $"Stan magazynu zostanie przywrócony.",
                "Zwrot towaru", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            try
            {
                var ret = new Return
                {
                    OriginalSaleId = SelectedSale.Id,
                    OriginalReceiptNumber = SelectedSale.ReceiptNumber,
                    ReturnNumber = DatabaseService.Instance.GenerateReturnNumber(),
                    ReturnDate = DateTime.Now,
                    TotalAmount = ReturnTotal,
                    RefundMethod = SelectedRefundMethod,
                    Items = selected.Select(i => new ReturnItem
                    {
                        ProductId = i.ProductId,
                        ProductName = i.ProductName,
                        Barcode = i.Barcode,
                        Quantity = i.ReturnQuantity,
                        UnitPrice = i.UnitPrice,
                        VatRate = i.VatRate
                    }).ToList()
                };

                DatabaseService.Instance.SaveReturn(ret);

                MessageBox.Show(
                    $"Zwrot zarejestrowany!\n\n" +
                    $"Nr zwrotu: {ret.ReturnNumber}\n" +
                    $"Kwota: {ReturnTotalDisplay}\n" +
                    $"Forma zwrotu: {SelectedRefundMethod}\n\n" +
                    $"Stan magazynu zaktualizowany.",
                    "Zwrot zakończony", MessageBoxButton.OK, MessageBoxImage.Information);

                _mainVM.SetStatus($"Zwrot {ret.ReturnNumber} — {ReturnTotalDisplay}");

                SelectedSale = null;
                ReturnItems.Clear();
                RefreshSales();
                LoadRecentReturns();
                UpdateTotals();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd podczas zwrotu: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadRecentReturns()
        {
            try
            {
                var returns = DatabaseService.Instance.GetReturns(DateTime.Today.AddDays(-30));
                RecentReturns.Clear();
                foreach (var r in returns) RecentReturns.Add(r);

                OnPropertyChanged(nameof(RecentReturnsTotalDisplay));
                OnPropertyChanged(nameof(RecentReturnsCount));
            }
            catch { }
        }

        private void UpdateTotals()
        {
            OnPropertyChanged(nameof(ReturnTotal));
            OnPropertyChanged(nameof(ReturnTotalDisplay));
            OnPropertyChanged(nameof(HasSelectedItems));
            OnPropertyChanged(nameof(CanProcessReturn));
        }
    }
}
