using DomainArmyImportFaction = InfinityMercsApp.Domain.Models.Army.ArmyImportFaction;
using DomainArmyImportResume = InfinityMercsApp.Domain.Models.Army.ArmyImportResume;
using DomainArmyImportUnit = InfinityMercsApp.Domain.Models.Army.ArmyImportUnit;
using InfinityMercsApp.Infrastructure.Models.Database.Army;
using InfinityMercsApp.Infrastructure.Repositories;
using System.Text.Json;
using Faction = InfinityMercsApp.Infrastructure.Models.Database.Army.Faction;
using Resume = InfinityMercsApp.Infrastructure.Models.Database.Army.Resume;
using Unit = InfinityMercsApp.Infrastructure.Models.Database.Army.Unit;

namespace InfinityMercsApp.Infrastructure.Providers;

/// <inheritdoc/>
public class ArmyImportProvider(ISQLiteRepository sqliteRepository) : IArmyImportProvider
{
    private const int YuJingFactionId = 201;
    private readonly SemaphoreSlim _specopsBackfillGate = new(1, 1);
    private bool _specopsBackfillCompleted;

    /// <inheritdoc/>
    public async Task ImportAsync(int factionId, DomainArmyImportFaction apiFaction, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var faction = new Faction
        {
            FactionId = factionId,
            Version = apiFaction.Version,
            ImportedAtUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            ReinforcementsJson = apiFaction.ReinforcementsJson,
            FiltersJson = apiFaction.FiltersJson,
            FireteamsJson = apiFaction.FireteamsJson,
            RelationsJson = apiFaction.RelationsJson,
            SpecopsJson = apiFaction.SpecopsJson,
            FireteamChartJson = apiFaction.FireteamChartJson,
            RawJson = apiFaction.RawJson
        };

        var units = apiFaction.Units.Select(x => new Unit
        {
            UnitKey = BuildUnitKey(factionId, x),
            FactionId = factionId,
            UnitId = x.Id,
            IdArmy = x.IdArmy,
            Canonical = x.Canonical,
            Isc = x.Isc,
            IscAbbr = x.IscAbbr,
            Name = x.Name,
            Slug = x.Slug,
            ProfileGroupsJson = x.ProfileGroupsJson,
            OptionsJson = x.OptionsJson,
            FiltersJson = x.FiltersJson,
            FactionsJson = x.FactionsJson
        }).ToList();

        var resume = apiFaction.Resume.Select(x => new Resume
        {
            ResumeKey = BuildResumeKey(factionId, x),
            FactionId = factionId,
            UnitId = x.Id,
            IdArmy = x.IdArmy,
            Isc = x.Isc,
            Name = x.Name,
            Slug = x.Slug,
            Logo = x.Logo,
            Type = x.Type,
            Category = x.Category
        }).ToList();

        var yuJingSpecops = faction.IsJsaFaction
            ? await TryGetFactionSpecopsRootAsync(YuJingFactionId, cancellationToken)
            : default;

        var specopsRoot = ParseJsonElement(apiFaction.SpecopsJson);
        var specopsSkills = BuildSpecopsSkillRecords(factionId, specopsRoot, yuJingSpecops);
        var specopsEquips = BuildSpecopsEquipRecords(factionId, specopsRoot, yuJingSpecops);
        var specopsWeapons = BuildSpecopsWeaponRecords(factionId, specopsRoot, yuJingSpecops);
        var specopsUnits = BuildSpecopsUnitRecords(factionId, specopsRoot);

        sqliteRepository.Delete<Unit>(x => x.FactionId == factionId);
        sqliteRepository.Delete<Resume>(x => x.FactionId == factionId);
        sqliteRepository.Delete<SpecopsSkill>(x => x.FactionId == factionId);
        sqliteRepository.Delete<SpecopsEquipment>(x => x.FactionId == factionId);
        sqliteRepository.Delete<SpecopsWeapon>(x => x.FactionId == factionId);
        sqliteRepository.Delete<SpecopsUnit>(x => x.FactionId == factionId);
        sqliteRepository.Delete<Faction>(x => x.FactionId == factionId);

        sqliteRepository.Insert([faction]);
        sqliteRepository.Insert(units);
        sqliteRepository.Insert(resume);
        sqliteRepository.Insert(specopsSkills);
        sqliteRepository.Insert(specopsEquips);
        sqliteRepository.Insert(specopsWeapons);
        sqliteRepository.Insert(specopsUnits);
    }

