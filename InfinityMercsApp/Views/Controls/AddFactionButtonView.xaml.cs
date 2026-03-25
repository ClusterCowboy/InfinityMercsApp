using SkiaSharp;
using SkiaSharp.Views.Maui;

namespace InfinityMercsApp.Views.Controls;

public partial class AddFactionButtonView : ContentView
{
    public static readonly BindableProperty IsExpandedProperty =
        BindableProperty.Create(
            nameof(IsExpanded),
            typeof(bool),
            typeof(AddFactionButtonView),
            true,
            propertyChanged: (_, _, _) => { });

    public event EventHandler<TappedEventArgs>? Tapped;

    public AddFactionButtonView()
    {
        InitializeComponent();
        SetBinding(
            IsExpandedProperty,
            new Binding("ShowFactionStrip", mode: BindingMode.TwoWay));
    }

    public bool IsExpanded
    {
        get => (bool)GetValue(IsExpandedProperty);
        set => SetValue(IsExpandedProperty, value);
    }

    private void OnTapped(object? sender, TappedEventArgs e)
    {
        IsExpanded = !IsExpanded;
        Tapped?.Invoke(this, e);
        InvalidateButtonCanvas();
    }

    private void OnCanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        var stroke = Math.Max(3f, Math.Min(e.Info.Width, e.Info.Height) * 0.16f);
        var half = Math.Min(e.Info.Width, e.Info.Height) * 0.32f;
        var cx = e.Info.Width / 2f;
        var cy = e.Info.Height / 2f;

        using var paint = new SKPaint
        {
            Color = SKColor.Parse("22C55E"),
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = stroke,
            StrokeCap = SKStrokeCap.Round
        };

        canvas.DrawLine(cx - half, cy, cx + half, cy, paint);
        if (!IsExpanded)
        {
            canvas.DrawLine(cx, cy - half, cx, cy + half, paint);
        }
    }

    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        if (propertyName == IsExpandedProperty.PropertyName)
        {
            InvalidateButtonCanvas();
        }
    }

    private void InvalidateButtonCanvas()
    {
        if (Content is Border border && border.Content is SkiaSharp.Views.Maui.Controls.SKCanvasView canvas)
        {
            canvas.InvalidateSurface();
        }
    }
}
