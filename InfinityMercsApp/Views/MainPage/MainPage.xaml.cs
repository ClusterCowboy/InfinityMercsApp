using InfinityMercsApp.ViewModels;
using InfinityMercsApp.ViewModels.Base;
using InfinityMercsApp.Views.Adaptive;

namespace InfinityMercsApp.Views;

public partial class ModeSelectionPage : AdaptiveContentPage
{
	public ModeSelectionPage(ModeSelectionViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
		ApplyLayout();
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();

		// Preserve the ContentPageBase initialization contract now that this page derives from
		// AdaptiveContentPage directly.
		if (BindingContext is IViewModelBase ivmb)
		{
			await ivmb.InitializeAsyncCommand.ExecuteAsync(null);
		}
	}

	protected override void OnLayoutModeChanged(AdaptiveLayoutMode mode) => ApplyLayout();

	private void ApplyLayout()
	{
		if (IsCompact)
		{
			// Stack the actions full-width on phones.
			ButtonBar.ColumnDefinitions = [new ColumnDefinition(GridLength.Star)];
			ButtonBar.RowDefinitions =
			[
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto)
			];

			Grid.SetColumn(NewCompanyButton, 0);
			Grid.SetRow(NewCompanyButton, 0);
			Grid.SetColumn(LoadCompanyButton, 0);
			Grid.SetRow(LoadCompanyButton, 1);

			NewCompanyButton.WidthRequest = -1;
			LoadCompanyButton.WidthRequest = -1;
			NewCompanyButton.HorizontalOptions = LayoutOptions.Fill;
			LoadCompanyButton.HorizontalOptions = LayoutOptions.Fill;

			ButtonBar.HorizontalOptions = LayoutOptions.Fill;
			RootGrid.MaximumWidthRequest = double.PositiveInfinity;
			RootGrid.HorizontalOptions = LayoutOptions.Fill;
		}
		else
		{
			// Side-by-side fixed-width actions; center the whole page inside a max content width.
			ButtonBar.RowDefinitions = [new RowDefinition(GridLength.Auto)];
			ButtonBar.ColumnDefinitions =
			[
				new ColumnDefinition(GridLength.Auto),
				new ColumnDefinition(GridLength.Auto)
			];

			Grid.SetRow(NewCompanyButton, 0);
			Grid.SetColumn(NewCompanyButton, 0);
			Grid.SetRow(LoadCompanyButton, 0);
			Grid.SetColumn(LoadCompanyButton, 1);

			NewCompanyButton.WidthRequest = 170;
			LoadCompanyButton.WidthRequest = 170;
			NewCompanyButton.HorizontalOptions = LayoutOptions.Center;
			LoadCompanyButton.HorizontalOptions = LayoutOptions.Center;

			ButtonBar.HorizontalOptions = LayoutOptions.Center;
			RootGrid.MaximumWidthRequest = IsMedium ? 600d : 760d;
			RootGrid.HorizontalOptions = LayoutOptions.Center;
		}
	}
}
