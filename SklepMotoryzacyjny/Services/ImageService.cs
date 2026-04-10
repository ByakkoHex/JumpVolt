using System.IO;

namespace SklepMotoryzacyjny.Services
{
    /// <summary>
    /// Zarządza zdjęciami produktów.
    /// Zdjęcia przechowywane w: %LOCALAPPDATA%\JumpVolt\Images\
    /// </summary>
    public static class ImageService
    {
        private static readonly string ImagesFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "JumpVolt", "Images");

        /// <summary>Upewnij się, że folder istnieje.</summary>
        public static void EnsureFolder()
        {
            Directory.CreateDirectory(ImagesFolder);
        }

        /// <summary>
        /// Kopiuje zdjęcie do folderu aplikacji.
        /// Zwraca nazwę pliku (bez ścieżki).
        /// </summary>
        public static string SaveImage(string sourcePath, int productId)
        {
            EnsureFolder();

            string ext = Path.GetExtension(sourcePath).ToLowerInvariant();
            if (string.IsNullOrEmpty(ext)) ext = ".jpg";

            // Nazwa: produkt_ID_timestamp.ext
            string fileName = $"produkt_{productId}_{DateTime.Now:yyyyMMddHHmmss}{ext}";
            string destPath = Path.Combine(ImagesFolder, fileName);

            File.Copy(sourcePath, destPath, overwrite: true);

            return fileName;
        }

        /// <summary>
        /// Zapisuje zdjęcie dla nowego produktu (ID jeszcze nieznane).
        /// Po zapisie produktu wywołaj RenameImage().
        /// </summary>
        public static string SaveImageTemp(string sourcePath)
        {
            EnsureFolder();

            string ext = Path.GetExtension(sourcePath).ToLowerInvariant();
            if (string.IsNullOrEmpty(ext)) ext = ".jpg";

            string fileName = $"temp_{Guid.NewGuid():N}{ext}";
            string destPath = Path.Combine(ImagesFolder, fileName);

            File.Copy(sourcePath, destPath, overwrite: true);

            return fileName;
        }

        /// <summary>Pełna ścieżka do zdjęcia.</summary>
        public static string GetFullPath(string fileName)
        {
            return Path.Combine(ImagesFolder, fileName);
        }

        /// <summary>Usuwa plik zdjęcia.</summary>
        public static void DeleteImage(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return;
            try
            {
                string path = Path.Combine(ImagesFolder, fileName);
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch { /* nie blokuj jeśli plik zajęty */ }
        }

        /// <summary>Sprawdza czy zdjęcie istnieje.</summary>
        public static bool ImageExists(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return false;
            return File.Exists(Path.Combine(ImagesFolder, fileName));
        }
    }
}
