using InfinityMercsApp.ViewModels;
using InfinityMercsApp.ViewModels.Base;
using InfinityMercsApp.Views.Adaptive;

namespace InfinityMercsApp.Views;

public partial class StandardCompanySourcePopupPage : AdaptiveContentPage
{
    public StandardCompanySourcePopupPage(StandardCompanySourcePopupPageViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        ApplyLayout();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is IViewModelBase ivmb)
        {
            await ivmb.InitializeAsyncCommand.ExecuteAsync(null);
        }
    }

    protected override void OnLayoutModeChanged(AdaptiveLayoutMode mode) => ApplyLayout();

    private void ApplyLayout()
    {
        AdaptiveSourcePopupLayout.Apply(this, ModalCard, CardsGrid, SourceCardOne, SourceCardTwo);

        // On the smallest screens the cards stack; shrink the artwork and min-heights so both choices
        // and the BACK button fit (the cards live in a ScrollView as a final safety net).
        var iconHeight = IsCompact ? 96d : 140d;
        var cardMinHeight = IsCompact ? 150d : 240d;

        OneVanillaFactionIcon.HeightRequest = iconHeight;
        TwoSectorialsIcon.HeightRequest = iconHeight;
        SourceCardOne.MinimumHeightRequest = cardMinHeight;
        SourceCardTwo.MinimumHeightRequest = cardMinHeight;
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//CreateNewCompanyPage");
    }
}
