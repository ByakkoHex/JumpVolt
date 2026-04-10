using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using SklepMotoryzacyjny.Helpers;
using SklepMotoryzacyjny.Models;
using SklepMotoryzacyjny.Services;
using System.Threading.Tasks;

namespace SklepMotoryzacyjny.ViewModels
{
    public class SettingsViewModel : BaseViewModel
    {
        private readonly MainViewModel _mainVM;

        // Dane firmy
        private string _companyName = "JumpVolt";
        private string _companyNIP = "";
        private string _companyAddress = "";
        private string _companyCity = "";
        private string _companyPhone = "";

        // Kasa fiskalna
        private string _fiscalConnectionType = "COM";
        private string _fiscalPort = "COM1";
        private int _fiscalBaudRate = 9600;
        private string _fiscalIpAddress = "192.168.1.100";
        private string _fiscalIpPort = "6001";
        private bool _autoPrintReceipt = true;
        private string _connectionTestResult = "";

        // Terminal
        private string _terminalConnectionType = "IP";
        private string _terminalComPort = "COM2";
        private int _terminalBaudRate = 115200;
        private string _terminalIpAddress = "192.168.1.101";
        private string _terminalIpPort = "8000";
        private string _terminalTestResult = "";

        // VAT
        private string _defaultVatRate = "A";

        // SMTP
        private string _smtpHost = "";
        private string _smtpPort = "587";
        private string _smtpUser = "";
        private string _smtpPassword = "";
        private bool _smtpUseSsl = true;
        private string _smtpFromEmail = "";
        private string _smtpFromName = "";
        private string _invoicePaymentDays = "14";
        private string _smtpTestResult = "";

        // Baza danych (AppConfig)
        private string _databaseMode = "Lokalny";
        private string _networkDatabasePath = "";

        // System
        private bool _autoConnectOnStart = true;
        private bool _runWithWindows;
        private string _updateCheckUrl = "";

        // Kolekcje
        public ObservableCollection<string> AvailablePorts { get; } = new();
        public List<string> ConnectionTypes { get; } = new() { "COM", "IP" };
        public List<int> BaudRates { get; } = new() { 9600, 19200, 38400, 57600, 115200 };
        public List<string> VatRates { get; } = new() { "A", "B", "C", "D" };

        // Baza danych
        public string DatabaseMode
        {
            get => _databaseMode;
            set { SetProperty(ref _databaseMode, value); OnPropertyChanged(nameof(IsLocalDatabase)); OnPropertyChanged(nameof(IsNetworkDatabase)); OnPropertyChanged(nameof(CurrentDatabaseInfo)); }
        }
        public string NetworkDatabasePath { get => _networkDatabasePath; set => SetProperty(ref _networkDatabasePath, value); }
        public bool IsLocalDatabase
        {
            get => DatabaseMode == "Lokalny";
            set { if (value) DatabaseMode = "Lokalny"; }
        }
        public bool IsNetworkDatabase
        {
            get => DatabaseMode == "Sieciowy";
            set { if (value) DatabaseMode = "Sieciowy"; }
        }
        public string CurrentDatabaseInfo =>
            AppConfigService.Current.DatabaseMode == "Sieciowy" && !string.IsNullOrEmpty(AppConfigService.Current.NetworkDatabasePath)
                ? $"Sieciowy: {AppConfigService.Current.NetworkDatabasePath}"
                : $"Lokalny: {AppConfigService.GetLocalDatabasePath()}";

        // System
        public bool AutoConnectOnStart { get => _autoConnectOnStart; set => SetProperty(ref _autoConnectOnStart, value); }
        public bool RunWithWindows { get => _runWithWindows; set => SetProperty(ref _runWithWindows, value); }
        public string UpdateCheckUrl { get => _updateCheckUrl; set => SetProperty(ref _updateCheckUrl, value); }

