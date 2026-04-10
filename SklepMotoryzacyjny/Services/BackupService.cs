using SklepMotoryzacyjny.Models;

namespace SklepMotoryzacyjny.Services
{
    public static class BackupService
    {
        public static string LastBackupResult { get; private set; } = string.Empty;

        public static bool DoBackup(AppSettings settings)
        {
            if (string.IsNullOrWhiteSpace(settings.BackupFolder)) return false;
            try
            {
                string dbPath = AppConfigService.GetEffectiveDatabasePath();
                if (!System.IO.File.Exists(dbPath)) return false;

                System.IO.Directory.CreateDirectory(settings.BackupFolder);
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string fileName = $"jumpvolt_backup_{timestamp}.db";
                string destPath = System.IO.Path.Combine(settings.BackupFolder, fileName);
                System.IO.File.Copy(dbPath, destPath, overwrite: true);

                CleanOldBackups(settings.BackupFolder, settings.BackupKeepCount);
                LastBackupResult = $"Kopia z {DateTime.Now:dd.MM.yyyy HH:mm}: {fileName}";
                return true;
            }
            catch (Exception ex)
            {
                LastBackupResult = $"Blad kopii: {ex.Message}";
                return false;
            }
        }

        private static void CleanOldBackups(string folder, int keepCount)
        {
            if (keepCount <= 0) keepCount = 7;
            var files = System.IO.Directory.GetFiles(folder, "jumpvolt_backup_*.db")
                .OrderByDescending(f => f)
                .Skip(keepCount)
                .ToList();
            foreach (var f in files)
                try { System.IO.File.Delete(f); } catch { }
        }

        public static string GetLastBackupInfo(string folder)
        {
            if (string.IsNullOrWhiteSpace(folder) || !System.IO.Directory.Exists(folder))
                return "Brak skonfigurowanego folderu kopii";
            var files = System.IO.Directory.GetFiles(folder, "jumpvolt_backup_*.db")
                .OrderByDescending(f => f).ToArray();
            if (files.Length == 0) return "Brak kopii zapasowych";
            var fi = new System.IO.FileInfo(files[0]);
            return $"Ostatnia kopia: {fi.LastWriteTime:dd.MM.yyyy HH:mm} ({fi.Name})";
        }
    }
}
