using InfinityMercsApp.Views.Controls;

namespace InfinityMercsApp.Views.Common;

public abstract partial class CompanySelectionPageBase
{
    /// <summary>
    /// Delegates to the workflow to apply a faction selection to the current slot state.
    /// </summary>
    protected static void SetSelectedFactionCore<TFaction>(
        FactionSlotSelectionState<TFaction> factionSelectionState,
        TFaction item,
        Action<TFaction> assignSelectedFactionToActiveSlot)
        where TFaction : CompanyFactionSelectionItemBase
    {
        CompanySelectionFactionSlotsWorkflow.SetSelectedFaction(
            factionSelectionState,
            item,
            assignSelectedFactionToActiveSlot);
    }

    /// <summary>
    /// Returns <c>true</c> if the chosen faction is already assigned to the active slot,
    /// preventing the user from selecting the same faction twice in the same slot.
    /// </summary>
    protected static bool IsDuplicateSelectionForActiveSlotCore<TFaction>(
        bool showRightSelectionBox,
        int activeSlotIndex,
        TFaction? leftSlotFaction,
        TFaction? rightSlotFaction,
        TFaction item)
        where TFaction : CompanyFactionSelectionItemBase
    {
        return CompanySelectionFactionSlotsWorkflow.IsDuplicateSelectionForActiveSlot(
            showRightSelectionBox,
            activeSlotIndex,
            leftSlotFaction,
            rightSlotFaction,
            item);
    }

    /// <summary>
    /// Attempts to assign the selected faction to the active slot.
    /// Returns <c>true</c> when the assignment succeeds; sets <paramref name="factionChanged"/>
    /// to indicate whether the slot actually changed so callers can trigger side-effects accordingly.
    /// </summary>
    protected static bool TryAssignSelectedFactionToActiveSlotCore<TFaction>(
        bool showRightSelectionBox,
        int activeSlotIndex,
        FactionSlotSelectionState<TFaction> factionSelectionState,
        TFaction item,
        Action<int, string> setSlotText,
        Action<int, string?, string?> loadSlotIcon,
        out bool factionChanged)
        where TFaction : CompanyFactionSelectionItemBase
    {
        return CompanySelectionFactionSlotsWorkflow.TryAssignSelectedFactionToActiveSlot(
            showRightSelectionBox,
            activeSlotIndex,
            factionSelectionState,
            item,
            setSlotText,
            loadSlotIcon,
            out factionChanged);
    }

    /// <summary>
    /// Clears all entries from the merc company roster and updates the total cost display.
    /// The optional <paramref name="postReset"/> callback runs after the collection is cleared.
    /// </summary>
    protected static void ResetMercsCompanyCore<TEntry>(
        ICollection<TEntry> mercsCompanyEntries,
        Action updateMercsCompanyTotal,
        Action? postReset = null)
    {
        CompanySelectionFactionSlotsWorkflow.ResetMercsCompany(
            mercsCompanyEntries,
            updateMercsCompanyTotal,
            postReset);
    }

    /// <summary>
    /// Resolves the effective active slot index, clamping it to valid bounds
    /// based on whether the right selection box is visible.
    /// </summary>
    protected static int ResolveActiveSlotIndexCore(int index, bool showRightSelectionBox)
    {
        return CompanySelectionFactionSlotsWorkflow.ResolveActiveSlotIndex(index, showRightSelectionBox);
    }

    /// <summary>
    /// Determines which slot should be auto-selected after a faction assignment,
    /// preferring empty slots so the user is guided through filling both slots.
    /// </summary>
    protected static int ResolveAutoSelectedSlotIndexCore<TFaction>(
        bool showRightSelectionBox,
        TFaction? leftSlotFaction,
        TFaction? rightSlotFaction,
        int currentActiveSlotIndex)
        where TFaction : class
    {
        return CompanySelectionFactionSlotsWorkflow.ResolveAutoSelectedSlotIndex(
            showRightSelectionBox,
            leftSlotFaction,
            rightSlotFaction,
            currentActiveSlotIndex);
    }

    /// <summary>
    /// Triggers the appropriate side effects after a faction slot assignment:
    /// auto-selects the next empty slot, resets the roster if the faction changed,
    /// and initiates a unit load for the newly active slot.
    /// </summary>
    protected static void HandleFactionAssignmentSideEffectsCore(
        bool factionChanged,
        Action autoSelectEmptySlot,
        Action resetMercsCompany,
        Func<Task> loadUnitsForActiveSlotAsync,
        Action? onAssignmentCompleted = null)
    {
        CompanySelectionFactionSlotsWorkflow.HandleFactionAssignmentSideEffects(
            factionChanged,
            autoSelectEmptySlot,
            resetMercsCompany,
            loadUnitsForActiveSlotAsync,
            onAssignmentCompleted);
    }
}
