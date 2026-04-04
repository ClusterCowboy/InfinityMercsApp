using System.Windows.Input;
using InfinityMercsApp.ViewModels;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using Svg.Skia;

namespace InfinityMercsApp.Views.Controls;

public partial class ViewerListRowView : ContentView
{
    private SKPicture? _logoPicture;
    private int _loadVersion;

    public static readonly BindableProperty ItemTappedCommandProperty =
        BindableProperty.Create(
            nameof(ItemTappedCommand),
            typeof(ICommand),
            typeof(ViewerListRowView));

    public static readonly BindableProperty ItemTappedCommandParameterProperty =
        BindableProperty.Create(
            nameof(ItemTappedCommandParameter),
            typeof(object),
            typeof(ViewerListRowView));

    public ViewerListRowView()
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
        _ = LoadLogoAsync();
    }

    private async Task LoadLogoAsync()
    {
        var currentVersion = ++_loadVersion;
        _logoPicture?.Dispose();
        _logoPicture = null;

        if (BindingContext is not IViewerListItem item)
        {
            LogoCanvas.InvalidateSurface();
            return;
        }

        try
        {
            Stream? stream = null;
            if (!string.IsNullOrWhiteSpace(item.CachedLogoPath) && File.Exists(item.CachedLogoPath))
            {
                stream = File.OpenRead(item.CachedLogoPath);
            }
            else if (!string.IsNullOrWhiteSpace(item.PackagedLogoPath))
            {
                foreach (var candidate in BuildPackagedCandidates(item.PackagedLogoPath))
                {
                    try
                    {
                        stream = await FileSystem.Current.OpenAppPackageFileAsync(candidate);
                        break;
                    }
                    catch
                    {
                        // Try next.
                    }
                }
            }

            if (stream is not null)
            {
                await using (stream)
                {
                    var svg = new SKSvg();
                    var loaded = svg.Load(stream);
                    if (currentVersion != _loadVersion)
                    {
                        loaded?.Dispose();
                        return;
                    }

                    _logoPicture = loaded;
                }
            }
        }
        catch
        {
            _logoPicture = null;
        }

        LogoCanvas.InvalidateSurface();
    }

    private static IEnumerable<string> BuildPackagedCandidates(string packagedPath)
    {
        var normalized = packagedPath.Replace('\\', '/').TrimStart('/');
        yield return normalized;
        yield return normalized.ToLowerInvariant();
    }

    private void OnLogoCanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        if (_logoPicture is null)
        {
            return;
        }

        var bounds = _logoPicture.CullRect;
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        var scale = Math.Min(e.Info.Width / bounds.Width, e.Info.Height / bounds.Height);
        var x = (e.Info.Width - (bounds.Width * scale)) / 2f;
        var y = (e.Info.Height - (bounds.Height * scale)) / 2f;

        canvas.Translate(x, y);
        canvas.Scale(scale);
        canvas.DrawPicture(_logoPicture);
    }

    private void OnTapped(object? sender, TappedEventArgs e)
    {
        var parameter = ItemTappedCommandParameter ?? BindingContext;
        if (ItemTappedCommand?.CanExecute(parameter) == true)
        {
            ItemTappedCommand.Execute(parameter);
        }
    }
}
