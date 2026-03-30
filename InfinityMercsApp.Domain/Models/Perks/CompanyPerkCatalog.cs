using System.Text.RegularExpressions;

namespace InfinityMercsApp.Domain.Models.Perks;

public static partial class CompanyPerkCatalog
{
    private static readonly IReadOnlyList<CompanyPerkListDefinition> PerkListsInternal =
    [
        BuildList(
            id: "mecha",
            name: "Mecha",
            isRandomlyGenerated: false,
            listRollSpec: null,
            trackRows:
            [
                (null, "S6, +1 ARM | < S7, +1 ARM, SR-1 | | < +1 ARM | < S8, +1 ARM, Baggage"),
                (null, "+3 BTS | < ECM (Total Control -3) | | < ECM (Hacker -3) | < +3 BTS"),
                (null, " | | Primary Weapon Gains Anti-Material Mode (B1 EXP) | < Buy HRMC for MULTI HMG Price | "),
                (null, "Aerial, No Cover, Super Jump (Jet Propulsion), Tech-Recovery | < Dodge (+3), Dodge (-3) | < Super Jump (3\") | < Move 8-2 | "),
                (null, " | TAG may equip two primaries | Tactical Awareness | | "),
                (null, "Ancillary: Gains any role TAG has | < Ancillary: +3 WIP | | Ancillary: Gains Explode, can self-trigger | "),
                (null, "+1 Wound Skill | | | < +1 Wound Skill | ")
            ]),
        BuildList(
            id: "initiative-1-7",
            name: "Initiative",
            isRandomlyGenerated: true,
            listRollSpec: "1-7",
            trackRows:
            [
                ("1-6", "Minelayer | Minelayer (2) | | | "),
                ("4-9", " | Combat Jump | < Combat Jump +3 | | "),
                ("7-12", " | Forward Deployment (+8\") | < Infiltration | < Infiltration (+3)"),
                ("10-15", " | Parachutist | | | < Parachutist (Deployment Zone)"),
                ("13-18", "Sapper | | | | "),
                ("16-20", "MOV 6-2 | < MOV 6-4 | | < MOV 6-6"),
                ("19-20/1-3", " | | Covering Fire | | ")
            ]),
        BuildList(
            id: "cool-5-10",
            name: "Cool",
            isRandomlyGenerated: true,
            listRollSpec: "5-10",
            trackRows:
            [
                ("1-4", "Decoy | | | | "),
                ("3-6", "Stealth | | | | "),
                ("5-8", " | Hidden Deployment | | | "),
                ("7-10", " | Mimetism (-3) | Mimetism (-6) | | "),
                ("9-12", "CC (-3) | | < CC (-6) | | "),
                ("11-14", "Martial Arts L1 | | < Martial Arts L3 | | < Martial Arts L5"),
                ("13-16", " | | +3 CC | | "),
                ("15-18", " | | Camouflage, Surprise Attack (-3) | | "),
                ("17-20/1-2", " | | | | Protheion")
            ]),
        BuildList(
            id: "body-8-13",
            name: "Body",
            isRandomlyGenerated: true,
            listRollSpec: "8-13",
            trackRows:
            [
                ("1-4", "Climbing Plus | | < Super Jump | | "),
                ("3-6", "Immunity (DA), Immunity (Shock) | < Immunity (AP) | < Immunity (ARM) OR Immunity (BTS) | | "),
                ("5-8", " | Berserk | < Berserk (+3) | | "),
                ("7-10", "Trade VITA for STR | | < REM, Remote Presence | | "),
                ("9-12", " | Regeneration | | | "),
                ("11-14", "Terrain (Total) | | | | "),
                ("13-16", " | +1 ARM | | < +1 ARM | "),
                ("15-18", " | Natural Born Warrior | | | "),
                ("17-20", " | | +1 Wounds Skill | | < +1 Wounds Skill"),
                ("19-20/1-2", " | | +2 PH + CC Attack (SR-2) | | ")
            ]),
        BuildList(
            id: "reflex-11-16",
            name: "Reflex",
            isRandomlyGenerated: true,
            listRollSpec: "11-16",
            trackRows:
            [
                ("1-5", "Triangluated Fire | | | < Marksmanship | "),
                ("4-8", "Sensor | | | | "),
                ("7-11", "Dodge (+3), Dodge (+1\") | Dodge (+6), Dodge (+2\") | < Dodge (-3) | | "),
                ("9-13", " | Sixth Sense | | | "),
                ("12-16", " | 360 Visor (Accessory) | | | "),
                ("14-18", " | Neurocinetics | | < Neurocinetics (B2 in Active) | < Total Reaction"),
                ("17-20", " | | BS Attack (-3) | < BS Attack (+1 SD) | "),
                ("19-20/1-3", " | +1 BS | | < +1 BS | ")
            ]),
        BuildList(
            id: "intelligence-14-19",
            name: "Intelligence",
            isRandomlyGenerated: true,
            listRollSpec: "14-19",
            trackRows:
            [
                ("1-5", " | | Forward Observer (+1 SD) (Role), Flash Pulse (+1 SD) | | "),
                ("4-8", "Medikit (no slot required) | | < Doctor (ReRoll -3) (Role) | < Doctor (2W) | "),
                ("7-11", "Gizmokit (no slot required) | | < Engineer (ReRoll -3) (Role) | | < Engineer (2W)"),
                ("9-13", "Hacker (Role) No device | < Hacking Device | < Upgrade: White Noise | | "),
                ("12-16", "Hacker (Role) No device | < Killer Hacking Device | < Upgrade: Trinity (-3) | | "),
                ("14-18", "Hacker (Role) No device | < EVO Hacking Device | < Network Support | | "),
                ("17-20", " | | +2 WIP | | "),
                ("19-20/1-3", " | | Non-Lethal (+1 SD) | | ")
            ]),
        BuildList(
            id: "empathy-17-20-1-4",
            name: "Empathy",
            isRandomlyGenerated: true,
            listRollSpec: "17-20/1-4",
            trackRows:
            [
                ("1-4", "Logistician, Baggage | | | | "),
                ("3-6", "Strategos L1 (Reworked) | | < Strategos L2 (Reworked) | | "),
                ("5-8", " | Counterintelligence | | | "),
                ("7-10", " | | Lieutenant (+1 Order) Must be Captain | | "),
                ("9-12", " | | Lieutenant (+1 Command Token) Must be Captain | | "),
                ("11-14", " | | Lieutenant Roll (+1 B) Must be Captain | | "),
                ("13-16", " | | Holomask | | "),
                ("15-18", " | | | | Tactical Awareness"),
                ("17-20/1-2", "Discover (+3) | < Discover (Reroll) | | | ")
            ])
    ];

