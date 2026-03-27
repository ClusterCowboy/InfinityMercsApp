using InfinityMercsApp.ViewModels;

namespace InfinityMercsApp.Views;

public partial class LoneWolfCompanySourcePopupPage
{
    public LoneWolfCompanySourcePopupPage(LoneWolfCompanySourcePopupPageViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//CreateNewCompanyPage");
    }
}
