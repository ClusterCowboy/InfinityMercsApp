using InfinityMercsApp.ViewModels;

namespace InfinityMercsApp.Views;

public partial class CreateNewCompanyPage
{
    public CreateNewCompanyPage(CreateNewCompanyPageViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
