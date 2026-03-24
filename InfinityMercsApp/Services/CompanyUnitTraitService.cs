using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace InfinityMercsApp.Services;

public static class CompanyUnitTraitService
{
    public static (bool HasRegular, bool HasIrregular, bool HasImpetuous, bool HasTacticalAwareness) ParseUnitOrderTraits(JsonElement profileGroupsArray)
    {
        var hasRegular = false;
        var hasIrregular = false;
        var hasImpetuous = false;
        var optionsSeen = 0;
        var tacticalOptions = 0;

        if (profileGroupsArray.ValueKind != JsonValueKind.Array)
        {
            return (false, false, false, false);
        }

        foreach (var group in profileGroupsArray.EnumerateArray())
        {
            if (!group.TryGetProperty("options", out var optionsElement) || optionsElement.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var option in optionsElement.EnumerateArray())
            {
                optionsSeen++;
                var optionHasTactical = false;
                if (!option.TryGetProperty("orders", out var ordersElement) || ordersElement.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var order in ordersElement.EnumerateArray())
                {
                    if (!order.TryGetProperty("type", out var typeElement) || typeElement.ValueKind != JsonValueKind.String)
                    {
                        continue;
                    }

                    var type = typeElement.GetString();
                    if (string.IsNullOrWhiteSpace(type))
                    {
                        continue;
                    }

                    if (string.Equals(type, "REGULAR", StringComparison.OrdinalIgnoreCase))
                    {
                        hasRegular = true;
                    }
                    else if (string.Equals(type, "IRREGULAR", StringComparison.OrdinalIgnoreCase))
                    {
                        hasIrregular = true;
                    }
                    else if (string.Equals(type, "IMPETUOUS", StringComparison.OrdinalIgnoreCase))
                    {
                        hasImpetuous = true;
                    }
                    else if (string.Equals(type, "TACTICAL", StringComparison.OrdinalIgnoreCase))
                    {
                        optionHasTactical = true;
                    }
                }

                if (optionHasTactical)
                {
                    tacticalOptions++;
                }
            }
        }

        var hasUnitWideTacticalAwareness = optionsSeen > 0 && tacticalOptions == optionsSeen;
        return (hasRegular, hasIrregular, hasImpetuous, hasUnitWideTacticalAwareness);
    }

    public static (bool HasCube, bool HasCube2, bool HasHackable) ParseUnitTechTraits(
        JsonElement profileGroupsArray,
        IReadOnlyDictionary<int, string> equipLookup,
        IReadOnlyDictionary<int, string> skillsLookup,
        IReadOnlyDictionary<int, string> charsLookup)
    {
        var hasCube = false;
        var hasCube2 = false;
        var hasHackable = false;

        if (profileGroupsArray.ValueKind != JsonValueKind.Array)
        {
            return (false, false, false);
        }

        foreach (var group in profileGroupsArray.EnumerateArray())
        {
            if (group.TryGetProperty("profiles", out var profilesElement) && profilesElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var profile in profilesElement.EnumerateArray())
                {
                    ApplyTechTraitsFromContainer(profile, "equip", equipLookup, ref hasCube, ref hasCube2, ref hasHackable);
                    ApplyTechTraitsFromContainer(profile, "skills", skillsLookup, ref hasCube, ref hasCube2, ref hasHackable);
                    ApplyTechTraitsFromContainer(profile, "chars", charsLookup, ref hasCube, ref hasCube2, ref hasHackable);
                }
            }

            if (group.TryGetProperty("options", out var optionsElement) && optionsElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var option in optionsElement.EnumerateArray())
                {
                    ApplyTechTraitsFromContainer(option, "equip", equipLookup, ref hasCube, ref hasCube2, ref hasHackable);
                    ApplyTechTraitsFromContainer(option, "skills", skillsLookup, ref hasCube, ref hasCube2, ref hasHackable);
                    ApplyTechTraitsFromContainer(option, "chars", charsLookup, ref hasCube, ref hasCube2, ref hasHackable);
                }
            }
        }

        return (hasCube, hasCube2, hasHackable);
    }

    private static void ApplyTechTraitsFromContainer(
        JsonElement container,
        string propertyName,
        IReadOnlyDictionary<int, string> lookup,
        ref bool hasCube,
        ref bool hasCube2,
        ref bool hasHackable)
    {
        if (!container.TryGetProperty(propertyName, out var arr) || arr.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var entry in arr.EnumerateArray())
        {
            if (!TryParseId(entry, out var id) || !lookup.TryGetValue(id, out var name))
            {
                continue;
            }

            ApplyTechTraitName(name, ref hasCube, ref hasCube2, ref hasHackable);
        }
    }

    private static void ApplyTechTraitName(string name, ref bool hasCube, ref bool hasCube2, ref bool hasHackable)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        var normalized = NormalizeTokenText(name);

        if (Regex.IsMatch(normalized, @"\bhackable\b", RegexOptions.IgnoreCase))
        {
            hasHackable = true;
        }

        var hasNegativeCube = Regex.IsMatch(
            normalized,
            @"\b(no[\s-]*cube|without[\s-]*cube|cube[\s-]*none)\b",
            RegexOptions.IgnoreCase);

        if (hasNegativeCube)
        {
            return;
        }

        var isCube2 = Regex.IsMatch(
            normalized,
            @"\bcube\s*2(?:\.0)?\b|\bcube2(?:\.0)?\b",
            RegexOptions.IgnoreCase);

        if (isCube2)
        {
            hasCube2 = true;
            return;
        }

        if (Regex.IsMatch(normalized, @"\bcube\b", RegexOptions.IgnoreCase))
        {
            hasCube = true;
        }
    }

    private static string NormalizeTokenText(string value)
    {
        var lowered = value.ToLowerInvariant();
        var sb = new StringBuilder(lowered.Length);
        foreach (var c in lowered)
        {
            if (char.IsLetterOrDigit(c) || c == '.')
            {
                sb.Append(c);
            }
            else
            {
                sb.Append(' ');
            }
        }

        return Regex.Replace(sb.ToString(), @"\s+", " ").Trim();
    }

    private static bool TryParseId(JsonElement element, out int id)
    {
        if (element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out var numberId))
        {
            id = numberId;
            return true;
        }

        if (element.ValueKind == JsonValueKind.String &&
            int.TryParse(element.GetString(), out var stringId))
        {
            id = stringId;
            return true;
        }

        if (element.ValueKind == JsonValueKind.Object &&
            element.TryGetProperty("id", out var idElement))
        {
            return TryParseId(idElement, out id);
        }

        id = 0;
        return false;
    }
}
