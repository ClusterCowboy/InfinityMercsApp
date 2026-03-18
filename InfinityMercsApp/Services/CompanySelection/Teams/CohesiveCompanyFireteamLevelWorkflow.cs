using System.Text.RegularExpressions;

namespace InfinityMercsApp.Views.Common;

internal static class CohesiveCompanyFireteamLevelWorkflow
{
    // Cumulative bonuses indexed by fireteam level.
    // Level 0 = no fireteam; level 1 = formed but no bonus yet.
    // Levels 2–6 each add one bonus; GetBonusText returns all bonuses up to the current level.
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

    // Returns a comma-separated string of all bonuses accumulated up to and including the given level.
    // Returns null when the level carries no bonus text (level 0, 1, or out of range).
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

    // Holds the normalised name keys and optionally the resolved unit/faction IDs for one
    // allowed-profile slot in a fireteam listing. Used by all matching and counting methods.
    private sealed class AllowedProfileDescriptor
    {
        // Normalised name keys derived from the profile's display name, including aliases
        // extracted from parentheses and simple singular/plural variants.
        public required HashSet<string> CountAsNames { get; init; }

        // Unit ID resolved by CompanyTeamMatchingWorkflow at build time. Present only when
        // the profile was successfully matched to a specific unit in the faction data.
        public required int? ResolvedUnitId { get; init; }

        // Faction ID paired with ResolvedUnitId. Both must be set for an ID-based match.
        public required int? ResolvedSourceFactionId { get; init; }
    }

    // DEFERRED — original level evaluator.
    // Groups matching roster entries by their normalised name key and returns the highest
    // per-group count. Represents "how many of the same unit type are present."
    // Not used by the active level evaluation; kept for potential future use.
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

            // Collect all canonical count-as keys from every matching descriptor.
            // Falls back to the entry's own name keys if no descriptor provides explicit keys.
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

    // Active level evaluator.
    // Iterates roster entries in order and greedily assigns each to the first matching
    // allowed-profile slot that still has remaining capacity (as defined by each profile's Max).
    // The count of successfully assigned entries is the fireteam level.
    // Entries that match no profile, or match only profiles already at capacity, do not count.
    // Uses the same slot-filling algorithm as EvaluateIrregularStatus.
    internal static int EvaluateLevelByCapacity<TEntry, TAllowedProfile>(
        IReadOnlyList<TEntry> entries,
        IReadOnlyList<TAllowedProfile> allowedProfiles,
        Func<TEntry, string?> readEntryBaseUnitName,
        Func<TEntry, string?> readEntryName,
        Func<TEntry, int> readEntrySourceUnitId,
        Func<TEntry, int> readEntrySourceFactionId,
        Func<TAllowedProfile, string?> readAllowedName,
        Func<TAllowedProfile, int?> readAllowedResolvedUnitId,
        Func<TAllowedProfile, int?> readAllowedResolvedSourceFactionId,
        Func<TAllowedProfile, string> readAllowedMax)
        where TEntry : class
        where TAllowedProfile : class
    {
        if (entries.Count == 0 || allowedProfiles.Count == 0)
        {
            return 0;
        }

        // Build descriptors and their initial slot capacities in parallel lists so indices align.
        var descriptors = new List<AllowedProfileDescriptor>(allowedProfiles.Count);
        var capacities = new List<int>(allowedProfiles.Count);
        foreach (var allowed in allowedProfiles)
        {
            var name = readAllowedName(allowed);
            var countAsNames = BuildAllowedNameKeys(name);
            var resolvedUnitId = readAllowedResolvedUnitId(allowed);
            var resolvedSourceFactionId = readAllowedResolvedSourceFactionId(allowed);

            // Skip profiles that carry neither a usable name nor resolved IDs — they cannot match.
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
            return 0;
        }

        var remainingSlots = capacities.ToArray();
        var count = 0;

        foreach (var entry in entries)
        {
            var entryNameKeys = BuildEntryNameKeys(readEntryBaseUnitName(entry), readEntryName(entry));
            var entryUnitId = readEntrySourceUnitId(entry);
            var entryFactionId = readEntrySourceFactionId(entry);

            // Find the first matching descriptor that still has a free slot.
            var firstWithCapacity = -1;
            for (var i = 0; i < descriptors.Count; i++)
            {
                if (!MatchesAllowedDescriptor(descriptors[i], entryNameKeys, entryUnitId, entryFactionId))
                {
                    continue;
                }

                if (firstWithCapacity < 0 && remainingSlots[i] > 0)
                {
                    firstWithCapacity = i;
                }
            }

            if (firstWithCapacity >= 0)
            {
                remainingSlots[firstWithCapacity]--;
                count++;
            }
        }

        return count;
    }

