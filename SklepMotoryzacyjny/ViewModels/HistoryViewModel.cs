using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using SklepMotoryzacyjny.Helpers;
using SklepMotoryzacyjny.Models;
using SklepMotoryzacyjny.Services;

namespace SklepMotoryzacyjny.ViewModels
{
    /// <summary>
    /// ViewModel historii sprzedaży i raportów.
    /// </summary>
    public class HistoryViewModel : BaseViewModel
    {
        private readonly MainViewModel _mainVM;
        private DateTime _dateFrom;
        private DateTime _dateTo;
        private string _searchText = string.Empty;
        private Sale? _selectedSale;

        public ObservableCollection<Sale> Sales { get; } = new();
        public ObservableCollection<SaleItem> SelectedSaleItems { get; } = new();

        // Komendy
        public ICommand SearchCommand { get; }
        public ICommand TodayCommand { get; }
        public ICommand ThisWeekCommand { get; }
        public ICommand ThisMonthCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand PrintXReportCommand { get; }
        public ICommand PrintZReportCommand { get; }

        // Właściwości
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

        public string SearchText
        {
            get => _searchText;
            set => SetProperty(ref _searchText, value);
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

        // Statystyki
        public string TotalSalesDisplay => $"{Sales.Where(s => !s.IsCancelled).Sum(s => s.TotalAmount):N2} zł";
        public int SalesCount => Sales.Count(s => !s.IsCancelled);
        public string AverageSaleDisplay => SalesCount > 0
            ? $"{Sales.Where(s => !s.IsCancelled).Sum(s => s.TotalAmount) / SalesCount:N2} zł"
            : "0,00 zł";

        public HistoryViewModel(MainViewModel mainVM)
        {
            _mainVM = mainVM;
            _dateFrom = DateTime.Today;
            _dateTo = DateTime.Today;

            SearchCommand = new RelayCommand(() => RefreshSales());
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
            RefreshCommand = new RelayCommand(() => RefreshSales());
            PrintXReportCommand = new RelayCommand(() => PrintFiscalReport(false));
            PrintZReportCommand = new RelayCommand(() => PrintFiscalReport(true));
        }

        public void RefreshSales()
        {
            try
            {
                var results = DatabaseService.Instance.GetSales(DateFrom, DateTo,
                    string.IsNullOrWhiteSpace(SearchText) ? null : SearchText);

                Sales.Clear();
                foreach (var sale in results)
                {
                    Sales.Add(sale);
                }

                OnPropertyChanged(nameof(TotalSalesDisplay));
                OnPropertyChanged(nameof(SalesCount));
                OnPropertyChanged(nameof(AverageSaleDisplay));
            }
            catch (Exception ex)
            {
                _mainVM.SetStatus($"Błąd ładowania historii: {ex.Message}");
            }
        }

        private void LoadSaleItems()
        {
            SelectedSaleItems.Clear();
            if (SelectedSale == null) return;

            foreach (var item in SelectedSale.Items)
            {
                SelectedSaleItems.Add(item);
            }
        }

        private void PrintFiscalReport(bool isZReport)
        {
            string reportType = isZReport ? "Z (zerujący)" : "X (podglądowy)";
            
            if (isZReport)
            {
                var confirm = MessageBox.Show(
                    "UWAGA!\n\nRaport Z jest nieodwracalny i zamyka dzień fiskalny.\n\n" +
                    "Czy na pewno wydrukować raport dobowy Z?",
                    "Raport Z - Potwierdzenie",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);
                if (confirm != MessageBoxResult.Yes) return;
            }

            try
            {
                var settings = DatabaseService.Instance.LoadSettings();
                using var fiscal = new NovitusFiscalService();

                if (!fiscal.ConnectFromSettings(settings))
                {
                    MessageBox.Show($"Nie można połączyć z kasą.\n{fiscal.LastError}",
                        "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                bool success = isZReport ? fiscal.PrintZReport() : fiscal.PrintXReport();

                if (success)
                {
                    MessageBox.Show($"Raport {reportType} został wydrukowany.",
                        "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
                    _mainVM.SetStatus($"Wydrukowano raport {reportType}");
                }
                else
                {
                    MessageBox.Show($"Błąd drukowania raportu {reportType}.\n{fiscal.LastError}",
                        "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
