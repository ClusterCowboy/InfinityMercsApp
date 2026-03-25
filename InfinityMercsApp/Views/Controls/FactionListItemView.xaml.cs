using InfinityMercsApp.ViewModels;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using Svg.Skia;
using System.Windows.Input;
using InfinityMercsApp.Views;
using InfinityMercsApp.Views.Common;

namespace InfinityMercsApp.Views.Controls;

public partial class FactionListItemView : ContentView
{
    private SKPicture? _svgPicture;
    private SKPicture? _rightPrimaryPicture;
    private SKPicture? _rightSecondaryPicture;
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
    public static readonly BindableProperty TrailingTextProperty =
        BindableProperty.Create(
            nameof(TrailingText),
            typeof(string),
            typeof(FactionListItemView),
            string.Empty,
            propertyChanged: OnTrailingTextChanged);
    public static readonly BindableProperty HasTrailingTextProperty =
        BindableProperty.Create(
            nameof(HasTrailingText),
            typeof(bool),
            typeof(FactionListItemView),
            false);
    public static readonly BindableProperty RightPrimaryIconPackagedPathProperty =
        BindableProperty.Create(
            nameof(RightPrimaryIconPackagedPath),
            typeof(string),
            typeof(FactionListItemView),
            string.Empty,
            propertyChanged: OnRightIconPathChanged);
    public static readonly BindableProperty RightSecondaryIconPackagedPathProperty =
        BindableProperty.Create(
            nameof(RightSecondaryIconPackagedPath),
            typeof(string),
            typeof(FactionListItemView),
            string.Empty,
            propertyChanged: OnRightIconPathChanged);
    public static readonly BindableProperty ShowRightPrimaryIconSlotProperty =
        BindableProperty.Create(
            nameof(ShowRightPrimaryIconSlot),
            typeof(bool),
            typeof(FactionListItemView),
            false);
    public static readonly BindableProperty ShowRightSecondaryIconSlotProperty =
        BindableProperty.Create(
            nameof(ShowRightSecondaryIconSlot),
            typeof(bool),
            typeof(FactionListItemView),
            false);
    public static readonly BindableProperty UseVerticalTileLayoutProperty =
        BindableProperty.Create(
            nameof(UseVerticalTileLayout),
            typeof(bool),
            typeof(FactionListItemView),
            false,
            propertyChanged: OnLayoutModeChanged);
    public static readonly BindableProperty IsHorizontalLayoutProperty =
        BindableProperty.Create(
            nameof(IsHorizontalLayout),
            typeof(bool),
            typeof(FactionListItemView),
            true);

    public FactionListItemView()
    {
        InitializeComponent();
        UpdateTitleFormattingState(TitleFormatted);
        RefreshLayoutModeVisibility();
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

    public string TrailingText
    {
        get => (string)GetValue(TrailingTextProperty);
        set => SetValue(TrailingTextProperty, value);
    }

    public bool HasTrailingText
    {
        get => (bool)GetValue(HasTrailingTextProperty);
        private set => SetValue(HasTrailingTextProperty, value);
    }

    public string RightPrimaryIconPackagedPath
    {
        get => (string)GetValue(RightPrimaryIconPackagedPathProperty);
        set => SetValue(RightPrimaryIconPackagedPathProperty, value);
    }

    public string RightSecondaryIconPackagedPath
    {
        get => (string)GetValue(RightSecondaryIconPackagedPathProperty);
        set => SetValue(RightSecondaryIconPackagedPathProperty, value);
    }

    public bool ShowRightPrimaryIconSlot
    {
        get => (bool)GetValue(ShowRightPrimaryIconSlotProperty);
        set => SetValue(ShowRightPrimaryIconSlotProperty, value);
    }

    public bool ShowRightSecondaryIconSlot
    {
        get => (bool)GetValue(ShowRightSecondaryIconSlotProperty);
        set => SetValue(ShowRightSecondaryIconSlotProperty, value);
    }

    public bool UseVerticalTileLayout
    {
        get => (bool)GetValue(UseVerticalTileLayoutProperty);
        set => SetValue(UseVerticalTileLayoutProperty, value);
    }

    public bool IsHorizontalLayout
    {
        get => (bool)GetValue(IsHorizontalLayoutProperty);
        private set => SetValue(IsHorizontalLayoutProperty, value);
    }

    private static void OnLayoutModeChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is not FactionListItemView view)
        {
            return;
        }

