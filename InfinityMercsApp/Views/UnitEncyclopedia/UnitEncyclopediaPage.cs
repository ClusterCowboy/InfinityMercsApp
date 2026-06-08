using InfinityMercsApp.ViewModels;
using InfinityMercsApp.Views.Controls;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using Svg.Skia;
using System.ComponentModel;

namespace InfinityMercsApp.Views.UnitEncyclopedia;

public partial class UnitEncyclopediaPage : ContentPage
{
	private readonly ViewerViewModel _viewModel;
	private bool _loaded;
	private double _factionDragStartScrollY;
	private double _unitDragStartScrollY;
	private SKPicture? _regularOrderIconPicture;
	private SKPicture? _irregularOrderIconPicture;
	private SKPicture? _impetuousIconPicture;
	private SKPicture? _tacticalAwarenessIconPicture;
	private SKPicture? _cubeIconPicture;
	private SKPicture? _cube2IconPicture;
	private SKPicture? _hackableIconPicture;
	private SKPicture? _filterIconPicture;
	private SKPicture? _selectedUnitPicture;
	private int _selectedUnitLogoLoadVersion;
	private UnitFilterPopupView? _activeUnitFilterPopup;

	public UnitEncyclopediaPage(ViewerViewModel viewModel)
	{
		InitializeComponent();
		_viewModel = viewModel;
		BindingContext = _viewModel;
		_viewModel.PropertyChanged += OnViewModelPropertyChanged;
		_ = LoadHeaderIconsAsync();
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		if (_viewModel.IsLoading)
		{
			return;
		}

		if (_loaded && _viewModel.Factions.Count > 0)
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
		_cubeIconPicture?.Dispose();
		_cubeIconPicture = null;
		_cube2IconPicture?.Dispose();
		_cube2IconPicture = null;
		_hackableIconPicture?.Dispose();
		_hackableIconPicture = null;
		_filterIconPicture?.Dispose();
		_filterIconPicture = null;

		try
		{
			await using var regularStream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/CBIcons/regular.svg");
			var regularSvg = new SKSvg();
			_regularOrderIconPicture = regularSvg.Load(regularStream);
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine($"UnitEncyclopediaPage regular order icon load failed: {ex.Message}");
		}

		try
		{
			await using var irregularStream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/CBIcons/irregular.svg");
			var irregularSvg = new SKSvg();
			_irregularOrderIconPicture = irregularSvg.Load(irregularStream);
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine($"UnitEncyclopediaPage irregular order icon load failed: {ex.Message}");
		}

		try
		{
			await using var impetuousStream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/CBIcons/impetuous.svg");
			var impetuousSvg = new SKSvg();
			_impetuousIconPicture = impetuousSvg.Load(impetuousStream);
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine($"UnitEncyclopediaPage impetuous icon load failed: {ex.Message}");
		}

		try
		{
			await using var tacticalStream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/CBIcons/tactical.svg");
			var tacticalSvg = new SKSvg();
			_tacticalAwarenessIconPicture = tacticalSvg.Load(tacticalStream);
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine($"UnitEncyclopediaPage tactical awareness icon load failed: {ex.Message}");
		}

		try
		{
			await using var cubeStream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/CBIcons/cube.svg");
			var cubeSvg = new SKSvg();
			_cubeIconPicture = cubeSvg.Load(cubeStream);
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine($"UnitEncyclopediaPage cube icon load failed: {ex.Message}");
		}

		try
		{
			await using var cube2Stream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/CBIcons/cube2.svg");
			var cube2Svg = new SKSvg();
			_cube2IconPicture = cube2Svg.Load(cube2Stream);
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine($"UnitEncyclopediaPage cube 2.0 icon load failed: {ex.Message}");
		}

		try
		{
			await using var hackableStream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/CBIcons/hackable.svg");
			var hackableSvg = new SKSvg();
			_hackableIconPicture = hackableSvg.Load(hackableStream);
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine($"UnitEncyclopediaPage hackable icon load failed: {ex.Message}");
		}

		try
		{
			await using var filterStream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/NonCBIcons/noun-filter.svg");
			var filterSvg = new SKSvg();
			_filterIconPicture = filterSvg.Load(filterStream);
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine($"UnitEncyclopediaPage filter icon load failed: {ex.Message}");
		}

		UnitSelectionFilterCanvas.InvalidateSurface();
		UnitDisplayConfigurationsView.RegularOrderIconPicture = _regularOrderIconPicture;
		UnitDisplayConfigurationsView.IrregularOrderIconPicture = _irregularOrderIconPicture;
		UnitDisplayConfigurationsView.ImpetuousIconPicture = _impetuousIconPicture;
		UnitDisplayConfigurationsView.TacticalAwarenessIconPicture = _tacticalAwarenessIconPicture;
		UnitDisplayConfigurationsView.CubeIconPicture = _cubeIconPicture;
		UnitDisplayConfigurationsView.Cube2IconPicture = _cube2IconPicture;
		UnitDisplayConfigurationsView.HackableIconPicture = _hackableIconPicture;
		UnitDisplayConfigurationsView.InvalidateHeaderIconsCanvas();
	}

