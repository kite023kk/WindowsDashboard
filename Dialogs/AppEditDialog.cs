using System.Windows;
using System.Windows.Controls;
using WindowsDashboard.Models;

namespace WindowsDashboard.Dialogs;

public sealed class AppEditDialog : Window
{
    private readonly TextBox _nameBox;
    private readonly TextBox _targetBox;
    private readonly TextBox _argumentsBox;
    private readonly TextBox _workingDirBox;

    public AppEditDialog(AppEntryConfig entry)
    {
        Title = "编辑应用";
        Width = 460;
        SizeToContent = SizeToContent.Height;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.NoResize;
        Background = (System.Windows.Media.Brush)Application.Current.FindResource("CardBrush");
        Foreground = (System.Windows.Media.Brush)Application.Current.FindResource("TextBrush");
        FontFamily = new System.Windows.Media.FontFamily("Segoe UI");

        var panel = new StackPanel { Margin = new Thickness(18) };
        _nameBox = AddField(panel, "名称", entry.Name);
        _targetBox = AddField(panel, "目标路径", entry.TargetPath);
        _argumentsBox = AddField(panel, "参数", entry.Arguments);
        _workingDirBox = AddField(panel, "工作目录", entry.WorkingDirectory);

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 14, 0, 0)
        };
        var ok = new Button { Content = "保存", Width = 76, Height = 30, Margin = new Thickness(0, 0, 8, 0) };
        ok.Click += (_, _) =>
        {
            AppName = _nameBox.Text.Trim();
            TargetPath = _targetBox.Text.Trim();
            Arguments = _argumentsBox.Text.Trim();
            WorkingDirectory = _workingDirBox.Text.Trim();
            DialogResult = true;
        };
        var cancel = new Button { Content = "取消", Width = 76, Height = 30 };
        cancel.Click += (_, _) => DialogResult = false;
        buttons.Children.Add(ok);
        buttons.Children.Add(cancel);
        panel.Children.Add(buttons);
        Content = panel;
    }

    public string AppName { get; private set; } = string.Empty;
    public string TargetPath { get; private set; } = string.Empty;
    public string Arguments { get; private set; } = string.Empty;
    public string WorkingDirectory { get; private set; } = string.Empty;

    private static TextBox AddField(Panel panel, string label, string value)
    {
        var text = new TextBlock
        {
            Text = label,
            FontSize = 12,
            Foreground = (System.Windows.Media.Brush)Application.Current.FindResource("TextSecondaryBrush"),
            Margin = new Thickness(0, 10, 0, 4)
        };
        var box = new TextBox
        {
            Text = value,
            FontSize = 12,
            Padding = new Thickness(6, 4, 6, 4),
            Background = (System.Windows.Media.Brush)Application.Current.FindResource("CardBorderBrush"),
            BorderBrush = (System.Windows.Media.Brush)Application.Current.FindResource("CardBorderBrush"),
            Foreground = (System.Windows.Media.Brush)Application.Current.FindResource("TextBrush")
        };
        panel.Children.Add(text);
        panel.Children.Add(box);
        return box;
    }
}
