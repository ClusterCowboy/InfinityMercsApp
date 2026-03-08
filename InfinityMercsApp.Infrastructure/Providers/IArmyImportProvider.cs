namespace InfinityMercsApp.Infrastructure.Providers;

/// <summary>
/// Handles importing Army data and parsing it into structured objects.
/// </summary>
public interface IArmyImportProvider
{
    /// <summary>
    /// Imports Army data from JSON.
    /// </summary>
    /// <param name="factionId"></param>
    /// <param name="json"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task ImportFactionArmyFromJsonAsync(int factionId, string json, CancellationToken cancellationToken = default);

    /// <summary>
    /// Imports Army data from a file.
    /// </summary>
    /// <param name="factionId"></param>
    /// <param name="filePath"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task ImportFactionArmyFromFileAsync(int factionId, string filePath, CancellationToken cancellationToken = default);
}
