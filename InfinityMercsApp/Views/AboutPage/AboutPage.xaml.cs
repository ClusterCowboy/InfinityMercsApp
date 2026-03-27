using SkiaSharp;
using SkiaSharp.Views.Maui;
using Svg.Skia;

namespace InfinityMercsApp.Views;

public partial class AboutPage : ContentPage
{
    private readonly Dictionary<int, SKPicture?> _attributionPictures = new();

    public AboutPage()
    {
        InitializeComponent();
        _ = LoadAttributionIconsAsync();
    }

    private static async Task OpenUriAsync(string uri)
    {
        await Launcher.Default.OpenAsync(uri);
    }

    private async void OnEmailTapped(object? sender, TappedEventArgs e)
    {
        await OpenUriAsync("mailto:jeremiahpatrick@protonmail.com");
    }

    private async void OnDiscordTapped(object? sender, TappedEventArgs e)
    {
        try
        {
            await OpenUriAsync("discord://");
        }
        catch
        {
            await OpenUriAsync("https://discord.com/app");
        }
    }

    private async void OnAttributionTapped(object? sender, TappedEventArgs e) => await OpenUriAsync("https://dazzleui.gumroad.com/l/dazzleiconsfree?ref=svgrepo.com");
    private async void OnSvgRepoAttributionTapped(object? sender, TappedEventArgs e) => await OpenUriAsync("https://www.svgrepo.com/svg/55949/cubes");
    private async void OnArrowCircleUpAttributionTapped(object? sender, TappedEventArgs e) => await OpenUriAsync("https://www.svgrepo.com/svg/491308/arrow-circle-up");
    private async void OnNounProjectAttributionTapped(object? sender, TappedEventArgs e) => await OpenUriAsync("https://thenounproject.com/browse/icons/term/arrow/");
    private async void OnNounEditAttributionTapped(object? sender, TappedEventArgs e) => await OpenUriAsync("https://thenounproject.com/browse/icons/term/edit/");
    private async void OnNounCircleArrowAttributionTapped(object? sender, TappedEventArgs e) => await OpenUriAsync("https://thenounproject.com/browse/icons/term/circle-arrow/");
    private async void OnNounFireAttributionTapped(object? sender, TappedEventArgs e) => await OpenUriAsync("https://thenounproject.com/browse/icons/term/fire/");
    private async void OnNounUploadAttributionTapped(object? sender, TappedEventArgs e) => await OpenUriAsync("https://thenounproject.com/browse/icons/term/upload/");
    private async void OnNounDoubleArrowsAttributionTapped(object? sender, TappedEventArgs e) => await OpenUriAsync("https://thenounproject.com/browse/icons/term/double-arrows/");
    private async void OnNounCircuitAttributionTapped(object? sender, TappedEventArgs e) => await OpenUriAsync("https://thenounproject.com/browse/icons/term/circuit/");
    private async void OnNounHackAttributionTapped(object? sender, TappedEventArgs e) => await OpenUriAsync("https://thenounproject.com/browse/icons/term/hack/");
    private async void OnNounTeamAttributionTapped(object? sender, TappedEventArgs e) => await OpenUriAsync("https://thenounproject.com/browse/icons/term/team/");
    private async void OnNounLeadershipAttributionTapped(object? sender, TappedEventArgs e) => await OpenUriAsync("https://thenounproject.com/browse/icons/term/leadership/");
    private async void OnNounAirborneAttributionTapped(object? sender, TappedEventArgs e) => await OpenUriAsync("https://thenounproject.com/browse/icons/term/airborne/");
    private async void OnNounBattleMechAttributionTapped(object? sender, TappedEventArgs e) => await OpenUriAsync("https://thenounproject.com/browse/icons/term/battle-mech/");
    private async void OnNounAssassinAttributionTapped(object? sender, TappedEventArgs e) => await OpenUriAsync("https://thenounproject.com/browse/icons/term/assassin/");
    private async void OnNounMalePlayerAttributionTapped(object? sender, TappedEventArgs e) => await OpenUriAsync("https://thenounproject.com/browse/icons/term/male-player/");
    private async void OnNounDuoAttributionTapped(object? sender, TappedEventArgs e) => await OpenUriAsync("https://thenounproject.com/browse/icons/term/duo/");
    private async void OnNounCheckAttributionTapped(object? sender, TappedEventArgs e) => await OpenUriAsync("https://thenounproject.com/browse/icons/term/check/");
    private async void OnNounXAttributionTapped(object? sender, TappedEventArgs e) => await OpenUriAsync("https://thenounproject.com/browse/icons/term/x/");
    private async void OnNounOpenFileAttributionTapped(object? sender, TappedEventArgs e) => await OpenUriAsync("https://thenounproject.com/browse/icons/term/open-file/");
    private async void OnNounTrashAttributionTapped(object? sender, TappedEventArgs e) => await OpenUriAsync("https://thenounproject.com/browse/icons/term/trash/");
    private async void OnNounCaptainAttributionTapped(object? sender, TappedEventArgs e) => await OpenUriAsync("https://thenounproject.com/browse/icons/term/captain/");
    private async void OnNoun5StarsAttributionTapped(object? sender, TappedEventArgs e) => await OpenUriAsync("https://thenounproject.com/browse/icons/term/5-stars/");
    private async void OnNounOptionsAttributionTapped(object? sender, TappedEventArgs e) => await OpenUriAsync("https://thenounproject.com/browse/icons/term/options/");
    private async void OnNounFilterAttributionTapped(object? sender, TappedEventArgs e) => await OpenUriAsync("https://thenounproject.com/browse/icons/term/filter/");
    private async void OnNounOkAttributionTapped(object? sender, TappedEventArgs e) => await OpenUriAsync("https://thenounproject.com/browse/icons/term/ok/");
    private async void OnNounRejectAttributionTapped(object? sender, TappedEventArgs e) => await OpenUriAsync("https://thenounproject.com/browse/icons/term/reject/");
    private async void OnNounDeltaAttributionTapped(object? sender, TappedEventArgs e) => await OpenUriAsync("https://thenounproject.com/browse/icons/term/delta/");
    private async void OnNounCyborgAttributionTapped(object? sender, TappedEventArgs e) => await OpenUriAsync("https://thenounproject.com/browse/icons/term/cyborg/");

