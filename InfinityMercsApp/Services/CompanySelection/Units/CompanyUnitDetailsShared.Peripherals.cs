using System.Text.Json;
using InfinityMercsApp.Services;

namespace InfinityMercsApp.Views.Common;

internal static partial class CompanyUnitDetailsShared
{
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
            logError($"CompanySelectionPage UpdatePeripheralStatBlockFromVisibleProfiles failed: {ex.Message}");
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