    private static string BuildUnitKey(int factionId, DomainArmyImportUnit unit)
    {
        return $"{factionId}:{unit.Id}:{unit.IdArmy ?? 0}:{unit.Slug ?? string.Empty}";
    }

    private static string BuildResumeKey(int factionId, DomainArmyImportResume resume)
    {
        return $"{factionId}:{resume.Id}:{resume.Slug ?? string.Empty}";
    }

    private static JsonElement ParseJsonElement(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return default;
        }

        try
        {
            using var jsonDocument = JsonDocument.Parse(json);
            return jsonDocument.RootElement.Clone();
        }
        catch
        {
            return default;
        }
    }

    private async Task EnsureSpecopsIndexedAsync(CancellationToken cancellationToken)
    {
        if (_specopsBackfillCompleted)
        {
            return;
        }

        await _specopsBackfillGate.WaitAsync(cancellationToken);
        try
        {
            if (_specopsBackfillCompleted)
            {
                return;
            }

            var snapshots = sqliteRepository.GetAll<Faction>(x => true);
            var yuJingSpecops = TryGetFactionSpecopsRootFromSnapshots(snapshots, YuJingFactionId);
            foreach (var snapshot in snapshots)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (string.IsNullOrWhiteSpace(snapshot.SpecopsJson))
                {
                    continue;
                }

                var existingSkills = sqliteRepository.GetAll<SpecopsSkill>(x => true).Count();
                var existingEquipment = sqliteRepository.GetAll<SpecopsSkill>(x => true).Count();
                var existingWeapons = sqliteRepository.GetAll<SpecopsSkill>(x => true).Count();
                var existingUnits = sqliteRepository.GetAll<SpecopsSkill>(x => true).Count();

                var existingCount = existingSkills + existingEquipment + existingWeapons + existingUnits;

                var fallbackSpecops = snapshot.IsJsaFaction ? yuJingSpecops : default;
                var shouldForceReindexForJsa = snapshot.IsJsaFaction && HasAnySpecopsArray(fallbackSpecops);

                if (existingCount > 0 && !shouldForceReindexForJsa)
                {
                    continue;
                }

                using var specopsDoc = JsonDocument.Parse(snapshot.SpecopsJson);
                var specopsRoot = specopsDoc.RootElement;
                var skillRows = BuildSpecopsSkillRecords(snapshot.FactionId, specopsRoot, fallbackSpecops);
                var equipRows = BuildSpecopsEquipRecords(snapshot.FactionId, specopsRoot, fallbackSpecops);
                var weaponRows = BuildSpecopsWeaponRecords(snapshot.FactionId, specopsRoot, fallbackSpecops);
                var unitRows = BuildSpecopsUnitRecords(snapshot.FactionId, specopsRoot);

                if (existingCount > 0)
                {
                    sqliteRepository.Delete<SpecopsSkill>(x => x.FactionId == snapshot.FactionId);
                    sqliteRepository.Delete<SpecopsEquipment>(x => x.FactionId == snapshot.FactionId);
                    sqliteRepository.Delete<SpecopsWeapon>(x => x.FactionId == snapshot.FactionId);
                    sqliteRepository.Delete<SpecopsUnit>(x => x.FactionId == snapshot.FactionId);
                }

                sqliteRepository.Insert(skillRows);
                sqliteRepository.Insert(equipRows);
                sqliteRepository.Insert(weaponRows);
                sqliteRepository.Insert(unitRows);
            }

            _specopsBackfillCompleted = true;
        }
        finally
        {
            _specopsBackfillGate.Release();
        }
    }

    private Task<JsonElement> TryGetFactionSpecopsRootAsync(int factionId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var snapshot = sqliteRepository.GetById<Faction>(factionId);
        if (snapshot is null || string.IsNullOrWhiteSpace(snapshot.SpecopsJson))
        {
            return Task.FromResult(default(JsonElement));
        }

        using var doc = JsonDocument.Parse(snapshot.SpecopsJson);
        return Task.FromResult(doc.RootElement.Clone());
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

    private static bool HasAnySpecopsArray(JsonElement specops)
    {
        return TryGetSpecopsArray(specops, "skills", out _) ||
               TryGetSpecopsArray(specops, "equip", out _) ||
               TryGetSpecopsArray(specops, "weapons", out _);
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
