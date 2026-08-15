using System;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.Windows.Media;
using WindowsDashboard.Services;
using WindowsDashboard.ViewModels;

namespace WindowsDashboard.Controls;

public partial class SystemMonitorCard : UserControl, IDisposable
{
    private readonly SystemMonitorService _service = new();
    private readonly ObservableCollection<MonitorItemViewModel> _items = new();

    public SystemMonitorCard()
    {
        InitializeComponent();

        _items.Add(new MonitorItemViewModel("CPU", new SolidColorBrush(Color.FromRgb(125, 155, 209))));
        _items.Add(new MonitorItemViewModel("Memory", new SolidColorBrush(Color.FromRgb(127, 195, 154))));
        _items.Add(new MonitorItemViewModel("C:", new SolidColorBrush(Color.FromRgb(161, 143, 209))));
        _items.Add(new MonitorItemViewModel("D:", new SolidColorBrush(Color.FromRgb(213, 189, 122))));
        _items.Add(new MonitorItemViewModel("GPU", new SolidColorBrush(Color.FromRgb(125, 155, 209))));
        _items.Add(new MonitorItemViewModel("Network", new SolidColorBrush(Color.FromRgb(127, 195, 154))));
        MonitorList.ItemsSource = _items;

        _service.Updated += OnSnapshot;
    }

    private void OnSnapshot(SystemMonitorSnapshot snapshot)
    {
        Dispatcher.BeginInvoke(new Action(() =>
        {
            Set(0, snapshot.CpuPercent, $"{snapshot.CpuPercent:0}%");
            Set(1, snapshot.MemoryPercent, $"{snapshot.MemoryPercent:0}%");
            Set(2, snapshot.CPercent, $"{snapshot.CPercent:0.0}%");
            Set(3, snapshot.DPercent, $"{snapshot.DPercent:0.0}%");
            Set(4, snapshot.GpuPercent, snapshot.GpuText);
            Set(5, snapshot.NetworkMbps, $"{snapshot.NetworkMbps:0.0} Mbps");
        }));
    }

    private void Set(int index, double percent, string text)
    {
        if (index >= _items.Count)
        {
            return;
        }

        _items[index].Percent = Math.Clamp(percent, 0, 100);
        _items[index].Text = text;
    }

    public void Dispose()
    {
        _service.Updated -= OnSnapshot;
        _service.Dispose();
        GC.SuppressFinalize(this);
    }
}
