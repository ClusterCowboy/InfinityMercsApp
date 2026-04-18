using System.Text.RegularExpressions;

namespace InfinityMercsApp.Domain.Models.Perks;

public static partial class CompanyPerkCatalog
{
    private static readonly IReadOnlyList<PerkNodeList> PerkNodeListsInternal = BuildIndependentPerkNodeLists();
    private static readonly IReadOnlyDictionary<string, PerkListMetadata> PerkListMetadataById = BuildPerkListMetadataByListId();
    private static readonly IReadOnlyList<PerkListCatalogEntry> PerkListCatalogInternal = BuildPerkListCatalogEntries(PerkNodeListsInternal);
    private static readonly IReadOnlyList<CompanyTrooperPerk> AllPerksInternal = BuildPerkIndex(PerkNodeListsInternal);

    public static IReadOnlyList<CompanyTrooperPerk> AllPerks => AllPerksInternal;

    public static CompanyTrooperPerk? FindById(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        return AllPerksInternal.FirstOrDefault(x =>
            string.Equals(x.Id, id.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    public static IReadOnlyList<PerkListCatalogEntry> GetPerkListCatalogEntries(int? listRoll = null)
    {
        return PerkListCatalogInternal
            .Where(list =>
                !listRoll.HasValue ||
                !list.IsRandomlyGenerated ||
                list.ListRollRanges.Any(range => range.Contains(listRoll.Value)))
            .Select(ClonePerkListCatalogEntry)
            .ToList();
    }

    public static PerkListCatalogEntry? FindPerkListCatalogEntry(string? listNameOrId, int? listRoll = null)
    {
        return FindPerkListCatalogEntries(listNameOrId, listRoll).FirstOrDefault();
    }

    public static IReadOnlyList<PerkListCatalogEntry> FindPerkListCatalogEntries(string? listNameOrId, int? listRoll = null)
    {
        if (string.IsNullOrWhiteSpace(listNameOrId))
        {
            return [];
        }

        var normalized = listNameOrId.Trim();
        return GetPerkListCatalogEntries(listRoll)
            .Where(list =>
                string.Equals(list.Id, normalized, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(list.Name, normalized, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public static IReadOnlyList<PerkNodeList> RollRandomPerkLists(int roll)
    {
        var listIds = PerkListCatalogInternal
            .Where(list =>
                list.IsRandomlyGenerated &&
                list.ListRollRanges.Any(range => range.Contains(roll)))
            .Select(list => list.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return PerkNodeListsInternal
            .Where(list => listIds.Contains(list.ListId))
            .Select(ClonePerkNodeList)
            .ToList();
    }

    /// <summary>
    /// Builds generic <see cref="PerkNode"/> trees for each perk list.
    /// </summary>
    public static IReadOnlyList<PerkNodeList> GetPerkNodeLists(int? listRoll = null)
    {
        return PerkNodeListsInternal
            .Where(list =>
                !listRoll.HasValue ||
                !list.IsRandomlyGenerated ||
                list.ListRollRanges.Any(range => range.Contains(listRoll.Value)))
            .Select(ClonePerkNodeList)
            .ToList();
    }

    /// <summary>
    /// Returns generated <see cref="PerkNode"/> roots for a specific list id/name.
    /// </summary>
    public static IReadOnlyList<PerkNode> GetPerkNodesForList(string listNameOrId, int? listRoll = null)
    {
        return GetPerkNodeLists(listRoll)
            .Where(x =>
                string.Equals(x.ListId, listNameOrId, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(x.ListName, listNameOrId, StringComparison.OrdinalIgnoreCase))
            .SelectMany(x => x.Roots)
            .ToList();
    }

    /// <summary>
    /// Returns every currently valid perk option for a rolled track value.
    /// Overlapping roll ranges can yield multiple options.
    /// </summary>
    public static IReadOnlyList<CompanyPerkRollOption> GetValidRollOptions(
        string listName,
        int trackRoll,
        IReadOnlyDictionary<int, IReadOnlyCollection<int>>? ownedTiersByTrack = null)
    {
        return GetValidRollOptions(listName, listRoll: null, trackRoll, ownedTiersByTrack);
    }

    /// <summary>
    /// Returns every currently valid perk option for a rolled track value,
    /// constrained to list charts that match the provided list-roll value.
    /// </summary>
    public static IReadOnlyList<CompanyPerkRollOption> GetValidRollOptions(
        string listName,
        int? listRoll,
        int trackRoll,
        IReadOnlyDictionary<int, IReadOnlyCollection<int>>? ownedTiersByTrack = null)
    {
        var listCandidates = FindPerkListCatalogEntries(listName, listRoll);
        if (listCandidates.Count == 0)
        {
            return [];
        }

        var allNodeLists = GetPerkNodeLists(listRoll)
            .ToDictionary(list => list.ListId, StringComparer.OrdinalIgnoreCase);

        var options = new List<CompanyPerkRollOption>();
        foreach (var list in listCandidates)
        {
            if (!allNodeLists.TryGetValue(list.Id, out var nodeList))
            {
                continue;
            }

            var nodeLookup = FlattenPerkNodes(nodeList.Roots)
                .Where(node => TryParseNodeTrackTier(node.Id, out _, out _))
                .ToLookup(node => GetTrackTier(node.Id).Track);

            foreach (var track in list.Tracks.Where(track => track.RollRanges.Any(range => range.Contains(trackRoll))))
            {
                var ownedTiers = GetOwnedTiers(track.TrackNumber, ownedTiersByTrack);
                var nodesInTrack = nodeLookup[track.TrackNumber].ToList();
                var nodesByTier = nodesInTrack
                    .GroupBy(node => node.Tier)
                    .ToDictionary(group => group.Key, group => group.ToList());

                foreach (var tierEntry in nodesByTier.OrderBy(entry => entry.Key))
                {
                    var tier = tierEntry.Key;
                    if (ownedTiers.Contains(tier))
                    {
                        continue;
                    }

                    var requiredTier = ResolveRequiredTierFromNodes(track.TrackNumber, tier, tierEntry.Value);
                    if (!CompanyPerkProgressionService.CanAcquireTier(tier, requiredTier, ownedTiers))
                    {
                        continue;
                    }

                    var perkText = string.Join(
                        " OR ",
                        tierEntry.Value
                            .Select(node => node.Name)
                            .Where(name => !string.IsNullOrWhiteSpace(name))
                            .Distinct(StringComparer.OrdinalIgnoreCase));

                    options.Add(new CompanyPerkRollOption
                    {
                        ListId = list.Id,
                        ListName = list.Name,
                        TrackNumber = track.TrackNumber,
                        Tier = tier,
                        PerkText = perkText,
                        RequiredTier = requiredTier
                    });
                }
            }
        }

        return options;
    }

    private static int? ResolveRequiredTierFromNodes(int trackNumber, int tier, IReadOnlyCollection<PerkNode> tierNodes)
    {
        if (tier <= 1 || tierNodes.Count == 0)
        {
            return null;
        }

        var requiredTiers = new List<int>();
        foreach (var node in tierNodes)
        {
            if (string.IsNullOrWhiteSpace(node.ParentId))
            {
                continue;
            }

            if (TryParseNodeTrackTier(node.ParentId, out var parentTrack, out var parentTier) &&
                parentTrack == trackNumber &&
                parentTier < tier)
            {
                requiredTiers.Add(parentTier);
            }
        }

        if (requiredTiers.Count == 0)
        {
            return null;
        }

        return requiredTiers.Min();
    }

    private static IReadOnlyList<CompanyTrooperPerk> BuildPerkIndex(IEnumerable<PerkNodeList> nodeLists)
    {
        var indexed = new List<CompanyTrooperPerk>();
        foreach (var list in nodeLists)
        {
            foreach (var node in FlattenPerkNodes(list.Roots))
            {
                if (string.IsNullOrWhiteSpace(node.Id) || string.IsNullOrWhiteSpace(node.Name))
                {
                    continue;
                }

                if (!TryParseNodeTrackTier(node.Id, out var trackNumber, out var tierNumber))
                {
                    continue;
                }

                indexed.Add(new CompanyTrooperPerk
                {
                    Id = node.Id,
                    Name = $"{list.ListName} T{tierNumber} Track {trackNumber}",
                    Description = node.Name,
                    MaxRank = 1
                });
            }
        }

        return indexed
            .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();
    }

    private static (int Track, int Tier) GetTrackTier(string nodeId)
    {
        TryParseNodeTrackTier(nodeId, out var track, out var tier);
        return (track, tier);
    }

    private static bool TryParseNodeTrackTier(string? nodeId, out int trackNumber, out int tierNumber)
    {
        trackNumber = 0;
        tierNumber = 0;
        if (string.IsNullOrWhiteSpace(nodeId))
        {
            return false;
        }

        var match = Regex.Match(
            nodeId,
            @"-track-(?<track>\d+)-tier-(?<tier>\d+)",
            RegexOptions.IgnoreCase);
        if (!match.Success)
        {
            return false;
        }

        return int.TryParse(match.Groups["track"].Value, out trackNumber) &&
               int.TryParse(match.Groups["tier"].Value, out tierNumber);
    }

    private static IEnumerable<PerkNode> FlattenPerkNodes(IEnumerable<PerkNode> roots)
    {
        foreach (var root in roots)
        {
            yield return root;
            foreach (var child in FlattenPerkNodes(root.Children))
            {
                yield return child;
            }
        }
    }

    private static IReadOnlyList<PerkListCatalogEntry> BuildPerkListCatalogEntries(IReadOnlyList<PerkNodeList> nodeLists)
    {
        var output = new List<PerkListCatalogEntry>(nodeLists.Count);
        foreach (var nodeList in nodeLists)
        {
            PerkListMetadataById.TryGetValue(nodeList.ListId, out var metadata);
            metadata ??= new PerkListMetadata
            {
                Name = nodeList.ListName,
                IsRandomlyGenerated = nodeList.IsRandomlyGenerated
            };

            var nodes = FlattenPerkNodes(nodeList.Roots)
                .Where(node => TryParseNodeTrackTier(node.Id, out _, out _))
                .ToList();

            var trackNumbers = new HashSet<int>(nodes.Select(node => GetTrackTier(node.Id).Track));
            foreach (var trackNumber in metadata.TrackRollSpecByNumber.Keys)
            {
                trackNumbers.Add(trackNumber);
            }

            var tracks = trackNumbers
                .OrderBy(x => x)
                .Select(trackNumber =>
                {
                    metadata.TrackRollSpecByNumber.TryGetValue(trackNumber, out var trackSpec);
                    return new PerkTrackCatalogEntry
                    {
                        TrackNumber = trackNumber,
                        RollRanges = ParseRollRanges(trackSpec)
                    };
                })
                .ToList();

            output.Add(new PerkListCatalogEntry
            {
                Id = nodeList.ListId,
                Name = metadata.Name,
                IsRandomlyGenerated = metadata.IsRandomlyGenerated,
                ListRollRanges = nodeList.ListRollRanges.Count > 0
                    ? nodeList.ListRollRanges.Select(CloneRange).ToList()
                    : ParseRollRanges(metadata.ListRollSpec),
                Tracks = tracks
            });
        }

        return output;
    }

    private static CompanyPerkRollRange CloneRange(CompanyPerkRollRange range)
    {
        return new CompanyPerkRollRange
        {
            Min = range.Min,
            Max = range.Max
        };
    }

    private static PerkListCatalogEntry ClonePerkListCatalogEntry(PerkListCatalogEntry source)
    {
        return new PerkListCatalogEntry
        {
            Id = source.Id,
            Name = source.Name,
            IsRandomlyGenerated = source.IsRandomlyGenerated,
            ListRollRanges = source.ListRollRanges.Select(CloneRange).ToList(),
            Tracks = source.Tracks
                .Select(track => new PerkTrackCatalogEntry
                {
                    TrackNumber = track.TrackNumber,
                    RollRanges = track.RollRanges.Select(CloneRange).ToList()
                })
                .ToList()
        };
    }

    private static Dictionary<string, PerkListMetadata> BuildPerkListMetadataByListId()
    {
        return new Dictionary<string, PerkListMetadata>(StringComparer.OrdinalIgnoreCase)
        {
            ["mecha"] = new()
            {
                Name = "Mecha",
                IsRandomlyGenerated = false,
                ListRollSpec = null
            },
            ["initiative-1-7"] = new()
            {
                Name = "Initiative",
                IsRandomlyGenerated = true,
                ListRollSpec = "1-7",
                TrackRollSpecByNumber =
                {
                    [1] = "1-6",
                    [2] = "4-9",
                    [3] = "7-12",
                    [4] = "10-15",
                    [5] = "13-18",
                    [6] = "16-20",
                    [7] = "19-20/1-3"
                }
            },
            ["cool-5-10"] = new()
            {
                Name = "Cool",
                IsRandomlyGenerated = true,
                ListRollSpec = "5-10",
                TrackRollSpecByNumber =
                {
                    [1] = "1-4",
                    [2] = "3-6",
                    [3] = "5-8",
                    [4] = "7-10",
                    [5] = "9-12",
                    [6] = "11-14",
                    [7] = "13-16",
                    [8] = "15-18",
                    [9] = "17-20/1-2"
                }
            },
            ["body-8-13"] = new()
            {
                Name = "Body",
                IsRandomlyGenerated = true,
                ListRollSpec = "8-13",
                TrackRollSpecByNumber =
                {
                    [1] = "1-4",
                    [2] = "3-6",
                    [3] = "5-8",
                    [4] = "7-10",
                    [5] = "9-12",
                    [6] = "11-14",
                    [7] = "13-16",
                    [8] = "15-18",
                    [9] = "17-20",
                    [10] = "19-20/1-2"
                }
            },
            ["reflex-11-16"] = new()
            {
                Name = "Reflex",
                IsRandomlyGenerated = true,
                ListRollSpec = "11-16",
                TrackRollSpecByNumber =
                {
                    [1] = "1-5",
                    [2] = "4-8",
                    [3] = "7-11",
                    [4] = "9-13",
                    [5] = "12-16",
                    [6] = "14-18",
                    [7] = "17-20",
                    [8] = "19-20/1-3"
                }
            },
            ["intelligence-14-19"] = new()
            {
                Name = "Intelligence",
                IsRandomlyGenerated = true,
                ListRollSpec = "14-19",
                TrackRollSpecByNumber =
                {
                    [1] = "1-5",
                    [2] = "4-8",
                    [3] = "7-11",
                    [4] = "9-13",
                    [5] = "12-16",
                    [6] = "14-18",
                    [7] = "17-20",
                    [8] = "19-20/1-3"
                }
            },
            ["empathy-17-20-1-4"] = new()
            {
                Name = "Empathy",
                IsRandomlyGenerated = true,
                ListRollSpec = "17-20/1-4",
                TrackRollSpecByNumber =
                {
                    [1] = "1-4",
                    [2] = "3-6",
                    [3] = "5-8",
                    [4] = "7-10",
                    [5] = "9-12",
                    [6] = "11-14",
                    [7] = "13-16",
                    [8] = "15-18",
                    [9] = "17-20/1-2"
                }
            }
        };
    }

    private sealed class PerkListMetadata
    {
        public string Name { get; init; } = string.Empty;
        public bool IsRandomlyGenerated { get; init; }
        public string? ListRollSpec { get; init; }
        public Dictionary<int, string?> TrackRollSpecByNumber { get; init; } = [];
    }

    private static IReadOnlyList<PerkNodeList> BuildIndependentPerkNodeLists()
    {
        return
        [
            GenerateMechaPerkNodeList(),
            GenerateInitiativePerkNodeList(),
            GenerateCoolPerkNodeList(),
            GenerateBodyPerkNodeList(),
            GenerateReflexPerkNodeList(),
            GenerateIntelligencePerkNodeList(),
            GenerateEmpathyPerkNodeList()
        ];
    }

    internal static List<CompanyPerkRollRange> ParseRollRanges(string? spec)
    {
        var ranges = new List<CompanyPerkRollRange>();
        if (string.IsNullOrWhiteSpace(spec))
        {
            return ranges;
        }

        var parts = spec.Split('/', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
        {
            if (TryParseSingleRange(part, out var parsed))
            {
                ranges.Add(parsed);
            }
        }

        return ranges;
    }

    private static bool TryParseSingleRange(string token, out CompanyPerkRollRange range)
    {
        range = new CompanyPerkRollRange { Min = 0, Max = 0 };
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        var value = token.Trim();
        var dashIndex = value.IndexOf('-');
        if (dashIndex < 0)
        {
            if (!int.TryParse(value, out var single))
            {
                return false;
            }

            range = new CompanyPerkRollRange { Min = single, Max = single };
            return true;
        }

        var left = value[..dashIndex].Trim();
        var right = value[(dashIndex + 1)..].Trim();
        if (!int.TryParse(left, out var min) || !int.TryParse(right, out var max))
        {
            return false;
        }

        if (min > max)
        {
            (min, max) = (max, min);
        }

        range = new CompanyPerkRollRange { Min = min, Max = max };
        return true;
    }

    private static PerkNodeList ClonePerkNodeList(PerkNodeList source)
    {
        return new PerkNodeList
        {
            ListId = source.ListId,
            ListName = source.ListName,
            IsRandomlyGenerated = source.IsRandomlyGenerated,
            ListRollRanges = source.ListRollRanges.Select(CloneRange).ToList(),
            Roots = source.Roots.Select(ClonePerkNode).ToList()
        };
    }

    private static PerkNode ClonePerkNode(PerkNode source)
    {
        var clone = new PerkNode
        {
            Id = source.Id,
            Name = source.Name,
            Tier = source.Tier,
            ParentId = source.ParentId,
            IsOwned = source.IsOwned,
            MOV = source.MOV,
            CC = source.CC,
            BS = source.BS,
            WIP = source.WIP,
            ARM = source.ARM,
            BTS = source.BTS,
            S = source.S,
            SkillsEquipmentGained = source.SkillsEquipmentGained
                .Select(x => Tuple.Create(x.Item1, x.Item2))
                .ToList()
        };

        foreach (var child in source.Children)
        {
            clone.AddChild(ClonePerkNode(child));
        }

        return clone;
    }

    private static List<Tuple<string, string>> BuildSkillsEquipmentGained(string perkText)
    {
        var tuples = new List<Tuple<string, string>>();
        if (string.IsNullOrWhiteSpace(perkText))
        {
            return tuples;
        }

        foreach (var commaPart in SplitTopLevel(perkText, ','))
        {
            var orParts = SplitTopLevelOr(commaPart).ToList();
            if (orParts.Count == 0)
            {
                continue;
            }

            foreach (var raw in orParts)
            {
                var part = raw.Trim();
                if (part.Length == 0)
                {
                    continue;
                }

                var extras = new List<string>();
                foreach (Match match in Regex.Matches(part, @"\(([^()]*)\)"))
                {
                    var value = match.Groups[1].Value.Trim();
                    if (value.Length > 0)
                    {
                        extras.Add(value);
                    }
                }

                var baseName = Regex.Replace(part, @"\([^()]*\)", " ");
                baseName = Regex.Replace(baseName, @"\s+", " ").Trim(' ', ',');

                var mustBeIndex = baseName.IndexOf(" Must be ", StringComparison.OrdinalIgnoreCase);
                if (mustBeIndex >= 0)
                {
                    extras.Add(baseName[mustBeIndex..].Trim());
                    baseName = baseName[..mustBeIndex].Trim();
                }

                if (extras.Count == 0 &&
                    !baseName.StartsWith('+') &&
                    !baseName.StartsWith('-'))
                {
                    var trailingExtra = Regex.Match(baseName, @"^(?<base>.+?)\s+(?<extra>[+\-]\d.*)$");
                    if (trailingExtra.Success)
                    {
                        baseName = trailingExtra.Groups["base"].Value.Trim();
                        extras.Add(trailingExtra.Groups["extra"].Value.Trim());
                    }
                }

                if (string.IsNullOrWhiteSpace(baseName))
                {
                    baseName = part;
                }

                var extraValue = extras.Count == 0
                    ? string.Empty
                    : string.Join(", ", extras.Distinct(StringComparer.OrdinalIgnoreCase));
                tuples.Add(Tuple.Create(baseName, extraValue));
            }
        }

        return tuples;
    }

    private static IEnumerable<string> SplitTopLevel(string text, char separator)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            yield break;
        }

        var depth = 0;
        var start = 0;
        for (var i = 0; i < text.Length; i++)
        {
            var ch = text[i];
            if (ch == '(')
            {
                depth++;
            }
            else if (ch == ')')
            {
                depth = Math.Max(0, depth - 1);
            }
            else if (ch == separator && depth == 0)
            {
                var value = text[start..i].Trim();
                if (value.Length > 0)
                {
                    yield return value;
                }

                start = i + 1;
            }
        }

        var last = text[start..].Trim();
        if (last.Length > 0)
        {
            yield return last;
        }
    }

    private static IEnumerable<string> SplitTopLevelOr(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            yield break;
        }

        var depth = 0;
        var start = 0;
        for (var i = 0; i < text.Length - 3; i++)
        {
            var ch = text[i];
            if (ch == '(')
            {
                depth++;
                continue;
            }

            if (ch == ')')
            {
                depth = Math.Max(0, depth - 1);
                continue;
            }

            if (depth == 0 &&
                text[i] == ' ' &&
                text[i + 1] == 'O' &&
                text[i + 2] == 'R' &&
                text[i + 3] == ' ')
            {
                var value = text[start..i].Trim();
                if (value.Length > 0)
                {
                    yield return value;
                }

                start = i + 4;
                i += 3;
            }
        }

        var last = text[start..].Trim();
        if (last.Length > 0)
        {
            yield return last;
        }
    }

    private static HashSet<int> GetOwnedTiers(
        int trackNumber,
        IReadOnlyDictionary<int, IReadOnlyCollection<int>>? ownedTiersByTrack)
    {
        if (ownedTiersByTrack is null ||
            !ownedTiersByTrack.TryGetValue(trackNumber, out var tiers) ||
            tiers is null)
        {
            return [];
        }

        return tiers
            .Where(x => x > 0)
            .ToHashSet();
    }
}
