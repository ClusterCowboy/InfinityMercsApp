using InfinityMercsApp.Infrastructure;
using InfinityMercsApp.Infrastructure.Options;
using InfinityMercsApp.Services;
using InfinityMercsApp.ViewModels;
using InfinityMercsApp.Views;
using InfinityMercsApp.Views.UnitEncyclopedia;
using Microsoft.Extensions.Logging;
using SkiaSharp.Views.Maui.Controls.Hosting;

namespace InfinityMercsApp;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseSkiaSharp()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
				fonts.AddFont("Ethnocentric-Regular.otf", "EthnocentricRegular");
				fonts.AddFont("Conthrax-SemiBold.otf", "ConthraxSemiBold");
			});

		// Seed the working DB from the bundled snapshot before anything opens a connection to it,
		// so first launch starts populated instead of importing every faction over the network.
		var dbPath = Path.Combine(FileSystem.Current.AppDataDirectory, "infinitymercs.db3");
		DatabaseSeeder.EnsureSeeded(dbPath);

		builder.Services
				.AddInfrastructureServices()
				.AddAppServices()
				.AddViewModels()
				.AddPages()
				// Change this once AppSettings is set up. Wish MAUI did this by default.
				.AddSingleton(new SQLIteConfiguration() { DBPath = dbPath });

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
