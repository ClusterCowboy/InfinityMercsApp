using System.Text.Json;
using InfinityMercsApp.Views.Common;
using InfinityMercsApp.Views.Controls;
using CCFactionFireteamValidityRecord = InfinityMercsApp.Domain.Models.Army.CCFactionFireteamValidityRecord;
using FactionRecord = InfinityMercsApp.Domain.Models.Metadata.Faction;

namespace InfinityMercsApp.Views.CohesiveCompany;

public partial class CohesiveCompanySelectionPage
{
    private async Task LoadFactionsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var maxCost = int.TryParse(SelectedStartSeasonPoints, out var parsedLimit) ? parsedLimit : 0;
            var ordered = await LoadFilteredFactionRecordsAsync(cancellationToken);
            var cacheFilterKey = BuildCCFactionValidityFilterKey(maxCost);
            var visibleFactions = new List<FactionRecord>();
            var factionIds = ordered.Select(x => x.Id).ToList();
            var cachedRows = await _specOpsProvider.GetCCFactionFireteamValidityAsync(
                cacheFilterKey,
                factionIds,
                cancellationToken);
            var cachedByFaction = cachedRows
                .GroupBy(x => x.FactionId)
                .ToDictionary(x => x.Key, x => x.OrderByDescending(r => r.EvaluatedAtUnixSeconds).First());

            var factionsToEvaluate = ordered
                .Where(faction =>
                    !cachedByFaction.TryGetValue(faction.Id, out var row) ||
                    string.IsNullOrWhiteSpace(row.ValidCoreFireteamsJson))
                .ToList();

            foreach (var faction in factionsToEvaluate)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var validCoreFireteamNames = await EvaluateValidCoreFireteamsForFactionAsync(faction, maxCost, cancellationToken);
                var hasValidCoreFireteams = validCoreFireteamNames.Count > 0;
                var validCoreFireteamsJson = JsonSerializer.Serialize(validCoreFireteamNames);
                await _specOpsProvider.UpsertCCFactionFireteamValidityAsync(
                    faction.Id,
                    cacheFilterKey,
                    hasValidCoreFireteams,
                    validCoreFireteamsJson,
                    cancellationToken);

                cachedByFaction[faction.Id] = new CCFactionFireteamValidityRecord
                {
                    FactionId = faction.Id,
                    FilterKey = cacheFilterKey,
                    HasValidCoreFireteams = hasValidCoreFireteams,
                    ValidCoreFireteamsJson = validCoreFireteamsJson,
                    EvaluatedAtUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                };
            }

            _validCoreFireteamsByFaction.Clear();
            foreach (var row in cachedByFaction.Values)
            {
                var validTeams = ParseValidCoreFireteams(row.ValidCoreFireteamsJson);
                _validCoreFireteamsByFaction[row.FactionId] = validTeams;
            }

            var validFactionIds = cachedByFaction.Values
                .Where(x => x.HasValidCoreFireteams)
                .Select(x => x.FactionId)
                .ToHashSet();

            foreach (var faction in ordered)
            {
                if (validFactionIds.Contains(faction.Id))
                {
                    visibleFactions.Add(faction);
                }
            }

            var items = BuildFactionSelectionItems(
                visibleFactions,
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
                TeamsView = false;
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

    private string BuildCCFactionValidityFilterKey(int maxCost)
    {
        var filterQuery = _filterState.ActiveUnitFilter.ToQuery();
        var termsKey = string.Join(";",
            filterQuery.Terms
                .OrderBy(term => term.Field)
                .Select(term => $"{term.Field}:{term.MatchMode}:{string.Join(",", term.Values.OrderBy(value => value, StringComparer.OrdinalIgnoreCase))}"));

        return string.Join("|",
            "cc-core-v2",
            $"pts:{maxCost}",
            $"lt:{(LieutenantOnlyUnits ? 1 : 0)}",
            $"terms:{termsKey}",
            $"min:{_filterState.ActiveUnitFilter.MinPoints?.ToString() ?? string.Empty}",
            $"max:{_filterState.ActiveUnitFilter.MaxPoints?.ToString() ?? string.Empty}",
            $"filterlt:{(_filterState.ActiveUnitFilter.LieutenantOnlyUnits ? 1 : 0)}");
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

