using InfinityMercsApp.ViewModels;
using InfinityMercsApp.Views.Common;

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
            loadSelectedUnitDetails: () => _ = LoadSelectedUnitDetailsAsync());
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
        var pointsLimit = int.TryParse(SelectedStartSeasonPoints, out var parsedLimit) ? parsedLimit : 0;
        var currentPoints = int.TryParse(SeasonPointsCapText, out var parsedPoints) ? parsedPoints : 0;
        var avaLimit = CompanySelectionSharedUtilities.ParseAvaLimit(UnitAva);
        var visibleProfiles = ApplyLieutenantProfileVisibilityCore(
            MercsCompanyEntries,
            Profiles,
            pointsLimit,
            currentPoints,
            avaLimit,
            _selectedUnit?.Id,
            _selectedUnit?.SourceFactionId,
            _filterState.ActiveUnitFilter,
            LieutenantOnlyUnits,
            entry => entry.IsLieutenant,
            entry => entry.SourceUnitId,
            entry => entry.SourceFactionId);

        ProfilesStatus = visibleProfiles == 0
            ? "No configurations found for this unit."
            : $"{visibleProfiles} configurations loaded.";

        UpdatePeripheralStatBlockFromVisibleProfiles();
        UpdateSeasonValidationState();
    }
}
