using InfinityMercsApp.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace InfinityMercsApp.Views;

public partial class ViewerPage : ContentPage
{
	private readonly ViewerViewModel _viewModel;
	private bool _loaded;

	public ViewerPage()
	{
		InitializeComponent();
		_viewModel = Application.Current?.Handler?.MauiContext?.Services.GetService<ViewerViewModel>()
			?? new ViewerViewModel();
		BindingContext = _viewModel;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		if (_loaded)
		{
			return;
		}

		_loaded = true;
		await _viewModel.LoadFactionsAsync();
	}
}
