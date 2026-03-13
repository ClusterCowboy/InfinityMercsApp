using System.Text.Json;
using InfinityMercsApp.Data.Database;
using InfinityMercsApp.Infrastructure.Models.Database.Army;
using InfinityMercsApp.Infrastructure.Repositories;

namespace InfinityMercsApp.Services.Compatibility;

internal sealed class SpecOpsDataAccessorAdapter(ISQLiteRepository sqliteRepository) : ISpecOpsDataAccessor
{
    private const int YuJingFactionId = 201;
    private const int JsaFactionId = 1101;
    private const int JsaShindenbutaiFactionId = 1102;
    private const int JsaObanFactionId = 1103;

    public Task ReplaceFactionSpecopsAsync(int factionId, JsonElement specops, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var snapshots = sqliteRepository.GetAll<Faction>(x => true);
        var fallbackSpecops = IsJsaFaction(factionId) ? TryGetFactionSpecopsRootFromSnapshots(snapshots, YuJingFactionId) : default;

        var skills = BuildSpecopsSkillRecords(factionId, specops, fallbackSpecops);
        var equipment = BuildSpecopsEquipRecords(factionId, specops, fallbackSpecops);
        var weapons = BuildSpecopsWeaponRecords(factionId, specops, fallbackSpecops);
        var units = BuildSpecopsUnitRecords(factionId, specops);

        sqliteRepository.Delete<SpecopsSkill>(x => x.FactionId == factionId);
        sqliteRepository.Delete<SpecopsEquipment>(x => x.FactionId == factionId);
        sqliteRepository.Delete<SpecopsWeapon>(x => x.FactionId == factionId);
        sqliteRepository.Delete<SpecopsUnit>(x => x.FactionId == factionId);

        if (skills.Count > 0)
        {
            sqliteRepository.Insert(skills);
        }

        if (equipment.Count > 0)
        {
            sqliteRepository.Insert(equipment);
        }

        if (weapons.Count > 0)
        {
            sqliteRepository.Insert(weapons);
        }

        if (units.Count > 0)
        {
            sqliteRepository.Insert(units);
        }

        return Task.CompletedTask;
    }

    public Task EnsureSpecopsIndexedAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ArmySpecopsSkillRecord>> GetSpecopsSkillsByFactionAsync(int factionId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var rows = sqliteRepository
            .GetAll<SpecopsSkill>(x => x.FactionId == factionId, x => x.EntryOrder)
            .Select(x => new ArmySpecopsSkillRecord
            {
                SpecopsSkillKey = x.SpecopsSkillKey,
                FactionId = x.FactionId,
                EntryOrder = x.EntryOrder,
                SkillId = x.SkillId,
                Exp = x.Exp,
                ExtrasJson = x.ExtrasJson,
                EquipJson = x.EquipJson,
                WeaponsJson = x.WeaponsJson,
                RawJson = x.RawJson
            })
            .ToList();

        return Task.FromResult<IReadOnlyList<ArmySpecopsSkillRecord>>(rows);
    }

    public Task<IReadOnlyList<ArmySpecopsEquipRecord>> GetSpecopsEquipsByFactionAsync(int factionId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var rows = sqliteRepository
            .GetAll<SpecopsEquipment>(x => x.FactionId == factionId, x => x.EntryOrder)
            .Select(x => new ArmySpecopsEquipRecord
            {
                SpecopsEquipKey = x.SpecopsEquipmentKey,
                FactionId = x.FactionId,
                EntryOrder = x.EntryOrder,
                EquipId = x.EquipmentId,
                Exp = x.Exp,
                ExtrasJson = x.ExtrasJson,
                SkillsJson = x.SkillsJson,
                WeaponsJson = x.WeaponsJson,
                RawJson = x.RawJson
            })
            .ToList();

        return Task.FromResult<IReadOnlyList<ArmySpecopsEquipRecord>>(rows);
    }

    public Task<IReadOnlyList<ArmySpecopsWeaponRecord>> GetSpecopsWeaponsByFactionAsync(int factionId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var rows = sqliteRepository
            .GetAll<SpecopsWeapon>(x => x.FactionId == factionId, x => x.EntryOrder)
            .Select(x => new ArmySpecopsWeaponRecord
            {
                SpecopsWeaponKey = x.SpecopsWeaponKey,
                FactionId = x.FactionId,
                EntryOrder = x.EntryOrder,
                WeaponId = x.WeaponId,
                Exp = x.Exp,
                RawJson = x.RawJson
            })
            .ToList();

        return Task.FromResult<IReadOnlyList<ArmySpecopsWeaponRecord>>(rows);
    }

