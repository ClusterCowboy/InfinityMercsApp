using InfinityMercsApp.Data.Database;
using InfinityMercsApp.Services;
using InfinityMercsApp.ViewModels;
using InfinityMercsApp.Views;
using InfinityMercsApp.Views.UnitEncyclopedia;

namespace InfinityMercsApp;

public static class ServiceRegistryExtensions
{
    public static IServiceCollection AddViewModels(this IServiceCollection services)
    {
        services.AddTransient<ModeSelectionViewModel>()
                .AddTransient<SplashPageViewModel>()
                .AddTransient<MainViewModel>()
                .AddTransient<ViewerViewModel>()
                .AddTransient<CreateNewCompanyPageViewModel>()
                .AddTransient<StandardCompanySourcePopupPageViewModel>();

        return services;
    }

    public static IServiceCollection AddPages(this IServiceCollection services)
    {
        services.AddTransient<AppShell>()
                .AddTransient<ModeSelectionPage>()
                .AddTransient<CreateNewCompanyPage>()
                .AddTransient<StandardCompanySourcePopupPage>()
                .AddTransient<SplashPage>()
                .AddTransient<SettingsPage>()
                .AddTransient<CompanyViewerPage>()
                .AddTransient<UnitEncyclopediaPage>()
                .AddTransient<FeedbackBugsPage>();

        return services;
    }

    public static IServiceCollection AddAppServices(this IServiceCollection services)
    {
        services.AddSingleton<DatabaseContext>()
                .AddSingleton<SpecOpsDataAccessor>()
                .AddSingleton<ArmyDataAccessor>()
                .AddSingleton<MercsArmyListAccessor>()
                .AddSingleton<CohesiveCompanyFactionQueryAccessor>()
                .AddSingleton<INavigationService, MauiNavigationService>()
                .AddSingleton<ICompanySelectionPageFactory, CompanySelectionPageFactory>()
                .AddSingleton<FactionLogoCacheService>()
                .AddSingleton<IFeedbackService, FeedbackService>()
                .AddSingleton<IImportService, ImportService>();

        return services;
    }
}
