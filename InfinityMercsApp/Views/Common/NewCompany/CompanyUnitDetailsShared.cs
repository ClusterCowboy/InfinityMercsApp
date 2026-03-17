using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using InfinityMercsApp.Domain.Models.Army;
using InfinityMercsApp.Services;
using InfinityMercsApp.Views.Controls;

namespace InfinityMercsApp.Views.Common.NewCompany;

internal static class CompanyUnitDetailsShared
{
    private const int CharacterCategoryId = 10;
    internal sealed record CompanyUnitStatProjection(
        int? MoveFirstCm,
        int? MoveSecondCm,
        string Mov,
        string Cc,
        string Bs,
        string Ph,
        string Wip,
        string Arm,
        string Bts,
        string S,
        string Ava,
        string VitalityHeader,
        string Vitality);

    internal static Dictionary<int, string> BuildIdNameLookup(string? filtersJson, string sectionName)
    {
        return CompanySelectionSharedUtilities.BuildIdNameLookup(filtersJson, sectionName);
    }

    internal static bool HasAsteriskMin(JsonElement element)
    {
        return CompanySelectionSharedUtilities.HasAsteriskMin(element);
    }

    internal static string ReadString(JsonElement element, string propertyName, string fallback)
    {
        return CompanySelectionSharedUtilities.ReadString(element, propertyName, fallback);
    }

    internal static int ReadInt(JsonElement element, string propertyName, int fallback)
    {
        return CompanySelectionSharedUtilities.ReadInt(element, propertyName, fallback);
    }

    internal static bool ReadBool(JsonElement element, string propertyName, bool fallback)
    {
        return CompanySelectionSharedUtilities.ReadBool(element, propertyName, fallback);
    }

    internal static bool IsLieutenantOption(JsonElement option, IReadOnlyDictionary<int, string> skillsLookup)
    {
        return CompanySelectionSharedUtilities.IsLieutenantOption(option, skillsLookup);
    }

    internal static bool HasLieutenantOrder(JsonElement option)
    {
        return CompanySelectionSharedUtilities.HasLieutenantOrder(option);
    }

    internal static bool IsPositiveSwc(string swc)
    {
        return CompanySelectionSharedUtilities.IsPositiveSwc(swc);
    }

    internal static bool TryParseId(JsonElement element, out int id)
    {
        return CompanySelectionSharedUtilities.TryParseId(element, out id);
    }

    internal static IEnumerable<JsonElement> EnumerateOptions(JsonElement profileGroupsRoot)
    {
        return CompanySelectionSharedUtilities.EnumerateOptions(profileGroupsRoot);
    }

    internal static string ReadMoveFromProfile(JsonElement profile)
    {
        return CompanySelectionSharedUtilities.ReadMoveFromProfile(profile);
    }

    internal static string ReplaceSubtitleMoveDisplay(string? subtitle, string moveDisplay)
    {
        return CompanySelectionSharedUtilities.ReplaceSubtitleMoveDisplay(subtitle, moveDisplay);
    }

    internal static string ExtractFirstPeripheralName(string? peripheralsText)
    {
        return CompanySelectionSharedUtilities.ExtractFirstPeripheralName(peripheralsText);
    }

    internal static string NormalizePeripheralNameForDedupe(string? value)
    {
        return CompanySelectionSharedUtilities.NormalizePeripheralNameForDedupe(value);
    }

    internal static int GetPeripheralTotalCount(IEnumerable<string> peripheralNames)
    {
        return CompanySelectionSharedUtilities.GetPeripheralTotalCount(peripheralNames);
    }

    internal static bool TryBuildSinglePeripheralDisplay(
        IReadOnlyList<string> peripheralNames,
        out string peripheralName,
        out int peripheralCount)
    {
        peripheralName = string.Empty;
        peripheralCount = 0;

        if (peripheralNames.Count != 1)
        {
            return false;
        }

        var only = peripheralNames[0];
        if (string.IsNullOrWhiteSpace(only) || only == "-")
        {
            return false;
        }

        var match = Regex.Match(only, @"^(.*)\((\d+)\)\s*$");
        if (!match.Success || !int.TryParse(match.Groups[2].Value, out peripheralCount))
        {
            return false;
        }

        peripheralName = match.Groups[1].Value.Trim();
        if (string.IsNullOrWhiteSpace(peripheralName))
        {
            return false;
        }

        return peripheralCount > 0;
    }

