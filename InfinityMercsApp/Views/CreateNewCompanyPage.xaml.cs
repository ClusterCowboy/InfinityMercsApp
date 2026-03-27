using InfinityMercsApp.ViewModels;

namespace InfinityMercsApp.Views;

public partial class CreateNewCompanyPage
{
    public CreateNewCompanyPage(CreateNewCompanyPageViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
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
}
