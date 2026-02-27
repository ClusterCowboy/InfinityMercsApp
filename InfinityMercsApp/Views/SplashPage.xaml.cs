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

	protected override void OnAppearing()
	{
		base.OnAppearing();

		if (_navigated)
		{
			return;
		}

		_navigated = true;

		var initializationService = Application.Current?.Handler?.MauiContext?.Services.GetService<AppInitializationService>();
		if (Application.Current?.Windows.Count > 0)
		{
			Application.Current.Windows[0].Page = new AppShell();
		}

		_ = RunStartupCheckAsync(initializationService);
	}

	private static async Task RunStartupCheckAsync(AppInitializationService? initializationService)
	{
		if (initializationService is null)
		{
			return;
		}

		try
		{
			// Let the shell render before running network/database startup checks.
			await Task.Delay(250);
			await Task.Run(() => initializationService.InitializeAsync());
		}
		catch
		{
			// Ignore startup check failures so app launch is never blocked.
		}
	}
}
