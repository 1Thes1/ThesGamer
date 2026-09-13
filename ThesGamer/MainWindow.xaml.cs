using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using ThesGamer.Services;

namespace ThesGamer;

public partial class MainWindow : Window
{
    private readonly DispatcherTimer _uiTimer;
    private readonly AppSettings _settings;
    private DateTime _lastAutoRamUtc = DateTime.MinValue;

    public MainWindow()
    {
        InitializeComponent();
        _settings = SettingsStore.Load();

        _uiTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _uiTimer.Tick += (_, _) =>
        {
            RefreshOverview(silent: true);
            MaybeAutoFreeRam();
        };

        Loaded += (_, _) =>
        {
            TryEnableDarkTitleBar();
            ApplySettingsToUi();
            RefreshOverview(silent: false);
            RefreshMemory();
            UpdateNavIcons();
            _uiTimer.Start();
        };
        Closed += (_, _) =>
        {
            _uiTimer.Stop();
            PersistSettings();
        };
    }

    private void ApplySettingsToUi()
    {
        AutoRamCheck.IsChecked = _settings.AutoRamEnabled;
        AutoRamSlider.Value = _settings.AutoRamThresholdPercent;
        AutoRamThresholdLabel.Text = $"{_settings.AutoRamThresholdPercent}%";
        RestorePointCheck.IsChecked = _settings.CreateRestorePointBeforeBoost;
        GameProcessBox.Text = _settings.LastGameProcess;

        PresetBox.ItemsSource = GamePresets.All;
        var preset = GamePresets.All.FirstOrDefault(p => p.Name == _settings.LastPresetName)
                     ?? GamePresets.All[0];
        PresetBox.SelectedItem = preset;
        PresetHint.Text = preset.Name;
        SelectProfile(_settings.LastGameProfile);
    }

    private void PersistSettings()
    {
        _settings.AutoRamEnabled = AutoRamCheck.IsChecked == true;
        _settings.AutoRamThresholdPercent = (int)AutoRamSlider.Value;
        _settings.CreateRestorePointBeforeBoost = RestorePointCheck.IsChecked == true;
        _settings.LastGameProcess = GameProcessBox.Text.Trim();
        _settings.LastGameProfile = SelectedProfile();
        if (PresetBox.SelectedItem is GamePreset preset)
            _settings.LastPresetName = preset.Name;
        SettingsStore.Save(_settings);
    }

