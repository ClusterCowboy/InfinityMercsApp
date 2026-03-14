using InfinityMercsApp.Domain.Models.Army;
using InfinityMercsApp.Infrastructure.Repositories;
using DbFaction = InfinityMercsApp.Infrastructure.Models.Database.Army.Faction;
using DbResume = InfinityMercsApp.Infrastructure.Models.Database.Army.Resume;
using DbUnit = InfinityMercsApp.Infrastructure.Models.Database.Army.Unit;

namespace InfinityMercsApp.Infrastructure.Providers;

/// <inheritdoc/>
public sealed class FactionProvider(ISQLiteRepository sqliteRepository) : IFactionProvider
{
    private const int CharacterCategory = 10;
    private const int TagType = 4;
    private const int VehicleType = 8;
    private const string MercSlugPrefix = "merc-";

    /// <inheritdoc/>
    public bool HasFactionArmy(int factionId)
    {
        return sqliteRepository.GetAll<DbFaction>(x => x.FactionId == factionId).Count() > 0;
    }

    /// <inheritdoc/>
    public IReadOnlyList<int> GetStoredFactionIds()
    {
        var snapshots = sqliteRepository.GetAll<DbFaction>(x => true, x => x.FactionId).ToList();
        return snapshots.Select(x => x.FactionId).ToList();
    }

    /// <inheritdoc/>
    public Faction? GetFactionSnapshot(int factionId)
    {
        var snapshot = sqliteRepository.GetById<DbFaction>(factionId);
        return snapshot is null ? null : MapFaction(snapshot);
    }

    /// <inheritdoc/>
    public IReadOnlyList<Unit> GetUnitsByFaction(int factionId)
    {
        return sqliteRepository
            .GetAll<DbUnit>(x => x.FactionId == factionId && (x.Slug == null || !x.Slug.StartsWith(MercSlugPrefix)), null)
            .Select(MapUnit)
            .ToList();
    }

    /// <inheritdoc/>
    public Unit? GetUnit(int factionId, int unitId)
    {
        var row = sqliteRepository
            .GetAll<DbUnit>(
                x => x.FactionId == factionId
                     && x.UnitId == unitId
                     && (x.Slug == null || !x.Slug.StartsWith(MercSlugPrefix)),
                null)
            .FirstOrDefault();
        return row is null ? null : MapUnit(row);
    }

    /// <inheritdoc/>
    public IReadOnlyList<Unit> SearchUnits(string searchTerm, int? factionId = null)
    {
        var term = searchTerm?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(term))
        {
            if (factionId is not null)
            {
                return GetUnitsByFaction(factionId.Value);
            }

            return sqliteRepository
                .GetAll<DbUnit>(x => x.Slug == null || !x.Slug.StartsWith(MercSlugPrefix))
                .Take(250)
                .Select(MapUnit)
                .ToList();
        }

        if (factionId.HasValue)
        {
            return sqliteRepository
                .GetAll<DbUnit>(x => x.Name.Contains(term) && x.FactionId == factionId.Value && (x.Slug == null || !x.Slug.StartsWith(MercSlugPrefix)))
                .Select(MapUnit)
                .ToList();
        }

        return sqliteRepository
            .GetAll<DbUnit>(x => x.Name.Contains(term) && (x.Slug == null || !x.Slug.StartsWith(MercSlugPrefix)))
            .Select(MapUnit)
            .ToList();
    }

    /// <inheritdoc/>
    public IReadOnlyList<Resume> GetResumeByFaction(int factionId)
    {
        return sqliteRepository
            .GetAll<DbResume>(x => x.FactionId == factionId && (x.Slug == null || !x.Slug.StartsWith(MercSlugPrefix)))
            .Select(MapResume)
            .ToList();
    }

    /// <inheritdoc/>
    public IReadOnlyList<Resume> GetResumeByFactionMercsOnly(int factionId)
    {
        return sqliteRepository
            .GetAll<DbResume>(
                x => x.FactionId == factionId
                     && (x.Slug == null || !x.Slug.StartsWith(MercSlugPrefix))
                     && (x.Category == null || x.Category != CharacterCategory)
                     && (x.Type == null || x.Type != TagType)
                     && (x.Type == null || x.Type != VehicleType),
                orderBy: x => x.Name)
            .Select(MapResume)
            .ToList();
    }

    private static Faction MapFaction(DbFaction source)
    {
        return new Faction
        {
            FactionId = source.FactionId,
            Version = source.Version,
            ImportedAtUnixSeconds = source.ImportedAtUnixSeconds,
            ReinforcementsJson = source.ReinforcementsJson,
            FiltersJson = source.FiltersJson,
            FireteamsJson = source.FireteamsJson,
            RelationsJson = source.RelationsJson,
            SpecopsJson = source.SpecopsJson,
            FireteamChartJson = source.FireteamChartJson,
            RawJson = source.RawJson
        };
    }

    private static Unit MapUnit(DbUnit source)
    {
        return new Unit
        {
            UnitKey = source.UnitKey,
            FactionId = source.FactionId,
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
            FactionsJson = source.FactionsJson
        };
    }

    private static Resume MapResume(DbResume source)
    {
        return new Resume
        {
            ResumeKey = source.ResumeKey,
            FactionId = source.FactionId,
            UnitId = source.UnitId,
            IdArmy = source.IdArmy,
            Isc = source.Isc,
            Name = source.Name,
            Slug = source.Slug,
            Logo = source.Logo,
            Type = source.Type,
            Category = source.Category
        };
    }
}
