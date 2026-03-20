using ArmyFactionRecord = InfinityMercsApp.Domain.Models.Army.Faction;
using ArmyResumeRecord = InfinityMercsApp.Domain.Models.Army.Resume;
using ArmyUnitRecord = InfinityMercsApp.Domain.Models.Army.Unit;
using MercsArmyListEntry = InfinityMercsApp.Domain.Models.Army.MercsArmyListEntry;

namespace InfinityMercsApp.Views.Common;

public abstract partial class CompanySelectionPageBase
{
    /// <summary>
    /// Returns a snapshot of a single faction record from the army data service.
    /// Returns <c>null</c> when the faction is not found.
    /// </summary>
    protected ArmyFactionRecord? GetFactionSnapshotFromProvider(int factionId, CancellationToken cancellationToken = default)
    {
        return ArmyDataService.GetFactionSnapshot(factionId, cancellationToken);
    }

    /// <summary>
    /// Returns the resume (unit list) for a faction, filtered to Mercenary-eligible units only.
    /// </summary>
    protected IReadOnlyList<ArmyResumeRecord> GetResumeByFactionMercsOnlyFromProvider(int factionId, CancellationToken cancellationToken = default)
    {
        return ArmyDataService.GetResumeByFactionMercsOnly(factionId, cancellationToken);
    }

    /// <summary>
    /// Returns the full unit record for a specific unit within a faction.
    /// Returns <c>null</c> when the unit is not found.
    /// </summary>
    protected ArmyUnitRecord? GetUnitFromProvider(int factionId, int unitId, CancellationToken cancellationToken = default)
    {
        return ArmyDataService.GetUnit(factionId, unitId, cancellationToken);
    }

    /// <summary>
    /// Asynchronously retrieves the merged mercenary army list across all provided faction IDs.
    /// Used to build the combined unit pool when multiple factions are active.
    /// </summary>
    protected async Task<IReadOnlyList<MercsArmyListEntry>> GetMergedMercsArmyListFromQueryAccessorAsync(
        IReadOnlyCollection<int> factionIds,
        CancellationToken cancellationToken = default)
    {
        return await ArmyDataService.GetMergedMercsArmyListAsync(factionIds, cancellationToken);
    }
}