        var vertical = newValue is bool enabled && enabled;
        view.IsHorizontalLayout = !vertical;
        view.RefreshLayoutModeVisibility();
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

    private static void OnTrailingTextChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is FactionListItemView view)
        {
            view.HasTrailingText = !string.IsNullOrWhiteSpace(newValue as string);
        }
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

    private void RefreshLayoutModeVisibility()
    {
        HorizontalLayoutRoot.IsVisible = IsHorizontalLayout;
        VerticalLayoutRoot.IsVisible = !IsHorizontalLayout;
    }

    private void RefreshTitlePresentation()
    {
        HorizontalFormattedNameLabel.IsVisible = HasTitleFormatted;
        HorizontalNameLabel.IsVisible = !HasTitleFormatted;
        VerticalFormattedNameLabel.IsVisible = HasTitleFormatted;
        VerticalNameLabel.IsVisible = !HasTitleFormatted;
    }

    private static void OnRightIconPathChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is FactionListItemView view)
        {
            _ = view.LoadRightIconsAsync();
        }
    }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();
        _ = LoadSvgFromCacheAsync();
        _ = LoadRightIconsAsync();
    }

    private async Task LoadSvgFromCacheAsync()
    {
        var loadVersion = ++_logoLoadVersion;
        _svgPicture?.Dispose();
        _svgPicture = null;

        var item = BindingContext as IViewerListItem;
        if (item is null)
        {
            LogoCanvas.InvalidateSurface();
            LogoCanvasVertical.InvalidateSurface();
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
                LogoCanvasVertical.InvalidateSurface();
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
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"FactionListItemView SVG load failed for '{item.CachedLogoPath ?? item.PackagedLogoPath}': {ex.Message}");
            _svgPicture = null;
        }

        LogoCanvas.InvalidateSurface();
        LogoCanvasVertical.InvalidateSurface();
    }

    private async Task LoadRightIconsAsync()
    {
        _rightPrimaryPicture?.Dispose();
        _rightPrimaryPicture = null;
        _rightSecondaryPicture?.Dispose();
        _rightSecondaryPicture = null;

        _rightPrimaryPicture = await TryLoadPackagedSvgAsync(RightPrimaryIconPackagedPath);
        _rightSecondaryPicture = await TryLoadPackagedSvgAsync(RightSecondaryIconPackagedPath);

        RightIconPrimaryCanvas.InvalidateSurface();
        RightIconSecondaryCanvas.InvalidateSurface();
    }

    private static async Task<SKPicture?> TryLoadPackagedSvgAsync(string? packagedPath)
    {
        if (string.IsNullOrWhiteSpace(packagedPath))
        {
            return null;
        }

        try
        {
            await using var stream = await FileSystem.Current.OpenAppPackageFileAsync(packagedPath);
            var svg = new SKSvg();
            return svg.Load(stream);
        }
        catch
        {
            return null;
        }
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

    private void OnLogoCanvasVerticalPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        OnLogoCanvasPaintSurface(sender, e);
    }

    private void OnRightIconPrimaryCanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        DrawCanvasPicture(_rightPrimaryPicture, e);
    }

    private void OnRightIconSecondaryCanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        DrawCanvasPicture(_rightSecondaryPicture, e);
    }

    private static void DrawCanvasPicture(SKPicture? picture, SKPaintSurfaceEventArgs e)
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
