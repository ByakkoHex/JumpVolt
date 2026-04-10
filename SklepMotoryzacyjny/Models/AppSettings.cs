namespace SklepMotoryzacyjny.Models
{
    public class AppSettings
    {
        public string CompanyName { get; set; } = "JumpVolt";
        public string CompanyNIP { get; set; } = "";
        public string CompanyAddress { get; set; } = "";
        public string CompanyCity { get; set; } = "";
        public string CompanyPhone { get; set; } = "";

        // Kasa fiskalna - typ połączenia
        /// <summary>COM lub IP</summary>
        public string FiscalConnectionType { get; set; } = "COM";

        // Połączenie COM
        public string FiscalPrinterPort { get; set; } = "COM1";
        public int FiscalPrinterBaudRate { get; set; } = 9600;

        // Połączenie IP (kasa)
        public string FiscalIpAddress { get; set; } = "192.168.1.100";
        public int FiscalIpPort { get; set; } = 6001;

        // Terminal płatniczy - typ połączenia
        /// <summary>COM lub IP</summary>
        public string TerminalConnectionType { get; set; } = "IP";

        // Terminal COM
        public string TerminalComPort { get; set; } = "COM2";
        public int TerminalBaudRate { get; set; } = 115200;

        // Terminal IP
        public string TerminalIpAddress { get; set; } = "192.168.1.101";
        public int TerminalIpPort { get; set; } = 8000;

        // Ogólne
        public bool AutoPrintReceipt { get; set; } = true;
        public string DefaultVatRate { get; set; } = "A";

        // System
        /// <summary>Czy próbować połączyć z kasą automatycznie przy starcie</summary>
        public bool AutoConnectOnStart { get; set; } = true;

        // Aktualizacje
        /// <summary>URL do pliku JSON z informacją o nowej wersji. Pusty = wyłączone.</summary>
        public string UpdateCheckUrl { get; set; } = "";

        // Email / SMTP
        public string SmtpHost { get; set; } = "";
        public int SmtpPort { get; set; } = 587;
        public string SmtpUser { get; set; } = "";
        public string SmtpPassword { get; set; } = "";
        public bool SmtpUseSsl { get; set; } = true;
        public string SmtpFromEmail { get; set; } = "";
        public string SmtpFromName { get; set; } = "";
        public int InvoicePaymentDays { get; set; } = 14;

        // Backup
        public string BackupFolder { get; set; } = "";
        public bool BackupOnStartup { get; set; } = false;
        public int BackupKeepCount { get; set; } = 7;
    }
}
