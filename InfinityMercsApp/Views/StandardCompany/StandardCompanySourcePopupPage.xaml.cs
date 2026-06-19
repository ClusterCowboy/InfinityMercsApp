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

    private void ApplyLayout() =>
        AdaptiveSourcePopupLayout.Apply(this, ModalCard, CardsGrid, SourceCardOne, SourceCardTwo);

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//CreateNewCompanyPage");
    }
}
