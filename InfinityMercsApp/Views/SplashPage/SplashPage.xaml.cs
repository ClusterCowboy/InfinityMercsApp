using InfinityMercsApp.ViewModels;

namespace InfinityMercsApp.Views;

public partial class SplashPage
{
	public SplashPage(SplashPageViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}
