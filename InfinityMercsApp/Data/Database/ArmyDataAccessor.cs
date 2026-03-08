using System.Text.Json;
using System.Text.Json.Serialization;
using SQLite;

namespace InfinityMercsApp.Data.Database;

public class ArmyDataAccessor : IArmyDataAccessor
{
    private const int CharacterCategory = 10;
    private const int TagType = 4;
    private const int VehicleType = 8;
    private const string MercSlugPrefix = "merc-%";
    private const int YuJingFactionId = 201;
    private const int JsaFactionId = 1101;
    private const int JsaShindenbutaiFactionId = 1102;
    private const int JsaObanFactionId = 1103;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        Converters = { new RelaxedInt32Converter(), new RelaxedNullableInt32Converter() }
    };

    private readonly IDatabaseContext _databaseContext;
    private readonly SQLiteAsyncConnection _connection;
    private readonly SemaphoreSlim _specopsBackfillGate = new(1, 1);
    private bool _specopsBackfillCompleted;

    public ArmyDataAccessor(IDatabaseContext databaseContext)
    {
        _databaseContext = databaseContext;
        _connection = databaseContext.Connection;
    }

    public async Task ImportFactionArmyFromJsonAsync(int factionId, string json, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _databaseContext.InitializeAsync(cancellationToken);

        var document = JsonSerializer.Deserialize<ArmyDocument>(json, JsonOptions)
            ?? throw new InvalidOperationException("Failed to deserialize army JSON.");

        var snapshot = new ArmyFactionRecord
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

        var units = document.Units.Select(x => new ArmyUnitRecord
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

        var resume = document.Resume.Select(x => new ArmyResumeRecord
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

        var yuJingSpecops = IsJsaFaction(factionId)
            ? await TryGetFactionSpecopsRootAsync(YuJingFactionId, cancellationToken)
            : default;

        var specopsSkills = BuildSpecopsSkillRecords(factionId, document.Specops, yuJingSpecops);
        var specopsEquips = BuildSpecopsEquipRecords(factionId, document.Specops, yuJingSpecops);
        var specopsWeapons = BuildSpecopsWeaponRecords(factionId, document.Specops, yuJingSpecops);
        var specopsUnits = BuildSpecopsUnitRecords(factionId, document.Specops);

        await _connection.ExecuteAsync("DELETE FROM army_units WHERE FactionId = ?", factionId);
        await _connection.ExecuteAsync("DELETE FROM army_resume WHERE FactionId = ?", factionId);
        await _connection.ExecuteAsync("DELETE FROM army_specops_skills WHERE FactionId = ?", factionId);
        await _connection.ExecuteAsync("DELETE FROM army_specops_equips WHERE FactionId = ?", factionId);
        await _connection.ExecuteAsync("DELETE FROM army_specops_weapons WHERE FactionId = ?", factionId);
        await _connection.ExecuteAsync("DELETE FROM army_specops_units WHERE FactionId = ?", factionId);
        await _connection.ExecuteAsync("DELETE FROM cc_faction_fireteam_validity WHERE FactionId = ?", factionId);
        await _connection.DeleteAsync<ArmyFactionRecord>(factionId);

        await _connection.InsertAsync(snapshot);

        if (units.Count > 0)
        {
            await _connection.InsertAllAsync(units);
        }

        if (resume.Count > 0)
        {
            await _connection.InsertAllAsync(resume);
        }

        if (specopsSkills.Count > 0)
        {
            await _connection.InsertAllAsync(specopsSkills);
        }

        if (specopsEquips.Count > 0)
        {
            await _connection.InsertAllAsync(specopsEquips);
        }

        if (specopsWeapons.Count > 0)
        {
            await _connection.InsertAllAsync(specopsWeapons);
        }

        if (specopsUnits.Count > 0)
        {
            await _connection.InsertAllAsync(specopsUnits);
        }
    }

    public async Task ImportFactionArmyFromFileAsync(int factionId, string filePath, CancellationToken cancellationToken = default)
    {
        var json = await File.ReadAllTextAsync(filePath, cancellationToken);
        await ImportFactionArmyFromJsonAsync(factionId, json, cancellationToken);
    }

    public async Task<bool> HasFactionArmyAsync(int factionId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _databaseContext.InitializeAsync(cancellationToken);
        await EnsureSpecopsIndexedAsync(cancellationToken);
        return await _connection.Table<ArmyFactionRecord>().Where(x => x.FactionId == factionId).CountAsync() > 0;
    }

    public async Task<IReadOnlyList<int>> GetStoredFactionIdsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _databaseContext.InitializeAsync(cancellationToken);
        await EnsureSpecopsIndexedAsync(cancellationToken);
        var snapshots = await _connection.Table<ArmyFactionRecord>().OrderBy(x => x.FactionId).ToListAsync();
        return snapshots.Select(x => x.FactionId).ToList();
    }

    public async Task<ArmyFactionRecord?> GetFactionSnapshotAsync(int factionId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _databaseContext.InitializeAsync(cancellationToken);
        await EnsureSpecopsIndexedAsync(cancellationToken);
        return await _connection.FindAsync<ArmyFactionRecord>(factionId);
    }

    public async Task<IReadOnlyList<ArmyUnitRecord>> GetUnitsByFactionAsync(int factionId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _databaseContext.InitializeAsync(cancellationToken);
        await EnsureSpecopsIndexedAsync(cancellationToken);
        const string sql = """
            SELECT *
            FROM army_units
            WHERE FactionId = ?
              AND (Slug IS NULL OR Slug NOT LIKE ?)
            ORDER BY Name
            """;

        return await _connection.QueryAsync<ArmyUnitRecord>(sql, factionId, MercSlugPrefix);
    }

    public async Task<ArmyUnitRecord?> GetUnitAsync(int factionId, int unitId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _databaseContext.InitializeAsync(cancellationToken);
        await EnsureSpecopsIndexedAsync(cancellationToken);
        const string sql = """
            SELECT *
            FROM army_units
            WHERE FactionId = ?
              AND UnitId = ?
              AND (Slug IS NULL OR Slug NOT LIKE ?)
            LIMIT 1
            """;

        var rows = await _connection.QueryAsync<ArmyUnitRecord>(sql, factionId, unitId, MercSlugPrefix);
        return rows.FirstOrDefault();
    }

    public async Task<IReadOnlyList<ArmyUnitRecord>> SearchUnitsAsync(string searchTerm, int? factionId = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _databaseContext.InitializeAsync(cancellationToken);
        await EnsureSpecopsIndexedAsync(cancellationToken);

        var term = searchTerm?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(term))
        {
            if (factionId.HasValue)
            {
                const string sqlByFaction = """
                    SELECT *
                    FROM army_units
                    WHERE FactionId = ?
                      AND (Slug IS NULL OR Slug NOT LIKE ?)
                    ORDER BY Name
                    LIMIT 250
                    """;

                return await _connection.QueryAsync<ArmyUnitRecord>(sqlByFaction, factionId.Value, MercSlugPrefix);
            }

            const string sqlAll = """
                SELECT *
                FROM army_units
                WHERE (Slug IS NULL OR Slug NOT LIKE ?)
                ORDER BY Name
                LIMIT 250
                """;

            return await _connection.QueryAsync<ArmyUnitRecord>(sqlAll, MercSlugPrefix);
        }

        if (factionId.HasValue)
        {
            const string sqlByFactionWithTerm = """
                SELECT *
                FROM army_units
                WHERE FactionId = ?
                  AND Name LIKE ?
                  AND (Slug IS NULL OR Slug NOT LIKE ?)
                ORDER BY Name
                """;

            return await _connection.QueryAsync<ArmyUnitRecord>(sqlByFactionWithTerm, factionId.Value, $"%{term}%", MercSlugPrefix);
        }

        const string sqlWithTerm = """
            SELECT *
            FROM army_units
            WHERE Name LIKE ?
              AND (Slug IS NULL OR Slug NOT LIKE ?)
            ORDER BY Name
            """;

        return await _connection.QueryAsync<ArmyUnitRecord>(sqlWithTerm, $"%{term}%", MercSlugPrefix);
    }

    public async Task<IReadOnlyList<ArmyResumeRecord>> GetResumeByFactionAsync(int factionId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _databaseContext.InitializeAsync(cancellationToken);
        await EnsureSpecopsIndexedAsync(cancellationToken);
        const string sql = """
            SELECT *
            FROM army_resume
            WHERE FactionId = ?
              AND (Slug IS NULL OR Slug NOT LIKE ?)
            ORDER BY Name
            """;

        return await _connection.QueryAsync<ArmyResumeRecord>(sql, factionId, MercSlugPrefix);
    }

    public async Task<IReadOnlyList<ArmyResumeRecord>> GetResumeByFactionMercsOnlyAsync(int factionId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _databaseContext.InitializeAsync(cancellationToken);
        await EnsureSpecopsIndexedAsync(cancellationToken);

        const string sql = """
            SELECT r.*
            FROM army_resume r
            WHERE r.FactionId = ?
              AND EXISTS (
                    SELECT 1
                    FROM army_units u
                    WHERE u.FactionId = r.FactionId
                      AND u.UnitId = r.UnitId
                    UNION
                    SELECT 1
                    FROM army_specops_units su
                    WHERE su.FactionId = r.FactionId
                      AND su.UnitId = r.UnitId
                )
              AND (r.Slug IS NULL OR r.Slug NOT LIKE ?)
              AND (r.Category IS NULL OR r.Category <> ?)
              AND (r.Type IS NULL OR r.Type <> ?)
              AND (r.Type IS NULL OR r.Type <> ?)
            ORDER BY r.Name
            """;

        return await _connection.QueryAsync<ArmyResumeRecord>(
            sql,
            factionId,
            MercSlugPrefix,
            CharacterCategory,
            TagType,
            VehicleType);
    }

    public async Task<IReadOnlyList<ArmySpecopsSkillRecord>> GetSpecopsSkillsByFactionAsync(int factionId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _databaseContext.InitializeAsync(cancellationToken);
        await EnsureSpecopsIndexedAsync(cancellationToken);
        const string sql = """
            SELECT *
            FROM army_specops_skills
            WHERE FactionId = ?
            ORDER BY EntryOrder
            """;

        return await _connection.QueryAsync<ArmySpecopsSkillRecord>(sql, factionId);
    }

    public async Task<IReadOnlyList<ArmySpecopsEquipRecord>> GetSpecopsEquipsByFactionAsync(int factionId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _databaseContext.InitializeAsync(cancellationToken);
        await EnsureSpecopsIndexedAsync(cancellationToken);
        const string sql = """
            SELECT *
            FROM army_specops_equips
            WHERE FactionId = ?
            ORDER BY EntryOrder
            """;

        return await _connection.QueryAsync<ArmySpecopsEquipRecord>(sql, factionId);
    }

    public async Task<IReadOnlyList<ArmySpecopsWeaponRecord>> GetSpecopsWeaponsByFactionAsync(int factionId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _databaseContext.InitializeAsync(cancellationToken);
        await EnsureSpecopsIndexedAsync(cancellationToken);
        const string sql = """
            SELECT *
            FROM army_specops_weapons
            WHERE FactionId = ?
            ORDER BY EntryOrder
            """;

        return await _connection.QueryAsync<ArmySpecopsWeaponRecord>(sql, factionId);
    }

    public async Task<IReadOnlyList<ArmySpecopsUnitRecord>> GetSpecopsUnitsByFactionAsync(int factionId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _databaseContext.InitializeAsync(cancellationToken);
        await EnsureSpecopsIndexedAsync(cancellationToken);
        const string sql = """
            SELECT *
            FROM army_specops_units
            WHERE FactionId = ?
            ORDER BY EntryOrder
            """;

        return await _connection.QueryAsync<ArmySpecopsUnitRecord>(sql, factionId);
    }

    public async Task<IReadOnlyList<CCFactionFireteamValidityRecord>> GetCCFactionFireteamValidityAsync(
        string filterKey,
        IReadOnlyCollection<int> factionIds,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _databaseContext.InitializeAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(filterKey) || factionIds.Count == 0)
        {
            return [];
        }

        var rows = await _connection.Table<CCFactionFireteamValidityRecord>()
            .Where(x => x.FilterKey == filterKey)
            .ToListAsync();

        var ids = new HashSet<int>(factionIds);
        return rows.Where(x => ids.Contains(x.FactionId)).ToList();
    }

    public async Task UpsertCCFactionFireteamValidityAsync(
        int factionId,
        string filterKey,
        bool hasValidCoreFireteams,
        string? validCoreFireteamsJson,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _databaseContext.InitializeAsync(cancellationToken);

        if (factionId <= 0 || string.IsNullOrWhiteSpace(filterKey))
        {
            return;
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

        await _connection.InsertOrReplaceAsync(record);
    }

    private static string BuildUnitKey(int factionId, ArmyUnitDto unit)
    {
        return $"{factionId}:{unit.Id}:{unit.IdArmy ?? 0}:{unit.Slug ?? string.Empty}";
    }

    private static string BuildResumeKey(int factionId, ArmyResumeDto unit)
    {
        return $"{factionId}:{unit.Id}:{unit.Slug ?? string.Empty}";
    }

    private static string BuildCCFactionFireteamValidityCacheKey(int factionId, string filterKey)
    {
        return $"{factionId}:{filterKey}";
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

            var snapshots = await _connection.Table<ArmyFactionRecord>().ToListAsync();
            var yuJingSpecops = TryGetFactionSpecopsRootFromSnapshots(snapshots, YuJingFactionId);
            foreach (var snapshot in snapshots)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (string.IsNullOrWhiteSpace(snapshot.SpecopsJson))
                {
                    continue;
                }

                var existingCount = await _connection.ExecuteScalarAsync<int>(
                    """
                    SELECT
                        (SELECT COUNT(*) FROM army_specops_skills WHERE FactionId = ?)
                      + (SELECT COUNT(*) FROM army_specops_equips WHERE FactionId = ?)
                      + (SELECT COUNT(*) FROM army_specops_weapons WHERE FactionId = ?)
                      + (SELECT COUNT(*) FROM army_specops_units WHERE FactionId = ?)
                    """,
                    snapshot.FactionId,
                    snapshot.FactionId,
                    snapshot.FactionId,
                    snapshot.FactionId);

                var fallbackSpecops = IsJsaFaction(snapshot.FactionId) ? yuJingSpecops : default;
                var shouldForceReindexForJsa = IsJsaFaction(snapshot.FactionId) && HasAnySpecopsArray(fallbackSpecops);

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
                    await _connection.ExecuteAsync("DELETE FROM army_specops_skills WHERE FactionId = ?", snapshot.FactionId);
                    await _connection.ExecuteAsync("DELETE FROM army_specops_equips WHERE FactionId = ?", snapshot.FactionId);
                    await _connection.ExecuteAsync("DELETE FROM army_specops_weapons WHERE FactionId = ?", snapshot.FactionId);
                    await _connection.ExecuteAsync("DELETE FROM army_specops_units WHERE FactionId = ?", snapshot.FactionId);
                }

                if (skillRows.Count > 0)
                {
                    await _connection.InsertAllAsync(skillRows);
                }

                if (equipRows.Count > 0)
                {
                    await _connection.InsertAllAsync(equipRows);
                }

                if (weaponRows.Count > 0)
                {
                    await _connection.InsertAllAsync(weaponRows);
                }

                if (unitRows.Count > 0)
                {
                    await _connection.InsertAllAsync(unitRows);
                }
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
        var snapshot = await _connection.FindAsync<ArmyFactionRecord>(factionId);
        if (snapshot is null || string.IsNullOrWhiteSpace(snapshot.SpecopsJson))
        {
            return default;
        }

        using var doc = JsonDocument.Parse(snapshot.SpecopsJson);
        return doc.RootElement.Clone();
    }

    private static JsonElement TryGetFactionSpecopsRootFromSnapshots(IEnumerable<ArmyFactionRecord> snapshots, int factionId)
    {
        var snapshot = snapshots.FirstOrDefault(x => x.FactionId == factionId);
        if (snapshot is null || string.IsNullOrWhiteSpace(snapshot.SpecopsJson))
        {
            return default;
        }

        using var doc = JsonDocument.Parse(snapshot.SpecopsJson);
        return doc.RootElement.Clone();
    }

    private static bool IsJsaFaction(int factionId)
    {
        return factionId is JsaFactionId or JsaShindenbutaiFactionId or JsaObanFactionId;
    }

    private static bool HasAnySpecopsArray(JsonElement specops)
    {
        return TryGetSpecopsArray(specops, "skills", out _) ||
               TryGetSpecopsArray(specops, "equip", out _) ||
               TryGetSpecopsArray(specops, "weapons", out _);
    }

    private static List<ArmySpecopsSkillRecord> BuildSpecopsSkillRecords(int factionId, JsonElement specops, JsonElement fallbackSpecops = default)
    {
        var records = new List<ArmySpecopsSkillRecord>();
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
            records.Add(new ArmySpecopsSkillRecord
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

    private static List<ArmySpecopsEquipRecord> BuildSpecopsEquipRecords(int factionId, JsonElement specops, JsonElement fallbackSpecops = default)
    {
        var records = new List<ArmySpecopsEquipRecord>();
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
            records.Add(new ArmySpecopsEquipRecord
            {
                SpecopsEquipKey = $"{factionId}:{entryOrder}:{equipId}:{exp}",
                FactionId = factionId,
                EntryOrder = entryOrder,
                EquipId = equipId,
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

    private static List<ArmySpecopsWeaponRecord> BuildSpecopsWeaponRecords(int factionId, JsonElement specops, JsonElement fallbackSpecops = default)
    {
        var records = new List<ArmySpecopsWeaponRecord>();
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
            records.Add(new ArmySpecopsWeaponRecord
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

    private static List<ArmySpecopsUnitRecord> BuildSpecopsUnitRecords(int factionId, JsonElement specops)
    {
        var records = new List<ArmySpecopsUnitRecord>();
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

            records.Add(new ArmySpecopsUnitRecord
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
