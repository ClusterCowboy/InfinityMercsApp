using InfinityMercsApp.Domain.Models.DataImport;
using InfinityMercsApp.Infrastructure.Models.Database.Metadata;

namespace InfinityMercsApp.Infrastructure.Providers;

/// <summary>
/// Handles Metadata records, both importing and getting from local storage.
/// </summary>
public interface IMetadataProvider
{
    /// <summary>
    /// Imports data to SQLite.
    /// </summary>
    /// <param name="metadata"></param>
    void Import(Models.API.Metadata.MetadataDocument metadata);

    /// <summary>
    /// Determines whether metadata exists.
    /// </summary>
    /// <returns></returns>
    bool HasMetadata();

    /// <summary>
    /// Gets factions from stored metadata.
    /// </summary>
    /// <param name="includeDiscontinued"></param>
    /// <returns></returns>
    IReadOnlyList<Faction> GetFactions(bool includeDiscontinued = false);

    /// <summary>
    /// Gets faction metadata by id, if it exists.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    Faction? GetFactionById(int id);

    /// <summary>
    /// Gets weapons by filter.
    /// </summary>
    /// <param name="searchTerm"></param>
    /// <returns></returns>
    IReadOnlyList<Weapon> SearchWeaponsByName(string searchTerm);

    /// <summary>
    /// Gets skills from metadata.
    /// </summary>
    /// <returns></returns>
    IReadOnlyList<Skill> GetSkills();
}
