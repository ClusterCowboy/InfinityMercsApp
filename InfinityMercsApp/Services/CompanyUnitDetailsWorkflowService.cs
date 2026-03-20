using System.Text.Json;
namespace InfinityMercsApp.Services;

internal sealed class CompanyUnitDetailsWorkflowRequest
{
    public required string UnitName { get; init; }
    public required int UnitId { get; init; }
    public required int SourceFactionId { get; init; }
    public required bool IsSpecOps { get; init; }
    public required bool LieutenantOnlyUnits { get; init; }
    public required bool ShowUnitsInInches { get; init; }
    public required Func<int, int, CancellationToken, InfinityMercsApp.Domain.Models.Army.Unit?> GetUnit { get; init; }
    public required Func<int, CancellationToken, InfinityMercsApp.Domain.Models.Army.Faction?> GetFactionSnapshot { get; init; }
    public required Func<int, CancellationToken, Task<IReadOnlyList<InfinityMercsApp.Domain.Models.Army.SpecopsUnit>>> GetSpecopsUnitsByFactionAsync { get; init; }
    public required Func<int, InfinityMercsApp.Domain.Models.Army.Unit?, CancellationToken, Task> ApplyUnitHeaderColorsAsync { get; init; }
    public required Func<string?, string, Dictionary<int, string>> BuildIdNameLookup { get; init; }
    public required CompanyUnitDetailDisplayNameContext.TryParseIdDelegate TryParseId { get; init; }
    public required Func<CancellationToken, Task> ApplyGlobalDisplayUnitsPreferenceAsync { get; init; }
    public required Func<JsonElement, IEnumerable<JsonElement>> EnumerateOptions { get; init; }
    public required Func<JsonElement, string> ReadOptionSwc { get; init; }
    public required Func<string, bool> IsPositiveSwc { get; init; }
    public required Func<JsonElement, IReadOnlyDictionary<int, string>, bool> IsLieutenantOption { get; init; }
    public required Action<JsonElement> PopulateUnitStatsFromFirstProfile { get; init; }
    public required Func<JsonElement, (bool HasRegular, bool HasIrregular, bool HasImpetuous, bool HasTacticalAwareness)> ParseUnitOrderTraits { get; init; }
    public required Action<bool, bool, bool, bool> SetOrderTraits { get; init; }
    public required Func<JsonElement, IReadOnlyDictionary<int, string>, IReadOnlyDictionary<int, string>, IReadOnlyDictionary<int, string>, (bool HasCube, bool HasCube2, bool HasHackable)> ParseUnitTechTraits { get; init; }
    public required Action<bool, bool, bool> SetTechTraits { get; init; }
    public required Func<IEnumerable<string>, List<string>> EnsureLieutenantSkill { get; init; }
    public required Action<IReadOnlyList<string>, IReadOnlyList<string>, bool> SetCommonEquipmentSkills { get; init; }
    public required Action<string, string> SetSummaryText { get; init; }
    public required Action RefreshSummaryFormatted { get; init; }
    public required Action<JsonElement, string?, bool> PopulateProfilesFromProfileGroups { get; init; }
    public required Action UpdatePeripheralStatBlockFromVisibleProfiles { get; init; }
    public required Action<string?> SetSelectedUnitProfileGroupsJson { get; init; }
    public required Action<string?> SetSelectedUnitFiltersJson { get; init; }
    public required Action<string> SetUnitNameHeading { get; init; }
    public required Action<bool> SetSummaryHighlightLieutenant { get; init; }
    public required Action<string> LogInfo { get; init; }
    public required Action<string> LogError { get; init; }
}

