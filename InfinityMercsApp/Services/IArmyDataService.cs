using ArmyFactionRecord = InfinityMercsApp.Domain.Models.Army.Faction;
using ArmyResumeRecord = InfinityMercsApp.Domain.Models.Army.Resume;
using ArmyUnitRecord = InfinityMercsApp.Domain.Models.Army.Unit;
using FactionRecord = InfinityMercsApp.Domain.Models.Metadata.Faction;
using MercsArmyListEntry = InfinityMercsApp.Domain.Models.Army.MercsArmyListEntry;
using System.Text.Json;

namespace InfinityMercsApp.Services;

/// <summary>
/// Shared backend facade for metadata/army read operations used by UI screens.
/// </summary>
public interface IArmyDataService
{
    IReadOnlyList<FactionRecord> GetMetadataFactions(bool includeDiscontinued = false, CancellationToken cancellationToken = default);
    FactionRecord? GetMetadataFactionById(int id, CancellationToken cancellationToken = default);
    ArmyFactionRecord? GetFactionSnapshot(int factionId, CancellationToken cancellationToken = default);
    IReadOnlyList<ArmyResumeRecord> GetResumeByFaction(int factionId, CancellationToken cancellationToken = default);
    IReadOnlyList<ArmyResumeRecord> GetResumeByFactionMercsOnly(int factionId, CancellationToken cancellationToken = default);
    ArmyUnitRecord? GetUnit(int factionId, int unitId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MercsArmyListEntry>> GetMergedMercsArmyListAsync(IReadOnlyCollection<int> factionIds, CancellationToken cancellationToken = default);
    (int? FirstCm, int? SecondCm, string DisplayValue) ReadMoveValue(JsonElement element);
    string FormatMoveValue(int? firstCm, int? secondCm);
}
