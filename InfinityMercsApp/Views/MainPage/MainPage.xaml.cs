using InfinityMercsApp.ViewModels;

namespace InfinityMercsApp.Views;

public partial class ModeSelectionPage
{
	public ModeSelectionPage(ModeSelectionViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}
