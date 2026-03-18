using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace InfinityMercsApp.Services;

internal sealed class CompanyPeripheralProfileSelectionResult
{
    public required string PeripheralName { get; init; }
    public required JsonElement PeripheralProfile { get; init; }
}

internal sealed class CompanyPeripheralProfileSelectionRequest
{
    public required JsonElement ProfileGroupsRoot { get; init; }
    public required IReadOnlyDictionary<int, string> PeripheralLookup { get; init; }
    public required bool ForceLieutenant { get; init; }
    public required bool LieutenantOnlyUnits { get; init; }
    public required IReadOnlyDictionary<int, string> SkillsLookup { get; init; }
    public required Func<JsonElement, IReadOnlyDictionary<int, string>, bool> IsLieutenantOption { get; init; }
    public required CompanyUnitDetailDisplayNameContext.TryParseIdDelegate TryParseId { get; init; }
}

internal static class CompanyPeripheralProfileSelectionService
{
    public static CompanyPeripheralProfileSelectionResult? FindFirstVisiblePeripheralProfile(
        CompanyPeripheralProfileSelectionRequest request)
    {
        if (request.ProfileGroupsRoot.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var hasControllerGroups = request.ProfileGroupsRoot.EnumerateArray()
            .Any(group => CompanyProfileOptionService.IsControllerGroup(request.ProfileGroupsRoot, group));

        foreach (var group in request.ProfileGroupsRoot.EnumerateArray())
        {
            if (hasControllerGroups && !CompanyProfileOptionService.IsControllerGroup(request.ProfileGroupsRoot, group))
            {
                continue;
            }

            if (!group.TryGetProperty("options", out var optionsElement) || optionsElement.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var option in optionsElement.EnumerateArray())
            {
                if (IsPositiveSwc(CompanyProfileOptionService.ReadOptionSwc(option)))
                {
                    continue;
                }

                if (!request.ForceLieutenant && request.LieutenantOnlyUnits && !request.IsLieutenantOption(option, request.SkillsLookup))
                {
                    continue;
                }

                foreach (var entry in CompanyProfileOptionService.GetDisplayPeripheralEntriesForOption(request.ProfileGroupsRoot, group, option))
                {
                    if (!request.TryParseId(entry, out var peripheralId))
                    {
                        continue;
                    }

                    var peripheralName = request.PeripheralLookup.TryGetValue(peripheralId, out var resolvedName)
                        ? resolvedName
                        : peripheralId.ToString(CultureInfo.InvariantCulture);

                    if (!TryFindPeripheralStatElement(request.ProfileGroupsRoot, peripheralName, out var peripheralProfile))
                    {
                        continue;
                    }

                    return new CompanyPeripheralProfileSelectionResult
                    {
                        PeripheralName = peripheralName,
                        PeripheralProfile = peripheralProfile
                    };
                }
            }
        }

        return null;
    }

    public static bool TryFindPeripheralStatElement(
        JsonElement profileGroupsRoot,
        string peripheralName,
        out JsonElement profile)
    {
        profile = default;
        var expected = NormalizeComparisonToken(peripheralName);
        if (string.IsNullOrWhiteSpace(expected) || profileGroupsRoot.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        foreach (var group in profileGroupsRoot.EnumerateArray())
        {
            var groupIsc = group.TryGetProperty("isc", out var groupIscElement) && groupIscElement.ValueKind == JsonValueKind.String
                ? groupIscElement.GetString() ?? string.Empty
                : string.Empty;
            var normalizedGroupIsc = NormalizeComparisonToken(groupIsc);

            if (group.TryGetProperty("profiles", out var profilesElement) && profilesElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var candidate in profilesElement.EnumerateArray())
                {
                    var profileName = candidate.TryGetProperty("name", out var nameElement) && nameElement.ValueKind == JsonValueKind.String
                        ? nameElement.GetString() ?? string.Empty
                        : string.Empty;
                    var normalizedProfileName = NormalizeComparisonToken(profileName);
                    if (normalizedProfileName == expected || normalizedGroupIsc == expected)
                    {
                        profile = candidate;
                        return true;
                    }
                }
            }

            if (group.TryGetProperty("options", out var optionsElement) && optionsElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var candidateOption in optionsElement.EnumerateArray())
                {
                    var optionName = candidateOption.TryGetProperty("name", out var optionNameElement) && optionNameElement.ValueKind == JsonValueKind.String
                        ? optionNameElement.GetString() ?? string.Empty
                        : string.Empty;
                    var normalizedOptionName = NormalizeComparisonToken(optionName);
                    if (normalizedOptionName == expected)
                    {
                        if (group.TryGetProperty("profiles", out var optionMatchedProfiles) &&
                            optionMatchedProfiles.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var optionMatchedProfile in optionMatchedProfiles.EnumerateArray())
                            {
                                if (HasStatFields(optionMatchedProfile))
                                {
                                    profile = optionMatchedProfile;
                                    return true;
                                }
                            }
                        }

                        if (HasStatFields(candidateOption))
                        {
                            profile = candidateOption;
                            return true;
                        }
                    }
                }
            }

            if (normalizedGroupIsc == expected &&
                group.TryGetProperty("profiles", out var groupProfilesElement) &&
                groupProfilesElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var fallbackProfile in groupProfilesElement.EnumerateArray())
                {
                    profile = fallbackProfile;
                    return true;
                }
            }
        }

        return false;
    }

    private static string NormalizeComparisonToken(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return Regex.Replace(value, @"[^a-z0-9]", string.Empty, RegexOptions.IgnoreCase).ToLowerInvariant();
    }

    private static bool HasStatFields(JsonElement element)
    {
        return element.TryGetProperty("cc", out _) ||
               element.TryGetProperty("bs", out _) ||
               element.TryGetProperty("ph", out _) ||
               element.TryGetProperty("wip", out _) ||
               element.TryGetProperty("arm", out _) ||
               element.TryGetProperty("bts", out _) ||
               element.TryGetProperty("w", out _) ||
               element.TryGetProperty("str", out _);
    }

    private static bool IsPositiveSwc(string swc)
    {
        if (string.IsNullOrWhiteSpace(swc) || swc == "-")
        {
            return false;
        }

        return decimal.TryParse(
                   swc,
                   NumberStyles.Number,
                   CultureInfo.InvariantCulture,
                   out var value)
               && value > 0m;
    }
}
