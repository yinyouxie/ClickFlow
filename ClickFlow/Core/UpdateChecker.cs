using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;

namespace ClickFlow.Core;

internal sealed record UpdateCheckResult(
    bool HasUpdate,
    string CurrentVersion,
    string LatestVersion,
    string ReleaseUrl,
    string? Error);

internal static class UpdateChecker
{
    private const string LatestReleaseApi =
        "https://api.github.com/repos/yinyouxie/ClickFlow/releases/latest";

    private static readonly HttpClient Client = CreateClient();

    public static string CurrentVersion
    {
        get
        {
            var raw = Application.ProductVersion.Split('+')[0];
            if (Version.TryParse(raw, out var version))
            {
                return new Version(
                    version.Major,
                    version.Minor,
                    Math.Max(0, version.Build)).ToString();
            }

            return Normalize(raw);
        }
    }

    public static async Task<UpdateCheckResult> CheckAsync(CancellationToken token = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, LatestReleaseApi);
            request.Headers.UserAgent.ParseAdd("ClickFlow/" + CurrentVersion);
            request.Headers.Accept.ParseAdd("application/vnd.github+json");

            using var response = await Client.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                token);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync(token);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: token);
            var root = document.RootElement;
            var tag = root.TryGetProperty("tag_name", out var tagValue)
                ? tagValue.GetString() ?? string.Empty
                : string.Empty;
            var url = root.TryGetProperty("html_url", out var urlValue)
                ? urlValue.GetString() ?? string.Empty
                : string.Empty;
            var latest = Normalize(tag);
            var hasUpdate = CompareVersions(latest, CurrentVersion) > 0;

            return new UpdateCheckResult(
                hasUpdate,
                CurrentVersion,
                latest,
                string.IsNullOrWhiteSpace(url)
                    ? "https://github.com/yinyouxie/ClickFlow/releases"
                    : url,
                null);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new UpdateCheckResult(
                false,
                CurrentVersion,
                CurrentVersion,
                "https://github.com/yinyouxie/ClickFlow/releases",
                ex.Message);
        }
    }

    public static void OpenReleasePage(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            url = "https://github.com/yinyouxie/ClickFlow/releases";
        }

        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch
        {
            // Opening a browser is best effort.
        }
    }

    private static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(12) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("ClickFlow");
        return client;
    }

    private static string Normalize(string value)
    {
        var text = (value ?? string.Empty).Trim();
        if (text.StartsWith('v') || text.StartsWith('V'))
        {
            text = text[1..];
        }

        var dash = text.IndexOf('-');
        if (dash > 0)
        {
            text = text[..dash];
        }

        return text;
    }

    private static int CompareVersions(string left, string right)
    {
        if (!Version.TryParse(left, out var a) || !Version.TryParse(right, out var b))
        {
            return string.Compare(left, right, StringComparison.OrdinalIgnoreCase);
        }

        return a.CompareTo(b);
    }
}
