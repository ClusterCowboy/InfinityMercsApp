using System.Text.RegularExpressions;

namespace InfinityMercsApp.Views.Common;

internal static class CohesiveCompanyFireteamLevelWorkflow
{
    private static readonly string?[] LevelBonuses =
    [
        null,               // 0 — no fireteam
        null,               // 1 — no bonus
        "BS Attack (+1 SD)",
        "+3 Discover and +1 Dodge",
        "+1 BS",
        "Sixth Sense",
        "+1 WIP",
    ];

    internal static string? GetBonusText(int level)
    {
        if (level <= 0 || level >= LevelBonuses.Length)
        {
            return null;
        }

        var bonuses = LevelBonuses[1..Math.Min(level + 1, LevelBonuses.Length)]
            .Where(b => b is not null)
            .ToList();

        return bonuses.Count > 0 ? string.Join(", ", bonuses) : null;
    }

    private sealed class AllowedProfileDescriptor
    {
        public required HashSet<string> CountAsNames { get; init; }
        public required int? ResolvedUnitId { get; init; }
        public required int? ResolvedSourceFactionId { get; init; }
    }

    internal static int EvaluateLevel<TEntry, TAllowedProfile>(
        IReadOnlyList<TEntry> entries,
        IReadOnlyList<TAllowedProfile> allowedProfiles,
        Func<TEntry, string?> readEntryBaseUnitName,
        Func<TEntry, string?> readEntryName,
        Func<TEntry, int> readEntrySourceUnitId,
        Func<TEntry, int> readEntrySourceFactionId,
        Func<TAllowedProfile, string?> readAllowedName,
        Func<TAllowedProfile, int?> readAllowedResolvedUnitId,
        Func<TAllowedProfile, int?> readAllowedResolvedSourceFactionId)
        where TEntry : class
        where TAllowedProfile : class
    {
        if (entries.Count == 0 || allowedProfiles.Count == 0)
        {
            return 0;
        }

        var descriptors = BuildAllowedProfileDescriptors(
            allowedProfiles,
            readAllowedName,
            readAllowedResolvedUnitId,
            readAllowedResolvedSourceFactionId);
        if (descriptors.Count == 0)
        {
            return 0;
        }

        var countsByName = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var entry in entries)
        {
            var entryNameKeys = BuildEntryNameKeys(readEntryBaseUnitName(entry), readEntryName(entry));
            var matchingDescriptors = descriptors
                .Where(x => MatchesAllowedDescriptor(
                    x,
                    entryNameKeys,
                    readEntrySourceUnitId(entry),
                    readEntrySourceFactionId(entry)))
                .ToList();
            if (matchingDescriptors.Count == 0)
            {
                continue;
            }

            var countAsNamesForEntry = new HashSet<string>(StringComparer.Ordinal);
            foreach (var descriptor in matchingDescriptors)
            {
                foreach (var key in descriptor.CountAsNames)
                {
                    countAsNamesForEntry.Add(key);
                }
            }

            if (countAsNamesForEntry.Count == 0)
            {
                foreach (var key in entryNameKeys)
                {
                    countAsNamesForEntry.Add(key);
                }
            }

            foreach (var key in countAsNamesForEntry)
            {
                countsByName[key] = countsByName.TryGetValue(key, out var existing) ? existing + 1 : 1;
            }
        }

