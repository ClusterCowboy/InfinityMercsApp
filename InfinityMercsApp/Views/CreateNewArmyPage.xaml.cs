using SkiaSharp;
using SkiaSharp.Views.Maui;
using Svg.Skia;

namespace InfinityMercsApp.Views;

public partial class CreateNewArmyPage : ContentPage
{
    private SKPicture? _standardCompanyIconPicture;
    private SKPicture? _cohesiveCompanyIconPicture;
    private SKPicture? _inspiringLeaderIconPicture;
    private SKPicture? _airborneCompanyIconPicture;
    private SKPicture? _tagCompanyIconPicture;
    private SKPicture? _proxyPackIconPicture;

    public CreateNewArmyPage()
    {
        InitializeComponent();
        _ = LoadStandardCompanyIconAsync();
        _ = LoadCohesiveCompanyIconAsync();
        _ = LoadInspiringLeaderIconAsync();
        _ = LoadAirborneCompanyIconAsync();
        _ = LoadTagCompanyIconAsync();
        _ = LoadProxyPackIconAsync();
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//MainPage");
    }

    private async void OnStandardCompanyClicked(object? sender, EventArgs e)
    {
        await Navigation.PushModalAsync(new StandardCompanySourcePopupPage());
    }

    private void OnStandardCompanyTapped(object? sender, TappedEventArgs e)
    {
        OnStandardCompanyClicked(sender, EventArgs.Empty);
    }

    private void OnCohesiveCompanyClicked(object? sender, EventArgs e)
    {
        Console.WriteLine("[CreateNewArmyPage] Cohesive Company selected.");
    }

    private void OnCohesiveCompanyTapped(object? sender, TappedEventArgs e)
    {
        OnCohesiveCompanyClicked(sender, EventArgs.Empty);
    }

    private void OnInspiringLeaderClicked(object? sender, EventArgs e)
    {
        Console.WriteLine("[CreateNewArmyPage] Inspiring Leader selected.");
    }

    private void OnInspiringLeaderTapped(object? sender, TappedEventArgs e)
    {
        OnInspiringLeaderClicked(sender, EventArgs.Empty);
    }

    private void OnAirborneCompanyClicked(object? sender, EventArgs e)
    {
        Console.WriteLine("[CreateNewArmyPage] Airborne Company selected.");
    }

    private void OnAirborneCompanyTapped(object? sender, TappedEventArgs e)
    {
        OnAirborneCompanyClicked(sender, EventArgs.Empty);
    }

    private void OnTagCompanyClicked(object? sender, EventArgs e)
    {
        Console.WriteLine("[CreateNewArmyPage] TAG Company selected.");
    }

    private void OnTagCompanyTapped(object? sender, TappedEventArgs e)
    {
        OnTagCompanyClicked(sender, EventArgs.Empty);
    }

    private void OnProxyPackClicked(object? sender, EventArgs e)
    {
        Console.WriteLine("[CreateNewArmyPage] Proxy Pack selected.");
    }

    private void OnProxyPackTapped(object? sender, TappedEventArgs e)
    {
        OnProxyPackClicked(sender, EventArgs.Empty);
    }

    private async Task LoadStandardCompanyIconAsync()
    {
        _standardCompanyIconPicture?.Dispose();
        _standardCompanyIconPicture = null;

        try
        {
            await using var stream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/MercsIcons/noun-hack-2277937.svg");
            var svg = new SKSvg();
            _standardCompanyIconPicture = svg.Load(stream);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CreateNewArmyPage] Failed to load standard company icon: {ex.Message}");
        }