    public Task<IReadOnlyList<ArmySpecopsUnitRecord>> GetSpecopsUnitsByFactionAsync(int factionId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var rows = sqliteRepository
            .GetAll<SpecopsUnit>(x => x.FactionId == factionId, x => x.EntryOrder)
            .Select(x => new ArmySpecopsUnitRecord
            {
                SpecopsUnitKey = x.SpecopsUnitKey,
                FactionId = x.FactionId,
                EntryOrder = x.EntryOrder,
                UnitId = x.UnitId,
                IdArmy = x.IdArmy,
                Canonical = x.Canonical,
                Isc = x.Isc,
                IscAbbr = x.IscAbbr,
                Name = x.Name,
                Slug = x.Slug,
                ProfileGroupsJson = x.ProfileGroupsJson,
                OptionsJson = x.OptionsJson,
                FiltersJson = x.FiltersJson,
                FactionsJson = x.FactionsJson,
                RawJson = x.RawJson
            })
            .ToList();

        return Task.FromResult<IReadOnlyList<ArmySpecopsUnitRecord>>(rows);
    }

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

        var ids = factionIds.ToHashSet();
        var rows = sqliteRepository
            .GetAll<CCFactionFireteamValidityRecord>(x => x.FilterKey == filterKey)
            .Where(x => ids.Contains(x.FactionId))
            .ToList();
        return Task.FromResult<IReadOnlyList<CCFactionFireteamValidityRecord>>(rows);
    }

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
        var record = new CCFactionFireteamValidityRecord
        {
            CacheKey = BuildCCFactionFireteamValidityCacheKey(factionId, normalizedFilterKey),
            FactionId = factionId,
            FilterKey = normalizedFilterKey,
            HasValidCoreFireteams = hasValidCoreFireteams,
            ValidCoreFireteamsJson = string.IsNullOrWhiteSpace(validCoreFireteamsJson) ? null : validCoreFireteamsJson,
            EvaluatedAtUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        sqliteRepository.Delete<CCFactionFireteamValidityRecord>(x => x.CacheKey == record.CacheKey);
        sqliteRepository.Insert([record]);
        return Task.CompletedTask;
    }

    private static string BuildCCFactionFireteamValidityCacheKey(int factionId, string filterKey)
    {
        return $"{factionId}:{filterKey}";
    }

    private static bool IsJsaFaction(int factionId)
    {
        return factionId is JsaFactionId or JsaShindenbutaiFactionId or JsaObanFactionId;
    }

    private static JsonElement TryGetFactionSpecopsRootFromSnapshots(IEnumerable<Faction> snapshots, int factionId)
    {
        var snapshot = snapshots.FirstOrDefault(x => x.FactionId == factionId);
        if (snapshot is null || string.IsNullOrWhiteSpace(snapshot.SpecopsJson))
        {
            return default;
        }

        using var doc = JsonDocument.Parse(snapshot.SpecopsJson);
        return doc.RootElement.Clone();
    }

    private static List<SpecopsSkill> BuildSpecopsSkillRecords(int factionId, JsonElement specops, JsonElement fallbackSpecops = default)
    {
        var records = new List<SpecopsSkill>();
        if (!TryGetSpecopsArray(specops, fallbackSpecops, "skills", out var array))
        {
            return records;
        }

        var entryOrder = 0;
        foreach (var entry in array.EnumerateArray())
        {
            if (!TryReadIntProperty(entry, "id", out var skillId))
            {
                continue;
            }

            var exp = TryReadIntProperty(entry, "exp", out var parsedExp) ? parsedExp : 0;
            records.Add(new SpecopsSkill
            {
                SpecopsSkillKey = $"{factionId}:{entryOrder}:{skillId}:{exp}",
                FactionId = factionId,
                EntryOrder = entryOrder,
                SkillId = skillId,
                Exp = exp,
                ExtrasJson = TryReadJsonProperty(entry, "extras"),
                EquipJson = TryReadJsonProperty(entry, "equip"),
                WeaponsJson = TryReadJsonProperty(entry, "weapons"),
                RawJson = entry.GetRawText()
            });

            entryOrder++;
        }

        return records;
    }

    private static List<SpecopsEquipment> BuildSpecopsEquipRecords(int factionId, JsonElement specops, JsonElement fallbackSpecops = default)
    {
        var records = new List<SpecopsEquipment>();
        if (!TryGetSpecopsArray(specops, fallbackSpecops, "equip", out var array))
        {
            return records;
        }

        var entryOrder = 0;
        foreach (var entry in array.EnumerateArray())
        {
            if (!TryReadIntProperty(entry, "id", out var equipId))
            {
                continue;
            }

            var exp = TryReadIntProperty(entry, "exp", out var parsedExp) ? parsedExp : 0;
            records.Add(new SpecopsEquipment
            {
                SpecopsEquipmentKey = $"{factionId}:{entryOrder}:{equipId}:{exp}",
                FactionId = factionId,
                EntryOrder = entryOrder,
                EquipmentId = equipId,
                Exp = exp,
                ExtrasJson = TryReadJsonProperty(entry, "extras"),
                SkillsJson = TryReadJsonProperty(entry, "skills"),
                WeaponsJson = TryReadJsonProperty(entry, "weapons"),
                RawJson = entry.GetRawText()
            });

            entryOrder++;
        }

        return records;
    }

    private static List<SpecopsWeapon> BuildSpecopsWeaponRecords(int factionId, JsonElement specops, JsonElement fallbackSpecops = default)
    {
        var records = new List<SpecopsWeapon>();
        if (!TryGetSpecopsArray(specops, fallbackSpecops, "weapons", out var array))
        {
            return records;
        }

        var entryOrder = 0;
        foreach (var entry in array.EnumerateArray())
        {
            if (!TryReadIntProperty(entry, "id", out var weaponId))
            {
                continue;
            }

            var exp = TryReadIntProperty(entry, "exp", out var parsedExp) ? parsedExp : 0;
            records.Add(new SpecopsWeapon
            {
                SpecopsWeaponKey = $"{factionId}:{entryOrder}:{weaponId}:{exp}",
                FactionId = factionId,
                EntryOrder = entryOrder,
                WeaponId = weaponId,
                Exp = exp,
                RawJson = entry.GetRawText()
            });

            entryOrder++;
        }

        return records;
    }

    private static List<SpecopsUnit> BuildSpecopsUnitRecords(int factionId, JsonElement specops)
    {
        var records = new List<SpecopsUnit>();
        if (!TryGetSpecopsArray(specops, "units", out var array))
        {
            return records;
        }

        var entryOrder = 0;
        foreach (var entry in array.EnumerateArray())
        {
            if (!TryReadIntProperty(entry, "id", out var unitId))
            {
                continue;
            }

            records.Add(new SpecopsUnit
            {
                SpecopsUnitKey = $"{factionId}:{entryOrder}:{unitId}",
                FactionId = factionId,
                EntryOrder = entryOrder,
                UnitId = unitId,
                IdArmy = TryReadNullableIntProperty(entry, "idArmy"),
                Canonical = TryReadNullableIntProperty(entry, "canonical"),
                Isc = TryReadStringProperty(entry, "isc"),
                IscAbbr = TryReadStringProperty(entry, "iscAbbr"),
                Name = TryReadStringProperty(entry, "name") ?? string.Empty,
                Slug = TryReadStringProperty(entry, "slug"),
                ProfileGroupsJson = TryReadJsonProperty(entry, "profileGroups"),
                OptionsJson = TryReadJsonProperty(entry, "options"),
                FiltersJson = TryReadJsonProperty(entry, "filters"),
                FactionsJson = TryReadJsonProperty(entry, "factions"),
                RawJson = entry.GetRawText()
            });

            entryOrder++;
        }

        return records;
    }

    private static bool TryGetSpecopsArray(JsonElement specops, string propertyName, out JsonElement array)
    {
        array = default;
        return specops.ValueKind == JsonValueKind.Object &&
               specops.TryGetProperty(propertyName, out array) &&
               array.ValueKind == JsonValueKind.Array;
    }

    private static bool TryGetSpecopsArray(JsonElement specops, JsonElement fallbackSpecops, string propertyName, out JsonElement array)
    {
        if (TryGetSpecopsArray(specops, propertyName, out array))
        {
            return true;
        }

        return TryGetSpecopsArray(fallbackSpecops, propertyName, out array);
    }

    private static bool TryReadIntProperty(JsonElement container, string propertyName, out int value)
    {
        value = default;
        if (container.ValueKind != JsonValueKind.Object ||
            !container.TryGetProperty(propertyName, out var propertyElement))
        {
            return false;
        }

        return TryReadInt(propertyElement, out value);
    }

    private static int? TryReadNullableIntProperty(JsonElement container, string propertyName)
    {
        if (!TryReadIntProperty(container, propertyName, out var value))
        {
            return null;
        }

        return value;
    }

    private static bool TryReadInt(JsonElement element, out int value)
    {
        value = default;
        if (element.ValueKind == JsonValueKind.Number)
        {
            return element.TryGetInt32(out value);
        }

        if (element.ValueKind == JsonValueKind.String)
        {
            return int.TryParse(element.GetString(), out value);
        }

        return false;
    }

    private static string? TryReadStringProperty(JsonElement container, string propertyName)
    {
        if (container.ValueKind != JsonValueKind.Object ||
            !container.TryGetProperty(propertyName, out var propertyElement) ||
            propertyElement.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return propertyElement.GetString();
    }

    private static string? TryReadJsonProperty(JsonElement container, string propertyName)
    {
        if (container.ValueKind != JsonValueKind.Object ||
            !container.TryGetProperty(propertyName, out var propertyElement))
        {
            return null;
        }

        return ToJsonOrNull(propertyElement);
    }

    private static string? ToJsonOrNull(JsonElement element)
    {
        return element.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null
            ? null
            : element.GetRawText();
    }
}
