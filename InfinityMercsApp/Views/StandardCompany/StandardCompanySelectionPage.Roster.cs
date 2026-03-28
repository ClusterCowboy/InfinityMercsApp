using InfinityMercsApp.ViewModels;
using InfinityMercsApp.Views.Common;
using InfinityMercsApp.Views.Common.UICommon;
using InfinityMercsApp.Services;
using TagGen = InfinityMercsApp.Infrastructure.Providers.TagCompanyFactionGenerator;

namespace InfinityMercsApp.Views.StandardCompany;

public partial class StandardCompanySelectionPage
{
    private void SetSelectedUnit(ArmyUnitSelectionItem item)
    {
        _selectedUnit = SetSelectedUnitCore(
            item,
            _selectedUnit,
            selectionContextChanged: false,
            onContextChangedForSameSelection: () => { },
            loadSelectedUnitLogo: selectedItem => _ = LoadSelectedUnitLogoAsync(selectedItem),
            loadSelectedUnitDetails: selectedItem =>
            {
                _selectedUnit = selectedItem;
                _ = LoadSelectedUnitDetailsAsync();
            });
    }

    private async Task AddProfileToMercsCompanyAsync(ViewerProfileItem? profile)
    {
        if (profile is null || _selectedUnit is null)
        {
            return;
        }

        if (!profile.IsVisible || profile.IsLieutenantBlocked)
        {
            return;
        }

        var peripheralStats = BuildMercsCompanyPeripheralStats(profile);
        var entry = BuildMercsCompanyEntryCore<MercsCompanyEntry, ArmyUnitSelectionItem, PeripheralMercsCompanyStats>(
            _selectedUnit,
            profile,
            UnitDisplayConfigurationsView.SelectedUnitCommonEquipment,
            UnitDisplayConfigurationsView.SelectedUnitCommonSkills,
            UnitMov,
            UnitCc,
            UnitBs,
            UnitPh,
            UnitWip,
            UnitArm,
            UnitBts,
            UnitVitalityHeader,
            UnitVitality,
            UnitS,
            UnitMoveFirstCm,
            UnitMoveSecondCm,
            FormatMoveValue,
            peripheralStats);

        AddMercsCompanyEntryCore(
            entry,
            MercsCompanyEntries,
            x => x.IsLieutenant,
            UpdateMercsCompanyTotal,
            ApplyLieutenantVisualStates,
            () => _ = ApplyUnitVisibilityFiltersAsync());

        if (!ShouldRunTagFinalizeWorkflow(profile, entry))
        {
            return;
        }

        SavedImprovedCaptainStats? finalizedTagStats;
        try
        {
            finalizedTagStats = await CompanySpecOpsWorkflowService.ShowTagSpecOpsConfigurationAsync<SavedImprovedCaptainStats>(
                BuildTagSpecOpsWorkflowRequest(entry));
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"StandardCompanySelectionPage TAG finalize popup failed: {ex}");
            return;
        }

        if (finalizedTagStats is null)
        {
            RemoveMercsCompanyEntry(entry);
            return;
        }

