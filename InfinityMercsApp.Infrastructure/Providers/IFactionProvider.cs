using InfinityMercsApp.Infrastructure.Models.Database.Army;

namespace InfinityMercsApp.Infrastructure.Providers;

/// <summary>
/// A provider that handles faction data.
/// </summary>
public interface IFactionProvider
{
    /// <summary>
    /// Determines whether a faction exists.
    /// </summary>
    /// <param name="factionId"></param>
    /// <returns></returns>
    bool HasFactionArmy(int factionId);

    /// <summary>
    /// Gets a list of FactionIDs.
    /// </summary>
    /// <returns></returns>
    IReadOnlyList<int> GetStoredFactionIds();

    /// <summary>
    /// Gets a full representation of a faction.
    /// </summary>
    /// <param name="factionId"></param>
    /// <returns></returns>
    Faction? GetFactionSnapshot(int factionId);

    /// <summary>
    /// Gets units for a faction.
    /// </summary>
    /// <param name="factionId"></param>
    /// <returns></returns>
    IReadOnlyList<Unit> GetUnitsByFaction(int factionId);

    /// <summary>
    /// Gets a unit for a faction.
    /// </summary>
    /// <param name="factionId"></param>
    /// <param name="unitId"></param>
    /// <returns></returns>
    Unit? GetUnit(int factionId, int unitId);

    /// <summary>
    /// Searches units, optionally specifying a faction.
    /// </summary>
    /// <param name="searchTerm"></param>
    /// <param name="factionId"></param>
    /// <returns></returns>
    IReadOnlyList<Unit> SearchUnits(string searchTerm, int? factionId = null);

    /// <summary>
    /// Gets a resume by faction.
    /// </summary>
    /// <param name="factionId"></param>
    /// <returns></returns>
    IReadOnlyList<Resume> GetResumeByFaction(int factionId);

    /// <summary>
    /// Gets a resume by a faction containing only mercs.
    /// </summary>
    /// <param name="factionId"></param>
    /// <returns></returns>
    IReadOnlyList<Resume> GetResumeByFactionMercsOnly(int factionId);

    /// <summary>
    /// Gets a collection of units by faction and unit identifiers.
    /// </summary>
    /// <param name="factionId"></param>
    /// <param name="unitIds"></param>
    /// <returns></returns>
    IReadOnlyDictionary<int, Unit> GetUnitsByFactionAndIds(int factionId, IReadOnlyCollection<int> unitIds);
}