        StandardCompanyIconCanvas.InvalidateSurface();
    }

    private void OnStandardCompanyIconCanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        DrawIcon(_standardCompanyIconPicture, e);
    }

    private async Task LoadCohesiveCompanyIconAsync()
    {
        _cohesiveCompanyIconPicture?.Dispose();
        _cohesiveCompanyIconPicture = null;

        try
        {
            await using var stream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/MercsIcons/noun-team-7662436.svg");
            var svg = new SKSvg();
            _cohesiveCompanyIconPicture = svg.Load(stream);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CreateNewArmyPage] Failed to load cohesive company icon: {ex.Message}");
        }

        CohesiveCompanyIconCanvas.InvalidateSurface();
    }

    private void OnCohesiveCompanyIconCanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        DrawIcon(_cohesiveCompanyIconPicture, e);
    }

    private async Task LoadInspiringLeaderIconAsync()
    {
        _inspiringLeaderIconPicture?.Dispose();
        _inspiringLeaderIconPicture = null;

        try
        {
            try
            {
                await using var stream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/MercsIcons/noun-leadership-719245.svg");
                var svg = new SKSvg();
                _inspiringLeaderIconPicture = svg.Load(stream);
            }
            catch
            {
                await using var stream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/MercsIcons/noun-leadership-7195245.svg");
                var svg = new SKSvg();
                _inspiringLeaderIconPicture = svg.Load(stream);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CreateNewArmyPage] Failed to load inspiring leader icon: {ex.Message}");
        }

        InspiringLeaderIconCanvas.InvalidateSurface();
    }

    private void OnInspiringLeaderIconCanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        DrawIcon(_inspiringLeaderIconPicture, e);
    }

    private async Task LoadAirborneCompanyIconAsync()
    {
        _airborneCompanyIconPicture?.Dispose();
        _airborneCompanyIconPicture = null;

        try
        {
            await using var stream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/MercsIcons/noun-airborne-8005870.svg");
            var svg = new SKSvg();
            _airborneCompanyIconPicture = svg.Load(stream);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CreateNewArmyPage] Failed to load airborne company icon: {ex.Message}");
        }

        AirborneCompanyIconCanvas.InvalidateSurface();
    }

    private void OnAirborneCompanyIconCanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        DrawIcon(_airborneCompanyIconPicture, e);
    }

    private async Task LoadTagCompanyIconAsync()
    {
        _tagCompanyIconPicture?.Dispose();
        _tagCompanyIconPicture = null;

        try
        {
            await using var stream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/MercsIcons/noun-battle-mech-1731140.svg");
            var svg = new SKSvg();
            _tagCompanyIconPicture = svg.Load(stream);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CreateNewArmyPage] Failed to load TAG company icon: {ex.Message}");
        }

        TagCompanyIconCanvas.InvalidateSurface();
    }

    private void OnTagCompanyIconCanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        DrawIcon(_tagCompanyIconPicture, e);
    }

    private async Task LoadProxyPackIconAsync()
    {
        _proxyPackIconPicture?.Dispose();
        _proxyPackIconPicture = null;

        try
        {
            await using var stream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/MercsIcons/noun-assassin-5981200.svg");
            var svg = new SKSvg();
            _proxyPackIconPicture = svg.Load(stream);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CreateNewArmyPage] Failed to load proxy pack icon: {ex.Message}");
        }

        ProxyPackIconCanvas.InvalidateSurface();
    }

    private void OnProxyPackIconCanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        DrawIcon(_proxyPackIconPicture, e);
    }

    private static void DrawIcon(SKPicture? picture, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        if (picture is null)
        {
            return;
        }

        var width = e.Info.Width;
        var height = e.Info.Height;
        if (width <= 0 || height <= 0)
        {
            return;
        }

        var source = picture.CullRect;
        if (source.Width <= 0 || source.Height <= 0)
        {
            return;
        }

        var scale = Math.Min(width / source.Width, height / source.Height);
        var scaledWidth = source.Width * scale;
        var scaledHeight = source.Height * scale;
        var left = (width - scaledWidth) / 2f;
        var top = (height - scaledHeight) / 2f;

        canvas.Translate(left, top);
        canvas.Scale(scale);
        canvas.DrawPicture(picture);
    }
}
