using InfinityMercsApp.ViewModels;
using SkiaSharp;
using SkiaSharp.Views.Maui;

namespace InfinityMercsApp.Views;

public partial class SplashPage
{
    private SKBitmap? _logoBitmap;
    private bool _logoLoadAttempted;

	public SplashPage(SplashPageViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_logoLoadAttempted)
        {
            return;
        }

        _logoLoadAttempted = true;
        await LoadLogoAsync();
    }

    private async Task LoadLogoAsync()
    {
        try
        {
            SKBitmap? bitmap = await TryLoadBitmapAsync("SVGCache/NonCBIcons/M2-no-bg-short.webp")
                ?? await TryLoadBitmapAsync("m2_no_bg_short.webp");

            _logoBitmap?.Dispose();
            _logoBitmap = bitmap;
            SplashLogoCanvas.InvalidateSurface();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Splash logo load failed: {ex.Message}");
        }
    }

    private static async Task<SKBitmap?> TryLoadBitmapAsync(string path)
    {
        try
        {
            await using var stream = await FileSystem.Current.OpenAppPackageFileAsync(path);
            return SKBitmap.Decode(stream);
        }
        catch
        {
            return null;
        }
    }

    private void OnSplashLogoPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        if (_logoBitmap is null || _logoBitmap.Width <= 0 || _logoBitmap.Height <= 0)
        {
            return;
        }

        var width = e.Info.Width;
        var height = e.Info.Height;
        var scale = Math.Min(width / (float)_logoBitmap.Width, height / (float)_logoBitmap.Height);

        var drawWidth = _logoBitmap.Width * scale;
        var drawHeight = _logoBitmap.Height * scale;
        var left = (width - drawWidth) / 2f;
        var top = (height - drawHeight) / 2f;

        var destination = new SKRect(left, top, left + drawWidth, top + drawHeight);
        canvas.DrawBitmap(_logoBitmap, destination);
    }
}