    private async Task LoadAttributionIconsAsync()
    {
        foreach (var picture in _attributionPictures.Values)
        {
            picture?.Dispose();
        }

        _attributionPictures.Clear();

        _attributionPictures[1] = await TryLoadAttributionIconAsync(1, "SVGCache/CBIcons/cube2.svg");
        _attributionPictures[2] = await TryLoadAttributionIconAsync(2, "SVGCache/CBIcons/cube.svg");
        _attributionPictures[3] = await TryLoadAttributionIconAsync(3, "SVGCache/NonCBIcons/noun-edit-333556.svg");
        _attributionPictures[4] = await TryLoadAttributionIconAsync(4, "SVGCache/CBIcons/irregular.svg");
        _attributionPictures[5] = await TryLoadAttributionIconAsync(5, "SVGCache/CBIcons/regular.svg");
        _attributionPictures[6] = await TryLoadAttributionIconAsync(6, "SVGCache/CBIcons/impetuous.svg");
        _attributionPictures[7] = await TryLoadAttributionIconAsync(7, "SVGCache/CBIcons/lieutenant.svg");
        _attributionPictures[8] = await TryLoadAttributionIconAsync(8, "SVGCache/CBIcons/tactical.svg");
        _attributionPictures[9] = await TryLoadAttributionIconAsync(9, "SVGCache/CBIcons/hackable.svg");
        _attributionPictures[10] = await TryLoadAttributionIconAsync(10, "SVGCache/MercsIcons/noun-hack-2277937.svg");
        _attributionPictures[11] = await TryLoadAttributionIconAsync(11, "SVGCache/NonCBIcons/Fireteam/noun-team-base.svg");
        _attributionPictures[12] = await TryLoadAttributionIconAsync(12, "SVGCache/MercsIcons/noun-leadership-719245.svg", "SVGCache/MercsIcons/noun-leadership-7195245.svg");
        _attributionPictures[13] = await TryLoadAttributionIconAsync(13, "SVGCache/MercsIcons/noun-airborne-8005870.svg");
        _attributionPictures[14] = await TryLoadAttributionIconAsync(14, "SVGCache/MercsIcons/noun-battle-mech-1731140.svg");
        _attributionPictures[15] = await TryLoadAttributionIconAsync(15, "SVGCache/MercsIcons/noun-assassin-5981200.svg");
        _attributionPictures[16] = await TryLoadAttributionIconAsync(16, "SVGCache/MercsIcons/noun-male-player-2851844.svg");
        _attributionPictures[17] = await TryLoadAttributionIconAsync(17, "SVGCache/MercsIcons/noun-duo-2851835.svg");
        _attributionPictures[18] = await TryLoadAttributionIconAsync(18, "SVGCache/NonCBIcons/noun-check-3612574.svg");
        _attributionPictures[19] = await TryLoadAttributionIconAsync(19, "SVGCache/NonCBIcons/noun-x-1890844.svg");
        _attributionPictures[20] = await TryLoadAttributionIconAsync(20, "SVGCache/NonCBIcons/noun-open-file-8064123.svg");
        _attributionPictures[21] = await TryLoadAttributionIconAsync(21, "SVGCache/NonCBIcons/noun-trash-1523235.svg");
        _attributionPictures[22] = await TryLoadAttributionIconAsync(22, "SVGCache/NonCBIcons/noun-captain-8115950.svg");
        _attributionPictures[23] = await TryLoadAttributionIconAsync(23, "SVGCache/NonCBIcons/noun-5-stars-5872927.svg");
        _attributionPictures[24] = await TryLoadAttributionIconAsync(24, "SVGCache/NonCBIcons/noun-options-6682476.svg");
        _attributionPictures[25] = await TryLoadAttributionIconAsync(25, "SVGCache/NonCBIcons/noun-ok-5506550.svg");
        _attributionPictures[26] = await TryLoadAttributionIconAsync(26, "SVGCache/NonCBIcons/noun-reject-8230150.svg");
        _attributionPictures[27] = await TryLoadAttributionIconAsync(27, "SVGCache/NonCBIcons/noun-delta.svg");
        _attributionPictures[28] = await TryLoadAttributionIconAsync(28, "SVGCache/NonCBIcons/noun-filter.svg");
        _attributionPictures[29] = await TryLoadAttributionIconAsync(29, "SVGCache/units/2003-1.svg");

        InvalidateCanvases();
    }

