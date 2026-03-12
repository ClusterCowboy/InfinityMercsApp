using System.Text.Json;

namespace InfinityMercsApp.Data.Database;

/// <summary>
/// Dedicated accessor for Spec-Ops indexing/querying and cohesive-company fireteam validity cache rows.
/// </summary>
public interface ISpecOpsDataAccessor
{
    Task ReplaceFactionSpecopsAsync(int factionId, JsonElement specops, CancellationToken cancellationToken = default);

    Task EnsureSpecopsIndexedAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ArmySpecopsSkillRecord>> GetSpecopsSkillsByFactionAsync(int factionId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ArmySpecopsEquipRecord>> GetSpecopsEquipsByFactionAsync(int factionId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ArmySpecopsWeaponRecord>> GetSpecopsWeaponsByFactionAsync(int factionId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ArmySpecopsUnitRecord>> GetSpecopsUnitsByFactionAsync(int factionId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CCFactionFireteamValidityRecord>> GetCCFactionFireteamValidityAsync(
        string filterKey,
        IReadOnlyCollection<int> factionIds,
        CancellationToken cancellationToken = default);

    Task UpsertCCFactionFireteamValidityAsync(
        int factionId,
        string filterKey,
        bool hasValidCoreFireteams,
        string? validCoreFireteamsJson,
        CancellationToken cancellationToken = default);
}
