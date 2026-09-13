using System.IO;
using ThesGamer.Models;

namespace ThesGamer.Services;

public static class CleanupService
{
    public static CleanupResult CleanTemp()
    {
        long freed = 0;
        var removed = 0;

        var targets = new[]
        {
            Path.GetTempPath(),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Temp")
        };

        foreach (var dir in targets.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!Directory.Exists(dir))
                continue;

            foreach (var file in Directory.EnumerateFiles(dir, "*", SearchOption.TopDirectoryOnly))
            {
                try
                {
                    var info = new FileInfo(file);
                    var size = info.Length;
                    info.Delete();
                    freed += size;
                    removed++;
                }
                catch
                {
                    // locked files are skipped
                }
            }

            foreach (var sub in Directory.EnumerateDirectories(dir))
            {
                try
                {
                    var size = DirSize(sub);
                    Directory.Delete(sub, true);
                    freed += size;
                    removed++;
                }
                catch
                {
                    // skip locked folders
                }
            }
        }

        return new CleanupResult
        {
            BytesFreed = freed,
            FilesRemoved = removed,
            Message = $"Удалено объектов: {removed}. Освобождено ~{freed / (1024d * 1024d):0.0} МБ."
        };
    }

    public static string OpenDiskCleanup()
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "cleanmgr.exe",
                UseShellExecute = true
            });
            return "Запущена стандартная очистка диска Windows.";
        }
        catch (Exception ex)
        {
            return $"Не удалось открыть cleanmgr: {ex.Message}";
        }
    }

    private static long DirSize(string path)
    {
        long size = 0;
        try
        {
            foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
            {
                try { size += new FileInfo(file).Length; } catch { /* ignore */ }
            }
        }
        catch
        {
            // ignore
        }
        return size;
    }
}
