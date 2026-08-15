using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using WindowsDashboard.Models;

namespace WindowsDashboard.Dialogs;

public sealed class SettingsWindow : Window
{
    private readonly CheckBox _desktopBox;
    private readonly CheckBox _widgetBox;
    private readonly CheckBox _clockBox;
    private readonly CheckBox _quickBox;
    private readonly CheckBox _monitorBox;
    private readonly CheckBox _calendarBox;
    private readonly CheckBox _notesBox;
    private readonly CheckBox _autoStartBox;
    private readonly Slider _opacitySlider;
    private readonly TextBox _fontColorBox;
    private readonly ListBox _hiddenList;
    private readonly List<string> _restoredCards = new();

    public SettingsWindow(AppConfig config)
    {
        Title = "设置 - Windows 桌面整理器";
        Width = 520;
        SizeToContent = SizeToContent.Height;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.NoResize;
        Background = (System.Windows.Media.Brush)Application.Current.FindResource("CardBrush");
        Foreground = (System.Windows.Media.Brush)Application.Current.FindResource("TextBrush");
        FontFamily = new System.Windows.Media.FontFamily("Segoe UI");

        var panel = new StackPanel { Margin = new Thickness(20) };
        var title = new TextBlock
        {
            Text = "工作台设置",
            FontSize = 16,
            FontWeight = FontWeights.SemiBold,
            Foreground = (System.Windows.Media.Brush)Application.Current.FindResource("TextBrush")
        };
        panel.Children.Add(title);

        _desktopBox = AddCheckBox(panel, "桌面模式（嵌入 Windows 桌面层）", config.DesktopMode);
        _widgetBox = AddCheckBox(panel, "桌面小组件模式（紧凑小组件）", config.WidgetMode);
        _autoStartBox = AddCheckBox(panel, "开机启动", config.AutoStart);

        panel.Children.Add(new TextBlock
        {
            Text = "小组件内容（可同时显示多个）",
            FontSize = 12,
            Foreground = (System.Windows.Media.Brush)Application.Current.FindResource("TextSecondaryBrush"),
            Margin = new Thickness(0, 16, 0, 4)
        });
        _clockBox = AddCheckBox(panel, "时钟", config.WidgetShowClock);
        _quickBox = AddCheckBox(panel, "快捷工具", config.WidgetShowQuick);
        _monitorBox = AddCheckBox(panel, "系统监控", config.WidgetShowMonitor);
        _calendarBox = AddCheckBox(panel, "日历", config.WidgetShowCalendar);
        _notesBox = AddCheckBox(panel, "便签", config.WidgetShowNotes);

        panel.Children.Add(new TextBlock
        {
            Text = "透明度",
            FontSize = 12,
            Foreground = (System.Windows.Media.Brush)Application.Current.FindResource("TextSecondaryBrush"),
            Margin = new Thickness(0, 16, 0, 4)
        });
        _opacitySlider = new Slider
        {
            Minimum = 70,
            Maximum = 100,
            Value = config.OpacityPercent,
            Width = 280,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        panel.Children.Add(_opacitySlider);

        panel.Children.Add(new TextBlock
        {
            Text = "字体颜色",
            FontSize = 12,
            Foreground = (System.Windows.Media.Brush)Application.Current.FindResource("TextSecondaryBrush"),
            Margin = new Thickness(0, 16, 0, 4)
        });

        var fontColorBox = new TextBox
        {
            Text = string.IsNullOrWhiteSpace(config.FontColor) ? "#F6F7FA" : config.FontColor,
            Width = 130,
            Height = 26,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        _fontColorBox = fontColorBox;
        panel.Children.Add(fontColorBox);

        var colorRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 0, 0, 6)
        };
        foreach (var hex in new[] { "#F6F7FA", "#FFFFFF", "#B7BECE", "#D5BD7A", "#7FC39A", "#D4877F", "#A18FD1", "#111111" })
        {
            var swatch = new Button
            {
                Width = 26,
                Height = 26,
                Margin = new Thickness(0, 0, 6, 0),
                Background = SwatchBrush(hex),
                BorderBrush = (System.Windows.Media.Brush)Application.Current.FindResource("CardBorderBrush"),
                BorderThickness = new Thickness(1),
                ToolTip = hex,
                Tag = hex
            };
            swatch.Click += (_, e) =>
            {
                if (((Button)e.Source).Tag is string selected)
                {
                    fontColorBox.Text = selected;
                }
            };
            colorRow.Children.Add(swatch);
        }

        panel.Children.Add(colorRow);

        var actions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 16, 0, 0)
        };
        var rescan = new Button
        {
            Content = "重新扫描桌面快捷方式",
            Width = 170,
            Height = 32,
            Margin = new Thickness(0, 0, 8, 0)
        };
        rescan.Click += (_, _) => RescanShortcuts = true;
        var reset = new Button
        {
            Content = "重置卡片布局",
            Width = 120,
            Height = 32
        };
        reset.Click += (_, _) => ResetLayout = true;
        actions.Children.Add(rescan);
        actions.Children.Add(reset);
        panel.Children.Add(actions);

        panel.Children.Add(new TextBlock
        {
            Text = "隐藏 / 删除的卡片",
            FontSize = 12,
            Foreground = (System.Windows.Media.Brush)Application.Current.FindResource("TextSecondaryBrush"),
            Margin = new Thickness(0, 18, 0, 4)
        });
        _hiddenList = new ListBox
        {
            Height = 132,
            BorderBrush = (System.Windows.Media.Brush)Application.Current.FindResource("CardBorderBrush"),
            Background = (System.Windows.Media.Brush)Application.Current.FindResource("CardBorderBrush")
        };
        foreach (var card in config.Cards)
        {
            if (!card.State.Equals("Visible", StringComparison.OrdinalIgnoreCase))
            {
                var row = new DockPanel();
                var label = new TextBlock
                {
                    Text = card.Title,
                    FontSize = 12,
                    VerticalAlignment = VerticalAlignment.Center
                };
                var restore = new Button
                {
                    Content = "显示",
                    Width = 56,
                    Height = 26,
                    Tag = card.Id
                };
                restore.Click += (_, e) =>
                {
                    if (((Button)e.Source).Tag is string id)
                    {
                        _restoredCards.Add(id);
                        _hiddenList.Items.Remove(row);
                    }
                };
                DockPanel.SetDock(restore, Dock.Right);
                row.Children.Add(restore);
                row.Children.Add(label);
                _hiddenList.Items.Add(row);
            }
        }

        panel.Children.Add(_hiddenList);

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 18, 0, 0)
        };
        var ok = new Button { Content = "保存", Width = 90, Height = 32, Margin = new Thickness(0, 0, 8, 0) };
        ok.Click += (_, _) => Finish(true);
        var cancel = new Button { Content = "取消", Width = 90, Height = 32 };
        cancel.Click += (_, _) => Finish(false);
        buttons.Children.Add(ok);
        buttons.Children.Add(cancel);
        panel.Children.Add(buttons);
        Content = panel;
    }

    public bool DesktopMode => _desktopBox.IsChecked == true;
    public bool DesktopModeChanged { get; private set; }
    public bool WidgetMode => _widgetBox.IsChecked == true;
    public bool WidgetModeChanged { get; private set; }
    public bool ShowClockWidget => _clockBox.IsChecked == true;
    public bool ShowQuickWidget => _quickBox.IsChecked == true;
    public bool ShowMonitorWidget => _monitorBox.IsChecked == true;
    public bool ShowCalendarWidget => _calendarBox.IsChecked == true;
    public bool ShowNotesWidget => _notesBox.IsChecked == true;
    public bool AutoStart => _autoStartBox.IsChecked == true;
    public bool AutoStartChanged { get; private set; }
    public int OpacityPercent => (int)_opacitySlider.Value;
    public string FontColor => string.IsNullOrWhiteSpace(_fontColorBox.Text) ? "#F6F7FA" : _fontColorBox.Text.Trim();
    public bool RescanShortcuts { get; private set; }
    public bool ResetLayout { get; private set; }
    public IReadOnlyList<string> RestoredCards => _restoredCards;

    private void Finish(bool ok)
    {
        if (ok)
        {
            DesktopModeChanged = true;
            WidgetModeChanged = true;
            AutoStartChanged = true;
        }

        DialogResult = ok;
    }

    private static CheckBox AddCheckBox(Panel panel, string text, bool isChecked)
    {
        var box = new CheckBox
        {
            Content = text,
            IsChecked = isChecked,
            FontSize = 13,
            Foreground = (System.Windows.Media.Brush)Application.Current.FindResource("TextSecondaryBrush"),
            Margin = new Thickness(0, 12, 0, 0)
        };
        panel.Children.Add(box);
        return box;
    }

    private static System.Windows.Media.Brush SwatchBrush(string hex)
    {
        try
        {
            return new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hex));
        }
        catch
        {
            return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Gray);
        }
    }
}
