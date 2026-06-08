using InfinityMercsApp.ViewModels;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using Svg.Skia;
using System.Windows.Input;

namespace InfinityMercsApp.Views.Controls;

public partial class CompanyViewerUnitTileView : ContentView
{
    private const int TagCompanyFactionId = 2003;
    private const string TagCompanyFallbackIconPath = "SVGCache/MercsIcons/noun-battle-mech-1731140.svg";

    private SKPicture? _svgPicture;
    private SKPicture? _captainBadgePicture;
    private SKPicture? _experienceBadgePicture;
    private int _logoLoadVersion;
    private double _panLastTotalX;

    public static readonly BindableProperty ItemTappedCommandProperty =
        BindableProperty.Create(
            nameof(ItemTappedCommand),
            typeof(ICommand),
            typeof(CompanyViewerUnitTileView));

    public static readonly BindableProperty ItemTappedCommandParameterProperty =
        BindableProperty.Create(
            nameof(ItemTappedCommandParameter),
            typeof(object),
            typeof(CompanyViewerUnitTileView));

    public static readonly BindableProperty PanScrollCommandProperty =
        BindableProperty.Create(
            nameof(PanScrollCommand),
            typeof(ICommand),
            typeof(CompanyViewerUnitTileView));

    public CompanyViewerUnitTileView()
    {
        InitializeComponent();
    }

    public ICommand? ItemTappedCommand
    {
        get => (ICommand?)GetValue(ItemTappedCommandProperty);
        set => SetValue(ItemTappedCommandProperty, value);
    }

    public object? ItemTappedCommandParameter
    {
        get => GetValue(ItemTappedCommandParameterProperty);
        set => SetValue(ItemTappedCommandParameterProperty, value);
    }

    public ICommand? PanScrollCommand
    {
        get => (ICommand?)GetValue(PanScrollCommandProperty);
        set => SetValue(PanScrollCommandProperty, value);
    }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();
        _ = LoadSvgFromCacheAsync();
    }

    private async Task LoadSvgFromCacheAsync()
    {
        var loadVersion = ++_logoLoadVersion;
        _svgPicture?.Dispose();
        _svgPicture = null;
        _captainBadgePicture?.Dispose();
        _captainBadgePicture = null;
        _experienceBadgePicture?.Dispose();
        _experienceBadgePicture = null;

        var item = BindingContext as IViewerListItem;
        if (item is null)
        {
            LogoCanvas.InvalidateSurface();
            CaptainBadgeCanvas.InvalidateSurface();
            ExperienceBadgeCanvas.InvalidateSurface();
            return;
        }

        try
        {
            Stream? stream = await OpenBestLogoStreamAsync(item);
            if (stream is null)
            {
                if (loadVersion != _logoLoadVersion)
                {
                    return;
                }

                LogoCanvas.InvalidateSurface();
                CaptainBadgeCanvas.InvalidateSurface();
                ExperienceBadgeCanvas.InvalidateSurface();
                return;
            }

            SKPicture? loadedPicture = null;
            await using (stream)
            {
                var svg = new SKSvg();
                loadedPicture = svg.Load(stream);
            }

            if (loadVersion != _logoLoadVersion)
            {
                loadedPicture?.Dispose();
                return;
            }

            _svgPicture?.Dispose();
            _svgPicture = loadedPicture;

            if (item is CompanyViewerUnitListItem companyItem)
            {
                _captainBadgePicture = await LoadPictureAsync(companyItem.CaptainIconPackagedPath);
                _experienceBadgePicture = await LoadPictureAsync(companyItem.ExperienceIconPackagedPath);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"CompanyViewerUnitTileView SVG load failed: {ex.Message}");
            _svgPicture = null;
        }

        LogoCanvas.InvalidateSurface();
        CaptainBadgeCanvas.InvalidateSurface();
        ExperienceBadgeCanvas.InvalidateSurface();
    }

    private static async Task<Stream?> OpenBestLogoStreamAsync(IViewerListItem item)
    {
        if (!string.IsNullOrWhiteSpace(item.CachedLogoPath) && File.Exists(item.CachedLogoPath))
        {
            return File.OpenRead(item.CachedLogoPath);
        }

        if (!string.IsNullOrWhiteSpace(item.PackagedLogoPath))
        {
            foreach (var candidate in BuildPackagedCandidates(item.PackagedLogoPath))
            {
                try
                {
                    return await FileSystem.Current.OpenAppPackageFileAsync(candidate);
                }
                catch
                {
                    // Try next candidate.
                }
            }
        }

        foreach (var fallback in BuildFallbackLogoCandidates(item))
        {
            try
            {
                return await FileSystem.Current.OpenAppPackageFileAsync(fallback);
            }
            catch
            {
                // Try next fallback.
            }
        }

        return null;
    }

    private static IEnumerable<string> BuildPackagedCandidates(string packagedPath)
    {
        var normalized = packagedPath.Replace('\\', '/').TrimStart('/');
        yield return normalized;
        yield return normalized.ToLowerInvariant();
    }

    private static IEnumerable<string> BuildFallbackLogoCandidates(IViewerListItem item)
    {
        if (item is not CompanyViewerUnitListItem companyItem)
        {
            yield break;
        }

        var isTagCompanyUnit = companyItem.VisualFactionId == TagCompanyFactionId ||
                               companyItem.SourceFactionId == TagCompanyFactionId ||
                               companyItem.BaseUnitName.Contains("Repurposed Mining Equipment", StringComparison.OrdinalIgnoreCase) ||
                               companyItem.BaseUnitName.Contains("Turtlemek", StringComparison.OrdinalIgnoreCase);

        if (isTagCompanyUnit)
        {
            yield return TagCompanyFallbackIconPath;
        }
    }

    private static async Task<SKPicture?> LoadPictureAsync(string? packagedPath)
    {
        if (string.IsNullOrWhiteSpace(packagedPath))
        {
            return null;
        }

        foreach (var candidate in BuildPackagedCandidates(packagedPath))
        {
            try
            {
                await using var stream = await FileSystem.Current.OpenAppPackageFileAsync(candidate);
                var svg = new SKSvg();
                return svg.Load(stream);
            }
            catch
            {
                // Try next candidate.
            }
        }

        return null;
    }

    private void OnLogoCanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        DrawPictureOnCanvas(e, _svgPicture);
    }

    private void OnCaptainBadgeCanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        DrawPictureOnCanvas(e, _captainBadgePicture);
    }

    private void OnExperienceBadgeCanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        DrawPictureOnCanvas(e, _experienceBadgePicture);
    }

    private static void DrawPictureOnCanvas(SKPaintSurfaceEventArgs e, SKPicture? picture)
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

    private void OnTilePanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        switch (e.StatusType)
        {
            case GestureStatus.Started:
                _panLastTotalX = 0d;
                break;
            case GestureStatus.Running:
                var deltaX = e.TotalX - _panLastTotalX;
                _panLastTotalX = e.TotalX;
                if (PanScrollCommand?.CanExecute(deltaX) == true)
                    PanScrollCommand.Execute(deltaX);
                break;
            case GestureStatus.Completed:
            case GestureStatus.Canceled:
                _panLastTotalX = 0d;
                break;
        }
    }

    private void OnTileTapped(object? sender, TappedEventArgs e)
    {
        var parameter = ItemTappedCommandParameter ?? BindingContext;
        if (ItemTappedCommand?.CanExecute(parameter) == true)
        {
            ItemTappedCommand.Execute(parameter);
        }
    }
}
