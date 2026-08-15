using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WindowsDashboard.ViewModels;

namespace WindowsDashboard.Controls;

public partial class GitCard : UserControl
{
    public GitCard()
    {
        InitializeComponent();
    }

    public event Action<GitItemViewModel, Point>? ContextRequested;

    public void SetProjects(IEnumerable<GitItemViewModel> projects)
    {
        GitList.ItemsSource = projects.ToList();
    }

    private void OnGitClick(object sender, RoutedEventArgs e)
    {
        if (((Button)sender).DataContext is GitItemViewModel vm)
        {
            ContextRequested?.Invoke(vm, ((Button)sender).TranslatePoint(new Point(0, 0), this));
        }
    }

    private void OnGitRightClick(object sender, MouseButtonEventArgs e)
    {
        if (FindAncestor<Button>((DependencyObject)e.OriginalSource) is { DataContext: GitItemViewModel vm } button)
        {
            ContextRequested?.Invoke(vm, button.TranslatePoint(new Point(0, 0), this));
            e.Handled = true;
        }
    }

    private static T? FindAncestor<T>(DependencyObject current) where T : DependencyObject
    {
        while (current != null)
        {
            if (current is T match)
            {
                return match;
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return null;
    }
}
