using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using WindowsDashboard.Controls;
using WindowsDashboard.Dialogs;
using WindowsDashboard.Models;
using WindowsDashboard.Services;
using WindowsDashboard.ViewModels;
using WindowsDashboard.Win32;
using AppItemControl = WindowsDashboard.Controls.AppGroupCard;
using CardControl = WindowsDashboard.Controls.CardChrome;

namespace WindowsDashboard;

public partial class MainWindow : Window
{
    private const int HotKeyId = 0xD415;

    private readonly ConfigService _configService = new();
    private readonly DispatcherTimer _saveTimer;
    private AppConfig _config;
    private DesktopLayerService? _desktopLayer;
    private TrayIconService? _tray;
    private HwndSource? _source;
    private readonly Dictionary<CardControl, string> _cardIds = new();
    private readonly Dictionary<string, AppItemControl> _appCards = new();
    private ObservableCollection<NoteItemViewModel>? _noteItems;
    private bool _isShuttingDown;

    public MainWindow()
    {
        InitializeComponent();

        _config = _configService.Load();
        var args = Environment.GetCommandLineArgs();
        if (args.Contains("--normal", StringComparer.OrdinalIgnoreCase))
        {
            _config.DesktopMode = false;
        }
        else if (args.Contains("--widget", StringComparer.OrdinalIgnoreCase))
        {
            _config.DesktopMode = true;
            _config.WidgetMode = true;
        }

        AppScanner.MergeDesktopShortcuts(_config);
        _configService.Save(_config);

        ApplyFontColor(_config.FontColor);

        SourceInitialized += OnSourceInitialized;
        Closed += (_, _) => ShutdownServices();

        _saveTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(400)
        };
        _saveTimer.Tick += (_, _) =>
        {
            _saveTimer.Stop();
            _configService.Save(_config);
        };

