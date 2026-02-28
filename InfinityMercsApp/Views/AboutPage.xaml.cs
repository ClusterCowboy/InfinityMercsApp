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
    private SKPicture? _attributionIcon7Picture;
    private SKPicture? _attributionIcon8Picture;
    private SKPicture? _attributionIcon9Picture;

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

    private async void OnNounUploadAttributionTapped(object? sender, TappedEventArgs e)
    {
        await Launcher.Default.OpenAsync("https://thenounproject.com/browse/icons/term/upload/");
    }

    private async void OnNounDoubleArrowsAttributionTapped(object? sender, TappedEventArgs e)
    {
        await Launcher.Default.OpenAsync("https://thenounproject.com/browse/icons/term/double-arrows/");
    }

    private async void OnNounCircuitAttributionTapped(object? sender, TappedEventArgs e)
    {
        await Launcher.Default.OpenAsync("https://thenounproject.com/browse/icons/term/circuit/");
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
        _attributionIcon7Picture?.Dispose();
        _attributionIcon7Picture = null;
        _attributionIcon8Picture?.Dispose();
        _attributionIcon8Picture = null;
        _attributionIcon9Picture?.Dispose();
        _attributionIcon9Picture = null;

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

        try
        {
            await using var stream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/NonCBIcons/noun-upload-2450840.svg");
            var svg = new SKSvg();
            _attributionIcon7Picture = svg.Load(stream);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"AboutPage attribution icon 7 load failed: {ex.Message}");
            _attributionIcon7Picture = null;
        }

        try
        {
            await using var stream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/NonCBIcons/noun-double-arrows-7302616.svg");
            var svg = new SKSvg();
            _attributionIcon8Picture = svg.Load(stream);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"AboutPage attribution icon 8 load failed: {ex.Message}");
            _attributionIcon8Picture = null;
        }

        try
        {
            await using var stream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/NonCBIcons/noun-circuit-8241852.svg");
            var svg = new SKSvg();
            _attributionIcon9Picture = svg.Load(stream);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"AboutPage attribution icon 9 load failed: {ex.Message}");
            _attributionIcon9Picture = null;
        }

        AttributionIconCanvas.InvalidateSurface();
        AttributionIcon2Canvas.InvalidateSurface();
        AttributionIcon4Canvas.InvalidateSurface();
        AttributionIcon5Canvas.InvalidateSurface();
        AttributionIcon6Canvas.InvalidateSurface();
        AttributionIcon7Canvas.InvalidateSurface();
        AttributionIcon8Canvas.InvalidateSurface();
        AttributionIcon9Canvas.InvalidateSurface();
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

    private void OnAttributionIcon7CanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        if (_attributionIcon7Picture is null)
        {
            return;
        }

        var bounds = _attributionIcon7Picture.CullRect;
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        var scale = Math.Min(e.Info.Width / bounds.Width, e.Info.Height / bounds.Height);
        var x = (e.Info.Width - (bounds.Width * scale)) / 2f;
        var y = (e.Info.Height - (bounds.Height * scale)) / 2f;

        canvas.Translate(x, y);
        canvas.Scale(scale);
        canvas.DrawPicture(_attributionIcon7Picture);
    }

    private void OnAttributionIcon8CanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        if (_attributionIcon8Picture is null)
        {
            return;
        }

        var bounds = _attributionIcon8Picture.CullRect;
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        var scale = Math.Min(e.Info.Width / bounds.Width, e.Info.Height / bounds.Height);
        var x = (e.Info.Width - (bounds.Width * scale)) / 2f;
        var y = (e.Info.Height - (bounds.Height * scale)) / 2f;

        canvas.Translate(x, y);
        canvas.Scale(scale);
        canvas.DrawPicture(_attributionIcon8Picture);
    }

    private void OnAttributionIcon9CanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        if (_attributionIcon9Picture is null)
        {
            return;
        }

        var bounds = _attributionIcon9Picture.CullRect;
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        var scale = Math.Min(e.Info.Width / bounds.Width, e.Info.Height / bounds.Height);
        var x = (e.Info.Width - (bounds.Width * scale)) / 2f;
        var y = (e.Info.Height - (bounds.Height * scale)) / 2f;

        canvas.Translate(x, y);
        canvas.Scale(scale);
        canvas.DrawPicture(_attributionIcon9Picture);
    }
}
