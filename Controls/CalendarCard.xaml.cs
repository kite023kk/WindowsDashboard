using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace WindowsDashboard.Controls;

public partial class CalendarCard : UserControl
{
    private static readonly IReadOnlyDictionary<int, string> Events = new Dictionary<int, string>
    {
        [6] = "周报",
        [14] = "回顾",
        [20] = "发布",
        [27] = "复盘"
    };

    public CalendarCard()
    {
        InitializeComponent();
        BuildCalendar();
    }

    private void BuildCalendar()
    {
        var now = DateTime.Now;
        var year = now.Year;
        var month = now.Month;
        var firstDay = new DateTime(year, month, 1);
        var offset = (int)firstDay.DayOfWeek == 0 ? 6 : (int)firstDay.DayOfWeek - 1;
        var days = DateTime.DaysInMonth(year, month);

        for (var i = 0; i < 7; i++)
        {
            var label = new TextBlock
            {
                Text = "一二三四五六日"[i].ToString(),
                FontSize = 11,
                Foreground = (Brush)FindResource("TextMutedBrush"),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            WeekHeader.Children.Add(label);
        }

        for (var r = 0; r < 6; r++)
        {
            CalendarGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        }

        for (var c = 0; c < 7; c++)
        {
            CalendarGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        }

        for (var i = 0; i < 42; i++)
        {
            var day = i - offset + 1;
            var border = new Border
            {
                CornerRadius = new CornerRadius(6),
                Margin = new Thickness(1),
                Padding = new Thickness(0, 3, 0, 0)
            };

            var stack = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var number = new TextBlock
            {
                Text = day >= 1 && day <= days ? day.ToString() : string.Empty,
                FontSize = 12,
                Foreground = day == now.Day
                    ? (Brush)FindResource("TextBrush")
                    : (Brush)FindResource("TextSecondaryBrush"),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            stack.Children.Add(number);

            if (day == now.Day)
            {
                border.Background = new SolidColorBrush(Color.FromArgb(80, 125, 155, 209));
                border.BorderBrush = new SolidColorBrush(Color.FromArgb(120, 125, 155, 209));
                border.BorderThickness = new Thickness(1);
            }

            if (Events.TryGetValue(day, out var ev))
            {
                var evText = new TextBlock
                {
                    Text = ev,
                    FontSize = 10,
                    Foreground = (Brush)FindResource("YellowBrush"),
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                stack.Children.Add(evText);
            }

            border.Child = stack;
            Grid.SetRow(border, i / 7);
            Grid.SetColumn(border, i % 7);
            CalendarGrid.Children.Add(border);
        }
    }
}
