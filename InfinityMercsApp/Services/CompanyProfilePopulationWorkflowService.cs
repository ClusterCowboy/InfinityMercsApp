using System.Text.Json;
using System.Globalization;
using InfinityMercsApp.ViewModels;

namespace InfinityMercsApp.Services;

internal sealed class CompanyProfilePopulationRequest<TPeripheralStats>
{
    public required JsonElement ProfileGroupsRoot { get; init; }
    public required string? FiltersJson { get; init; }
    public required bool ForceLieutenant { get; init; }
    public required bool ShowTacticalAwarenessIcon { get; init; }
    public required bool ShowUnitsInInches { get; init; }
    public required CompanyUnitDetailDisplayNameContext.TryParseIdDelegate TryParseId { get; init; }
    public required Func<string?, string, IReadOnlyDictionary<int, string>> BuildIdNameLookup { get; init; }
    public required Func<JsonElement, JsonElement, string, bool> ShouldIncludeOption { get; init; }
    public required Func<string, int> ParseCostValue { get; init; }
    public required Func<string, JsonElement?> TryFindPeripheralProfile { get; init; }
    public required Func<string, JsonElement, TPeripheralStats?> BuildPeripheralStatBlock { get; init; }
    public required Func<string, int?> TryGetPeripheralUnitCost { get; init; }
    public required Func<IReadOnlyList<string>, (bool Success, string Name, int Count)> TryBuildSinglePeripheralDisplay { get; init; }
    public required Func<string?, string> ExtractFirstPeripheralName { get; init; }
    public required Func<string, string> NormalizePeripheralNameForDedupe { get; init; }
    public required Func<IEnumerable<string>, int> GetPeripheralTotalCount { get; init; }
    public required Func<JsonElement, bool> IsLieutenantOption { get; init; }
    public required Func<int?, int?, string> FormatMoveValue { get; init; }
    public required Func<TPeripheralStats?, string> BuildPeripheralSubtitle { get; init; }
    public required Func<TPeripheralStats?, string> ReadPeripheralNameHeading { get; init; }
    public required Func<TPeripheralStats?, int?> ReadPeripheralMoveFirstCm { get; init; }
    public required Func<TPeripheralStats?, int?> ReadPeripheralMoveSecondCm { get; init; }
    public required Func<TPeripheralStats?, string> ReadPeripheralCc { get; init; }
    public required Func<TPeripheralStats?, string> ReadPeripheralBs { get; init; }
    public required Func<TPeripheralStats?, string> ReadPeripheralPh { get; init; }
    public required Func<TPeripheralStats?, string> ReadPeripheralWip { get; init; }
    public required Func<TPeripheralStats?, string> ReadPeripheralArm { get; init; }
    public required Func<TPeripheralStats?, string> ReadPeripheralBts { get; init; }
    public required Func<TPeripheralStats?, string> ReadPeripheralVitalityHeader { get; init; }
    public required Func<TPeripheralStats?, string> ReadPeripheralVitality { get; init; }
    public required Func<TPeripheralStats?, string> ReadPeripheralS { get; init; }
    public required Func<TPeripheralStats?, string> ReadPeripheralAva { get; init; }
    public required Func<TPeripheralStats?, string> ReadPeripheralEquipment { get; init; }
    public required Func<TPeripheralStats?, string> ReadPeripheralSkills { get; init; }
}

