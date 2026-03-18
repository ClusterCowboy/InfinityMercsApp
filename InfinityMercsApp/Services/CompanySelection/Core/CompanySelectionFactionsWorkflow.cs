using System.Text.RegularExpressions;
using InfinityMercsApp.Services;
using FactionRecord = InfinityMercsApp.Domain.Models.Metadata.Faction;

namespace InfinityMercsApp.Views.Common;

internal static class CompanySelectionFactionsWorkflow
{
    internal static async Task<List<FactionRecord>> LoadFilteredFactionRecordsAsync(
        IArmyDataService armyDataService,
        FactionLogoCacheService? factionLogoCacheService,
        ArmySourceSelectionMode mode,
        CancellationToken cancellationToken = default)
    {
        var factions = armyDataService
            .GetMetadataFactions(includeDiscontinued: true, cancellationToken)
            .ToList();

        if (factionLogoCacheService is not null)
        {
            await factionLogoCacheService.CacheFactionLogosFromRecordsAsync(factions, cancellationToken);
        }

        IEnumerable<FactionRecord> filtered = factions;
        if (mode == ArmySourceSelectionMode.VanillaFactions)
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

    internal static List<TFactionItem> BuildFactionSelectionItems<TFactionItem>(
        IEnumerable<FactionRecord> factions,
        FactionLogoCacheService? factionLogoCacheService,
        Func<int, int, string, string?, string?, TFactionItem> createItem)
    {
        return factions
            .Select(faction => createItem(
                faction.Id,
                faction.ParentId,
                faction.Name,
                factionLogoCacheService?.TryGetCachedLogoPath(faction.Id),
                factionLogoCacheService?.GetPackagedFactionLogoPath(faction.Id) ?? $"SVGCache/factions/{faction.Id}.svg"))
            .ToList();
    }

    internal static IEnumerable<FactionRecord> CollapseContractedBackUpVariants(IEnumerable<FactionRecord> factions)
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

    internal static bool IsNonAlignedArmyName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        return name.Trim().Equals("Non-Aligned Armies", StringComparison.OrdinalIgnoreCase);
    }

    internal static bool IsContractedBackUpName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        var normalized = Regex.Replace(name.Trim(), @"[\s\-]+", " ").ToLowerInvariant();
        return normalized == "contracted back up";
    }

    internal static bool LooksAllCaps(string? value)
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
