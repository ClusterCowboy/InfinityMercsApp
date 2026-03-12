using SkiaSharp;
using SkiaSharp.Views.Maui;

namespace InfinityMercsApp.Views;

public partial class MainPage : ContentPage
{
	private SKBitmap? _headerBitmap;

	public MainPage()
	{
		InitializeComponent();
	}

	private async void OnNewArmyClicked(object? sender, EventArgs e)
	{
		await Shell.Current.GoToAsync(nameof(CreateNewArmyPage));
	}

	private async void OnLoadCompanyClicked(object? sender, EventArgs e)
	{
		await Shell.Current.GoToAsync(nameof(LoadCompanyPage));
	}
}
