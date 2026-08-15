using System;
using System.Windows.Controls;
using System.Windows.Threading;

namespace WindowsDashboard.Controls;

public partial class ClockCard : UserControl
{
    private readonly DispatcherTimer _timer;
    private static readonly string[] Weathers = { "多云 29°C", "晴 31°C", "小雨 26°C", "阴 28°C" };

    public ClockCard()
    {
        InitializeComponent();
        WeatherText.Text = Weathers[DateTime.Now.Day % Weathers.Length];
        UpdateClock();
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _timer.Tick += (_, _) => UpdateClock();
        _timer.Start();
    }

    private void UpdateClock()
    {
        var now = DateTime.Now;
        TimeText.Text = now.ToString("HH:mm:ss");
        DateText.Text = now.ToString("yyyy年M月d日");
        WeekText.Text = "星期" + "日一二三四五六"[(int)now.DayOfWeek];
    }
}
