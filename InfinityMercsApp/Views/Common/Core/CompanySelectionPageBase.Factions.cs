using FactionRecord = InfinityMercsApp.Domain.Models.Metadata.Faction;

namespace InfinityMercsApp.Views.Common;

public abstract partial class CompanySelectionPageBase
{
    protected async Task<List<FactionRecord>> LoadFilteredFactionRecordsAsync(CancellationToken cancellationToken = default)
    {
        return await CompanySelectionFactionsWorkflow.LoadFilteredFactionRecordsAsync(
            ArmyDataService,
            FactionLogoCacheService,
            Mode,
            cancellationToken);
    }

    protected List<TFactionItem> BuildFactionSelectionItems<TFactionItem>(
        IEnumerable<FactionRecord> factions,
        Func<int, int, string, string?, string?, TFactionItem> createItem)
    {
        return CompanySelectionFactionsWorkflow.BuildFactionSelectionItems(
            factions,
            FactionLogoCacheService,
            createItem);
    }

    protected static IEnumerable<FactionRecord> CollapseContractedBackUpVariants(IEnumerable<FactionRecord> factions)
    {
        return CompanySelectionFactionsWorkflow.CollapseContractedBackUpVariants(factions);
    }

    protected static bool IsNonAlignedArmyName(string? name)
    {
        return CompanySelectionFactionsWorkflow.IsNonAlignedArmyName(name);
    }

    protected static bool IsContractedBackUpName(string? name)
    {
        return CompanySelectionFactionsWorkflow.IsContractedBackUpName(name);
    }

    protected static bool LooksAllCaps(string? value)
    {
        return CompanySelectionFactionsWorkflow.LooksAllCaps(value);
    }
}