        // SMTP właściwości
        public string SmtpHost { get => _smtpHost; set => SetProperty(ref _smtpHost, value); }
        public string SmtpPort { get => _smtpPort; set => SetProperty(ref _smtpPort, value); }
        public string SmtpUser { get => _smtpUser; set => SetProperty(ref _smtpUser, value); }
        public string SmtpPassword { get => _smtpPassword; set => SetProperty(ref _smtpPassword, value); }
        public bool SmtpUseSsl { get => _smtpUseSsl; set => SetProperty(ref _smtpUseSsl, value); }
        public string SmtpFromEmail { get => _smtpFromEmail; set => SetProperty(ref _smtpFromEmail, value); }
        public string SmtpFromName { get => _smtpFromName; set => SetProperty(ref _smtpFromName, value); }
        public string InvoicePaymentDays { get => _invoicePaymentDays; set => SetProperty(ref _invoicePaymentDays, value); }
        public string SmtpTestResult { get => _smtpTestResult; set => SetProperty(ref _smtpTestResult, value); }

        // Komendy
        public ICommand SaveCommand { get; }
        public ICommand TestFiscalCommand { get; }
        public ICommand TestTerminalCommand { get; }
        public ICommand RefreshPortsCommand { get; }
        public ICommand OpenCashDrawerCommand { get; }
        public ICommand CopyToNetworkCommand { get; }
        public ICommand TestSmtpCommand { get; }

        // Właściwości firmy
        public string CompanyName { get => _companyName; set => SetProperty(ref _companyName, value); }
        public string CompanyNIP { get => _companyNIP; set => SetProperty(ref _companyNIP, value); }
        public string CompanyAddress { get => _companyAddress; set => SetProperty(ref _companyAddress, value); }
        public string CompanyCity { get => _companyCity; set => SetProperty(ref _companyCity, value); }
        public string CompanyPhone { get => _companyPhone; set => SetProperty(ref _companyPhone, value); }

        // Kasa fiskalna
        public string FiscalConnectionType
        {
            get => _fiscalConnectionType;
            set { SetProperty(ref _fiscalConnectionType, value); OnPropertyChanged(nameof(IsFiscalCom)); OnPropertyChanged(nameof(IsFiscalIp)); }
        }
        public string FiscalPort { get => _fiscalPort; set => SetProperty(ref _fiscalPort, value); }
        public int FiscalBaudRate { get => _fiscalBaudRate; set => SetProperty(ref _fiscalBaudRate, value); }
        public string FiscalIpAddress { get => _fiscalIpAddress; set => SetProperty(ref _fiscalIpAddress, value); }
        public string FiscalIpPort { get => _fiscalIpPort; set => SetProperty(ref _fiscalIpPort, value); }
        public bool AutoPrintReceipt { get => _autoPrintReceipt; set => SetProperty(ref _autoPrintReceipt, value); }
        public string ConnectionTestResult { get => _connectionTestResult; set => SetProperty(ref _connectionTestResult, value); }

        public bool IsFiscalCom => FiscalConnectionType == "COM";
        public bool IsFiscalIp => FiscalConnectionType == "IP";

        // Terminal
        public string TerminalConnectionType
        {
            get => _terminalConnectionType;
            set { SetProperty(ref _terminalConnectionType, value); OnPropertyChanged(nameof(IsTerminalCom)); OnPropertyChanged(nameof(IsTerminalIp)); }
        }
        public string TerminalComPort { get => _terminalComPort; set => SetProperty(ref _terminalComPort, value); }
        public int TerminalBaudRate { get => _terminalBaudRate; set => SetProperty(ref _terminalBaudRate, value); }
        public string TerminalIpAddress { get => _terminalIpAddress; set => SetProperty(ref _terminalIpAddress, value); }
        public string TerminalIpPort { get => _terminalIpPort; set => SetProperty(ref _terminalIpPort, value); }
        public string TerminalTestResult { get => _terminalTestResult; set => SetProperty(ref _terminalTestResult, value); }

        public bool IsTerminalCom => TerminalConnectionType == "COM";
        public bool IsTerminalIp => TerminalConnectionType == "IP";

        // VAT
        public string DefaultVatRate { get => _defaultVatRate; set => SetProperty(ref _defaultVatRate, value); }