    private static async Task<SKPicture?> TryLoadAttributionIconAsync(int iconNumber, params string[] paths)
    {
        Exception? lastException = null;

        foreach (var path in paths)
        {
            try
            {
                await using var stream = await FileSystem.Current.OpenAppPackageFileAsync(path);
                var svg = new SKSvg();
                return svg.Load(stream);
            }
            catch (Exception ex)
            {
                lastException = ex;
            }
        }

        if (lastException is not null)
        {
            Console.Error.WriteLine($"AboutPage attribution icon {iconNumber} load failed: {lastException.Message}");
        }

        return null;
    }

    private void InvalidateCanvases()
    {
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
        AttributionIcon29Canvas.InvalidateSurface();
    }

    private SKPicture? GetAttributionIcon(int iconNumber)
    {
        return _attributionPictures.GetValueOrDefault(iconNumber);
    }

    private static void DrawAttributionIcon(SKPaintSurfaceEventArgs e, SKPicture? picture)
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

        var scale = Math.Min(e.Info.Width / bounds.Width, e.Info.Height / bounds.Height);
        var x = (e.Info.Width - (bounds.Width * scale)) / 2f;
        var y = (e.Info.Height - (bounds.Height * scale)) / 2f;

        canvas.Translate(x, y);
        canvas.Scale(scale);
        canvas.DrawPicture(picture);
    }

    private void OnAttributionIconCanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e) => DrawAttributionIcon(e, GetAttributionIcon(1));
    private void OnAttributionIcon2CanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e) => DrawAttributionIcon(e, GetAttributionIcon(2));
    private void OnAttributionIcon3CanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e) => DrawAttributionIcon(e, GetAttributionIcon(3));
    private void OnAttributionIcon4CanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e) => DrawAttributionIcon(e, GetAttributionIcon(4));
    private void OnAttributionIcon5CanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e) => DrawAttributionIcon(e, GetAttributionIcon(5));
    private void OnAttributionIcon6CanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e) => DrawAttributionIcon(e, GetAttributionIcon(6));
    private void OnAttributionIcon7CanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e) => DrawAttributionIcon(e, GetAttributionIcon(7));
    private void OnAttributionIcon8CanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e) => DrawAttributionIcon(e, GetAttributionIcon(8));
    private void OnAttributionIcon9CanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e) => DrawAttributionIcon(e, GetAttributionIcon(9));
    private void OnAttributionIcon10CanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e) => DrawAttributionIcon(e, GetAttributionIcon(10));
    private void OnAttributionIcon11CanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e) => DrawAttributionIcon(e, GetAttributionIcon(11));
    private void OnAttributionIcon12CanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e) => DrawAttributionIcon(e, GetAttributionIcon(12));
    private void OnAttributionIcon13CanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e) => DrawAttributionIcon(e, GetAttributionIcon(13));
    private void OnAttributionIcon14CanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e) => DrawAttributionIcon(e, GetAttributionIcon(14));
    private void OnAttributionIcon15CanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e) => DrawAttributionIcon(e, GetAttributionIcon(15));
    private void OnAttributionIcon16CanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e) => DrawAttributionIcon(e, GetAttributionIcon(16));
    private void OnAttributionIcon17CanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e) => DrawAttributionIcon(e, GetAttributionIcon(17));
    private void OnAttributionIcon18CanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e) => DrawAttributionIcon(e, GetAttributionIcon(18));
    private void OnAttributionIcon19CanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e) => DrawAttributionIcon(e, GetAttributionIcon(19));
    private void OnAttributionIcon20CanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e) => DrawAttributionIcon(e, GetAttributionIcon(20));
    private void OnAttributionIcon21CanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e) => DrawAttributionIcon(e, GetAttributionIcon(21));
    private void OnAttributionIcon22CanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e) => DrawAttributionIcon(e, GetAttributionIcon(22));
    private void OnAttributionIcon23CanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e) => DrawAttributionIcon(e, GetAttributionIcon(23));
    private void OnAttributionIcon24CanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e) => DrawAttributionIcon(e, GetAttributionIcon(24));
    private void OnAttributionIcon25CanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e) => DrawAttributionIcon(e, GetAttributionIcon(25));
    private void OnAttributionIcon26CanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e) => DrawAttributionIcon(e, GetAttributionIcon(26));
    private void OnAttributionIcon27CanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e) => DrawAttributionIcon(e, GetAttributionIcon(27));
    private void OnAttributionIcon28CanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e) => DrawAttributionIcon(e, GetAttributionIcon(28));
    private void OnAttributionIcon29CanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e) => DrawAttributionIcon(e, GetAttributionIcon(29));
}
