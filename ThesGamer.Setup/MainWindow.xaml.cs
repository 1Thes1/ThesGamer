using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Windows;
using File = System.IO.File;

namespace ThesGamer.Setup;

public partial class MainWindow : Window
{
    private static readonly string InstallDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Thes", "ThesGamer");

    private static readonly string ExePath = Path.Combine(InstallDir, "ThesGamer.exe");

    public MainWindow()
    {
        InitializeComponent();
        PathText.Text = "Install path / Путь:\n" + InstallDir;
    }

    private async void Install_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            SetBusy(true, "Preparing… / Подготовка…");
            Directory.CreateDirectory(InstallDir);

            var localExe = FindLocalPayload();
            if (localExe is not null)
            {
                Progress.Value = 40;
                StatusText.Text = "Copying local build… / Копирование локального файла…";
                File.Copy(localExe, ExePath, overwrite: true);
                Progress.Value = 80;
            }
            else
            {
                StatusText.Text = "Downloading latest from GitHub… / Скачивание с GitHub…";
                await DownloadLatestAsync();
            }

            WriteUninstallScript();
            if (DesktopShortcut.IsChecked == true)
                CreateShortcut(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "Thes Gamer.lnk"));
            if (StartMenuShortcut.IsChecked == true)
            {
                var startDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
                    "Programs", "Thes");
                Directory.CreateDirectory(startDir);
                CreateShortcut(Path.Combine(startDir, "Thes Gamer.lnk"));
            }

            Progress.Value = 100;
            StatusText.Text = "Installed! / Установлено!\n" + ExePath;
            SetBusy(false);

            if (LaunchAfter.IsChecked == true)
            {
                Process.Start(new ProcessStartInfo { FileName = ExePath, UseShellExecute = true });
                Close();
            }
        }
        catch (Exception ex)
        {
            StatusText.Text = "Error / Ошибка: " + ex.Message;
            SetBusy(false);
        }
    }

    private void Uninstall_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            foreach (var p in Process.GetProcessesByName("ThesGamer"))
            {
                try { p.Kill(entireProcessTree: true); } catch { /* ignore */ }
            }

            if (Directory.Exists(InstallDir))
                Directory.Delete(InstallDir, true);

            DeleteShortcut(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "Thes Gamer.lnk"));
            DeleteShortcut(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
                "Programs", "Thes", "Thes Gamer.lnk"));

            StatusText.Text = "Uninstalled. / Удалено.";
            Progress.Value = 0;
        }
        catch (Exception ex)
        {
            StatusText.Text = "Error / Ошибка: " + ex.Message;
        }
    }

    private async Task DownloadLatestAsync()
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
        http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("ThesGamer-Setup", "0.4.0"));
        http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

        using var resp = await http.GetAsync("https://api.github.com/repos/1Thes1/ThesGamer/releases/latest");
        resp.EnsureSuccessStatusCode();
        await using var stream = await resp.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        var assets = doc.RootElement.GetProperty("assets");

        string? zipUrl = null;
        string? exeUrl = null;
        foreach (var asset in assets.EnumerateArray())
        {
            var name = asset.GetProperty("name").GetString() ?? "";
            var url = asset.GetProperty("browser_download_url").GetString();
            if (name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) &&
                name.Contains("win-x64", StringComparison.OrdinalIgnoreCase) &&
                !name.Contains("Setup", StringComparison.OrdinalIgnoreCase))
                zipUrl = url;
            if (name.Equals("ThesGamer.exe", StringComparison.OrdinalIgnoreCase))
                exeUrl = url;
        }

        var temp = Path.Combine(Path.GetTempPath(), "ThesGamerSetup_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        try
        {
            if (zipUrl is not null)
            {
                var zipPath = Path.Combine(temp, "app.zip");
                await DownloadAsync(http, zipUrl, zipPath);
                Progress.Value = 70;
                ZipFile.ExtractToDirectory(zipPath, temp, true);
                var found = Directory.EnumerateFiles(temp, "ThesGamer.exe", SearchOption.AllDirectories).FirstOrDefault()
                            ?? throw new FileNotFoundException("ThesGamer.exe missing in release zip.");
                File.Copy(found, ExePath, true);
            }
            else if (exeUrl is not null)
            {
                await DownloadAsync(http, exeUrl, ExePath);
            }
            else
            {
                throw new InvalidOperationException("No downloadable ThesGamer asset found on GitHub Releases.");
            }
        }
        finally
        {
            try { Directory.Delete(temp, true); } catch { /* ignore */ }
        }
    }

    private async Task DownloadAsync(HttpClient http, string url, string path)
    {
        using var resp = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        resp.EnsureSuccessStatusCode();
        var total = resp.Content.Headers.ContentLength ?? -1;
        await using var input = await resp.Content.ReadAsStreamAsync();
        await using var output = File.Create(path);
        var buffer = new byte[81920];
        long readTotal = 0;
        int read;
        while ((read = await input.ReadAsync(buffer)) > 0)
        {
            await output.WriteAsync(buffer.AsMemory(0, read));
            readTotal += read;
            if (total > 0)
                Progress.Value = Math.Min(95, readTotal * 70.0 / total + 10);
        }
    }

    private static string? FindLocalPayload()
    {
        var baseDir = AppContext.BaseDirectory;
        var candidates = new[]
        {
            Path.Combine(baseDir, "ThesGamer.exe"),
            Path.Combine(baseDir, "payload", "ThesGamer.exe"),
            Path.GetFullPath(Path.Combine(baseDir, "..", "ThesGamer.exe"))
        };
        return candidates.FirstOrDefault(File.Exists);
    }

    private void WriteUninstallScript()
    {
        var bat = Path.Combine(InstallDir, "Uninstall.bat");
        File.WriteAllText(bat,
            $"""
            @echo off
            taskkill /F /IM ThesGamer.exe >nul 2>&1
            rmdir /S /Q "{InstallDir}"
            del "%USERPROFILE%\Desktop\Thes Gamer.lnk" >nul 2>&1
            del "%APPDATA%\Microsoft\Windows\Start Menu\Programs\Thes\Thes Gamer.lnk" >nul 2>&1
            """);
    }

    private static void CreateShortcut(string lnkPath)
    {
        try
        {
            // Prefer PowerShell to avoid COM dependency issues
            var ps =
                "$s=(New-Object -ComObject WScript.Shell).CreateShortcut('" + lnkPath.Replace("'", "''") + "');" +
                "$s.TargetPath='" + ExePath.Replace("'", "''") + "';" +
                "$s.WorkingDirectory='" + InstallDir.Replace("'", "''") + "';" +
                "$s.Description='Thes Gamer';$s.Save()";
            Process.Start(new ProcessStartInfo
            {
                FileName = "powershell",
                Arguments = "-NoProfile -Command \"" + ps.Replace("\"", "\\\"") + "\"",
                UseShellExecute = false,
                CreateNoWindow = true
            })?.WaitForExit(8000);
        }
        catch
        {
            // shortcuts are optional
        }
    }

    private static void DeleteShortcut(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch { /* ignore */ }
    }

    private void SetBusy(bool busy, string? status = null)
    {
        if (status is not null) StatusText.Text = status;
        IsEnabled = !busy;
    }
}
