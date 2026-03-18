using System.Text.Json;
using InfinityMercsApp.ViewModels;
using InfinityMercsApp.Views.Controls;
using ArmyUnitRecord = InfinityMercsApp.Domain.Models.Army.Unit;

namespace InfinityMercsApp.Views.Common;

public abstract partial class CompanySelectionPageBase
{
    /// <summary>
    /// Builds the peripheral (e.g. remote or peripheral device) stat block for the currently
    /// selected unit profile, returning <c>null</c> when the profile has no peripheral stats.
    /// </summary>
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

    /// <summary>
    /// Updates the visibility state of unit profiles based on whether the selected unit
    /// meets the lieutenant constraints, points limit, and AVA restrictions.
    /// Returns the count of visible lieutenant profiles.
    /// </summary>
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

    /// <summary>
    /// Constructs a fully populated roster entry from the selected unit, chosen profile,
    /// equipment/skills lists, and computed stat strings.
    /// </summary>
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

    /// <summary>
    /// Applies a unit selection, reloading the logo and details when the unit changes.
    /// When the same unit is re-selected but the context has changed,
    /// <paramref name="onContextChangedForSameSelection"/> is invoked instead.
    /// </summary>
    protected static TUnit SetSelectedUnitCore<TUnit>(
        TUnit item,
        TUnit? currentSelectedUnit,
        bool selectionContextChanged,
        Action onContextChangedForSameSelection,
        Action<TUnit> loadSelectedUnitLogo,
        Action<TUnit> loadSelectedUnitDetails)
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

    /// <summary>
    /// Variant of <see cref="SetSelectedUnitCore{TUnit}"/> that also tracks a typed context value
    /// (e.g. fireteam slot index), updating it and triggering reload when it differs from the current context.
    /// </summary>
    protected static TUnit SetSelectedUnitWithContextCore<TUnit, TContext>(
        TUnit item,
        TUnit? currentSelectedUnit,
        TContext currentContext,
        TContext requestedContext,
        Action<TContext> setContext,
        Action onContextChangedForSameSelection,
        Action<TUnit> loadSelectedUnitLogo,
        Action<TUnit> loadSelectedUnitDetails)
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

    /// <summary>
    /// Updates the visual lieutenant badge states on roster entries and profile items
    /// based on points remaining, AVA, and the active unit filter.
    /// Returns the count of entries marked as lieutenant.
    /// </summary>
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

    /// <summary>
    /// Adds a new entry to the roster, inserting lieutenant entries at the front of the list
    /// so they remain visually prominent. Triggers cost and visibility updates after insertion.
    /// </summary>
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

    /// <summary>
    /// Removes an entry from the roster and triggers cost and visibility updates.
    /// Does nothing when <paramref name="entry"/> is <c>null</c>.
    /// </summary>
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

    /// <summary>
    /// Selects a roster entry for detailed viewing, resolving its corresponding unit from the loaded
    /// unit list or falling back to a freshly fetched unit record when not found.
    /// Switches the view to unit-selection mode and loads the full unit details.
    /// </summary>
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
