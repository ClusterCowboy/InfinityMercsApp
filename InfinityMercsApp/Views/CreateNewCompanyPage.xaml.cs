using InfinityMercsApp.ViewModels;

namespace InfinityMercsApp.Views;

public partial class CreateNewCompanyPage
{
    public CreateNewCompanyPage(CreateNewCompanyPageViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//ModeSelectionPage");
    }
}
