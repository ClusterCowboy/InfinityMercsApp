using InfinityMercsApp.Infrastructure.Models.Database.Army;
using System.Text.Json;

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
    IReadOnlyList<SpecopsSkill> GetSpecopsSkillsByFaction(int factionId);

    /// <summary>
    /// Gets all SpecOps equipment for a faction.
    /// </summary>
    /// <param name="factionId"></param>
    /// <returns></returns>
    IReadOnlyList<SpecopsEquipment> GetSpecopsEquipmentByFaction(int factionId);

    /// <summary>
    /// Gets all SpecOps weapons for a faction.
    /// </summary>
    /// <param name="factionId"></param>
    /// <returns></returns>
    IReadOnlyList<SpecopsWeapon> GetSpecopsWeaponsByFaction(int factionId);

    /// <summary>
    /// Gets all SpecOps units for a faction.
    /// </summary>
    /// <param name="factionId"></param>
    /// <returns></returns>
    IReadOnlyList<SpecopsUnit> GetSpecopsUnitsByFaction(int factionId);

    /// <summary>
    /// Replaces SpecOps entries for a faction.
    /// </summary>
    /// <param name="factionId"></param>
    /// <param name="specops"></param>
    /// <returns></returns>
    void ReplaceFactionSpecops(int factionId, JsonElement specops);

    /// <summary>
    /// Ensures that SpecOps records are saved locally.
    /// </summary>
    /// <returns></returns>
    void EnsureSpecopsIndexed();

    /// <summary>
    /// Gets fireteam information for a cohesive company.
    /// </summary>
    /// <param name="filterKey"></param>
    /// <param name="factionIds"></param>
    /// <returns></returns>
    IReadOnlyList<CohesiveCompanyFireteam> GetCohesiveCompanyFireteams(
        string filterKey,
        IReadOnlyCollection<int> factionIds);

    /// <summary>
    /// Stores information about fireteams for a cohesive company.
    /// </summary>
    /// <param name="factionId"></param>
    /// <param name="filterKey"></param>
    /// <param name="hasValidCoreFireteams"></param>
    /// <param name="validCoreFireteamsJson"></param>
    /// <returns></returns>
    void UpsertCohesiveCompanyFireteams(
        int factionId,
        string filterKey,
        bool hasValidCoreFireteams,
        string? validCoreFireteamsJson);
}
