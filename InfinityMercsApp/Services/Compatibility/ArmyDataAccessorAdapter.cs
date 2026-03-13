using System.Text.Json;
using System.Text.Json.Serialization;
using InfinityMercsApp.Data.Database;
using InfinityMercsApp.Infrastructure.Models.Database.Army;
using InfinityMercsApp.Infrastructure.Providers;

namespace InfinityMercsApp.Services.Compatibility;

internal sealed class ArmyDataAccessorAdapter(
    IFactionProvider factionProvider,
    IArmyImportProvider armyImportProvider) : IArmyDataAccessor
{
    private const int CharacterCategory = 10;
    private const int TagType = 4;
    private const int VehicleType = 8;
    private const string MercSlugPrefix = "merc-";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        Converters = { new RelaxedInt32Converter(), new RelaxedNullableInt32Converter() }
    };

    public async Task ImportFactionArmyFromJsonAsync(int factionId, string json, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var faction = JsonSerializer.Deserialize<Infrastructure.Models.API.Army.Faction>(json, JsonOptions)
            ?? throw new InvalidOperationException("Failed to deserialize army JSON.");

        await armyImportProvider.ImportAsync(factionId, faction, cancellationToken);
    }

    public async Task ImportFactionArmyFromFileAsync(int factionId, string filePath, CancellationToken cancellationToken = default)
    {
        var json = await File.ReadAllTextAsync(filePath, cancellationToken);
        await ImportFactionArmyFromJsonAsync(factionId, json, cancellationToken);
    }

    public Task<bool> HasFactionArmyAsync(int factionId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(factionProvider.HasFactionArmy(factionId));
    }

    public Task<IReadOnlyList<int>> GetStoredFactionIdsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(factionProvider.GetStoredFactionIds());
    }

    public Task<ArmyFactionRecord?> GetFactionSnapshotAsync(int factionId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(MapFactionRecord(factionProvider.GetFactionSnapshot(factionId)));
    }

    public Task<IReadOnlyList<ArmyUnitRecord>> GetUnitsByFactionAsync(int factionId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var rows = factionProvider
            .GetUnitsByFaction(factionId)
            .Where(IsNotMercSlug)
            .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .Select(MapUnitRecord)
            .ToList();
        return Task.FromResult<IReadOnlyList<ArmyUnitRecord>>(rows);
    }

    public Task<ArmyUnitRecord?> GetUnitAsync(int factionId, int unitId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var row = factionProvider
            .GetUnitsByFaction(factionId)
            .Where(IsNotMercSlug)
            .FirstOrDefault(x => x.UnitId == unitId);
        return Task.FromResult(row is null ? null : MapUnitRecord(row));
    }

    public Task<IReadOnlyDictionary<int, ArmyUnitRecord>> GetUnitsByFactionAndIdsAsync(
        int factionId,
        IReadOnlyCollection<int> unitIds,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (unitIds.Count == 0)
        {
            return Task.FromResult<IReadOnlyDictionary<int, ArmyUnitRecord>>(new Dictionary<int, ArmyUnitRecord>());
        }

        var allowedIds = unitIds
            .Where(id => id > 0)
            .Distinct()
            .ToHashSet();

        var rows = factionProvider
            .GetUnitsByFaction(factionId)
            .Where(IsNotMercSlug)
            .Where(x => allowedIds.Contains(x.UnitId))
            .Select(MapUnitRecord)
            .ToDictionary(x => x.UnitId, x => x);

        return Task.FromResult<IReadOnlyDictionary<int, ArmyUnitRecord>>(rows);
    }

    public Task<IReadOnlyList<ArmyUnitRecord>> SearchUnitsAsync(string searchTerm, int? factionId = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var rows = factionProvider
            .SearchUnits(searchTerm, factionId)
            .Where(IsNotMercSlug)
            .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .Select(MapUnitRecord)
            .ToList();
        return Task.FromResult<IReadOnlyList<ArmyUnitRecord>>(rows);
    }

    public Task<IReadOnlyList<ArmyResumeRecord>> GetResumeByFactionAsync(int factionId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var rows = factionProvider
            .GetResumeByFaction(factionId)
            .Where(IsNotMercSlug)
            .Select(MapResumeRecord);

        var sorted = ArmyUnitSort
            .OrderByUnitTypeAndName(rows, x => x.Type, x => x.Name)
            .ToList();
        return Task.FromResult<IReadOnlyList<ArmyResumeRecord>>(sorted);
    }

    public Task<IReadOnlyList<ArmyResumeRecord>> GetResumeByFactionMercsOnlyAsync(int factionId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var rows = factionProvider
            .GetResumeByFactionMercsOnly(factionId)
            .Where(IsNotMercSlug)
            .Where(x => x.Category is null || x.Category != CharacterCategory)
            .Where(x => x.Type is null || x.Type != TagType)
            .Where(x => x.Type is null || x.Type != VehicleType)
            .Select(MapResumeRecord);

        var sorted = ArmyUnitSort
            .OrderByUnitTypeAndName(rows, x => x.Type, x => x.Name)
            .ToList();
        return Task.FromResult<IReadOnlyList<ArmyResumeRecord>>(sorted);
    }

    private static bool IsNotMercSlug(Unit unit)
    {
        return string.IsNullOrWhiteSpace(unit.Slug) ||
               !unit.Slug.Contains(MercSlugPrefix, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsNotMercSlug(Resume resume)
    {
        return string.IsNullOrWhiteSpace(resume.Slug) ||
               !resume.Slug.Contains(MercSlugPrefix, StringComparison.OrdinalIgnoreCase);
    }

    private static ArmyFactionRecord? MapFactionRecord(Faction? faction)
    {
        if (faction is null)
        {
            return null;
        }

        return new ArmyFactionRecord
        {
            FactionId = faction.FactionId,
            Version = faction.Version,
            ImportedAtUnixSeconds = faction.ImportedAtUnixSeconds,
            ReinforcementsJson = faction.ReinforcementsJson,
            FiltersJson = faction.FiltersJson,
            FireteamsJson = faction.FireteamsJson,
            RelationsJson = faction.RelationsJson,
            SpecopsJson = faction.SpecopsJson,
            FireteamChartJson = faction.FireteamChartJson,
            RawJson = faction.RawJson
        };
    }

    private static ArmyUnitRecord MapUnitRecord(Unit unit)
    {
        return new ArmyUnitRecord
        {
            UnitKey = unit.UnitKey,
            FactionId = unit.FactionId,
            UnitId = unit.UnitId,
            IdArmy = unit.IdArmy,
            Canonical = unit.Canonical,
            Isc = unit.Isc,
            IscAbbr = unit.IscAbbr,
            Name = unit.Name,
            Slug = unit.Slug,
            ProfileGroupsJson = unit.ProfileGroupsJson,
            OptionsJson = unit.OptionsJson,
            FiltersJson = unit.FiltersJson,
            FactionsJson = unit.FactionsJson
        };
    }

    private static ArmyResumeRecord MapResumeRecord(Resume resume)
    {
        return new ArmyResumeRecord
        {
            ResumeKey = resume.ResumeKey,
            FactionId = resume.FactionId,
            UnitId = resume.UnitId,
            IdArmy = resume.IdArmy,
            Isc = resume.Isc,
            Name = resume.Name,
            Slug = resume.Slug,
            Logo = resume.Logo,
            Type = resume.Type,
            Category = resume.Category
        };
    }
}
