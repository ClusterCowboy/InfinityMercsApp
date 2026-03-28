using System.Collections.ObjectModel;
using System.Globalization;
using InfinityMercsApp.Domain.Utilities;
using InfinityMercsApp.Services;
using InfinityMercsApp.Views.Controls;
using InfinityMercsApp.Views.Common;
using InspiringGen = InfinityMercsApp.Infrastructure.Providers.InspiringCompanyFactionGenerator;

namespace InfinityMercsApp.Views.InspiringCompany;

public partial class InspiringCompanySelectionPage
{
    private void OnToggleFactionStripTapped(object? sender, TappedEventArgs e)
    {
        ToggleFactionStrip(sender);
    }

    private void OnUnitSelectionFilterButtonTapped(object? sender, TappedEventArgs e)
    {
        _filterState.ActiveUnitFilterPopup = CompanySelectionUnitFilterWorkflow.TryOpenUnitFilterPopup(
            GetPreparedPopupOptionsForCurrentPoints(),
            _filterState.ActiveUnitFilter,
            LieutenantOnlyUnits,
            teamsView: false,
            ResolveUnitFilterPopupHeight(),
            OnFilterArmyApplied,
            OnUnitFilterPopupCloseRequested,
            UnitFilterPopupHost,
            UnitFilterOverlay,
            message => Console.Error.WriteLine(message),
            teamsViewEnabled: false);
    }

    private void OnFilterArmyApplied(object? sender, UnitFilterCriteria criteria)
    {
        _filterState.ActiveUnitFilter = CompanySelectionUnitFilterWorkflow.ApplyCriteriaFromPopup(
            criteria,
            value => LieutenantOnlyUnits = value,
            _ => TeamsView = false);
        if (_filterState.ActiveUnitFilter.TeamsView)
        {
            _filterState.ActiveUnitFilter = new UnitFilterCriteria
            {
                Terms = _filterState.ActiveUnitFilter.Terms,
                MinPoints = _filterState.ActiveUnitFilter.MinPoints,
                MaxPoints = _filterState.ActiveUnitFilter.MaxPoints,
                LieutenantOnlyUnits = _filterState.ActiveUnitFilter.LieutenantOnlyUnits,
                TeamsView = false
            };
        }

        TeamsView = false;
        SetIsUnitFilterActive(_filterState.ActiveUnitFilter.IsActive);
        CloseUnitFilterPopup(sender as UnitFilterPopupView);
        _ = ApplyUnitVisibilityFiltersAsync();
    }

    private void OnUnitFilterPopupCloseRequested(object? sender, EventArgs e)
    {
        CloseUnitFilterPopup(sender as UnitFilterPopupView);
    }

    private void CloseUnitFilterPopup(UnitFilterPopupView? popup)
    {
        _filterState.ActiveUnitFilterPopup = CompanySelectionUnitFilterWorkflow.CloseUnitFilterPopup(
            popup,
            _filterState.ActiveUnitFilterPopup,
            OnFilterArmyApplied,
            OnUnitFilterPopupCloseRequested,
            UnitFilterPopupHost,
            UnitFilterOverlay);
    }

    private double ResolveUnitFilterPopupHeight()
    {
        return CompanySelectionUnitFilterWorkflow.ResolveUnitFilterPopupHeight(this);
    }

    private void OnUnitSelectionHeaderBorderSizeChanged(object? sender, EventArgs e)
    {
        CompanySelectionUnitSelectionUiWorkflow.ApplyHeaderFilterButtonSizes(
            sender,
            UnitSelectionPanel.FilterButton,
            UnitSelectionPanel.FilterCanvas,
            UnitSelectionPanel.FilterButton,
            UnitSelectionPanel.FilterCanvas,
            ApplyFilterButtonSize);
    }