	private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName is nameof(ViewerViewModel.ShowRegularOrderIcon)
			or nameof(ViewerViewModel.ShowIrregularOrderIcon)
			or nameof(ViewerViewModel.ShowImpetuousIcon)
			or nameof(ViewerViewModel.ShowTacticalAwarenessIcon)
			or nameof(ViewerViewModel.ShowCubeIcon)
			or nameof(ViewerViewModel.ShowCube2Icon)
			or nameof(ViewerViewModel.ShowHackableIcon))
		{
			UnitDisplayConfigurationsView.InvalidateHeaderIconsCanvas();
		}

		if (e.PropertyName == nameof(ViewerViewModel.SelectedUnit))
		{
			_ = LoadSelectedUnitLogoAsync(_viewModel.SelectedUnit);
		}
	}

	private async Task LoadSelectedUnitLogoAsync(ViewerUnitItem? unit)
	{
		var loadVersion = ++_selectedUnitLogoLoadVersion;
		SKPicture? loadedPicture = null;

		try
		{
			if (unit is not null)
			{
				Stream? stream = null;
				try
				{
					if (!string.IsNullOrWhiteSpace(unit.CachedLogoPath) && File.Exists(unit.CachedLogoPath))
					{
						stream = File.OpenRead(unit.CachedLogoPath);
					}
					else if (!string.IsNullOrWhiteSpace(unit.PackagedLogoPath))
					{
						stream = await FileSystem.Current.OpenAppPackageFileAsync(unit.PackagedLogoPath);
					}

					if (stream is not null)
					{
						await using (stream)
						{
							var svg = new SKSvg();
							loadedPicture = svg.Load(stream);
						}
					}
				}
				catch (Exception ex)
				{
					Console.Error.WriteLine($"UnitEncyclopediaPage selected unit logo load failed: {ex.Message}");
				}
			}

			if (loadVersion != _selectedUnitLogoLoadVersion)
			{
				loadedPicture?.Dispose();
				return;
			}

			_selectedUnitPicture?.Dispose();
			_selectedUnitPicture = loadedPicture;
			UnitDisplayConfigurationsView.SelectedUnitPicture = _selectedUnitPicture;
			UnitDisplayConfigurationsView.InvalidateSelectedUnitCanvas();
		}
		catch
		{
			loadedPicture?.Dispose();
			throw;
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

	private async void OnUnitSelectionFilterButtonTapped(object? sender, TappedEventArgs e)
	{
		try
		{
			var options = await _viewModel.BuildUnitFilterPopupOptionsAsync();
			var popup = new UnitFilterPopupView(
				options,
				_viewModel.ActiveUnitFilter,
				lieutenantOnlyUnits: _viewModel.LieutenantOnlyUnits,
				teamsView: false);
			var popupHeight = ResolveUnitFilterPopupHeight();
			popup.HeightRequest = popupHeight;
			popup.FilterArmyApplied += OnFilterArmyApplied;
			popup.CloseRequested += OnUnitFilterPopupCloseRequested;
			_activeUnitFilterPopup = popup;
			UnitFilterPopupHost.HeightRequest = popupHeight;
			UnitFilterPopupHost.Content = popup;
			UnitFilterOverlay.IsVisible = true;
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine($"UnitEncyclopediaPage filter popup open failed: {ex.Message}");
		}
	}

	private async void OnFilterArmyApplied(object? sender, UnitFilterCriteria criteria)
	{
		CloseUnitFilterPopup(sender as UnitFilterPopupView);
		await _viewModel.ApplyActiveUnitFilterAsync(criteria);
	}

	private void OnUnitFilterPopupCloseRequested(object? sender, EventArgs e)
	{
		CloseUnitFilterPopup(sender as UnitFilterPopupView);
	}

	private void CloseUnitFilterPopup(UnitFilterPopupView? popup)
	{
		var target = popup ?? _activeUnitFilterPopup;
		if (target is not null)
		{
			target.FilterArmyApplied -= OnFilterArmyApplied;
			target.CloseRequested -= OnUnitFilterPopupCloseRequested;
		}

		_activeUnitFilterPopup = null;
		UnitFilterPopupHost.Content = null;
		UnitFilterPopupHost.HeightRequest = -1;
		UnitFilterOverlay.IsVisible = false;
	}

	private double ResolveUnitFilterPopupHeight()
	{
		var pageHeight = Height > 0 ? Height : Window?.Height ?? Application.Current?.Windows.FirstOrDefault()?.Page?.Height ?? 0;
		if (pageHeight <= 0)
		{
			return 800;
		}

		return pageHeight * 0.9;
	}

	private void OnUnitSelectionFilterCanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
	{
		var canvas = e.Surface.Canvas;
		canvas.Clear(SKColors.Transparent);
		if (_filterIconPicture is null)
		{
			return;
		}

		DrawPictureInRect(canvas, _filterIconPicture, new SKRect(0, 0, e.Info.Width, e.Info.Height));
	}

	private static void DrawPictureInRect(SKCanvas canvas, SKPicture picture, SKRect destination)
	{
		var bounds = picture.CullRect;
		if (bounds.Width <= 0 || bounds.Height <= 0)
		{
			return;
		}

		var scale = Math.Min(destination.Width / bounds.Width, destination.Height / bounds.Height);
		var drawnWidth = bounds.Width * scale;
		var drawnHeight = bounds.Height * scale;
		var translateX = destination.Left + ((destination.Width - drawnWidth) / 2f) - (bounds.Left * scale);
		var translateY = destination.Top + ((destination.Height - drawnHeight) / 2f) - (bounds.Top * scale);

		using var restore = new SKAutoCanvasRestore(canvas, true);
		canvas.Translate(translateX, translateY);
		canvas.Scale(scale);
		canvas.DrawPicture(picture);
	}
}

