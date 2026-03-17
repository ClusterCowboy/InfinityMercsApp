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
        if (factionSelectionState.SelectedFaction == item)
        {
            assignSelectedFactionToActiveSlot(item);
            return;
        }

        if (factionSelectionState.SelectedFaction is not null)
        {
            factionSelectionState.SelectedFaction.IsSelected = false;
        }

        factionSelectionState.SelectedFaction = item;
        factionSelectionState.SelectedFaction.IsSelected = true;
        assignSelectedFactionToActiveSlot(item);
    }

    protected static bool IsDuplicateSelectionForActiveSlotCore<TFaction>(
        bool showRightSelectionBox,
        int activeSlotIndex,
        TFaction? leftSlotFaction,
        TFaction? rightSlotFaction,
        TFaction item)
        where TFaction : CompanyFactionSelectionItemBase
    {
        if (!showRightSelectionBox)
        {
            return false;
        }

        if (activeSlotIndex == 0)
        {
            return rightSlotFaction is not null
                && rightSlotFaction.Id == item.Id
                && (leftSlotFaction is null || leftSlotFaction.Id != item.Id);
        }

        return leftSlotFaction is not null
            && leftSlotFaction.Id == item.Id
            && (rightSlotFaction is null || rightSlotFaction.Id != item.Id);
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
        if (IsDuplicateSelectionForActiveSlotCore(
                showRightSelectionBox,
                activeSlotIndex,
                factionSelectionState.LeftSlotFaction,
                factionSelectionState.RightSlotFaction,
                item))
        {
            factionChanged = false;
            return false;
        }

        if (activeSlotIndex == 0 || !showRightSelectionBox)
        {
            factionChanged = factionSelectionState.LeftSlotFaction?.Id != item.Id;
            factionSelectionState.LeftSlotFaction = item;
            setSlotText(0, item.Name);
            loadSlotIcon(0, item.CachedLogoPath, item.PackagedLogoPath);
            return true;
        }

        factionChanged = factionSelectionState.RightSlotFaction?.Id != item.Id;
        factionSelectionState.RightSlotFaction = item;
        setSlotText(1, item.Name);
        loadSlotIcon(1, item.CachedLogoPath, item.PackagedLogoPath);
        return true;
    }

    protected static void ResetMercsCompanyCore<TEntry>(
        ICollection<TEntry> mercsCompanyEntries,
        Action updateMercsCompanyTotal,
        Action? postReset = null)
    {
        if (mercsCompanyEntries.Count > 0)
        {
            mercsCompanyEntries.Clear();
        }

        updateMercsCompanyTotal();
        postReset?.Invoke();
    }

    protected static int ResolveActiveSlotIndexCore(int index, bool showRightSelectionBox)
    {
        return index == 1 && showRightSelectionBox ? 1 : 0;
    }

    protected static int ResolveAutoSelectedSlotIndexCore<TFaction>(
        bool showRightSelectionBox,
        TFaction? leftSlotFaction,
        TFaction? rightSlotFaction,
        int currentActiveSlotIndex)
        where TFaction : class
    {
        if (!showRightSelectionBox)
        {
            return 0;
        }

        var leftEmpty = leftSlotFaction is null;
        var rightEmpty = rightSlotFaction is null;

        if (leftEmpty && !rightEmpty)
        {
            return 0;
        }

        if (rightEmpty && !leftEmpty)
        {
            return 1;
        }

        return currentActiveSlotIndex;
    }

    protected static void HandleFactionAssignmentSideEffectsCore(
        bool factionChanged,
        Action autoSelectEmptySlot,
        Action resetMercsCompany,
        Func<Task> loadUnitsForActiveSlotAsync,
        Action? onAssignmentCompleted = null)
    {
        autoSelectEmptySlot();
        if (factionChanged)
        {
            resetMercsCompany();
            _ = loadUnitsForActiveSlotAsync();
        }

        onAssignmentCompleted?.Invoke();
    }
}
