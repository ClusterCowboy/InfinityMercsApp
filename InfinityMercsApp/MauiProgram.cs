using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Core;
using InfinityMercsApp.Infrastructure;
using InfinityMercsApp.Infrastructure.Options;
using InfinityMercsApp.Services;
using InfinityMercsApp.ViewModels;
using InfinityMercsApp.Views;
using Microsoft.Extensions.Logging;
using SkiaSharp.Views.Maui.Controls.Hosting;
using System.Net;

namespace InfinityMercsApp;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .UseMauiCommunityToolkitCore()
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
		})
				.AddInfrastructureServices()
				.AddAppServices()
				.AddViewModels()
				.AddPages()
				// Change this once AppSettings is set up. Wish MAUI did this by default.
				.AddSingleton(new SQLIteConfiguration() { DBPath = Path.Combine(FileSystem.Current.AppDataDirectory, "infinitymercs.db3") });

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
