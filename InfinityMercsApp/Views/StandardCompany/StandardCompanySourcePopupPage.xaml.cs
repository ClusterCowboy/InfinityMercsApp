using InfinityMercsApp.Domain.CompanyCreation;
using InfinityMercsApp.Infrastructure.Services;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using Svg.Skia;

namespace InfinityMercsApp.Views.StandardCompany;

public partial class StandardCompanySourcePopupPage : ContentPage
{
    private SKPicture? _oneVanillaFactionIconPicture;
    private SKPicture? _twoSectorialsIconPicture;
    private readonly IArmySourceSelectionModeService _armySourceSelectionModeService;

    public StandardCompanySourcePopupPage(IArmySourceSelectionModeService armySourceSelectionModeService)
    {
        InitializeComponent();
        _ = LoadIconsAsync();

        _armySourceSelectionModeService = armySourceSelectionModeService;
    }

    private async Task LoadIconsAsync()
    {
        _oneVanillaFactionIconPicture?.Dispose();
        _oneVanillaFactionIconPicture = null;
        _twoSectorialsIconPicture?.Dispose();
        _twoSectorialsIconPicture = null;

        try
        {
            await using var stream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/MercsIcons/noun-male-player-2851844.svg");
            var svg = new SKSvg();
            _oneVanillaFactionIconPicture = svg.Load(stream);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[StandardCompanySourcePopupPage] Failed to load One Vanilla Faction icon: {ex.Message}");
        }

        try
        {
            await using var stream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/MercsIcons/noun-duo-2851835.svg");
            var svg = new SKSvg();
            _twoSectorialsIconPicture = svg.Load(stream);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[StandardCompanySourcePopupPage] Failed to load Two Sectorials icon: {ex.Message}");
        }

        OneVanillaFactionIconCanvas.InvalidateSurface();
        TwoSectorialsIconCanvas.InvalidateSurface();
    }

    private void OnOneVanillaFactionIconCanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        DrawIcon(_oneVanillaFactionIconPicture, e);
    }

    private void OnTwoSectorialsIconCanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        DrawIcon(_twoSectorialsIconPicture, e);
    }

    private static void DrawIcon(SKPicture? picture, SKPaintSurfaceEventArgs e)
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

        var width = e.Info.Width;
        var height = e.Info.Height;
        if (width <= 0 || height <= 0)
        {
            return;
        }

        var scale = Math.Min(width / bounds.Width, height / bounds.Height);
        var x = (width - (bounds.Width * scale)) / 2f;
        var y = (height - (bounds.Height * scale)) / 2f;

        canvas.Translate(x, y);
        canvas.Scale(scale);
        canvas.DrawPicture(picture);
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }

    private async void OnOneVanillaFactionClicked(object? sender, EventArgs e)
    {
        Console.WriteLine("[StandardCompanySourcePopupPage] Selected source: One Vanilla Faction");
        await NavigateToFactionSelectionAsync(ArmySourceSelectionMode.VanillaFactions);
    }

    private async void OnTwoSectorialsClicked(object? sender, EventArgs e)
    {
        Console.WriteLine("[StandardCompanySourcePopupPage] Selected source: Two Sectorials");
        await NavigateToFactionSelectionAsync(ArmySourceSelectionMode.Sectorials);
    }

    private async Task NavigateToFactionSelectionAsync(ArmySourceSelectionMode mode)
    {
        try
        {
            _armySourceSelectionModeService.Set(mode);

            await Navigation.PopModalAsync(false);
            var navigation = Shell.Current?.Navigation;
            if (navigation is null)
            {
                Console.Error.WriteLine("[StandardCompanySourcePopupPage] Navigation unavailable for faction selection page.");
                return;
            }

            // TODO: Change this navigation. This is a good reason not to mix stack based navigation
            // With shell based. We should present a popup on the CreateNewArmyPage, capture the result
            // And navigate with shell navigation after it returns
            var services = Application.Current?.Handler?.MauiContext?.Services;
            var page = services?.GetService<StandardCompanySelectionPage>();
            await navigation.PushAsync(page);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[StandardCompanySourcePopupPage] Failed to navigate to faction selection page: {ex.Message}");
        }
    }
}
