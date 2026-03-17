using ArmyFactionRecord = InfinityMercsApp.Domain.Models.Army.Faction;
using ArmyResumeRecord = InfinityMercsApp.Domain.Models.Army.Resume;
using ArmyUnitRecord = InfinityMercsApp.Domain.Models.Army.Unit;
using MercsArmyListEntry = InfinityMercsApp.Domain.Models.Army.MercsArmyListEntry;

namespace InfinityMercsApp.Views.Common;

public abstract partial class CompanySelectionPageBase
{
    protected ArmyFactionRecord? GetFactionSnapshotFromProvider(int factionId, CancellationToken cancellationToken = default)
    {
        return ArmyDataService.GetFactionSnapshot(factionId, cancellationToken);
    }

    protected IReadOnlyList<ArmyResumeRecord> GetResumeByFactionMercsOnlyFromProvider(int factionId, CancellationToken cancellationToken = default)
    {
        return ArmyDataService.GetResumeByFactionMercsOnly(factionId, cancellationToken);
    }

    protected ArmyUnitRecord? GetUnitFromProvider(int factionId, int unitId, CancellationToken cancellationToken = default)
    {
        return ArmyDataService.GetUnit(factionId, unitId, cancellationToken);
    }

    protected async Task<IReadOnlyList<MercsArmyListEntry>> GetMergedMercsArmyListFromQueryAccessorAsync(
        IReadOnlyCollection<int> factionIds,
        CancellationToken cancellationToken = default)
    {
        return await ArmyDataService.GetMergedMercsArmyListAsync(factionIds, cancellationToken);
    }
}