    private void Nav_Checked(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded) return;
        ShowPage(
            overview: NavOverview.IsChecked == true,
            memory: NavMemory.IsChecked == true,
            cleanup: NavCleanup.IsChecked == true,
            drivers: NavDrivers.IsChecked == true,
            games: NavGames.IsChecked == true);
        UpdateNavIcons();
    }

    private void ShowPage(bool overview, bool memory, bool cleanup, bool drivers, bool games)
    {
        PageOverview.Visibility = overview ? Visibility.Visible : Visibility.Collapsed;
        PageMemory.Visibility = memory ? Visibility.Visible : Visibility.Collapsed;
        PageCleanup.Visibility = cleanup ? Visibility.Visible : Visibility.Collapsed;
        PageDrivers.Visibility = drivers ? Visibility.Visible : Visibility.Collapsed;
        PageGames.Visibility = games ? Visibility.Visible : Visibility.Collapsed;

        HeaderTitle.Text = overview ? "Обзор"
            : memory ? "Память"
            : cleanup ? "Чистка"
            : drivers ? "Драйверы"
            : "Игры";
    }

    private void UpdateNavIcons()
    {
        var accent = (Brush)FindResource("BrushAccent");
        var muted = (Brush)FindResource("BrushMuted");
        SetIcon(IconOverviewPath, NavOverview.IsChecked == true, accent, muted);
        SetIcon(IconMemoryPath, NavMemory.IsChecked == true, accent, muted);
        SetIcon(IconCleanupPath, NavCleanup.IsChecked == true, accent, muted);
        SetIcon(IconDriversPath, NavDrivers.IsChecked == true, accent, muted);
        SetIcon(IconGamesPath, NavGames.IsChecked == true, accent, muted);
    }

    private static void SetIcon(Shape path, bool active, Brush accent, Brush muted) =>
        path.Stroke = active ? accent : muted;

    private void GitHubLink_OnClick(object sender, MouseButtonEventArgs e) =>
        OpenUrl("https://github.com/1Thes1");

    private void RefreshOverview_Click(object sender, RoutedEventArgs e) => RefreshOverview(silent: false);

    private void RefreshOverview(bool silent)
    {
        try
        {
            var snap = SystemMonitorService.GetSnapshot();
            CpuValue.Text = $"{snap.CpuPercent:0}%";
            CpuBar.Value = snap.CpuPercent;
            CpuPill.Text = $"{snap.CpuPercent:0}";
            RingGeometry.SetPercent(CpuRing, snap.CpuPercent);

            RamBar.Value = snap.RamPercent;
            RamPill.Text = $"{snap.RamPercent:0}";
            RamValue.Text = $"{snap.RamUsedGb:0.0} GB / {snap.RamTotalGb:0.0} GB";
            RingGeometry.SetPercent(RamRing, snap.RamPercent);

            DiskValue.Text = $"{snap.DiskPercent:0}%";
            DiskBar.Value = snap.DiskPercent;
            DiskPill.Text = $"{snap.DiskPercent:0}";
            RingGeometry.SetPercent(DiskRing, snap.DiskPercent);

            HostInfo.Text = snap.Hostname;
            UptimeInfo.Text = $"{snap.OsName} · аптайм {snap.Uptime.Days}д {snap.Uptime.Hours}ч {snap.Uptime.Minutes}м";
            ProcessCountInfo.Text =
                $"{snap.ProcessCount} процессов · свободно на диске {snap.DiskFreeGb:0.0} GB";

            ProcessGrid.ItemsSource = SystemMonitorService.GetTopProcesses();

            var def = DefenderService.GetStatus();
            var on = def.Realtime.Equals("True", StringComparison.OrdinalIgnoreCase) ||
                     def.Realtime.Equals("1", StringComparison.OrdinalIgnoreCase);
            DefenderValue.Text = on ? "Defender · online" : $"Defender · {def.Realtime}";
            DefenderValue.Foreground = on
                ? (Brush)FindResource("BrushAccent")
                : (Brush)FindResource("BrushWarn");
            DefenderUpdated.Text = $"База сигнатур: {def.LastUpdate}";
            MemoryStatus.Text = $"{snap.RamAvailableGb:0.00} GB свободно";

            if (!silent)
                SetStatus("Обновлено");
        }
        catch (Exception ex)
        {
            SetStatus("Ошибка: " + ex.Message);
        }
    }

    private void MaybeAutoFreeRam()
    {
        if (AutoRamCheck.IsChecked != true)
            return;

        var snap = SystemMonitorService.GetSnapshot();
        if (snap.RamPercent < AutoRamSlider.Value)
            return;

        if ((DateTime.UtcNow - _lastAutoRamUtc).TotalSeconds < _settings.AutoRamCooldownSeconds)
            return;

        _lastAutoRamUtc = DateTime.UtcNow;
        MemoryLog.Text = "Авто: " + MemoryService.FreeMemory();
        SetStatus("Автоочистка RAM");
        RefreshMemory();
    }

    private void DefenderScan_Click(object sender, RoutedEventArgs e) =>
        SetStatus(DefenderService.StartQuickScan());

    private void FreeRam_Click(object sender, RoutedEventArgs e)
    {
        MemoryLog.Text = MemoryService.FreeMemory();
        RefreshMemory();
        SetStatus(MemoryLog.Text);
    }

    private void RefreshMemory_Click(object sender, RoutedEventArgs e) => RefreshMemory();

    private void RefreshMemory()
    {
        var snap = SystemMonitorService.GetSnapshot();
        MemoryStatus.Text = $"{snap.RamAvailableGb:0.00} GB свободно · {snap.RamPercent:0}% занято";
        RamValue.Text = $"{snap.RamUsedGb:0.0} GB / {snap.RamTotalGb:0.0} GB";
        RamBar.Value = snap.RamPercent;
        RamPill.Text = $"{snap.RamPercent:0}";
        RingGeometry.SetPercent(RamRing, snap.RamPercent);
    }

    private void AutoRam_Changed(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded) return;
        PersistSettings();
        SetStatus(AutoRamCheck.IsChecked == true
            ? $"Авто-RAM · {AutoRamSlider.Value:0}%"
            : "Авто-RAM выключена");
    }

    private void AutoRamSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (AutoRamThresholdLabel is null) return;
        AutoRamThresholdLabel.Text = $"{e.NewValue:0}%";
        if (IsLoaded)
            PersistSettings();
    }

    private void CleanTemp_Click(object sender, RoutedEventArgs e)
    {
        CleanupLog.Text = CleanupService.CleanTemp().Message;
        SetStatus(CleanupLog.Text);
    }

    private void DiskCleanup_Click(object sender, RoutedEventArgs e)
    {
        CleanupLog.Text = CleanupService.OpenDiskCleanup();
        SetStatus(CleanupLog.Text);
    }

    private void ScanDrivers_Click(object sender, RoutedEventArgs e)
    {
        SetStatus("Сканирование драйверов…");
        DriverGrid.ItemsSource = DriverService.ScanDrivers();
        SetStatus("Скан драйверов готов");
    }

    private void WindowsUpdate_Click(object sender, RoutedEventArgs e) =>
        SetStatus(DriverService.OpenWindowsUpdate());

    private void DeviceManager_Click(object sender, RoutedEventArgs e) =>
        SetStatus(DriverService.OpenDeviceManager());

    private void PresetBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded) return;
        if (PresetBox.SelectedItem is not GamePreset preset) return;
        ApplyPreset(preset);
    }

    private void ApplyPreset(GamePreset preset)
    {
        PresetHint.Text = preset.Name;
        if (!string.IsNullOrWhiteSpace(preset.ProcessName))
            GameProcessBox.Text = preset.ProcessName;
        SelectProfile(preset.Profile);
        PersistSettings();
    }

    private void ApplyBoost_Click(object sender, RoutedEventArgs e)
    {
        PersistSettings();
        var msg = GameBoostService.ApplyBoost(SelectedProfile(), RestorePointCheck.IsChecked == true);
        GameLog.Text = msg;

        if (!string.IsNullOrWhiteSpace(GameProcessBox.Text))
        {
            var focus = GameBoostService.FocusGameProcess(GameProcessBox.Text.Trim());
            GameLog.Text = msg + Environment.NewLine + focus;
        }

        SetStatus("Буст применён");
        NavGames.IsChecked = true;
    }

    private void FocusGame_Click(object sender, RoutedEventArgs e)
    {
        GameLog.Text = GameBoostService.FocusGameProcess(GameProcessBox.Text.Trim());
        SetStatus(GameLog.Text);
        PersistSettings();
    }

    private void OpenRestore_Click(object sender, RoutedEventArgs e) =>
        SetStatus(RestorePointService.OpenSystemRestore());

    private string SelectedProfile() =>
        (GameProfileBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "FPS";

    private void SelectProfile(string profile)
    {
        foreach (ComboBoxItem item in GameProfileBox.Items)
        {
            if (string.Equals(item.Content?.ToString(), profile, StringComparison.OrdinalIgnoreCase))
            {
                GameProfileBox.SelectedItem = item;
                return;
            }
        }
        GameProfileBox.SelectedIndex = 0;
    }

    private void SetStatus(string text) => StatusText.Text = text;

    private static void OpenUrl(string url) =>
        Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });

    private void TryEnableDarkTitleBar()
    {
        try
        {
            var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            if (hwnd == IntPtr.Zero)
            {
                SourceInitialized += (_, _) => TryEnableDarkTitleBar();
                return;
            }

            var useDark = 1;
            DwmSetWindowAttribute(hwnd, 20, ref useDark, sizeof(int));
            DwmSetWindowAttribute(hwnd, 19, ref useDark, sizeof(int));
        }
        catch
        {
            // ignore on unsupported systems
        }
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);
}
