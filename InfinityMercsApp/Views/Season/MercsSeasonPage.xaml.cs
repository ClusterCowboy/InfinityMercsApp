namespace InfinityMercsApp.Views.Season;

public partial class MercsSeasonPage : ContentPage
{
    public MercsSeasonPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        // Always return to the main two-button state when the page reappears.
        ShowMainActions();
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