internal static class CompanyProfilePopulationWorkflowService
{
    public static IReadOnlyList<ViewerProfileItem> BuildProfiles<TPeripheralStats>(
        CompanyProfilePopulationRequest<TPeripheralStats> request)
    {
        if (request.ProfileGroupsRoot.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var weaponsLookup = request.BuildIdNameLookup(request.FiltersJson, "weapons");
        var equipLookup = request.BuildIdNameLookup(request.FiltersJson, "equip");
        var skillsLookup = request.BuildIdNameLookup(request.FiltersJson, "skills");
        var peripheralLookup = request.BuildIdNameLookup(request.FiltersJson, "peripheral");
        var displayNameContext = CompanyUnitDetailDisplayNameContext.Create(request.FiltersJson, request.ShowUnitsInInches, request.TryParseId);

        var buildRequest = new CompanyProfileBuildRequest<TPeripheralStats>
        {
            ProfileGroupsRoot = request.ProfileGroupsRoot,
            ForceLieutenant = request.ForceLieutenant,
            ShowTacticalAwarenessIcon = request.ShowTacticalAwarenessIcon,
            WeaponsLookup = weaponsLookup,
            EquipLookup = equipLookup,
            SkillsLookup = skillsLookup,
            PeripheralLookup = peripheralLookup,
            IsControllerGroup = group => CompanyProfileOptionService.IsControllerGroup(request.ProfileGroupsRoot, group),
            ShouldIncludeOption = request.ShouldIncludeOption,
            GetOptionEntriesWithIncludes = (option, propertyName) =>
                CompanyProfileOptionService.GetOptionEntriesWithIncludes(request.ProfileGroupsRoot, option, propertyName),
            GetDisplayPeripheralEntriesForOption = (group, option) =>
                CompanyProfileOptionService.GetDisplayPeripheralEntriesForOption(request.ProfileGroupsRoot, group, option),
            GetOrderedDisplayNames = (entries, lookup) => displayNameContext.GetOrderedIdDisplayNamesFromEntries(entries, lookup),
            GetCountedDisplayNames = (entries, lookup) => displayNameContext.GetCountedDisplayNamesFromEntries(entries, lookup),
            ReadOptionSwc = CompanyProfileOptionService.ReadOptionSwc,
            IsPositiveSwc = IsPositiveSwc,
            IsMeleeWeaponName = CompanyProfileTextService.IsMeleeWeaponName,
            ReadAdjustedOptionCost = (group, option) => CompanyProfileOptionService.ReadAdjustedOptionCost(request.ProfileGroupsRoot, group, option),
            ParseCostValue = request.ParseCostValue,
            ReadOptionCost = CompanyProfileOptionService.ReadOptionCost,
            TryFindPeripheralProfile = request.TryFindPeripheralProfile,
            BuildPeripheralStatBlock = request.BuildPeripheralStatBlock,
            TryGetPeripheralUnitCost = request.TryGetPeripheralUnitCost,
            TryBuildSinglePeripheralDisplay = request.TryBuildSinglePeripheralDisplay,
            ExtractFirstPeripheralName = request.ExtractFirstPeripheralName,
            NormalizePeripheralNameForDedupe = request.NormalizePeripheralNameForDedupe,
            GetPeripheralTotalCount = request.GetPeripheralTotalCount,
            IsLieutenantOption = request.IsLieutenantOption,
            FormatMoveValue = request.FormatMoveValue,
            BuildPeripheralSubtitle = request.BuildPeripheralSubtitle,
            ReadPeripheralNameHeading = request.ReadPeripheralNameHeading,
            ReadPeripheralMoveFirstCm = request.ReadPeripheralMoveFirstCm,
            ReadPeripheralMoveSecondCm = request.ReadPeripheralMoveSecondCm,
            ReadPeripheralCc = request.ReadPeripheralCc,
            ReadPeripheralBs = request.ReadPeripheralBs,
            ReadPeripheralPh = request.ReadPeripheralPh,
            ReadPeripheralWip = request.ReadPeripheralWip,
            ReadPeripheralArm = request.ReadPeripheralArm,
            ReadPeripheralBts = request.ReadPeripheralBts,
            ReadPeripheralVitalityHeader = request.ReadPeripheralVitalityHeader,
            ReadPeripheralVitality = request.ReadPeripheralVitality,
            ReadPeripheralS = request.ReadPeripheralS,
            ReadPeripheralAva = request.ReadPeripheralAva,
            ReadPeripheralEquipment = request.ReadPeripheralEquipment,
            ReadPeripheralSkills = request.ReadPeripheralSkills
        };

        var profileCoordinator = new CompanyProfileCoordinator();
        return profileCoordinator.BuildProfiles(buildRequest).ToList();
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
