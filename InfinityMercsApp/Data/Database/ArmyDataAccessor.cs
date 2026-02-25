using System.Text.Json;
using System.Text.Json.Serialization;
using SQLite;

namespace InfinityMercsApp.Data.Database;

public class ArmyDataAccessor : IArmyDataAccessor
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        Converters = { new RelaxedInt32Converter(), new RelaxedNullableInt32Converter() }
    };

    private readonly IDatabaseContext _databaseContext;
    private readonly SQLiteAsyncConnection _connection;

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

        await _connection.ExecuteAsync("DELETE FROM army_units WHERE FactionId = ?", factionId);
        await _connection.ExecuteAsync("DELETE FROM army_resume WHERE FactionId = ?", factionId);
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
        return await _connection.Table<ArmyFactionRecord>().Where(x => x.FactionId == factionId).CountAsync() > 0;
    }

    public async Task<IReadOnlyList<int>> GetStoredFactionIdsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _databaseContext.InitializeAsync(cancellationToken);
        var snapshots = await _connection.Table<ArmyFactionRecord>().OrderBy(x => x.FactionId).ToListAsync();
        return snapshots.Select(x => x.FactionId).ToList();
    }

    public async Task<ArmyFactionRecord?> GetFactionSnapshotAsync(int factionId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _databaseContext.InitializeAsync(cancellationToken);
        return await _connection.FindAsync<ArmyFactionRecord>(factionId);
    }

    public async Task<IReadOnlyList<ArmyUnitRecord>> GetUnitsByFactionAsync(int factionId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _databaseContext.InitializeAsync(cancellationToken);
        return await _connection.Table<ArmyUnitRecord>()
            .Where(x => x.FactionId == factionId)
            .OrderBy(x => x.Name)
            .ToListAsync();
    }

    public async Task<ArmyUnitRecord?> GetUnitAsync(int factionId, int unitId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _databaseContext.InitializeAsync(cancellationToken);
        return await _connection.Table<ArmyUnitRecord>()
            .Where(x => x.FactionId == factionId && x.UnitId == unitId)
            .FirstOrDefaultAsync();
    }

    public async Task<IReadOnlyList<ArmyUnitRecord>> SearchUnitsAsync(string searchTerm, int? factionId = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _databaseContext.InitializeAsync(cancellationToken);

        var term = searchTerm?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(term))
        {
            if (factionId.HasValue)
            {
                return await _connection.Table<ArmyUnitRecord>()
                    .Where(x => x.FactionId == factionId.Value)
                    .OrderBy(x => x.Name)
                    .Take(250)
                    .ToListAsync();
            }

            return await _connection.Table<ArmyUnitRecord>()
                .OrderBy(x => x.Name)
                .Take(250)
                .ToListAsync();
        }

        if (factionId.HasValue)
        {
            return await _connection.Table<ArmyUnitRecord>()
                .Where(x => x.FactionId == factionId.Value && x.Name.Contains(term))
                .OrderBy(x => x.Name)
                .ToListAsync();
        }

        return await _connection.Table<ArmyUnitRecord>()
            .Where(x => x.Name.Contains(term))
            .OrderBy(x => x.Name)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<ArmyResumeRecord>> GetResumeByFactionAsync(int factionId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _databaseContext.InitializeAsync(cancellationToken);
        return await _connection.Table<ArmyResumeRecord>()
            .Where(x => x.FactionId == factionId)
            .OrderBy(x => x.Name)
            .ToListAsync();
    }

    private static string BuildUnitKey(int factionId, ArmyUnitDto unit)
    {
        return $"{factionId}:{unit.Id}:{unit.IdArmy ?? 0}:{unit.Slug ?? string.Empty}";
    }

    private static string BuildResumeKey(int factionId, ArmyResumeDto unit)
    {
        return $"{factionId}:{unit.Id}:{unit.Slug ?? string.Empty}";
    }

    private static string? ToJsonOrNull(JsonElement element)
    {
        return element.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null
            ? null
            : element.GetRawText();
    }
}
