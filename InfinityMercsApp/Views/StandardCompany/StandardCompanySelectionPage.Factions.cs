using InfinityMercsApp.Views.Common;
using InfinityMercsApp.Views.Controls;

namespace InfinityMercsApp.Views.StandardCompany;

public partial class StandardCompanySelectionPage
{
    private async Task LoadFactionsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var filteredFactions = await LoadFilteredFactionRecordsAsync(cancellationToken);
            var items = BuildFactionSelectionItems(
                filteredFactions,
                (id, parentId, name, cachedLogoPath, packagedLogoPath) => new ArmyFactionSelectionItem
                {
                    Id = id,
                    ParentId = parentId,
                    Name = name,
                    CachedLogoPath = cachedLogoPath,
                    PackagedLogoPath = packagedLogoPath
                });

            Factions.Clear();
            foreach (var faction in items)
            {
                Factions.Add(faction);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ArmyFactionSelectionPage LoadFactionsAsync failed: {ex.Message}");
        }
    }

    private void SetSelectedFaction(ArmyFactionSelectionItem item)
    {
        SetSelectedFactionCore(
            _factionSelectionState,
            item,
            AssignSelectedFactionToActiveSlot);
    }

    private void AssignSelectedFactionToActiveSlot(ArmyFactionSelectionItem item)
    {
        if (!TryAssignSelectedFactionToActiveSlotCore(
                ShowRightSelectionBox,
                _activeSlotIndex,
                _factionSelectionState,
                item,
                (slotIndex, text) =>
                {
                    if (slotIndex == 0)
                    {
                        FactionSlotSelectorView.LeftSlotText = text;
                    }
                    else
                    {
                        FactionSlotSelectorView.RightSlotText = text;
                    }
                },
                (slotIndex, cachedPath, packagedPath) => _ = LoadSlotIconAsync(slotIndex, cachedPath, packagedPath),
                out var factionChanged))
        {
            Console.WriteLine($"[ArmyFactionSelectionPage] Duplicate selection blocked for faction {item.Id} ({item.Name}).");
            return;
        }

        HandleFactionAssignmentSideEffectsCore(
            factionChanged,
            AutoSelectEmptySlot,
            ResetMercsCompany,
            () => LoadUnitsForActiveSlotAsync());
    }

    private void ResetMercsCompany()
    {
        ResetMercsCompanyCore(
            MercsCompanyEntries,
            UpdateMercsCompanyTotal);
    }

    private void AutoSelectEmptySlot()
    {
        SetActiveSlot(ResolveAutoSelectedSlotIndexCore(
            ShowRightSelectionBox,
            _factionSelectionState.LeftSlotFaction,
            _factionSelectionState.RightSlotFaction,
            _activeSlotIndex));
    }

    private void SetActiveSlot(int index)
    {
        _activeSlotIndex = ResolveActiveSlotIndexCore(index, ShowRightSelectionBox);
        FactionSlotSelectorView.ApplyActiveSlotBorders(_activeSlotIndex);
    }
}