        public SettingsViewModel(MainViewModel mainVM)
        {
            _mainVM = mainVM;

            SaveCommand = new RelayCommand(() => SaveSettings());
            TestFiscalCommand = new RelayCommand(() => TestFiscalConnection());
            TestTerminalCommand = new RelayCommand(() => TestTerminalConnection());
            RefreshPortsCommand = new RelayCommand(() => RefreshAvailablePorts());
            OpenCashDrawerCommand = new RelayCommand(() => OpenDrawer());
            CopyToNetworkCommand = new RelayCommand(() => CopyDatabaseToNetwork());
            TestSmtpCommand = new RelayCommand(async () => await TestSmtp());

            LoadSettings();
            RefreshAvailablePorts();
        }

        private void LoadSettings()
        {
            try
            {
                var s = DatabaseService.Instance.LoadSettings();
                CompanyName = s.CompanyName;
                CompanyNIP = s.CompanyNIP;
                CompanyAddress = s.CompanyAddress;
                CompanyCity = s.CompanyCity;
                CompanyPhone = s.CompanyPhone;

                FiscalConnectionType = s.FiscalConnectionType;
                FiscalPort = s.FiscalPrinterPort;
                FiscalBaudRate = s.FiscalPrinterBaudRate;
                FiscalIpAddress = s.FiscalIpAddress;
                FiscalIpPort = s.FiscalIpPort.ToString();

                TerminalConnectionType = s.TerminalConnectionType;
                TerminalComPort = s.TerminalComPort;
                TerminalBaudRate = s.TerminalBaudRate;
                TerminalIpAddress = s.TerminalIpAddress;
                TerminalIpPort = s.TerminalIpPort.ToString();

                AutoPrintReceipt = s.AutoPrintReceipt;
                DefaultVatRate = s.DefaultVatRate;
                AutoConnectOnStart = s.AutoConnectOnStart;
                UpdateCheckUrl = s.UpdateCheckUrl;
                RunWithWindows = IsInStartup();

                SmtpHost = s.SmtpHost;
                SmtpPort = s.SmtpPort.ToString();
                SmtpUser = s.SmtpUser;
                SmtpPassword = s.SmtpPassword;
                SmtpUseSsl = s.SmtpUseSsl;
                SmtpFromEmail = s.SmtpFromEmail;
                SmtpFromName = s.SmtpFromName;
                InvoicePaymentDays = s.InvoicePaymentDays.ToString();

                DatabaseMode = AppConfigService.Current.DatabaseMode;
                NetworkDatabasePath = AppConfigService.Current.NetworkDatabasePath;
            }
            catch (Exception ex) { _mainVM.SetStatus($"Błąd: {ex.Message}"); }
        }

