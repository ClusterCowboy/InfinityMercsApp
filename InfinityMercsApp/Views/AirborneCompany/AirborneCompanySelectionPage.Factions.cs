using InfinityMercsApp.Views.Common;
using InfinityMercsApp.Views.Controls;
using FactionRecord = InfinityMercsApp.Domain.Models.Metadata.Faction;

namespace InfinityMercsApp.Views.AirborneCompany;

public partial class AirborneCompanySelectionPage
{
    private async Task LoadFactionsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var filteredFactions = await LoadFilteredFactionRecordsAsync(cancellationToken);
            var factionsWithLieutenant = await FilterFactionsWithLieutenantAsync(filteredFactions, cancellationToken);
            var items = BuildFactionSelectionItems(
                factionsWithLieutenant,
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
            Console.Error.WriteLine($"AirborneCompanySelectionPage LoadFactionsAsync failed: {ex.Message}");
        }
    }

    private async Task<List<FactionRecord>> FilterFactionsWithLieutenantAsync(
        List<FactionRecord> factions,
        CancellationToken cancellationToken = default)
    {
        var result = new List<FactionRecord>();
        foreach (var faction in factions)
        {
            var snapshot = _armyDataService.GetFactionSnapshot(faction.Id, cancellationToken);
            var skillsLookup = CompanySelectionSharedUtilities.BuildIdNameLookup(snapshot?.FiltersJson, "skills");
            var mercsEntries = await _armyDataService.GetMergedMercsArmyListAsync([faction.Id], cancellationToken);

            var hasLieutenant = mercsEntries.Any(entry =>
                CompanySelectionSharedUtilities.UnitHasLieutenantOption(entry.ProfileGroupsJson, skillsLookup));

            if (hasLieutenant)
            {
                result.Add(faction);
            }
        }

        return result;
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
                true,
                0,
                _factionSelectionState,
                item,
                (slotIndex, text) => FactionSlotSelectorView.LeftSlotText = text,
                (slotIndex, cachedPath, packagedPath) => _ = LoadSlotIconAsync(slotIndex, cachedPath, packagedPath),
                out var factionChanged))
        {
            Console.WriteLine($"[AirborneCompanySelectionPage] Duplicate selection blocked for faction {item.Id} ({item.Name}).");
            return;
        }

        SwitchToLeftSlot();

        HandleFactionAssignmentSideEffectsCore(
            factionChanged,
            () => { },
            ResetMercsCompany,
            () => LoadUnitsForActiveSlotAsync(),
            onAssignmentCompleted: () =>
            {
                IsFactionSelectionActive = false;
            });
    }

    private void ResetMercsCompany()
    {
        ResetMercsCompanyCore(
            MercsCompanyEntries,
            UpdateMercsCompanyTotal);
    }
}
