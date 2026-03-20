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
    private SKPicture? _attributionIcon10Picture;
    private SKPicture? _attributionIcon11Picture;
    private SKPicture? _attributionIcon12Picture;
    private SKPicture? _attributionIcon13Picture;
    private SKPicture? _attributionIcon14Picture;
    private SKPicture? _attributionIcon15Picture;
    private SKPicture? _attributionIcon16Picture;
    private SKPicture? _attributionIcon17Picture;
    private SKPicture? _attributionIcon18Picture;
    private SKPicture? _attributionIcon19Picture;
    private SKPicture? _attributionIcon20Picture;
    private SKPicture? _attributionIcon21Picture;
    private SKPicture? _attributionIcon22Picture;
    private SKPicture? _attributionIcon23Picture;
    private SKPicture? _attributionIcon24Picture;
    private SKPicture? _attributionIcon25Picture;
    private SKPicture? _attributionIcon26Picture;
    private SKPicture? _attributionIcon27Picture;
    private SKPicture? _attributionIcon28Picture;

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

    private async void OnNounEditAttributionTapped(object? sender, TappedEventArgs e)
    {
        await Launcher.Default.OpenAsync("https://thenounproject.com/browse/icons/term/edit/");
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

    private async void OnNounHackAttributionTapped(object? sender, TappedEventArgs e)
    {
        await Launcher.Default.OpenAsync("https://thenounproject.com/browse/icons/term/hack/");
    }

    private async void OnNounTeamAttributionTapped(object? sender, TappedEventArgs e)
    {
        await Launcher.Default.OpenAsync("https://thenounproject.com/browse/icons/term/team/");
    }

    private async void OnNounLeadershipAttributionTapped(object? sender, TappedEventArgs e)
    {
        await Launcher.Default.OpenAsync("https://thenounproject.com/browse/icons/term/leadership/");
    }

    private async void OnNounAirborneAttributionTapped(object? sender, TappedEventArgs e)
    {
        await Launcher.Default.OpenAsync("https://thenounproject.com/browse/icons/term/airborne/");
    }

    private async void OnNounBattleMechAttributionTapped(object? sender, TappedEventArgs e)
    {
        await Launcher.Default.OpenAsync("https://thenounproject.com/browse/icons/term/battle-mech/");
    }

    private async void OnNounAssassinAttributionTapped(object? sender, TappedEventArgs e)
    {
        await Launcher.Default.OpenAsync("https://thenounproject.com/browse/icons/term/assassin/");
    }

    private async void OnNounMalePlayerAttributionTapped(object? sender, TappedEventArgs e)
    {
        await Launcher.Default.OpenAsync("https://thenounproject.com/browse/icons/term/male-player/");
    }

    private async void OnNounDuoAttributionTapped(object? sender, TappedEventArgs e)
    {
        await Launcher.Default.OpenAsync("https://thenounproject.com/browse/icons/term/duo/");
    }

    private async void OnNounCheckAttributionTapped(object? sender, TappedEventArgs e)
    {
        await Launcher.Default.OpenAsync("https://thenounproject.com/browse/icons/term/check/");
    }

    private async void OnNounXAttributionTapped(object? sender, TappedEventArgs e)
    {
        await Launcher.Default.OpenAsync("https://thenounproject.com/browse/icons/term/x/");
    }

    private async void OnNounOpenFileAttributionTapped(object? sender, TappedEventArgs e)
    {
        await Launcher.Default.OpenAsync("https://thenounproject.com/browse/icons/term/open-file/");
    }

    private async void OnNounTrashAttributionTapped(object? sender, TappedEventArgs e)
    {
        await Launcher.Default.OpenAsync("https://thenounproject.com/browse/icons/term/trash/");
    }

    private async void OnNounCaptainAttributionTapped(object? sender, TappedEventArgs e)
    {
        await Launcher.Default.OpenAsync("https://thenounproject.com/browse/icons/term/captain/");
    }

    private async void OnNoun5StarsAttributionTapped(object? sender, TappedEventArgs e)
    {
        await Launcher.Default.OpenAsync("https://thenounproject.com/browse/icons/term/5-stars/");
    }

    private async void OnNounOptionsAttributionTapped(object? sender, TappedEventArgs e)
    {
        await Launcher.Default.OpenAsync("https://thenounproject.com/browse/icons/term/options/");
    }

    private async void OnNounFilterAttributionTapped(object? sender, TappedEventArgs e)
    {
        await Launcher.Default.OpenAsync("https://thenounproject.com/browse/icons/term/filter/");
    }

    private async void OnNounOkAttributionTapped(object? sender, TappedEventArgs e)
    {
        await Launcher.Default.OpenAsync("https://thenounproject.com/browse/icons/term/ok/");
    }

    private async void OnNounRejectAttributionTapped(object? sender, TappedEventArgs e)
    {
        await Launcher.Default.OpenAsync("https://thenounproject.com/browse/icons/term/reject/");
    }

    private async void OnNounDeltaAttributionTapped(object? sender, TappedEventArgs e)
    {
        await Launcher.Default.OpenAsync("https://thenounproject.com/browse/icons/term/delta/");
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
        _attributionIcon10Picture?.Dispose();
        _attributionIcon10Picture = null;
        _attributionIcon11Picture?.Dispose();
        _attributionIcon11Picture = null;
        _attributionIcon12Picture?.Dispose();
        _attributionIcon12Picture = null;
        _attributionIcon13Picture?.Dispose();
        _attributionIcon13Picture = null;
        _attributionIcon14Picture?.Dispose();
        _attributionIcon14Picture = null;
        _attributionIcon15Picture?.Dispose();
        _attributionIcon15Picture = null;
        _attributionIcon16Picture?.Dispose();
        _attributionIcon16Picture = null;
        _attributionIcon17Picture?.Dispose();
        _attributionIcon17Picture = null;
        _attributionIcon18Picture?.Dispose();
        _attributionIcon18Picture = null;
        _attributionIcon19Picture?.Dispose();
        _attributionIcon19Picture = null;
        _attributionIcon20Picture?.Dispose();
        _attributionIcon20Picture = null;
        _attributionIcon21Picture?.Dispose();
        _attributionIcon21Picture = null;
        _attributionIcon22Picture?.Dispose();
        _attributionIcon22Picture = null;
        _attributionIcon23Picture?.Dispose();
        _attributionIcon23Picture = null;
        _attributionIcon24Picture?.Dispose();
        _attributionIcon24Picture = null;
        _attributionIcon25Picture?.Dispose();
        _attributionIcon25Picture = null;
        _attributionIcon26Picture?.Dispose();
        _attributionIcon26Picture = null;
        _attributionIcon27Picture?.Dispose();
        _attributionIcon27Picture = null;
        _attributionIcon28Picture?.Dispose();
        _attributionIcon28Picture = null;

        try
        {
            await using var stream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/CBIcons/cube2.svg");
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
            await using var stream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/CBIcons/cube.svg");
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
            await using var stream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/NonCBIcons/noun-edit-333556.svg");
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
            await using var stream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/CBIcons/irregular.svg");
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
            await using var stream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/CBIcons/regular.svg");
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
            await using var stream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/CBIcons/impetuous.svg");
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
            await using var stream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/CBIcons/lieutenant.svg");
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
            await using var stream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/CBIcons/tactical.svg");
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
            await using var stream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/CBIcons/hackable.svg");
            var svg = new SKSvg();
            _attributionIcon9Picture = svg.Load(stream);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"AboutPage attribution icon 9 load failed: {ex.Message}");
            _attributionIcon9Picture = null;
        }

        try
        {
            await using var stream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/MercsIcons/noun-hack-2277937.svg");
            var svg = new SKSvg();
            _attributionIcon10Picture = svg.Load(stream);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"AboutPage attribution icon 10 load failed: {ex.Message}");
            _attributionIcon10Picture = null;
        }

        try
        {
            await using var stream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/NonCBIcons/Fireteam/noun-team-base.svg");
            var svg = new SKSvg();
            _attributionIcon11Picture = svg.Load(stream);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"AboutPage attribution icon 11 load failed: {ex.Message}");
            _attributionIcon11Picture = null;
        }

        try
        {
            try
            {
                await using var stream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/MercsIcons/noun-leadership-719245.svg");
                var svg = new SKSvg();
                _attributionIcon12Picture = svg.Load(stream);
            }
            catch
            {
                await using var stream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/MercsIcons/noun-leadership-7195245.svg");
                var svg = new SKSvg();
                _attributionIcon12Picture = svg.Load(stream);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"AboutPage attribution icon 12 load failed: {ex.Message}");
            _attributionIcon12Picture = null;
        }

        try
        {
            await using var stream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/MercsIcons/noun-airborne-8005870.svg");
            var svg = new SKSvg();
            _attributionIcon13Picture = svg.Load(stream);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"AboutPage attribution icon 13 load failed: {ex.Message}");
            _attributionIcon13Picture = null;
        }

        try
        {
            await using var stream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/MercsIcons/noun-battle-mech-1731140.svg");
            var svg = new SKSvg();
            _attributionIcon14Picture = svg.Load(stream);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"AboutPage attribution icon 14 load failed: {ex.Message}");
            _attributionIcon14Picture = null;
        }

        try
        {
            await using var stream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/MercsIcons/noun-assassin-5981200.svg");
            var svg = new SKSvg();
            _attributionIcon15Picture = svg.Load(stream);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"AboutPage attribution icon 15 load failed: {ex.Message}");
            _attributionIcon15Picture = null;
        }

        try
        {
            await using var stream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/MercsIcons/noun-male-player-2851844.svg");
            var svg = new SKSvg();
            _attributionIcon16Picture = svg.Load(stream);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"AboutPage attribution icon 16 load failed: {ex.Message}");
            _attributionIcon16Picture = null;
        }

        try
        {
            await using var stream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/MercsIcons/noun-duo-2851835.svg");
            var svg = new SKSvg();
            _attributionIcon17Picture = svg.Load(stream);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"AboutPage attribution icon 17 load failed: {ex.Message}");
            _attributionIcon17Picture = null;
        }

        try
        {
            await using var stream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/NonCBIcons/noun-check-3612574.svg");
            var svg = new SKSvg();
            _attributionIcon18Picture = svg.Load(stream);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"AboutPage attribution icon 18 load failed: {ex.Message}");
            _attributionIcon18Picture = null;
        }

        try
        {
            await using var stream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/NonCBIcons/noun-x-1890844.svg");
            var svg = new SKSvg();
            _attributionIcon19Picture = svg.Load(stream);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"AboutPage attribution icon 19 load failed: {ex.Message}");
            _attributionIcon19Picture = null;
        }

        try
        {
            await using var stream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/NonCBIcons/noun-open-file-8064123.svg");
            var svg = new SKSvg();
            _attributionIcon20Picture = svg.Load(stream);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"AboutPage attribution icon 20 load failed: {ex.Message}");
            _attributionIcon20Picture = null;
        }

        try
        {
            await using var stream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/NonCBIcons/noun-trash-1523235.svg");
            var svg = new SKSvg();
            _attributionIcon21Picture = svg.Load(stream);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"AboutPage attribution icon 21 load failed: {ex.Message}");
            _attributionIcon21Picture = null;
        }

        try
        {
            await using var stream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/NonCBIcons/noun-captain-8115950.svg");
            var svg = new SKSvg();
            _attributionIcon22Picture = svg.Load(stream);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"AboutPage attribution icon 22 load failed: {ex.Message}");
            _attributionIcon22Picture = null;
        }

        try
        {
            await using var stream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/NonCBIcons/noun-5-stars-5872927.svg");
            var svg = new SKSvg();
            _attributionIcon23Picture = svg.Load(stream);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"AboutPage attribution icon 23 load failed: {ex.Message}");
            _attributionIcon23Picture = null;
        }

        try
        {
            await using var stream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/NonCBIcons/noun-options-6682476.svg");
            var svg = new SKSvg();
            _attributionIcon24Picture = svg.Load(stream);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"AboutPage attribution icon 24 load failed: {ex.Message}");
            _attributionIcon24Picture = null;
        }

        try
        {
            await using var stream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/NonCBIcons/noun-ok-5506550.svg");
            var svg = new SKSvg();
            _attributionIcon25Picture = svg.Load(stream);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"AboutPage attribution icon 25 load failed: {ex.Message}");
            _attributionIcon25Picture = null;
        }

        try
        {
            await using var stream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/NonCBIcons/noun-reject-8230150.svg");
            var svg = new SKSvg();
            _attributionIcon26Picture = svg.Load(stream);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"AboutPage attribution icon 26 load failed: {ex.Message}");
            _attributionIcon26Picture = null;
        }

        try
        {
            await using var stream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/NonCBIcons/noun-delta.svg");
            var svg = new SKSvg();
            _attributionIcon27Picture = svg.Load(stream);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"AboutPage attribution icon 27 load failed: {ex.Message}");
            _attributionIcon27Picture = null;
        }

        try
        {
            await using var stream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/NonCBIcons/noun-filter.svg");
            var svg = new SKSvg();
            _attributionIcon28Picture = svg.Load(stream);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"AboutPage attribution icon 28 load failed: {ex.Message}");
            _attributionIcon28Picture = null;
        }

        CbCubeCanvas.InvalidateSurface();
        CbCube2Canvas.InvalidateSurface();
        CbHackableCanvas.InvalidateSurface();
        CbImpetuousCanvas.InvalidateSurface();
        CbIrregularCanvas.InvalidateSurface();
        CbLieutenantCanvas.InvalidateSurface();
        CbRegularCanvas.InvalidateSurface();
        CbTacticalCanvas.InvalidateSurface();


        AttributionIcon3Canvas.InvalidateSurface();
        AttributionIcon10Canvas.InvalidateSurface();
        AttributionIcon11Canvas.InvalidateSurface();
        AttributionIcon12Canvas.InvalidateSurface();
        AttributionIcon13Canvas.InvalidateSurface();
        AttributionIcon14Canvas.InvalidateSurface();
        AttributionIcon15Canvas.InvalidateSurface();
        AttributionIcon16Canvas.InvalidateSurface();
        AttributionIcon17Canvas.InvalidateSurface();
        AttributionIcon18Canvas.InvalidateSurface();
        AttributionIcon19Canvas.InvalidateSurface();
        AttributionIcon20Canvas.InvalidateSurface();
        AttributionIcon21Canvas.InvalidateSurface();
        AttributionIcon22Canvas.InvalidateSurface();
        AttributionIcon23Canvas.InvalidateSurface();
        AttributionIcon24Canvas.InvalidateSurface();
        AttributionIcon25Canvas.InvalidateSurface();
        AttributionIcon26Canvas.InvalidateSurface();
        AttributionIcon27Canvas.InvalidateSurface();
        AttributionIcon28Canvas.InvalidateSurface();
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

    private void OnAttributionIcon10CanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        if (_attributionIcon10Picture is null)
        {
            return;
        }

        var bounds = _attributionIcon10Picture.CullRect;
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        var scale = Math.Min(e.Info.Width / bounds.Width, e.Info.Height / bounds.Height);
        var x = (e.Info.Width - (bounds.Width * scale)) / 2f;
        var y = (e.Info.Height - (bounds.Height * scale)) / 2f;

        canvas.Translate(x, y);
        canvas.Scale(scale);
        canvas.DrawPicture(_attributionIcon10Picture);
    }

    private void OnAttributionIcon11CanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        if (_attributionIcon11Picture is null)
        {
            return;
        }

        var bounds = _attributionIcon11Picture.CullRect;
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        var scale = Math.Min(e.Info.Width / bounds.Width, e.Info.Height / bounds.Height);
        var x = (e.Info.Width - (bounds.Width * scale)) / 2f;
        var y = (e.Info.Height - (bounds.Height * scale)) / 2f;

        canvas.Translate(x, y);
        canvas.Scale(scale);
        canvas.DrawPicture(_attributionIcon11Picture);
    }

    private void OnAttributionIcon12CanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        if (_attributionIcon12Picture is null)
        {
            return;
        }

        var bounds = _attributionIcon12Picture.CullRect;
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        var scale = Math.Min(e.Info.Width / bounds.Width, e.Info.Height / bounds.Height);
        var x = (e.Info.Width - (bounds.Width * scale)) / 2f;
        var y = (e.Info.Height - (bounds.Height * scale)) / 2f;

        canvas.Translate(x, y);
        canvas.Scale(scale);
        canvas.DrawPicture(_attributionIcon12Picture);
    }

    private void OnAttributionIcon13CanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        if (_attributionIcon13Picture is null)
        {
            return;
        }

        var bounds = _attributionIcon13Picture.CullRect;
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        var scale = Math.Min(e.Info.Width / bounds.Width, e.Info.Height / bounds.Height);
        var x = (e.Info.Width - (bounds.Width * scale)) / 2f;
        var y = (e.Info.Height - (bounds.Height * scale)) / 2f;

        canvas.Translate(x, y);
        canvas.Scale(scale);
        canvas.DrawPicture(_attributionIcon13Picture);
    }

    private void OnAttributionIcon14CanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        if (_attributionIcon14Picture is null)
        {
            return;
        }

        var bounds = _attributionIcon14Picture.CullRect;
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        var scale = Math.Min(e.Info.Width / bounds.Width, e.Info.Height / bounds.Height);
        var x = (e.Info.Width - (bounds.Width * scale)) / 2f;
        var y = (e.Info.Height - (bounds.Height * scale)) / 2f;

        canvas.Translate(x, y);
        canvas.Scale(scale);
        canvas.DrawPicture(_attributionIcon14Picture);
    }

    private void OnAttributionIcon15CanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        if (_attributionIcon15Picture is null)
        {
            return;
        }

        var bounds = _attributionIcon15Picture.CullRect;
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        var scale = Math.Min(e.Info.Width / bounds.Width, e.Info.Height / bounds.Height);
        var x = (e.Info.Width - (bounds.Width * scale)) / 2f;
        var y = (e.Info.Height - (bounds.Height * scale)) / 2f;

        canvas.Translate(x, y);
        canvas.Scale(scale);
        canvas.DrawPicture(_attributionIcon15Picture);
    }

    private void OnAttributionIcon16CanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        if (_attributionIcon16Picture is null)
        {
            return;
        }

        var bounds = _attributionIcon16Picture.CullRect;
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        var scale = Math.Min(e.Info.Width / bounds.Width, e.Info.Height / bounds.Height);
        var x = (e.Info.Width - (bounds.Width * scale)) / 2f;
        var y = (e.Info.Height - (bounds.Height * scale)) / 2f;

        canvas.Translate(x, y);
        canvas.Scale(scale);
        canvas.DrawPicture(_attributionIcon16Picture);
    }

    private void OnAttributionIcon17CanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        if (_attributionIcon17Picture is null)
        {
            return;
        }

        var bounds = _attributionIcon17Picture.CullRect;
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        var scale = Math.Min(e.Info.Width / bounds.Width, e.Info.Height / bounds.Height);
        var x = (e.Info.Width - (bounds.Width * scale)) / 2f;
        var y = (e.Info.Height - (bounds.Height * scale)) / 2f;

        canvas.Translate(x, y);
        canvas.Scale(scale);
        canvas.DrawPicture(_attributionIcon17Picture);
    }

    private void OnAttributionIcon18CanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        if (_attributionIcon18Picture is null)
        {
            return;
        }

        var bounds = _attributionIcon18Picture.CullRect;
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        var scale = Math.Min(e.Info.Width / bounds.Width, e.Info.Height / bounds.Height);
        var x = (e.Info.Width - (bounds.Width * scale)) / 2f;
        var y = (e.Info.Height - (bounds.Height * scale)) / 2f;

        canvas.Translate(x, y);
        canvas.Scale(scale);
        canvas.DrawPicture(_attributionIcon18Picture);
    }

    private void OnAttributionIcon19CanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        if (_attributionIcon19Picture is null)
        {
            return;
        }

        var bounds = _attributionIcon19Picture.CullRect;
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        var scale = Math.Min(e.Info.Width / bounds.Width, e.Info.Height / bounds.Height);
        var x = (e.Info.Width - (bounds.Width * scale)) / 2f;
        var y = (e.Info.Height - (bounds.Height * scale)) / 2f;

        canvas.Translate(x, y);
        canvas.Scale(scale);
        canvas.DrawPicture(_attributionIcon19Picture);
    }

    private void OnAttributionIcon20CanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        if (_attributionIcon20Picture is null)
        {
            return;
        }

        var bounds = _attributionIcon20Picture.CullRect;
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        var scale = Math.Min(e.Info.Width / bounds.Width, e.Info.Height / bounds.Height);
        var x = (e.Info.Width - (bounds.Width * scale)) / 2f;
        var y = (e.Info.Height - (bounds.Height * scale)) / 2f;

        canvas.Translate(x, y);
        canvas.Scale(scale);
        canvas.DrawPicture(_attributionIcon20Picture);
    }

    private void OnAttributionIcon21CanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        if (_attributionIcon21Picture is null)
        {
            return;
        }

        var bounds = _attributionIcon21Picture.CullRect;
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        var scale = Math.Min(e.Info.Width / bounds.Width, e.Info.Height / bounds.Height);
        var x = (e.Info.Width - (bounds.Width * scale)) / 2f;
        var y = (e.Info.Height - (bounds.Height * scale)) / 2f;

        canvas.Translate(x, y);
        canvas.Scale(scale);
        canvas.DrawPicture(_attributionIcon21Picture);
    }

    private void OnAttributionIcon22CanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        if (_attributionIcon22Picture is null)
        {
            return;
        }

        var bounds = _attributionIcon22Picture.CullRect;
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        var scale = Math.Min(e.Info.Width / bounds.Width, e.Info.Height / bounds.Height);
        var x = (e.Info.Width - (bounds.Width * scale)) / 2f;
        var y = (e.Info.Height - (bounds.Height * scale)) / 2f;

        canvas.Translate(x, y);
        canvas.Scale(scale);
        canvas.DrawPicture(_attributionIcon22Picture);
    }

    private void OnAttributionIcon23CanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        if (_attributionIcon23Picture is null)
        {
            return;
        }

        var bounds = _attributionIcon23Picture.CullRect;
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        var scale = Math.Min(e.Info.Width / bounds.Width, e.Info.Height / bounds.Height);
        var x = (e.Info.Width - (bounds.Width * scale)) / 2f;
        var y = (e.Info.Height - (bounds.Height * scale)) / 2f;

        canvas.Translate(x, y);
        canvas.Scale(scale);
        canvas.DrawPicture(_attributionIcon23Picture);
    }

    private void OnAttributionIcon24CanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        if (_attributionIcon24Picture is null)
        {
            return;
        }

        var bounds = _attributionIcon24Picture.CullRect;
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        var scale = Math.Min(e.Info.Width / bounds.Width, e.Info.Height / bounds.Height);
        var x = (e.Info.Width - (bounds.Width * scale)) / 2f;
        var y = (e.Info.Height - (bounds.Height * scale)) / 2f;

        canvas.Translate(x, y);
        canvas.Scale(scale);
        canvas.DrawPicture(_attributionIcon24Picture);
    }

    private void OnAttributionIcon25CanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        if (_attributionIcon25Picture is null)
        {
            return;
        }

        var bounds = _attributionIcon25Picture.CullRect;
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        var scale = Math.Min(e.Info.Width / bounds.Width, e.Info.Height / bounds.Height);
        var x = (e.Info.Width - (bounds.Width * scale)) / 2f;
        var y = (e.Info.Height - (bounds.Height * scale)) / 2f;

        canvas.Translate(x, y);
        canvas.Scale(scale);
        canvas.DrawPicture(_attributionIcon25Picture);
    }

    private void OnAttributionIcon26CanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        if (_attributionIcon26Picture is null)
        {
            return;
        }

        var bounds = _attributionIcon26Picture.CullRect;
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        var scale = Math.Min(e.Info.Width / bounds.Width, e.Info.Height / bounds.Height);
        var x = (e.Info.Width - (bounds.Width * scale)) / 2f;
        var y = (e.Info.Height - (bounds.Height * scale)) / 2f;

        canvas.Translate(x, y);
        canvas.Scale(scale);
        canvas.DrawPicture(_attributionIcon26Picture);
    }

    private void OnAttributionIcon27CanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        if (_attributionIcon27Picture is null)
        {
            return;
        }

        var bounds = _attributionIcon27Picture.CullRect;
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        var scale = Math.Min(e.Info.Width / bounds.Width, e.Info.Height / bounds.Height);
        var x = (e.Info.Width - (bounds.Width * scale)) / 2f;
        var y = (e.Info.Height - (bounds.Height * scale)) / 2f;

        canvas.Translate(x, y);
        canvas.Scale(scale);
        canvas.DrawPicture(_attributionIcon27Picture);
    }

    private void OnAttributionIcon28CanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        if (_attributionIcon28Picture is null)
        {
            return;
        }

        var bounds = _attributionIcon28Picture.CullRect;
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        var scale = Math.Min(e.Info.Width / bounds.Width, e.Info.Height / bounds.Height);
        var x = (e.Info.Width - (bounds.Width * scale)) / 2f;
        var y = (e.Info.Height - (bounds.Height * scale)) / 2f;

        canvas.Translate(x, y);
        canvas.Scale(scale);
        canvas.DrawPicture(_attributionIcon28Picture);
    }
}
