using InfinityMercsApp.Views.Controls;

namespace InfinityMercsApp.Views.Common;

public abstract partial class CompanySelectionPageBase
{
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

    protected static int ResolveActiveSlotIndexCore(int index, bool showRightSelectionBox)
    {
        return CompanySelectionFactionSlotsWorkflow.ResolveActiveSlotIndex(index, showRightSelectionBox);
    }

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