    internal static string ReadIntAsString(JsonElement option, string propertyName)
    {
        return CompanySelectionSharedUtilities.ReadIntAsString(option, propertyName);
    }

    internal static (string Header, string Value) ReadVitality(JsonElement option)
    {
        var str = ReadIntAsString(option, "str");
        if (str != "-")
        {
            return ("STR", str);
        }

        var w = ReadIntAsString(option, "w");
        if (w != "-")
        {
            return ("VITA", w);
        }

        return ("VITA", ReadIntAsString(option, "vita"));
    }

    internal static string ReadAvaAsString(JsonElement option)
    {
        if (!TryGetPropertyFlexible(option, "ava", out var avaElement))
        {
            return "-";
        }

        int value;
        if (avaElement.ValueKind == JsonValueKind.Number && avaElement.TryGetInt32(out value))
        {
            return value switch
            {
                < 0 => "-",
                255 => "T",
                _ => value.ToString(CultureInfo.InvariantCulture)
            };
        }

        if (avaElement.ValueKind == JsonValueKind.String && int.TryParse(avaElement.GetString(), out value))
        {
            return value switch
            {
                < 0 => "-",
                255 => "T",
                _ => value.ToString(CultureInfo.InvariantCulture)
            };
        }

        return "-";
    }

    internal static bool TryGetPropertyFlexible(JsonElement element, string propertyName, out JsonElement value)
    {
        return CompanySelectionSharedUtilities.TryGetPropertyFlexible(element, propertyName, out value);
    }

    internal static string BuildUnitSubtitle(
        Resume unit,
        IReadOnlyDictionary<int, string> typeLookup,
        IReadOnlyDictionary<int, string> categoryLookup)
    {
        var typeName = unit.Type.HasValue && typeLookup.TryGetValue(unit.Type.Value, out var t)
            ? t
            : (unit.Type?.ToString() ?? "?");

        var categoryName = unit.Category.HasValue && categoryLookup.TryGetValue(unit.Category.Value, out var c)
            ? c
            : (unit.Category?.ToString() ?? "?");

        return $"{typeName} - {categoryName}";
    }

