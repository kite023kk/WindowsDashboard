using System.Windows;
using System.Windows.Controls;
using WindowsDashboard.Models;
using WindowsDashboard.Services;

namespace WindowsDashboard.Controls;

public partial class AIGroupCard : UserControl
{
    public AIGroupCard()
    {
        InitializeComponent();
    }

    private void OnToolClick(object sender, RoutedEventArgs e)
    {
        var target = ((Button)sender).Tag switch
        {
            "claude" => "claude",
            "codex" => "codex",
            "chatgpt" => "https://chatgpt.com/",
            _ => string.Empty
        };

        if (!string.IsNullOrWhiteSpace(target))
        {
            ProcessLauncher.Launch(new AppEntryConfig { Name = target, TargetPath = target });
        }
    }
}
