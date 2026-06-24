using InfinityMercsApp.ViewModels;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using Svg.Skia;
using System.Windows.Input;
using InfinityMercsApp.Views;
using InfinityMercsApp.Views.Common;

namespace InfinityMercsApp.Views.Controls;

/// <summary>
/// A single faction tile (centred logo + name) used by the faction strip and the compact faction
/// selector overlay. Renders the faction logo SVG into one <see cref="SkiaSharp.Views.Maui.Controls.SKCanvasView"/>;
/// the SVG is parsed off the UI thread so filling the large faction list does not block on parsing.
/// </summary>
public partial class FactionListItemView : ContentView
{
    private SKPicture? _svgPicture;
    private int _logoLoadVersion;
    public event EventHandler? ItemTapped;

    public static readonly BindableProperty ItemTappedCommandProperty =
        BindableProperty.Create(
            nameof(ItemTappedCommand),
            typeof(ICommand),
            typeof(FactionListItemView));

    public static readonly BindableProperty ItemTappedCommandParameterProperty =
        BindableProperty.Create(
            nameof(ItemTappedCommandParameter),
            typeof(object),
            typeof(FactionListItemView));

    public static readonly BindableProperty TitleFormattedProperty =
        BindableProperty.Create(
            nameof(TitleFormatted),
            typeof(FormattedString),
            typeof(FactionListItemView),
            propertyChanged: OnTitleFormattedChanged);

    // Retained for binding compatibility: both call sites set UseVerticalTileLayout="True" in XAML.
    // The control now only renders the vertical tile, so the value is no longer acted on.
    public static readonly BindableProperty UseVerticalTileLayoutProperty =
        BindableProperty.Create(
            nameof(UseVerticalTileLayout),
            typeof(bool),
            typeof(FactionListItemView),
            true);

    public FactionListItemView()
    {
        InitializeComponent();
        UpdateTitleFormattingState(TitleFormatted);
        RefreshTitlePresentation();
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

    public FormattedString? TitleFormatted
    {
        get => (FormattedString?)GetValue(TitleFormattedProperty);
        set => SetValue(TitleFormattedProperty, value);
    }

    public bool HasTitleFormatted { get; private set; }

    public bool UseVerticalTileLayout
    {
        get => (bool)GetValue(UseVerticalTileLayoutProperty);
        set => SetValue(UseVerticalTileLayoutProperty, value);
    }

    private static void OnTitleFormattedChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is not FactionListItemView view)
        {
            return;
        }

        view.UpdateTitleFormattingState(newValue as FormattedString);
        view.RefreshTitlePresentation();
    }

    private void UpdateTitleFormattingState(FormattedString? formattedTitle)
    {
        var hasFormattedTitle = formattedTitle?.Spans is { Count: > 0 };
        if (HasTitleFormatted == hasFormattedTitle)
        {
            return;
        }

        HasTitleFormatted = hasFormattedTitle;
        OnPropertyChanged(nameof(HasTitleFormatted));
    }

    private void RefreshTitlePresentation()
    {
        VerticalFormattedNameLabel.IsVisible = HasTitleFormatted;
        VerticalNameLabel.IsVisible = !HasTitleFormatted;
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

        var item = BindingContext as IViewerListItem;
        if (item is null)
        {
            LogoCanvasVertical.InvalidateSurface();
            return;
        }

        SKPicture? loadedPicture = null;
        try
        {
            // Open and parse the logo off the UI thread: SKSvg.Load is synchronous CPU work and,
            // multiplied across the ~40-item faction list, blocked item creation. SKPicture is
            // immutable once built, so it is safe to hand back and draw on the UI thread later.
            loadedPicture = await Task.Run(async () =>
            {
                var stream = await OpenBestLogoStreamAsync(item);
                if (stream is null)
                {
                    return null;
                }

                await using (stream)
                {
                    var svg = new SKSvg();
                    return svg.Load(stream);
                }
            });
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"FactionListItemView SVG load failed for '{item.CachedLogoPath ?? item.PackagedLogoPath}': {ex.Message}");
            loadedPicture = null;
        }

        // A newer load (recycled item / fast rebind) superseded this one; drop the stale picture.
        if (loadVersion != _logoLoadVersion)
        {
            loadedPicture?.Dispose();
            return;
        }

        _svgPicture?.Dispose();
        _svgPicture = loadedPicture;
        LogoCanvasVertical.InvalidateSurface();
    }

    private static async Task<Stream?> OpenBestLogoStreamAsync(IViewerListItem item)
    {
        foreach (var cachedPath in BuildCachedCandidates(item))
        {
            if (!string.IsNullOrWhiteSpace(cachedPath) && File.Exists(cachedPath))
            {
                return File.OpenRead(cachedPath);
            }
        }

        foreach (var packagedPath in BuildPackagedCandidates(item))
        {
            if (string.IsNullOrWhiteSpace(packagedPath))
            {
                continue;
            }

            try
            {
                return await FileSystem.Current.OpenAppPackageFileAsync(packagedPath);
            }
            catch
            {
                // Try next candidate.
            }
        }

        return null;
    }

    private static IEnumerable<string?> BuildCachedCandidates(IViewerListItem item)
    {
        yield return item.CachedLogoPath;

        if (item is ICompanyMercsEntry mercEntry &&
            mercEntry.LogoSourceFactionId > 0 &&
            mercEntry.LogoSourceUnitId > 0)
        {
            yield return Path.Combine(
                FileSystem.Current.AppDataDirectory,
                "svg-cache",
                "units",
                $"{mercEntry.LogoSourceFactionId}-{mercEntry.LogoSourceUnitId}.svg");
        }

        if (item is ArmyUnitSelectionItem unit)
        {
            yield return Path.Combine(FileSystem.Current.AppDataDirectory, "svg-cache", "units", $"{unit.SourceFactionId}-{unit.Id}.svg");
        }
        else if (item is ArmyFactionSelectionItem faction)
        {
            yield return Path.Combine(FileSystem.Current.AppDataDirectory, "svg-cache", $"{faction.Id}.svg");
        }
    }

    private static IEnumerable<string?> BuildPackagedCandidates(IViewerListItem item)
    {
        yield return item.PackagedLogoPath;

        if (item is ICompanyMercsEntry mercEntry &&
            mercEntry.LogoSourceFactionId > 0 &&
            mercEntry.LogoSourceUnitId > 0)
        {
            yield return $"SVGCache/units/{mercEntry.LogoSourceFactionId}-{mercEntry.LogoSourceUnitId}.svg";
        }

        if (item is ArmyUnitSelectionItem unit)
        {
            yield return $"SVGCache/units/{unit.SourceFactionId}-{unit.Id}.svg";
        }
        else if (item is ArmyFactionSelectionItem faction)
        {
            yield return $"SVGCache/factions/{faction.Id}.svg";
        }
    }

    private void OnLogoCanvasVerticalPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
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

    private void OnItemTapped(object? sender, TappedEventArgs e)
    {
        ItemTapped?.Invoke(this, EventArgs.Empty);

        var parameter = ItemTappedCommandParameter ?? BindingContext;
        if (ItemTappedCommand?.CanExecute(parameter) == true)
        {
            ItemTappedCommand.Execute(parameter);
        }
    }
}
