using InfinityMercsApp.Infrastructure.Providers;
using InfinityMercsApp.Views;
using InfinityMercsApp.Views.AirborneCompany;
using InfinityMercsApp.Views.CohesiveCompany;
using InfinityMercsApp.Views.InspiringCompany;
using InfinityMercsApp.Views.StandardCompany;

namespace InfinityMercsApp.Services;

public sealed class CompanySelectionPageFactory : ICompanySelectionPageFactory
{
    private readonly IMetadataProvider? _metadataProvider;
    private readonly IFactionProvider? _factionProvider;
    private readonly ISpecOpsProvider _specOpsProvider;
    private readonly ICohesiveCompanyFactionQueryProvider _cohesiveCompanyFactionQueryProvider;
    private readonly FactionLogoCacheService? _factionLogoCacheService;
    private readonly IAppSettingsProvider? _appSettingsProvider;
    private readonly IArmyDataService _armyDataService;

    public CompanySelectionPageFactory(
        IMetadataProvider? metadataProvider,
        IFactionProvider? factionProvider,
        ISpecOpsProvider specOpsProvider,
        ICohesiveCompanyFactionQueryProvider cohesiveCompanyFactionQueryProvider,
        FactionLogoCacheService? factionLogoCacheService,
        IAppSettingsProvider? appSettingsProvider,
        IArmyDataService armyDataService)
    {
        _metadataProvider = metadataProvider;
        _factionProvider = factionProvider;
        _specOpsProvider = specOpsProvider;
        _cohesiveCompanyFactionQueryProvider = cohesiveCompanyFactionQueryProvider;
        _factionLogoCacheService = factionLogoCacheService;
        _appSettingsProvider = appSettingsProvider;
        _armyDataService = armyDataService;
    }

    public StandardCompanySelectionPage CreateStandard(ArmySourceSelectionMode mode)
    {
        return new StandardCompanySelectionPage(
            mode,
            _metadataProvider,
            _factionProvider,
            _specOpsProvider,
            _cohesiveCompanyFactionQueryProvider,
            _factionLogoCacheService,
            _appSettingsProvider,
            _armyDataService);
    }

    public CohesiveCompanySelectionPage CreateCohesive(ArmySourceSelectionMode mode)
    {
        return new CohesiveCompanySelectionPage(
            mode,
            _metadataProvider,
            _factionProvider,
            _specOpsProvider,
            _cohesiveCompanyFactionQueryProvider,
            _factionLogoCacheService,
            _appSettingsProvider,
            _armyDataService);
    }

    public InspiringCompanySelectionPage CreateInspiring()
    {
        return new InspiringCompanySelectionPage(
            _metadataProvider,
            _factionProvider,
            _specOpsProvider,
            _cohesiveCompanyFactionQueryProvider,
            _factionLogoCacheService,
            _appSettingsProvider,
            _armyDataService);
    }
}

