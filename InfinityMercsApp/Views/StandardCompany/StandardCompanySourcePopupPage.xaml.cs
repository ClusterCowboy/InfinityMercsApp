using InfinityMercsApp.ViewModels;

namespace InfinityMercsApp.Views.StandardCompany;

public partial class StandardCompanySourcePopupPage
{
    public StandardCompanySourcePopupPage(StandardCompanySourcePopupPageViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
