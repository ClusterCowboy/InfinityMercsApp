using System.Net;
using InfinityMercsApp.Data.Database;
using InfinityMercsApp.Data.WebAccess;
using InfinityMercsApp.Services;
using InfinityMercsApp.ViewModels;
using InfinityMercsApp.Views;
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
			});

		builder.Services.AddSingleton(sp =>
		{
			var handler = new HttpClientHandler
			{
				AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
			};

			return new HttpClient(handler);
		});
		builder.Services.AddSingleton<IDatabaseContext, DatabaseContext>();
		builder.Services.AddSingleton<IMetadataAccessor, MetadataAccessor>();
		builder.Services.AddSingleton<IArmyDataAccessor, ArmyDataAccessor>();
		builder.Services.AddSingleton<IWebAccessObject, CBWebApi>();
		builder.Services.AddSingleton<FactionLogoCacheService>();
		builder.Services.AddSingleton<AppInitializationService>();
		builder.Services.AddTransient<MainViewModel>();
		builder.Services.AddTransient<ViewerViewModel>();
		builder.Services.AddTransient<MainPage>();
		builder.Services.AddTransient<SplashPage>();
		builder.Services.AddTransient<ViewerPage>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
