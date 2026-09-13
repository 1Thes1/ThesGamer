using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

namespace ThesGamer.Services;

public static class AppPaths
{
    public static string InstallDir =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Thes", "ThesGamer");

    public static string ExePath => Path.Combine(InstallDir, "ThesGamer.exe");
}

public static class UpdateService
{
    private const string RepoApi = "https://api.github.com/repos/1Thes1/ThesGamer/releases/latest";
    private static readonly HttpClient Http = CreateClient();

    private static HttpClient CreateClient()
    {
        var c = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
        c.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("ThesGamer", "0.4.0"));
        c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        return c;
    }

    public sealed record UpdateInfo(
        bool Ok,
        bool UpdateAvailable,
        string LatestTag,
        string Message,
        string? HtmlUrl,
        string? DownloadUrl,
        string? AssetName);

    public static async Task<UpdateInfo> CheckAsync(string currentVersion, CancellationToken ct = default)
    {
        try
        {
            using var resp = await Http.GetAsync(RepoApi, ct);
            if (!resp.IsSuccessStatusCode)
                return Fail(Loc.T("update_fail"));

            await using var stream = await resp.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
            var root = doc.RootElement;
            var tag = root.GetProperty("tag_name").GetString()?.TrimStart('v', 'V') ?? "";
            var html = root.TryGetProperty("html_url", out var u) ? u.GetString() : null;

            string? download = null;
            string? assetName = null;
            if (root.TryGetProperty("assets", out var assets))
            {
                foreach (var asset in assets.EnumerateArray())
                {
                    var name = asset.GetProperty("name").GetString() ?? "";
                    var url = asset.GetProperty("browser_download_url").GetString();
                    if (name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) &&
                        name.Contains("win-x64", StringComparison.OrdinalIgnoreCase) &&
                        !name.Contains("Setup", StringComparison.OrdinalIgnoreCase))
                    {
                        download = url;
                        assetName = name;
                        break;
                    }
                }

                if (download is null)
                {
                    foreach (var asset in assets.EnumerateArray())
                    {
                        var name = asset.GetProperty("name").GetString() ?? "";
                        var url = asset.GetProperty("browser_download_url").GetString();
                        if (name.Equals("ThesGamer.exe", StringComparison.OrdinalIgnoreCase) ||
                            (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) &&
                             name.Contains("ThesGamer", StringComparison.OrdinalIgnoreCase) &&
                             !name.Contains("Setup", StringComparison.OrdinalIgnoreCase)))
                        {
                            download = url;
                            assetName = name;
                            break;
                        }
                    }
                }
            }

            var newer = Compare(Normalize(tag), Normalize(currentVersion)) > 0;
            return newer
                ? new UpdateInfo(true, true, tag, Loc.Tf("update_avail", tag), html, download, assetName)
                : new UpdateInfo(true, false, tag, Loc.T("update_ok"), html, download, assetName);
        }
        catch
        {
            return Fail(Loc.T("update_fail"));
        }
    }

    public static async Task<(bool Ok, string Message)> DownloadAndApplyAsync(
        UpdateInfo info,
        string targetExePath,
        IProgress<double>? progress = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(info.DownloadUrl))
            return (false, Loc.T("update_fail"));

        var tempRoot = Path.Combine(Path.GetTempPath(), "ThesGamerUpdate_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);

        try
        {
            var downloadPath = Path.Combine(tempRoot, info.AssetName ?? "update.bin");
            await DownloadFileAsync(info.DownloadUrl!, downloadPath, progress, ct);

            string newExe;
            if (downloadPath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                var extractDir = Path.Combine(tempRoot, "extract");
                ZipFile.ExtractToDirectory(downloadPath, extractDir, overwriteFiles: true);
                newExe = Directory.EnumerateFiles(extractDir, "ThesGamer.exe", SearchOption.AllDirectories).FirstOrDefault()
                         ?? throw new FileNotFoundException("ThesGamer.exe not found in update zip.");
            }
            else
            {
                newExe = downloadPath;
            }

            var targetDir = Path.GetDirectoryName(targetExePath)!;
            Directory.CreateDirectory(targetDir);

            var staging = Path.Combine(targetDir, "ThesGamer.exe.new");
            File.Copy(newExe, staging, overwrite: true);

            var bat = Path.Combine(tempRoot, "apply-update.bat");
            var log = Path.Combine(tempRoot, "update.log");
            await File.WriteAllTextAsync(bat,
                $"""
                @echo off
                setlocal
                ping 127.0.0.1 -n 2 >nul
                :retry
                move /Y "{staging}" "{targetExePath}" >> "{log}" 2>&1
                if errorlevel 1 (
                  ping 127.0.0.1 -n 2 >nul
                  goto retry
                )
                start "" "{targetExePath}"
                endlocal
                """, ct);

            Process.Start(new ProcessStartInfo
            {
                FileName = bat,
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                WorkingDirectory = tempRoot
            });

            return (true, Loc.T("update_applying"));
        }
        catch (Exception ex)
        {
            try { Directory.Delete(tempRoot, true); } catch { /* ignore */ }
            return (false, Loc.T("update_fail") + " " + ex.Message);
        }
    }

    private static async Task DownloadFileAsync(string url, string path, IProgress<double>? progress, CancellationToken ct)
    {
        using var resp = await Http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
        resp.EnsureSuccessStatusCode();
        var total = resp.Content.Headers.ContentLength ?? -1L;
        await using var input = await resp.Content.ReadAsStreamAsync(ct);
        await using var output = File.Create(path);
        var buffer = new byte[81920];
        long readTotal = 0;
        int read;
        while ((read = await input.ReadAsync(buffer.AsMemory(0, buffer.Length), ct)) > 0)
        {
            await output.WriteAsync(buffer.AsMemory(0, read), ct);
            readTotal += read;
            if (total > 0)
                progress?.Report(readTotal * 100.0 / total);
        }
    }

    private static UpdateInfo Fail(string message) =>
        new(false, false, "", message, null, null, null);

    private static Version Normalize(string v)
    {
        var clean = new string(v.Where(ch => char.IsDigit(ch) || ch == '.').ToArray());
        return Version.TryParse(clean, out var ver) ? ver : new Version(0, 0, 0);
    }

    private static int Compare(Version a, Version b) => a.CompareTo(b);
}
