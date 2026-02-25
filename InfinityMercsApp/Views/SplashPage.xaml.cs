using InfinityMercsApp.Services;
using Microsoft.Extensions.DependencyInjection;

namespace InfinityMercsApp.Views;

public partial class SplashPage : ContentPage
{
	private bool _navigated;

	public SplashPage()
	{
		InitializeComponent();
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();

		if (_navigated)
		{
			return;
		}

		_navigated = true;

		var initializationService = Application.Current?.Handler?.MauiContext?.Services.GetService<AppInitializationService>();
		if (initializationService is not null)
		{
			await initializationService.InitializeAsync();
		}

		await Task.Delay(2000);

		if (Application.Current?.Windows.Count > 0)
		{
			Application.Current.Windows[0].Page = new AppShell();
		}
	}
}
