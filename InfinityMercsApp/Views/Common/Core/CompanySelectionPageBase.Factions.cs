using System.Text.RegularExpressions;
using FactionRecord = InfinityMercsApp.Domain.Models.Metadata.Faction;

namespace InfinityMercsApp.Views.Common;

public abstract partial class CompanySelectionPageBase
{
    protected async Task<List<FactionRecord>> LoadFilteredFactionRecordsAsync(CancellationToken cancellationToken = default)
    {
        var factions = ArmyDataService
            .GetMetadataFactions(includeDiscontinued: true, cancellationToken)
            .ToList();

        if (FactionLogoCacheService is not null)
        {
            await FactionLogoCacheService.CacheFactionLogosFromRecordsAsync(factions, cancellationToken);
        }

        IEnumerable<FactionRecord> filtered = factions;
        if (Mode == ArmySourceSelectionMode.VanillaFactions)
        {
            filtered = filtered.Where(x => x.Id == x.ParentId);
        }
        else
        {
            filtered = filtered.Where(x => x.Id != x.ParentId);
        }

        filtered = filtered.Where(x => !IsNonAlignedArmyName(x.Name));
        filtered = CollapseContractedBackUpVariants(filtered);
        return filtered.OrderBy(x => x.Name).ToList();
    }

    protected List<TFactionItem> BuildFactionSelectionItems<TFactionItem>(
        IEnumerable<FactionRecord> factions,
        Func<int, int, string, string?, string?, TFactionItem> createItem)
    {
        return factions
            .Select(faction => createItem(
                faction.Id,
                faction.ParentId,
                faction.Name,
                FactionLogoCacheService?.TryGetCachedLogoPath(faction.Id),
                FactionLogoCacheService?.GetPackagedFactionLogoPath(faction.Id) ?? $"SVGCache/factions/{faction.Id}.svg"))
            .ToList();
    }

    protected static IEnumerable<FactionRecord> CollapseContractedBackUpVariants(IEnumerable<FactionRecord> factions)
    {
        var list = factions.ToList();
        var contracted = list
            .Where(x => IsContractedBackUpName(x.Name))
            .ToList();

        if (contracted.Count <= 1)
        {
            return list;
        }

        var preferred = contracted.FirstOrDefault(x => !LooksAllCaps(x.Name))
            ?? contracted.First();

        return list.Where(x => !IsContractedBackUpName(x.Name) || x.Id == preferred.Id);
    }

    protected static bool IsNonAlignedArmyName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        return name.Trim().Equals("Non-Aligned Armies", StringComparison.OrdinalIgnoreCase);
    }

    protected static bool IsContractedBackUpName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        var normalized = Regex.Replace(name.Trim(), @"[\s\-]+", " ").ToLowerInvariant();
        return normalized == "contracted back up";
    }

    protected static bool LooksAllCaps(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var letters = value.Where(char.IsLetter).ToArray();
        if (letters.Length == 0)
        {
            return false;
        }

        return letters.All(char.IsUpper);
    }
}
