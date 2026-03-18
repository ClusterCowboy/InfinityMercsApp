using InfinityMercsApp.Infrastructure.Providers;
using InfinityMercsApp.Services;

namespace InfinityMercsApp.Views.Common.UICommon;

internal sealed class CompanyCaptainWorkflowRequest
{
    public required INavigation Navigation { get; init; }
    public required int FallbackSourceFactionId { get; init; }
    public required int? FirstSourceFactionId { get; init; }
    public required string UnitName { get; init; }
    public required int UnitCost { get; init; }
    public required string UnitStatline { get; init; }
    public required string UnitRangedWeapons { get; init; }
    public required string UnitCcWeapons { get; init; }
    public required string UnitSkills { get; init; }
    public required string UnitEquipment { get; init; }
    public string? UnitCachedLogoPath { get; init; }
    public string? UnitPackagedLogoPath { get; init; }
    public required Func<int, int?> TryGetParentFactionId { get; init; }
    public required Func<int, string?> TryGetFactionName { get; init; }
    public required Func<int, string?> TryGetMetadataFactionName { get; init; }
    public required IArmyDataService ArmyDataService { get; init; }
    public required ISpecOpsProvider SpecOpsProvider { get; init; }
    public required bool ShowUnitsInInches { get; init; }
}
