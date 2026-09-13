using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using ThesGamer.Services;

namespace ThesGamer;

public partial class MainWindow : Window
{
    private readonly DispatcherTimer _uiTimer;
    private readonly AppSettings _settings;
    private DateTime _lastAutoRamUtc = DateTime.MinValue;
    private FrameworkElement? _currentPage;

    public MainWindow()
    {
        InitializeComponent();
        _settings = SettingsStore.Load();
        Loc.SetLanguage(_settings.Language);

        _uiTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _uiTimer.Tick += (_, _) =>
        {
            RefreshOverview(silent: true);
            MaybeAutoFreeRam();
        };

        Loaded += (_, _) =>
        {
            ApplyLanguage();
            ApplySettingsToUi();
            RefreshOverview(silent: false);
            RefreshMemory();
            UpdateNavIcons();
            _currentPage = PageOverview;
            _uiTimer.Start();
            _ = CheckUpdatesSilentAsync();
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
        HighlightLangButtons();
    }

    private void PersistSettings()
    {
        _settings.AutoRamEnabled = AutoRamCheck.IsChecked == true;
        _settings.AutoRamThresholdPercent = (int)AutoRamSlider.Value;
        _settings.CreateRestorePointBeforeBoost = RestorePointCheck.IsChecked == true;
        _settings.LastGameProcess = GameProcessBox.Text.Trim();
        _settings.LastGameProfile = SelectedProfile();
        _settings.Language = Loc.Language;
        if (PresetBox.SelectedItem is GamePreset preset)
            _settings.LastPresetName = preset.Name;
        SettingsStore.Save(_settings);
    }

    private void ApplyLanguage()
    {
        TxtBrand.Text = Loc.T("brand");
        BtnRefresh.Content = Loc.T("refresh");
        BtnSafeBoost.Content = Loc.T("safe_boost");
        BtnCheckUpdates.Content = Loc.T("check_updates");
        TxtLoad.Text = Loc.T("load");
        TxtTemps.Text = Loc.T("temps");
        TxtCpuTempLabel.Text = Loc.T("cpu_temp");
        TxtGpuTempLabel.Text = Loc.T("gpu_temp");
        BtnScanDefender.Content = Loc.T("scan_defender");
        BtnFreeRamOverview.Content = Loc.T("free_ram");
        TxtDiskC.Text = Loc.T("disk_c");
        TxtTopProc.Text = Loc.T("top_proc");
        TxtRamTitle.Text = Loc.T("ram_clean_title");
        TxtRamSub.Text = Loc.T("ram_clean_sub");
        BtnFreeRam.Content = Loc.T("free_ram");
        BtnRefreshMem.Content = Loc.T("refresh");
        TxtBeforeAfter.Text = Loc.T("before_after");
        AutoRamCheck.Content = Loc.T("auto_ram");
        TxtTempTitle.Text = Loc.T("temp_title");
        TxtTempSub.Text = Loc.T("temp_sub");
        BtnCleanTemp.Content = Loc.T("clean_temp");
        BtnDiskCleanup.Content = Loc.T("disk_cleanup");
        TxtCleanupBA.Text = Loc.T("before_after");
        BtnScanDrivers.Content = Loc.T("scan");
        BtnDeviceMgr.Content = Loc.T("device_mgr");
        TxtBoostFeats.Text = Loc.T("boost_feats");
        TxtBoostReady.Text = Loc.T("boost_ready");
        TxtProfile.Text = Loc.T("profile");
        TxtGameProc.Text = Loc.T("game_proc");
        TxtGameProcHint.Text = Loc.T("game_proc_hint");
        RestorePointCheck.Content = Loc.T("restore_point");
        BtnApplyBoost.Content = Loc.T("apply");
        BtnPriority.Content = Loc.T("priority");
        BtnProtect.Content = Loc.T("protect");
        GameLog.Text = Loc.T("ready_boost");
        TxtQuickPick.Text = Loc.T("quick_pick");
        StatusText.Text = Loc.T("ready");
        UpdateHeaderTitle();
        HighlightLangButtons();
    }

    private void HighlightLangButtons()
    {
        LangRuBtn.Opacity = Loc.Language == "ru" ? 1 : 0.55;
        LangEnBtn.Opacity = Loc.Language == "en" ? 1 : 0.55;
    }

    private void LangRu_Click(object sender, RoutedEventArgs e)
    {
        Loc.SetLanguage("ru");
        PersistSettings();
        ApplyLanguage();
    }

    private void LangEn_Click(object sender, RoutedEventArgs e)
    {
        Loc.SetLanguage("en");
        PersistSettings();
        ApplyLanguage();
    }

    private void UpdateHeaderTitle()
    {
        HeaderTitle.Text = NavOverview.IsChecked == true ? Loc.T("overview")
            : NavMemory.IsChecked == true ? Loc.T("memory")
            : NavCleanup.IsChecked == true ? Loc.T("cleanup")
            : NavDrivers.IsChecked == true ? Loc.T("drivers")
            : Loc.T("games");
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
        UpdateHeaderTitle();
    }

    private void ShowPage(bool overview, bool memory, bool cleanup, bool drivers, bool games)
    {
        FrameworkElement next =
            overview ? PageOverview
            : memory ? PageMemory
            : cleanup ? PageCleanup
            : drivers ? PageDrivers
            : PageGames;

        PageOverview.Visibility = Visibility.Collapsed;
        PageMemory.Visibility = Visibility.Collapsed;
        PageCleanup.Visibility = Visibility.Collapsed;
        PageDrivers.Visibility = Visibility.Collapsed;
        PageGames.Visibility = Visibility.Collapsed;

        next.Visibility = Visibility.Visible;
        AnimatePageIn(next);
        _currentPage = next;
    }

    private static void AnimatePageIn(FrameworkElement page)
    {
        page.Opacity = 0;
        page.RenderTransform = new TranslateTransform(0, 10);
        var fade = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(180))
        {
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        var slide = new DoubleAnimation(10, 0, TimeSpan.FromMilliseconds(180))
        {
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        page.BeginAnimation(OpacityProperty, fade);
        ((TranslateTransform)page.RenderTransform).BeginAnimation(TranslateTransform.YProperty, slide);
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

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            Maximize_Click(sender, e);
            return;
        }
        try { DragMove(); } catch { /* ignore */ }
    }

    private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void Maximize_Click(object sender, RoutedEventArgs e) =>
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private void GitHubLink_OnClick(object sender, MouseButtonEventArgs e) =>
        OpenUrl("https://github.com/1Thes1/ThesGamer");

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
            UptimeInfo.Text = $"{snap.OsName} · {snap.Uptime.Days}d {snap.Uptime.Hours}h {snap.Uptime.Minutes}m";
            ProcessCountInfo.Text =
                $"{snap.ProcessCount} · disk free {snap.DiskFreeGb:0.0} GB";

            ProcessGrid.ItemsSource = SystemMonitorService.GetTopProcesses();

            var def = DefenderService.GetStatus();
            var on = def.Realtime.Equals("True", StringComparison.OrdinalIgnoreCase) ||
                     def.Realtime.Equals("1", StringComparison.OrdinalIgnoreCase);
            DefenderValue.Text = on ? "Defender · online" : $"Defender · {def.Realtime}";
            DefenderValue.Foreground = on
                ? (Brush)FindResource("BrushAccent")
                : (Brush)FindResource("BrushWarn");
            DefenderUpdated.Text = def.LastUpdate;
            MemoryStatus.Text = $"{snap.RamAvailableGb:0.00} GB free";

            var temps = ThermalService.Read();
            CpuTempValue.Text = temps.Cpu;
            GpuTempValue.Text = temps.Gpu;

            if (!silent)
                SetStatus(Loc.T("updated"));
        }
        catch (Exception ex)
        {
            SetStatus(ex.Message);
        }
    }

    private void MaybeAutoFreeRam()
    {
        if (AutoRamCheck.IsChecked != true) return;
        var snap = SystemMonitorService.GetSnapshot();
        if (snap.RamPercent < AutoRamSlider.Value) return;
        if ((DateTime.UtcNow - _lastAutoRamUtc).TotalSeconds < _settings.AutoRamCooldownSeconds) return;
        _lastAutoRamUtc = DateTime.UtcNow;
        FreeRamInternal(auto: true);
    }

    private void DefenderScan_Click(object sender, RoutedEventArgs e) =>
        SetStatus(DefenderService.StartQuickScan());

    private void FreeRam_Click(object sender, RoutedEventArgs e) => FreeRamInternal(auto: false);

    private void FreeRamInternal(bool auto)
    {
        var before = SystemMonitorService.GetSnapshot();
        var msg = MemoryService.FreeMemory();
        var after = SystemMonitorService.GetSnapshot();
        var freed = Math.Max(0, after.RamAvailableGb - before.RamAvailableGb);
        MemoryBeforeAfter.Text =
            $"{Loc.T("before")}: {before.RamAvailableGb:0.00} GB  →  {Loc.T("after")}: {after.RamAvailableGb:0.00} GB  ·  {Loc.T("freed")}: {freed:0.00} GB";
        MemoryLog.Text = (auto ? "Auto: " : "") + msg;
        RefreshMemory();
        SetStatus(MemoryBeforeAfter.Text);
    }

    private void RefreshMemory_Click(object sender, RoutedEventArgs e) => RefreshMemory();

    private void RefreshMemory()
    {
        var snap = SystemMonitorService.GetSnapshot();
        MemoryStatus.Text = $"{snap.RamAvailableGb:0.00} GB free · {snap.RamPercent:0}%";
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
            ? $"Auto-RAM · {AutoRamSlider.Value:0}%"
            : "Auto-RAM off");
    }

    private void AutoRamSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (AutoRamThresholdLabel is null) return;
        AutoRamThresholdLabel.Text = $"{e.NewValue:0}%";
        if (IsLoaded) PersistSettings();
    }

    private void CleanTemp_Click(object sender, RoutedEventArgs e)
    {
        var before = SystemMonitorService.GetSnapshot();
        var result = CleanupService.CleanTemp();
        var after = SystemMonitorService.GetSnapshot();
        CleanupBeforeAfter.Text =
            $"{Loc.T("before")}: {before.DiskFreeGb:0.00} GB free  →  {Loc.T("after")}: {after.DiskFreeGb:0.00} GB free  ·  {Loc.T("freed")}: {result.BytesFreed / (1024d * 1024d):0.0} MB";
        CleanupLog.Text = result.Message;
        SetStatus(CleanupBeforeAfter.Text);
        RefreshOverview(silent: true);
    }

    private void DiskCleanup_Click(object sender, RoutedEventArgs e)
    {
        CleanupLog.Text = CleanupService.OpenDiskCleanup();
        SetStatus(CleanupLog.Text);
    }

    private void ScanDrivers_Click(object sender, RoutedEventArgs e)
    {
        DriverGrid.ItemsSource = DriverService.ScanDrivers();
        SetStatus(Loc.T("updated"));
    }

    private void WindowsUpdate_Click(object sender, RoutedEventArgs e) =>
        SetStatus(DriverService.OpenWindowsUpdate());

    private void DeviceManager_Click(object sender, RoutedEventArgs e) =>
        SetStatus(DriverService.OpenDeviceManager());

    private void PresetBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded) return;
        if (PresetBox.SelectedItem is not GamePreset preset) return;
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
            GameLog.Text = msg + Environment.NewLine + GameBoostService.FocusGameProcess(GameProcessBox.Text.Trim());
        SetStatus(msg);
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

    private async void CheckUpdates_Click(object sender, RoutedEventArgs e)
    {
        SetStatus("…");
        var ver = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.3.0";
        var result = await UpdateService.CheckAsync(ver);
        SetStatus(result.Message);
        if (result.UpdateAvailable && !string.IsNullOrWhiteSpace(result.HtmlUrl))
        {
            var open = MessageBox.Show(result.Message + "\n\nOpen release page?", "Thes Gamer",
                MessageBoxButton.YesNo, MessageBoxImage.Information);
            if (open == MessageBoxResult.Yes)
                OpenUrl(result.HtmlUrl!);
        }
    }

    private async Task CheckUpdatesSilentAsync()
    {
        try
        {
            var ver = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.3.0";
            var result = await UpdateService.CheckAsync(ver);
            if (result.UpdateAvailable)
                SetStatus(result.Message);
        }
        catch
        {
            // ignore silent failures
        }
    }

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
}
