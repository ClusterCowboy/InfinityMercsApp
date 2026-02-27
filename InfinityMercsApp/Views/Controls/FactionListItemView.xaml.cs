using InfinityMercsApp.ViewModels;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using Svg.Skia;

namespace InfinityMercsApp.Views.Controls;

public partial class FactionListItemView : ContentView
{
    private SKPicture? _svgPicture;

    public FactionListItemView()
    {
        InitializeComponent();
    }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();
        _ = LoadSvgFromCacheAsync();
    }

    private async Task LoadSvgFromCacheAsync()
    {
        _svgPicture?.Dispose();
        _svgPicture = null;

        var item = BindingContext as IViewerListItem;
        if (item is null)
        {
            LogoCanvas.InvalidateSurface();
            return;
        }

        try
        {
            Stream? stream = null;
            if (!string.IsNullOrWhiteSpace(item.CachedLogoPath) && File.Exists(item.CachedLogoPath))
            {
                stream = File.OpenRead(item.CachedLogoPath);
            }
            else if (!string.IsNullOrWhiteSpace(item.PackagedLogoPath))
            {
                stream = await FileSystem.Current.OpenAppPackageFileAsync(item.PackagedLogoPath);
            }

            if (stream is null)
            {
                LogoCanvas.InvalidateSurface();
                return;
            }

            await using (stream)
            {
            var svg = new SKSvg();
                _svgPicture = svg.Load(stream);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"FactionListItemView SVG load failed for '{item.CachedLogoPath ?? item.PackagedLogoPath}': {ex.Message}");
            _svgPicture = null;
        }

        LogoCanvas.InvalidateSurface();
    }

    private void OnLogoCanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        if (_svgPicture is null)
        {
            return;
        }

        var bounds = _svgPicture.CullRect;
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        var scale = Math.Min(e.Info.Width / bounds.Width, e.Info.Height / bounds.Height);
        var x = (e.Info.Width - (bounds.Width * scale)) / 2f;
        var y = (e.Info.Height - (bounds.Height * scale)) / 2f;

        canvas.Translate(x, y);
        canvas.Scale(scale);
        canvas.DrawPicture(_svgPicture);
    }
}
