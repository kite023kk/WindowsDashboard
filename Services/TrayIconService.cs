using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace WindowsDashboard.Services;

public sealed class TrayIconService : IDisposable
{
    private NotifyIcon? _notifyIcon;

    public event Action? ShowRequested;
    public event Action? HideRequested;
    public event Action? ToggleModeRequested;
    public event Action? ToggleWidgetModeRequested;
    public event Action? SettingsRequested;
    public event Action? ReloadRequested;
    public event Action? ExitRequested;

    public void Show()
    {
        if (_notifyIcon != null)
        {
            return;
        }

        var menu = new ContextMenuStrip();
        menu.Items.Add("显示桌面整理器", null, (_, _) => ShowRequested?.Invoke());
        menu.Items.Add("隐藏桌面整理器", null, (_, _) => HideRequested?.Invoke());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("桌面模式", null, (_, _) => ToggleModeRequested?.Invoke());
        menu.Items.Add("普通模式", null, (_, _) => ToggleModeRequested?.Invoke());
        menu.Items.Add("桌面小组件模式", null, (_, _) => ToggleWidgetModeRequested?.Invoke());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("设置", null, (_, _) => SettingsRequested?.Invoke());
        menu.Items.Add("重新加载布局", null, (_, _) => ReloadRequested?.Invoke());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("退出", null, (_, _) => ExitRequested?.Invoke());

        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "app.ico");
        var icon = File.Exists(iconPath)
            ? new Icon(iconPath)
            : SystemIcons.Application;

        _notifyIcon = new NotifyIcon
        {
            Icon = icon,
            Text = "Windows 桌面整理器",
            ContextMenuStrip = menu,
            Visible = true
        };
        _notifyIcon.DoubleClick += (_, _) => ShowRequested?.Invoke();
    }

    public void Dispose()
    {
        if (_notifyIcon != null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _notifyIcon = null;
        }

        GC.SuppressFinalize(this);
    }
}
