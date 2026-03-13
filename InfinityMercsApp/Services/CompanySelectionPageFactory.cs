using InfinityMercsApp.Data.Database;
using InfinityMercsApp.Infrastructure.Providers;
using InfinityMercsApp.Views;
using InfinityMercsApp.Views.CohesiveCompany;
using InfinityMercsApp.Views.StandardCompany;

namespace InfinityMercsApp.Services;

public sealed class CompanySelectionPageFactory : ICompanySelectionPageFactory
{
    private readonly IMetadataProvider? _metadataProvider;
    private readonly IFactionProvider? _factionProvider;
    private readonly ISpecOpsDataAccessor _specOpsDataAccessor;
    private readonly ICohesiveCompanyFactionQueryAccessor? _cohesiveCompanyFactionQueryAccessor;
    private readonly FactionLogoCacheService? _factionLogoCacheService;
    private readonly IAppSettingsProvider? _appSettingsProvider;

    public CompanySelectionPageFactory(
        IMetadataProvider? metadataProvider,
        IFactionProvider? factionProvider,
        ISpecOpsDataAccessor specOpsDataAccessor,
        ICohesiveCompanyFactionQueryAccessor? cohesiveCompanyFactionQueryAccessor,
        FactionLogoCacheService? factionLogoCacheService,
        IAppSettingsProvider? appSettingsProvider)
    {
        _metadataProvider = metadataProvider;
        _factionProvider = factionProvider;
        _specOpsDataAccessor = specOpsDataAccessor;
        _cohesiveCompanyFactionQueryAccessor = cohesiveCompanyFactionQueryAccessor;
        _factionLogoCacheService = factionLogoCacheService;
        _appSettingsProvider = appSettingsProvider;
    }

    public StandardCompanySelectionPage CreateStandard(ArmySourceSelectionMode mode)
    {
        return new StandardCompanySelectionPage(
            mode,
            _metadataProvider,
            _factionProvider,
            _specOpsDataAccessor,
            _cohesiveCompanyFactionQueryAccessor,
            _factionLogoCacheService,
            _appSettingsProvider);
    }

    public CCArmyFactionSelectionPage CreateCohesive(ArmySourceSelectionMode mode)
    {
        return new CCArmyFactionSelectionPage(
            mode,
            _metadataProvider,
            _factionProvider,
            _specOpsDataAccessor,
            _cohesiveCompanyFactionQueryAccessor,
            _factionLogoCacheService,
            _appSettingsProvider);
    }
}