        BuildCards();
        BuildWidget();
        ApplyOpacity(_config.OpacityPercent);
        SetupTray();
        ApplyMode(_config.DesktopMode, initial: true);
    }

    public void ShutdownServices()
    {
        if (_isShuttingDown)
        {
            return;
        }

        _isShuttingDown = true;
        _configService.Save(_config);
        WidgetMonitor?.Dispose();
        _desktopLayer?.Dispose();
        _tray?.Dispose();
        UnregisterHotKey();
    }

    public void ShowFromSecondInstance()
    {
        Dispatcher.BeginInvoke(new Action(ShowDashboard));
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        var handle = new WindowInteropHelper(this).Handle;
        _source = HwndSource.FromHwnd(handle);
        _source?.AddHook(WndProc);
        NativeMethods.RegisterHotKey(handle, HotKeyId, NativeMethods.MOD_CONTROL | NativeMethods.MOD_SHIFT, 0x48);
        _desktopLayer = new DesktopLayerService(this);
        if (_config.DesktopMode)
        {
            ApplyDesktopBoundsToLayer();
            _desktopLayer.Start();
            if (!_desktopLayer.IsAttached)
            {
                // If the desktop layer is unavailable, fall back to a normal window.
                _config.DesktopMode = false;
                ApplyMode(false, initial: true);
            }
        }
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == NativeMethods.WM_HOTKEY && wParam.ToInt32() == HotKeyId)
        {
            ToggleDashboardVisibility();
            handled = true;
            return IntPtr.Zero;
        }

        if (msg == NativeMethods.WM_NCHITTEST &&
            _config.DesktopMode &&
            !_config.WidgetMode &&
            RootGrid.Visibility == Visibility.Visible)
        {
            var x = (short)(lParam.ToInt64() & 0xFFFF);
            var y = (short)((lParam.ToInt64() >> 16) & 0xFFFF);
            var point = PointFromScreen(new Point(x, y));
            var insideCard = _cardIds.Keys.Any(card =>
            {
                var left = Canvas.GetLeft(card);
                if (double.IsNaN(left))
                {
                    left = card.Margin.Left;
                }

                var top = Canvas.GetTop(card);
                if (double.IsNaN(top))
                {
                    top = card.Margin.Top;
                }

                return point.X >= left &&
                       point.X <= left + card.ActualWidth &&
                       point.Y >= top &&
                       point.Y <= top + card.ActualHeight;
            });

            if (!insideCard)
            {
                handled = true;
                return new IntPtr(NativeMethods.HTTRANSPARENT);
            }
        }

        return IntPtr.Zero;
    }

    private void UnregisterHotKey()
    {
        if (_source != null)
        {
            NativeMethods.UnregisterHotKey(_source.Handle, HotKeyId);
        }
    }

    private void BuildCards()
    {
        foreach (var existing in _cardIds.Keys.Select(k => k.Content).OfType<SystemMonitorCard>())
        {
            existing.Dispose();
        }

        CardGrid.Children.Clear();
        _cardIds.Clear();
        _appCards.Clear();
        _noteItems = new ObservableCollection<NoteItemViewModel>();

        var defaults = ConfigService.CreateDefaults();

        AddCard("calendar", "日历", new CalendarCard(), GetSubtitle(defaults, "calendar"));
        AddCard("ai", "AI", new AIGroupCard());

        var devCategory = _config.Categories.FirstOrDefault(c => c.Name == "开发") ?? new CategoryConfig { Name = "开发" };
        var otherCategory = _config.Categories.FirstOrDefault(c => c.Name == "其他") ?? new CategoryConfig { Name = "其他" };
        AddAppCard("dev", "开发", devCategory);
        AddAppCard("other", "其他", otherCategory);

        AddCard("quick", "快捷工具", BuildQuickCard());
        AddCard("git", "Git", BuildGitCard());
        AddCard("time", "时间", new ClockCard());
        AddCard("monitor", "系统监控", new SystemMonitorCard());

        var notesCard = new StickyNoteCard();
        foreach (var note in _config.Notes)
        {
            _noteItems.Add(new NoteItemViewModel(note));
        }

        notesCard.SetNotes(_noteItems);
        notesCard.AddRequested += AddNote;
        notesCard.DeleteRequested += DeleteNote;
        notesCard.NotesChanged += ScheduleSave;
        AddCard("notes", "便签", notesCard);

        var projectCard = new ProjectCard();
        projectCard.SetFolders(_config.ProjectFolders.Select(p => new ProjectFolderViewModel(p)));
        AddCard("project", "Project", projectCard);

        ApplyCardLayouts();
        LoadIconsAsync();
    }

    private string GetSubtitle(AppConfig defaults, string id)
    {
        return id switch
        {
            "calendar" => DateTime.Now.ToString("yyyy年M月"),
            "git" => $"{_config.GitProjects.Count} 个项目",
            "notes" => $"{_config.Notes.Count} 条",
            _ => string.Empty
        };
    }

    private void AddCard(string id, string title, object content, string subtitle = "")
    {
        var cfg = _config.Cards.FirstOrDefault(c => c.Id == id) ??
                  ConfigService.CreateDefaults().Cards.First(c => c.Id == id);
        if (!cfg.State.Equals("Visible", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var card = new CardControl
        {
            Title = title,
            Subtitle = subtitle,
            Content = content,
            Opacity = cfg.Opacity
        };
        card.SetOpacity(cfg.Opacity);
        card.Moved += OnCardMoved;
        card.Resized += OnCardResized;
        card.MenuRequested += OnCardMenuRequested;
        card.ContextRequested += (c, _) => OnCardMenuRequested(c);

        _cardIds[card] = id;
        CardGrid.Children.Add(card);
        ApplyCardLayout(card, cfg);
    }

    private void AddAppCard(string id, string title, CategoryConfig category)
    {
        var cfg = _config.Cards.FirstOrDefault(c => c.Id == id) ??
                  ConfigService.CreateDefaults().Cards.First(c => c.Id == id);
        if (!cfg.State.Equals("Visible", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var appCard = new AppItemControl
        {
            CategoryName = category.Name,
            AllowDrop = true
        };
        var vms = category.Apps.Select(a => new AppItemViewModel(a, category.Name)).ToList();
        appCard.SetApps(vms);
        appCard.AppActivated += vm => ProcessLauncher.Launch(vm.Entry);
        appCard.AppContextRequested += (vm, point) => ShowAppContextMenu(appCard, vm);
        appCard.AddRequested += () => AddAppFromPicker(category);
        appCard.FilesDropped += paths => AddDroppedFiles(category, paths);

        _appCards[id] = appCard;
        AddCard(id, title, appCard);
    }

    private QuickToolsCard BuildQuickCard()
    {
        var card = new QuickToolsCard();
        card.SetTools(CreateQuickTools());
        card.ToolActivated += vm => ProcessLauncher.Launch(new AppEntryConfig
        {
            Name = vm.Name,
            TargetPath = vm.Command
        });
        card.ToolContextRequested += (vm, _) => ShowQuickToolMenu(vm);
        return card;
    }

    private void BuildWidget()
    {
        WidgetQuick.SetTools(CreateQuickTools());
        WidgetQuick.ToolActivated += vm => ProcessLauncher.Launch(new AppEntryConfig
        {
            Name = vm.Name,
            TargetPath = vm.Command
        });
        WidgetQuick.ToolContextRequested += (vm, _) => ShowQuickToolMenu(vm);

        WidgetNotes.SetNotes(_noteItems ?? new ObservableCollection<NoteItemViewModel>());
        WidgetNotes.AddRequested += AddNote;
        WidgetNotes.DeleteRequested += DeleteNote;
        WidgetNotes.NotesChanged += ScheduleSave;

        ApplyWidgetSections();
    }

    private void ApplyWidgetSections()
    {
        WidgetClockSection.Visibility = _config.WidgetShowClock ? Visibility.Visible : Visibility.Collapsed;
        WidgetQuickSection.Visibility = _config.WidgetShowQuick ? Visibility.Visible : Visibility.Collapsed;
        WidgetMonitorSection.Visibility = _config.WidgetShowMonitor ? Visibility.Visible : Visibility.Collapsed;
        WidgetCalendarSection.Visibility = _config.WidgetShowCalendar ? Visibility.Visible : Visibility.Collapsed;
        WidgetNotesSection.Visibility = _config.WidgetShowNotes ? Visibility.Visible : Visibility.Collapsed;
    }

    private List<QuickToolViewModel> CreateQuickTools()
    {
        var tools = new[]
        {
            new QuickToolViewModel("CMD", "cmd.exe"),
            new QuickToolViewModel("PowerShell", "powershell.exe"),
            new QuickToolViewModel("设备管理器", "devmgmt.msc"),
            new QuickToolViewModel("磁盘管理", "diskmgmt.msc"),
            new QuickToolViewModel("注册表", "regedit.exe"),
            new QuickToolViewModel("任务管理器", "taskmgr.exe"),
            new QuickToolViewModel("VS Code", FindInstalledApp("Visual Studio Code") ?? "code"),
            new QuickToolViewModel("Visual Studio", FindInstalledApp("Visual Studio") ?? "devenv"),
            new QuickToolViewModel("Explorer", "explorer.exe")
        };
        return tools.ToList();
    }

    private string? FindInstalledApp(string name)
    {
        return _config.Categories
            .SelectMany(c => c.Apps)
            .FirstOrDefault(a => string.Equals(a.Name, name, StringComparison.OrdinalIgnoreCase))?.TargetPath;
    }

    private GitCard BuildGitCard()
    {
        var card = new GitCard();
        card.SetProjects(_config.GitProjects.Select(p => new GitItemViewModel(p)));
        card.ContextRequested += (vm, _) => ShowGitMenu(vm);
        return card;
    }

    private void ApplyCardLayouts()
    {
        foreach (var card in _cardIds.Keys)
        {
            var id = _cardIds[card];
            var cfg = _config.Cards.FirstOrDefault(c => c.Id == id);
            if (cfg != null)
            {
                ApplyCardLayout(card, cfg);
            }
        }
    }

    private void ApplyCardLayout(CardControl card, CardConfig cfg)
    {
        var width = Math.Max(60, CardGrid.ActualWidth * cfg.WidthPercent / 100);
        var height = Math.Max(60, CardGrid.ActualHeight * cfg.HeightPercent / 100);
        card.HorizontalAlignment = HorizontalAlignment.Left;
        card.VerticalAlignment = VerticalAlignment.Top;
        card.Width = width;
        card.Height = height;
        card.Margin = new Thickness(
            CardGrid.ActualWidth * cfg.LeftPercent / 100,
            CardGrid.ActualHeight * cfg.TopPercent / 100,
            0,
            0);
    }

    private void OnCardGridSizeChanged(object sender, SizeChangedEventArgs e)
    {
        ApplyCardLayouts();
    }

    private void OnCardMoved(CardControl card, double dx, double dy)
    {
        if (!_cardIds.TryGetValue(card, out var id))
        {
            return;
        }

        var cfg = _config.Cards.FirstOrDefault(c => c.Id == id);
        if (cfg == null)
        {
            return;
        }

        var left = Math.Clamp(card.Margin.Left + dx, 0, Math.Max(0, CardGrid.ActualWidth - card.ActualWidth));
        var top = Math.Clamp(card.Margin.Top + dy, 0, Math.Max(0, CardGrid.ActualHeight - card.ActualHeight));
        card.Margin = new Thickness(left, top, 0, 0);
        cfg.LeftPercent = CardGrid.ActualWidth <= 0 ? 0 : left / CardGrid.ActualWidth * 100;
        cfg.TopPercent = CardGrid.ActualHeight <= 0 ? 0 : top / CardGrid.ActualHeight * 100;
        ScheduleSave();
    }

    private void OnCardResized(CardControl card, double dx, double dy)
    {
        if (!_cardIds.TryGetValue(card, out var id))
        {
            return;
        }

        var cfg = _config.Cards.FirstOrDefault(c => c.Id == id);
        if (cfg == null)
        {
            return;
        }

        var minW = CardGrid.ActualWidth * 0.10;
        var maxW = CardGrid.ActualWidth * 0.34;
        var minH = CardGrid.ActualHeight * 0.08;
        var maxH = CardGrid.ActualHeight * 0.72;
        var width = Math.Clamp(card.Width + dx, minW, maxW);
        var height = Math.Clamp(card.Height + dy, minH, maxH);
        card.Width = width;
        card.Height = height;
        cfg.WidthPercent = CardGrid.ActualWidth <= 0 ? 0 : width / CardGrid.ActualWidth * 100;
        cfg.HeightPercent = CardGrid.ActualHeight <= 0 ? 0 : height / CardGrid.ActualHeight * 100;
        ScheduleSave();
    }

    private void OnCardMenuRequested(CardControl card)
    {
        if (!_cardIds.TryGetValue(card, out var id))
        {
            return;
        }

        var cfg = _config.Cards.FirstOrDefault(c => c.Id == id);
        if (cfg == null)
        {
            return;
        }

        ShowContextMenu(card,
            new MenuItemSpec("恢复默认位置", () => ResetCard(card, id)),
            new MenuItemSpec("隐藏卡片", () => SetCardState(id, "Hidden")),
            new MenuItemSpec("删除卡片", () => SetCardState(id, "Deleted"), Danger: true),
            new MenuItemSpec("设置", () => OpenSettings()));
    }

    private void ResetCard(CardControl card, string id)
    {
        var defaults = ConfigService.CreateDefaults().Cards.FirstOrDefault(c => c.Id == id);
        var cfg = _config.Cards.FirstOrDefault(c => c.Id == id);
        if (defaults == null || cfg == null)
        {
            return;
        }

        cfg.LeftPercent = defaults.LeftPercent;
        cfg.TopPercent = defaults.TopPercent;
        cfg.WidthPercent = defaults.WidthPercent;
        cfg.HeightPercent = defaults.HeightPercent;
        ApplyCardLayout(card, cfg);
        ScheduleSave();
    }

    private void SetCardState(string id, string state)
    {
        var cfg = _config.Cards.FirstOrDefault(c => c.Id == id);
        if (cfg != null)
        {
            cfg.State = state;
        }

        var card = _cardIds.FirstOrDefault(kv => kv.Value == id).Key;
        if (card != null)
        {
            if (card.Content is SystemMonitorCard monitor)
            {
                monitor.Dispose();
            }

            CardGrid.Children.Remove(card);
            _cardIds.Remove(card);
        }

        ScheduleSave();
    }

    private void ShowAppContextMenu(AppItemControl card, AppItemViewModel vm)
    {
        ShowContextMenu(card,
            new MenuItemSpec("打开", () => ProcessLauncher.Launch(vm.Entry)),
            new MenuItemSpec("打开文件位置", () => ProcessLauncher.OpenFileLocation(vm.ShortcutPath)),
            new MenuItemSpec("编辑", () => EditApp(card.CategoryName, vm)),
            new MenuItemSpec("从分类移除", () => RemoveApp(card.CategoryName, vm)),
            new MenuItemSpec("删除快捷方式", () => DeleteShortcut(vm), Danger: true));
    }

    private void ShowQuickToolMenu(QuickToolViewModel vm)
    {
        ShowContextMenu(WindowBar,
            new MenuItemSpec("打开", () => ProcessLauncher.Launch(new AppEntryConfig { Name = vm.Name, TargetPath = vm.Command })),
            new MenuItemSpec("打开文件位置", () => ProcessLauncher.OpenFileLocation(AppScanner.ResolveExecutable(vm.Command) ?? vm.Command)));
    }

    private void ShowGitMenu(GitItemViewModel vm)
    {
        ShowContextMenu(WindowBar,
            new MenuItemSpec("打开目录", () => ProcessLauncher.OpenPath(vm.Path)),
            new MenuItemSpec("打开 VS Code", () => ProcessLauncher.OpenPath(vm.Path, "code")),
            new MenuItemSpec("打开终端", () => ProcessLauncher.OpenPath(vm.Path, "terminal")));
    }

    private void AddAppFromPicker(CategoryConfig category)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "选择快捷方式或程序",
            Filter = "快捷方式或程序|*.lnk;*.exe;*.url|所有文件|*.*",
            Multiselect = true
        };
        if (dialog.ShowDialog(this) == true)
        {
            AddDroppedFiles(category, dialog.FileNames);
        }
    }

    private void AddDroppedFiles(CategoryConfig category, IEnumerable<string> paths)
    {
        foreach (var path in paths)
        {
            var entry = CreateEntryFromPath(path);
            if (entry != null)
            {
                category.Apps.Add(entry);
            }
        }

        RefreshCategoryCards();
        ScheduleSave();
    }

    private static AppEntryConfig? CreateEntryFromPath(string path)
    {
        path = Environment.ExpandEnvironmentVariables(path);
        if (!File.Exists(path) && !Directory.Exists(path))
        {
            return null;
        }

        var target = path;
        if (path.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase))
        {
            target = ShortcutResolver.ResolveTarget(path) ?? path;
        }

        return new AppEntryConfig
        {
            Name = Path.GetFileNameWithoutExtension(path),
            TargetPath = target,
            ShortcutPath = path,
            IconPath = target,
            WorkingDirectory = File.Exists(target) ? Path.GetDirectoryName(target) ?? string.Empty : string.Empty
        };
    }

    private void EditApp(string categoryName, AppItemViewModel vm)
    {
        var dialog = new AppEditDialog(vm.Entry);
        dialog.Owner = this;
        if (dialog.ShowDialog() == true)
        {
            vm.Entry.Name = dialog.AppName;
            vm.Entry.TargetPath = dialog.TargetPath;
            vm.Entry.Arguments = dialog.Arguments;
            vm.Entry.WorkingDirectory = dialog.WorkingDirectory;
            RefreshCategoryCards();
            ScheduleSave();
        }
    }

    private void RemoveApp(string categoryName, AppItemViewModel vm)
    {
        var category = _config.Categories.FirstOrDefault(c => c.Name == categoryName);
        category?.Apps.Remove(vm.Entry);
        RefreshCategoryCards();
        ScheduleSave();
    }

    private void DeleteShortcut(AppItemViewModel vm)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(vm.ShortcutPath) && File.Exists(vm.ShortcutPath))
            {
                File.Delete(vm.ShortcutPath);
            }

            var category = _config.Categories.FirstOrDefault(c => c.Name == vm.Category);
            category?.Apps.Remove(vm.Entry);
            RefreshCategoryCards();
            ScheduleSave();
        }
        catch
        {
            MessageBox.Show(this, "无法删除快捷方式，可能已被占用。", "Windows 桌面整理器",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void RefreshCategoryCards()
    {
        foreach (var id in _appCards.Keys.ToList())
        {
            if (!_appCards.TryGetValue(id, out var card))
            {
                continue;
            }

            var categoryName = card.CategoryName;
            var category = _config.Categories.FirstOrDefault(c => c.Name == categoryName);
            if (category != null)
            {
                card.SetApps(category.Apps.Select(a => new AppItemViewModel(a, category.Name)));
            }
        }

        var gitCard = _cardIds.Keys.Select(k => k.Content).OfType<GitCard>().FirstOrDefault();
        gitCard?.SetProjects(_config.GitProjects.Select(p => new GitItemViewModel(p)));
    }

    private void AddNote()
    {
        var note = new StickyNote { Id = Guid.NewGuid().ToString("N"), Text = string.Empty };
        _config.Notes.Add(note);
        _noteItems?.Add(new NoteItemViewModel(note));
        ScheduleSave();
    }

    private void DeleteNote(NoteItemViewModel vm)
    {
        _config.Notes.Remove(vm.Note);
        _noteItems?.Remove(vm);
        ScheduleSave();
    }

    private void LoadIconsAsync()
    {
        var vms = new List<object>();
        foreach (var appCard in _appCards.Values)
        {
            vms.AddRange(appCard.ItemsSource.OfType<object>());
        }

        foreach (var quickCard in _cardIds.Keys.Select(k => k.Content).OfType<QuickToolsCard>())
        {
            vms.AddRange(quickCard.ItemsSource.OfType<object>());
        }

        foreach (var vm in vms)
        {
            if (vm is AppItemViewModel appVm)
            {
                var path = appVm.Entry.IconPath;
                if (string.IsNullOrWhiteSpace(path))
                {
                    path = appVm.Entry.ShortcutPath;
                }

                if (string.IsNullOrWhiteSpace(path))
                {
                    path = appVm.Entry.TargetPath;
                }

                if (string.IsNullOrWhiteSpace(path))
                {
                    continue;
                }

                Task.Run(() =>
                {
                    var icon = IconExtractor.ExtractIcon(path, appVm.Entry.IconIndex) ?? IconExtractor.CreateDefaultIcon();
                    if (icon != null)
                    {
                        Dispatcher.BeginInvoke(new Action(() => appVm.Icon = icon));
                    }
                });
            }
            else if (vm is QuickToolViewModel quickVm)
            {
                var path = AppScanner.ResolveExecutable(quickVm.Command) ?? quickVm.Command;
                if (string.IsNullOrWhiteSpace(path))
                {
                    continue;
                }

                Task.Run(() =>
                {
                    var icon = IconExtractor.ExtractIcon(path) ?? IconExtractor.CreateDefaultIcon();
                    if (icon != null)
                    {
                        Dispatcher.BeginInvoke(new Action(() => quickVm.Icon = icon));
                    }
                });
            }
        }
    }

    private void SetupTray()
    {
        _tray = new TrayIconService();
        _tray.ShowRequested += ShowDashboard;
        _tray.HideRequested += HideDashboard;
        _tray.ToggleModeRequested += ToggleMode;
        _tray.ToggleWidgetModeRequested += ToggleWidgetMode;
        _tray.SettingsRequested += OpenSettings;
        _tray.ReloadRequested += ReloadLayout;
        _tray.ExitRequested += () => Application.Current.Shutdown();
        _tray.Show();
    }

    private void ApplyMode(bool desktopMode, bool initial = false)
    {
        _config.DesktopMode = desktopMode;
        RootGrid.Visibility = Visibility.Visible;
        CardGrid.Visibility = _config.WidgetMode ? Visibility.Collapsed : Visibility.Visible;
        WidgetShell.Visibility = _config.WidgetMode ? Visibility.Visible : Visibility.Collapsed;

        if (desktopMode)
        {
            WindowBar.Visibility = Visibility.Collapsed;
            ShowInTaskbar = false;
            WindowState = _config.WidgetMode ? WindowState.Normal : WindowState.Maximized;
            ApplyDesktopBoundsToLayer();
            _desktopLayer?.Start();
        }
        else
        {
            _desktopLayer?.Stop();
            ShowInTaskbar = true;
            WindowState = WindowState.Normal;
            WindowBar.Visibility = Visibility.Visible;

            if (_config.WidgetMode)
            {
                EnsureWidgetBounds();
                Width = _config.WidgetWidth;
                Height = _config.WidgetHeight;
                Left = _config.WidgetLeft;
                Top = _config.WidgetTop;
            }
            else
            {
                Width = Math.Max(Width, 1180);
                Height = Math.Max(Height, 720);
                Left = (SystemParameters.VirtualScreenWidth - Width) / 2;
                Top = (SystemParameters.VirtualScreenHeight - Height) / 2;
            }
        }

        if (!initial)
        {
            ScheduleSave();
        }
    }

    private void ApplyDesktopBoundsToLayer()
    {
        if (_desktopLayer == null)
        {
            return;
        }

        if (_config.WidgetMode)
        {
            EnsureWidgetBounds();
            var dpi = VisualTreeHelper.GetDpi(this);
            _desktopLayer.SetBounds(
                (int)(_config.WidgetLeft * dpi.PixelsPerDip),
                (int)(_config.WidgetTop * dpi.PixelsPerDip),
                (int)(_config.WidgetWidth * dpi.PixelsPerDip),
                (int)(_config.WidgetHeight * dpi.PixelsPerDip));
        }
        else
        {
            _desktopLayer.SetFullScreen();
        }
    }

    private void EnsureWidgetBounds()
    {
        if (_config.WidgetLeft < 0 ||
            _config.WidgetTop < 0 ||
            _config.WidgetWidth < 280 ||
            _config.WidgetHeight < 420)
        {
            _config.WidgetLeft = Math.Max(8, SystemParameters.VirtualScreenWidth - _config.WidgetWidth - 32);
            _config.WidgetTop = 48;
            _config.WidgetWidth = 360;
            _config.WidgetHeight = 560;
        }
    }

    private void ToggleMode()
    {
        ApplyMode(!_config.DesktopMode);
    }

    private void ToggleWidgetMode()
    {
        _config.WidgetMode = !_config.WidgetMode;
        ApplyMode(_config.DesktopMode);
    }

    private void ShowDashboard()
    {
        RootGrid.Visibility = Visibility.Visible;
        if (_config.DesktopMode)
        {
            NativeMethods.ShowWindow(new WindowInteropHelper(this).Handle, 5);
            _desktopLayer?.EnsureConnected();
        }
        else
        {
            Show();
            Activate();
        }
    }

    private void HideDashboard()
    {
        RootGrid.Visibility = Visibility.Collapsed;
    }

    private void ToggleDashboardVisibility()
    {
        if (RootGrid.Visibility == Visibility.Visible)
        {
            HideDashboard();
        }
        else
        {
            ShowDashboard();
        }
    }

    private void OpenSettings()
    {
        var dialog = new SettingsWindow(_config)
        {
            Owner = this
        };
        if (dialog.ShowDialog() == true)
        {
            if (dialog.DesktopModeChanged || dialog.WidgetModeChanged)
            {
                _config.WidgetMode = dialog.WidgetMode;
                ApplyMode(dialog.DesktopMode);
            }

            if (dialog.AutoStartChanged)
            {
                AutostartService.SetEnabled(dialog.AutoStart);
            }

            ApplyFontColor(dialog.FontColor);

            if (dialog.RescanShortcuts)
            {
                AppScanner.MergeDesktopShortcuts(_config);
                RefreshCategoryCards();
            }

            if (dialog.ResetLayout)
            {
                ResetAllCards();
            }

            foreach (var id in dialog.RestoredCards)
            {
                var card = _config.Cards.FirstOrDefault(c => c.Id == id);
                if (card != null)
                {
                    card.State = "Visible";
                }
            }

            _config.WidgetShowClock = dialog.ShowClockWidget;
            _config.WidgetShowQuick = dialog.ShowQuickWidget;
            _config.WidgetShowMonitor = dialog.ShowMonitorWidget;
            _config.WidgetShowCalendar = dialog.ShowCalendarWidget;
            _config.WidgetShowNotes = dialog.ShowNotesWidget;

            BuildCards();
            WidgetNotes.SetNotes(_noteItems ?? new ObservableCollection<NoteItemViewModel>());
            ApplyWidgetSections();
            ApplyOpacity(dialog.OpacityPercent);
            ScheduleSave();
        }
    }

    private void ResetAllCards()
    {
        var defaults = ConfigService.CreateDefaults();
        foreach (var cfg in _config.Cards)
        {
            var d = defaults.Cards.FirstOrDefault(c => c.Id == cfg.Id);
            if (d != null)
            {
                cfg.LeftPercent = d.LeftPercent;
                cfg.TopPercent = d.TopPercent;
                cfg.WidthPercent = d.WidthPercent;
                cfg.HeightPercent = d.HeightPercent;
                cfg.State = "Visible";
                cfg.Opacity = 1.0;
            }
        }
    }

    private void ApplyOpacity(int opacityPercent)
    {
        _config.OpacityPercent = opacityPercent;
        var alpha = Math.Clamp(opacityPercent / 100.0, 0.7, 1.0);
        foreach (var card in _cardIds.Keys)
        {
            card.SetOpacity(alpha);
        }
    }

    private void ApplyFontColor(string hex)
    {
        if (!TryParseColor(hex, out var color))
        {
            return;
        }

        _config.FontColor = ToHex(color);
        SetTextBrush("TextBrush", color);
        SetTextBrush("TextSecondaryBrush", color);
        SetTextBrush("TextMutedBrush", color);
    }

    private static void SetTextBrush(string key, Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        Application.Current.Resources[key] = brush;
    }

    private static bool TryParseColor(string? text, out Color color)
    {
        color = Colors.Transparent;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        try
        {
            color = (Color)ColorConverter.ConvertFromString(text.Trim());
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string ToHex(Color color)
    {
        return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
    }

    private void ReloadLayout()
    {
        _config = _configService.Load();
        ApplyFontColor(_config.FontColor);
        AppScanner.MergeDesktopShortcuts(_config);
        BuildCards();
        WidgetNotes.SetNotes(_noteItems ?? new ObservableCollection<NoteItemViewModel>());
        ApplyWidgetSections();
        ApplyMode(_config.DesktopMode);
    }

    private void ScheduleSave()
    {
        _saveTimer.Stop();
        _saveTimer.Start();
    }

    private void ShowContextMenu(UIElement target, params MenuItemSpec[] items)
    {
        var menu = new ContextMenu();
        foreach (var item in items)
        {
            var menuItem = new MenuItem
            {
                Header = item.Header,
                Foreground = item.Danger ? (Brush)FindResource("RedBrush") : (Brush)FindResource("TextSecondaryBrush")
            };
            menuItem.Click += (_, _) => item.Action();
            menu.Items.Add(menuItem);
        }

        menu.PlacementTarget = target;
        menu.IsOpen = true;
    }

    private void OnWidgetDragDelta(object sender, DragDeltaEventArgs e)
    {
        if (!_config.WidgetMode)
        {
            return;
        }

        EnsureWidgetBounds();
        var maxLeft = Math.Max(0, SystemParameters.VirtualScreenWidth - _config.WidgetWidth);
        var maxTop = Math.Max(0, SystemParameters.VirtualScreenHeight - _config.WidgetHeight);
        _config.WidgetLeft = Math.Clamp(_config.WidgetLeft + e.HorizontalChange, 0, maxLeft);
        _config.WidgetTop = Math.Clamp(_config.WidgetTop + e.VerticalChange, 0, maxTop);

        if (_desktopLayer != null)
        {
            ApplyDesktopBoundsToLayer();
        }
        else
        {
            Left = _config.WidgetLeft;
            Top = _config.WidgetTop;
        }

        ScheduleSave();
    }

    private void OnWidgetSettingsClick(object sender, RoutedEventArgs e) => OpenSettings();

    private void OnWidgetExpandClick(object sender, RoutedEventArgs e)
    {
        _config.WidgetMode = false;
        ApplyMode(_config.DesktopMode);
    }

    private void OnWidgetHideClick(object sender, RoutedEventArgs e) => HideDashboard();

    private void OnSettingsClick(object sender, RoutedEventArgs e) => OpenSettings();

    private void OnMinClick(object sender, RoutedEventArgs e)
    {
        if (_config.DesktopMode)
        {
            HideDashboard();
        }
        else
        {
            WindowState = WindowState.Minimized;
        }
    }

    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        HideDashboard();
    }

    private sealed record MenuItemSpec(string Header, Action Action, bool Danger = false);
}
