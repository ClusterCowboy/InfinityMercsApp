using InfinityMercsApp.Domain.Models.Metadata;

namespace InfinityMercsApp.Domain.Utilities;

public static class FactionResolver
{
    private static readonly IReadOnlyDictionary<int, int> MetadataParentAliases = new Dictionary<int, int>
    {
        [191] = 101,
        [291] = 201,
        [391] = 301,
        [491] = 401,
        [591] = 501,
        [691] = 601,
        [791] = 701,
        [891] = 801,
        [1091] = 1001,
        [1191] = 1101
    };

    /// <summary>
    /// Returns the vanilla (top-level) faction for the given faction by walking
    /// the parent chain until a faction with no parent is reached.
    /// If <paramref name="faction"/> already has no parent it is returned unchanged.
    /// </summary>
    public static Faction ResolveToVanilla(Faction faction, IReadOnlyList<Faction> allFactions)
    {
        var current = faction;
        var visited = new HashSet<int>();

        while (current.ParentId != 0 && visited.Add(current.Id))
        {
            var parentId = ResolveMetadataParentAlias(current.ParentId);
            if (parentId == current.Id)
                break;

            var parent = allFactions.FirstOrDefault(f => f.Id == parentId);
            if (parent is null)
                break;
            current = parent;
        }

        return current;
    }

    /// <summary>
    /// Builds the full set of faction names that should be used when matching
    /// stores. For each faction in <paramref name="factions"/> both the faction's
    /// own name and its vanilla parent name are included, so a sectorial player
    /// can access both sectorial-specific stores and vanilla faction stores.
    /// </summary>
    public static IReadOnlySet<string> GetExpandedFactionNames(
        IReadOnlyList<Faction> factions,
        IReadOnlyList<Faction> allFactions)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var faction in factions)
        {
            result.Add(faction.Name);
            result.Add(ResolveToVanilla(faction, allFactions).Name);
        }
        return result;
    }

    private static int ResolveMetadataParentAlias(int parentId)
    {
        return MetadataParentAliases.TryGetValue(parentId, out var alias)
            ? alias
            : parentId;
    }
}
