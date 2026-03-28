using InfinityMercsApp.ViewModels;
using InfinityMercsApp.Views.Common;
using InfinityMercsApp.Services;
using AirborneGen = InfinityMercsApp.Infrastructure.Providers.AirborneCompanyFactionGenerator;

namespace InfinityMercsApp.Views.AirborneCompany;

public partial class AirborneCompanySelectionPage
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

    private const string ParachutistSkillName = "Parachutist";
    private const string NetworkSupportSkillName = "Network Support (Controlled Jump)";

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
        if (IsLeftSlotUnit(_selectedUnit))
        {
            commonSkills = BuildCaptainSkills(commonSkills, profile.UniqueSkills);
        }

        var peripheralStats = BuildMercsCompanyPeripheralStats(profile);
        var entry = BuildMercsCompanyEntryCore<MercsCompanyEntry, ArmyUnitSelectionItem, PeripheralMercsCompanyStats>(
            _selectedUnit,
            profile,
            UnitDisplayConfigurationsView.SelectedUnitCommonEquipment,
            commonSkills,
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

    private static List<string> BuildCaptainSkills(IReadOnlyCollection<string> commonSkills, string? uniqueSkills)
    {
        var allExisting = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var skill in commonSkills)
        {
            allExisting.Add(skill);
        }

        if (!string.IsNullOrWhiteSpace(uniqueSkills))
        {
            foreach (var part in CompanyProfileTextService.SplitDisplayLine(uniqueSkills))
            {
                allExisting.Add(part);
            }
        }

        var result = new List<string>(commonSkills);
        if (!allExisting.Contains(ParachutistSkillName))
        {
            result.Add(ParachutistSkillName);
        }

        if (!allExisting.Contains(NetworkSupportSkillName))
        {
            result.Add(NetworkSupportSkillName);
        }

        return result;
    }

    private static bool IsLeftSlotUnit(ArmyUnitSelectionItem? unit)
    {
        return unit is not null && unit.SourceFactionId != AirborneGen.AirborneCompanyFactionId;
    }

    private bool HasLeftSlotEntry()
    {
        return MercsCompanyEntries.Any(e => e.SourceFactionId != AirborneGen.AirborneCompanyFactionId);
    }
}
