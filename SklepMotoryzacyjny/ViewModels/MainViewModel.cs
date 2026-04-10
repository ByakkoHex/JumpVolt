using System.Windows.Input;
using SklepMotoryzacyjny.Helpers;
using SklepMotoryzacyjny.Services;

namespace SklepMotoryzacyjny.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private BaseViewModel _currentViewModel;
        private string _currentViewName = "Sprzedaż";
        private string _statusMessage = "Gotowy";
        private string _connectionStatus = "Kasa: niepołączona";
        private bool _isFiscalConnected;

        public SalesViewModel SalesVM { get; }
        public ProductsViewModel ProductsVM { get; }
        public HistoryViewModel HistoryVM { get; }
        public ReturnsViewModel ReturnsVM { get; }
        public SettingsViewModel SettingsVM { get; }
        public HelpViewModel HelpVM { get; }
        public TestsViewModel TestsVM { get; }
        public InvoicesViewModel InvoicesVM { get; }

        public ICommand ShowSalesCommand { get; }
        public ICommand ShowProductsCommand { get; }
        public ICommand ShowHistoryCommand { get; }
        public ICommand ShowReturnsCommand { get; }
        public ICommand ShowSettingsCommand { get; }
        public ICommand ShowHelpCommand { get; }
        public ICommand ShowTestsCommand { get; }
        public ICommand ShowInvoicesCommand { get; }

        public BaseViewModel CurrentViewModel
        {
            get => _currentViewModel;
            set => SetProperty(ref _currentViewModel, value);
        }

        public string CurrentViewName
        {
            get => _currentViewName;
            set => SetProperty(ref _currentViewName, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public string ConnectionStatus
        {
            get => _connectionStatus;
            set => SetProperty(ref _connectionStatus, value);
        }

        public bool IsFiscalConnected
        {
            get => _isFiscalConnected;
            set => SetProperty(ref _isFiscalConnected, value);
        }

        public string TodayDate => DateTime.Now.ToString("dddd, d MMMM yyyy",
            new System.Globalization.CultureInfo("pl-PL"));

        public MainViewModel()
        {
            SalesVM = new SalesViewModel(this);
            ProductsVM = new ProductsViewModel(this);
            HistoryVM = new HistoryViewModel(this);
            ReturnsVM = new ReturnsViewModel(this);
            SettingsVM = new SettingsViewModel(this);
            HelpVM = new HelpViewModel();
            TestsVM = new TestsViewModel(this);
            InvoicesVM = new InvoicesViewModel(this);

            _currentViewModel = SalesVM;

            ShowSalesCommand = new RelayCommand(() => NavigateTo(SalesVM, "Sprzedaż"));
            ShowProductsCommand = new RelayCommand(() => NavigateTo(ProductsVM, "Magazyn"));
            ShowHistoryCommand = new RelayCommand(() => NavigateTo(HistoryVM, "Historia"));
            ShowReturnsCommand = new RelayCommand(() => NavigateTo(ReturnsVM, "Zwroty"));
            ShowSettingsCommand = new RelayCommand(() => NavigateTo(SettingsVM, "Ustawienia"));
            ShowHelpCommand = new RelayCommand(() => NavigateTo(HelpVM, "Instrukcja"));
            ShowTestsCommand = new RelayCommand(() => NavigateTo(TestsVM, "Testy"));
            ShowInvoicesCommand = new RelayCommand(() => NavigateTo(InvoicesVM, "Faktury"));

            TryConnectFiscal();
        }

        private void NavigateTo(BaseViewModel vm, string name)
        {
            CurrentViewModel = vm;
            CurrentViewName = name;
            if (vm is ProductsViewModel pvm) pvm.RefreshProducts();
            if (vm is HistoryViewModel hvm) hvm.RefreshSales();
            if (vm is SalesViewModel svm) svm.RefreshProducts();
            if (vm is ReturnsViewModel rvm) rvm.RefreshAll();
            if (vm is InvoicesViewModel ivm) ivm.RefreshInvoices();
        }

        public void SetStatus(string message) => StatusMessage = message;

        private void TryConnectFiscal()
        {
            try
            {
                var settings = DatabaseService.Instance.LoadSettings();

                if (!settings.AutoConnectOnStart)
                {
                    IsFiscalConnected = false;
                    ConnectionStatus = "Kasa: auto-połączenie wyłączone";
                    return;
                }

                var fiscal = new NovitusFiscalService();
                if (fiscal.ConnectFromSettings(settings))
                {
                    IsFiscalConnected = true;
                    ConnectionStatus = $"Kasa: {fiscal.ConnectionInfo} ✓";
                    fiscal.Disconnect();
                }
                else
                {
                    IsFiscalConnected = false;
                    ConnectionStatus = $"Kasa: niepołączona ({fiscal.LastError})";
                }
            }
            catch (Exception ex)
            {
                IsFiscalConnected = false;
                ConnectionStatus = $"Kasa: błąd ({ex.Message})";
            }
        }

        public void RefreshFiscalStatus() => TryConnectFiscal();
    }
}
