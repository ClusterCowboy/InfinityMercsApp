using InfinityMercsApp.Views.Common;
using InfinityMercsApp.Views.Controls;
using AirborneGen = InfinityMercsApp.Infrastructure.Providers.AirborneCompanyFactionGenerator;
using InspiringGen = InfinityMercsApp.Infrastructure.Providers.InspiringCompanyFactionGenerator;

namespace InfinityMercsApp.Views.StandardCompany;

public partial class StandardCompanySelectionPage
{
    private async Task LoadFactionsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var filteredFactions = await LoadFilteredFactionRecordsAsync(cancellationToken);
            filteredFactions = filteredFactions
                .Where(x => x.Id != AirborneGen.AirborneCompanyFactionId &&
                            x.Id != InspiringGen.InspiringCompanyFactionId)
                .ToList();
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
            Console.Error.WriteLine($"CompanySelectionPage LoadFactionsAsync failed: {ex.Message}");
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
                        FactionSlotSelectorView.LeftSlotText = string.Empty;
                    }
                    else
                    {
                        FactionSlotSelectorView.RightSlotText = string.Empty;
                    }
                },
                (slotIndex, cachedPath, packagedPath) => _ = LoadSlotIconAsync(slotIndex, cachedPath, packagedPath),
                out var factionChanged))
        {
            Console.WriteLine($"[CompanySelectionPage] Duplicate selection blocked for faction {item.Id} ({item.Name}).");
            return;
        }

        _autoSelectUnitAfterFactionLoad = factionChanged &&
                                          _factionSelectionState.LeftSlotFaction is not null &&
                                          (!ShowRightSelectionBox || _factionSelectionState.RightSlotFaction is not null);

        HandleFactionAssignmentSideEffectsCore(
            factionChanged,
            AutoSelectEmptySlot,
            ResetMercsCompany,
            () => LoadUnitsForActiveSlotAsync(),
            onAssignmentCompleted: () =>
            {
                if (AllFactionSlotsFilled())
                {
                    IsFactionSelectionActive = false;
                    ShowFactionStrip = false;
                }
            });
    }

    private bool AllFactionSlotsFilled()
    {
        if (_factionSelectionState.LeftSlotFaction is null)
        {
            return false;
        }

        return !ShowRightSelectionBox || _factionSelectionState.RightSlotFaction is not null;
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
