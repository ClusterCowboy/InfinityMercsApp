using InfinityMercsApp.ViewModels;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using Svg.Skia;
using System.Windows.Input;
using InfinityMercsApp.Views;

namespace InfinityMercsApp.Views.Controls;

public partial class FactionListItemView : ContentView
{
    private SKPicture? _svgPicture;
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

    public FactionListItemView()
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
            Stream? stream = await OpenBestLogoStreamAsync(item);

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
