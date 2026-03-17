using System.Text.Json;
using InfinityMercsApp.Services;
using InfinityMercsApp.ViewModels;
using InfinityMercsApp.Views.Controls;
using ArmyUnitRecord = InfinityMercsApp.Domain.Models.Army.Unit;

namespace InfinityMercsApp.Views.Common;

public abstract partial class CompanySelectionPageBase
{
    protected static TPeripheralStats? BuildMercsCompanyPeripheralStatsCore<TPeripheralStats>(
        ViewerProfileItem profile,
        string? selectedUnitProfileGroupsJson,
        string? selectedUnitFiltersJson,
        Func<string, JsonElement, string?, TPeripheralStats?> buildPeripheralStatBlock)
        where TPeripheralStats : class
    {
        var peripheralName = CompanyUnitDetailsShared.ExtractFirstPeripheralName(profile.Peripherals);
        if (string.IsNullOrWhiteSpace(peripheralName) || string.IsNullOrWhiteSpace(selectedUnitProfileGroupsJson))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(selectedUnitProfileGroupsJson);
            if (!CompanyPeripheralProfileSelectionService.TryFindPeripheralStatElement(doc.RootElement, peripheralName, out var peripheralProfile))
            {
                return null;
            }

            return buildPeripheralStatBlock(peripheralName, peripheralProfile, selectedUnitFiltersJson);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ArmyFactionSelectionPage BuildMercsCompanyPeripheralStats failed: {ex.Message}");
            return null;
        }
    }

    protected static int ApplyLieutenantProfileVisibilityCore<TEntry>(
        IEnumerable<TEntry> mercsCompanyEntries,
        IEnumerable<ViewerProfileItem> profiles,
        int pointsLimit,
        int currentPoints,
        int? avaLimit,
        int? selectedUnitId,
        int? selectedUnitSourceFactionId,
        UnitFilterCriteria activeUnitFilter,
        bool lieutenantOnlyUnits,
        Func<TEntry, bool> readIsLieutenant,
        Func<TEntry, int> readSourceUnitId,
        Func<TEntry, int> readSourceFactionId)
    {
        var entries = mercsCompanyEntries.ToList();
        var pointsRemaining = pointsLimit - currentPoints;
        var selectedUnitCountInCompany = (!selectedUnitId.HasValue || !selectedUnitSourceFactionId.HasValue)
            ? 0
            : entries.Count(x =>
                readSourceUnitId(x) == selectedUnitId.Value &&
                readSourceFactionId(x) == selectedUnitSourceFactionId.Value);
        var avaReached = avaLimit.HasValue && selectedUnitCountInCompany >= avaLimit.Value;
        var hasActiveLieutenant = entries.Any(readIsLieutenant);
        var visibleProfiles = 0;

        foreach (var profile in profiles)
        {
            var profileCost = CompanyUnitFilterService.ParseCostValue(profile.Cost);
            var overRemainingPoints = profileCost > pointsRemaining;
            var belowMinFilterPoints = activeUnitFilter.MinPoints.HasValue && profileCost < activeUnitFilter.MinPoints.Value;
            var aboveMaxFilterPoints = activeUnitFilter.MaxPoints.HasValue && profileCost > activeUnitFilter.MaxPoints.Value;
            var lieutenantFilteredOut = lieutenantOnlyUnits && !profile.IsLieutenant;

            profile.IsVisible = !lieutenantFilteredOut &&
                                !overRemainingPoints &&
                                !belowMinFilterPoints &&
                                !aboveMaxFilterPoints;
            profile.IsLieutenantBlocked =
                (hasActiveLieutenant && profile.IsLieutenant) ||
                overRemainingPoints ||
                avaReached;

            if (profile.IsVisible)
            {
                visibleProfiles++;
            }
        }

        return visibleProfiles;
    }

    protected static TEntry BuildMercsCompanyEntryCore<TEntry, TUnit, TPeripheralStats>(
        TUnit selectedUnit,
        ViewerProfileItem profile,
        IReadOnlyCollection<string> selectedUnitCommonEquipment,
        IReadOnlyCollection<string> selectedUnitCommonSkills,
        string unitMov,
        string unitCc,
        string unitBs,
        string unitPh,
        string unitWip,
        string unitArm,
        string unitBts,
        string unitVitalityHeader,
        string unitVitality,
        string unitS,
        int? unitMoveFirstCm,
        int? unitMoveSecondCm,
        Func<int?, int?, string> formatMoveValue,
        TPeripheralStats? peripheralStats)
        where TEntry : CompanyMercsCompanyEntryBase, new()
        where TUnit : CompanyUnitSelectionItemBase
        where TPeripheralStats : CompanyPeripheralMercsCompanyStatsBase
    {
        var combinedEquipment = CompanyProfileTextService.MergeCommonAndUnique(selectedUnitCommonEquipment, profile.UniqueEquipment);
        var combinedSkills = CompanyProfileTextService.MergeCommonAndUnique(selectedUnitCommonSkills, profile.UniqueSkills);
        var combinedEquipmentText = CompanyProfileTextService.JoinOrDash(combinedEquipment);
        var combinedSkillsText = CompanyProfileTextService.JoinOrDash(combinedSkills);
        var currentUnitMove = formatMoveValue(unitMoveFirstCm, unitMoveSecondCm);
        var statline =
            $"MOV {unitMov} | CC {unitCc} | BS {unitBs} | PH {unitPh} | WIP {unitWip} | ARM {unitArm} | BTS {unitBts} | {unitVitalityHeader} {unitVitality} | S {unitS}";

        return new TEntry
        {
            Name = profile.Name,
            BaseUnitName = selectedUnit.Name,
            NameFormatted = profile.NameFormatted ?? CompanyProfileTextService.BuildNameFormatted(profile.Name),
            Subtitle = statline,
            UnitTypeCode = ExtractUnitTypeCode(selectedUnit.Subtitle),
            CostDisplay = $"C {profile.Cost}",
            CostValue = CompanyUnitFilterService.ParseCostValue(profile.Cost),
            IsLieutenant = profile.IsLieutenant,
            ProfileKey = profile.ProfileKey,
            SourceUnitId = selectedUnit.Id,
            SourceFactionId = selectedUnit.SourceFactionId,
            CachedLogoPath = selectedUnit.CachedLogoPath,
            PackagedLogoPath = selectedUnit.PackagedLogoPath,
            SavedEquipment = combinedEquipmentText,
            SavedSkills = combinedSkillsText,
            SavedRangedWeapons = profile.RangedWeapons,
            SavedCcWeapons = profile.MeleeWeapons,
            ExperiencePoints = 0,
            EquipmentLineFormatted = CompanyProfileTextService.BuildMercsCompanyLineFormatted("Equipment", combinedEquipmentText, Color.FromArgb("#06B6D4")),
            HasEquipmentLine = combinedEquipment.Count > 0,
            SkillsLineFormatted = CompanyProfileTextService.BuildMercsCompanyLineFormatted("Skills", combinedSkillsText, Color.FromArgb("#F59E0B")),
            HasSkillsLine = combinedSkills.Count > 0,
            RangedLineFormatted = CompanyProfileTextService.BuildMercsCompanyLineFormatted("Ranged Weapons", profile.RangedWeapons, Color.FromArgb("#EF4444")),
            CcLineFormatted = CompanyProfileTextService.BuildMercsCompanyLineFormatted("CC Weapons", profile.MeleeWeapons, Color.FromArgb("#22C55E")),
            HasPeripheralStatBlock = peripheralStats is not null,
            PeripheralNameHeading = peripheralStats?.NameHeading ?? string.Empty,
            PeripheralMov = peripheralStats is null ? "-" : formatMoveValue(peripheralStats.MoveFirstCm, peripheralStats.MoveSecondCm),
            PeripheralCc = peripheralStats?.Cc ?? "-",
            PeripheralBs = peripheralStats?.Bs ?? "-",
            PeripheralPh = peripheralStats?.Ph ?? "-",
            PeripheralWip = peripheralStats?.Wip ?? "-",
            PeripheralArm = peripheralStats?.Arm ?? "-",
            PeripheralBts = peripheralStats?.Bts ?? "-",
            PeripheralVitalityHeader = peripheralStats?.VitalityHeader ?? "VITA",
            PeripheralVitality = peripheralStats?.Vitality ?? "-",
            PeripheralS = peripheralStats?.S ?? "-",
            PeripheralAva = peripheralStats?.Ava ?? "-",
            SavedPeripheralEquipment = peripheralStats?.Equipment ?? "-",
            SavedPeripheralSkills = peripheralStats?.Skills ?? "-",
            PeripheralEquipmentLineFormatted =
                CompanyProfileTextService.BuildMercsCompanyLineFormatted("Equipment", peripheralStats?.Equipment, Color.FromArgb("#06B6D4")),
            HasPeripheralEquipmentLine =
                peripheralStats is not null && !string.IsNullOrWhiteSpace(peripheralStats.Equipment) && peripheralStats.Equipment != "-",
            PeripheralSkillsLineFormatted =
                CompanyProfileTextService.BuildMercsCompanyLineFormatted("Skills", peripheralStats?.Skills, Color.FromArgb("#F59E0B")),
            HasPeripheralSkillsLine =
                peripheralStats is not null && !string.IsNullOrWhiteSpace(peripheralStats.Skills) && peripheralStats.Skills != "-",
            UnitMoveFirstCm = unitMoveFirstCm,
            UnitMoveSecondCm = unitMoveSecondCm,
            UnitMoveDisplay = currentUnitMove,
            PeripheralMoveFirstCm = peripheralStats?.MoveFirstCm,
            PeripheralMoveSecondCm = peripheralStats?.MoveSecondCm
        };
    }

    protected static TUnit SetSelectedUnitCore<TUnit>(
        TUnit item,
        TUnit? currentSelectedUnit,
        bool selectionContextChanged,
        Action onContextChangedForSameSelection,
        Action<TUnit> loadSelectedUnitLogo,
        Action loadSelectedUnitDetails)
        where TUnit : CompanyUnitSelectionItemBase
    {
        Console.WriteLine($"ArmyFactionSelectionPage SetSelectedUnit requested: id={item.Id}, faction={item.SourceFactionId}, name='{item.Name}'.");
        if (currentSelectedUnit == item)
        {
            if (selectionContextChanged)
            {
                Console.WriteLine("ArmyFactionSelectionPage SetSelectedUnit context changed; reloading selected unit details.");
                onContextChangedForSameSelection();
            }
            else
            {
                Console.WriteLine("ArmyFactionSelectionPage SetSelectedUnit skipped (same item instance).");
            }

            return currentSelectedUnit;
        }

        if (currentSelectedUnit is not null)
        {
            currentSelectedUnit.IsSelected = false;
        }

        item.IsSelected = true;
        Console.WriteLine($"ArmyFactionSelectionPage selected unit now: id={item.Id}, faction={item.SourceFactionId}, name='{item.Name}'.");
        loadSelectedUnitLogo(item);
        loadSelectedUnitDetails();
        return item;
    }

    protected static void AddMercsCompanyEntryCore<TEntry>(
        TEntry entry,
        IList<TEntry> mercsCompanyEntries,
        Func<TEntry, bool> readIsLieutenant,
        Action updateMercsCompanyTotal,
        Action postEntryMutation,
        Action applyUnitVisibilityFilters)
    {
        if (readIsLieutenant(entry))
        {
            mercsCompanyEntries.Insert(0, entry);
        }
        else
        {
            mercsCompanyEntries.Add(entry);
        }

        updateMercsCompanyTotal();
        postEntryMutation();
        applyUnitVisibilityFilters();
    }

    protected static void RemoveMercsCompanyEntryCore<TEntry>(
        TEntry? entry,
        ICollection<TEntry> mercsCompanyEntries,
        Action updateMercsCompanyTotal,
        Action postEntryMutation,
        Action applyUnitVisibilityFilters)
        where TEntry : class
    {
        if (entry is null)
        {
            return;
        }

        mercsCompanyEntries.Remove(entry);
        updateMercsCompanyTotal();
        postEntryMutation();
        applyUnitVisibilityFilters();
    }

    protected static async Task SelectMercsCompanyEntryAsyncCore<TEntry, TUnit>(
        TEntry? entry,
        IReadOnlyList<TUnit> units,
        Func<int, int, CancellationToken, ArmyUnitRecord?> getUnitFromProvider,
        Func<int, int, string, string?, string?, TUnit> createFallbackUnit,
        Action<TUnit> setSelectedUnit,
        Func<CancellationToken, Task> loadSelectedUnitDetailsAsync,
        Action setFactionSelectionInactive,
        CancellationToken cancellationToken = default)
        where TEntry : class, ICompanyMercsEntry
        where TUnit : CompanyUnitSelectionItemBase
    {
        if (entry is null)
        {
            return;
        }

        try
        {
            // Prefer existing unit item (even if hidden), otherwise build a temporary item
            // so details can load regardless of current list visibility filters.
            var unitItem = units.FirstOrDefault(x =>
                x.Id == entry.SourceUnitId &&
                x.SourceFactionId == entry.SourceFactionId);

            if (unitItem is null)
            {
                var unitRecord = getUnitFromProvider(entry.SourceFactionId, entry.SourceUnitId, cancellationToken);
                var unitName = !string.IsNullOrWhiteSpace(unitRecord?.Name)
                    ? unitRecord.Name
                    : entry.Name;

                unitItem = createFallbackUnit(
                    entry.SourceUnitId,
                    entry.SourceFactionId,
                    unitName,
                    entry.CachedLogoPath,
                    entry.PackagedLogoPath);
            }

            setSelectedUnit(unitItem);
            // Force-refresh details/configurations even if the selected unit instance
            // did not change (SetSelectedUnit can short-circuit on same instance).
            await loadSelectedUnitDetailsAsync(cancellationToken);
            setFactionSelectionInactive();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ArmyFactionSelectionPage SelectMercsCompanyEntryAsync failed: {ex.Message}");
        }
    }
}
