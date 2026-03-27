using InfinityMercsApp.Views.Common;
using InfinityMercsApp.Views.Controls;
using AirborneGen = InfinityMercsApp.Infrastructure.Providers.AirborneCompanyFactionGenerator;
using InspiringGen = InfinityMercsApp.Infrastructure.Providers.InspiringCompanyFactionGenerator;
using TagGen = InfinityMercsApp.Infrastructure.Providers.TagCompanyFactionGenerator;

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
                            x.Id != InspiringGen.InspiringCompanyFactionId &&
                            x.Id != TagGen.TagCompanyFactionId)
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
        var targetSlotIndex = IsTagCompanyMode ? 0 : _activeSlotIndex;
        var targetSlotWasEmpty = targetSlotIndex switch
        {
            0 => _factionSelectionState.LeftSlotFaction is null,
            1 => _factionSelectionState.RightSlotFaction is null,
            _ => _factionSelectionState.LeftSlotFaction is null
        };

        if (!TryAssignSelectedFactionToActiveSlotCore(
                ShowRightSelectionBox,
                targetSlotIndex,
                _factionSelectionState,
                item,
                (slotIndex, cachedPath, packagedPath) => _ = LoadSlotIconAsync(slotIndex, cachedPath, packagedPath),
                blockCrossSlotDuplicateSelection: true,
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
            IsTagCompanyMode
                ? () => SetActiveSlot(0)
                : targetSlotWasEmpty ? AutoSelectEmptySlot : () => SetActiveSlot(targetSlotIndex),
            ResetMercsCompany,
            () => LoadUnitsForActiveSlotAsync(),
            onAssignmentCompleted: () =>
            {
                TeamsView = false;
                if (IsTagCompanyMode)
                {
                    SetActiveSlot(0);
                }

                if (AllFactionSlotsFilled())
                {
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
        if (IsTagCompanyMode)
        {
            SetActiveSlot(0);
            return;
        }

        SetActiveSlot(ResolveAutoSelectedSlotIndexCore(
            ShowRightSelectionBox,
            _factionSelectionState.LeftSlotFaction,
            _factionSelectionState.RightSlotFaction,
            _activeSlotIndex));
    }

    private void SetActiveSlot(int index)
    {
        _activeSlotIndex = IsTagCompanyMode
            ? 0
            : ResolveActiveSlotIndexCore(index, ShowRightSelectionBox);
        FactionSlotSelectorView.ApplyActiveSlotBorders(_activeSlotIndex);
    }
}

