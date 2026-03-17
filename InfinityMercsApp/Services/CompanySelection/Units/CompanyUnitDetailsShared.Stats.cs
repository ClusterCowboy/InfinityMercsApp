using System.Text.Json;

namespace InfinityMercsApp.Views.Common;

internal static partial class CompanyUnitDetailsShared
{
    internal static void MergeFireteamEntries(
        string? fireteamChartJson,
        Action<CompanyFireteamChartEntry> mergeEntry,
        Action<string>? logError = null)
    {
        foreach (var entry in CompanyUnitDetailsFireteamCommon.ParseEntries(fireteamChartJson, logError))
        {
            mergeEntry(entry);
        }
    }

    internal static void PopulateUnitStatsFromFirstProfile(
        JsonElement profileGroupsArray,
        Action resetUnitStatsOnly,
        Action<JsonElement> populateUnitStatsFromElement)
    {
        resetUnitStatsOnly();

        if (profileGroupsArray.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        JsonElement? firstProfile = null;
        foreach (var group in profileGroupsArray.EnumerateArray())
        {
            if (!group.TryGetProperty("profiles", out var profilesElement) || profilesElement.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var profile in profilesElement.EnumerateArray())
            {
                firstProfile = profile;
                break;
            }

            if (firstProfile.HasValue)
            {
                break;
            }
        }

        if (firstProfile.HasValue)
        {
            populateUnitStatsFromElement(firstProfile.Value);
            return;
        }

        var firstOption = EnumerateOptions(profileGroupsArray).FirstOrDefault();
        if (firstOption.ValueKind == JsonValueKind.Object)
        {
            populateUnitStatsFromElement(firstOption);
        }
    }

    internal static CompanyUnitStatProjection BuildUnitStatProjection(
        JsonElement selectedElement,
        Func<JsonElement, (int? FirstCm, int? SecondCm, string DisplayValue)> readMoveValue,
        Func<JsonElement, string, string> readIntAsString,
        Func<JsonElement, string> readAvaAsString,
        Func<JsonElement, (string Header, string Value)> readVitality)
    {
        var unitMove = readMoveValue(selectedElement);
        var (vitalityHeader, vitalityValue) = readVitality(selectedElement);
        return new CompanyUnitStatProjection(
            unitMove.FirstCm,
            unitMove.SecondCm,
            unitMove.DisplayValue,
            readIntAsString(selectedElement, "cc"),
            readIntAsString(selectedElement, "bs"),
            readIntAsString(selectedElement, "ph"),
            readIntAsString(selectedElement, "wip"),
            readIntAsString(selectedElement, "arm"),
            readIntAsString(selectedElement, "bts"),
            readIntAsString(selectedElement, "s"),
            readAvaAsString(selectedElement),
            vitalityHeader,
            vitalityValue);
    }
}
