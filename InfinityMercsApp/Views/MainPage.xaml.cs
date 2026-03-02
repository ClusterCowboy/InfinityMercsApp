using SkiaSharp;
using SkiaSharp.Views.Maui;

namespace InfinityMercsApp.Views;

public partial class MainPage : ContentPage
{
	private SKBitmap? _headerBitmap;

	public MainPage()
	{
		InitializeComponent();
		_ = LoadHeaderImageAsync();
	}

	private async void OnNewArmyClicked(object? sender, EventArgs e)
	{
		await Shell.Current.GoToAsync(nameof(CreateNewArmyPage));
	}

	private async void OnLoadCompanyClicked(object? sender, EventArgs e)
	{
		await Shell.Current.GoToAsync(nameof(LoadCompanyPage));
	}

	private async Task LoadHeaderImageAsync()
	{
		try
		{
			Stream? stream = null;
			try
			{
				stream = await FileSystem.Current.OpenAppPackageFileAsync("m2_no_bg_short.webp");
			}
			catch
			{
				// Fallback to packaged SVGCache asset path.
			}

			stream ??= await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/NonCBIcons/M2-no-bg-short.webp");
			await using (stream)
			{
				_headerBitmap?.Dispose();
				_headerBitmap = SKBitmap.Decode(stream);
			}
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine($"MainPage header image load failed: {ex.Message}");
			_headerBitmap?.Dispose();
			_headerBitmap = null;
		}

		HeaderImageCanvas.InvalidateSurface();
	}

	private void OnHeaderImageCanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
	{
		var canvas = e.Surface.Canvas;
		canvas.Clear(SKColors.Transparent);
		if (_headerBitmap is null || _headerBitmap.Width <= 0 || _headerBitmap.Height <= 0)
		{
			return;
		}

		var info = e.Info;
		var scale = Math.Min((float)info.Width / _headerBitmap.Width, (float)info.Height / _headerBitmap.Height);
		var width = _headerBitmap.Width * scale;
		var height = _headerBitmap.Height * scale;
		var x = (info.Width - width) / 2f;
		var y = (info.Height - height) / 2f;
		var dest = new SKRect(x, y, x + width, y + height);
		canvas.DrawBitmap(_headerBitmap, dest);
	}

}
