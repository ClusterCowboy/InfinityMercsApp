using InfinityMercsApp.Views.Adaptive;

namespace InfinityMercsApp.Views.Season;

public partial class MercsSeasonPage : AdaptiveContentPage
{
    public MercsSeasonPage()
    {
        InitializeComponent();
        ApplyLayout();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        // Always return to the main two-button state when the page reappears.
        ShowMainActions();

#if WINDOWS
        // The window/Shell are fully live by the time the home page appears, so this is a
        // reliable point to wire up the Ctrl+Shift+Q/W/E/R resize shortcuts (idempotent).
        InfinityMercsApp.WinUI.App.EnsureWindowResizeShortcuts();
#endif
    }

    protected override void OnLayoutModeChanged(AdaptiveLayoutMode mode) => ApplyLayout();

    private void ApplyLayout()
    {
        if (IsExpandedOrWider)
        {
            // Split horizontally: brand on the left, action panel on the right.
            RootGrid.RowDefinitions = [new RowDefinition(GridLength.Star)];
            RootGrid.ColumnDefinitions =
            [
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star)
            ];

            Grid.SetRow(BrandPanel, 0);
            Grid.SetColumn(BrandPanel, 0);
            Grid.SetRow(ActionArea, 0);
            Grid.SetColumn(ActionArea, 1);

            ActionArea.Padding = new Thickness(24, 0);
            ActionArea.VerticalOptions = LayoutOptions.Center;
            ActionArea.HorizontalOptions = LayoutOptions.Center;
            ActionArea.MaximumWidthRequest = 420d;
        }
        else
        {
            // Stacked: brand above, actions anchored near the bottom.
            RootGrid.ColumnDefinitions = [new ColumnDefinition(GridLength.Star)];
            RootGrid.RowDefinitions =
            [
                new RowDefinition(GridLength.Star),
                new RowDefinition(GridLength.Auto)
            ];

            Grid.SetRow(BrandPanel, 0);
            Grid.SetColumn(BrandPanel, 0);
            Grid.SetRow(ActionArea, 1);
            Grid.SetColumn(ActionArea, 0);

            ActionArea.Padding = new Thickness(24, 0, 24, 40);
            ActionArea.VerticalOptions = LayoutOptions.End;

            // Medium keeps the stacked layout but constrains the action column to a readable width.
            if (IsMedium)
            {
                ActionArea.HorizontalOptions = LayoutOptions.Center;
                ActionArea.MaximumWidthRequest = 520d;
            }
            else
            {
                ActionArea.HorizontalOptions = LayoutOptions.Fill;
                ActionArea.MaximumWidthRequest = double.PositiveInfinity;
            }
        }
    }

    private void OnLoadSeasonClicked(object sender, EventArgs e)
    {
        _ = Shell.Current.GoToAsync(nameof(LoadSeasonPage));
    }

    private void OnStartNewSeasonClicked(object sender, EventArgs e)
    {
        MainActionsPanel.IsVisible = false;
        NewSeasonChoicePanel.IsVisible = true;
    }

    private async void OnLoadCompanyClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync($"//{nameof(Views.LoadCompanyPage)}?seasonMode=true");
    }

    private async void OnCreateNewTeamClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync($"//{nameof(Views.CreateNewCompanyPage)}");
    }

    private void OnCancelNewSeasonClicked(object sender, EventArgs e)
    {
        ShowMainActions();
    }

    private void ShowMainActions()
    {
        NewSeasonChoicePanel.IsVisible = false;
        MainActionsPanel.IsVisible = true;
    }
}
