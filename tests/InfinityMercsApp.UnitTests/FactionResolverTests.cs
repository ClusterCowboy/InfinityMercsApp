using System.Text.Json;
using InfinityMercsApp.Domain.Models.Metadata;
using InfinityMercsApp.Domain.Utilities;

namespace InfinityMercsApp.UnitTests;

public sealed class FactionResolverTests
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

    private static readonly Lazy<IReadOnlyList<Faction>> AllFactions = new(LoadFactionsFromMetadataJson);

    public static IEnumerable<object[]> AllFactionResolveCases =>
        AllFactions.Value.Select(faction =>
        {
            var expectedVanilla = ResolveExpectedVanillaFromDefinitions(faction, AllFactions.Value);
            return new object[] { faction.Id, faction.Name, expectedVanilla.Id, expectedVanilla.Name };
        });

    [Theory]
    [MemberData(nameof(AllFactionResolveCases))]
    public void ResolveToVanilla_UsesMetadataFactionParentDefinitions(
        int factionId,
        string factionName,
        int expectedVanillaId,
        string expectedVanillaName)
    {
        var faction = GetFactionById(factionId);
        var expectedVanilla = GetFactionById(expectedVanillaId);

        var result = FactionResolver.ResolveToVanilla(faction, AllFactions.Value);

        Assert.Equal(factionName, faction.Name);
        Assert.Equal(expectedVanillaName, expectedVanilla.Name);
        Assert.Equal(expectedVanilla.Id, result.Id);
        Assert.Equal(expectedVanilla.Name, result.Name);
    }

    private static Faction GetFactionById(int id)
    {
        return AllFactions.Value.Single(
            faction => faction.Id == id);
    }

    private static Faction ResolveExpectedVanillaFromDefinitions(
        Faction faction,
        IReadOnlyList<Faction> allFactions)
    {
        var factionsById = allFactions.ToDictionary(x => x.Id);
        var current = faction;
        var visited = new HashSet<int>();

        while (current.ParentId != 0 &&
               ResolveMetadataParentAlias(current.ParentId) != current.Id &&
               visited.Add(current.Id) &&
               factionsById.TryGetValue(ResolveMetadataParentAlias(current.ParentId), out var parent))
        {
            current = parent;
        }

        return current;
    }

    private static int ResolveMetadataParentAlias(int parentId)
    {
        return MetadataParentAliases.TryGetValue(parentId, out var alias)
            ? alias
            : parentId;
    }

    private static IReadOnlyList<Faction> LoadFactionsFromMetadataJson()
    {
        var metadataPath = ResolveRepoFilePath("InfinityMercsApp/ReferenceData/metadata.json");
        using var document = JsonDocument.Parse(File.ReadAllText(metadataPath));

        return document.RootElement
            .GetProperty("factions")
            .EnumerateArray()
            .Select(faction => new Faction
            {
                Id = faction.GetProperty("id").GetInt32(),
                ParentId = faction.TryGetProperty("parent", out var parent) ? parent.GetInt32() : 0,
                Name = faction.GetProperty("name").GetString() ?? string.Empty,
                Slug = faction.GetProperty("slug").GetString() ?? string.Empty,
                Discontinued = faction.TryGetProperty("discontinued", out var discontinued) && discontinued.GetBoolean(),
                Logo = faction.TryGetProperty("logo", out var logo) && logo.ValueKind != JsonValueKind.Null
                    ? logo.GetString()
                    : null
            })
            .ToList();
    }

    private static string ResolveRepoFilePath(string relativePath)
    {
        var directory = AppContext.BaseDirectory;
        while (!File.Exists(Path.Combine(directory, relativePath)))
        {
            var parent = Directory.GetParent(directory);
            if (parent is null)
                throw new FileNotFoundException($"Could not find {relativePath} from {AppContext.BaseDirectory}.");

            directory = parent.FullName;
        }

        return Path.Combine(directory, relativePath);
    }
}
