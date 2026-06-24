using InfinityMercsApp.ViewModels;
using InfinityMercsApp.ViewModels.Base;
using InfinityMercsApp.Views.Adaptive;
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
        ApplyLayout();
        _ = LoadCompanyIconsAsync();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Preserve the ContentPageBase initialization contract now that this page derives from
        // AdaptiveContentPage directly.
        if (BindingContext is IViewModelBase ivmb)
        {
            await ivmb.InitializeAsyncCommand.ExecuteAsync(null);
        }

        ClearResidualNavigationStack();
    }

    protected override void OnLayoutModeChanged(AdaptiveLayoutMode mode) => ApplyLayout();

    // Icon canvas size and card height for the two card layouts. Phones (Compact) lay the icon and
    // title out side-by-side in a short row; wider screens stack the large icon above the title.
    private const double CompactIconSize = 56d;
    private const double ExpandedIconSize = 120d;
    private const double CompactCardHeight = 72d;
    private const double ExpandedCardHeight = 160d;

    private void ApplyLayout()
    {
        // 1 column on phones, 2 on tablet portrait, 3 on larger screens.
        var cols = IsCompact ? 1 : IsMedium ? 2 : 3;

        // Visible company choices, in display order. Lone Wolf stays hidden and is excluded.
        var cards = new[] { CardStandard, CardCohesive, CardInspiring, CardAirborne, CardTag };
        var rows = (int)Math.Ceiling(cards.Length / (double)cols);

        CardGrid.ColumnDefinitions = [.. Enumerable.Range(0, cols).Select(_ => new ColumnDefinition(GridLength.Star))];
        CardGrid.RowDefinitions = [.. Enumerable.Range(0, rows).Select(_ => new RowDefinition(GridLength.Auto))];

        for (var i = 0; i < cards.Length; i++)
        {
            Grid.SetRow(cards[i], i / cols);
            Grid.SetColumn(cards[i], i % cols);
            cards[i].MinimumHeightRequest = IsCompact ? CompactCardHeight : ExpandedCardHeight;
        }

        ConfigureCardContent(StandardCardContent, StandardCompanyIcon, StandardCompanyLabel);
        ConfigureCardContent(CohesiveCardContent, CohesiveCompanyIcon, CohesiveCompanyLabel);
        ConfigureCardContent(InspiringCardContent, InspiringLeaderIcon, InspiringLeaderLabel);
        ConfigureCardContent(AirborneCardContent, AirborneCompanyIcon, AirborneCompanyLabel);
        ConfigureCardContent(TagCardContent, TagCompanyIcon, TagCompanyLabel);

        CardGrid.HorizontalOptions = IsCompact ? LayoutOptions.Fill : LayoutOptions.Center;
        CardGrid.MaximumWidthRequest = IsCompact ? double.PositiveInfinity : cols == 2 ? 760d : 1100d;
    }

    /// <summary>
    /// Reflows a single company card's interior. On phones (Compact) the icon sits on the left with
    /// the title beside it on the right in a short row; on wider screens the large icon stacks above
    /// the centred title.
    /// </summary>
    private void ConfigureCardContent(Grid content, SKCanvasView icon, Label label)
    {
        if (IsCompact)
        {
            content.RowDefinitions = [new RowDefinition(GridLength.Auto)];
            content.ColumnDefinitions =
            [
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star)
            ];
            content.RowSpacing = 0;
            content.ColumnSpacing = 12;

            Grid.SetRow(icon, 0);
            Grid.SetColumn(icon, 0);
            icon.WidthRequest = CompactIconSize;
            icon.HeightRequest = CompactIconSize;
            icon.VerticalOptions = LayoutOptions.Center;
            icon.HorizontalOptions = LayoutOptions.Center;

            Grid.SetRow(label, 0);
            Grid.SetColumn(label, 1);
            label.VerticalOptions = LayoutOptions.Center;
            label.HorizontalOptions = LayoutOptions.Start;
            label.HorizontalTextAlignment = TextAlignment.Start;
            label.SetDynamicResource(Label.FontSizeProperty, "FontSizeCardTitleCompact");
        }
        else
        {
            content.ColumnDefinitions = [new ColumnDefinition(GridLength.Star)];
            content.RowDefinitions =
            [
                new RowDefinition(GridLength.Star),
                new RowDefinition(GridLength.Auto)
            ];
            content.ColumnSpacing = 0;
            content.RowSpacing = 6;

            Grid.SetColumn(icon, 0);
            Grid.SetRow(icon, 0);
            icon.WidthRequest = ExpandedIconSize;
            icon.HeightRequest = ExpandedIconSize;
            icon.VerticalOptions = LayoutOptions.Center;
            icon.HorizontalOptions = LayoutOptions.Center;

            Grid.SetColumn(label, 0);
            Grid.SetRow(label, 1);
            label.VerticalOptions = LayoutOptions.Start;
            label.HorizontalOptions = LayoutOptions.Center;
            label.HorizontalTextAlignment = TextAlignment.Center;
            label.SetDynamicResource(Label.FontSizeProperty, "FontSizeHeadline");
        }
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//MercsSeasonPage");
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
