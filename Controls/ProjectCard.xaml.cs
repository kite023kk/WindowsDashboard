using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using WindowsDashboard.Services;
using WindowsDashboard.ViewModels;

namespace WindowsDashboard.Controls;

public partial class ProjectCard : UserControl
{
    public ProjectCard()
    {
        InitializeComponent();
    }

    public void SetFolders(IEnumerable<ProjectFolderViewModel> folders)
    {
        FolderList.ItemsSource = folders.ToList();
    }

    private void OnOpen(object sender, RoutedEventArgs e)
    {
        if (((Button)sender).Tag is string path)
        {
            ProcessLauncher.OpenPath(path);
        }
    }
}
