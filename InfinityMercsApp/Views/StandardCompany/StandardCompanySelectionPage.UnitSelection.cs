using System.Collections.ObjectModel;
using InfinityMercsApp.Domain.Utilities;
using InfinityMercsApp.Services;
using InfinityMercsApp.Views.Controls;
using InfinityMercsApp.Views.Templates.NewCompany;

namespace InfinityMercsApp.Views.StandardCompany;

/// <summary>
/// UI-level unit/faction selection flow: tab switching, filter popup, and source-unit list assembly.
/// </summary>
public partial class StandardCompanySelectionPage
{
    /// <summary>
    /// Handles on faction selection header tapped.
    /// </summary>
    private void OnFactionSelectionHeaderTapped(object? sender, TappedEventArgs e)
    {
        CompanySelectionUnitSelectionUiWorkflow.ActivateFactionSelection(value => IsFactionSelectionActive = value);
    }

    /// <summary>
    /// Handles on unit selection header tapped.
    /// </summary>
    private void OnUnitSelectionHeaderTapped(object? sender, TappedEventArgs e)
    {
        CompanySelectionUnitSelectionUiWorkflow.ActivateUnitSelection(value => IsFactionSelectionActive = value);
    }

    /// <summary>
    /// Handles on unit selection filter button tapped.
    /// </summary>
    private void OnUnitSelectionFilterButtonTapped(object? sender, TappedEventArgs e)
    {
        _activeUnitFilterPopup = CompanySelectionUnitFilterWorkflow.TryOpenUnitFilterPopup(
            GetPreparedPopupOptionsForCurrentPoints(),
            _activeUnitFilter,
            LieutenantOnlyUnits,
            TeamsView,
            ResolveUnitFilterPopupHeight(),
            OnFilterArmyApplied,
            OnUnitFilterPopupCloseRequested,
            UnitFilterPopupHost,
            UnitFilterOverlay,
            message => Console.Error.WriteLine(message));
    }

    /// <summary>
    /// Handles on filter army applied.
    /// </summary>
    private void OnFilterArmyApplied(object? sender, UnitFilterCriteria criteria)
    {
        _activeUnitFilter = CompanySelectionUnitFilterWorkflow.ApplyCriteriaFromPopup(
            criteria,
            value => LieutenantOnlyUnits = value,
            value => TeamsView = value);
        CloseUnitFilterPopup(sender as UnitFilterPopupView);
        _ = ApplyUnitVisibilityFiltersAsync();
    }

    /// <summary>
    /// Handles on unit filter popup close requested.
    /// </summary>
    private void OnUnitFilterPopupCloseRequested(object? sender, EventArgs e)
    {
        CloseUnitFilterPopup(sender as UnitFilterPopupView);
    }

    /// <summary>
    /// Handles close unit filter popup.
    /// </summary>
    private void CloseUnitFilterPopup(UnitFilterPopupView? popup)
    {
        _activeUnitFilterPopup = CompanySelectionUnitFilterWorkflow.CloseUnitFilterPopup(
            popup,
            _activeUnitFilterPopup,
            OnFilterArmyApplied,
            OnUnitFilterPopupCloseRequested,
            UnitFilterPopupHost,
            UnitFilterOverlay);
    }

    /// <summary>
    /// Handles resolve unit filter popup height.
    /// </summary>
    private double ResolveUnitFilterPopupHeight()
    {
        return CompanySelectionUnitFilterWorkflow.ResolveUnitFilterPopupHeight(this);
    }

    /// <summary>
    /// Handles on unit selection header border size changed.
    /// </summary>
    private void OnUnitSelectionHeaderBorderSizeChanged(object? sender, EventArgs e)
    {
        CompanySelectionUnitSelectionUiWorkflow.ApplyHeaderFilterButtonSizes(
            sender,
            UnitSelectionFilterButtonInactive,
            UnitSelectionFilterCanvasInactive,
            UnitSelectionFilterButtonActive,
            UnitSelectionFilterCanvasActive,
            ApplyFilterButtonSize);
    }

    /// <summary>
    /// Handles build unit filter popup options async.
    /// </summary>
    private async Task<UnitFilterPopupOptions> BuildUnitFilterPopupOptionsAsync(CancellationToken cancellationToken = default)
    {
        return await CompanySelectionUnitFilterWorkflow.BuildUnitFilterPopupOptionsAsync(
            ShowRightSelectionBox,
            _factionSelectionState.LeftSlotFaction,
            _factionSelectionState.RightSlotFaction,
            faction => faction.Id,
            (factionId, ct) => _armyDataService.GetFactionSnapshot(factionId, ct)?.FiltersJson,
            (factionIds, ct) => _armyDataService.GetMergedMercsArmyListAsync(factionIds, ct),
            ResolveFilterPopupMaxPoints(),
            value => _preparedUnitFilterPopupOptions = value,
            message => Console.WriteLine(message),
            cancellationToken);
    }

