using InfinityMercsApp.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace InfinityMercsApp.Views;

public partial class ViewerPage : ContentPage
{
	private readonly ViewerViewModel _viewModel;
	private bool _loaded;
	private double _factionDragStartScrollY;
	private double _unitDragStartScrollY;

	public ViewerPage()
	{
		InitializeComponent();
		var services = Application.Current?.Handler?.MauiContext?.Services;
		_viewModel = services?.GetService<ViewerViewModel>()
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

	private async void OnFactionListPanUpdated(object? sender, PanUpdatedEventArgs e)
	{
		switch (e.StatusType)
		{
			case GestureStatus.Started:
				_factionDragStartScrollY = FactionScrollView.ScrollY;
				break;
			case GestureStatus.Running:
				var targetY = Math.Max(0, _factionDragStartScrollY - e.TotalY);
				await FactionScrollView.ScrollToAsync(0, targetY, false);
				break;
		}
	}

	private async void OnUnitListPanUpdated(object? sender, PanUpdatedEventArgs e)
	{
		switch (e.StatusType)
		{
			case GestureStatus.Started:
				_unitDragStartScrollY = UnitScrollView.ScrollY;
				break;
			case GestureStatus.Running:
				var targetY = Math.Max(0, _unitDragStartScrollY - e.TotalY);
				await UnitScrollView.ScrollToAsync(0, targetY, false);
				break;
		}
	}
}