        return countsByName.Count == 0 ? 0 : countsByName.Values.Max();
    }

    private static bool MatchesAllowedDescriptor(
        AllowedProfileDescriptor descriptor,
        IReadOnlyCollection<string> entryNameKeys,
        int entrySourceUnitId,
        int entrySourceFactionId)
    {
        if (descriptor.ResolvedUnitId.HasValue &&
            descriptor.ResolvedSourceFactionId.HasValue &&
            descriptor.ResolvedUnitId.Value == entrySourceUnitId &&
            descriptor.ResolvedSourceFactionId.Value == entrySourceFactionId)
        {
            return true;
        }

        if (entryNameKeys.Count == 0 || descriptor.CountAsNames.Count == 0)
        {
            return false;
        }

        return entryNameKeys.Any(descriptor.CountAsNames.Contains);
    }

    private static List<AllowedProfileDescriptor> BuildAllowedProfileDescriptors<TAllowedProfile>(
        IReadOnlyList<TAllowedProfile> allowedProfiles,
        Func<TAllowedProfile, string?> readAllowedName,
        Func<TAllowedProfile, int?> readAllowedResolvedUnitId,
        Func<TAllowedProfile, int?> readAllowedResolvedSourceFactionId)
        where TAllowedProfile : class
    {
        var descriptors = new List<AllowedProfileDescriptor>(allowedProfiles.Count);
        foreach (var allowed in allowedProfiles)
        {
            var name = readAllowedName(allowed);
            var countAsNames = BuildAllowedNameKeys(name);
            var resolvedUnitId = readAllowedResolvedUnitId(allowed);
            var resolvedSourceFactionId = readAllowedResolvedSourceFactionId(allowed);
            if (countAsNames.Count == 0 && (!resolvedUnitId.HasValue || !resolvedSourceFactionId.HasValue))
            {
                continue;
            }

            descriptors.Add(new AllowedProfileDescriptor
            {
                CountAsNames = countAsNames,
                ResolvedUnitId = resolvedUnitId,
                ResolvedSourceFactionId = resolvedSourceFactionId
            });
        }

        return descriptors;
    }

    private static HashSet<string> BuildAllowedNameKeys(string? rawName)
    {
        var keys = new HashSet<string>(StringComparer.Ordinal);
        AddNameKey(keys, Regex.Replace(rawName ?? string.Empty, @"\([^)]*\)", " "));

        foreach (Match match in Regex.Matches(rawName ?? string.Empty, @"\(([^)]*)\)"))
        {
            var aliases = match.Groups[1].Value.Split(
                [',', '/', ';', '|'],
                StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            foreach (var alias in aliases)
            {
                AddNameKey(keys, alias);
            }
        }

        return keys;
    }

    private static HashSet<string> BuildEntryNameKeys(string? baseUnitName, string? entryName)
    {
        var keys = new HashSet<string>(StringComparer.Ordinal);
        AddNameKey(keys, baseUnitName);
        AddNameKey(keys, entryName);
        return keys;
    }

    private static void AddNameKey(HashSet<string> target, string? value)
    {
        var normalized = CompanyTeamMatchingWorkflow.NormalizeTeamUnitName(value);
        if (!string.IsNullOrWhiteSpace(normalized))
        {
            target.Add(normalized);
            foreach (var variant in BuildSimplePluralVariants(normalized))
            {
                target.Add(variant);
            }
        }
    }

    internal static void EvaluateIrregularStatus<TEntry, TAllowedProfile>(
        IReadOnlyList<TEntry> entries,
        IReadOnlyList<TAllowedProfile> allowedProfiles,
        Func<TEntry, string?> readEntryBaseUnitName,
        Func<TEntry, string?> readEntryName,
        Func<TEntry, int> readEntrySourceUnitId,
        Func<TEntry, int> readEntrySourceFactionId,
        Func<TAllowedProfile, string?> readAllowedName,
        Func<TAllowedProfile, int?> readAllowedResolvedUnitId,
        Func<TAllowedProfile, int?> readAllowedResolvedSourceFactionId,
        Func<TAllowedProfile, string> readAllowedMax,
        Action<TEntry, bool> setIrregular)
        where TEntry : class
        where TAllowedProfile : class
    {
        if (entries.Count == 0)
        {
            return;
        }

        if (allowedProfiles.Count == 0)
        {
            foreach (var entry in entries)
            {
                setIrregular(entry, true);
            }

            return;
        }

        var descriptors = new List<AllowedProfileDescriptor>(allowedProfiles.Count);
        var capacities = new List<int>(allowedProfiles.Count);
        foreach (var allowed in allowedProfiles)
        {
            var name = readAllowedName(allowed);
            var countAsNames = BuildAllowedNameKeys(name);
            var resolvedUnitId = readAllowedResolvedUnitId(allowed);
            var resolvedSourceFactionId = readAllowedResolvedSourceFactionId(allowed);
            if (countAsNames.Count == 0 && (!resolvedUnitId.HasValue || !resolvedSourceFactionId.HasValue))
            {
                continue;
            }

            descriptors.Add(new AllowedProfileDescriptor
            {
                CountAsNames = countAsNames,
                ResolvedUnitId = resolvedUnitId,
                ResolvedSourceFactionId = resolvedSourceFactionId
            });
            capacities.Add(ParseMaxSlots(readAllowedMax(allowed)));
        }

        if (descriptors.Count == 0)
        {
            foreach (var entry in entries)
            {
                setIrregular(entry, true);
            }

            return;
        }

        var remainingSlots = capacities.ToArray();

        foreach (var entry in entries)
        {
            var entryNameKeys = BuildEntryNameKeys(readEntryBaseUnitName(entry), readEntryName(entry));
            var entryUnitId = readEntrySourceUnitId(entry);
            var entryFactionId = readEntrySourceFactionId(entry);
            var firstWithCapacity = -1;
            var hasAnyMatch = false;

            for (var i = 0; i < descriptors.Count; i++)
            {
                if (!MatchesAllowedDescriptor(descriptors[i], entryNameKeys, entryUnitId, entryFactionId))
                {
                    continue;
                }

                hasAnyMatch = true;
                if (firstWithCapacity < 0 && remainingSlots[i] > 0)
                {
                    firstWithCapacity = i;
                }
            }

            if (!hasAnyMatch)
            {
                setIrregular(entry, true);
            }
            else if (firstWithCapacity >= 0)
            {
                remainingSlots[firstWithCapacity]--;
                setIrregular(entry, false);
            }
            else
            {
                setIrregular(entry, true);
            }
        }
    }

    private static int ParseMaxSlots(string? maxStr)
    {
        if (string.IsNullOrWhiteSpace(maxStr) || maxStr.Trim() == "*")
        {
            return int.MaxValue / 2;
        }

        return int.TryParse(maxStr.Trim(), out var n) && n > 0 ? n : 0;
    }

    private static IEnumerable<string> BuildSimplePluralVariants(string normalized)
    {
        if (string.IsNullOrWhiteSpace(normalized) || normalized.Length <= 1)
        {
            yield break;
        }

        if (normalized.EndsWith('S'))
        {
            var singular = normalized[..^1];
            if (!string.IsNullOrWhiteSpace(singular))
            {
                yield return singular;
            }

            yield break;
        }

        yield return $"{normalized}S";
    }
}
