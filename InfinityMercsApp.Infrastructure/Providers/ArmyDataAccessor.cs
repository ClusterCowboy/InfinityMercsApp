using InfinityMercsApp.Infrastructure.API.InfinityArmy;
using InfinityMercsApp.Infrastructure.Repositories;
using InfinityMercsApp.Infrastructure.Repositories.Models.Army;
using System.Text.Json;
using System.Text.Json.Serialization;
using Faction = InfinityMercsApp.Infrastructure.Repositories.Models.Army.Faction;
using Resume = InfinityMercsApp.Infrastructure.Repositories.Models.Army.Resume;
using Unit = InfinityMercsApp.Infrastructure.Repositories.Models.Army.Unit;

namespace InfinityMercsApp.Infrastructure.Providers;

/// <inheritdoc/>
public class ArmyDataAccessor(ISQLiteRepository sqliteRepository, IInfinityArmyAPI infinityArmyAPI) : IArmyDataAccessor
{
    private const int CharacterCategory = 10;
    private const int TagType = 4;
    private const int VehicleType = 8;
    private const string MercSlugPrefix = "merc-%";
    private const int YuJingFactionId = 201;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        Converters = { new RelaxedInt32Converter(), new RelaxedNullableInt32Converter() }
    };
    private readonly SemaphoreSlim _specopsBackfillGate = new(1, 1);
    private bool _specopsBackfillCompleted;

    public async Task ImportFactionArmyFromJsonAsync(int factionId, string json, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var document = JsonSerializer.Deserialize<API.InfinityArmy.Models.Army.Faction>(json, JsonOptions)
            ?? throw new InvalidOperationException("Failed to deserialize army JSON.");

        var faction = new Faction
        {
            FactionId = factionId,
            Version = document.Version,
            ImportedAtUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            ReinforcementsJson = ToJsonOrNull(document.Reinforcements),
            FiltersJson = ToJsonOrNull(document.Filters),
            FireteamsJson = ToJsonOrNull(document.Fireteams),
            RelationsJson = ToJsonOrNull(document.Relations),
            SpecopsJson = ToJsonOrNull(document.Specops),
            FireteamChartJson = ToJsonOrNull(document.FireteamChart),
            RawJson = json
        };

        var units = document.Units.Select(x => new Unit
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
            ProfileGroupsJson = ToJsonOrNull(x.ProfileGroups),
            OptionsJson = ToJsonOrNull(x.Options),
            FiltersJson = ToJsonOrNull(x.Filters),
            FactionsJson = ToJsonOrNull(x.Factions)
        }).ToList();

        var resume = document.Resume.Select(x => new Resume
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

        var specopsSkills = BuildSpecopsSkillRecords(factionId, document.Specops, yuJingSpecops);
        var specopsEquips = BuildSpecopsEquipRecords(factionId, document.Specops, yuJingSpecops);
        var specopsWeapons = BuildSpecopsWeaponRecords(factionId, document.Specops, yuJingSpecops);
        var specopsUnits = BuildSpecopsUnitRecords(factionId, document.Specops);

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

    public async Task ImportFactionArmyFromFileAsync(int factionId, string filePath, CancellationToken cancellationToken = default)
    {
        var json = await File.ReadAllTextAsync(filePath, cancellationToken);
        await ImportFactionArmyFromJsonAsync(factionId, json, cancellationToken);
    }

    public async Task<bool> HasFactionArmyAsync(int factionId, CancellationToken cancellationToken = default)
    {
        await EnsureSpecopsIndexedAsync(cancellationToken);
        return sqliteRepository.GetAll<Faction>(x => x.FactionId == factionId).Count() > 0;
    }

    public async Task<IReadOnlyList<int>> GetStoredFactionIdsAsync(CancellationToken cancellationToken = default)
    {
        await EnsureSpecopsIndexedAsync(cancellationToken);
        var snapshots = sqliteRepository.GetAll<Faction>(x => true, x => x.FactionId).ToList();
        return snapshots.Select(x => x.FactionId).ToList();
    }

    public async Task<Faction?> GetFactionSnapshotAsync(int factionId, CancellationToken cancellationToken = default)
    {
        await EnsureSpecopsIndexedAsync(cancellationToken);
        return sqliteRepository.GetById<Faction>(factionId);
    }

    public async Task<IReadOnlyList<Unit>> GetUnitsByFactionAsync(int factionId, CancellationToken cancellationToken = default)
    {
        await EnsureSpecopsIndexedAsync(cancellationToken);
        return sqliteRepository.GetAll<Unit>(x => x.FactionId == factionId && (x.Slug == null || !x.Slug.Contains(MercSlugPrefix)), null);
    }

    public async Task<Unit?> GetUnitAsync(int factionId, int unitId, CancellationToken cancellationToken = default)
    {
        await EnsureSpecopsIndexedAsync(cancellationToken);
        return sqliteRepository.GetAll<Unit>(x => x.FactionId == factionId && (x.Slug == null || !x.Slug.Contains(MercSlugPrefix)), null).FirstOrDefault();
    }

    public async Task<IReadOnlyList<Unit>> SearchUnitsAsync(string searchTerm, int? factionId = null, CancellationToken cancellationToken = default)
    {
        await EnsureSpecopsIndexedAsync(cancellationToken);

        var term = searchTerm?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(term))
        {
            if (factionId is not null)
            {
                return await GetUnitsByFactionAsync(factionId.Value, cancellationToken);
            }

            return sqliteRepository.GetAll<Unit>(x => x.Slug == null || !x.Slug.Contains(MercSlugPrefix)).Take(250).ToList();
        }

        if (factionId.HasValue)
        {
            return sqliteRepository.GetAll<Unit>(x => x.Name.Contains(term) && x.FactionId == factionId.Value && (x.Slug == null || !x.Slug.Contains(MercSlugPrefix)));
        }

        return sqliteRepository.GetAll<Unit>(x => x.Name.Contains(term) && (x.Slug == null || !x.Slug.Contains(MercSlugPrefix)));
    }

    public async Task<IReadOnlyList<Resume>> GetResumeByFactionAsync(int factionId, CancellationToken cancellationToken = default)
    {
        await EnsureSpecopsIndexedAsync(cancellationToken);
        return sqliteRepository.GetAll<Resume>(x => x.FactionId == factionId && (x.Slug == null || !x.Slug.Contains(MercSlugPrefix)));
    }

    public async Task<IReadOnlyList<Resume>> GetResumeByFactionMercsOnlyAsync(int factionId, CancellationToken cancellationToken = default)
    {
        await EnsureSpecopsIndexedAsync(cancellationToken);

        return sqliteRepository.GetAll<Resume>(x => x.FactionId == factionId
                                                    && (x.Slug == null || !x.Slug.Contains(MercSlugPrefix))
                                                    && (x.Category == null || x.Category != CharacterCategory)
                                                    && (x.Type == null || x.Type != TagType)
                                                    && (x.Type == null || x.Type != VehicleType),
                                                    orderBy: x => x.Name);
    }

    public async Task<IReadOnlyList<SpecopsSkill>> GetSpecopsSkillsByFactionAsync(int factionId, CancellationToken cancellationToken = default)
    {
        await EnsureSpecopsIndexedAsync(cancellationToken);
        return sqliteRepository.GetAll<SpecopsSkill>(x => x.FactionId == factionId, x => x.EntryOrder);
    }

    public async Task<IReadOnlyList<SpecopsEquipment>> GetSpecopsEquipmentByFactionAsync(int factionId, CancellationToken cancellationToken = default)
    {
        await EnsureSpecopsIndexedAsync(cancellationToken);
        return sqliteRepository.GetAll<SpecopsEquipment>(x => x.FactionId == factionId, x => x.EntryOrder);
    }

    public async Task<IReadOnlyList<SpecopsWeapon>> GetSpecopsWeaponsByFactionAsync(int factionId, CancellationToken cancellationToken = default)
    {
        await EnsureSpecopsIndexedAsync(cancellationToken);
        return sqliteRepository.GetAll<SpecopsWeapon>(x => x.FactionId == factionId, x => x.EntryOrder);
    }

    public async Task<IReadOnlyList<SpecopsUnit>> GetSpecopsUnitsByFactionAsync(int factionId, CancellationToken cancellationToken = default)
    {
        await EnsureSpecopsIndexedAsync(cancellationToken);
        return sqliteRepository.GetAll<SpecopsUnit>(x => x.FactionId == factionId, x => x.EntryOrder);
    }

    private static string BuildUnitKey(int factionId, API.InfinityArmy.Models.Army.Unit unit)
    {
        return $"{factionId}:{unit.Id}:{unit.IdArmy ?? 0}:{unit.Slug ?? string.Empty}";
    }

    private static string BuildResumeKey(int factionId, API.InfinityArmy.Models.Army.Resume resume)
    {
        return $"{factionId}:{resume.Id}:{resume.Slug ?? string.Empty}";
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

    private async Task<JsonElement> TryGetFactionSpecopsRootAsync(int factionId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var snapshot = sqliteRepository.GetById<Repositories.Models.Army.Faction>(factionId);
        if (snapshot is null || string.IsNullOrWhiteSpace(snapshot.SpecopsJson))
        {
            return default;
        }

        using var doc = JsonDocument.Parse(snapshot.SpecopsJson);
        return doc.RootElement.Clone();
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
