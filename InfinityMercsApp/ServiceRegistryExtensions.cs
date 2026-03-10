using InfinityMercsApp.Infrastructure.API.InfinityArmy;
using InfinityMercsApp.Infrastructure.Providers;
using InfinityMercsApp.Infrastructure.Repositories;
using InfinityMercsApp.Services;
using InfinityMercsApp.ViewModels;
using InfinityMercsApp.Views;
using System;
using System.Collections.Generic;
using System.Text;

namespace InfinityMercsApp;

public static class ServiceRegistryExtensions
{
    public static IServiceCollection AddViewModels(this IServiceCollection services)
    {
        services.AddTransient<ModeSelectionViewModel>()
                .AddTransient<SplashPageViewModel>()
                .AddTransient<ViewerViewModel>()
                .AddTransient<CreateNewArmyPageViewModel>();

        return services;
    }

    public static IServiceCollection AddPages(this IServiceCollection services)
    {
        services.AddTransient<ModeSelectionPage>()
                .AddTransient<SplashPage>()
                .AddTransient<ViewerPage>()
                .AddTransient<FeedbackBugsPage>();

        return services;
    }

    public static IServiceCollection AddAppServices(this IServiceCollection services)
    {
        services.AddSingleton<INavigationService, MauiNavigationService>()
                .AddSingleton<FactionLogoCacheService>()
                .AddSingleton<IFeedbackService, FeedbackService>()
                .AddSingleton<IImportService, ImportService>();

        return services;
    }
}
