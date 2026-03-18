using InfinityMercsApp.Domain.Models.Army;

namespace InfinityMercsApp.Infrastructure.Providers;

/// <summary>
/// An interface to handle interactions with SpecOps
/// </summary>
public interface ISpecOpsProvider
{
    /// <summary>
    /// Gets all SpecOps skills for a faction.
    /// </summary>
    /// <param name="factionId"></param>
    /// <returns></returns>
    Task<IReadOnlyList<SpecopsSkill>> GetSpecopsSkillsByFactionAsync(int factionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all SpecOps equipment for a faction.
    /// </summary>
    /// <param name="factionId"></param>
    /// <returns></returns>
    Task<IReadOnlyList<SpecopsEquipment>> GetSpecopsEquipsByFactionAsync(int factionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all SpecOps weapons for a faction.
    /// </summary>
    /// <param name="factionId"></param>
    /// <returns></returns>
    Task<IReadOnlyList<SpecopsWeapon>> GetSpecopsWeaponsByFactionAsync(int factionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all SpecOps units for a faction.
    /// </summary>
    /// <param name="factionId"></param>
    /// <returns></returns>
    Task<IReadOnlyList<SpecopsUnit>> GetSpecopsUnitsByFactionAsync(int factionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets cached cohesive-company fireteam validity rows for one filter key and faction set.
    /// </summary>
    /// <param name="filterKey"></param>
    /// <param name="factionIds"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<IReadOnlyList<CCFactionFireteamValidityRecord>> GetCCFactionFireteamValidityAsync(
        string filterKey,
        IReadOnlyCollection<int> factionIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Upserts a cohesive-company fireteam validity cache row.
    /// </summary>
    /// <param name="factionId"></param>
    /// <param name="filterKey"></param>
    /// <param name="hasValidCoreFireteams"></param>
    /// <param name="validCoreFireteamsJson"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task UpsertCCFactionFireteamValidityAsync(
        int factionId,
        string filterKey,
        bool hasValidCoreFireteams,
        string? validCoreFireteamsJson,
        CancellationToken cancellationToken = default);
}

