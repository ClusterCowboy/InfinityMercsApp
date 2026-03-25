using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

namespace InfinityMercsApp.Views.Controls;

/// <summary>
/// Displays left/right faction slots with icon canvases and tap handling.
/// </summary>
public partial class FactionSlotSelectorView : ContentView
{
    private static readonly Color ActiveBorder = Color.FromArgb("#2563EB");
    private static readonly Color InactiveBorder = Color.FromArgb("#9CA3AF");
    private SKPicture? _leftSlotPicture;
    private SKPicture? _rightSlotPicture;

    public static readonly BindableProperty LeftSlotBorderColorProperty =
        BindableProperty.Create(nameof(LeftSlotBorderColor), typeof(Color), typeof(FactionSlotSelectorView), Colors.Transparent);

    public static readonly BindableProperty RightSlotBorderColorProperty =
        BindableProperty.Create(nameof(RightSlotBorderColor), typeof(Color), typeof(FactionSlotSelectorView), Colors.Transparent);

    public static readonly BindableProperty ShowRightSelectionBoxProperty =
        BindableProperty.Create(nameof(ShowRightSelectionBox), typeof(bool), typeof(FactionSlotSelectorView), false);

    public event EventHandler? LeftSlotTapped;
    public event EventHandler? RightSlotTapped;

    public FactionSlotSelectorView()
    {
        InitializeComponent();
    }

    public Color LeftSlotBorderColor
    {
        get => (Color)GetValue(LeftSlotBorderColorProperty);
        set => SetValue(LeftSlotBorderColorProperty, value);
    }

    public Color RightSlotBorderColor
    {
        get => (Color)GetValue(RightSlotBorderColorProperty);
        set => SetValue(RightSlotBorderColorProperty, value);
    }

    public bool ShowRightSelectionBox
    {
        get => (bool)GetValue(ShowRightSelectionBoxProperty);
        set => SetValue(ShowRightSelectionBoxProperty, value);
    }

    public SKPicture? LeftSlotPicture
    {
        get => _leftSlotPicture;
        set
        {
            if (ReferenceEquals(_leftSlotPicture, value))
            {
                return;
            }

            _leftSlotPicture?.Dispose();
            _leftSlotPicture = value;
            LeftSlotCanvas.InvalidateSurface();
        }
    }

    public SKPicture? RightSlotPicture
    {
        get => _rightSlotPicture;
        set
        {
            if (ReferenceEquals(_rightSlotPicture, value))
            {
                return;
            }

            _rightSlotPicture?.Dispose();
            _rightSlotPicture = value;
            RightSlotCanvas.InvalidateSurface();
        }
    }

    /// <summary>
    /// Applies active/inactive border colors to slot selectors.
    /// </summary>
    public void ApplyActiveSlotBorders(int activeSlotIndex)
    {
        var activeIndex = activeSlotIndex == 1 && ShowRightSelectionBox ? 1 : 0;
        LeftSlotBorderColor = activeIndex == 0 ? ActiveBorder : InactiveBorder;
        RightSlotBorderColor = activeIndex == 1 ? ActiveBorder : InactiveBorder;
    }

    private void OnLeftSlotTapped(object? sender, TappedEventArgs e)
    {
        LeftSlotTapped?.Invoke(this, EventArgs.Empty);
    }

    private void OnRightSlotTapped(object? sender, TappedEventArgs e)
    {
        RightSlotTapped?.Invoke(this, EventArgs.Empty);
    }

    private void OnLeftSlotCanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        DrawSlotPicture(_leftSlotPicture, e);
    }

    private void OnRightSlotCanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        DrawSlotPicture(_rightSlotPicture, e);
    }

    private static void DrawSlotPicture(SKPicture? picture, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        if (picture is null)
        {
            return;
        }

        var bounds = picture.CullRect;
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        var width = e.Info.Width;
        var height = e.Info.Height;
        var scale = Math.Min(width / bounds.Width, height / bounds.Height);
        var x = (width - (bounds.Width * scale)) / 2f;
        var y = (height - (bounds.Height * scale)) / 2f;

        using var restore = new SKAutoCanvasRestore(canvas, true);
        canvas.Translate(x, y);
        canvas.Scale(scale);
        canvas.DrawPicture(picture);
    }
}
