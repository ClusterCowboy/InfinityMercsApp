using InfinityMercsApp.ViewModels;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using Svg.Skia;
using System.Windows.Input;

namespace InfinityMercsApp.Views.Controls;

public partial class UnitSelectionListItemView : ContentView
{
    private SKPicture? _svgPicture;
    private int _logoLoadVersion;

    public static readonly BindableProperty ItemTappedCommandProperty =
        BindableProperty.Create(
            nameof(ItemTappedCommand),
            typeof(ICommand),
            typeof(UnitSelectionListItemView));

    public static readonly BindableProperty ItemTappedCommandParameterProperty =
        BindableProperty.Create(
            nameof(ItemTappedCommandParameter),
            typeof(object),
            typeof(UnitSelectionListItemView));

    public UnitSelectionListItemView()
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
        var loadVersion = ++_logoLoadVersion;
        _svgPicture?.Dispose();
        _svgPicture = null;

        if (BindingContext is not IViewerListItem item)
        {
            LogoCanvas.InvalidateSurface();
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
            Console.Error.WriteLine($"UnitSelectionListItemView SVG load failed: {ex.Message}");
            _svgPicture = null;
        }

        LogoCanvas.InvalidateSurface();
    }

    private static async Task<Stream?> OpenBestLogoStreamAsync(IViewerListItem item)
    {
        if (!string.IsNullOrWhiteSpace(item.CachedLogoPath) && File.Exists(item.CachedLogoPath))
        {
            return File.OpenRead(item.CachedLogoPath);
        }

        if (!string.IsNullOrWhiteSpace(item.PackagedLogoPath))
        {
            try
            {
                return await FileSystem.Current.OpenAppPackageFileAsync(item.PackagedLogoPath);
            }
            catch
            {
                return null;
            }
        }

        return null;
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
        var parameter = ItemTappedCommandParameter ?? BindingContext;
        if (ItemTappedCommand?.CanExecute(parameter) == true)
        {
            ItemTappedCommand.Execute(parameter);
        }
    }
}
