using InfinityMercsApp.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using Svg.Skia;
using System.ComponentModel;

namespace InfinityMercsApp.Views;

public partial class ViewerPage : ContentPage
{
	private const int MaxIconsPerRow = 3;
	private const float IconSize = 24f;
	private const float IconGap = 20f;
	private const float RightPadding = 24f;
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
		_cubeIconPicture?.Dispose();
		_cubeIconPicture = null;
		_cube2IconPicture?.Dispose();
		_cube2IconPicture = null;
		_hackableIconPicture?.Dispose();
		_hackableIconPicture = null;

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

		try
		{
			await using var cubeStream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/NonCBIcons/cube-alt-2-svgrepo-com.svg");
			var cubeSvg = new SKSvg();
			_cubeIconPicture = cubeSvg.Load(cubeStream);
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine($"ViewerPage cube icon load failed: {ex.Message}");
		}

		try
		{
			await using var cube2Stream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/NonCBIcons/cubes-svgrepo-com.svg");
			var cube2Svg = new SKSvg();
			_cube2IconPicture = cube2Svg.Load(cube2Stream);
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine($"ViewerPage cube 2.0 icon load failed: {ex.Message}");
		}

		try
		{
			await using var hackableStream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/NonCBIcons/noun-circuit-8241852.svg");
			var hackableSvg = new SKSvg();
			_hackableIconPicture = hackableSvg.Load(hackableStream);
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine($"ViewerPage hackable icon load failed: {ex.Message}");
		}

		TopIconRowCanvas.InvalidateSurface();
		BottomIconRowCanvas.InvalidateSurface();
	}

	private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName is nameof(ViewerViewModel.ShowRegularOrderIcon)
			or nameof(ViewerViewModel.ShowIrregularOrderIcon)
			or nameof(ViewerViewModel.ShowImpetuousIcon)
			or nameof(ViewerViewModel.ShowTacticalAwarenessIcon))
		{
			TopIconRowCanvas.InvalidateSurface();
		}

		if (e.PropertyName is nameof(ViewerViewModel.ShowCubeIcon)
			or nameof(ViewerViewModel.ShowCube2Icon)
			or nameof(ViewerViewModel.ShowHackableIcon))
		{
			BottomIconRowCanvas.InvalidateSurface();
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

	private void OnTopIconRowCanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
	{
		var canvas = e.Surface.Canvas;
		canvas.Clear(SKColors.Transparent);

		var pictures = new List<SKPicture>(MaxIconsPerRow);
		var orderTypePicture = _viewModel.ShowIrregularOrderIcon ? _irregularOrderIconPicture : _regularOrderIconPicture;
		if (_viewModel.HasOrderTypeIcon && orderTypePicture is not null)
		{
			pictures.Add(orderTypePicture);
		}

		if (_viewModel.ShowImpetuousIcon && _impetuousIconPicture is not null)
		{
			pictures.Add(_impetuousIconPicture);
		}

		if (_viewModel.ShowTacticalAwarenessIcon && _tacticalAwarenessIconPicture is not null)
		{
			pictures.Add(_tacticalAwarenessIconPicture);
		}

		DrawIconRow(canvas, e.Info, pictures);
	}

	private void OnBottomIconRowCanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
	{
		var canvas = e.Surface.Canvas;
		canvas.Clear(SKColors.Transparent);

		var pictures = new List<SKPicture>(MaxIconsPerRow);

		if (_viewModel.ShowCubeIcon && _cubeIconPicture is not null)
		{
			pictures.Add(_cubeIconPicture);
		}

		if (_viewModel.ShowCube2Icon && _cube2IconPicture is not null)
		{
			pictures.Add(_cube2IconPicture);
		}

		if (_viewModel.ShowHackableIcon && _hackableIconPicture is not null)
		{
			pictures.Add(_hackableIconPicture);
		}

		DrawIconRow(canvas, e.Info, pictures);
	}

	private static void DrawIconRow(SKCanvas canvas, SKImageInfo info, IReadOnlyList<SKPicture> pictures)
	{
		if (pictures.Count == 0)
		{
			return;
		}

		var drawCount = Math.Min(MaxIconsPerRow, pictures.Count);
		var rowWidth = (MaxIconsPerRow * IconSize) + ((MaxIconsPerRow - 1) * IconGap);
		var startX = info.Width - RightPadding - rowWidth;
		if (startX < 0)
		{
			startX = 0;
		}

		for (var i = 0; i < drawCount; i++)
		{
			var x = startX + (i * (IconSize + IconGap));
			var y = (info.Height - IconSize) / 2f;
			var destination = new SKRect(x, y, x + IconSize, y + IconSize);
			DrawPictureInRect(canvas, pictures[i], destination);
		}
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
