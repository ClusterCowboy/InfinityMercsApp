using InfinityMercsApp.Infrastructure.Models.Database.Army;

namespace InfinityMercsApp.Infrastructure.Providers;

/// <summary>
/// An interface to handle interactions with SpecOps
/// </summary>
internal interface ISpecOpsProvider
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
}
