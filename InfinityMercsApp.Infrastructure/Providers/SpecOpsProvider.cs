using InfinityMercsApp.Infrastructure.Models.Database.Army;
using InfinityMercsApp.Infrastructure.Repositories;
using System.Data.Common;
using System.Text.Json;

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

    /// <inheritdoc/>
    public void ReplaceFactionSpecops(int factionId, JsonElement specops)
    {
        // Not sure if this is needed. Shouldn't this be handled in the ArmyImportProvider?
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public void EnsureSpecopsIndexed()
    {
        // Not sure if this is needed. Shouldn't this be handled in the ArmyImportProvider?
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public IReadOnlyList<CohesiveCompanyFireteam> GetCohesiveCompanyFireteams(string filterKey, IReadOnlyCollection<int> factionIds)
    {
        if (string.IsNullOrWhiteSpace(filterKey) || factionIds.Count == 0)
        {
            return [];
        }

        var rows = sqliteRepository.GetAll<CohesiveCompanyFireteam>(x => x.FilterKey == filterKey);

        var ids = new HashSet<int>(factionIds);
        return rows.Where(x => ids.Contains(x.FactionId)).ToList();
    }

    /// <inheritdoc/>
    public void UpsertCohesiveCompanyFireteams(int factionId, string filterKey, bool hasValidCoreFireteams, string? validCoreFireteamsJson)
    {
        if (factionId <= 0 || string.IsNullOrWhiteSpace(filterKey))
        {
            return;
        }

        var normalizedFilterKey = filterKey.Trim();
        var record = new CohesiveCompanyFireteam
        {
            CacheKey = $"{factionId}:{filterKey}",
            FactionId = factionId,
            FilterKey = normalizedFilterKey,
            HasValidCoreFireteams = hasValidCoreFireteams,
            ValidCoreFireteamsJson = string.IsNullOrWhiteSpace(validCoreFireteamsJson) ? null : validCoreFireteamsJson,
            EvaluatedAtUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        sqliteRepository.Upsert(record);
    }
}
