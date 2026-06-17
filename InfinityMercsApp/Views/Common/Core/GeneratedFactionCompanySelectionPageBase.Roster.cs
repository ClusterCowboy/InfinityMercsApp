using InfinityMercsApp.ViewModels;
using InfinityMercsApp.Views.Common;
using InfinityMercsApp.Services;

namespace InfinityMercsApp.Views.Common;

public abstract partial class GeneratedFactionCompanySelectionPageBase
{
    protected void SetSelectedUnit(ArmyUnitSelectionItem item)
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

    protected void AddProfileToMercsCompany(ViewerProfileItem? profile)
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

        var commonSkills = UnitDisplayConfigurationsViewForVisuals.SelectedUnitCommonSkills;
        var captainSkills = ApplyCaptainSkills(commonSkills, profile);

        var peripheralStats = BuildMercsCompanyPeripheralStats(profile);
        var entry = BuildMercsCompanyEntryCore(
            _selectedUnit,
            profile,
            UnitDisplayConfigurationsViewForVisuals.SelectedUnitCommonEquipment,
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
            UnitDisplayConfigurationsViewForVisuals.SelectedUnitProfileGroupsJson,
            UnitDisplayConfigurationsViewForVisuals.SelectedUnitFiltersJson,
            (peripheralName, peripheralProfile, selectedUnitFiltersJson) =>
                BuildPeripheralStatBlock(peripheralName, peripheralProfile, selectedUnitFiltersJson));
    }

    protected void RemoveMercsCompanyEntry(MercsCompanyEntry? entry)
    {
        RemoveMercsCompanyEntryCore(
            entry,
            MercsCompanyEntries,
            UpdateMercsCompanyTotal,
            ApplyLieutenantVisualStates,
            () => _ = ApplyUnitVisibilityFiltersAsync());
    }

    protected async Task SelectMercsCompanyEntryAsync(MercsCompanyEntry? entry, CancellationToken cancellationToken = default)
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

    protected void ApplyLieutenantVisualStates()
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

    protected bool IsLeftSlotUnit(ArmyUnitSelectionItem? unit)
    {
        return unit is not null && unit.SourceFactionId != CompanyFactionId;
    }

    protected bool HasLeftSlotEntry()
    {
        return MercsCompanyEntries.Any(e => e.SourceFactionId != CompanyFactionId);
    }
}
