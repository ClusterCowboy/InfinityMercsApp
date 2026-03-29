namespace InfinityMercsApp.Views.Common;

/// <summary>
/// Maps a unit profile's known text attributes (skills/equipment/weapons/chars)
/// to perk node ids using deterministic perk-tree node text matching.
/// </summary>
public static class CompanyPerkOwnershipResolver
{
    public static IReadOnlyList<string> ResolveOwnedPerkNodeIds(
        IEnumerable<string>? skills,
        IEnumerable<string>? equipment = null,
        IEnumerable<string>? weapons = null,
        IEnumerable<string>? characteristics = null)
    {
        var profileTerms = BuildProfileTerms(skills, equipment, weapons, characteristics);
        if (profileTerms.Count == 0)
        {
            return [];
        }

        var matchedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var tree in CompanyPerkCatalog.GetAllPerkTrees())
        {
            foreach (var node in Flatten(tree.Roots))
            {
                if (string.IsNullOrWhiteSpace(node.Id) || string.IsNullOrWhiteSpace(node.PerkText))
                {
                    continue;
                }

                var nodeTerms = ExtractPerkTerms(node.PerkText);
                if (nodeTerms.Count == 0)
                {
                    continue;
                }

                if (profileTerms.Any(profileTerm => nodeTerms.Any(nodeTerm => IsMatch(profileTerm, nodeTerm))))
                {
                    matchedIds.Add(node.Id);
                }
            }
        }

        return matchedIds
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static HashSet<string> BuildProfileTerms(
        IEnumerable<string>? skills,
        IEnumerable<string>? equipment,
        IEnumerable<string>? weapons,
        IEnumerable<string>? characteristics)
    {
        var terms = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        AddTerms(terms, skills);
        AddTerms(terms, equipment);
        AddTerms(terms, weapons);
        AddTerms(terms, characteristics);
        return terms;
    }

    private static void AddTerms(HashSet<string> terms, IEnumerable<string>? source)
    {
        if (source is null)
        {
            return;
        }

        foreach (var item in source)
        {
            if (string.IsNullOrWhiteSpace(item))
            {
                continue;
            }

            foreach (var token in SplitCandidates(item))
            {
                var normalized = Normalize(token);
                if (normalized.Length >= 3)
                {
                    terms.Add(normalized);
                }
            }
        }
    }

    private static List<string> ExtractPerkTerms(string perkText)
    {
        var terms = new List<string>();
        foreach (var token in SplitCandidates(perkText))
        {
            var baseToken = token;
            var parenIndex = baseToken.IndexOf('(');
            if (parenIndex >= 0)
            {
                baseToken = baseToken[..parenIndex];
            }

            var normalized = Normalize(baseToken);
            if (normalized.Length >= 3)
            {
                terms.Add(normalized);
            }
        }

        return terms;
    }

    private static IEnumerable<string> SplitCandidates(string text)
    {
        var commaParts = text.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in commaParts)
        {
            var orParts = part.Split(" OR ", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            foreach (var orPart in orParts)
            {
                yield return orPart;
            }
        }
    }

    private static bool IsMatch(string profileTerm, string nodeTerm)
    {
        return profileTerm.Contains(nodeTerm, StringComparison.OrdinalIgnoreCase) ||
               nodeTerm.Contains(profileTerm, StringComparison.OrdinalIgnoreCase);
    }

    private static string Normalize(string input)
    {
        var chars = input
            .Trim()
            .ToLowerInvariant()
            .Where(ch => char.IsLetterOrDigit(ch) || char.IsWhiteSpace(ch))
            .ToArray();
        return string.Join(
            ' ',
            new string(chars).Split(' ', StringSplitOptions.RemoveEmptyEntries));
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
