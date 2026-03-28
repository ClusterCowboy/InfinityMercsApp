using InfinityMercsApp.ViewModels;

namespace InfinityMercsApp.Views;

public partial class TagCompanySourcePopupPage
{
    public TagCompanySourcePopupPage(TagCompanySourcePopupPageViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//CreateNewCompanyPage");
    }
}
