using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using InfinityMercsApp.Services;
using InfinityMercsApp.ViewModels;
using InfinityMercsApp.Views.Controls;
using InfinityMercsApp.Views.Templates.NewCompany;
using Svg.Skia;
using ArmyResumeRecord = InfinityMercsApp.Domain.Models.Army.Resume;
using ArmySpecopsUnitRecord = InfinityMercsApp.Domain.Models.Army.SpecopsUnit;
namespace InfinityMercsApp.Views.CohesiveCompany;
/// <summary>
/// Selected-unit detail pipeline: profile parsing, stat projection, and peripheral/trait extraction.
/// </summary>
public partial class CCArmyFactionSelectionPage
{
    private static int ParseCostValue(string? cost)
    {
        return CompanySelectionSharedUtilities.ParseCostValue(cost);
    }

    private async Task LoadSelectedUnitDetailsAsync(CancellationToken cancellationToken = default)
    {
        ResetUnitDetails(clearLogo: false, resetHeaderColors: false);
        if (_selectedUnit is null)
        {
            Console.Error.WriteLine("ArmyFactionSelectionPage LoadSelectedUnitDetailsAsync aborted: selected unit or accessor missing.");
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
                    GetSpecopsUnitsByFactionAsync = (factionId, token) => _specOpsProvider.GetSpecopsUnitsByFactionAsync(factionId, token),
                    ApplyUnitHeaderColorsAsync = ApplyUnitHeaderColorsAsync,
                    BuildIdNameLookup = BuildIdNameLookup,
                    TryParseId = TryParseId,
                    ApplyGlobalDisplayUnitsPreferenceAsync = ApplyGlobalDisplayUnitsPreferenceAsync,
                    EnumerateOptions = EnumerateOptions,
                    ReadOptionSwc = CompanyProfileOptionService.ReadOptionSwc,
                    IsPositiveSwc = IsPositiveSwc,
                    IsLieutenantOption = IsLieutenantOption,
                    PopulateUnitStatsFromFirstProfile = PopulateUnitStatsFromFirstProfile,
                    ParseUnitOrderTraits = ParseUnitOrderTraits,
                    SetOrderTraits = (hasIrregular, hasRegular, hasImpetuous, hasTacticalAwareness) =>
                    {
                        ShowIrregularOrderIcon = hasIrregular;
                        ShowRegularOrderIcon = !hasIrregular && hasRegular;
                        ShowImpetuousIcon = hasImpetuous;
                        ShowTacticalAwarenessIcon = hasTacticalAwareness;
                    },
                    ParseUnitTechTraits = ParseUnitTechTraits,
                    SetTechTraits = (hasCube, hasCube2, hasHackable) =>
                    {
                        ShowCubeIcon = hasCube;
                        ShowCube2Icon = hasCube2;
                        ShowHackableIcon = hasHackable;
                    },
                    EnsureLieutenantSkill = CompanyProfileTextService.EnsureLieutenantSkill,
                    SetCommonEquipmentSkills = (equipment, skills, highlightLieutenant) =>
                    {
                        UnitDisplayConfigurationsView.SelectedUnitCommonEquipment = equipment.ToList();
                        UnitDisplayConfigurationsView.SelectedUnitCommonSkills = skills.ToList();
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
                    SetSelectedUnitProfileGroupsJson = value => UnitDisplayConfigurationsView.SelectedUnitProfileGroupsJson = value,
                    SetSelectedUnitFiltersJson = value => UnitDisplayConfigurationsView.SelectedUnitFiltersJson = value,
                    SetUnitNameHeading = value => UnitNameHeading = value,
                    SetSummaryHighlightLieutenant = value => _summaryHighlightLieutenant = value,
                    LogInfo = Console.WriteLine,
                    LogError = Console.Error.WriteLine
                },
                cancellationToken);
            Console.WriteLine($"ArmyFactionSelectionPage LoadSelectedUnitDetailsAsync completed: heading='{UnitNameHeading}', MOV='{UnitMov}', equipment='{EquipmentSummary}'.");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ArmyFactionSelectionPage LoadSelectedUnitDetailsAsync failed: {ex}");
        }
    }

    private async Task LoadSelectedUnitLogoAsync(ArmyUnitSelectionItem item)
    {
        UnitDisplayConfigurationsView.SelectedUnitPicture?.Dispose();
        UnitDisplayConfigurationsView.SelectedUnitPicture = null;
        UnitDisplayConfigurationsView.SelectedUnitPicture = await CompanyUnitLogoWorkflowService.LoadSelectedUnitLogoAsync(
            item.Name,
            item.Id,
            item.SourceFactionId,
            () => OpenBestUnitLogoStreamAsync(item));
        UnitDisplayConfigurationsView.InvalidateSelectedUnitCanvas();
    }

    private void PopulateProfilesFromProfileGroups(JsonElement profileGroupsRoot, string? filtersJson, bool forceLieutenant = false)
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
                ShouldIncludeOption = (_, _, optionName) => !_restrictSelectedUnitProfilesToFto || IsFtoLabel(optionName),
                ParseCostValue = ParseCostValue,
                TryFindPeripheralProfile = peripheralName =>
                    CompanyPeripheralProfileSelectionService.TryFindPeripheralStatElement(profileGroupsRoot, peripheralName, out var peripheralProfile)
                        ? peripheralProfile
                        : (JsonElement?)null,
                BuildPeripheralStatBlock = (peripheralName, peripheralProfile) => BuildPeripheralStatBlock(peripheralName, peripheralProfile, filtersJson),
                TryGetPeripheralUnitCost = peripheralName =>
                    TryGetPeripheralUnitCost(profileGroupsRoot, peripheralName, out var peripheralCost)
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
                BuildPeripheralSubtitle = BuildPeripheralSubtitle,
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

    private async Task<Stream?> OpenBestUnitLogoStreamAsync(ArmyUnitSelectionItem item)
    {
        return await CompanyUnitLogoWorkflowService.OpenBestUnitLogoStreamAsync(
            item.Name,
            item.Id,
            item.SourceFactionId,
            BuildUnitCachedPathCandidates(item),
            BuildUnitPackagedPathCandidates(item));
    }

    private IEnumerable<string?> BuildUnitCachedPathCandidates(ArmyUnitSelectionItem item)
    {
        yield return item.CachedLogoPath;

        if (_factionLogoCacheService is not null)
        {
            yield return _factionLogoCacheService.GetCachedUnitLogoPath(item.SourceFactionId, item.Id);

            if (_factionSelectionState.LeftSlotFaction is not null)
            {
                yield return _factionLogoCacheService.GetCachedUnitLogoPath(_factionSelectionState.LeftSlotFaction.Id, item.Id);
            }

            if (_factionSelectionState.RightSlotFaction is not null)
            {
                yield return _factionLogoCacheService.GetCachedUnitLogoPath(_factionSelectionState.RightSlotFaction.Id, item.Id);
            }

            yield return _factionLogoCacheService.GetCachedLogoPath(item.SourceFactionId);
        }
    }

    private IEnumerable<string?> BuildUnitPackagedPathCandidates(ArmyUnitSelectionItem item)
    {
        yield return item.PackagedLogoPath;

        if (_factionLogoCacheService is not null)
        {
            yield return _factionLogoCacheService.GetPackagedUnitLogoPath(item.SourceFactionId, item.Id);

            if (_factionSelectionState.LeftSlotFaction is not null)
            {
                yield return _factionLogoCacheService.GetPackagedUnitLogoPath(_factionSelectionState.LeftSlotFaction.Id, item.Id);
            }

            if (_factionSelectionState.RightSlotFaction is not null)
            {
                yield return _factionLogoCacheService.GetPackagedUnitLogoPath(_factionSelectionState.RightSlotFaction.Id, item.Id);
            }

            yield return _factionLogoCacheService.GetPackagedFactionLogoPath(item.SourceFactionId);
        }
        else
        {
            yield return $"SVGCache/units/{item.SourceFactionId}-{item.Id}.svg";
            if (_factionSelectionState.LeftSlotFaction is not null)
            {
                yield return $"SVGCache/units/{_factionSelectionState.LeftSlotFaction.Id}-{item.Id}.svg";
            }

            if (_factionSelectionState.RightSlotFaction is not null)
            {
                yield return $"SVGCache/units/{_factionSelectionState.RightSlotFaction.Id}-{item.Id}.svg";
            }

            yield return $"SVGCache/factions/{item.SourceFactionId}.svg";
        }
    }

    private List<ArmyFactionSelectionItem> GetUnitSourceFactions()
    {
        if (!ShowRightSelectionBox)
        {
            return _factionSelectionState.LeftSlotFaction is null ? [] : [_factionSelectionState.LeftSlotFaction];
        }

        var list = new List<ArmyFactionSelectionItem>(2);
        if (_factionSelectionState.LeftSlotFaction is not null)
        {
            list.Add(_factionSelectionState.LeftSlotFaction);
        }

        if (_factionSelectionState.RightSlotFaction is not null && (_factionSelectionState.LeftSlotFaction is null || _factionSelectionState.RightSlotFaction.Id != _factionSelectionState.LeftSlotFaction.Id))
        {
            list.Add(_factionSelectionState.RightSlotFaction);
        }

        return list;
    }

    private static Dictionary<int, string> BuildIdNameLookup(string? filtersJson, string sectionName)
    {
        return CompanySelectionSharedUtilities.BuildIdNameLookup(filtersJson, sectionName);
    }

    private static void MergeFireteamEntries(
        string? fireteamChartJson,
        Dictionary<string, TeamAggregate> target)
    {
        if (string.IsNullOrWhiteSpace(fireteamChartJson))
        {
            return;
        }

        try
        {
            using var doc = JsonDocument.Parse(fireteamChartJson);
            if (!doc.RootElement.TryGetProperty("teams", out var teamsElement) ||
                teamsElement.ValueKind != JsonValueKind.Array)
            {
                return;
            }

            foreach (var teamElement in teamsElement.EnumerateArray())
            {
                var name = ReadString(teamElement, "name", string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                var (duo, haris, core) = ReadTeamTypeCounts(teamElement);
                if (!target.TryGetValue(name, out var aggregate))
                {
                    aggregate = new TeamAggregate(name);
                    target[name] = aggregate;
                }

                aggregate.AddCounts(duo, haris, core);

                foreach (var limit in ReadTeamUnitLimits(teamElement))
                {
                    aggregate.MergeUnitLimit(limit.Name, limit.Min, limit.Max, limit.Slug, limit.MinAsterisk);
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ArmyFactionSelectionPage MergeFireteamEntries failed: {ex.Message}");
        }
    }

    private static (int Duo, int Haris, int Core) ReadTeamTypeCounts(JsonElement teamElement)
    {
        var duo = 0;
        var haris = 0;
        var core = 0;

        if (!teamElement.TryGetProperty("type", out var typeElement) || typeElement.ValueKind != JsonValueKind.Array)
        {
            return (duo, haris, core);
        }

        foreach (var type in typeElement.EnumerateArray())
        {
            if (type.ValueKind != JsonValueKind.String)
            {
                continue;
            }

            var value = type.GetString();
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            if (value.Equals("DUO", StringComparison.OrdinalIgnoreCase))
            {
                duo++;
            }
            else if (value.Equals("HARIS", StringComparison.OrdinalIgnoreCase))
            {
                haris++;
            }
            else if (value.Equals("CORE", StringComparison.OrdinalIgnoreCase))
            {
                core++;
            }
        }

        return (duo, haris, core);
    }

    private static List<(string Name, int Min, int Max, string? Slug, bool MinAsterisk)> ReadTeamUnitLimits(JsonElement teamElement)
    {
        var results = new List<(string Name, int Min, int Max, string? Slug, bool MinAsterisk)>();
        if (!teamElement.TryGetProperty("units", out var unitsElement) || unitsElement.ValueKind != JsonValueKind.Array)
        {
            return results;
        }

        foreach (var unitElement in unitsElement.EnumerateArray())
        {
            var name = ReadString(unitElement, "name", string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                name = ReadString(unitElement, "slug", "Unknown");
            }

            var comment = ReadString(unitElement, "comment", string.Empty).Trim();
            var slug = ReadString(unitElement, "slug", string.Empty).Trim();
            var displayName = name;
            if (!string.IsNullOrWhiteSpace(comment))
            {
                displayName = $"{name} {comment}".Trim();
            }

            var min = ReadInt(unitElement, "min", 0);
            var max = ReadInt(unitElement, "max", 0);
            var minAsterisk = HasAsteriskMin(unitElement) || ReadBool(unitElement, "required", false);
            results.Add((displayName, min, max, string.IsNullOrWhiteSpace(slug) ? null : slug, minAsterisk));
        }

        return results;
    }

    private static bool HasAsteriskMin(JsonElement element)
    {
        return CompanySelectionSharedUtilities.HasAsteriskMin(element);
    }

    private static string ReadString(JsonElement element, string propertyName, string fallback)
    {
        return CompanySelectionSharedUtilities.ReadString(element, propertyName, fallback);
    }

    private static int ReadInt(JsonElement element, string propertyName, int fallback)
    {
        return CompanySelectionSharedUtilities.ReadInt(element, propertyName, fallback);
    }

    private static bool ReadBool(JsonElement element, string propertyName, bool fallback)
    {
        return CompanySelectionSharedUtilities.ReadBool(element, propertyName, fallback);
    }

    private static bool UnitHasLieutenantOption(string? profileGroupsJson, IReadOnlyDictionary<int, string> skillsLookup)
    {
        return CompanySelectionSharedUtilities.UnitHasLieutenantOption(profileGroupsJson, skillsLookup);
    }

    private bool MatchesClassificationFilter(
        ArmyUnitSelectionItem unit,
        IReadOnlyDictionary<int, string> typeLookup)
    {
        var classificationTerm = _activeUnitFilter.ToQuery().GetTerm(UnitFilterField.Classification);
        if (classificationTerm is null || classificationTerm.Values.Count == 0)
        {
            return true;
        }

        if (!unit.Type.HasValue || typeLookup.Count == 0)
        {
            return false;
        }

        if (!typeLookup.TryGetValue(unit.Type.Value, out var typeName))
        {
            return false;
        }

        return classificationTerm.MatchMode == UnitFilterMatchMode.All
            ? classificationTerm.Values.All(value => string.Equals(typeName, value, StringComparison.OrdinalIgnoreCase))
            : classificationTerm.Values.Any(value => string.Equals(typeName, value, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsLieutenantOption(JsonElement option, IReadOnlyDictionary<int, string> skillsLookup)
    {
        return CompanySelectionSharedUtilities.IsLieutenantOption(option, skillsLookup);
    }

    private static bool HasLieutenantOrder(JsonElement option)
    {
        return CompanySelectionSharedUtilities.HasLieutenantOrder(option);
    }

    private static bool IsPositiveSwc(string swc)
    {
        return CompanySelectionSharedUtilities.IsPositiveSwc(swc);
    }

    private static bool TryParseId(JsonElement element, out int id)
    {
        return CompanySelectionSharedUtilities.TryParseId(element, out id);
    }

    private static string BuildUnitSubtitle(
        ArmyResumeRecord unit,
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

    private static bool IsCharacterCategory(ArmyResumeRecord unit, IReadOnlyDictionary<int, string> categoryLookup)
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

    private void ResetUnitDetails(bool clearLogo = true, bool resetHeaderColors = true)
    {
        UnitNameHeading = "Select a unit";
        if (resetHeaderColors)
        {
            ApplyUnitHeaderColorsByVanillaFactionName(null);
        }
        if (clearLogo)
        {
            Console.WriteLine("ArmyFactionSelectionPage ResetUnitDetails: clearing selected unit logo.");
        UnitDisplayConfigurationsView.SelectedUnitPicture?.Dispose();
        UnitDisplayConfigurationsView.SelectedUnitPicture = null;
        UnitDisplayConfigurationsView.InvalidateSelectedUnitCanvas();
    }
        UnitDisplayConfigurationsView.SelectedUnitProfileGroupsJson = null;
        UnitDisplayConfigurationsView.SelectedUnitFiltersJson = null;
        ResetUnitStatsOnly();
        EquipmentSummary = "Equipment: -";
        SpecialSkillsSummary = "Special Skills: -";
        UnitDisplayConfigurationsView.SelectedUnitCommonEquipment = [];
        UnitDisplayConfigurationsView.SelectedUnitCommonSkills = [];
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

    private void ResetUnitStatsOnly()
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

    private void ResetPeripheralStatsOnly()
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
        PeripheralEquipmentFormatted = CompanyProfileTextService.BuildNamedSummaryFormatted("Equipment", Array.Empty<string>(), Color.FromArgb("#06B6D4"));
        PeripheralSkillsFormatted = CompanyProfileTextService.BuildNamedSummaryFormatted("Skills", Array.Empty<string>(), Color.FromArgb("#F59E0B"));
    }

    private static IEnumerable<JsonElement> EnumerateOptions(JsonElement profileGroupsRoot)
    {
        return CompanySelectionSharedUtilities.EnumerateOptions(profileGroupsRoot);
    }

    private static bool TryGetFirstProfileGroup(string? profileGroupsJson, out JsonElement group)
    {
        return CompanySelectionSharedUtilities.TryGetFirstProfileGroup(profileGroupsJson, out group);
    }

    private void PopulateUnitStatsFromFirstProfile(JsonElement profileGroupsArray)
    {
        ResetUnitStatsOnly();

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

        if (!firstProfile.HasValue)
        {
            var firstOption = EnumerateOptions(profileGroupsArray).FirstOrDefault();
            if (firstOption.ValueKind != JsonValueKind.Object)
            {
                return;
            }

            PopulateUnitStatsFromElement(firstOption);
            return;
        }

        PopulateUnitStatsFromElement(firstProfile.Value);
    }

    private void PopulateUnitStatsFromElement(JsonElement selectedElement)
    {
        var unitMove = _armyDataService.ReadMoveValue(selectedElement);
        UnitMoveFirstCm = unitMove.FirstCm;
        UnitMoveSecondCm = unitMove.SecondCm;
        UnitMov = unitMove.DisplayValue;
        UnitCc = ReadIntAsString(selectedElement, "cc");
        UnitBs = ReadIntAsString(selectedElement, "bs");
        UnitPh = ReadIntAsString(selectedElement, "ph");
        UnitWip = ReadIntAsString(selectedElement, "wip");
        UnitArm = ReadIntAsString(selectedElement, "arm");
        UnitBts = ReadIntAsString(selectedElement, "bts");
        UnitS = ReadIntAsString(selectedElement, "s");
        UnitAva = ReadAvaAsString(selectedElement);
        var (vitalityHeader, vitalityValue) = ReadVitality(selectedElement);
        UnitVitalityHeader = vitalityHeader;
        UnitVitality = vitalityValue;
    }

    private static string ReadMoveFromProfile(JsonElement profile)
    {
        return CompanySelectionSharedUtilities.ReadMoveFromProfile(profile);
    }

    private string FormatMoveValue(int? firstCm, int? secondCm)
    {
        return _armyDataService.FormatMoveValue(firstCm, secondCm);
    }

    private static string ReplaceSubtitleMoveDisplay(string? subtitle, string moveDisplay)
    {
        return CompanySelectionSharedUtilities.ReplaceSubtitleMoveDisplay(subtitle, moveDisplay);
    }

    private void UpdateUnitMoveDisplay()
    {
        UnitMov = _armyDataService.FormatMoveValue(UnitMoveFirstCm, UnitMoveSecondCm);
    }

    private void UpdatePeripheralMoveDisplay()
    {
        PeripheralMov = _armyDataService.FormatMoveValue(PeripheralMoveFirstCm, PeripheralMoveSecondCm);
    }

    private void PopulatePeripheralStatsFromElement(JsonElement selectedElement, string peripheralName)
    {
        var peripheralStats = BuildPeripheralStatBlock(peripheralName, selectedElement, UnitDisplayConfigurationsView.SelectedUnitFiltersJson);
        if (peripheralStats is null)
        {
            return;
        }

        ApplyPeripheralStatBlock(peripheralStats);
    }

    private void UpdatePeripheralStatBlockFromVisibleProfiles()
    {
        ResetPeripheralStatsOnly();

        if (string.IsNullOrWhiteSpace(UnitDisplayConfigurationsView.SelectedUnitProfileGroupsJson))
        {
            return;
        }

        var firstPeripheralProfile = Profiles.FirstOrDefault(x => x.IsVisible && x.HasPeripherals);
        if (firstPeripheralProfile is null)
        {
            return;
        }

        var peripheralName = ExtractFirstPeripheralName(firstPeripheralProfile.Peripherals);
        if (string.IsNullOrWhiteSpace(peripheralName))
        {
            return;
        }

        try
        {
            using var doc = JsonDocument.Parse(UnitDisplayConfigurationsView.SelectedUnitProfileGroupsJson);
            if (!CompanyPeripheralProfileSelectionService.TryFindPeripheralStatElement(doc.RootElement, peripheralName, out var peripheralProfile))
            {
                return;
            }

            PopulatePeripheralStatsFromElement(peripheralProfile, peripheralName);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ArmyFactionSelectionPage UpdatePeripheralStatBlockFromVisibleProfiles failed: {ex.Message}");
        }
    }

    private PeripheralMercsCompanyStats? BuildPeripheralStatBlock(string peripheralName, JsonElement peripheralProfile, string? filtersJson)
    {
        var peripheralMove = _armyDataService.ReadMoveValue(peripheralProfile);
        var commonResult = CompanyPeripheralStatBlockService.Build(new CompanyPeripheralStatBlockRequest
        {
            PeripheralName = peripheralName,
            PeripheralProfile = peripheralProfile,
            FiltersJson = filtersJson,
            ShowUnitsInInches = ShowUnitsInInches,
            MoveFirstCm = peripheralMove.FirstCm,
            MoveSecondCm = peripheralMove.SecondCm,
            TryParseId = TryParseId,
            ReadVitality = ReadVitality,
            ReadMoveFromProfile = ReadMoveFromProfile,
            ReadIntAsString = ReadIntAsString,
            ReadAvaAsString = ReadAvaAsString
        });
        if (commonResult is null)
        {
            return null;
        }

        return new PeripheralMercsCompanyStats
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
            Skills = commonResult.Skills
        };
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
        PeripheralEquipmentFormatted = CompanyProfileTextService.BuildNamedSummaryFormatted("Equipment", CompanyProfileTextService.SplitDisplayLine(PeripheralEquipment), Color.FromArgb("#06B6D4"));
        PeripheralSkillsFormatted = CompanyProfileTextService.BuildNamedSummaryFormatted("Skills", CompanyProfileTextService.SplitDisplayLine(PeripheralSkills), Color.FromArgb("#F59E0B"));
        HasPeripheralStatBlock = true;
    }

    private static string ExtractFirstPeripheralName(string? peripheralsText)
    {
        return CompanySelectionSharedUtilities.ExtractFirstPeripheralName(peripheralsText);
    }

    private static string NormalizePeripheralNameForDedupe(string? value)
    {
        return CompanySelectionSharedUtilities.NormalizePeripheralNameForDedupe(value);
    }

    private static int GetPeripheralTotalCount(IEnumerable<string> peripheralNames)
    {
        return CompanySelectionSharedUtilities.GetPeripheralTotalCount(peripheralNames);
    }

    private static bool TryBuildSinglePeripheralDisplay(
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

    private static bool TryGetPeripheralUnitCost(JsonElement profileGroupsRoot, string peripheralName, out int peripheralUnitCost)
    {
        peripheralUnitCost = 0;
        if (profileGroupsRoot.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        var expected = NormalizeComparisonToken(peripheralName);
        if (string.IsNullOrWhiteSpace(expected))
        {
            return false;
        }

        foreach (var group in profileGroupsRoot.EnumerateArray())
        {
            var groupIsc = group.TryGetProperty("isc", out var groupIscElement) && groupIscElement.ValueKind == JsonValueKind.String
                ? groupIscElement.GetString() ?? string.Empty
                : string.Empty;
            var groupMatch = NormalizeComparisonToken(groupIsc) == expected;

            if (!groupMatch &&
                group.TryGetProperty("profiles", out var profilesElement) &&
                profilesElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var profile in profilesElement.EnumerateArray())
                {
                    var profileName = profile.TryGetProperty("name", out var profileNameElement) && profileNameElement.ValueKind == JsonValueKind.String
                        ? profileNameElement.GetString() ?? string.Empty
                        : string.Empty;
                    if (NormalizeComparisonToken(profileName) == expected)
                    {
                        groupMatch = true;
                        break;
                    }
                }
            }

            if (!groupMatch &&
                group.TryGetProperty("options", out var matchOptionsElement) &&
                matchOptionsElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var option in matchOptionsElement.EnumerateArray())
                {
                    var optionName = option.TryGetProperty("name", out var optionNameElement) && optionNameElement.ValueKind == JsonValueKind.String
                        ? optionNameElement.GetString() ?? string.Empty
                        : string.Empty;
                    if (NormalizeComparisonToken(optionName) == expected)
                    {
                        groupMatch = true;
                        break;
                    }
                }
            }

            if (!groupMatch ||
                !group.TryGetProperty("options", out var optionsElement) ||
                optionsElement.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var option in optionsElement.EnumerateArray())
            {
                var optionCost = ParseCostValue(CompanyProfileOptionService.ReadOptionCost(option));
                if (optionCost <= 0)
                {
                    continue;
                }

                var minis = Math.Max(1, CompanyProfileOptionService.ReadOptionMinis(option));
                peripheralUnitCost = Math.Max(1, optionCost / minis);
                return true;
            }
        }

        return false;
    }

    private static bool HasStatFields(JsonElement element)
    {
        return CompanySelectionSharedUtilities.HasStatFields(element);
    }

    private void PopulatePeripheralStatBlock(
        JsonElement profileGroupsRoot,
        string? filtersJson,
        bool forceLieutenant,
        IReadOnlyDictionary<int, string> skillsLookup)
    {
        ResetPeripheralStatsOnly();

        if (profileGroupsRoot.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        var peripheralLookup = BuildIdNameLookup(filtersJson, "peripheral");
        var result = CompanyPeripheralProfileSelectionService.FindFirstVisiblePeripheralProfile(
            new CompanyPeripheralProfileSelectionRequest
            {
                ProfileGroupsRoot = profileGroupsRoot,
                PeripheralLookup = peripheralLookup,
                ForceLieutenant = forceLieutenant,
                LieutenantOnlyUnits = LieutenantOnlyUnits,
                SkillsLookup = skillsLookup,
                IsLieutenantOption = IsLieutenantOption,
                TryParseId = TryParseId
            });
        if (result is not null)
        {
            PopulatePeripheralStatsFromElement(result.PeripheralProfile, result.PeripheralName);
        }
    }

    private static IEnumerable<JsonElement> GetContainerEntries(JsonElement container, string propertyName)
    {
        return CompanySelectionSharedUtilities.GetContainerEntries(container, propertyName);
    }

    private static (bool HasRegular, bool HasIrregular, bool HasImpetuous, bool HasTacticalAwareness) ParseUnitOrderTraits(JsonElement profileGroupsArray)
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

    private static bool HasTacticalAwarenessOrder(JsonElement option)
    {
        if (!option.TryGetProperty("orders", out var ordersElement) || ordersElement.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        foreach (var order in ordersElement.EnumerateArray())
        {
            if (!order.TryGetProperty("type", out var typeElement) || typeElement.ValueKind != JsonValueKind.String)
            {
                continue;
            }

            if (string.Equals(typeElement.GetString(), "TACTICAL", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static string ReadIntAsString(JsonElement option, string propertyName)
    {
        return CompanySelectionSharedUtilities.ReadIntAsString(option, propertyName);
    }

    private static string ReadNumericString(JsonElement element)
    {
        return CompanySelectionSharedUtilities.ReadNumericString(element);
    }

    private static string ReadMove(JsonElement option)
    {
        return CompanySelectionSharedUtilities.ReadMove(option);
    }

    private static (string Header, string Value) ReadVitality(JsonElement option)
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

    private static string ReadAvaAsString(JsonElement option)
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
                _ => value.ToString()
            };
        }

        if (avaElement.ValueKind == JsonValueKind.String && int.TryParse(avaElement.GetString(), out value))
        {
            return value switch
            {
                < 0 => "-",
                255 => "T",
                _ => value.ToString()
            };
        }

        return "-";
    }

    private static bool TryGetPropertyFlexible(JsonElement element, string propertyName, out JsonElement value)
    {
        return CompanySelectionSharedUtilities.TryGetPropertyFlexible(element, propertyName, out value);
    }

    private Task ApplyGlobalDisplayUnitsPreferenceAsync(CancellationToken cancellationToken = default)
    {
        if (_appSettingsProvider is null)
        {
            return Task.CompletedTask;
        }

        try
        {
            var showInches = GetShowUnitsInInchesFromProvider(cancellationToken);
            if (ShowUnitsInInches == showInches)
            {
                return Task.CompletedTask;
            }

            ShowUnitsInInches = showInches;
            UpdateUnitMoveDisplay();
            UpdatePeripheralMoveDisplay();
            RefreshMercsCompanyEntryDistanceDisplays();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ArmyFactionSelectionPage ApplyGlobalDisplayUnitsPreferenceAsync failed: {ex.Message}");
        }

        return Task.CompletedTask;
    }

    private static List<string> ComputeCommonNamesFromProfiles(
        string? profileGroupsJson,
        string propertyName,
        IReadOnlyDictionary<int, string> lookup)
    {
        if (string.IsNullOrWhiteSpace(profileGroupsJson))
        {
            return [];
        }

        HashSet<string>? common = null;
        try
        {
            using var doc = JsonDocument.Parse(profileGroupsJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            foreach (var group in doc.RootElement.EnumerateArray())
            {
                if (!group.TryGetProperty("profiles", out var profilesElement) || profilesElement.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var profile in profilesElement.EnumerateArray())
                {
                    var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    if (profile.TryGetProperty(propertyName, out var arr) && arr.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var entry in arr.EnumerateArray())
                        {
                            if (TryParseId(entry, out var id) && lookup.TryGetValue(id, out var name) && !string.IsNullOrWhiteSpace(name))
                            {
                                set.Add(name);
                            }
                        }
                    }

                    if (common is null)
                    {
                        common = set;
                    }
                    else
                    {
                        common.IntersectWith(set);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ArmyFactionSelectionPage ComputeCommonNamesFromProfiles failed for '{propertyName}': {ex.Message}");
            return [];
        }

        if (common is null || common.Count == 0)
        {
            return [];
        }

        return common.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static (bool HasCube, bool HasCube2, bool HasHackable) ParseUnitTechTraits(
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

        if (Regex.IsMatch(normalized, @"\bhackable\b", RegexOptions.IgnoreCase) &&
            !Regex.IsMatch(normalized, @"\b(non[\s-]*hackable|not[\s-]*hackable)\b", RegexOptions.IgnoreCase))
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

    private static string BuildPeripheralSubtitle(PeripheralMercsCompanyStats? peripheralStats)
    {
        if (peripheralStats is null)
        {
            return "-";
        }

        return $"MOV {peripheralStats.Mov} | CC {peripheralStats.Cc} | BS {peripheralStats.Bs} | PH {peripheralStats.Ph} | WIP {peripheralStats.Wip} | ARM {peripheralStats.Arm} | BTS {peripheralStats.Bts} | {peripheralStats.VitalityHeader} {peripheralStats.Vitality} | S {peripheralStats.S} | AVA {peripheralStats.Ava}";
    }

    private static List<string> IntersectNamedIds(
        IReadOnlyList<JsonElement> options,
        string propertyName,
        IReadOnlyDictionary<int, string> lookup)
    {
        HashSet<int>? intersection = null;
        foreach (var option in options)
        {
            var ids = new HashSet<int>();
            if (option.TryGetProperty(propertyName, out var arr) && arr.ValueKind == JsonValueKind.Array)
            {
                foreach (var entry in arr.EnumerateArray())
                {
                    if (TryParseId(entry, out var id))
                    {
                        ids.Add(id);
                    }
                }
            }

            if (intersection is null)
            {
                intersection = ids;
            }
            else
            {
                intersection.IntersectWith(ids);
            }
        }

        if (intersection is null || intersection.Count == 0)
        {
            return [];
        }

        return intersection
            .Where(lookup.ContainsKey)
            .Select(id => lookup[id])
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static List<string> IntersectNamedIdsWithIncludes(
        JsonElement profileGroupsRoot,
        IReadOnlyList<JsonElement> options,
        string propertyName,
        IReadOnlyDictionary<int, string> lookup)
    {
        HashSet<int>? intersection = null;
        foreach (var option in options)
        {
            var ids = new HashSet<int>();
            foreach (var entry in CompanyProfileOptionService.GetOptionEntriesWithIncludes(profileGroupsRoot, option, propertyName))
            {
                if (TryParseId(entry, out var id))
                {
                    ids.Add(id);
                }
            }

            if (intersection is null)
            {
                intersection = ids;
            }
            else
            {
                intersection.IntersectWith(ids);
            }
        }

        if (intersection is null || intersection.Count == 0)
        {
            return [];
        }

        return intersection
            .Where(lookup.ContainsKey)
            .Select(id => lookup[id])
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string NormalizeComparisonToken(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return Regex.Replace(value, @"[^a-z0-9]", string.Empty, RegexOptions.IgnoreCase).ToLowerInvariant();
    }

    private static string ConvertDistanceText(string distanceText, bool showUnitsInInches)
    {
        return CompanySelectionSharedUtilities.ConvertDistanceText(distanceText, showUnitsInInches);
    }


}




