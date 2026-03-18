using InfinityMercsApp.ViewModels;
using InfinityMercsApp.Views.Common;

namespace InfinityMercsApp.Views.CohesiveCompany;

public partial class CohesiveCompanySelectionPage
{
    private const string CohesiveDefaultSkill = "Number 2";

    private void SetSelectedUnit(ArmyUnitSelectionItem item, bool restrictProfilesToFto = false)
    {
        _selectedUnit = SetSelectedUnitWithContextCore(
            item,
            _selectedUnit,
            _restrictSelectedUnitProfilesToFto,
            restrictProfilesToFto,
            value => _restrictSelectedUnitProfilesToFto = value,
            onContextChangedForSameSelection: () => _ = LoadSelectedUnitDetailsAsync(),
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

        var peripheralStats = BuildMercsCompanyPeripheralStats(profile);
        var cohesiveCommonSkills = EnsureCohesiveDefaultSkills(UnitDisplayConfigurationsView.SelectedUnitCommonSkills);
        var entry = BuildMercsCompanyEntryCore<MercsCompanyEntry, ArmyUnitSelectionItem, PeripheralMercsCompanyStats>(
            _selectedUnit,
            profile,
            UnitDisplayConfigurationsView.SelectedUnitCommonEquipment,
            cohesiveCommonSkills,
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
            () =>
            {
                ReevaluateTrackedFireteamLevel();
                ApplyLieutenantVisualStates();
            },
            () => _ = ApplyUnitVisibilityFiltersAsync());
    }

    private static IReadOnlyList<string> EnsureCohesiveDefaultSkills(IReadOnlyCollection<string> selectedUnitCommonSkills)
    {
        var skills = selectedUnitCommonSkills
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .ToList();

        if (!skills.Any(x => x.Contains("number 2", StringComparison.OrdinalIgnoreCase)))
        {
            skills.Add(CohesiveDefaultSkill);
        }

        return skills;
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
            () =>
            {
                ReevaluateTrackedFireteamLevel();
                ApplyLieutenantVisualStates();
            },
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
            item => SetSelectedUnit(item),
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

        ProfilesStatus = visibleProfiles == 0
            ? "No configurations found for this unit."
            : $"{visibleProfiles} configurations loaded.";

        UpdatePeripheralStatBlockFromVisibleProfiles();
        UpdateSeasonValidationState();
    }
}
