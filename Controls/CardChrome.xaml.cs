using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace WindowsDashboard.Controls;

public partial class CardChrome : UserControl
{
    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
        nameof(Title),
        typeof(string),
        typeof(CardChrome),
        new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty SubtitleProperty = DependencyProperty.Register(
        nameof(Subtitle),
        typeof(string),
        typeof(CardChrome),
        new PropertyMetadata(string.Empty));

    public CardChrome()
    {
        InitializeComponent();
        DragThumb.DragDelta += OnDragDelta;
        ResizeThumb.DragDelta += OnResizeDelta;
        MenuButton.Click += (_, _) => MenuRequested?.Invoke(this);
        PreviewMouseRightButtonUp += (_, e) =>
        {
            ContextRequested?.Invoke(this, e.GetPosition(this));
            e.Handled = true;
        };
    }

    public event Action<CardChrome, double, double>? Moved;
    public event Action<CardChrome, double, double>? Resized;
    public event Action<CardChrome>? MenuRequested;
    public event Action<CardChrome, Point>? ContextRequested;

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Subtitle
    {
        get => (string)GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }

    public void SetOpacity(double opacity)
    {
        Backdrop.Opacity = Math.Clamp(opacity, 0.7, 1);
    }

    private void OnDragDelta(object sender, DragDeltaEventArgs e)
    {
        Moved?.Invoke(this, e.HorizontalChange, e.VerticalChange);
    }

    private void OnResizeDelta(object sender, DragDeltaEventArgs e)
    {
        Resized?.Invoke(this, e.HorizontalChange, e.VerticalChange);
    }
}
