using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SklepMotoryzacyjny.Services
{
    /// <summary>
    /// Konfiguracja bootstrap — czytana PRZED uruchomieniem bazy danych.
    /// Zapisywana jako config.json w %LocalAppData%\JumpVolt\
    /// </summary>
    public class AppConfig
    {
        [JsonPropertyName("databaseMode")]
        public string DatabaseMode { get; set; } = "Lokalny"; // "Lokalny" | "Sieciowy"

        [JsonPropertyName("networkDatabasePath")]
        public string NetworkDatabasePath { get; set; } = "";
    }

    public static class AppConfigService
    {
        private static readonly string _configDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "JumpVolt");

        private static readonly string _configPath;

        static AppConfigService()
        {
            _configPath = Path.Combine(_configDir, "config.json");
        }

        public static AppConfig Current { get; private set; } = new();

        /// <summary>
        /// Wczytuje config.json z dysku. Wywołać przed pierwszym użyciem DatabaseService!
        /// </summary>
        public static void Load()
        {
            try
            {
                Directory.CreateDirectory(_configDir);
                if (File.Exists(_configPath))
                {
                    var json = File.ReadAllText(_configPath);
                    Current = JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
                }
            }
            catch
            {
                Current = new AppConfig();
            }
        }

        public static void Save(AppConfig config)
        {
            try
            {
                Directory.CreateDirectory(_configDir);
                var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_configPath, json);
                Current = config;
            }
            catch { }
        }

        /// <summary>
        /// Zwraca ścieżkę do aktywnego pliku bazy danych.
        /// Jeśli tryb Sieciowy i ścieżka jest ustawiona — zwraca ścieżkę sieciową.
        /// W przeciwnym razie — lokalny %LocalAppData%\JumpVolt\jumpvolt.db
        /// </summary>
        public static string GetEffectiveDatabasePath()
        {
            if (Current.DatabaseMode == "Sieciowy" && !string.IsNullOrWhiteSpace(Current.NetworkDatabasePath))
                return Current.NetworkDatabasePath;

            return GetLocalDatabasePath();
        }

        public static string GetLocalDatabasePath() =>
            Path.Combine(_configDir, "jumpvolt.db");
    }
}
