using System.Collections;
using System.Windows.Input;

namespace InfinityMercsApp.Views.Controls;

public partial class FactionSelectionStripView : ContentView
{
    private double _panStartScrollX;
    private bool _isMouseDragging;
    private Point? _lastPointerPosition;

    public static readonly BindableProperty ItemsSourceProperty =
        BindableProperty.Create(nameof(ItemsSource), typeof(IEnumerable), typeof(FactionSelectionStripView));

    public static readonly BindableProperty SelectFactionCommandProperty =
        BindableProperty.Create(nameof(SelectFactionCommand), typeof(ICommand), typeof(FactionSelectionStripView));

    public FactionSelectionStripView()
    {
        InitializeComponent();
    }

    public IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public ICommand? SelectFactionCommand
    {
        get => (ICommand?)GetValue(SelectFactionCommandProperty);
        set => SetValue(SelectFactionCommandProperty, value);
    }

    private void OnFactionStripPanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        switch (e.StatusType)
        {
            case GestureStatus.Started:
                _panStartScrollX = FactionScrollView.ScrollX;
                break;
            case GestureStatus.Running:
                var targetX = Math.Max(0, _panStartScrollX - e.TotalX);
                _ = FactionScrollView.ScrollToAsync(targetX, 0, false);
                break;
        }
    }

    private void OnStripPointerPressed(object? sender, PointerEventArgs e)
    {
        _isMouseDragging = true;
        _lastPointerPosition = e.GetPosition(this);
    }

    private void OnStripPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isMouseDragging)
        {
            return;
        }

        var currentPosition = e.GetPosition(this);
        if (!currentPosition.HasValue || !_lastPointerPosition.HasValue)
        {
            return;
        }

        var deltaX = currentPosition.Value.X - _lastPointerPosition.Value.X;
        var targetX = Math.Max(0, FactionScrollView.ScrollX - deltaX);
        _ = FactionScrollView.ScrollToAsync(targetX, 0, false);
        _lastPointerPosition = currentPosition;
    }

    private void OnStripPointerReleased(object? sender, PointerEventArgs e)
    {
        _isMouseDragging = false;
        _lastPointerPosition = null;
    }
}
