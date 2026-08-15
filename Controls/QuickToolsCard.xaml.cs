using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WindowsDashboard.Models;
using WindowsDashboard.Services;
using WindowsDashboard.ViewModels;

namespace WindowsDashboard.Controls;

public partial class QuickToolsCard : UserControl
{
    public QuickToolsCard()
    {
        InitializeComponent();
    }

    public event Action<QuickToolViewModel>? ToolActivated;
    public event Action<QuickToolViewModel, Point>? ToolContextRequested;

    public IEnumerable ItemsSource => QuickGrid.ItemsSource ?? Array.Empty<object>();

    public void SetTools(IEnumerable<QuickToolViewModel> tools)
    {
        QuickGrid.ItemsSource = tools.ToList();
    }

    private void OnToolClick(object sender, RoutedEventArgs e)
    {
        if (((Button)sender).DataContext is QuickToolViewModel vm)
        {
            ToolActivated?.Invoke(vm);
        }
    }

    private void OnToolRightClick(object sender, MouseButtonEventArgs e)
    {
        if (FindAncestor<Button>((DependencyObject)e.OriginalSource) is { DataContext: QuickToolViewModel vm } button)
        {
            ToolContextRequested?.Invoke(vm, button.TranslatePoint(new Point(0, 0), this));
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