    /// <summary>
    /// Handles load units for active slot async.
    /// </summary>
    private async Task LoadUnitsForActiveSlotAsync(CancellationToken cancellationToken = default)
    {
        _preparedUnitFilterPopupOptions = null;
        Units.Clear();
        TeamEntries.Clear();
        _selectedUnit = null;
        ResetUnitDetails();

        var factions = CompanyUnitDetailsShared.BuildUnitSourceFactions(
            ShowRightSelectionBox,
            _factionSelectionState.LeftSlotFaction,
            _factionSelectionState.RightSlotFaction,
            faction => faction.Id);
        if (factions.Count == 0)
        {
            return;
        }

        try
        {
            var mergedUnits = new Dictionary<string, ArmyUnitSelectionItem>(StringComparer.OrdinalIgnoreCase);
            var mergedTeams = new Dictionary<string, TeamAggregate>(StringComparer.OrdinalIgnoreCase);
            var wildcardUnitLimits = new Dictionary<string, (int Min, int Max, string? Slug, bool MinAsterisk)>(StringComparer.OrdinalIgnoreCase);

            foreach (var faction in factions)
            {
                var units = _armyDataService.GetResumeByFactionMercsOnly(faction.Id, cancellationToken);
                var resumeByUnitId = units
                    .GroupBy(x => x.UnitId)
                    .ToDictionary(x => x.Key, x => x.First());
                var specopsUnits = await _specOpsProvider.GetSpecopsUnitsByFactionAsync(faction.Id, cancellationToken);
                var specopsByUnitId = specopsUnits
                    .GroupBy(x => x.UnitId)
                    .ToDictionary(x => x.Key, x => x.First());
                var snapshot = _armyDataService.GetFactionSnapshot(faction.Id, cancellationToken);
                var typeLookup = CompanyUnitDetailsShared.BuildIdNameLookup(snapshot?.FiltersJson, "type");
                var categoryLookup = CompanyUnitDetailsShared.BuildIdNameLookup(snapshot?.FiltersJson, "category");
                MergeFireteamEntries(snapshot?.FireteamChartJson, mergedTeams);

                if (_factionLogoCacheService is not null)
                {
                    await _factionLogoCacheService.CacheUnitLogosFromRecordsAsync(faction.Id, units, cancellationToken);
                }

                foreach (var unit in units)
                {
                    var key = unit.Name.Trim();
                    if (string.IsNullOrWhiteSpace(key) || mergedUnits.ContainsKey(key))
                    {
                        continue;
                    }

                    mergedUnits[key] = new ArmyUnitSelectionItem
                    {
                        Id = unit.UnitId,
                        SourceFactionId = faction.Id,
                        Slug = unit.Slug,
                        Name = unit.Name,
                        Type = unit.Type,
                        IsCharacter = IsCharacterCategory(unit, categoryLookup),
                        Subtitle = BuildUnitSubtitle(unit, typeLookup, categoryLookup),
                        IsSpecOps = false,
                        CachedLogoPath = _factionLogoCacheService?.TryGetCachedUnitLogoPath(faction.Id, unit.UnitId),
                        PackagedLogoPath = _factionLogoCacheService?.GetPackagedUnitLogoPath(faction.Id, unit.UnitId)
                            ?? $"SVGCache/units/{faction.Id}-{unit.UnitId}.svg"
                    };
                }

                foreach (var specopsUnit in specopsUnits.OrderBy(x => x.EntryOrder))
                {
                    var baseName = string.IsNullOrWhiteSpace(specopsUnit.Name)
                        ? units.FirstOrDefault(x => x.UnitId == specopsUnit.UnitId)?.Name ?? $"Unit {specopsUnit.UnitId}"
                        : specopsUnit.Name.Trim();
                    var key = $"{baseName} - Spec Ops";
                    if (string.IsNullOrWhiteSpace(key) || mergedUnits.ContainsKey(key))
                    {
                        continue;
                    }

                    mergedUnits[key] = new ArmyUnitSelectionItem
                    {
                        Id = specopsUnit.UnitId,
                        SourceFactionId = faction.Id,
                        Slug = specopsUnit.Slug,
                        Name = key,
                        Type = resumeByUnitId.TryGetValue(specopsUnit.UnitId, out var resumeUnit) ? resumeUnit.Type : null,
                        IsCharacter = resumeByUnitId.TryGetValue(specopsUnit.UnitId, out var characterUnit) &&
                                      IsCharacterCategory(characterUnit, categoryLookup),
                        Subtitle = resumeByUnitId.TryGetValue(specopsUnit.UnitId, out var subtitleUnit)
                            ? BuildUnitSubtitle(subtitleUnit, typeLookup, categoryLookup)
                            : "Spec Ops",
                        IsSpecOps = true,
                        CachedLogoPath = _factionLogoCacheService?.TryGetCachedUnitLogoPath(faction.Id, specopsUnit.UnitId),
                        PackagedLogoPath = _factionLogoCacheService?.GetPackagedUnitLogoPath(faction.Id, specopsUnit.UnitId)
                            ?? $"SVGCache/units/{faction.Id}-{specopsUnit.UnitId}.svg"
                    };
                }
            }

            foreach (var unit in ArmyUnitSort.OrderByUnitTypeAndName(mergedUnits.Values, x => x.Type, x => x.Name))
            {
                Units.Add(unit);
            }

            foreach (var team in mergedTeams.Values
                         .Where(x => x.Duo > 0)
                         .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase))
            {
                var nonCharacterUnitLimits = StandardCompanyTeamService.FilterCharacterUnitLimits(team.UnitLimits, mergedUnits.Values);
                var nonCharacterNonWildcardUnitLimits = StandardCompanyTeamService.FilterWildcardUnitLimits(nonCharacterUnitLimits);
                var allowedProfiles = StandardCompanyTeamService.BuildAllowedTeamProfiles(nonCharacterNonWildcardUnitLimits, mergedUnits.Values);
                if (allowedProfiles.Count == 0)
                {
                    continue;
                }

                TeamEntries.Add(new ArmyTeamListItem
                {
                    Name = team.Name,
                    TeamCountsText = $"D: {team.Duo}",
                    IsExpanded = true,
                    AllowedProfiles = new ObservableCollection<ArmyTeamUnitLimitItem>(allowedProfiles)
                });
            }

            foreach (var team in mergedTeams.Values)
            {
                var isWildcardTeam = StandardCompanyTeamService.IsWildcardTeamName(team.Name);
                var nonCharacterUnitLimits = StandardCompanyTeamService.FilterCharacterUnitLimits(team.UnitLimits, mergedUnits.Values);
                foreach (var entry in nonCharacterUnitLimits)
                {
                    var unitName = entry.Key;
                    var value = entry.Value;
                    if (!isWildcardTeam && !StandardCompanyTeamService.IsWildcardEntry(unitName, value.Slug))
                    {
                        continue;
                    }

                    if (wildcardUnitLimits.TryGetValue(unitName, out var existing))
                    {
                        wildcardUnitLimits[unitName] = (
                            Math.Min(existing.Min, value.Min),
                            Math.Max(existing.Max, value.Max),
                            string.IsNullOrWhiteSpace(existing.Slug) ? value.Slug : existing.Slug,
                            existing.MinAsterisk || value.MinAsterisk);
                    }
                    else
                    {
                        wildcardUnitLimits[unitName] = value;
                    }
                }
            }

            if (wildcardUnitLimits.Count > 0)
            {
                var wildcardAllowedProfiles = wildcardUnitLimits
                    .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
                    .Select(x => StandardCompanyTeamService.BuildTeamUnitLimitItem(
                        x.Key,
                        x.Value.MinAsterisk ? "*" : x.Value.Min.ToString(),
                        x.Value.Max.ToString(),
                        x.Value.Slug,
                        mergedUnits.Values))
                    .Where(x => !x.IsCharacter)
                    .ToList();

                if (wildcardAllowedProfiles.Count > 0)
                {
                    TeamEntries.Add(new ArmyTeamListItem
                    {
                        Name = "Wildcards",
                        TeamCountsText = string.Empty,
                        IsWildcardBucket = true,
                        IsExpanded = true,
                        AllowedProfiles = new ObservableCollection<ArmyTeamUnitLimitItem>(wildcardAllowedProfiles)
                    });
                }
            }

            await ApplyUnitVisibilityFiltersAsync(cancellationToken);
            await BuildUnitFilterPopupOptionsAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ArmyFactionSelectionPage LoadUnitsForActiveSlotAsync failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles resolve filter popup max points.
    /// </summary>
    private int ResolveFilterPopupMaxPoints()
    {
        return CompanySelectionUnitFilterWorkflow.ResolveFilterPopupMaxPoints(SelectedStartSeasonPoints);
    }

    /// <summary>
    /// Handles clone popup options for current points.
    /// </summary>
    private UnitFilterPopupOptions ClonePopupOptionsForCurrentPoints(UnitFilterPopupOptions source)
    {
        return CompanySelectionUnitFilterWorkflow.ClonePopupOptionsForCurrentPoints(source, ResolveFilterPopupMaxPoints());
    }

    /// <summary>
    /// Handles get prepared popup options for current points.
    /// </summary>
    private UnitFilterPopupOptions GetPreparedPopupOptionsForCurrentPoints()
    {
        return CompanySelectionUnitFilterWorkflow.GetPreparedPopupOptionsForCurrentPoints(
            _preparedUnitFilterPopupOptions,
            ResolveFilterPopupMaxPoints());
    }

}


