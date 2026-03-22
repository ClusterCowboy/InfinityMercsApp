using InfinityMercsApp.Domain.Models.Army;
using InfinityMercsApp.Infrastructure.Repositories;
using DbCCFireteamValidity = InfinityMercsApp.Infrastructure.Models.Database.Army.CCFactionFireteamValidity;
using DbSpecopsEquipment = InfinityMercsApp.Infrastructure.Models.Database.Army.SpecopsEquipment;
using DbSpecopsSkill = InfinityMercsApp.Infrastructure.Models.Database.Army.SpecopsSkill;
using DbSpecopsUnit = InfinityMercsApp.Infrastructure.Models.Database.Army.SpecopsUnit;
using DbSpecopsWeapon = InfinityMercsApp.Infrastructure.Models.Database.Army.SpecopsWeapon;

namespace InfinityMercsApp.Infrastructure.Providers;

/// <inheritdoc/>
public sealed class SpecOpsProvider(ISQLiteRepository sqliteRepository) : ISpecOpsProvider
{
    /// <inheritdoc/>
    public Task<IReadOnlyList<SpecopsSkill>> GetSpecopsSkillsByFactionAsync(int factionId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var records = sqliteRepository
            .GetAll<DbSpecopsSkill>(x => x.FactionId == factionId, x => x.EntryOrder)
            .Select(MapSkill)
            .ToList();
        return Task.FromResult<IReadOnlyList<SpecopsSkill>>(records);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<SpecopsEquipment>> GetSpecopsEquipsByFactionAsync(int factionId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var records = sqliteRepository
            .GetAll<DbSpecopsEquipment>(x => x.FactionId == factionId, x => x.EntryOrder)
            .Select(MapEquipment)
            .ToList();
        return Task.FromResult<IReadOnlyList<SpecopsEquipment>>(records);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<SpecopsWeapon>> GetSpecopsWeaponsByFactionAsync(int factionId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var records = sqliteRepository
            .GetAll<DbSpecopsWeapon>(x => x.FactionId == factionId, x => x.EntryOrder)
            .Select(MapWeapon)
            .ToList();
        return Task.FromResult<IReadOnlyList<SpecopsWeapon>>(records);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<SpecopsUnit>> GetSpecopsUnitsByFactionAsync(int factionId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var records = sqliteRepository
            .GetAll<DbSpecopsUnit>(x => x.FactionId == factionId, x => x.EntryOrder)
            .Select(MapUnit)
            .ToList();
        return Task.FromResult<IReadOnlyList<SpecopsUnit>>(records);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<CCFactionFireteamValidityRecord>> GetCCFactionFireteamValidityAsync(
        string filterKey,
        IReadOnlyCollection<int> factionIds,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(filterKey) || factionIds.Count == 0)
        {
            return Task.FromResult<IReadOnlyList<CCFactionFireteamValidityRecord>>([]);
        }

        var rows = new List<CCFactionFireteamValidityRecord>();
        foreach (var factionId in factionIds)
        {
            var fid = factionId;
            var record = sqliteRepository.FirstOrDefault<DbCCFireteamValidity>(
                x => x.FilterKey == filterKey && x.FactionId == fid);
            if (record is not null)
            {
                rows.Add(MapFireteamValidity(record));
            }
        }

        return Task.FromResult<IReadOnlyList<CCFactionFireteamValidityRecord>>(rows);
    }

    /// <inheritdoc/>
    public Task UpsertCCFactionFireteamValidityAsync(
        int factionId,
        string filterKey,
        bool hasValidCoreFireteams,
        string? validCoreFireteamsJson,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (factionId <= 0 || string.IsNullOrWhiteSpace(filterKey))
        {
            return Task.CompletedTask;
        }

        var normalizedFilterKey = filterKey.Trim();
        var record = new DbCCFireteamValidity
        {
            CacheKey = BuildCCFactionFireteamValidityCacheKey(factionId, normalizedFilterKey),
            FactionId = factionId,
            FilterKey = normalizedFilterKey,
            HasValidCoreFireteams = hasValidCoreFireteams,
            ValidCoreFireteamsJson = string.IsNullOrWhiteSpace(validCoreFireteamsJson) ? null : validCoreFireteamsJson,
            EvaluatedAtUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        sqliteRepository.Delete<DbCCFireteamValidity>(x => x.CacheKey == record.CacheKey);
        sqliteRepository.Insert([record]);
        return Task.CompletedTask;
    }

    private static string BuildCCFactionFireteamValidityCacheKey(int factionId, string filterKey)
    {
        return $"{factionId}:{filterKey}";
    }

    private static SpecopsSkill MapSkill(DbSpecopsSkill source)
    {
        return new SpecopsSkill
        {
            SpecopsSkillKey = source.SpecopsSkillKey,
            FactionId = source.FactionId,
            EntryOrder = source.EntryOrder,
            SkillId = source.SkillId,
            Exp = source.Exp,
            ExtrasJson = source.ExtrasJson,
            EquipJson = source.EquipJson,
            WeaponsJson = source.WeaponsJson,
            RawJson = source.RawJson
        };
    }

    private static SpecopsEquipment MapEquipment(DbSpecopsEquipment source)
    {
        return new SpecopsEquipment
        {
            SpecopsEquipmentKey = source.SpecopsEquipmentKey,
            FactionId = source.FactionId,
            EntryOrder = source.EntryOrder,
            EquipmentId = source.EquipmentId,
            Exp = source.Exp,
            ExtrasJson = source.ExtrasJson,
            SkillsJson = source.SkillsJson,
            WeaponsJson = source.WeaponsJson,
            RawJson = source.RawJson
        };
    }

    private static SpecopsWeapon MapWeapon(DbSpecopsWeapon source)
    {
        return new SpecopsWeapon
        {
            SpecopsWeaponKey = source.SpecopsWeaponKey,
            FactionId = source.FactionId,
            EntryOrder = source.EntryOrder,
            WeaponId = source.WeaponId,
            Exp = source.Exp,
            RawJson = source.RawJson
        };
    }

    private static SpecopsUnit MapUnit(DbSpecopsUnit source)
    {
        return new SpecopsUnit
        {
            SpecopsUnitKey = source.SpecopsUnitKey,
            FactionId = source.FactionId,
            EntryOrder = source.EntryOrder,
            UnitId = source.UnitId,
            IdArmy = source.IdArmy,
            Canonical = source.Canonical,
            Isc = source.Isc,
            IscAbbr = source.IscAbbr,
            Name = source.Name,
            Slug = source.Slug,
            ProfileGroupsJson = source.ProfileGroupsJson,
            OptionsJson = source.OptionsJson,
            FiltersJson = source.FiltersJson,
            FactionsJson = source.FactionsJson,
            RawJson = source.RawJson
        };
    }

    private static CCFactionFireteamValidityRecord MapFireteamValidity(DbCCFireteamValidity source)
    {
        return new CCFactionFireteamValidityRecord
        {
            CacheKey = source.CacheKey,
            FactionId = source.FactionId,
            FilterKey = source.FilterKey,
            HasValidCoreFireteams = source.HasValidCoreFireteams,
            ValidCoreFireteamsJson = source.ValidCoreFireteamsJson,
            EvaluatedAtUnixSeconds = source.EvaluatedAtUnixSeconds
        };
    }
}
