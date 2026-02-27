using InfinityMercsApp.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using Svg.Skia;
using System.ComponentModel;
using InfinityMercsApp.Services;

namespace InfinityMercsApp.Views;

public partial class ViewerPage : ContentPage
{
	private readonly ViewerViewModel _viewModel;
	private readonly FactionLogoCacheService? _factionLogoCacheService;
	private SKPicture? _svgPicture;
	private bool _loaded;
	private double _dragStartScrollY;

	public ViewerPage()
	{
		InitializeComponent();
		var services = Application.Current?.Handler?.MauiContext?.Services;
		_viewModel = services?.GetService<ViewerViewModel>()
			?? new ViewerViewModel();
		_factionLogoCacheService = services?.GetService<FactionLogoCacheService>();
		BindingContext = _viewModel;
		_viewModel.PropertyChanged += OnViewModelPropertyChanged;
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

	private async void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName == nameof(ViewerViewModel.SelectedFaction))
		{
			await LoadSelectedFactionLogoAsync();
		}
	}

	private async Task LoadSelectedFactionLogoAsync()
	{
		if (_factionLogoCacheService is null)
		{
			SvgStatusLabel.Text = "Logo cache service unavailable.";
			return;
		}

		var faction = _viewModel.SelectedFaction;
		if (faction is null)
		{
			_svgPicture?.Dispose();
			_svgPicture = null;
			SvgStatusLabel.Text = "Select a faction.";
			SvgCanvas.InvalidateSurface();
			return;
		}

		var cachedPath = _factionLogoCacheService.TryGetCachedLogoPath(faction.Id);
		if (string.IsNullOrWhiteSpace(cachedPath))
		{
			if (faction.Id == FactionLogoCacheService.DebugFactionId)
			{
				var debugInfo = _factionLogoCacheService.GetDebugInfo(faction.Id, faction.Logo);
				Console.Error.WriteLine(
					$"[SVG DEBUG] Viewer miss for {faction.Id}: exists={debugInfo.Exists}, bytes={debugInfo.SizeBytes}, path={debugInfo.LocalPath}, url={debugInfo.ExpectedLogoUrl ?? "<null>"}");
			}

			_svgPicture?.Dispose();
			_svgPicture = null;
			SvgStatusLabel.Text = "No cached SVG for this faction yet.";
			SvgCanvas.InvalidateSurface();
			return;
		}

		try
		{
			SvgStatusLabel.Text = "Loading cached SVG...";
			await using var stream = File.OpenRead(cachedPath);
			var svg = new SKSvg();
			_svgPicture?.Dispose();
			_svgPicture = svg.Load(stream);
			SvgStatusLabel.Text = _svgPicture is null ? "SVG could not be parsed." : cachedPath;
			SvgCanvas.InvalidateSurface();
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine($"LoadSelectedFactionLogoAsync failed: {ex.Message}");
			_svgPicture?.Dispose();
			_svgPicture = null;
			SvgStatusLabel.Text = $"SVG load failed: {ex.Message}";
			SvgCanvas.InvalidateSurface();
		}
	}

	private void OnSvgCanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
	{
		var canvas = e.Surface.Canvas;
		canvas.Clear(SKColors.Transparent);

		if (_svgPicture is null)
		{
			return;
		}

		var pictureBounds = _svgPicture.CullRect;
		if (pictureBounds.Width <= 0 || pictureBounds.Height <= 0)
		{
			return;
		}

		var scale = Math.Min(
			e.Info.Width / pictureBounds.Width,
			e.Info.Height / pictureBounds.Height);

		var x = (e.Info.Width - (pictureBounds.Width * scale)) / 2f;
		var y = (e.Info.Height - (pictureBounds.Height * scale)) / 2f;

		canvas.Translate(x, y);
		canvas.Scale(scale);
		canvas.DrawPicture(_svgPicture);
	}

	private async void OnFactionListPanUpdated(object? sender, PanUpdatedEventArgs e)
	{
		switch (e.StatusType)
		{
			case GestureStatus.Started:
				_dragStartScrollY = FactionScrollView.ScrollY;
				break;
			case GestureStatus.Running:
				var targetY = Math.Max(0, _dragStartScrollY - e.TotalY);
				await FactionScrollView.ScrollToAsync(0, targetY, false);
				break;
		}
	}
}