    private async Task<UnitFilterPopupOptions> BuildUnitFilterPopupOptionsAsync(CancellationToken cancellationToken = default)
    {
        var activeSlotFaction = _activeSlotIndex == 0
            ? _factionSelectionState.LeftSlotFaction
            : _factionSelectionState.RightSlotFaction;
        return await CompanySelectionUnitFilterWorkflow.BuildUnitFilterPopupOptionsAsync(
            false,
            activeSlotFaction,
            null,
            faction => faction.Id,
            (factionId, ct) => _armyDataService.GetFactionSnapshot(factionId, ct)?.FiltersJson,
            (factionIds, ct) => _armyDataService.GetMergedMercsArmyListAsync(factionIds, ct),
            ResolveFilterPopupMaxPoints(),
            value => _filterState.PreparedUnitFilterPopupOptions = value,
            message => Console.WriteLine(message),
            cancellationToken);
    }

    private async Task LoadUnitsForActiveSlotAsync(CancellationToken cancellationToken = default)
    {
        _filterState.PreparedUnitFilterPopupOptions = null;
        Units.Clear();
        TeamEntries.Clear();
        _selectedUnit = null;
        ResetUnitDetails();

        var factions = CompanyUnitDetailsShared.BuildUnitSourceFactionsForActiveSlot(
            _activeSlotIndex,
            _factionSelectionState.LeftSlotFaction,
            _factionSelectionState.RightSlotFaction);
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
                    // Skip logo caching for Inspiring Company — logos are resolved via source metadata.
                    if (_factionLogoCacheService is not null && factionId != InspiringGen.InspiringCompanyFactionId)
                    {
                        await _factionLogoCacheService.CacheUnitLogosFromRecordsAsync(factionId, units, ct);
                    }
                },
                MergeFireteamEntries,
                IsCharacterCategory,
                BuildUnitSubtitle,
                (factionId, unit, typeLookup, categoryLookup) =>
                {
                    ResolveUnitLogoIds(factionId, unit.UnitId, unit.Logo, out var logoFactionId, out var logoUnitId);
                    return new ArmyUnitSelectionItem
                    {
                        Id = unit.UnitId,
                        SourceFactionId = factionId,
                        LogoSourceFactionId = logoFactionId,
                        LogoSourceUnitId = logoUnitId,
                        Slug = unit.Slug,
                        Name = unit.Name,
                        Type = unit.Type,
                        IsCharacter = IsCharacterCategory(unit, categoryLookup),
                        Subtitle = BuildUnitSubtitle(unit, typeLookup, categoryLookup),
                        IsSpecOps = false,
                        CachedLogoPath = _factionLogoCacheService?.TryGetCachedUnitLogoPath(logoFactionId, logoUnitId),
                        PackagedLogoPath = _factionLogoCacheService?.GetPackagedUnitLogoPath(logoFactionId, logoUnitId)
                            ?? $"SVGCache/units/{logoFactionId}-{logoUnitId}.svg"
                    };
                },
                (factionId, specopsUnit, resumeByUnitId, units, typeLookup, categoryLookup) =>
                {
                    var baseName = string.IsNullOrWhiteSpace(specopsUnit.Name)
                        ? units.FirstOrDefault(x => x.UnitId == specopsUnit.UnitId)?.Name ?? $"Unit {specopsUnit.UnitId}"
                        : specopsUnit.Name.Trim();
                    var key = $"{baseName} - Spec Ops";
                    var logoField = resumeByUnitId.TryGetValue(specopsUnit.UnitId, out var logoResume) ? logoResume.Logo : null;
                    ResolveUnitLogoIds(factionId, specopsUnit.UnitId, logoField, out var logoFactionId, out var logoUnitId);
                    return new ArmyUnitSelectionItem
                    {
                        Id = specopsUnit.UnitId,
                        SourceFactionId = factionId,
                        LogoSourceFactionId = logoFactionId,
                        LogoSourceUnitId = logoUnitId,
                        Slug = specopsUnit.Slug,
                        Name = key,
                        Type = resumeByUnitId.TryGetValue(specopsUnit.UnitId, out var resumeUnit) ? resumeUnit.Type : null,
                        IsCharacter = resumeByUnitId.TryGetValue(specopsUnit.UnitId, out var characterUnit) &&
                                      IsCharacterCategory(characterUnit, categoryLookup),
                        Subtitle = resumeByUnitId.TryGetValue(specopsUnit.UnitId, out var subtitleUnit)
                            ? BuildUnitSubtitle(subtitleUnit, typeLookup, categoryLookup)
                            : "Spec Ops",
                        IsSpecOps = true,
                        CachedLogoPath = _factionLogoCacheService?.TryGetCachedUnitLogoPath(logoFactionId, logoUnitId),
                        PackagedLogoPath = _factionLogoCacheService?.GetPackagedUnitLogoPath(logoFactionId, logoUnitId)
                            ?? $"SVGCache/units/{logoFactionId}-{logoUnitId}.svg"
                    };
                },
                cancellationToken);

            PopulateUnitsCollection(Units, merged.UnitsByKey.Values);
            Console.WriteLine($"[InspiringCompanySelectionPage] Loaded {Units.Count} unit(s) for active slot {_activeSlotIndex}.");
            if (Units.Count == 0 &&
                _activeSlotIndex == 1 &&
                _factionSelectionState.LeftSlotFaction is not null)
            {
                // Synthetic slot can be empty if generated faction data is unavailable.
                // Fall back to the selected left slot so the unit list never appears blank.
                SwitchToLeftSlot();
                await LoadUnitsForActiveSlotAsync(cancellationToken);
                return;
            }

            BuildTeamEntriesFromMerged<ArmyUnitSelectionItem, ArmyTeamUnitLimitItem, ArmyTeamListItem>(
                merged,
                TeamEntries,
                includeTeam: _ => true,
                readTeamCount: team => team.Duo,
                buildTeamCountText: team => $"D: {team.Duo}",
                buildTeamUnitLimitItem: (name, min, max, slug, sourceUnits) =>
                    CompanyTeamProfilesWorkflow.BuildTeamUnitLimitItem<ArmyUnitSelectionItem, ArmyTeamUnitLimitItem>(
                        name, min, max, slug, sourceUnits),
                createTeam: (name, teamCountsText, isWildcardBucket, isExpanded, allowedProfiles) => new ArmyTeamListItem
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
            Console.Error.WriteLine($"InspiringCompanySelectionPage LoadUnitsForActiveSlotAsync failed: {ex.Message}");
        }
    }

    private int ResolveFilterPopupMaxPoints()
    {
        return CompanySelectionUnitFilterWorkflow.ResolveFilterPopupMaxPoints(SelectedStartSeasonPoints);
    }

    private UnitFilterPopupOptions GetPreparedPopupOptionsForCurrentPoints()
    {
        return CompanySelectionUnitFilterWorkflow.GetPreparedPopupOptionsForCurrentPoints(
            _filterState.PreparedUnitFilterPopupOptions,
            ResolveFilterPopupMaxPoints());
    }

    /// <summary>
    /// For Inspiring Company synthetic units, the Resume's Logo field stores
    /// the original source IDs as "{sourceFactionId}-{sourceUnitId}".
    /// Parse these to resolve the correct cached logo path.
    /// </summary>
    private static void ResolveUnitLogoIds(int factionId, int unitId, string? logoField, out int logoFactionId, out int logoUnitId)
    {
        logoFactionId = factionId;
        logoUnitId = unitId;

        if (factionId != InspiringGen.InspiringCompanyFactionId || string.IsNullOrWhiteSpace(logoField))
        {
            return;
        }

        var dashIndex = logoField.IndexOf('-');
        if (dashIndex <= 0 || dashIndex >= logoField.Length - 1)
        {
            return;
        }

        if (int.TryParse(logoField.AsSpan(0, dashIndex), NumberStyles.Integer, CultureInfo.InvariantCulture, out var srcFaction)
            && int.TryParse(logoField.AsSpan(dashIndex + 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out var srcUnit))
        {
            logoFactionId = srcFaction;
            logoUnitId = srcUnit;
        }
    }

}
