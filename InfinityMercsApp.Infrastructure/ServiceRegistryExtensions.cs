using InfinityMercsApp.Infrastructure.API.InfinityArmy;
using InfinityMercsApp.Infrastructure.Providers;
using InfinityMercsApp.Infrastructure.Repositories;
using InfinityMercsApp.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace InfinityMercsApp.Infrastructure;

public static class ServiceRegistryExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        // Register all the related services here
        services.AddSingleton<ISQLiteRepository, SQLiteRepository>()
                .AddSingleton<IInfinityArmyAPI, InfinityArmyAPI>()
                .AddSingleton<IAppSettingsProvider, AppSettingsProvider>()
                .AddSingleton<IArmyImportProvider, ArmyImportProvider>()
                .AddSingleton<IFactionProvider, FactionProvider>()
                .AddSingleton<IMetadataProvider, MetadataProvider>()
                .AddSingleton<ISpecOpsProvider, SpecOpsProvider>()
                .AddSingleton<IArmySourceSelectionModeService,  ArmySourceSelectionModeService>()
                .AddSingleton<IMercsArmyListProvider, MercsArmyListProvider>()
                ;
        return services;
    }
}
