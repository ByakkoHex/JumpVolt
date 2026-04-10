using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using SklepMotoryzacyjny.Helpers;
using SklepMotoryzacyjny.Services;

namespace SklepMotoryzacyjny.ViewModels
{
    public class TestResult
    {
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // "OK", "BŁĄD", "INFO"
        public string Message { get; set; } = string.Empty;
        public DateTime Time { get; set; } = DateTime.Now;

        public string Icon => Status switch
        {
            "OK"    => "✓",
            "BŁĄD"  => "✗",
            "INFO"  => "ℹ",
            _       => "•"
        };

        public string TimeDisplay => Time.ToString("HH:mm:ss");
    }

    public class TestsViewModel : BaseViewModel
    {
        private readonly MainViewModel _main;
        private bool _isBusy;
        private string _updateStatus = string.Empty;
        private string? _pendingDownloadUrl;
        private string? _pendingVersion;
        private int _downloadProgress;
        private bool _isDownloading;

        public ObservableCollection<TestResult> Results { get; } = new();

        public bool IsBusy
        {
            get => _isBusy;
            set { SetProperty(ref _isBusy, value); RelayCommand.RaiseCanExecuteChanged(); }
        }

        public string UpdateStatus
        {
            get => _updateStatus;
            set => SetProperty(ref _updateStatus, value);
        }

        public bool HasUpdate => _pendingDownloadUrl != null;

        public int DownloadProgress
        {
            get => _downloadProgress;
            set => SetProperty(ref _downloadProgress, value);
        }

        public bool IsDownloading
        {
            get => _isDownloading;
            set => SetProperty(ref _isDownloading, value);
        }

        // ===== Informacje o aplikacji =====

        public string AppVersion =>
            Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0";

        public string DatabasePath => AppConfigService.GetEffectiveDatabasePath();
        public string DatabaseMode => AppConfigService.Current.DatabaseMode;

        public string DatabaseSize
        {
            get
            {
                try
                {
                    var fi = new FileInfo(DatabasePath);
                    if (!fi.Exists) return "brak pliku";
                    var kb = fi.Length / 1024.0;
                    return kb < 1024 ? $"{kb:F1} KB" : $"{kb / 1024:F2} MB";
                }
                catch { return "?"; }
            }
        }

        // ===== Komendy =====

        public ICommand TestConnectionCommand { get; }
        public ICommand TestNonFiscalPrintCommand { get; }
        public ICommand TestDrawerCommand { get; }
        public ICommand ClearResultsCommand { get; }
        public ICommand CheckUpdatesCommand { get; }
        public ICommand DownloadUpdateCommand { get; }

        public TestsViewModel(MainViewModel main)
        {
            _main = main;

            TestConnectionCommand     = new RelayCommand(async () => await TestConnectionAsync(),     () => !IsBusy);
            TestNonFiscalPrintCommand = new RelayCommand(async () => await TestNonFiscalPrintAsync(), () => !IsBusy);
            TestDrawerCommand         = new RelayCommand(async () => await TestDrawerAsync(),         () => !IsBusy);
            ClearResultsCommand       = new RelayCommand(() => Results.Clear());
            CheckUpdatesCommand       = new RelayCommand(async () => await CheckUpdatesAsync(),       () => !IsBusy);
            DownloadUpdateCommand     = new RelayCommand(async () => await DownloadUpdateAsync(),     () => !IsBusy && HasUpdate);
        }

        private void AddResult(string name, string status, string message)
        {
            Application.Current.Dispatcher.Invoke(() =>
                Results.Insert(0, new TestResult { Name = name, Status = status, Message = message }));
        }

        // ====================================================
        // TESTY KASY FISKALNEJ
        // ====================================================

        private async Task TestConnectionAsync()
        {
            IsBusy = true;
            _main.SetStatus("Testowanie połączenia z kasą...");

            await Task.Run(() =>
            {
                try
                {
                    var settings = DatabaseService.Instance.LoadSettings();
                    using var fiscal = new NovitusFiscalService();

                    if (!fiscal.ConnectFromSettings(settings))
                    {
                        AddResult("Połączenie z kasą", "BŁĄD", $"Nie można połączyć: {fiscal.LastError}");
                        return;
                    }

                    bool ok = fiscal.TestConnection();

                    if (ok)
                        AddResult("Połączenie z kasą", "OK", $"Kasa odpowiada — {fiscal.ConnectionInfo}");
                    else
                        AddResult("Połączenie z kasą", "BŁĄD", $"Podłączona, lecz nie odpowiada: {fiscal.LastError}");
                }
                catch (Exception ex)
                {
                    AddResult("Połączenie z kasą", "BŁĄD", ex.Message);
                }
            });

            _main.SetStatus("Gotowy");
            IsBusy = false;
        }