    // DEFERRED — simple membership evaluator.
    // Counts every roster entry that matches any allowed profile descriptor, with no regard for
    // each profile's Max slot limit. Not used by the active level evaluation; kept for potential
    // future use.
    internal static int EvaluateLevelByMembership<TEntry, TAllowedProfile>(
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

        var count = 0;
        foreach (var entry in entries)
        {
            var entryNameKeys = BuildEntryNameKeys(readEntryBaseUnitName(entry), readEntryName(entry));
            var entryUnitId = readEntrySourceUnitId(entry);
            var entryFactionId = readEntrySourceFactionId(entry);
            if (descriptors.Any(d => MatchesAllowedDescriptor(d, entryNameKeys, entryUnitId, entryFactionId)))
            {
                count++;
            }
        }

        return count;
    }

    // Returns true if the roster entry matches the given allowed-profile descriptor.
    // Prefers an exact unit/faction ID match when both are resolved; falls back to name-key
    // intersection when IDs are unavailable.
    private static bool MatchesAllowedDescriptor(
        AllowedProfileDescriptor descriptor,
        IReadOnlyCollection<string> entryNameKeys,
        int entrySourceUnitId,
        int entrySourceFactionId)
    {
        // ID match is authoritative and preferred over name matching.
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

    // Converts a list of allowed profiles into AllowedProfileDescriptor instances.
    // Profiles that provide neither usable name keys nor resolved IDs are skipped,
    // as they cannot be matched against any roster entry.
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

    // Derives the full set of normalised name keys from an allowed-profile display name.
    // The base key is the name with any parenthetical groups stripped.
    // Additional keys are generated for each alias found inside parentheses,
    // split on common delimiter characters (, / ; |).
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

    // Builds the set of normalised name keys for a roster entry.
    // Both the base unit name (the source unit's canonical name) and the profile/option name
    // are included so that either can match against an allowed-profile descriptor.
    private static HashSet<string> BuildEntryNameKeys(string? baseUnitName, string? entryName)
    {
        var keys = new HashSet<string>(StringComparer.Ordinal);
        AddNameKey(keys, baseUnitName);
        AddNameKey(keys, entryName);
        return keys;
    }

    // Normalises a single name value and adds it to the target set, along with any simple
    // singular/plural variants. No-ops if the normalised result is blank.
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

    // For each roster entry, determines whether it is irregular with respect to the tracked
    // fireteam's allowed profiles. Uses the same greedy slot-filling approach as
    // EvaluateLevelByCapacity: entries are processed in order and each claims the first matching
    // profile slot that still has remaining capacity. An entry is irregular when it matches no
    // profile at all, or when every matching profile is already at its Max count.
    // Results are written back via the setIrregular callback.
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

        // No allowed profiles means every entry is irregular by definition.
        if (allowedProfiles.Count == 0)
        {
            foreach (var entry in entries)
            {
                setIrregular(entry, true);
            }

            return;
        }

        // Build descriptors and their initial slot capacities in parallel lists so indices align.
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

        // All profiles were skipped (none had usable keys or IDs) — treat all entries as irregular.
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
                // Entry's unit type does not appear in any allowed profile.
                setIrregular(entry, true);
            }
            else if (firstWithCapacity >= 0)
            {
                // Entry's unit type is allowed and a slot is still free — claim it.
                remainingSlots[firstWithCapacity]--;
                setIrregular(entry, false);
            }
            else
            {
                // Entry's unit type is allowed but every matching profile is at capacity.
                setIrregular(entry, true);
            }
        }
    }

    // Parses the Max string from a fireteam profile slot into a slot-count integer.
    // "*" or blank means unbounded (treated as a very large finite number to avoid overflow).
    // Non-positive or unparseable values resolve to 0 (no slots available).
    private static int ParseMaxSlots(string? maxStr)
    {
        if (string.IsNullOrWhiteSpace(maxStr) || maxStr.Trim() == "*")
        {
            return int.MaxValue / 2;
        }

        return int.TryParse(maxStr.Trim(), out var n) && n > 0 ? n : 0;
    }

    // Yields singular/plural variants of a normalised name key to improve matching tolerance.
    // If the key ends with 'S', yields the singular form (strip trailing S).
    // Otherwise yields the plural form (append S).
    // Single-character keys are skipped to avoid noise.
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
