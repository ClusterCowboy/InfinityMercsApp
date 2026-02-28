using InfinityMercsApp.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using Svg.Skia;
using System.ComponentModel;

namespace InfinityMercsApp.Views;

public partial class ViewerPage : ContentPage
{
	private readonly ViewerViewModel _viewModel;
	private bool _loaded;
	private double _factionDragStartScrollY;
	private double _unitDragStartScrollY;
	private SKPicture? _regularOrderIconPicture;
	private SKPicture? _irregularOrderIconPicture;
	private SKPicture? _impetuousIconPicture;
	private SKPicture? _tacticalAwarenessIconPicture;

	public ViewerPage()
	{
		InitializeComponent();
		var services = Application.Current?.Handler?.MauiContext?.Services;
		_viewModel = services?.GetService<ViewerViewModel>()
			?? new ViewerViewModel();
		BindingContext = _viewModel;
		_viewModel.PropertyChanged += OnViewModelPropertyChanged;
		_ = LoadHeaderIconsAsync();
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

	private async Task LoadHeaderIconsAsync()
	{
		_regularOrderIconPicture?.Dispose();
		_regularOrderIconPicture = null;
		_irregularOrderIconPicture?.Dispose();
		_irregularOrderIconPicture = null;
		_impetuousIconPicture?.Dispose();
		_impetuousIconPicture = null;
		_tacticalAwarenessIconPicture?.Dispose();
		_tacticalAwarenessIconPicture = null;

		try
		{
			await using var regularStream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/NonCBIcons/noun-circle-arrow-803872.svg");
			var regularSvg = new SKSvg();
			_regularOrderIconPicture = regularSvg.Load(regularStream);
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine($"ViewerPage regular order icon load failed: {ex.Message}");
		}

		try
		{
			await using var irregularStream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/NonCBIcons/noun-arrow-963008.svg");
			var irregularSvg = new SKSvg();
			_irregularOrderIconPicture = irregularSvg.Load(irregularStream);
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine($"ViewerPage irregular order icon load failed: {ex.Message}");
		}

		try
		{
			await using var impetuousStream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/NonCBIcons/noun-fire-131591.svg");
			var impetuousSvg = new SKSvg();
			_impetuousIconPicture = impetuousSvg.Load(impetuousStream);
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine($"ViewerPage impetuous icon load failed: {ex.Message}");
		}

		try
		{
			await using var tacticalStream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/NonCBIcons/noun-double-arrows-7302616.svg");
			var tacticalSvg = new SKSvg();
			_tacticalAwarenessIconPicture = tacticalSvg.Load(tacticalStream);
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine($"ViewerPage tactical awareness icon load failed: {ex.Message}");
		}

		OrderTypeIconCanvas.InvalidateSurface();
		ImpetuousIconCanvas.InvalidateSurface();
		TacticalAwarenessIconCanvas.InvalidateSurface();
	}

	private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName is nameof(ViewerViewModel.ShowRegularOrderIcon) or nameof(ViewerViewModel.ShowIrregularOrderIcon))
		{
			OrderTypeIconCanvas.InvalidateSurface();
		}

		if (e.PropertyName == nameof(ViewerViewModel.ShowImpetuousIcon))
		{
			ImpetuousIconCanvas.InvalidateSurface();
		}

		if (e.PropertyName == nameof(ViewerViewModel.ShowTacticalAwarenessIcon))
		{
			TacticalAwarenessIconCanvas.InvalidateSurface();
		}
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

	private void OnOrderTypeIconCanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
	{
		var canvas = e.Surface.Canvas;
		canvas.Clear(SKColors.Transparent);

		var picture = _viewModel.ShowIrregularOrderIcon ? _irregularOrderIconPicture : _regularOrderIconPicture;
		DrawPictureCentered(canvas, e.Info, picture);
	}

	private void OnImpetuousIconCanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
	{
		var canvas = e.Surface.Canvas;
		canvas.Clear(SKColors.Transparent);
		DrawPictureCentered(canvas, e.Info, _impetuousIconPicture);
	}

	private void OnTacticalAwarenessIconCanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
	{
		var canvas = e.Surface.Canvas;
		canvas.Clear(SKColors.Transparent);
		DrawPictureCentered(canvas, e.Info, _tacticalAwarenessIconPicture);
	}

	private static void DrawPictureCentered(SKCanvas canvas, SKImageInfo info, SKPicture? picture)
	{
		if (picture is null)
		{
			return;
		}

		var bounds = picture.CullRect;
		if (bounds.Width <= 0 || bounds.Height <= 0)
		{
			return;
		}

		var scale = Math.Min(info.Width / bounds.Width, info.Height / bounds.Height);
		var x = (info.Width - (bounds.Width * scale)) / 2f;
		var y = (info.Height - (bounds.Height * scale)) / 2f;

		canvas.Translate(x, y);
		canvas.Scale(scale);
		canvas.DrawPicture(picture);
	}
}
