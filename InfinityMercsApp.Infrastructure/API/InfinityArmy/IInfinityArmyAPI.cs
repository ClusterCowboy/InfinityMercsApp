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
    Task<string> GetMetaDataAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all army data from the API.
    /// </summary>
    /// <param name="factionId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<string> GetArmyDataAsync(int factionId, CancellationToken cancellationToken = default);
}
