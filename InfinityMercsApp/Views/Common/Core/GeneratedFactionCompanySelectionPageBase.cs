using InfinityMercsApp.Infrastructure.Providers;
using InfinityMercsApp.Services;
using InfinityMercsApp.ViewModels;
using InfinityMercsApp.Views.Controls;

namespace InfinityMercsApp.Views.Common;

/// <summary>
/// Shared base for the two "generated faction" company pages (Airborne, Inspiring).
/// Both place a single synthetic company faction in the right selector slot and let the
/// player pick the captain/lieutenant from any sectorial in the left slot, so the entire
/// selection, visibility, unit-detail and roster flow is identical between them. Only a
/// handful of values differ, which the leaf pages supply through the abstract hooks below.
/// </summary>
public abstract partial class GeneratedFactionCompanySelectionPageBase : CompanySelectionPageBase, ICompanySelectionVisibilityState
{
    protected GeneratedFactionCompanySelectionPageBase(
        ArmySourceSelectionMode mode,
        IMetadataProvider? metadataProvider,
        IFactionProvider? factionProvider,
        ISpecOpsProvider specOpsProvider,
        ICohesiveCompanyFactionQueryProvider cohesiveCompanyFactionQueryProvider,
        FactionLogoCacheService? factionLogoCacheService,
        IAppSettingsProvider? appSettingsProvider)
        : base(mode, metadataProvider, factionProvider, specOpsProvider, cohesiveCompanyFactionQueryProvider, factionLogoCacheService, appSettingsProvider)
    {
    }

    /// <summary>Database id of this company's synthetic generated faction (placed in the right slot).</summary>
    protected abstract int CompanyFactionId { get; }

    /// <summary>Packaged SVG logo path for the synthetic company faction.</summary>
    protected abstract string CompanyLogoPath { get; }

    /// <summary>The season-points control hosted in the leaf page's XAML.</summary>
    protected abstract SeasonStartPointsView SeasonStartPointsControl { get; }

    /// <summary>The unit selection list panel hosted in the leaf page's XAML.</summary>
    protected abstract CompanyUnitSelectionListPanelView UnitSelectionPanelControl { get; }

    /// <summary>The unit-filter popup host hosted in the leaf page's XAML.</summary>
    protected abstract ContentView UnitFilterPopupHostControl { get; }

    /// <summary>The unit-filter overlay hosted in the leaf page's XAML.</summary>
    protected abstract Grid UnitFilterOverlayControl { get; }

    /// <summary>
    /// Applies the company-specific captain/lieutenant skill injection when building a roster
    /// entry. Airborne adds Parachutist/Network Support; Inspiring adds its leadership skill.
    /// </summary>
    protected abstract IReadOnlyCollection<string> ApplyCaptainSkills(IReadOnlyCollection<string> commonSkills, ViewerProfileItem profile);
}