        ReplaceMercsCompanyEntry(entry, BuildTagFinalizedEntry(entry, finalizedTagStats));
    }

    private PeripheralMercsCompanyStats? BuildMercsCompanyPeripheralStats(ViewerProfileItem profile)
    {
        return BuildMercsCompanyPeripheralStatsCore(
            profile,
            UnitDisplayConfigurationsView.SelectedUnitProfileGroupsJson,
            UnitDisplayConfigurationsView.SelectedUnitFiltersJson,
            (peripheralName, peripheralProfile, selectedUnitFiltersJson) =>
                BuildPeripheralStatBlock(peripheralName, peripheralProfile, selectedUnitFiltersJson));
    }

    private void RemoveMercsCompanyEntry(MercsCompanyEntry? entry)
    {
        RemoveMercsCompanyEntryCore(
            entry,
            MercsCompanyEntries,
            UpdateMercsCompanyTotal,
            ApplyLieutenantVisualStates,
            () => _ = ApplyUnitVisibilityFiltersAsync());
    }

    private async Task SelectMercsCompanyEntryAsync(MercsCompanyEntry? entry, CancellationToken cancellationToken = default)
    {
        await SelectMercsCompanyEntryAsyncCore(
            entry,
            Units,
            GetUnitFromProvider,
            (sourceUnitId, sourceFactionId, unitName, cachedLogoPath, packagedLogoPath) => new ArmyUnitSelectionItem
            {
                Id = sourceUnitId,
                SourceFactionId = sourceFactionId,
                Name = unitName,
                CachedLogoPath = cachedLogoPath,
                PackagedLogoPath = packagedLogoPath,
                Subtitle = null,
                IsVisible = false
            },
            SetSelectedUnit,
            LoadSelectedUnitDetailsAsync,
            cancellationToken);
    }

    private void ApplyLieutenantVisualStates()
    {
        var visibleProfiles = ApplyLieutenantVisualStatesCore(
            MercsCompanyEntries,
            Profiles,
            SelectedStartSeasonPoints,
            SeasonPointsCapText,
            UnitAva,
            _selectedUnit,
            _filterState.ActiveUnitFilter,
            LieutenantOnlyUnits,
            entry => entry.IsLieutenant,
            entry => entry.SourceUnitId,
            entry => entry.SourceFactionId,
            unit => unit.Id,
            unit => unit.SourceFactionId);

        ProfilesStatus = visibleProfiles == 0
            ? "No configurations found for this unit."
            : $"{visibleProfiles} configurations loaded.";

        UpdatePeripheralStatBlockFromVisibleProfiles();
        UpdateSeasonValidationState();
    }

    private bool ShouldRunTagFinalizeWorkflow(ViewerProfileItem profile, MercsCompanyEntry entry)
    {
        if (!IsTagCompanyMode || profile.IsLieutenant)
        {
            return false;
        }

        if (string.Equals(entry.UnitTypeCode, "TAG", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var selectedUnitType = ExtractUnitTypeCode(_selectedUnit?.Subtitle);
        return string.Equals(selectedUnitType, "TAG", StringComparison.OrdinalIgnoreCase);
    }

    private CompanySpecOpsWorkflowRequest BuildTagSpecOpsWorkflowRequest(MercsCompanyEntry entry)
    {
        var sourceFactionId = ResolveTagWorkflowFactionId(entry.SourceFactionId);
        var popupSkillLines = CompanyProfileTextService.SplitDisplayLine(entry.SavedSkills);
        if (entry.IsLieutenant &&
            !popupSkillLines.Any(x => x.Contains("lieutenant", StringComparison.OrdinalIgnoreCase)))
        {
            popupSkillLines.Add("Lieutenant");
        }

        return new CompanySpecOpsWorkflowRequest
        {
            Navigation = Navigation,
            PreferredOtherFactionId = sourceFactionId > 0 ? sourceFactionId : null,
            FallbackSourceFactionId = sourceFactionId > 0 ? sourceFactionId : entry.SourceFactionId,
            FirstSourceFactionId = sourceFactionId > 0 ? sourceFactionId : null,
            UnitName = entry.Name,
            UnitCost = entry.CostValue,
            UnitStatline = entry.Subtitle ?? "-",
            UnitRangedWeapons = entry.SavedRangedWeapons,
            UnitCcWeapons = entry.SavedCcWeapons,
            UnitSkills = popupSkillLines.Count == 0 ? "-" : string.Join(Environment.NewLine, popupSkillLines),
            UnitEquipment = entry.SavedEquipment,
            UnitCachedLogoPath = entry.CachedLogoPath,
            UnitPackagedLogoPath = entry.PackagedLogoPath,
            TryGetParentFactionId = factionId =>
                Factions.FirstOrDefault(x => x.Id == factionId)?.ParentId
                ?? _armyDataService.GetMetadataFactionById(factionId)?.ParentId,
            TryGetFactionName = factionId => Factions.FirstOrDefault(x => x.Id == factionId)?.Name,
            TryGetMetadataFactionName = factionId => _armyDataService.GetMetadataFactionById(factionId)?.Name,
            ArmyDataService = _armyDataService,
            SpecOpsProvider = _specOpsProvider,
            ShowUnitsInInches = ShowUnitsInInches,
            IsLieutenant = entry.IsLieutenant,
            BaseExperience = 20
        };
    }

    private int ResolveTagWorkflowFactionId(int fallbackFactionId)
    {
        var selectedIds = new[]
        {
            _factionSelectionState.LeftSlotFaction?.Id ?? 0,
            _factionSelectionState.RightSlotFaction?.Id ?? 0
        };

        var resolved = selectedIds.FirstOrDefault(id => id > 0 && id != TagGen.TagCompanyFactionId);
        if (resolved > 0)
        {
            return resolved;
        }

        return fallbackFactionId != TagGen.TagCompanyFactionId ? fallbackFactionId : 0;
    }

    private static MercsCompanyEntry BuildTagFinalizedEntry(
        MercsCompanyEntry entry,
        SavedImprovedCaptainStats stats)
    {
        var mergedRanged = MergeSelections(entry.SavedRangedWeapons, [stats.WeaponChoice1, stats.WeaponChoice2, stats.WeaponChoice3], includeMelee: false);
        var mergedCc = MergeSelections(entry.SavedCcWeapons, [stats.WeaponChoice1, stats.WeaponChoice2, stats.WeaponChoice3], includeMelee: true);
        var mergedSkills = MergeSelections(entry.SavedSkills, [stats.SkillChoice1, stats.SkillChoice2, stats.SkillChoice3], includeMelee: false);
        var mergedEquipment = MergeSelections(entry.SavedEquipment, [stats.EquipmentChoice1, stats.EquipmentChoice2, stats.EquipmentChoice3], includeMelee: false);
        var updatedName = string.IsNullOrWhiteSpace(stats.CaptainName) ? entry.Name : stats.CaptainName.Trim();
        var updatedStatline = BuildAdjustedStatline(entry.Subtitle, stats);

        return new MercsCompanyEntry
        {
            Name = updatedName,
            BaseUnitName = entry.BaseUnitName,
            NameFormatted = CompanyProfileTextService.BuildNameFormatted(updatedName),
            CostDisplay = entry.CostDisplay,
            CostValue = entry.CostValue,
            ProfileKey = entry.ProfileKey,
            IsLieutenant = entry.IsLieutenant,
            SourceUnitId = entry.SourceUnitId,
            SourceFactionId = entry.SourceFactionId,
            LogoSourceFactionId = entry.LogoSourceFactionId,
            LogoSourceUnitId = entry.LogoSourceUnitId,
            CachedLogoPath = entry.CachedLogoPath,
            PackagedLogoPath = entry.PackagedLogoPath,
            Subtitle = updatedStatline,
            UnitTypeCode = entry.UnitTypeCode,
            SavedEquipment = mergedEquipment,
            SavedSkills = mergedSkills,
            SavedCharacteristics = entry.SavedCharacteristics,
            SavedRangedWeapons = mergedRanged,
            SavedCcWeapons = mergedCc,
            UnitMoveFirstCm = entry.UnitMoveFirstCm,
            UnitMoveSecondCm = entry.UnitMoveSecondCm,
            UnitMoveDisplay = entry.UnitMoveDisplay,
            HasPeripheralStatBlock = entry.HasPeripheralStatBlock,
            PeripheralNameHeading = entry.PeripheralNameHeading,
            PeripheralMov = entry.PeripheralMov,
            PeripheralCc = entry.PeripheralCc,
            PeripheralBs = entry.PeripheralBs,
            PeripheralPh = entry.PeripheralPh,
            PeripheralWip = entry.PeripheralWip,
            PeripheralArm = entry.PeripheralArm,
            PeripheralBts = entry.PeripheralBts,
            PeripheralVitalityHeader = entry.PeripheralVitalityHeader,
            PeripheralVitality = entry.PeripheralVitality,
            PeripheralS = entry.PeripheralS,
            PeripheralAva = entry.PeripheralAva,
            PeripheralMoveFirstCm = entry.PeripheralMoveFirstCm,
            PeripheralMoveSecondCm = entry.PeripheralMoveSecondCm,
            SavedPeripheralEquipment = entry.SavedPeripheralEquipment,
            SavedPeripheralSkills = entry.SavedPeripheralSkills,
            SavedPeripheralCharacteristics = entry.SavedPeripheralCharacteristics,
            EquipmentLineFormatted = CompanyProfileTextService.BuildMercsCompanyLineFormatted("Equipment", mergedEquipment, Color.FromArgb("#06B6D4")),
            HasEquipmentLine = !string.Equals(mergedEquipment, "-", StringComparison.Ordinal),
            SkillsLineFormatted = CompanyProfileTextService.BuildMercsCompanyLineFormatted("Skills", mergedSkills, Color.FromArgb("#F59E0B")),
            HasSkillsLine = !string.Equals(mergedSkills, "-", StringComparison.Ordinal),
            RangedLineFormatted = CompanyProfileTextService.BuildMercsCompanyLineFormatted("Ranged Weapons", mergedRanged, Color.FromArgb("#EF4444")),
            CcLineFormatted = CompanyProfileTextService.BuildMercsCompanyLineFormatted("CC Weapons", mergedCc, Color.FromArgb("#22C55E")),
            PeripheralEquipmentLineFormatted = entry.PeripheralEquipmentLineFormatted,
            HasPeripheralEquipmentLine = entry.HasPeripheralEquipmentLine,
            PeripheralSkillsLineFormatted = entry.PeripheralSkillsLineFormatted,
            HasPeripheralSkillsLine = entry.HasPeripheralSkillsLine,
            ExperiencePoints = 20,
            Perks = entry.Perks
                .Select(x => new CompanyTrooperPerkState { Id = x.Id, Rank = x.Rank })
                .ToList(),
            IsIrregular = entry.IsIrregular,
            NormallyIrregular = entry.NormallyIrregular,
            IsSelected = entry.IsSelected,
            IsDetailsExpanded = entry.IsDetailsExpanded
        };
    }

    private static string MergeSelections(
        string existing,
        IEnumerable<string?> additions,
        bool includeMelee)
    {
        var values = CompanyProfileTextService.SplitDisplayLine(existing);
        foreach (var addition in additions)
        {
            if (string.IsNullOrWhiteSpace(addition))
            {
                continue;
            }

            var trimmed = addition.Trim();
            var isMeleeChoice = trimmed.Contains("cc weapon", StringComparison.OrdinalIgnoreCase) ||
                                trimmed.Contains("melee", StringComparison.OrdinalIgnoreCase);
            if (includeMelee != isMeleeChoice)
            {
                continue;
            }

            if (values.Any(x => string.Equals(x, trimmed, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            values.Add(trimmed);
        }

        return CompanyProfileTextService.JoinOrDash(values);
    }

    private static string BuildAdjustedStatline(string? subtitle, SavedImprovedCaptainStats stats)
    {
        if (string.IsNullOrWhiteSpace(subtitle))
        {
            return subtitle ?? string.Empty;
        }

        var statMap = ParseStatline(subtitle);
        if (statMap.Count == 0)
        {
            return subtitle;
        }

        ApplyStatBonus(statMap, "CC", stats.CcBonus);
        ApplyStatBonus(statMap, "BS", stats.BsBonus);
        ApplyStatBonus(statMap, "PH", stats.PhBonus);
        ApplyStatBonus(statMap, "WIP", stats.WipBonus);
        ApplyStatBonus(statMap, "ARM", stats.ArmBonus);
        ApplyStatBonus(statMap, "BTS", stats.BtsBonus);
        ApplyStatBonus(statMap, "VITA", stats.VitalityBonus);
        ApplyStatBonus(statMap, "STR", stats.VitalityBonus);

        return string.Join(" | ", statMap.Select(x => $"{x.Key} {x.Value}"));
    }

    private static Dictionary<string, string> ParseStatline(string statline)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var rawPart in statline.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            var part = rawPart.Trim();
            var firstSpace = part.IndexOf(' ');
            if (firstSpace <= 0 || firstSpace >= part.Length - 1)
            {
                continue;
            }

            var key = part[..firstSpace].Trim();
            var value = part[(firstSpace + 1)..].Trim();
            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            map[key] = value;
        }

        return map;
    }

    private static void ApplyStatBonus(Dictionary<string, string> statMap, string key, int bonus)
    {
        if (bonus == 0 || !statMap.TryGetValue(key, out var currentValue))
        {
            return;
        }

        if (!int.TryParse(currentValue, out var parsedBase))
        {
            return;
        }

        statMap[key] = (parsedBase + bonus).ToString();
    }

    private void ReplaceMercsCompanyEntry(MercsCompanyEntry original, MercsCompanyEntry replacement)
    {
        var index = MercsCompanyEntries.IndexOf(original);
        if (index < 0)
        {
            return;
        }

        MercsCompanyEntries[index] = replacement;
        UpdateMercsCompanyTotal();
        ApplyLieutenantVisualStates();
        _ = ApplyUnitVisibilityFiltersAsync();
    }
}
