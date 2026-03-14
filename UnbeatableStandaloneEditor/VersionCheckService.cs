using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using osu.Framework.Logging;

namespace UnbeatableStandaloneEditor;


public class VersionCheckService
{
    private const string GitHubApiUrl = "https://api.github.com/repos/ErikGXDev/UnbeatableStandaloneEditor/releases/latest";
    private const int TimeoutSeconds = 10;

    private static readonly HttpClient HttpClient = new();

    static VersionCheckService()
    {
        HttpClient.Timeout = TimeSpan.FromSeconds(TimeoutSeconds);
        // GitHub API requires a User-Agent header
        HttpClient.DefaultRequestHeaders.Add("User-Agent", "UnbeatableStandaloneEditor");
    }

    public static async Task<ReleaseInfo?> CheckForUpdateAsync()
    {
        // Fake update when in debug mode for testing
#if DEBUG
        await Task.Delay(1000); // Simulate network delay
        return new ReleaseInfo
        {
            Version = "999.0.0",
            ReleaseUrl = "https://github.com/ErikGXDev/UnbeatableStandaloneEditor/releases",
        };
#endif


        try
        {
            Logger.Log("Checking for updates...", LoggingTarget.Network);

            var response = await HttpClient.GetAsync(GitHubApiUrl);
            if (!response.IsSuccessStatusCode)
            {
                Logger.Log($"Failed to check for updates: HTTP {response.StatusCode}", LoggingTarget.Network);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var tagName = root.GetProperty("tag_name").GetString();
            var htmlUrl = root.GetProperty("html_url").GetString();

            if (tagName == null)
                return null;

            // Remove 'v' prefix for version comparison
            var latestVersion = tagName.TrimStart('v', 'V');
            var currentVersion = AppVersion.Current;

            // Check if there's a newer version available
            if (AppVersion.Compare(currentVersion, latestVersion) < 0)
            {
                Logger.Log($"Update available: {currentVersion} -> {latestVersion}", LoggingTarget.Network);

                return new ReleaseInfo
                {
                    Version = latestVersion,
                    // htmlUrl wouldve been in here but perhaps we want to list all releases
                    ReleaseUrl = "https://github.com/ErikGXDev/UnbeatableStandaloneEditor/releases"
                };
            }

            Logger.Log($"Already on latest version: {currentVersion}", LoggingTarget.Network);
            return null;
        }
        catch (HttpRequestException ex)
        {
            Logger.Log($"Network error while checking for updates: {ex.Message}", LoggingTarget.Network);
            return null;
        }
        catch (TaskCanceledException ex)
        {
            Logger.Log($"Update check timed out: {ex.Message}", LoggingTarget.Network);
            return null;
        }
        catch (Exception ex)
        {
            Logger.Log($"Unexpected error checking for updates: {ex.Message}", LoggingTarget.Network);
            return null;
        }
    }

    public class ReleaseInfo
    {
        public string Version { get; set; } = "";
        public string ReleaseUrl { get; set; } = "";
    }
}

