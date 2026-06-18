using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using InfinityMercsApp.ViewModels;
using InfinityMercsApp.Services;
using InfinityMercsApp.Views.Controls;
using InfinityMercsApp.Views.Common;
using static InfinityMercsApp.Views.Common.CompanyUnitDetailsShared;
using Svg.Skia;
using ArmyResumeRecord = InfinityMercsApp.Domain.Models.Army.Resume;
using ArmySpecopsUnitRecord = InfinityMercsApp.Domain.Models.Army.SpecopsUnit;

namespace InfinityMercsApp.Views.Common;

/// <summary>
/// Selected-unit detail pipeline: profile parsing, stat projection, and peripheral/trait extraction.
/// </summary>
public abstract partial class GeneratedFactionCompanySelectionPageBase
{
    protected async Task LoadSelectedUnitDetailsAsync(CancellationToken cancellationToken = default)
    {
        ResetUnitDetails(clearLogo: false, resetHeaderColors: false);
        if (_selectedUnit is null)
        {
            Console.Error.WriteLine("GeneratedFactionCompanyPage LoadSelectedUnitDetailsAsync aborted: selected unit or accessor missing.");
            return;
        }

        try
        {
            await CompanyUnitDetailsWorkflowService.LoadAsync(
                new CompanyUnitDetailsWorkflowRequest
                {
                    UnitName = _selectedUnit.Name,
                    UnitId = _selectedUnit.Id,
                    SourceFactionId = _selectedUnit.SourceFactionId,
                    IsSpecOps = _selectedUnit.IsSpecOps,
                    LieutenantOnlyUnits = LieutenantOnlyUnits,
                    ShowUnitsInInches = ShowUnitsInInches,
                    GetUnit = GetUnitFromProvider,
                    GetFactionSnapshot = GetFactionSnapshotFromProvider,
                    GetSpecopsUnitsByFactionAsync = (factionId, token) => SpecOpsProvider.GetSpecopsUnitsByFactionAsync(factionId, token),
                    ApplyUnitHeaderColorsAsync = ApplyUnitHeaderColorsAsync,
                    BuildIdNameLookup = BuildIdNameLookup,
                    TryParseId = TryParseId,
                    ApplyGlobalDisplayUnitsPreferenceAsync = token => CompanyUnitDetailsShared.ApplyGlobalDisplayUnitsPreferenceAsync(
                        () => GetShowUnitsInInchesFromProvider(token),
                        ShowUnitsInInches,
                        value => ShowUnitsInInches = value,
                        UpdateUnitMoveDisplay,
                        UpdatePeripheralMoveDisplay,
                        RefreshMercsCompanyEntryDistanceDisplays,
                        Console.Error.WriteLine),
                    EnumerateOptions = EnumerateOptions,
                    ReadOptionSwc = CompanyProfileOptionService.ReadOptionSwc,
                    IsPositiveSwc = IsPositiveSwc,
                    IsLieutenantOption = IsLieutenantOption,
                    PopulateUnitStatsFromFirstProfile = PopulateUnitStatsFromFirstProfile,
                    ParseUnitOrderTraits = CompanyUnitTraitService.ParseUnitOrderTraits,
                    SetOrderTraits = (hasRegular, hasIrregular, hasImpetuous, hasTacticalAwareness) =>
                    {
                        ShowIrregularOrderIcon = hasIrregular;
                        ShowRegularOrderIcon = !hasIrregular && hasRegular;
                        ShowImpetuousIcon = hasImpetuous;
                        ShowTacticalAwarenessIcon = hasTacticalAwareness;
                    },
                    ParseUnitTechTraits = CompanyUnitTraitService.ParseUnitTechTraits,
                    SetTechTraits = (hasCube, hasCube2, hasHackable) =>
                    {
                        ShowCubeIcon = hasCube;
                        ShowCube2Icon = hasCube2;
                        ShowHackableIcon = hasHackable;
                    },
                    EnsureLieutenantSkill = CompanyProfileTextService.EnsureLieutenantSkill,
                    SetCommonEquipmentSkills = (equipment, skills, highlightLieutenant) =>
                    {
                        UnitDisplayConfigurationsViewForVisuals.SelectedUnitCommonEquipment = equipment.ToList();
                        UnitDisplayConfigurationsViewForVisuals.SelectedUnitCommonSkills = skills.ToList();
                        _summaryHighlightLieutenant = highlightLieutenant;
                    },
                    SetSummaryText = (equipmentSummary, specialSkillsSummary) =>
                    {
                        EquipmentSummary = equipmentSummary;
                        SpecialSkillsSummary = specialSkillsSummary;
                    },
                    RefreshSummaryFormatted = RefreshSummaryFormatted,
                    PopulateProfilesFromProfileGroups = PopulateProfilesFromProfileGroups,
                    UpdatePeripheralStatBlockFromVisibleProfiles = UpdatePeripheralStatBlockFromVisibleProfiles,
                    SetSelectedUnitProfileGroupsJson = value => UnitDisplayConfigurationsViewForVisuals.SelectedUnitProfileGroupsJson = value,
                    SetSelectedUnitFiltersJson = value => UnitDisplayConfigurationsViewForVisuals.SelectedUnitFiltersJson = value,
                    SetUnitNameHeading = value => UnitNameHeading = value,
                    SetSummaryHighlightLieutenant = value => _summaryHighlightLieutenant = value,
                    LogInfo = Console.WriteLine,
                    LogError = Console.Error.WriteLine
                },
                cancellationToken);
            Console.WriteLine($"GeneratedFactionCompanyPage LoadSelectedUnitDetailsAsync completed: heading='{UnitNameHeading}', MOV='{UnitMov}', equipment='{EquipmentSummary}'.");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"GeneratedFactionCompanyPage LoadSelectedUnitDetailsAsync failed: {ex}");
        }
    }

    protected async Task LoadSelectedUnitLogoAsync(ArmyUnitSelectionItem item)
    {
        await LoadSelectedUnitLogoCoreAsync(
            item,
            UnitDisplayConfigurationsViewForVisuals,
            () => OpenBestUnitLogoStreamAsync(item));
    }

    protected void PopulateProfilesFromProfileGroups(JsonElement profileGroupsRoot, string? filtersJson, bool forceLieutenant = false)
    {
        Profiles.Clear();
        ProfilesStatus = "Loading profiles...";

        if (profileGroupsRoot.ValueKind != JsonValueKind.Array)
        {
            ProfilesStatus = "No profiles found for this unit.";
            return;
        }

        var skillsLookup = BuildIdNameLookup(filtersJson, "skills");
        var profiles = CompanyProfilePopulationWorkflowService.BuildProfiles(
            new CompanyProfilePopulationRequest<PeripheralMercsCompanyStats>
            {
                ProfileGroupsRoot = profileGroupsRoot,
                FiltersJson = filtersJson,
                ForceLieutenant = forceLieutenant,
                ShowTacticalAwarenessIcon = ShowTacticalAwarenessIcon,
                ShowUnitsInInches = ShowUnitsInInches,
                TryParseId = TryParseId,
                BuildIdNameLookup = BuildIdNameLookup,
                ShouldIncludeOption = (_, _, optionName) => true,
                ParseCostValue = CompanyUnitFilterService.ParseCostValue,
                TryFindPeripheralProfile = peripheralName =>
                    CompanyPeripheralProfileSelectionService.TryFindPeripheralStatElement(profileGroupsRoot, peripheralName, out var peripheralProfile)
                        ? peripheralProfile
                        : (JsonElement?)null,
                BuildPeripheralStatBlock = (peripheralName, peripheralProfile) => BuildPeripheralStatBlock(peripheralName, peripheralProfile, filtersJson),
                TryGetPeripheralUnitCost = peripheralName =>
                    CompanyUnitFilterService.TryGetPeripheralUnitCost(profileGroupsRoot, peripheralName, out var peripheralCost)
                        ? peripheralCost
                        : (int?)null,
                TryBuildSinglePeripheralDisplay = peripheralNames =>
                {
                    var success = TryBuildSinglePeripheralDisplay(peripheralNames, out var peripheralName, out var peripheralCount);
                    return (success, peripheralName, peripheralCount);
                },
                ExtractFirstPeripheralName = ExtractFirstPeripheralName,
                NormalizePeripheralNameForDedupe = NormalizePeripheralNameForDedupe,
                GetPeripheralTotalCount = GetPeripheralTotalCount,
                IsLieutenantOption = option => IsLieutenantOption(option, skillsLookup),
                FormatMoveValue = FormatMoveValue,
                BuildPeripheralSubtitle = stats => stats is null
                    ? "-"
                    : CompanyUnitDetailsShared.BuildPeripheralSubtitle(
                        stats.Mov, stats.Cc, stats.Bs, stats.Ph, stats.Wip,
                        stats.Arm, stats.Bts, stats.VitalityHeader, stats.Vitality, stats.S, stats.Ava),
                ReadPeripheralNameHeading = stats => stats?.NameHeading ?? string.Empty,
                ReadPeripheralMoveFirstCm = stats => stats?.MoveFirstCm,
                ReadPeripheralMoveSecondCm = stats => stats?.MoveSecondCm,
                ReadPeripheralCc = stats => stats?.Cc ?? "-",
                ReadPeripheralBs = stats => stats?.Bs ?? "-",
                ReadPeripheralPh = stats => stats?.Ph ?? "-",
                ReadPeripheralWip = stats => stats?.Wip ?? "-",
                ReadPeripheralArm = stats => stats?.Arm ?? "-",
                ReadPeripheralBts = stats => stats?.Bts ?? "-",
                ReadPeripheralVitalityHeader = stats => stats?.VitalityHeader ?? "VITA",
                ReadPeripheralVitality = stats => stats?.Vitality ?? "-",
                ReadPeripheralS = stats => stats?.S ?? "-",
                ReadPeripheralAva = stats => stats?.Ava ?? "-",
                ReadPeripheralEquipment = stats => stats?.Equipment ?? "-",
                ReadPeripheralSkills = stats => stats?.Skills ?? "-"
            });

        foreach (var profileItem in profiles)
        {
            Profiles.Add(profileItem);
        }

        ApplyLieutenantVisualStates();
    }

    protected async Task<Stream?> OpenBestUnitLogoStreamAsync(ArmyUnitSelectionItem item)
    {
        return await CompanyUnitDetailsShared.OpenBestUnitLogoStreamAsync(
            item.Name, item.Id, item.SourceFactionId,
            BuildUnitCachedPathCandidates(item),
            BuildUnitPackagedPathCandidates(item));
    }

    protected IEnumerable<string?> BuildUnitCachedPathCandidates(ArmyUnitSelectionItem item)
    {
        return BuildUnitCachedPathCandidatesCore(
            item,
            _factionSelectionState.LeftSlotFaction?.Id,
            null,
            FactionLogoCacheService is null ? null : (factionId, unitId) => FactionLogoCacheService.GetCachedUnitLogoPath(factionId, unitId),
            FactionLogoCacheService is null
                ? null
                : factionId => factionId == CompanyFactionId
                    ? null
                    : FactionLogoCacheService.GetCachedLogoPath(factionId));
    }

    protected IEnumerable<string?> BuildUnitPackagedPathCandidates(ArmyUnitSelectionItem item)
    {
        return BuildUnitPackagedPathCandidatesCore(
            item,
            _factionSelectionState.LeftSlotFaction?.Id,
            null,
            FactionLogoCacheService is null ? null : (factionId, unitId) => FactionLogoCacheService.GetPackagedUnitLogoPath(factionId, unitId),
            FactionLogoCacheService is null
                ? null
                : factionId => factionId == CompanyFactionId
                    ? CompanyLogoPath
                    : FactionLogoCacheService.GetPackagedFactionLogoPath(factionId));
    }

    protected static void MergeFireteamEntries(
        string? fireteamChartJson,
        Dictionary<string, CompanyTeamAggregate> target)
    {
        MergeFireteamEntriesCore(fireteamChartJson, target);
    }

    protected static string BuildUnitSubtitle(
        ArmyResumeRecord unit,
        IReadOnlyDictionary<int, string> typeLookup,
        IReadOnlyDictionary<int, string> categoryLookup)
    {
        return BuildUnitSubtitleCore(unit, typeLookup, categoryLookup);
    }

    protected static bool IsCharacterCategory(ArmyResumeRecord unit, IReadOnlyDictionary<int, string> categoryLookup)
    {
        return IsCharacterCategoryCore(unit, categoryLookup);
    }

    protected void ResetUnitDetails(bool clearLogo = true, bool resetHeaderColors = true)
    {
        UnitNameHeading = "Select a unit";
        if (resetHeaderColors)
        {
            ApplyUnitHeaderColorsByVanillaFactionName(null);
        }
        if (clearLogo)
        {
            ClearSelectedUnitLogoCore(
                UnitDisplayConfigurationsViewForVisuals,
                "GeneratedFactionCompanyPage ResetUnitDetails: clearing selected unit logo.");
        }
        UnitDisplayConfigurationsViewForVisuals.SelectedUnitProfileGroupsJson = null;
        UnitDisplayConfigurationsViewForVisuals.SelectedUnitFiltersJson = null;
        ResetUnitStatsOnly();
        EquipmentSummary = "Equipment: -";
        SpecialSkillsSummary = "Special Skills: -";
        UnitDisplayConfigurationsViewForVisuals.SelectedUnitCommonEquipment = [];
        UnitDisplayConfigurationsViewForVisuals.SelectedUnitCommonSkills = [];
        _summaryHighlightLieutenant = false;
        RefreshSummaryFormatted();
        Profiles.Clear();
        ProfilesStatus = "Select a unit.";
        ShowRegularOrderIcon = false;
        ShowIrregularOrderIcon = false;
        ShowImpetuousIcon = false;
        ShowTacticalAwarenessIcon = false;
        ShowCubeIcon = false;
        ShowCube2Icon = false;
        ShowHackableIcon = false;
    }

    protected void ResetUnitStatsOnly()
    {
        UnitMoveFirstCm = null;
        UnitMoveSecondCm = null;
        UnitMov = "-";
        UnitCc = "-";
        UnitBs = "-";
        UnitPh = "-";
        UnitWip = "-";
        UnitArm = "-";
        UnitBts = "-";
        UnitVitalityHeader = "VITA";
        UnitVitality = "-";
        UnitS = "-";
        UnitAva = "-";
        ResetPeripheralStatsOnly();
    }

    protected void ResetPeripheralStatsOnly()
    {
        PeripheralMoveFirstCm = null;
        PeripheralMoveSecondCm = null;
        HasPeripheralStatBlock = false;
        PeripheralNameHeading = string.Empty;
        PeripheralMov = "-";
        PeripheralCc = "-";
        PeripheralBs = "-";
        PeripheralPh = "-";
        PeripheralWip = "-";
        PeripheralArm = "-";
        PeripheralBts = "-";
        PeripheralVitalityHeader = "VITA";
        PeripheralVitality = "-";
        PeripheralS = "-";
        PeripheralAva = "-";
        PeripheralEquipment = "-";
        PeripheralSkills = "-";
        PeripheralEquipmentFormatted = CompanyProfileTextService.BuildNamedSummaryFormatted("Equipment", Array.Empty<string>(), Color.FromArgb("#B5C0CE"));
        PeripheralSkillsFormatted = CompanyProfileTextService.BuildNamedSummaryFormatted("Skills", Array.Empty<string>(), Color.FromArgb("#F59E0B"));
    }

    protected void PopulateUnitStatsFromFirstProfile(JsonElement profileGroupsArray)
    {
        CompanyUnitDetailsShared.PopulateUnitStatsFromFirstProfile(profileGroupsArray, ResetUnitStatsOnly, PopulateUnitStatsFromElement);
    }

    protected void PopulateUnitStatsFromElement(JsonElement selectedElement)
    {
        var projection = CompanyUnitDetailsShared.BuildUnitStatProjection(
            selectedElement,
            ArmyDataService.ReadMoveValue,
            ReadIntAsString,
            ReadAvaAsString,
            ReadVitality);
        UnitMoveFirstCm = projection.MoveFirstCm;
        UnitMoveSecondCm = projection.MoveSecondCm;
        UnitMov = projection.Mov;
        UnitCc = projection.Cc;
        UnitBs = projection.Bs;
        UnitPh = projection.Ph;
        UnitWip = projection.Wip;
        UnitArm = projection.Arm;
        UnitBts = projection.Bts;
        UnitS = projection.S;
        UnitAva = projection.Ava;
        UnitVitalityHeader = projection.VitalityHeader;
        UnitVitality = projection.Vitality;
    }

    protected string FormatMoveValue(int? firstCm, int? secondCm)
    {
        return ArmyDataService.FormatMoveValue(firstCm, secondCm);
    }

    protected void UpdateUnitMoveDisplay()
    {
        UnitMov = ArmyDataService.FormatMoveValue(UnitMoveFirstCm, UnitMoveSecondCm);
    }

    protected void UpdatePeripheralMoveDisplay()
    {
        PeripheralMov = ArmyDataService.FormatMoveValue(PeripheralMoveFirstCm, PeripheralMoveSecondCm);
    }

    protected void PopulatePeripheralStatsFromElement(JsonElement selectedElement, string peripheralName)
    {
        var peripheralStats = BuildPeripheralStatBlock(peripheralName, selectedElement, UnitDisplayConfigurationsViewForVisuals.SelectedUnitFiltersJson);
        if (peripheralStats is null)
        {
            return;
        }

        ApplyPeripheralStatBlock(peripheralStats);
    }

    protected void UpdatePeripheralStatBlockFromVisibleProfiles()
    {
        CompanyUnitDetailsShared.UpdatePeripheralStatBlockFromVisibleProfiles(
            UnitDisplayConfigurationsViewForVisuals.SelectedUnitProfileGroupsJson,
            Profiles,
            profile => profile.IsVisible,
            profile => profile.HasPeripherals,
            profile => profile.Peripherals,
            ExtractFirstPeripheralName,
            ResetPeripheralStatsOnly,
            PopulatePeripheralStatsFromElement,
            Console.Error.WriteLine);
    }

    private PeripheralMercsCompanyStats? BuildPeripheralStatBlock(string peripheralName, JsonElement peripheralProfile, string? filtersJson)
    {
        return CompanyUnitDetailsShared.BuildPeripheralStatBlock(
            peripheralName, peripheralProfile, filtersJson, ShowUnitsInInches,
            element =>
            {
                var move = ArmyDataService.ReadMoveValue(element);
                return (move.FirstCm, move.SecondCm);
            },
            commonResult => new PeripheralMercsCompanyStats
            {
                NameHeading = commonResult.NameHeading,
                MoveFirstCm = commonResult.MoveFirstCm,
                MoveSecondCm = commonResult.MoveSecondCm,
                Mov = commonResult.Mov,
                Cc = commonResult.Cc,
                Bs = commonResult.Bs,
                Ph = commonResult.Ph,
                Wip = commonResult.Wip,
                Arm = commonResult.Arm,
                Bts = commonResult.Bts,
                VitalityHeader = commonResult.VitalityHeader,
                Vitality = commonResult.Vitality,
                S = commonResult.S,
                Ava = commonResult.Ava,
                Equipment = commonResult.Equipment,
                Skills = commonResult.Skills,
                Characteristics = commonResult.Characteristics,
                RangedWeapons = commonResult.RangedWeapons,
                CcWeapons = commonResult.CcWeapons
            });
    }

    private void ApplyPeripheralStatBlock(PeripheralMercsCompanyStats peripheralStats)
    {
        PeripheralMoveFirstCm = peripheralStats.MoveFirstCm;
        PeripheralMoveSecondCm = peripheralStats.MoveSecondCm;
        UpdatePeripheralMoveDisplay();
        PeripheralNameHeading = peripheralStats.NameHeading;
        PeripheralCc = peripheralStats.Cc;
        PeripheralBs = peripheralStats.Bs;
        PeripheralPh = peripheralStats.Ph;
        PeripheralWip = peripheralStats.Wip;
        PeripheralArm = peripheralStats.Arm;
        PeripheralBts = peripheralStats.Bts;
        PeripheralVitalityHeader = peripheralStats.VitalityHeader;
        PeripheralVitality = peripheralStats.Vitality;
        PeripheralS = peripheralStats.S;
        PeripheralAva = peripheralStats.Ava;
        PeripheralEquipment = peripheralStats.Equipment;
        PeripheralSkills = peripheralStats.Skills;
        PeripheralEquipmentFormatted = CompanyProfileTextService.BuildNamedSummaryFormatted("Equipment", CompanyProfileTextService.SplitDisplayLine(PeripheralEquipment), Color.FromArgb("#B5C0CE"));
        PeripheralSkillsFormatted = CompanyProfileTextService.BuildNamedSummaryFormatted("Skills", CompanyProfileTextService.SplitDisplayLine(PeripheralSkills), Color.FromArgb("#F59E0B"));
        HasPeripheralStatBlock = true;
    }
}
