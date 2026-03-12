using System.Text.Json;
using System.Text.Json.Serialization;
using SQLite;

namespace InfinityMercsApp.Data.Database;

/// @brief SQLite-backed accessor for imported army snapshot data and non-SpecOps query paths.
public class ArmyDataAccessor : IArmyDataAccessor
{
    private const int CharacterCategory = 10;
    private const int TagType = 4;
    private const int VehicleType = 8;
    private const string MercSlugPrefix = "merc-%";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        Converters = { new RelaxedInt32Converter(), new RelaxedNullableInt32Converter() }
    };

    private readonly IDatabaseContext _databaseContext;
    // Spec-Ops persistence/query concerns live in the dedicated accessor to keep this class focused.
    private readonly ISpecOpsDataAccessor _specOpsDataAccessor;
    private readonly SQLiteAsyncConnection _connection;

    /// @brief Creates a new army data accessor.
    /// @param databaseContext Database context providing connection and initialization.
    /// @param specOpsDataAccessor Accessor responsible for SpecOps data.
    public ArmyDataAccessor(IDatabaseContext databaseContext, ISpecOpsDataAccessor specOpsDataAccessor)
    {
        _databaseContext = databaseContext;
        _specOpsDataAccessor = specOpsDataAccessor;
        _connection = databaseContext.Connection;
    }

    /// @brief Imports one faction's army JSON payload into local tables, replacing prior rows for that faction.
    /// @param factionId Target faction identifier.
    /// @param json Raw army JSON payload.
    /// @param cancellationToken Cancellation signal.
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

        await _connection.ExecuteAsync("DELETE FROM army_units WHERE FactionId = ?", factionId);
        await _connection.ExecuteAsync("DELETE FROM army_resume WHERE FactionId = ?", factionId);
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

        await _specOpsDataAccessor.ReplaceFactionSpecopsAsync(factionId, document.Specops, cancellationToken);
    }

    /// @brief Imports one faction's army snapshot from a file path.
    /// @param factionId Target faction identifier.
    /// @param filePath Path to a JSON file.
    /// @param cancellationToken Cancellation signal.
    public async Task ImportFactionArmyFromFileAsync(int factionId, string filePath, CancellationToken cancellationToken = default)
    {
        var json = await File.ReadAllTextAsync(filePath, cancellationToken);
        await ImportFactionArmyFromJsonAsync(factionId, json, cancellationToken);
    }

    /// @brief Checks whether army snapshot data exists for the given faction.
    /// @param factionId Faction identifier.
    /// @param cancellationToken Cancellation signal.
    /// @return True when at least one faction snapshot row is stored.
    public async Task<bool> HasFactionArmyAsync(int factionId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _databaseContext.InitializeAsync(cancellationToken);
        await _specOpsDataAccessor.EnsureSpecopsIndexedAsync(cancellationToken);
        return await _connection.Table<ArmyFactionRecord>().Where(x => x.FactionId == factionId).CountAsync() > 0;
    }

    /// @brief Returns all faction ids that currently have stored army snapshots.
    /// @param cancellationToken Cancellation signal.
    /// @return Sorted faction ids.
    public async Task<IReadOnlyList<int>> GetStoredFactionIdsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _databaseContext.InitializeAsync(cancellationToken);
        await _specOpsDataAccessor.EnsureSpecopsIndexedAsync(cancellationToken);
        var snapshots = await _connection.Table<ArmyFactionRecord>().OrderBy(x => x.FactionId).ToListAsync();
        return snapshots.Select(x => x.FactionId).ToList();
    }

    /// @brief Retrieves the stored snapshot row for one faction.
    /// @param factionId Faction identifier.
    /// @param cancellationToken Cancellation signal.
    /// @return Snapshot row, or null when not present.
    public async Task<ArmyFactionRecord?> GetFactionSnapshotAsync(int factionId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _databaseContext.InitializeAsync(cancellationToken);
        await _specOpsDataAccessor.EnsureSpecopsIndexedAsync(cancellationToken);
        return await _connection.FindAsync<ArmyFactionRecord>(factionId);
    }

    /// @brief Gets all non-merc unit rows for a faction.
    /// @param factionId Faction identifier.
    /// @param cancellationToken Cancellation signal.
    /// @return Unit rows ordered by name.
    public async Task<IReadOnlyList<ArmyUnitRecord>> GetUnitsByFactionAsync(int factionId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _databaseContext.InitializeAsync(cancellationToken);
        await _specOpsDataAccessor.EnsureSpecopsIndexedAsync(cancellationToken);
        const string sql = """
            SELECT *
            FROM army_units
            WHERE FactionId = ?
              AND (Slug IS NULL OR Slug NOT LIKE ?)
            ORDER BY Name
            """;

        return await _connection.QueryAsync<ArmyUnitRecord>(sql, factionId, MercSlugPrefix);
    }

    /// @brief Gets one non-merc unit row by faction and unit id.
    /// @param factionId Faction identifier.
    /// @param unitId Unit identifier.
    /// @param cancellationToken Cancellation signal.
    /// @return Matching unit row, or null when not present.
    public async Task<ArmyUnitRecord?> GetUnitAsync(int factionId, int unitId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _databaseContext.InitializeAsync(cancellationToken);
        await _specOpsDataAccessor.EnsureSpecopsIndexedAsync(cancellationToken);
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

    /// @brief Gets non-merc unit rows for one faction constrained to a unit id set.
    /// @param factionId Faction identifier.
    /// @param unitIds Unit identifiers to fetch.
    /// @param cancellationToken Cancellation signal.
    /// @return Lookup keyed by unit id.
    public async Task<IReadOnlyDictionary<int, ArmyUnitRecord>> GetUnitsByFactionAndIdsAsync(
        int factionId,
        IReadOnlyCollection<int> unitIds,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _databaseContext.InitializeAsync(cancellationToken);
        await _specOpsDataAccessor.EnsureSpecopsIndexedAsync(cancellationToken);

        if (unitIds.Count == 0)
        {
            return new Dictionary<int, ArmyUnitRecord>();
        }

        var normalizedUnitIds = unitIds
            .Where(id => id > 0)
            .Distinct()
            .ToArray();
        if (normalizedUnitIds.Length == 0)
        {
            return new Dictionary<int, ArmyUnitRecord>();
        }

        var placeholders = string.Join(",", Enumerable.Repeat("?", normalizedUnitIds.Length));
        var sql = $"""
            SELECT *
            FROM army_units
            WHERE FactionId = ?
              AND UnitId IN ({placeholders})
              AND (Slug IS NULL OR Slug NOT LIKE ?)
            """;

        var parameters = new object[normalizedUnitIds.Length + 2];
        parameters[0] = factionId;
        for (var i = 0; i < normalizedUnitIds.Length; i++)
        {
            parameters[i + 1] = normalizedUnitIds[i];
        }

        parameters[^1] = MercSlugPrefix;

        var rows = await _connection.QueryAsync<ArmyUnitRecord>(sql, parameters);
        return rows
            .GroupBy(x => x.UnitId)
            .ToDictionary(x => x.Key, x => x.First());
    }

    /// @brief Searches non-merc units by name, optionally scoped to one faction.
    /// @param searchTerm Name fragment; blank values return a capped default list.
    /// @param factionId Optional faction filter.
    /// @param cancellationToken Cancellation signal.
    /// @return Matching unit rows ordered by name.
    public async Task<IReadOnlyList<ArmyUnitRecord>> SearchUnitsAsync(string searchTerm, int? factionId = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _databaseContext.InitializeAsync(cancellationToken);
        await _specOpsDataAccessor.EnsureSpecopsIndexedAsync(cancellationToken);

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

    /// @brief Gets all non-merc resume rows for a faction.
    /// @param factionId Faction identifier.
    /// @param cancellationToken Cancellation signal.
    /// @return Resume rows ordered by name.
    public async Task<IReadOnlyList<ArmyResumeRecord>> GetResumeByFactionAsync(int factionId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _databaseContext.InitializeAsync(cancellationToken);
        await _specOpsDataAccessor.EnsureSpecopsIndexedAsync(cancellationToken);
        const string sql = """
            SELECT *
            FROM army_resume
            WHERE FactionId = ?
              AND (Slug IS NULL OR Slug NOT LIKE ?)
            ORDER BY Name
            """;

        var rows = await _connection.QueryAsync<ArmyResumeRecord>(sql, factionId, MercSlugPrefix);
        return ArmyUnitSort
            .OrderByUnitTypeAndName(rows, x => x.Type, x => x.Name)
            .ToList();
    }

    /// @brief Gets merc-only resume rows for a faction while excluding characters, TAGs, and vehicles.
    /// @param factionId Faction identifier.
    /// @param cancellationToken Cancellation signal.
    /// @return Merc-only resume rows ordered by name.
    public async Task<IReadOnlyList<ArmyResumeRecord>> GetResumeByFactionMercsOnlyAsync(int factionId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _databaseContext.InitializeAsync(cancellationToken);
        await _specOpsDataAccessor.EnsureSpecopsIndexedAsync(cancellationToken);

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

        var rows = await _connection.QueryAsync<ArmyResumeRecord>(
            sql,
            factionId,
            MercSlugPrefix,
            CharacterCategory,
            TagType,
            VehicleType);

        return ArmyUnitSort
            .OrderByUnitTypeAndName(rows, x => x.Type, x => x.Name)
            .ToList();
    }

    /// @brief Builds a deterministic primary key for one unit row.
    private static string BuildUnitKey(int factionId, ArmyUnitDto unit)
    {
        return $"{factionId}:{unit.Id}:{unit.IdArmy ?? 0}:{unit.Slug ?? string.Empty}";
    }

    /// @brief Builds a deterministic primary key for one resume row.
    private static string BuildResumeKey(int factionId, ArmyResumeDto unit)
    {
        return $"{factionId}:{unit.Id}:{unit.Slug ?? string.Empty}";
    }

    /// @brief Returns raw JSON text for an element unless it is null/undefined.
    private static string? ToJsonOrNull(JsonElement element)
    {
        return element.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null
            ? null
            : element.GetRawText();
    }
}