        private void SaveSettings()
        {
            try
            {
                int.TryParse(FiscalIpPort, out int fip);
                int.TryParse(TerminalIpPort, out int tip);

                int.TryParse(SmtpPort, out int smtpPortNum);
                int.TryParse(InvoicePaymentDays, out int invoicePayDays);

                var s = new AppSettings
                {
                    CompanyName = CompanyName,
                    CompanyNIP = CompanyNIP,
                    CompanyAddress = CompanyAddress,
                    CompanyCity = CompanyCity,
                    CompanyPhone = CompanyPhone,

                    FiscalConnectionType = FiscalConnectionType,
                    FiscalPrinterPort = FiscalPort,
                    FiscalPrinterBaudRate = FiscalBaudRate,
                    FiscalIpAddress = FiscalIpAddress,
                    FiscalIpPort = fip > 0 ? fip : 6001,

                    TerminalConnectionType = TerminalConnectionType,
                    TerminalComPort = TerminalComPort,
                    TerminalBaudRate = TerminalBaudRate,
                    TerminalIpAddress = TerminalIpAddress,
                    TerminalIpPort = tip > 0 ? tip : 8000,

                    AutoPrintReceipt = AutoPrintReceipt,
                    DefaultVatRate = DefaultVatRate,
                    AutoConnectOnStart = AutoConnectOnStart,
                    UpdateCheckUrl = UpdateCheckUrl,

                    SmtpHost = SmtpHost,
                    SmtpPort = smtpPortNum > 0 ? smtpPortNum : 587,
                    SmtpUser = SmtpUser,
                    SmtpPassword = SmtpPassword,
                    SmtpUseSsl = SmtpUseSsl,
                    SmtpFromEmail = SmtpFromEmail,
                    SmtpFromName = SmtpFromName,
                    InvoicePaymentDays = invoicePayDays > 0 ? invoicePayDays : 14
                };

                DatabaseService.Instance.SaveSettings(s);
                SetStartup(RunWithWindows);
                _mainVM.RefreshFiscalStatus();

                // Zapisz AppConfig (baza danych) — wymaga restartu jeśli zmieniono
                var currentCfg = AppConfigService.Current;
                bool dbChanged = currentCfg.DatabaseMode != DatabaseMode ||
                                 currentCfg.NetworkDatabasePath != NetworkDatabasePath;

                AppConfigService.Save(new AppConfig
                {
                    DatabaseMode = DatabaseMode,
                    NetworkDatabasePath = NetworkDatabasePath
                });
                OnPropertyChanged(nameof(CurrentDatabaseInfo));

                string startupInfo = RunWithWindows ? "\n✓ Aplikacja uruchomi się razem z Windows." : "";
                string dbInfo = dbChanged ? "\n\n⚠️ Zmieniono tryb bazy danych.\nUruchom ponownie aplikację, aby zmiana weszła w życie." : "";
                MessageBox.Show($"Ustawienia zapisane!{startupInfo}{dbInfo}", "Zapisano", MessageBoxButton.OK, MessageBoxImage.Information);
                _mainVM.SetStatus("Ustawienia zapisane");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RefreshAvailablePorts()
        {
            AvailablePorts.Clear();
            foreach (var port in NovitusFiscalService.GetAvailablePorts())
                AvailablePorts.Add(port);
            if (AvailablePorts.Count == 0) AvailablePorts.Add("COM1");
        }

        private void TestFiscalConnection()
        {
            ConnectionTestResult = "Łączenie...";

            try
            {
                using var fiscal = new NovitusFiscalService();
                bool connected;

                if (FiscalConnectionType == "IP")
                {
                    int.TryParse(FiscalIpPort, out int port);
                    connected = fiscal.ConnectTcp(FiscalIpAddress, port > 0 ? port : 6001);
                }
                else
                {
                    connected = fiscal.Connect(FiscalPort, FiscalBaudRate);
                }

                if (!connected)
                {
                    ConnectionTestResult = $"❌ BŁĄD: {fiscal.LastError}";
                    return;
                }

                if (fiscal.TestConnection())
                {
                    ConnectionTestResult = $"✅ POŁĄCZONO! ({fiscal.ConnectionInfo})";
                    MessageBox.Show($"Połączenie z kasą nawiązane!\n\n{fiscal.ConnectionInfo}",
                        "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    ConnectionTestResult = $"⚠️ Brak odpowiedzi: {fiscal.LastError}";
                    MessageBox.Show($"Kasa nie odpowiada.\n\n{fiscal.LastError}\n\nSprawdź:\n• Czy kasa jest włączona\n• Czy kabel jest podłączony\n• Czy adres/port jest prawidłowy",
                        "Brak odpowiedzi", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex) { ConnectionTestResult = $"❌ BŁĄD: {ex.Message}"; }
        }

        private void TestTerminalConnection()
        {
            TerminalTestResult = "Łączenie z terminalem...";
            try
            {
                if (TerminalConnectionType == "IP")
                {
                    int.TryParse(TerminalIpPort, out int port);
                    using var client = new System.Net.Sockets.TcpClient();
                    var task = client.ConnectAsync(TerminalIpAddress, port > 0 ? port : 8000);
                    if (task.Wait(3000))
                    {
                        TerminalTestResult = $"✅ Terminal dostępny ({TerminalIpAddress}:{port})";
                        MessageBox.Show($"Terminal płatniczy dostępny!\n\n{TerminalIpAddress}:{port}",
                            "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        TerminalTestResult = $"⚠️ Timeout - terminal nie odpowiada";
                    }
                }
                else
                {
                    TerminalTestResult = "Test COM terminala — sprawdź fizyczne połączenie";
                }
            }
            catch (Exception ex) { TerminalTestResult = $"❌ BŁĄD: {ex.Message}"; }
        }

        private void OpenDrawer()
        {
            try
            {
                using var fiscal = new NovitusFiscalService();
                var settings = DatabaseService.Instance.LoadSettings();
                if (fiscal.ConnectFromSettings(settings))
                {
                    fiscal.OpenCashDrawer();
                    _mainVM.SetStatus("Szuflada otwarta");
                }
                else
                    _mainVM.SetStatus($"Błąd: {fiscal.LastError}");
            }
            catch (Exception ex) { _mainVM.SetStatus($"Błąd: {ex.Message}"); }
        }

        private void CopyDatabaseToNetwork()
        {
            if (string.IsNullOrWhiteSpace(NetworkDatabasePath))
            {
                MessageBox.Show("Podaj ścieżkę sieciową do pliku bazy danych.",
                    "Brak ścieżki", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var localPath = AppConfigService.GetLocalDatabasePath();
            if (!System.IO.File.Exists(localPath))
            {
                MessageBox.Show("Lokalna baza danych nie istnieje lub nie była jeszcze utworzona.",
                    "Brak lokalnej bazy", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"Skopiować lokalną bazę danych na:\n{NetworkDatabasePath}\n\n" +
                "Jeśli plik już istnieje na serwerze — zostanie nadpisany.",
                "Potwierdzenie kopiowania", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;

            try
            {
                var dir = System.IO.Path.GetDirectoryName(NetworkDatabasePath);
                if (!string.IsNullOrEmpty(dir))
                    System.IO.Directory.CreateDirectory(dir);

                System.IO.File.Copy(localPath, NetworkDatabasePath, overwrite: true);
                MessageBox.Show("Baza danych skopiowana na serwer.\n\nPo zapisaniu ustawień uruchom ponownie aplikację.",
                    "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
                _mainVM.SetStatus("Baza skopiowana na serwer");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd kopiowania:\n{ex.Message}\n\nSprawdź:\n• Czy ścieżka sieciowa jest dostępna\n• Czy masz uprawnienia do zapisu",
                    "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task TestSmtp()
        {
            SmtpTestResult = "Łączenie z serwerem SMTP...";
            try
            {
                int.TryParse(SmtpPort, out int port);
                var settings = new AppSettings
                {
                    SmtpHost = SmtpHost,
                    SmtpPort = port > 0 ? port : 587,
                    SmtpUser = SmtpUser,
                    SmtpPassword = SmtpPassword,
                    SmtpUseSsl = SmtpUseSsl
                };
                await InvoiceEmailService.TestConnectionAsync(settings);
                SmtpTestResult = $"Polaczono z {SmtpHost}:{SmtpPort}";
                MessageBox.Show($"Polaczenie SMTP nawiazane!\n{SmtpHost}:{SmtpPort}", "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                SmtpTestResult = $"Blad: {ex.Message}";
            }
        }

        // ====================================================
        // REJESTR WINDOWS — autostart
        // ====================================================

        private const string StartupRegistryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        private const string AppRegistryName = "JumpVolt";

        private static bool IsInStartup()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(StartupRegistryKey, false);
                return key?.GetValue(AppRegistryName) != null;
            }
            catch { return false; }
        }

        private static void SetStartup(bool enable)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(StartupRegistryKey, true);
                if (key == null) return;

                if (enable)
                {
                    string? exePath = Environment.ProcessPath;
                    if (!string.IsNullOrEmpty(exePath))
                        key.SetValue(AppRegistryName, $"\"{exePath}\"");
                }
                else
                {
                    key.DeleteValue(AppRegistryName, throwOnMissingValue: false);
                }
            }
            catch { /* brak uprawnień lub inny błąd rejestru */ }
        }
    }
}
