using InfinityMercsApp.Domain.Models.Perks;

namespace InfinityMercsApp.UnitTests;

public sealed class UnitTestPerks
{
    [Fact]
    public void GetAllPerkTrees_AllNodes_HaveUniqueStableIds()
    {
        var ids = CompanyPerkCatalog.GetAllPerkTrees()
            .SelectMany(tree => Flatten(tree.Roots))
            .Select(node => node.Id)
            .ToList();

        Assert.DoesNotContain(ids, string.IsNullOrWhiteSpace);
        Assert.Equal(ids.Count, ids.Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Fact]
    public void GetValidRollOptions_AllLists_AllRolls_ReturnsExpectedOptions()
    {
        foreach (var list in CompanyPerkCatalog.AllPerkLists)
        {
            int? listRoll = list.IsRandomlyGenerated
                ? list.ListRollRanges.First().Min
                : null;

            for (var trackRoll = 1; trackRoll <= 20; trackRoll++)
            {
                var actual = CompanyPerkCatalog
                    .GetValidRollOptions(list.Name, listRoll, trackRoll)
                    .Where(option => string.Equals(option.ListId, list.Id, StringComparison.OrdinalIgnoreCase))
                    .Select(option => (option.TrackNumber, option.Tier, option.PerkText, option.RequiredTier))
                    .OrderBy(option => option.TrackNumber)
                    .ThenBy(option => option.Tier)
                    .ToList();

                var expected = BuildExpectedOptions(list, trackRoll)
                    .OrderBy(option => option.TrackNumber)
                    .ThenBy(option => option.Tier)
                    .ToList();

                Assert.Equal(expected, actual);
            }
        }
    }

    private static List<(int TrackNumber, int Tier, string PerkText, int? RequiredTier)> BuildExpectedOptions(
        CompanyPerkListDefinition list,
        int trackRoll)
    {
        var expected = new List<(int TrackNumber, int Tier, string PerkText, int? RequiredTier)>();
        foreach (var track in list.Tracks.Where(track => track.RollRanges.Any(range => range.Contains(trackRoll))))
        {
            foreach (var tier in track.Tiers.Where(tier => !tier.IsEmpty))
            {
                var requiredTier = CompanyPerkCatalog.ResolveRequiredTier(track, tier.Tier);
                if (requiredTier.HasValue)
                {
                    continue;
                }

                expected.Add((track.TrackNumber, tier.Tier, tier.PerkText, requiredTier));
            }
        }

        return expected;
    }

    private static IEnumerable<CompanyPerkTreeNode> Flatten(IEnumerable<CompanyPerkTreeNode> roots)
    {
        var stack = new Stack<CompanyPerkTreeNode>(roots.Reverse());
        while (stack.Count > 0)
        {
            var node = stack.Pop();
            yield return node;
            for (var i = node.Children.Count - 1; i >= 0; i--)
            {
                stack.Push(node.Children[i]);
            }
        }
    }
}
