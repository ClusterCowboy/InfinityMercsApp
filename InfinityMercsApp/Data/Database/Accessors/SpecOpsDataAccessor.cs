using System.Text.Json;
using SQLite;

namespace InfinityMercsApp.Data.Database;

/// @brief SQLite-backed accessor for Spec-Ops indexing, querying, and CC fireteam validity cache rows.
public class SpecOpsDataAccessor
{
    private const int YuJingFactionId = 201;
    private const int JsaFactionId = 1101;
    private const int JsaShindenbutaiFactionId = 1102;
    private const int JsaObanFactionId = 1103;

    private readonly DatabaseContext _databaseContext;
    private readonly SQLiteAsyncConnection _connection;
    // Backfill may be triggered from multiple call paths; this gate makes the operation one-time and thread-safe.
    private readonly SemaphoreSlim _specopsBackfillGate = new(1, 1);
    private bool _specopsBackfillCompleted;

    /// @brief Creates a new Spec-Ops data accessor.
    /// @param databaseContext Database context providing connection and initialization.
    public SpecOpsDataAccessor(DatabaseContext databaseContext)
    {
        _databaseContext = databaseContext;
        _connection = databaseContext.Connection;
    }

    /// @brief Replaces all persisted Spec-Ops rows for a faction from the provided Spec-Ops JSON payload.
    /// @param factionId Faction identifier.
    /// @param specops Spec-Ops JSON root element.
    /// @param cancellationToken Cancellation signal.
    public async Task ReplaceFactionSpecopsAsync(int factionId, JsonElement specops, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _databaseContext.InitializeAsync(cancellationToken);

        var yuJingSpecops = IsJsaFaction(factionId)
            ? await TryGetFactionSpecopsRootAsync(YuJingFactionId, cancellationToken)
            : default;

        var specopsSkills = BuildSpecopsSkillRecords(factionId, specops, yuJingSpecops);
        var specopsEquips = BuildSpecopsEquipRecords(factionId, specops, yuJingSpecops);
        var specopsWeapons = BuildSpecopsWeaponRecords(factionId, specops, yuJingSpecops);
        var specopsUnits = BuildSpecopsUnitRecords(factionId, specops);

        await _connection.ExecuteAsync("DELETE FROM army_specops_skills WHERE FactionId = ?", factionId);
        await _connection.ExecuteAsync("DELETE FROM army_specops_equips WHERE FactionId = ?", factionId);
        await _connection.ExecuteAsync("DELETE FROM army_specops_weapons WHERE FactionId = ?", factionId);
        await _connection.ExecuteAsync("DELETE FROM army_specops_units WHERE FactionId = ?", factionId);

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

    /// @brief Backfills Spec-Ops tables from stored snapshot JSON when rows are missing or need JSA fallback refresh.
    /// @param cancellationToken Cancellation signal.
    public async Task EnsureSpecopsIndexedAsync(CancellationToken cancellationToken = default)
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

    /// @brief Gets Spec-Ops skill rows for a faction.
    /// @param factionId Faction identifier.
    /// @param cancellationToken Cancellation signal.
    /// @return Skill rows ordered by entry order.
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

    /// @brief Gets Spec-Ops equipment rows for a faction.
    /// @param factionId Faction identifier.
    /// @param cancellationToken Cancellation signal.
    /// @return Equipment rows ordered by entry order.
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

    /// @brief Gets Spec-Ops weapon rows for a faction.
    /// @param factionId Faction identifier.
    /// @param cancellationToken Cancellation signal.
    /// @return Weapon rows ordered by entry order.
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

    /// @brief Gets Spec-Ops unit rows for a faction.
    /// @param factionId Faction identifier.
    /// @param cancellationToken Cancellation signal.
    /// @return Unit rows ordered by entry order.
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

    /// @brief Reads cohesive-company fireteam validity cache rows for a filter key and faction id set.
    /// @param filterKey Logical filter key.
    /// @param factionIds Faction ids to include.
    /// @param cancellationToken Cancellation signal.
    /// @return Matching cache rows.
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

    /// @brief Inserts or replaces one cohesive-company fireteam validity cache row.
    /// @param factionId Faction identifier.
    /// @param filterKey Logical filter key.
    /// @param hasValidCoreFireteams Whether valid core fireteams were found.
    /// @param validCoreFireteamsJson Optional serialized fireteam payload.
    /// @param cancellationToken Cancellation signal.
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

    /// @brief Builds a deterministic cache key for cohesive-company fireteam validity rows.
    private static string BuildCCFactionFireteamValidityCacheKey(int factionId, string filterKey)
    {
        return $"{factionId}:{filterKey}";
    }

    /// @brief Attempts to parse and return Spec-Ops JSON root for a stored faction snapshot.
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

    /// @brief Attempts to parse and return Spec-Ops JSON root from an in-memory snapshot collection.
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

    /// @brief Returns whether the faction id belongs to one of the JSA factions.
    private static bool IsJsaFaction(int factionId)
    {
        return factionId is JsaFactionId or JsaShindenbutaiFactionId or JsaObanFactionId;
    }

    /// @brief Returns whether any known Spec-Ops arrays are available in the JSON object.
    private static bool HasAnySpecopsArray(JsonElement specops)
    {
        return TryGetSpecopsArray(specops, "skills", out _) ||
               TryGetSpecopsArray(specops, "equip", out _) ||
               TryGetSpecopsArray(specops, "weapons", out _);
    }

    /// @brief Builds normalized Spec-Ops skill rows from JSON payload.
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

    /// @brief Builds normalized Spec-Ops equipment rows from JSON payload.
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

    /// @brief Builds normalized Spec-Ops weapon rows from JSON payload.
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

    /// @brief Builds normalized Spec-Ops unit rows from JSON payload.
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

    /// @brief Tries to read a named array property from a Spec-Ops JSON object.
    private static bool TryGetSpecopsArray(JsonElement specops, string propertyName, out JsonElement array)
    {
        array = default;
        return specops.ValueKind == JsonValueKind.Object &&
               specops.TryGetProperty(propertyName, out array) &&
               array.ValueKind == JsonValueKind.Array;
    }

    /// @brief Tries to read a named array property from primary or fallback Spec-Ops JSON objects.
    private static bool TryGetSpecopsArray(JsonElement specops, JsonElement fallbackSpecops, string propertyName, out JsonElement array)
    {
        if (TryGetSpecopsArray(specops, propertyName, out array))
        {
            return true;
        }

        return TryGetSpecopsArray(fallbackSpecops, propertyName, out array);
    }

    /// @brief Tries to read an integer property from a JSON object.
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

    /// @brief Tries to read a nullable integer property from a JSON object.
    private static int? TryReadNullableIntProperty(JsonElement container, string propertyName)
    {
        if (!TryReadIntProperty(container, propertyName, out var value))
        {
            return null;
        }

        return value;
    }

    /// @brief Tries to read an integer from a JSON number or numeric string.
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

    /// @brief Tries to read a string property from a JSON object.
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

    /// @brief Tries to read and serialize a JSON property to raw text.
    private static string? TryReadJsonProperty(JsonElement container, string propertyName)
    {
        if (container.ValueKind != JsonValueKind.Object ||
            !container.TryGetProperty(propertyName, out var propertyElement))
        {
            return null;
        }

        return ToJsonOrNull(propertyElement);
    }

    /// @brief Returns raw JSON text for an element unless it is null or undefined.
    private static string? ToJsonOrNull(JsonElement element)
    {
        return element.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null
            ? null
            : element.GetRawText();
    }
}
