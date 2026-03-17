using System.Collections.ObjectModel;
using InfinityMercsApp.Domain.Utilities;
using InfinityMercsApp.Services;
using InfinityMercsApp.Views.Controls;
using InfinityMercsApp.Views.Common;

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
            var merged = await BuildMergedUnitsAndTeamsAsync(
                factions,
                faction => faction.Id,
                _armyDataService.GetResumeByFactionMercsOnly,
                _specOpsProvider.GetSpecopsUnitsByFactionAsync,
                _armyDataService.GetFactionSnapshot,
                async (factionId, units, ct) =>
                {
                    if (_factionLogoCacheService is not null)
                    {
                        await _factionLogoCacheService.CacheUnitLogosFromRecordsAsync(factionId, units, ct);
                    }
                },
                MergeFireteamEntries,
                IsCharacterCategory,
                BuildUnitSubtitle,
                (factionId, unit, typeLookup, categoryLookup) => new ArmyUnitSelectionItem
                {
                    Id = unit.UnitId,
                    SourceFactionId = factionId,
                    Slug = unit.Slug,
                    Name = unit.Name,
                    Type = unit.Type,
                    IsCharacter = IsCharacterCategory(unit, categoryLookup),
                    Subtitle = BuildUnitSubtitle(unit, typeLookup, categoryLookup),
                    IsSpecOps = false,
                    CachedLogoPath = _factionLogoCacheService?.TryGetCachedUnitLogoPath(factionId, unit.UnitId),
                    PackagedLogoPath = _factionLogoCacheService?.GetPackagedUnitLogoPath(factionId, unit.UnitId)
                        ?? $"SVGCache/units/{factionId}-{unit.UnitId}.svg"
                },
                (factionId, specopsUnit, resumeByUnitId, units, typeLookup, categoryLookup) =>
                {
                    var baseName = string.IsNullOrWhiteSpace(specopsUnit.Name)
                        ? units.FirstOrDefault(x => x.UnitId == specopsUnit.UnitId)?.Name ?? $"Unit {specopsUnit.UnitId}"
                        : specopsUnit.Name.Trim();
                    var key = $"{baseName} - Spec Ops";
                    return new ArmyUnitSelectionItem
                    {
                        Id = specopsUnit.UnitId,
                        SourceFactionId = factionId,
                        Slug = specopsUnit.Slug,
                        Name = key,
                        Type = resumeByUnitId.TryGetValue(specopsUnit.UnitId, out var resumeUnit) ? resumeUnit.Type : null,
                        IsCharacter = resumeByUnitId.TryGetValue(specopsUnit.UnitId, out var characterUnit) &&
                                      IsCharacterCategory(characterUnit, categoryLookup),
                        Subtitle = resumeByUnitId.TryGetValue(specopsUnit.UnitId, out var subtitleUnit)
                            ? BuildUnitSubtitle(subtitleUnit, typeLookup, categoryLookup)
                            : "Spec Ops",
                        IsSpecOps = true,
                        CachedLogoPath = _factionLogoCacheService?.TryGetCachedUnitLogoPath(factionId, specopsUnit.UnitId),
                        PackagedLogoPath = _factionLogoCacheService?.GetPackagedUnitLogoPath(factionId, specopsUnit.UnitId)
                            ?? $"SVGCache/units/{factionId}-{specopsUnit.UnitId}.svg"
                    };
                },
                cancellationToken);

            PopulateUnitsCollection(Units, merged.UnitsByKey.Values);

            foreach (var team in merged.TeamsByName.Values
                         .Where(x => x.Duo > 0)
                         .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase))
            {
                var nonCharacterUnitLimits = CompanyTeamProfilesWorkflow.FilterCharacterUnitLimits(
                    team.UnitLimits,
                    merged.UnitsByKey.Values,
                    x => x.IsCharacter);
                var nonCharacterNonWildcardUnitLimits = CompanyTeamProfilesWorkflow.FilterWildcardUnitLimits(nonCharacterUnitLimits);
                var allowedProfiles = CompanyTeamProfilesWorkflow.BuildAllowedTeamProfiles(
                    nonCharacterNonWildcardUnitLimits,
                    merged.UnitsByKey.Values,
                    (displayName, min, max, slug, sourceUnits) => CompanyTeamProfilesWorkflow.BuildTeamUnitLimitItem<ArmyUnitSelectionItem, ArmyTeamUnitLimitItem>(
                        displayName,
                        min,
                        max,
                        slug,
                        sourceUnits));
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

            var wildcardUnitLimits = BuildWildcardUnitLimits(
                merged.TeamsByName.Values,
                merged.UnitsByKey.Values);
            AppendWildcardTeamEntry<ArmyUnitSelectionItem, ArmyTeamUnitLimitItem, ArmyTeamListItem>(
                wildcardUnitLimits,
                merged.UnitsByKey.Values,
                TeamEntries,
                (name, min, max, slug, sourceUnits) => CompanyTeamProfilesWorkflow.BuildTeamUnitLimitItem<ArmyUnitSelectionItem, ArmyTeamUnitLimitItem>(
                    name, min, max, slug, sourceUnits),
                (name, teamCountsText, isWildcardBucket, isExpanded, allowedProfiles) => new ArmyTeamListItem
                {
                    Name = name,
                    TeamCountsText = teamCountsText,
                    IsWildcardBucket = isWildcardBucket,
                    IsExpanded = isExpanded,
                    AllowedProfiles = allowedProfiles
                });

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




