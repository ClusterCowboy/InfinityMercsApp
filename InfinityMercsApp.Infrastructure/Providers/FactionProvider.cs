using InfinityMercsApp.Infrastructure.Models.Database.Army;
using InfinityMercsApp.Infrastructure.Repositories;

namespace InfinityMercsApp.Infrastructure.Providers;

/// <inheritdoc/>
public sealed class FactionProvider(ISQLiteRepository sqliteRepository) : IFactionProvider
{
    private const int CharacterCategory = 10;
    private const int TagType = 4;
    private const int VehicleType = 8;
    private const string MercSlugPrefix = "merc-%";

    /// <inheritdoc/>
    public bool HasFactionArmy(int factionId)
    {
        return sqliteRepository.GetAll<Faction>(x => x.FactionId == factionId).Count() > 0;
    }

    /// <inheritdoc/>
    public IReadOnlyList<int> GetStoredFactionIds()
    {
        var snapshots = sqliteRepository.GetAll<Faction>(x => true, x => x.FactionId).ToList();
        return snapshots.Select(x => x.FactionId).ToList();
    }

    /// <inheritdoc/>
    public Faction? GetFactionSnapshot(int factionId)
    {
        return sqliteRepository.GetById<Faction>(factionId);
    }

    /// <inheritdoc/>
    public IReadOnlyList<Unit> GetUnitsByFaction(int factionId)
    {
        return sqliteRepository.GetAll<Unit>(x => x.FactionId == factionId && (x.Slug == null || !x.Slug.Contains(MercSlugPrefix)), null);
    }

    /// <inheritdoc/>
    public Unit? GetUnit(int factionId, int unitId)
    {
        return sqliteRepository.GetAll<Unit>(x => x.FactionId == factionId && (x.Slug == null || !x.Slug.Contains(MercSlugPrefix)), null).FirstOrDefault();
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

            return sqliteRepository.GetAll<Unit>(x => x.Slug == null || !x.Slug.Contains(MercSlugPrefix)).Take(250).ToList();
        }

        if (factionId.HasValue)
        {
            return sqliteRepository.GetAll<Unit>(x => x.Name.Contains(term) && x.FactionId == factionId.Value && (x.Slug == null || !x.Slug.Contains(MercSlugPrefix)));
        }

        return sqliteRepository.GetAll<Unit>(x => x.Name.Contains(term) && (x.Slug == null || !x.Slug.Contains(MercSlugPrefix)));
    }

    /// <inheritdoc/>
    public IReadOnlyList<Resume> GetResumeByFaction(int factionId)
    {
        return sqliteRepository.GetAll<Resume>(x => x.FactionId == factionId && (x.Slug == null || !x.Slug.Contains(MercSlugPrefix)));
    }

    /// <inheritdoc/>
    public IReadOnlyList<Resume> GetResumeByFactionMercsOnly(int factionId)
    {
        return sqliteRepository.GetAll<Resume>(x => x.FactionId == factionId
                                                    && (x.Slug == null || !x.Slug.Contains(MercSlugPrefix))
                                                    && (x.Category == null || x.Category != CharacterCategory)
                                                    && (x.Type == null || x.Type != TagType)
                                                    && (x.Type == null || x.Type != VehicleType),
                                                    orderBy: x => x.Name);
    }
}
