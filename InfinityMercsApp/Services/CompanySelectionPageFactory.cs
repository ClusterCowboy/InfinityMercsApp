using InfinityMercsApp.Data.Database;
using InfinityMercsApp.Infrastructure.Providers;
using InfinityMercsApp.Views;
using InfinityMercsApp.Views.CohesiveCompany;
using InfinityMercsApp.Views.StandardCompany;

namespace InfinityMercsApp.Services;

public sealed class CompanySelectionPageFactory : ICompanySelectionPageFactory
{
    private readonly IMetadataProvider? _metadataProvider;
    private readonly IArmyDataAccessor? _armyDataAccessor;
    private readonly IMercsArmyListAccessor? _mercsArmyListAccessor;
    private readonly ISpecOpsDataAccessor _specOpsDataAccessor;
    private readonly FactionLogoCacheService? _factionLogoCacheService;
    private readonly AppSettingsService? _appSettingsService;

    public CompanySelectionPageFactory(
        IMetadataProvider? metadataProvider,
        IArmyDataAccessor? armyDataAccessor,
        IMercsArmyListAccessor? mercsArmyListAccessor,
        ISpecOpsDataAccessor specOpsDataAccessor,
        FactionLogoCacheService? factionLogoCacheService,
        AppSettingsService? appSettingsService)
    {
        _metadataProvider = metadataProvider;
        _armyDataAccessor = armyDataAccessor;
        _mercsArmyListAccessor = mercsArmyListAccessor;
        _specOpsDataAccessor = specOpsDataAccessor;
        _factionLogoCacheService = factionLogoCacheService;
        _appSettingsService = appSettingsService;
    }

    public StandardCompanySelectionPage CreateStandard(ArmySourceSelectionMode mode)
    {
        return new StandardCompanySelectionPage(
            mode,
            _metadataProvider,
            _armyDataAccessor,
            _mercsArmyListAccessor,
            _specOpsDataAccessor,
            _factionLogoCacheService,
            _appSettingsService);
    }

    public CCArmyFactionSelectionPage CreateCohesive(ArmySourceSelectionMode mode)
    {
        return new CCArmyFactionSelectionPage(
            mode,
            _metadataProvider,
            _armyDataAccessor,
            _mercsArmyListAccessor,
            _specOpsDataAccessor,
            _factionLogoCacheService,
            _appSettingsService);
    }
}
