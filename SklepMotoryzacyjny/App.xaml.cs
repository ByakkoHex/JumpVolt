using System.Text;
using System.Windows;
using SklepMotoryzacyjny.Services;
using SklepMotoryzacyjny.Models;

namespace SklepMotoryzacyjny
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // Wymagane dla .NET 6+ — rejestruje kodowania takie jak windows-1250
            // potrzebne do komunikacji z kasą fiskalną Novitus
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            // AppConfig musi być wczytany PRZED pierwszym użyciem DatabaseService
            AppConfigService.Load();

            base.OnStartup(e);
            DatabaseService.Instance.Initialize();
        }
    }
}
