using InfinityMercsApp.ViewModels;

namespace InfinityMercsApp.Views;

public partial class SettingsPage : ContentPage
{
	public SettingsPage(MainViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}
