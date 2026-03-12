using InfinityMercsApp.ViewModels;

namespace InfinityMercsApp.Views;

public partial class StandardCompanySourcePopupPage
{
    public StandardCompanySourcePopupPage(StandardCompanySourcePopupPageViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }
}
