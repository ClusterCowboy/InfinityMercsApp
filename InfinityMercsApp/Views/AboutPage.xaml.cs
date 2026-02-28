using SkiaSharp;
using SkiaSharp.Views.Maui;
using Svg.Skia;

namespace InfinityMercsApp.Views;

public partial class AboutPage : ContentPage
{
    private SKPicture? _attributionIconPicture;
    private SKPicture? _attributionIcon2Picture;
    private SKPicture? _attributionIcon3Picture;
    private SKPicture? _attributionIcon4Picture;
    private SKPicture? _attributionIcon5Picture;
    private SKPicture? _attributionIcon6Picture;

    public AboutPage()
    {
        InitializeComponent();
        _ = LoadAttributionIconsAsync();
    }

    private async void OnEmailTapped(object? sender, TappedEventArgs e)
    {
        await Launcher.Default.OpenAsync("mailto:jeremiahpatrick@protonmail.com");
    }

    private async void OnDiscordTapped(object? sender, TappedEventArgs e)
    {
        try
        {
            await Launcher.Default.OpenAsync("discord://");
        }
        catch
        {
            await Launcher.Default.OpenAsync("https://discord.com/app");
        }
    }

    private async void OnAttributionTapped(object? sender, TappedEventArgs e)
    {
        await Launcher.Default.OpenAsync("https://dazzleui.gumroad.com/l/dazzleiconsfree?ref=svgrepo.com");
    }

    private async void OnSvgRepoAttributionTapped(object? sender, TappedEventArgs e)
    {
        await Launcher.Default.OpenAsync("https://www.svgrepo.com/svg/55949/cubes");
    }

    private async void OnArrowCircleUpAttributionTapped(object? sender, TappedEventArgs e)
    {
        await Launcher.Default.OpenAsync("https://www.svgrepo.com/svg/491308/arrow-circle-up");
    }

    private async void OnNounProjectAttributionTapped(object? sender, TappedEventArgs e)
    {
        await Launcher.Default.OpenAsync("https://thenounproject.com/browse/icons/term/arrow/");
    }

    private async void OnNounCircleArrowAttributionTapped(object? sender, TappedEventArgs e)
    {
        await Launcher.Default.OpenAsync("https://thenounproject.com/browse/icons/term/circle-arrow/");
    }

    private async void OnNounFireAttributionTapped(object? sender, TappedEventArgs e)
    {
        await Launcher.Default.OpenAsync("https://thenounproject.com/browse/icons/term/fire/");
    }

    private async Task LoadAttributionIconsAsync()
    {
        _attributionIconPicture?.Dispose();
        _attributionIconPicture = null;
        _attributionIcon2Picture?.Dispose();
        _attributionIcon2Picture = null;
        _attributionIcon3Picture?.Dispose();
        _attributionIcon3Picture = null;
        _attributionIcon4Picture?.Dispose();
        _attributionIcon4Picture = null;
        _attributionIcon5Picture?.Dispose();
        _attributionIcon5Picture = null;
        _attributionIcon6Picture?.Dispose();
        _attributionIcon6Picture = null;

        try
        {
            await using var stream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/NonCBIcons/cubes-svgrepo-com.svg");
            var svg = new SKSvg();
            _attributionIconPicture = svg.Load(stream);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"AboutPage attribution icon 1 load failed: {ex.Message}");
            _attributionIconPicture = null;
        }

