using InfinityMercsApp.ViewModels;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using Svg.Skia;

namespace InfinityMercsApp.Views;

public partial class CreateNewCompanyPage
{
    private SKPicture? _standardCompanyPicture;
    private SKPicture? _cohesiveCompanyPicture;
    private SKPicture? _inspiringLeaderPicture;
    private SKPicture? _airborneCompanyPicture;
    private SKPicture? _tagCompanyPicture;
    private SKPicture? _loneWolfCompanyPicture;

    public CreateNewCompanyPage(CreateNewCompanyPageViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _ = LoadCompanyIconsAsync();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        ClearResidualNavigationStack();
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//ModeSelectionPage");
    }

    private void ClearResidualNavigationStack()
    {
        var navigation = Navigation;
        var stack = navigation?.NavigationStack;
        if (stack is null || stack.Count <= 1)
        {
            return;
        }

        // Keep only this page so "Create New Company" always starts from a clean state.
        var pagesToRemove = stack.Where(page => !ReferenceEquals(page, this)).ToList();
        foreach (var page in pagesToRemove)
        {
            navigation?.RemovePage(page);
        }
    }

    private async Task LoadCompanyIconsAsync()
    {
        _standardCompanyPicture = await LoadSvgPictureAsync("MercsIcons/standard_company.svg");
        _cohesiveCompanyPicture = await LoadSvgPictureAsync("MercsIcons/cohesive_company.svg");
        _inspiringLeaderPicture = await LoadSvgPictureAsync("MercsIcons/inspiring_leadership.svg");
        _airborneCompanyPicture = await LoadSvgPictureAsync("MercsIcons/airborne_company.svg");
        _tagCompanyPicture = await LoadSvgPictureAsync("MercsIcons/tag_company.svg");
        _loneWolfCompanyPicture = await LoadSvgPictureAsync("MercsIcons/proxy_pack.svg");

        MainThread.BeginInvokeOnMainThread(() =>
        {
            StandardCompanyIcon.InvalidateSurface();
            CohesiveCompanyIcon.InvalidateSurface();
            InspiringLeaderIcon.InvalidateSurface();
            AirborneCompanyIcon.InvalidateSurface();
            TagCompanyIcon.InvalidateSurface();
            LoneWolfCompanyIcon.InvalidateSurface();
        });
    }

    private static async Task<SKPicture?> LoadSvgPictureAsync(string packagedPath)
    {
        try
        {
            await using var stream = await FileSystem.Current.OpenAppPackageFileAsync(packagedPath);
            var svg = new SKSvg();
            return svg.Load(stream);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"CreateNewCompanyPage icon load failed for '{packagedPath}': {ex.Message}");
            return null;
        }
    }

    private void OnStandardCompanyIconPaintSurface(object? sender, SKPaintSurfaceEventArgs e) =>
        DrawPicture(e, _standardCompanyPicture);

    private void OnCohesiveCompanyIconPaintSurface(object? sender, SKPaintSurfaceEventArgs e) =>
        DrawPicture(e, _cohesiveCompanyPicture);

    private void OnInspiringLeaderIconPaintSurface(object? sender, SKPaintSurfaceEventArgs e) =>
        DrawPicture(e, _inspiringLeaderPicture);

    private void OnAirborneCompanyIconPaintSurface(object? sender, SKPaintSurfaceEventArgs e) =>
        DrawPicture(e, _airborneCompanyPicture);

    private void OnTagCompanyIconPaintSurface(object? sender, SKPaintSurfaceEventArgs e) =>
        DrawPicture(e, _tagCompanyPicture);

    private void OnLoneWolfCompanyIconPaintSurface(object? sender, SKPaintSurfaceEventArgs e) =>
        DrawPicture(e, _loneWolfCompanyPicture);

    private static void DrawPicture(SKPaintSurfaceEventArgs e, SKPicture? picture)
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

        var info = e.Info;
        var scale = Math.Min(info.Width / bounds.Width, info.Height / bounds.Height);
        var translateX = (info.Width - (bounds.Width * scale)) / 2f - (bounds.Left * scale);
        var translateY = (info.Height - (bounds.Height * scale)) / 2f - (bounds.Top * scale);

        canvas.Save();
        canvas.Translate(translateX, translateY);
        canvas.Scale(scale);
        canvas.DrawPicture(picture);
        canvas.Restore();
    }
}
