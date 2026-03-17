using System.Text.Json;
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
        return CompanySelectionRosterWorkflow.BuildMercsCompanyPeripheralStats(
            profile,
            selectedUnitProfileGroupsJson,
            selectedUnitFiltersJson,
            buildPeripheralStatBlock);
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
        return CompanySelectionRosterWorkflow.ApplyLieutenantProfileVisibility(
            mercsCompanyEntries,
            profiles,
            pointsLimit,
            currentPoints,
            avaLimit,
            selectedUnitId,
            selectedUnitSourceFactionId,
            activeUnitFilter,
            lieutenantOnlyUnits,
            readIsLieutenant,
            readSourceUnitId,
            readSourceFactionId);
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
        return CompanySelectionRosterWorkflow.BuildMercsCompanyEntry<TEntry, TUnit, TPeripheralStats>(
            selectedUnit,
            profile,
            selectedUnitCommonEquipment,
            selectedUnitCommonSkills,
            unitMov,
            unitCc,
            unitBs,
            unitPh,
            unitWip,
            unitArm,
            unitBts,
            unitVitalityHeader,
            unitVitality,
            unitS,
            unitMoveFirstCm,
            unitMoveSecondCm,
            formatMoveValue,
            peripheralStats);
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
        return CompanySelectionRosterWorkflow.SetSelectedUnit(
            item,
            currentSelectedUnit,
            selectionContextChanged,
            onContextChangedForSameSelection,
            loadSelectedUnitLogo,
            loadSelectedUnitDetails);
    }

    protected static TUnit SetSelectedUnitWithContextCore<TUnit, TContext>(
        TUnit item,
        TUnit? currentSelectedUnit,
        TContext currentContext,
        TContext requestedContext,
        Action<TContext> setContext,
        Action onContextChangedForSameSelection,
        Action<TUnit> loadSelectedUnitLogo,
        Action loadSelectedUnitDetails)
        where TUnit : CompanyUnitSelectionItemBase
        where TContext : IEquatable<TContext>
    {
        return CompanySelectionRosterWorkflow.SetSelectedUnitWithContext(
            item,
            currentSelectedUnit,
            currentContext,
            requestedContext,
            setContext,
            onContextChangedForSameSelection,
            loadSelectedUnitLogo,
            loadSelectedUnitDetails);
    }

    protected static int ApplyLieutenantVisualStatesCore<TEntry, TUnit>(
        IEnumerable<TEntry> mercsCompanyEntries,
        IEnumerable<ViewerProfileItem> profiles,
        string selectedStartSeasonPoints,
        string seasonPointsCapText,
        string unitAva,
        TUnit? selectedUnit,
        UnitFilterCriteria activeUnitFilter,
        bool lieutenantOnlyUnits,
        Func<TEntry, bool> readIsLieutenant,
        Func<TEntry, int> readSourceUnitId,
        Func<TEntry, int> readSourceFactionId,
        Func<TUnit, int> readUnitId,
        Func<TUnit, int> readSourceFactionIdFromUnit)
    {
        return CompanySelectionRosterWorkflow.ApplyLieutenantVisualStates(
            mercsCompanyEntries,
            profiles,
            selectedStartSeasonPoints,
            seasonPointsCapText,
            unitAva,
            selectedUnit,
            activeUnitFilter,
            lieutenantOnlyUnits,
            readIsLieutenant,
            readSourceUnitId,
            readSourceFactionId,
            readUnitId,
            readSourceFactionIdFromUnit);
    }

    protected static void AddMercsCompanyEntryCore<TEntry>(
        TEntry entry,
        IList<TEntry> mercsCompanyEntries,
        Func<TEntry, bool> readIsLieutenant,
        Action updateMercsCompanyTotal,
        Action postEntryMutation,
        Action applyUnitVisibilityFilters)
    {
        CompanySelectionRosterWorkflow.AddMercsCompanyEntry(
            entry,
            mercsCompanyEntries,
            readIsLieutenant,
            updateMercsCompanyTotal,
            postEntryMutation,
            applyUnitVisibilityFilters);
    }

    protected static void RemoveMercsCompanyEntryCore<TEntry>(
        TEntry? entry,
        ICollection<TEntry> mercsCompanyEntries,
        Action updateMercsCompanyTotal,
        Action postEntryMutation,
        Action applyUnitVisibilityFilters)
        where TEntry : class
    {
        CompanySelectionRosterWorkflow.RemoveMercsCompanyEntry(
            entry,
            mercsCompanyEntries,
            updateMercsCompanyTotal,
            postEntryMutation,
            applyUnitVisibilityFilters);
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
        await CompanySelectionRosterWorkflow.SelectMercsCompanyEntryAsync(
            entry,
            units,
            getUnitFromProvider,
            createFallbackUnit,
            setSelectedUnit,
            loadSelectedUnitDetailsAsync,
            setFactionSelectionInactive,
            cancellationToken);
    }
}
