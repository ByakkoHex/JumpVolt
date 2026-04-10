using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SklepMotoryzacyjny.Services
{
    public class UpdateInfo
    {
        [JsonPropertyName("version")]
        public string Version { get; set; } = string.Empty;

        [JsonPropertyName("downloadUrl")]
        public string DownloadUrl { get; set; } = string.Empty;

        [JsonPropertyName("changelog")]
        public string Changelog { get; set; } = string.Empty;

        public bool IsNewerThan(string currentVersion)
        {
            try
            {
                var remote = new Version(Version);
                var current = new Version(currentVersion);
                return remote > current;
            }
            catch
            {
                return false;
            }
        }
    }

    // Wewnętrzne — odpowiedź GitHub Releases API
    file sealed class GitHubRelease
    {
        [JsonPropertyName("tag_name")]
        public string TagName { get; set; } = string.Empty;

        [JsonPropertyName("body")]
        public string Body { get; set; } = string.Empty;

        [JsonPropertyName("assets")]
        public List<GitHubAsset> Assets { get; set; } = [];
    }

    file sealed class GitHubAsset
    {
        [JsonPropertyName("browser_download_url")]
        public string BrowserDownloadUrl { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// Sprawdza dostępność aktualizacji.
    ///
    /// Obsługuje dwa formaty URL:
    ///
    /// 1. GitHub Releases API (zalecane):
    ///    https://api.github.com/repos/ByakkoHex/JumpVolt/releases/latest
    ///
    /// 2. Własny serwer JSON:
    ///    {
    ///      "version": "1.1.0",
    ///      "downloadUrl": "https://example.com/JumpVolt-1.1.0.exe",
    ///      "changelog": "- Poprawka X\n- Nowa funkcja Y"
    ///    }
    /// </summary>
    public class UpdateService
    {
        private static readonly HttpClient _httpClient = new()
        {
            Timeout = TimeSpan.FromSeconds(15),
            DefaultRequestHeaders = { { "User-Agent", "JumpVolt-Updater" } }
        };

        public string CurrentVersion =>
            Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0";

        public string LastError { get; private set; } = string.Empty;

        public async Task<UpdateInfo?> CheckForUpdatesAsync(string updateUrl)
        {
            try
            {
                LastError = string.Empty;
                var json = await _httpClient.GetStringAsync(updateUrl);

                if (IsGitHubApiUrl(updateUrl))
                    return ParseGitHubRelease(json);

                return JsonSerializer.Deserialize<UpdateInfo>(json);
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                return null;
            }
        }

        private static bool IsGitHubApiUrl(string url) =>
            url.Contains("api.github.com", StringComparison.OrdinalIgnoreCase);

        private static UpdateInfo? ParseGitHubRelease(string json)
        {
            var release = JsonSerializer.Deserialize<GitHubRelease>(json);
            if (release == null) return null;

            // tag_name to "v1.2.0" — usuń prefix "v"
            var version = release.TagName.TrimStart('v');

            // Pierwsze assety .exe jako plik do pobrania
            var downloadUrl = release.Assets
                .FirstOrDefault(a => a.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                ?.BrowserDownloadUrl ?? string.Empty;

            return new UpdateInfo
            {
                Version    = version,
                DownloadUrl = downloadUrl,
                Changelog  = release.Body
            };
        }

        public async Task<string?> DownloadInstallerAsync(string downloadUrl, IProgress<int>? progress = null)
        {
            try
            {
                LastError = string.Empty;
                var tempFile = Path.Combine(
                    Path.GetTempPath(),
                    $"JumpVolt_Update_{DateTime.Now:yyyyMMddHHmmss}.exe");

                using var response = await _httpClient.GetAsync(
                    downloadUrl, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength ?? -1;
                long downloadedBytes = 0;
                var buffer = new byte[8192];

                await using var contentStream = await response.Content.ReadAsStreamAsync();
                await using var fileStream = File.Create(tempFile);

                int bytesRead;
                while ((bytesRead = await contentStream.ReadAsync(buffer)) > 0)
                {
                    await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                    downloadedBytes += bytesRead;
                    if (totalBytes > 0)
                        progress?.Report((int)(downloadedBytes * 100 / totalBytes));
                }

                return tempFile;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                return null;
            }
        }
    }
}
