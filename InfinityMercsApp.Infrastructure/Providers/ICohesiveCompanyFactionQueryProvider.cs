using InfinityMercsApp.Domain.Models.Army;

namespace InfinityMercsApp.Infrastructure.Providers;

/// <summary>
/// Provides cohesive-company query inputs and merged Mercs source data across factions.
/// </summary>
public interface ICohesiveCompanyFactionQueryProvider
{
    /// <summary>
    /// Returns combined filter dictionaries and merged mercenary source entries.
    /// </summary>
    /// <param name="factionIds"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<CohesiveCompanyFactionQueryResult> GetFilterQuerySourceAsync(
        IReadOnlyCollection<int> factionIds,
        CancellationToken cancellationToken = default);
}
