using InfinityMercsApp.Domain.Models.DataImport;

namespace InfinityMercsApp.Services;

/// <summary>
/// Handles importing of data.
/// </summary>
public interface IImportService
{
    /// <summary>
    /// Imports metadata, emitting an IAsyncEnumerable of strings as an update.
    /// </summary>
    /// <returns></returns>
    IAsyncEnumerable<SuccessWithStringResult> ImportMetadataAsync();

    /// <summary>
    /// Imports army data for a faction, emitting an IAsyncEnumerable of strings as an update.
    /// </summary>
    /// <param name="factionId"></param>
    /// <returns></returns>
    IAsyncEnumerable<SuccessWithStringResult> ImportFactionAsync(string factionId);

    /// <summary>
    /// Imports all data, emitting an IAsyncEnumerable of strings as an update.
    /// </summary>
    /// <returns></returns>
    IAsyncEnumerable<SuccessWithStringResult> ImportAllDataAsync();
}