    internal static bool IsCharacterCategory(Resume unit, IReadOnlyDictionary<int, string> categoryLookup)
    {
        if (unit.Category.HasValue && unit.Category.Value == CharacterCategoryId)
        {
            return true;
        }

        if (unit.Category.HasValue && categoryLookup.TryGetValue(unit.Category.Value, out var categoryName))
        {
            return !string.IsNullOrWhiteSpace(categoryName) &&
                   categoryName.Contains("character", StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    internal static async Task<Stream?> OpenBestUnitLogoStreamAsync(
        string unitName,
        int unitId,
        int sourceFactionId,
        IEnumerable<string?> cachedPathCandidates,
        IEnumerable<string?> packagedPathCandidates)
    {
        return await CompanyUnitLogoWorkflowService.OpenBestUnitLogoStreamAsync(
            unitName,
            unitId,
            sourceFactionId,
            cachedPathCandidates,
            packagedPathCandidates);
    }

    internal static IEnumerable<string?> BuildUnitCachedPathCandidates(
        string? itemCachedLogoPath,
        int sourceFactionId,
        int unitId,
        int? leftFactionId,
        int? rightFactionId,
        Func<int, int, string?> getCachedUnitLogoPath,
        Func<int, string?> getCachedLogoPath)
    {
        yield return itemCachedLogoPath;
        yield return getCachedUnitLogoPath(sourceFactionId, unitId);

        if (leftFactionId.HasValue)
        {
            yield return getCachedUnitLogoPath(leftFactionId.Value, unitId);
        }

        if (rightFactionId.HasValue)
        {
            yield return getCachedUnitLogoPath(rightFactionId.Value, unitId);
        }

        yield return getCachedLogoPath(sourceFactionId);
    }

    internal static IEnumerable<string?> BuildUnitPackagedPathCandidates(
        string? itemPackagedLogoPath,
        int sourceFactionId,
        int unitId,
        int? leftFactionId,
        int? rightFactionId,
        Func<int, int, string?>? getPackagedUnitLogoPath,
        Func<int, string?>? getPackagedFactionLogoPath)
    {
        yield return itemPackagedLogoPath;

        if (getPackagedUnitLogoPath is not null && getPackagedFactionLogoPath is not null)
        {
            yield return getPackagedUnitLogoPath(sourceFactionId, unitId);
            if (leftFactionId.HasValue)
            {
                yield return getPackagedUnitLogoPath(leftFactionId.Value, unitId);
            }

            if (rightFactionId.HasValue)
            {
                yield return getPackagedUnitLogoPath(rightFactionId.Value, unitId);
            }

            yield return getPackagedFactionLogoPath(sourceFactionId);
            yield break;
        }

        yield return $"SVGCache/units/{sourceFactionId}-{unitId}.svg";
        if (leftFactionId.HasValue)
        {
            yield return $"SVGCache/units/{leftFactionId.Value}-{unitId}.svg";
        }

        if (rightFactionId.HasValue)
        {
            yield return $"SVGCache/units/{rightFactionId.Value}-{unitId}.svg";
        }

        yield return $"SVGCache/factions/{sourceFactionId}.svg";
    }

    internal static List<TFaction> BuildUnitSourceFactions<TFaction>(
        bool showRightSelectionBox,
        TFaction? leftSlotFaction,
        TFaction? rightSlotFaction,
        Func<TFaction, int> readFactionId)
        where TFaction : class
    {
        if (!showRightSelectionBox)
        {
            return leftSlotFaction is null ? [] : [leftSlotFaction];
        }

        var list = new List<TFaction>(2);
        if (leftSlotFaction is not null)
        {
            list.Add(leftSlotFaction);
        }

        if (rightSlotFaction is not null &&
            (leftSlotFaction is null || readFactionId(rightSlotFaction) != readFactionId(leftSlotFaction)))
        {
            list.Add(rightSlotFaction);
        }

        return list;
    }

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

    internal static bool MatchesClassificationFilter(
        UnitFilterCriteria activeUnitFilter,
        int? unitType,
        IReadOnlyDictionary<int, string> typeLookup)
    {
        return CompanyUnitFilterService.MatchesClassificationFilter(activeUnitFilter, unitType, typeLookup);
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

    internal static Task ApplyGlobalDisplayUnitsPreferenceAsync(
        Func<bool> getShowUnitsInInches,
        bool showUnitsInInches,
        Action<bool> setShowUnitsInInches,
        Action updateUnitMoveDisplay,
        Action updatePeripheralMoveDisplay,
        Action refreshDistanceDisplays,
        Action<string> logError)
    {
        try
        {
            var showInches = getShowUnitsInInches();
            if (showUnitsInInches == showInches)
            {
                return Task.CompletedTask;
            }

            setShowUnitsInInches(showInches);
            updateUnitMoveDisplay();
            updatePeripheralMoveDisplay();
            refreshDistanceDisplays();
        }
        catch (Exception ex)
        {
            logError($"ArmyFactionSelectionPage ApplyGlobalDisplayUnitsPreferenceAsync failed: {ex.Message}");
        }

        return Task.CompletedTask;
    }

    internal static string BuildPeripheralSubtitle(
        string mov,
        string cc,
        string bs,
        string ph,
        string wip,
        string arm,
        string bts,
        string vitalityHeader,
        string vitality,
        string s,
        string ava)
    {
        return $"MOV {mov} | CC {cc} | BS {bs} | PH {ph} | WIP {wip} | ARM {arm} | BTS {bts} | {vitalityHeader} {vitality} | S {s} | AVA {ava}";
    }

    internal static TPeripheral? BuildPeripheralStatBlock<TPeripheral>(
        string peripheralName,
        JsonElement peripheralProfile,
        string? filtersJson,
        bool showUnitsInInches,
        Func<JsonElement, (int? FirstCm, int? SecondCm)> readMoveCm,
        Func<CompanyPeripheralStatBlockResult, TPeripheral> map)
    {
        var peripheralMove = readMoveCm(peripheralProfile);
        var commonResult = CompanyUnitDetailsPeripheralCommon.BuildPeripheralCore(
            peripheralName,
            peripheralProfile,
            filtersJson,
            showUnitsInInches,
            peripheralMove.FirstCm,
            peripheralMove.SecondCm);
        return commonResult is null ? default : map(commonResult);
    }

    internal static void UpdatePeripheralStatBlockFromVisibleProfiles<TProfile>(
        string? selectedUnitProfileGroupsJson,
        IEnumerable<TProfile> profiles,
        Func<TProfile, bool> isVisible,
        Func<TProfile, bool> hasPeripherals,
        Func<TProfile, string?> readPeripherals,
        Func<string?, string> extractFirstPeripheralName,
        Action resetPeripheralStatsOnly,
        Action<JsonElement, string> populatePeripheralStatsFromElement,
        Action<string> logError)
    {
        resetPeripheralStatsOnly();

        if (string.IsNullOrWhiteSpace(selectedUnitProfileGroupsJson))
        {
            return;
        }

        var firstPeripheralPeripherals = profiles
            .Where(x => isVisible(x) && hasPeripherals(x))
            .Select(readPeripherals)
            .FirstOrDefault();
        if (firstPeripheralPeripherals is null)
        {
            return;
        }

        var peripheralName = extractFirstPeripheralName(firstPeripheralPeripherals);
        if (string.IsNullOrWhiteSpace(peripheralName))
        {
            return;
        }

        try
        {
            using var doc = JsonDocument.Parse(selectedUnitProfileGroupsJson);
            if (!CompanyPeripheralProfileSelectionService.TryFindPeripheralStatElement(doc.RootElement, peripheralName, out var peripheralProfile))
            {
                return;
            }

            populatePeripheralStatsFromElement(peripheralProfile, peripheralName);
        }
        catch (Exception ex)
        {
            logError($"ArmyFactionSelectionPage UpdatePeripheralStatBlockFromVisibleProfiles failed: {ex.Message}");
        }
    }

    internal static void PopulatePeripheralStatBlock(
        JsonElement profileGroupsRoot,
        string? filtersJson,
        bool forceLieutenant,
        bool lieutenantOnlyUnits,
        IReadOnlyDictionary<int, string> skillsLookup,
        Func<string?, string, Dictionary<int, string>> buildIdNameLookup,
        Func<JsonElement, IReadOnlyDictionary<int, string>, bool> isLieutenantOption,
        CompanyUnitDetailDisplayNameContext.TryParseIdDelegate tryParseId,
        Action resetPeripheralStatsOnly,
        Action<JsonElement, string> populatePeripheralStatsFromElement)
    {
        resetPeripheralStatsOnly();

        if (profileGroupsRoot.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        var peripheralLookup = buildIdNameLookup(filtersJson, "peripheral");
        var result = CompanyPeripheralProfileSelectionService.FindFirstVisiblePeripheralProfile(
            new CompanyPeripheralProfileSelectionRequest
            {
                ProfileGroupsRoot = profileGroupsRoot,
                PeripheralLookup = peripheralLookup,
                ForceLieutenant = forceLieutenant,
                LieutenantOnlyUnits = lieutenantOnlyUnits,
                SkillsLookup = skillsLookup,
                IsLieutenantOption = isLieutenantOption,
                TryParseId = tryParseId
            });
        if (result is not null)
        {
            populatePeripheralStatsFromElement(result.PeripheralProfile, result.PeripheralName);
        }
    }

}