    private static readonly IReadOnlyList<CompanyTrooperPerk> AllPerksInternal = BuildPerkIndex(PerkListsInternal);
    private static readonly IReadOnlyList<PerkNodeList> PerkNodeListsInternal = BuildIndependentPerkNodeLists();

    public static IReadOnlyList<CompanyTrooperPerk> AllPerks => AllPerksInternal;
    public static IReadOnlyList<CompanyPerkListDefinition> AllPerkLists => PerkListsInternal;

    public static CompanyTrooperPerk? FindById(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        return AllPerksInternal.FirstOrDefault(x =>
            string.Equals(x.Id, id.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    public static CompanyPerkListDefinition? FindList(string? nameOrId)
    {
        return FindLists(nameOrId).FirstOrDefault();
    }

    public static IReadOnlyList<CompanyPerkListDefinition> FindLists(string? nameOrId)
    {
        if (string.IsNullOrWhiteSpace(nameOrId))
        {
            return [];
        }

        var normalized = nameOrId.Trim();
        return PerkListsInternal
            .Where(x =>
                string.Equals(x.Id, normalized, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(x.Name, normalized, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public static int? ResolveRequiredTier(CompanyPerkTrackDefinition track, int tier)
    {
        if (track is null || tier <= 1 || tier > track.Tiers.Count)
        {
            return null;
        }

        var tierEntry = track.Tiers[tier - 1];
        if (tierEntry.IsEmpty || !tierEntry.RequiresPreviousTier)
        {
            return null;
        }

        for (var i = tier - 2; i >= 0; i--)
        {
            if (!track.Tiers[i].IsEmpty)
            {
                return i + 1;
            }
        }

        return null;
    }

    public static CompanyPerkTrackDefinition? RollRandomTrack(string listName, int roll)
    {
        var list = FindList(listName);
        if (list is null || !list.IsRandomlyGenerated)
        {
            return null;
        }

        return list.Tracks.FirstOrDefault(track =>
            track.RollRanges.Any(range => range.Contains(roll)));
    }

    public static CompanyPerkListDefinition? RollRandomList(int roll)
    {
        return RollRandomLists(roll).FirstOrDefault();
    }

    public static IReadOnlyList<CompanyPerkListDefinition> RollRandomLists(int roll)
    {
        return PerkListsInternal
            .Where(list =>
                list.IsRandomlyGenerated &&
                list.ListRollRanges.Any(range => range.Contains(roll)))
            .ToList();
    }

    public static IReadOnlyList<CompanyPerkTrackTree> GetPerkTrees(
        string listNameOrId,
        int? listRoll = null)
    {
        var lists = FindLists(listNameOrId);
        if (lists.Count == 0)
        {
            return [];
        }

        var trees = new List<CompanyPerkTrackTree>();
        foreach (var list in lists)
        {
            if (listRoll.HasValue &&
                list.IsRandomlyGenerated &&
                !list.ListRollRanges.Any(range => range.Contains(listRoll.Value)))
            {
                continue;
            }

            trees.AddRange(list.Tracks.Select(track => BuildTrackTree(list, track)));
        }

        return trees;
    }

    public static IReadOnlyList<CompanyPerkTrackTree> GetAllPerkTrees()
    {
        var trees = new List<CompanyPerkTrackTree>();
        foreach (var list in PerkListsInternal)
        {
            trees.AddRange(list.Tracks.Select(track => BuildTrackTree(list, track)));
        }

        return trees;
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
        var lists = FindLists(listName);
        if (lists.Count == 0)
        {
            return [];
        }

        var options = new List<CompanyPerkRollOption>();
        foreach (var list in lists)
        {
            if (listRoll.HasValue &&
                list.IsRandomlyGenerated &&
                !list.ListRollRanges.Any(range => range.Contains(listRoll.Value)))
            {
                continue;
            }

            foreach (var track in list.Tracks.Where(track =>
                         track.RollRanges.Any(range => range.Contains(trackRoll))))
            {
                var ownedTiers = GetOwnedTiers(track.TrackNumber, ownedTiersByTrack);
                foreach (var tier in track.Tiers.Where(tier => !tier.IsEmpty))
                {
                    if (ownedTiers.Contains(tier.Tier))
                    {
                        continue;
                    }

                    if (!CompanyPerkProgressionService.CanAcquireTier(track, tier.Tier, ownedTiers))
                    {
                        continue;
                    }

                    options.Add(new CompanyPerkRollOption
                    {
                        ListId = list.Id,
                        ListName = list.Name,
                        TrackNumber = track.TrackNumber,
                        Tier = tier.Tier,
                        PerkText = tier.PerkText,
                        RequiredTier = ResolveRequiredTier(track, tier.Tier)
                    });
                }
            }
        }

        return options;
    }

    private static CompanyPerkListDefinition BuildList(
        string id,
        string name,
        bool isRandomlyGenerated,
        string? listRollSpec,
        IEnumerable<(string? RollSpec, string RowSpec)> trackRows)
    {
        var tracks = new List<CompanyPerkTrackDefinition>();
        var trackNumber = 1;
        foreach (var (rollSpec, rowSpec) in trackRows)
        {
            var parsedTiers = CompanyPerkDefinitionParser.ParseTierRow(rowSpec);
            parsedTiers = parsedTiers
                .Select(tier => tier.IsEmpty
                    ? tier
                    : new CompanyPerkTierDefinition
                    {
                        Id = BuildTierId(id, trackNumber, tier.Tier),
                        Tier = tier.Tier,
                        PerkText = tier.PerkText,
                        RequiresPreviousTier = tier.RequiresPreviousTier
                    })
                .ToList();

            tracks.Add(new CompanyPerkTrackDefinition
            {
                TrackNumber = trackNumber++,
                RollRanges = CompanyPerkDefinitionParser.ParseRollRanges(rollSpec),
                Tiers = parsedTiers
            });
        }

        return new CompanyPerkListDefinition
        {
            Id = id,
            Name = name,
            IsRandomlyGenerated = isRandomlyGenerated,
            ListRollRanges = CompanyPerkDefinitionParser.ParseRollRanges(listRollSpec),
            Tracks = tracks
        };
    }

    private static IReadOnlyList<CompanyTrooperPerk> BuildPerkIndex(IEnumerable<CompanyPerkListDefinition> lists)
    {
        var indexed = new List<CompanyTrooperPerk>();
        foreach (var list in lists)
        {
            foreach (var track in list.Tracks)
            {
                foreach (var tier in track.Tiers.Where(tier => !tier.IsEmpty))
                {
                    indexed.Add(new CompanyTrooperPerk
                    {
                        Id = string.IsNullOrWhiteSpace(tier.Id)
                            ? BuildTierId(list.Id, track.TrackNumber, tier.Tier)
                            : tier.Id,
                        Name = $"{list.Name} T{tier.Tier} Track {track.TrackNumber}",
                        Description = tier.PerkText,
                        MaxRank = 1
                    });
                }
            }
        }

        return indexed;
    }

    private static string BuildTierId(string listId, int trackNumber, int tier)
    {
        return $"{listId}-track-{trackNumber}-tier-{tier}";
    }

    private static CompanyPerkTrackTree BuildTrackTree(
        CompanyPerkListDefinition list,
        CompanyPerkTrackDefinition track)
    {
        var nodesByTier = track.Tiers
            .Where(tier => !tier.IsEmpty)
            .ToDictionary(
                tier => tier.Tier,
                tier => new CompanyPerkTreeNode
                {
                    Id = string.IsNullOrWhiteSpace(tier.Id)
                        ? BuildTierId(list.Id, track.TrackNumber, tier.Tier)
                        : tier.Id,
                    Tier = tier.Tier,
                    PerkText = tier.PerkText,
                    RequiredTier = ResolveRequiredTier(track, tier.Tier)
                });

        var roots = new List<CompanyPerkTreeNode>();
        foreach (var node in nodesByTier.Values.OrderBy(node => node.Tier))
        {
            if (node.RequiredTier.HasValue &&
                nodesByTier.TryGetValue(node.RequiredTier.Value, out var parent))
            {
                parent.Children.Add(node);
                continue;
            }

            roots.Add(node);
        }

        return new CompanyPerkTrackTree
        {
            ListId = list.Id,
            ListName = list.Name,
            TrackNumber = track.TrackNumber,
            RollRanges = [.. track.RollRanges],
            Roots = roots
        };
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

    private static PerkNodeList ClonePerkNodeList(PerkNodeList source)
    {
        return new PerkNodeList
        {
            ListId = source.ListId,
            ListName = source.ListName,
            IsRandomlyGenerated = source.IsRandomlyGenerated,
            ListRollRanges = source.ListRollRanges.Select(range => new CompanyPerkRollRange
            {
                Min = range.Min,
                Max = range.Max
            }).ToList(),
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
