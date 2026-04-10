using System.Windows;
using System.Windows.Controls;
using SklepMotoryzacyjny.ViewModels;

namespace SklepMotoryzacyjny.Views
{
    public partial class SettingsView : UserControl
    {
        public SettingsView()
        {
            InitializeComponent();
        }

        private SettingsViewModel? VM => DataContext as SettingsViewModel;

        private void FiscalCom_Checked(object sender, RoutedEventArgs e) 
        { if (VM != null) VM.FiscalConnectionType = "COM"; }
        
        private void FiscalIp_Checked(object sender, RoutedEventArgs e)
        { if (VM != null) VM.FiscalConnectionType = "IP"; }

        private void TerminalCom_Checked(object sender, RoutedEventArgs e)
        { if (VM != null) VM.TerminalConnectionType = "COM"; }
        
        private void TerminalIp_Checked(object sender, RoutedEventArgs e)
        { if (VM != null) VM.TerminalConnectionType = "IP"; }
    }
}
