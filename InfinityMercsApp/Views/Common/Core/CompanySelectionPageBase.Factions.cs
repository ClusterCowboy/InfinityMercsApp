using FactionRecord = InfinityMercsApp.Domain.Models.Metadata.Faction;

namespace InfinityMercsApp.Views.Common;

public abstract partial class CompanySelectionPageBase
{
    /// <summary>
    /// Loads the list of factions available for selection in the current mode,
    /// pre-filtering out factions that are ineligible (e.g. non-merc factions in merc-only mode).
    /// Also triggers any required logo caching as a side effect.
    /// </summary>
    protected async Task<List<FactionRecord>> LoadFilteredFactionRecordsAsync(CancellationToken cancellationToken = default)
    {
        return await CompanySelectionFactionsWorkflow.LoadFilteredFactionRecordsAsync(
            ArmyDataService,
            FactionLogoCacheService,
            Mode,
            cancellationToken);
    }

    /// <summary>
    /// Converts raw faction records into view-model items using the supplied factory function,
    /// resolving logo paths from the cache service in the process.
    /// </summary>
    protected List<TFactionItem> BuildFactionSelectionItems<TFactionItem>(
        IEnumerable<FactionRecord> factions,
        Func<int, int, string, string?, string?, TFactionItem> createItem)
    {
        return CompanySelectionFactionsWorkflow.BuildFactionSelectionItems(
            factions,
            FactionLogoCacheService,
            createItem);
    }

    /// <summary>
    /// Removes Contracted Back-Up faction variants from the list, keeping only the primary entry.
    /// Prevents duplicate entries when a faction has both a standard and a contracted variant.
    /// </summary>
    protected static IEnumerable<FactionRecord> CollapseContractedBackUpVariants(IEnumerable<FactionRecord> factions)
    {
        return CompanySelectionFactionsWorkflow.CollapseContractedBackUpVariants(factions);
    }

    /// <summary>
    /// Returns <c>true</c> when the faction name identifies a Non-Aligned Army (mercenary pool).
    /// </summary>
    protected static bool IsNonAlignedArmyName(string? name)
    {
        return CompanySelectionFactionsWorkflow.IsNonAlignedArmyName(name);
    }

    /// <summary>
    /// Returns <c>true</c> when the faction name identifies a Contracted Back-Up variant.
    /// </summary>
    protected static bool IsContractedBackUpName(string? name)
    {
        return CompanySelectionFactionsWorkflow.IsContractedBackUpName(name);
    }

    /// <summary>
    /// Returns <c>true</c> when the value appears to be an all-caps abbreviation,
    /// used to decide whether a faction name should be displayed verbatim or title-cased.
    /// </summary>
    protected static bool LooksAllCaps(string? value)
    {
        return CompanySelectionFactionsWorkflow.LooksAllCaps(value);
    }
}