internal static class CompanyUnitDetailsWorkflowService
{
    /// <summary>
    /// Runs the shared selected-unit details workflow and applies outputs via callbacks.
    /// </summary>
    public static async Task LoadAsync(
        CompanyUnitDetailsWorkflowRequest request,
        CancellationToken cancellationToken = default)
    {
        request.LogInfo($"ArmyFactionSelectionPage LoadSelectedUnitDetailsAsync started: id={request.UnitId}, faction={request.SourceFactionId}, name='{request.UnitName}'.");
        request.SetUnitNameHeading(request.UnitName);

        var unit = request.GetUnit(request.SourceFactionId, request.UnitId, cancellationToken);
        InfinityMercsApp.Domain.Models.Army.SpecopsUnit? specopsUnit = null;
        if (request.IsSpecOps || unit is null)
        {
            var specopsUnits = await request.GetSpecopsUnitsByFactionAsync(request.SourceFactionId, cancellationToken);
            specopsUnit = specopsUnits.FirstOrDefault(x => x.UnitId == request.UnitId);
        }

        var treatAsSpecOps = request.IsSpecOps || (unit is null && specopsUnit is not null);
        await request.ApplyUnitHeaderColorsAsync(request.SourceFactionId, unit, cancellationToken);

        var profileGroupsJson = unit?.ProfileGroupsJson;
        if (treatAsSpecOps && !string.IsNullOrWhiteSpace(specopsUnit?.ProfileGroupsJson))
        {
            profileGroupsJson = specopsUnit.ProfileGroupsJson;
        }
        else if (string.IsNullOrWhiteSpace(profileGroupsJson))
        {
            profileGroupsJson = specopsUnit?.ProfileGroupsJson;
        }

        var snapshot = request.GetFactionSnapshot(request.SourceFactionId, cancellationToken);
        if (string.IsNullOrWhiteSpace(profileGroupsJson))
        {
            request.LogError($"ArmyFactionSelectionPage: profile groups not found for faction={request.SourceFactionId}, unit={request.UnitId}.");
            return;
        }

        var equipLookup = request.BuildIdNameLookup(snapshot?.FiltersJson, "equip");
        var skillsLookup = request.BuildIdNameLookup(snapshot?.FiltersJson, "skills");
        var charsLookup = request.BuildIdNameLookup(snapshot?.FiltersJson, "chars");
        var displayNameContext = CompanyUnitDetailDisplayNameContext.Create(snapshot?.FiltersJson, request.ShowUnitsInInches, request.TryParseId);
        request.SetSelectedUnitProfileGroupsJson(profileGroupsJson);
        request.SetSelectedUnitFiltersJson(snapshot?.FiltersJson);
        await request.ApplyGlobalDisplayUnitsPreferenceAsync(cancellationToken);

        using var doc = JsonDocument.Parse(profileGroupsJson);
        var options = request.EnumerateOptions(doc.RootElement).ToList();
        var visibleOptions = options
            .Where(option => !request.IsPositiveSwc(request.ReadOptionSwc(option)))
            .Where(option => !treatAsSpecOps && request.LieutenantOnlyUnits ? request.IsLieutenantOption(option, skillsLookup) : true)
            .ToList();

        request.PopulateUnitStatsFromFirstProfile(doc.RootElement);
        var orderTraits = request.ParseUnitOrderTraits(doc.RootElement);
        request.SetOrderTraits(orderTraits.HasRegular, orderTraits.HasIrregular, orderTraits.HasImpetuous, orderTraits.HasTacticalAwareness);

        var techTraits = request.ParseUnitTechTraits(doc.RootElement, equipLookup, skillsLookup, charsLookup);
        request.SetTechTraits(techTraits.HasCube, techTraits.HasCube2, techTraits.HasHackable);

        var stableEquipFromProfiles = displayNameContext.ComputeCommonDisplayNamesFromProfiles(profileGroupsJson, "equip", equipLookup);
        var stableEquipFromVisibleOptions = visibleOptions.Count > 0
            ? displayNameContext.IntersectDisplayNamesWithIncludes(doc.RootElement, visibleOptions, "equip", equipLookup)
            : [];
        var stableEquip = stableEquipFromProfiles
            .Concat(stableEquipFromVisibleOptions)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var stableSkillsFromProfiles = displayNameContext.ComputeCommonDisplayNamesFromProfiles(profileGroupsJson, "skills", skillsLookup);
        var stableSkillsFromVisibleOptions = visibleOptions.Count > 0
            ? displayNameContext.IntersectDisplayNamesWithIncludes(doc.RootElement, visibleOptions, "skills", skillsLookup)
            : [];
        var stableSkills = stableSkillsFromProfiles
            .Concat(stableSkillsFromVisibleOptions)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();

        stableSkills = treatAsSpecOps
            ? request.EnsureLieutenantSkill(stableSkills)
            : stableSkills.Where(x => !x.Contains("lieutenant", StringComparison.OrdinalIgnoreCase)).ToList();

        request.SetCommonEquipmentSkills(stableEquip, stableSkills, treatAsSpecOps);
        request.LogInfo(
            $"ArmyFactionSelectionPage summary extraction: unit='{request.UnitName}', options={visibleOptions.Count}, commonEquip={stableEquip.Count}, commonSkills={stableSkills.Count}.");

        request.SetSummaryText(
            $"Equipment: {(stableEquip.Count == 0 ? "-" : string.Join(", ", stableEquip))}",
            $"Special Skills: {(stableSkills.Count == 0 ? "-" : string.Join(", ", stableSkills))}");
        request.RefreshSummaryFormatted();
        request.PopulateProfilesFromProfileGroups(doc.RootElement, snapshot?.FiltersJson, treatAsSpecOps);
        request.UpdatePeripheralStatBlockFromVisibleProfiles();
    }
}