        try
        {
            await using var stream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/NonCBIcons/cube-alt-2-svgrepo-com.svg");
            var svg = new SKSvg();
            _attributionIcon2Picture = svg.Load(stream);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"AboutPage attribution icon 2 load failed: {ex.Message}");
            _attributionIcon2Picture = null;
        }

        try
        {
            await using var stream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/NonCBIcons/arrow-circle-up-svgrepo-com.svg");
            var svg = new SKSvg();
            _attributionIcon3Picture = svg.Load(stream);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"AboutPage attribution icon 3 load failed: {ex.Message}");
            _attributionIcon3Picture = null;
        }

        try
        {
            await using var stream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/NonCBIcons/noun-arrow-963008.svg");
            var svg = new SKSvg();
            _attributionIcon4Picture = svg.Load(stream);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"AboutPage attribution icon 4 load failed: {ex.Message}");
            _attributionIcon4Picture = null;
        }

        try
        {
            await using var stream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/NonCBIcons/noun-circle-arrow-803872.svg");
            var svg = new SKSvg();
            _attributionIcon5Picture = svg.Load(stream);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"AboutPage attribution icon 5 load failed: {ex.Message}");
            _attributionIcon5Picture = null;
        }

        try
        {
            await using var stream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/NonCBIcons/noun-fire-131591.svg");
            var svg = new SKSvg();
            _attributionIcon6Picture = svg.Load(stream);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"AboutPage attribution icon 6 load failed: {ex.Message}");
            _attributionIcon6Picture = null;
        }

        AttributionIconCanvas.InvalidateSurface();
        AttributionIcon2Canvas.InvalidateSurface();
        AttributionIcon4Canvas.InvalidateSurface();
        AttributionIcon5Canvas.InvalidateSurface();
        AttributionIcon6Canvas.InvalidateSurface();
    }

    private void OnAttributionIconCanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        if (_attributionIconPicture is null)
        {
            return;
        }

        var bounds = _attributionIconPicture.CullRect;
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        var scale = Math.Min(e.Info.Width / bounds.Width, e.Info.Height / bounds.Height);
        var x = (e.Info.Width - (bounds.Width * scale)) / 2f;
        var y = (e.Info.Height - (bounds.Height * scale)) / 2f;

        canvas.Translate(x, y);
        canvas.Scale(scale);
        canvas.DrawPicture(_attributionIconPicture);
    }

    private void OnAttributionIcon2CanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        if (_attributionIcon2Picture is null)
        {
            return;
        }

        var bounds = _attributionIcon2Picture.CullRect;
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        var scale = Math.Min(e.Info.Width / bounds.Width, e.Info.Height / bounds.Height);
        var x = (e.Info.Width - (bounds.Width * scale)) / 2f;
        var y = (e.Info.Height - (bounds.Height * scale)) / 2f;

        canvas.Translate(x, y);
        canvas.Scale(scale);
        canvas.DrawPicture(_attributionIcon2Picture);
    }

    private void OnAttributionIcon3CanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        if (_attributionIcon3Picture is null)
        {
            return;
        }

        var bounds = _attributionIcon3Picture.CullRect;
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        var scale = Math.Min(e.Info.Width / bounds.Width, e.Info.Height / bounds.Height);
        var x = (e.Info.Width - (bounds.Width * scale)) / 2f;
        var y = (e.Info.Height - (bounds.Height * scale)) / 2f;

        canvas.Translate(x, y);
        canvas.Scale(scale);
        canvas.DrawPicture(_attributionIcon3Picture);
    }

    private void OnAttributionIcon4CanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        if (_attributionIcon4Picture is null)
        {
            return;
        }

        var bounds = _attributionIcon4Picture.CullRect;
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        var scale = Math.Min(e.Info.Width / bounds.Width, e.Info.Height / bounds.Height);
        var x = (e.Info.Width - (bounds.Width * scale)) / 2f;
        var y = (e.Info.Height - (bounds.Height * scale)) / 2f;

        canvas.Translate(x, y);
        canvas.Scale(scale);
        canvas.DrawPicture(_attributionIcon4Picture);
    }

    private void OnAttributionIcon5CanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        if (_attributionIcon5Picture is null)
        {
            return;
        }

        var bounds = _attributionIcon5Picture.CullRect;
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        var scale = Math.Min(e.Info.Width / bounds.Width, e.Info.Height / bounds.Height);
        var x = (e.Info.Width - (bounds.Width * scale)) / 2f;
        var y = (e.Info.Height - (bounds.Height * scale)) / 2f;

        canvas.Translate(x, y);
        canvas.Scale(scale);
        canvas.DrawPicture(_attributionIcon5Picture);
    }

    private void OnAttributionIcon6CanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        if (_attributionIcon6Picture is null)
        {
            return;
        }

        var bounds = _attributionIcon6Picture.CullRect;
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        var scale = Math.Min(e.Info.Width / bounds.Width, e.Info.Height / bounds.Height);
        var x = (e.Info.Width - (bounds.Width * scale)) / 2f;
        var y = (e.Info.Height - (bounds.Height * scale)) / 2f;

        canvas.Translate(x, y);
        canvas.Scale(scale);
        canvas.DrawPicture(_attributionIcon6Picture);
    }
}
