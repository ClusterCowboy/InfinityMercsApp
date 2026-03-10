using InfinityMercsApp.Infrastructure.Models.Database.Army;
using InfinityMercsApp.Infrastructure.Repositories;

namespace InfinityMercsApp.Infrastructure.Providers;

/// <inheritdoc/>
public sealed class SpecOpsProvider(ISQLiteRepository sqliteRepository) : ISpecOpsProvider
{
    /// <inheritdoc/>
    public IReadOnlyList<SpecopsSkill> GetSpecopsSkillsByFaction(int factionId)
    {
        return sqliteRepository.GetAll<SpecopsSkill>(x => x.FactionId == factionId, x => x.EntryOrder);
    }

    /// <inheritdoc/>
    public IReadOnlyList<SpecopsEquipment> GetSpecopsEquipmentByFaction(int factionId)
    {
        return sqliteRepository.GetAll<SpecopsEquipment>(x => x.FactionId == factionId, x => x.EntryOrder);
    }

    /// <inheritdoc/>
    public IReadOnlyList<SpecopsWeapon> GetSpecopsWeaponsByFaction(int factionId)
    {
        return sqliteRepository.GetAll<SpecopsWeapon>(x => x.FactionId == factionId, x => x.EntryOrder);
    }

    /// <inheritdoc/>
    public IReadOnlyList<SpecopsUnit> GetSpecopsUnitsByFaction(int factionId)
    {
        return sqliteRepository.GetAll<SpecopsUnit>(x => x.FactionId == factionId, x => x.EntryOrder);
    }
}
