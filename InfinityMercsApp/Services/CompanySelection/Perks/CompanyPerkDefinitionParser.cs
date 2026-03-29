namespace InfinityMercsApp.Views.Common;

internal static class CompanyPerkDefinitionParser
{
    private const int MaxTiers = 5;

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

    internal static List<CompanyPerkTierDefinition> ParseTierRow(string rowSpec)
    {
        var tokens = rowSpec.Split('|');
        var tiers = new List<CompanyPerkTierDefinition>(MaxTiers);

        for (var tier = 1; tier <= MaxTiers; tier++)
        {
            var token = tier - 1 < tokens.Length
                ? tokens[tier - 1].Trim()
                : string.Empty;

            var requiresPrevious = false;
            if (tier > 1 && token.StartsWith('<'))
            {
                requiresPrevious = true;
                token = token[1..].TrimStart();
            }

            if (string.IsNullOrWhiteSpace(token))
            {
                token = string.Empty;
                requiresPrevious = false;
            }

            // Rule: tier 1 never has prerequisites.
            if (tier == 1)
            {
                requiresPrevious = false;
            }

            tiers.Add(new CompanyPerkTierDefinition
            {
                Tier = tier,
                PerkText = token,
                RequiresPreviousTier = requiresPrevious
            });
        }

        return tiers;
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
}
