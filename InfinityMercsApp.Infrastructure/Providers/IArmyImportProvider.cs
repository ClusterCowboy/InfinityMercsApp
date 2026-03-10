using InfinityMercsApp.Infrastructure.Models.API.Army;

namespace InfinityMercsApp.Infrastructure.Providers;

/// <summary>
/// Handles importing Army data and parsing it into structured objects.
/// </summary>
public interface IArmyImportProvider
{
    /// <summary>
    /// Imports Army data from the Army API.
    /// </summary>
    /// <param name="factionId"></param>
    /// <param name="apiFaction"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task ImportAsync(int factionId, Faction apiFaction, CancellationToken cancellationToken = default);
}