        private async Task TestNonFiscalPrintAsync()
        {
            IsBusy = true;
            _main.SetStatus("Drukowanie strony testowej...");

            await Task.Run(() =>
            {
                try
                {
                    var settings = DatabaseService.Instance.LoadSettings();
                    using var fiscal = new NovitusFiscalService();

                    if (!fiscal.ConnectFromSettings(settings))
                    {
                        AddResult("Wydruk testowy", "BŁĄD", $"Brak połączenia: {fiscal.LastError}");
                        return;
                    }

                    var lines = new List<string>
                    {
                        "================================",
                        "        WYDRUK TESTOWY",
                        "     DOKUMENT NIEFISKALNY",
                        "================================",
                        $"Data: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                        $"Firma: {settings.CompanyName}",
                    };
                    if (!string.IsNullOrEmpty(settings.CompanyNIP))
                        lines.Add($"NIP: {settings.CompanyNIP}");
                    lines.AddRange(new[]
                    {
                        "--------------------------------",
                        "  Produkt testowy A    1 x 10.00",
                        "  Produkt testowy B    2 x 25.00",
                        "  Produkt testowy C    1 x  5.00",
                        "--------------------------------",
                        "  RAZEM:               65.00 PLN",
                        "================================",
                        " Ten wydruk NIE jest dowodem",
                        " sprzedazy. Dokument testowy.",
                        "================================",
                        $"  Wersja JumpVolt: {AppVersion}",
                        "================================",
                    });

                    bool ok = fiscal.PrintNonFiscalDocument(lines);

                    if (ok)
                        AddResult("Wydruk testowy", "OK", "Wydruk niefiskalny wysłany do kasy");
                    else
                        AddResult("Wydruk testowy", "BŁĄD", fiscal.LastError);
                }
                catch (Exception ex)
                {
                    AddResult("Wydruk testowy", "BŁĄD", ex.Message);
                }
            });

            _main.SetStatus("Gotowy");
            IsBusy = false;
        }

        private async Task TestDrawerAsync()
        {
            IsBusy = true;
            _main.SetStatus("Otwieranie szuflady...");

            await Task.Run(() =>
            {
                try
                {
                    var settings = DatabaseService.Instance.LoadSettings();
                    using var fiscal = new NovitusFiscalService();

                    if (!fiscal.ConnectFromSettings(settings))
                    {
                        AddResult("Test szuflady", "BŁĄD", $"Brak połączenia: {fiscal.LastError}");
                        return;
                    }

                    bool ok = fiscal.OpenCashDrawer();

                    if (ok)
                        AddResult("Test szuflady", "OK", "Komenda otwarcia szuflady wysłana");
                    else
                        AddResult("Test szuflady", "BŁĄD", fiscal.LastError);
                }
                catch (Exception ex)
                {
                    AddResult("Test szuflady", "BŁĄD", ex.Message);
                }
            });

            _main.SetStatus("Gotowy");
            IsBusy = false;
        }

        // ====================================================
        // AKTUALIZACJE
        // ====================================================

        private async Task CheckUpdatesAsync()
        {
            var settings = DatabaseService.Instance.LoadSettings();

            if (string.IsNullOrWhiteSpace(settings.UpdateCheckUrl))
            {
                UpdateStatus = "Brak adresu serwera aktualizacji.\nSkonfiguruj go w: Ustawienia → Aktualizacje.";
                return;
            }

            IsBusy = true;
            UpdateStatus = "Sprawdzanie...";

            var svc = new UpdateService();
            var info = await svc.CheckForUpdatesAsync(settings.UpdateCheckUrl);

            if (info == null)
            {
                UpdateStatus = $"Błąd połączenia z serwerem:\n{svc.LastError}";
                AddResult("Sprawdzanie aktualizacji", "BŁĄD", svc.LastError);
            }
            else if (info.IsNewerThan(AppVersion))
            {
                _pendingVersion = info.Version;
                _pendingDownloadUrl = info.DownloadUrl;
                OnPropertyChanged(nameof(HasUpdate));
                RelayCommand.RaiseCanExecuteChanged();

                var changelog = string.IsNullOrEmpty(info.Changelog) ? "" : $"\n\nZmiany:\n{info.Changelog}";
                UpdateStatus = $"Dostępna nowa wersja: {info.Version}{changelog}";
                AddResult("Sprawdzanie aktualizacji", "OK", $"Dostępna v{info.Version} (aktualna: v{AppVersion})");
            }
            else
            {
                _pendingDownloadUrl = null;
                _pendingVersion = null;
                OnPropertyChanged(nameof(HasUpdate));
                RelayCommand.RaiseCanExecuteChanged();
                UpdateStatus = $"Masz najnowszą wersję (v{AppVersion})";
                AddResult("Sprawdzanie aktualizacji", "OK", "Brak nowych aktualizacji");
            }

            IsBusy = false;
        }

        private async Task DownloadUpdateAsync()
        {
            if (_pendingDownloadUrl == null) return;

            IsBusy = true;
            IsDownloading = true;
            DownloadProgress = 0;

            var svc = new UpdateService();
            var progress = new Progress<int>(p => { DownloadProgress = p; });
            var tempFile = await svc.DownloadInstallerAsync(_pendingDownloadUrl, progress);

            IsDownloading = false;
            IsBusy = false;

            if (tempFile == null)
            {
                AddResult("Pobieranie aktualizacji", "BŁĄD", $"Nie pobrano: {svc.LastError}");
                return;
            }

            AddResult("Pobieranie aktualizacji", "OK", $"Pobrano do: {Path.GetFileName(tempFile)}");

            var result = MessageBox.Show(
                $"Aktualizacja JumpVolt v{_pendingVersion} pobrana.\n\nUruchomić instalator?\n\nAplikacja zostanie zamknięta.",
                "Zainstalować aktualizację?",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                Process.Start(new ProcessStartInfo(tempFile) { UseShellExecute = true });
                Application.Current.Shutdown();
            }
        }
    }
}
