using InfinityMercsApp.ViewModels;
using InfinityMercsApp.Views.Common;
using InspiringGen = InfinityMercsApp.Infrastructure.Providers.InspiringCompanyFactionGenerator;

namespace InfinityMercsApp.Views.InspiringCompany;

public partial class InspiringCompanySelectionPage
{
    private const string InspiringLeadershipSkillName = "Inspiring Leadership";

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

    private void AddProfileToMercsCompany(ViewerProfileItem? profile)
    {
        if (profile is null || _selectedUnit is null)
        {
            return;
        }

        if (!profile.IsVisible || profile.IsLieutenantBlocked)
        {
            return;
        }

        if (IsLeftSlotUnit(_selectedUnit) && HasLeftSlotEntry())
        {
            return;
        }

        var commonSkills = UnitDisplayConfigurationsView.SelectedUnitCommonSkills;
        var captainSkills = profile.IsLieutenant
            ? EnsureInspiringLeadershipSkill(commonSkills)
            : commonSkills;

        var peripheralStats = BuildMercsCompanyPeripheralStats(profile);
        var entry = BuildMercsCompanyEntryCore<MercsCompanyEntry, ArmyUnitSelectionItem, PeripheralMercsCompanyStats>(
            _selectedUnit,
            profile,
            UnitDisplayConfigurationsView.SelectedUnitCommonEquipment,
            captainSkills,
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

        var isLeftSlotEntry = IsLeftSlotUnit(_selectedUnit);

        AddMercsCompanyEntryCore(
            entry,
            MercsCompanyEntries,
            x => x.IsLieutenant,
            UpdateMercsCompanyTotal,
            ApplyLieutenantVisualStates,
            () => _ = ApplyUnitVisibilityFiltersAsync());

        if (isLeftSlotEntry)
        {
            SetActiveSlot(1);
        }
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
            () => IsFactionSelectionActive = false,
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

        if (IsLeftSlotUnit(_selectedUnit) && HasLeftSlotEntry())
        {
            foreach (var profile in Profiles)
            {
                profile.IsLieutenantBlocked = true;
            }

            visibleProfiles = 0;
        }

        ProfilesStatus = visibleProfiles == 0
            ? "No configurations found for this unit."
            : $"{visibleProfiles} configurations loaded.";

        UpdatePeripheralStatBlockFromVisibleProfiles();
        UpdateSeasonValidationState();
    }

    private static bool IsLeftSlotUnit(ArmyUnitSelectionItem? unit)
    {
        return unit is not null && unit.SourceFactionId != InspiringGen.InspiringCompanyFactionId;
    }

    private bool HasLeftSlotEntry()
    {
        return MercsCompanyEntries.Any(e => e.SourceFactionId != InspiringGen.InspiringCompanyFactionId);
    }

    private static IReadOnlyCollection<string> EnsureInspiringLeadershipSkill(IReadOnlyCollection<string> skills)
    {
        if (skills.Any(x => string.Equals(x?.Trim(), InspiringLeadershipSkillName, StringComparison.OrdinalIgnoreCase)))
        {
            return skills;
        }

        var merged = skills
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .ToList();
        merged.Add(InspiringLeadershipSkillName);
        return merged;
    }
}
