using InfinityMercsApp.Domain.Models.Metadata;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using System.Text.Json;

namespace InfinityMercsApp.Views.Controls;

public partial class WeaponRangeBandBarView : ContentView
{
    public static readonly BindableProperty DistanceJsonProperty =
        BindableProperty.Create(
            nameof(DistanceJson),
            typeof(string),
            typeof(WeaponRangeBandBarView),
            null,
            propertyChanged: (bindable, _, _) => ((WeaponRangeBandBarView)bindable).Canvas.InvalidateSurface());

    public static readonly BindableProperty ShowUnitsInInchesProperty =
        BindableProperty.Create(
            nameof(ShowUnitsInInches),
            typeof(bool),
            typeof(WeaponRangeBandBarView),
            true,
            propertyChanged: (bindable, _, _) => ((WeaponRangeBandBarView)bindable).Canvas.InvalidateSurface());

    public static readonly BindableProperty BarHeightRequestProperty =
        BindableProperty.Create(
            nameof(BarHeightRequest),
            typeof(double),
            typeof(WeaponRangeBandBarView),
            88.0,
            propertyChanged: (bindable, _, newVal) =>
                ((WeaponRangeBandBarView)bindable).Canvas.HeightRequest = (double)newVal);

    public WeaponRangeBandBarView()
    {
        InitializeComponent();
    }

    public string? DistanceJson
    {
        get => (string?)GetValue(DistanceJsonProperty);
        set => SetValue(DistanceJsonProperty, value);
    }

    public bool ShowUnitsInInches
    {
        get => (bool)GetValue(ShowUnitsInInchesProperty);
        set => SetValue(ShowUnitsInInchesProperty, value);
    }

    public double BarHeightRequest
    {
        get => (double)GetValue(BarHeightRequestProperty);
        set => SetValue(BarHeightRequestProperty, value);
    }

    private void OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear();

        var bands = ParseBands(DistanceJson);
        if (bands.Count == 0)
            return;

        float totalRange = bands[^1].RangeEnd;
        if (totalRange <= 0)
            return;

        float canvasWidth = e.Info.Width;
        float canvasHeight = e.Info.Height;
        float density = (float)DeviceDisplay.MainDisplayInfo.Density;
        float cornerRadius = 4f * density;

        // Split: top 65% colored bands, bottom 35% white label strip
        float labelBarHeight = canvasHeight * 0.35f;
        float coloredHeight = canvasHeight - labelBarHeight;

        // Clip the entire drawing to a rounded rect so corners are handled automatically
        using var clipPath = new SKPath();
        clipPath.AddRoundRect(new SKRect(0, 0, canvasWidth, canvasHeight), cornerRadius, cornerRadius);
        canvas.Save();
        canvas.ClipPath(clipPath, SKClipOperation.Intersect, antialias: true);

        using var fillPaint = new SKPaint { IsAntialias = true };
        using var modPaint = new SKPaint { IsAntialias = true, Color = SKColors.Black };
        using var whitePaint = new SKPaint { Color = SKColors.White };
        using var dividerPaint = new SKPaint { Color = new SKColor(180, 180, 180), StrokeWidth = 1f };
        using var labelPaint = new SKPaint { IsAntialias = true, Color = SKColors.Black };

        using var modFont = new SKFont(SKTypeface.Default, coloredHeight * 0.52f) { Embolden = true };
        using var labelFont = new SKFont(SKTypeface.Default, labelBarHeight * 0.58f);

        // Collect band boundary x-positions as we draw
        var boundaries = new List<(float X, int Cm)> { (0f, 0) };
        float x = 0f;

        // Draw colored bands
        for (int i = 0; i < bands.Count; i++)
        {
            var band = bands[i];
            float bandWidth = (band.RangeEnd - band.RangeStart) / totalRange * canvasWidth;

            fillPaint.Color = GetBandColor(band.Mod);
            canvas.DrawRect(new SKRect(x, 0, x + bandWidth, coloredHeight), fillPaint);

            // Modifier text centered in the colored area
            float centerX = x + bandWidth / 2f;
            float centerY = coloredHeight / 2f + modFont.Size * 0.38f;
            canvas.DrawText(band.Mod, centerX, centerY, SKTextAlign.Center, modFont, modPaint);

            x += bandWidth;
            boundaries.Add((x, band.RangeEnd));
        }

        // White label strip
        canvas.DrawRect(new SKRect(0, coloredHeight, canvasWidth, canvasHeight), whitePaint);

        // Draw thin vertical divider lines and distance labels in the white strip
        float textY = coloredHeight + labelBarHeight / 2f + labelFont.Size * 0.38f;
        float edgePadding = 3f * density;

        for (int i = 0; i < boundaries.Count; i++)
        {
            var (bx, cm) = boundaries[i];
            string label = FormatDistanceLabel(cm, ShowUnitsInInches);

            // Thin divider line through the full height at interior boundaries
            if (i > 0 && i < boundaries.Count - 1)
                canvas.DrawLine(bx, 0, bx, coloredHeight, dividerPaint);

            // Number alignment: left-edge → left-align, right-edge → right-align, interior → center
            SKTextAlign align;
            float drawX;
            if (i == 0)
            {
                align = SKTextAlign.Left;
                drawX = bx + edgePadding;
            }
            else if (i == boundaries.Count - 1)
            {
                align = SKTextAlign.Right;
                drawX = bx - edgePadding;
            }
            else
            {
                align = SKTextAlign.Center;
                drawX = bx;
            }

            canvas.DrawText(label, drawX, textY, align, labelFont, labelPaint);
        }

        canvas.Restore();
    }

    private static IReadOnlyList<(string Label, int RangeStart, int RangeEnd, string Mod)> ParseBands(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return [];

        try
        {
            var distance = JsonSerializer.Deserialize<WeaponDistance>(json);
            return distance?.GetOrderedBands() ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static SKColor GetBandColor(string mod)
    {
        if (!int.TryParse(mod, out int value))
            return new SKColor(107, 114, 128);

        return value switch
        {
            > 0 => new SKColor(34, 197, 94),   // green
            0 => new SKColor(107, 114, 128),    // grey
            >= -3 => new SKColor(245, 158, 11), // amber
            _ => new SKColor(220, 38, 38)       // red
        };
    }

    private static string FormatDistanceLabel(int cm, bool showUnitsInInches)
    {
        return showUnitsInInches
            ? $"{(int)Math.Round(cm / 2.5, MidpointRounding.AwayFromZero)}\""
            : $"{cm}cm";
    }
}
