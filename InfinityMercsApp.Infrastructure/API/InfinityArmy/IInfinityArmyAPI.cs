using InfinityMercsApp.Infrastructure.Models.API.Metadata;

namespace InfinityMercsApp.Infrastructure.API.InfinityArmy;

/// <summary>
/// An interface to handle interactions with the Infinity Army API
/// </summary>
public interface IInfinityArmyAPI
{
    /// <summary>
    /// Gets all metadata from the API.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<MetadataDocument?> GetMetaDataAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all army data from the API.
    /// </summary>
    /// <param name="factionId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<Models.API.Army.Faction?> GetArmyDataAsync(int factionId, CancellationToken cancellationToken = default);
}
