using System.Collections.ObjectModel;
using InfinityMercsApp.Services;
using InfinityMercsApp.Views.Common;

namespace InfinityMercsApp.Views.CohesiveCompany;

public partial class CohesiveCompanySelectionPage
{
    private async Task LoadUnitsForActiveSlotAsync(CancellationToken cancellationToken = default)
    {
        var trackedFireteamNameToRestore = _trackedFireteamName;
        _filterState.PreparedUnitFilterPopupOptions = null;
        AreTeamEntriesReady = false;
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
                GetResumeByFactionMercsOnlyFromProvider,
                _specOpsProvider.GetSpecopsUnitsByFactionAsync,
                GetFactionSnapshotFromProvider,
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

            var validCoreTeamNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var faction in factions)
            {
                if (_validCoreFireteamsByFaction.TryGetValue(faction.Id, out var cachedValidTeamNames))
                {
                    foreach (var teamName in cachedValidTeamNames)
                    {
                        validCoreTeamNames.Add(teamName);
                    }
                }
            }

            BuildTeamEntriesFromMerged<ArmyUnitSelectionItem, ArmyTeamUnitLimitItem, ArmyTeamListItem>(
                merged,
                TeamEntries,
                includeTeam: team => validCoreTeamNames.Count == 0 || validCoreTeamNames.Contains(team.Name),
                readTeamCount: team => team.Core,
                buildTeamCountText: team => $"C: {team.Core}",
                buildTeamUnitLimitItem: (name, min, max, slug, sourceUnits) =>
                    CompanyTeamProfilesWorkflow.BuildTeamUnitLimitItem<ArmyUnitSelectionItem, ArmyTeamUnitLimitItem>(
                        name, NormalizeCohesiveDisplayedMinimum(min), max, slug, sourceUnits),
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
            RestoreTrackedFireteamSelection(trackedFireteamNameToRestore);
            TryAutoSelectFirstVisibleUnitAfterFactionLoad();
            AreTeamEntriesReady = true;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"CompanySelectionPage LoadUnitsForActiveSlotAsync failed: {ex.Message}");
            RestoreTrackedFireteamSelection(string.Empty);
            AreTeamEntriesReady = false;
        }
    }

    private void TryAutoSelectFirstVisibleUnitAfterFactionLoad()
    {
        if (!_autoSelectUnitAfterFactionLoad || _selectedUnit is not null)
        {
            return;
        }

        var firstVisibleUnit = Units.FirstOrDefault(x => x.IsVisible);
        if (firstVisibleUnit is null)
        {
            _autoSelectUnitAfterFactionLoad = false;
            return;
        }

        SetSelectedUnit(firstVisibleUnit);
        IsFactionSelectionActive = false;
        _autoSelectUnitAfterFactionLoad = false;
    }

    private static string NormalizeCohesiveDisplayedMinimum(string? min)
    {
        if (string.Equals(min?.Trim(), "*", StringComparison.Ordinal))
        {
            return "0";
        }

        if (int.TryParse(min, out var parsedMin) && parsedMin > 0)
        {
            return "0";
        }

        return min ?? "0";
    }
}
