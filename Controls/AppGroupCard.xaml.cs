using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WindowsDashboard.ViewModels;

namespace WindowsDashboard.Controls;

public partial class AppGroupCard : UserControl
{
    public AppGroupCard()
    {
        InitializeComponent();
    }

    public event Action<AppItemViewModel>? AppActivated;
    public event Action<AppItemViewModel, Point>? AppContextRequested;
    public event Action? AddRequested;
    public event Action<IEnumerable<string>>? FilesDropped;

    public string CategoryName { get; set; } = string.Empty;

    public IEnumerable ItemsSource => AppList.ItemsSource ?? Array.Empty<object>();

    public void SetApps(IEnumerable<AppItemViewModel> apps)
    {
        AppList.ItemsSource = apps.ToList();
    }

    private void OnAppClick(object sender, RoutedEventArgs e)
    {
        if (((Button)sender).DataContext is AppItemViewModel vm)
        {
            AppActivated?.Invoke(vm);
        }
    }

    private void OnAppRightClick(object sender, MouseButtonEventArgs e)
    {
        if (FindAncestor<Button>((DependencyObject)e.OriginalSource) is { DataContext: AppItemViewModel vm } button)
        {
            AppContextRequested?.Invoke(vm, button.TranslatePoint(new Point(0, 0), this));
            e.Handled = true;
        }
    }

    private void OnAdd(object sender, RoutedEventArgs e)
    {
        AddRequested?.Invoke();
    }

    private void OnDragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void OnDrop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop) is string[] files && files.Length > 0)
        {
            FilesDropped?.Invoke(files);
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
