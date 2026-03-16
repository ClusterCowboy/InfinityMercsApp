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
        IsFactionSelectionActive = true;
    }

    /// <summary>
    /// Handles on unit selection header tapped.
    /// </summary>
    private void OnUnitSelectionHeaderTapped(object? sender, TappedEventArgs e)
    {
        IsFactionSelectionActive = false;
    }

    /// <summary>
    /// Handles on unit selection filter button tapped.
    /// </summary>
    private void OnUnitSelectionFilterButtonTapped(object? sender, TappedEventArgs e)
    {
        try
        {
            var options = GetPreparedPopupOptionsForCurrentPoints();
            var popup = new UnitFilterPopupView(
                options,
                _activeUnitFilter,
                lieutenantOnlyUnits: LieutenantOnlyUnits,
                teamsView: TeamsView);
            var popupHeight = ResolveUnitFilterPopupHeight();
            popup.HeightRequest = popupHeight;
            popup.FilterArmyApplied += OnFilterArmyApplied;
            popup.CloseRequested += OnUnitFilterPopupCloseRequested;
            _activeUnitFilterPopup = popup;
            UnitFilterPopupHost.HeightRequest = popupHeight;
            UnitFilterPopupHost.Content = popup;
            UnitFilterOverlay.IsVisible = true;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ArmyFactionSelectionPage filter popup open failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles on filter army applied.
    /// </summary>
    private void OnFilterArmyApplied(object? sender, UnitFilterCriteria criteria)
    {
        _activeUnitFilter = criteria ?? UnitFilterCriteria.None;
        if (criteria is not null)
        {
            LieutenantOnlyUnits = criteria.LieutenantOnlyUnits;
            TeamsView = criteria.TeamsView;
        }
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
        var target = popup ?? _activeUnitFilterPopup;
        if (target is not null)
        {
            target.FilterArmyApplied -= OnFilterArmyApplied;
            target.CloseRequested -= OnUnitFilterPopupCloseRequested;
        }

        _activeUnitFilterPopup = null;
        UnitFilterPopupHost.Content = null;
        UnitFilterPopupHost.HeightRequest = -1;
        UnitFilterOverlay.IsVisible = false;
    }

    /// <summary>
    /// Handles resolve unit filter popup height.
    /// </summary>
    private double ResolveUnitFilterPopupHeight()
    {
        var pageHeight = Height > 0 ? Height : Window?.Height ?? Application.Current?.Windows.FirstOrDefault()?.Page?.Height ?? 0;
        if (pageHeight <= 0)
        {
            return 800;
        }

        return pageHeight * 0.9;
    }

    /// <summary>
    /// Handles on unit selection header border size changed.
    /// </summary>
    private void OnUnitSelectionHeaderBorderSizeChanged(object? sender, EventArgs e)
    {
        if (sender is not Border border || border.Height <= 0)
        {
            return;
        }

        var iconButtonSize = border.Height * 0.8;
        ApplyFilterButtonSize(UnitSelectionFilterButtonInactive, UnitSelectionFilterCanvasInactive, iconButtonSize);
        ApplyFilterButtonSize(UnitSelectionFilterButtonActive, UnitSelectionFilterCanvasActive, iconButtonSize);
    }

    /// <summary>
    /// Handles build unit filter popup options async.
    /// </summary>
    private async Task<UnitFilterPopupOptions> BuildUnitFilterPopupOptionsAsync(CancellationToken cancellationToken = default)
    {
        var classification = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var characteristics = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var skills = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var equipment = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var weapons = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var ammo = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var sourceFactions = GetUnitSourceFactions();
        var sourceFactionIds = sourceFactions
            .Select(x => x.Id)
            .Distinct()
            .ToArray();
        var typeLookup = new Dictionary<int, string>();
        var charsLookup = new Dictionary<int, string>();
        var skillsLookup = new Dictionary<int, string>();
        var equipLookup = new Dictionary<int, string>();
        var weaponsLookup = new Dictionary<int, string>();
        var ammoLookup = new Dictionary<int, string>();

        foreach (var factionId in sourceFactionIds)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var snapshot = _armyDataService.GetFactionSnapshot(factionId, cancellationToken);
            var filtersJson = snapshot?.FiltersJson;
            if (string.IsNullOrWhiteSpace(filtersJson))
            {
                continue;
            }

            MergeLookup(typeLookup, BuildIdNameLookup(filtersJson, "type"));
            MergeLookup(charsLookup, BuildIdNameLookup(filtersJson, "chars"));
            MergeLookup(skillsLookup, BuildIdNameLookup(filtersJson, "skills"));
            MergeLookup(equipLookup, BuildIdNameLookup(filtersJson, "equip"));
            MergeLookup(weaponsLookup, BuildIdNameLookup(filtersJson, "weapons"));
            MergeLookup(ammoLookup, BuildIdNameLookup(filtersJson, "ammunition"));
        }

        var mergedMercsList = await _armyDataService.GetMergedMercsArmyListAsync(sourceFactionIds, cancellationToken);
        foreach (var entry in mergedMercsList)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (entry.Resume.Type is int typeId &&
                typeLookup.TryGetValue(typeId, out var typeName) &&
                !string.IsNullOrWhiteSpace(typeName))
            {
                classification.Add(typeName.Trim());
            }

            if (string.IsNullOrWhiteSpace(entry.ProfileGroupsJson))
            {
                continue;
            }

            CompanyUnitFilterService.AddFilterOptionsFromVisibleProfilesAndOptions(
                entry.ProfileGroupsJson,
                charsLookup,
                skillsLookup,
                equipLookup,
                weaponsLookup,
                ammoLookup,
                requireLieutenant: false,
                requireZeroSwc: true,
                maxCost: null,
                includeProfileValues: false,
                characteristics,
                skills,
                equipment,
                weapons,
                ammo);
        }

        var options = new UnitFilterPopupOptions
        {
            Classification = classification.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList(),
            Characteristics = characteristics.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList(),
            Skills = skills.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList(),
            Equipment = equipment.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList(),
            Weapons = weapons.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList(),
            Ammo = ammo.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList(),
            MinPoints = 0,
            MaxPoints = ResolveFilterPopupMaxPoints()
        };
        _preparedUnitFilterPopupOptions = options;
        Console.WriteLine($"ArmyFactionSelectionPage filter options: class={options.Classification.Count}, chars={options.Characteristics.Count}, skills={options.Skills.Count}, equip={options.Equipment.Count}, weapons={options.Weapons.Count}, ammo={options.Ammo.Count}.");
        return ClonePopupOptionsForCurrentPoints(options);
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

        var factions = GetUnitSourceFactions();
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
                var typeLookup = BuildIdNameLookup(snapshot?.FiltersJson, "type");
                var categoryLookup = BuildIdNameLookup(snapshot?.FiltersJson, "category");
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
    /// Handles merge lookup.
    /// </summary>
    private static void MergeLookup(Dictionary<int, string> target, IReadOnlyDictionary<int, string> source)
    {
        CompanySelectionSharedUtilities.MergeLookup(target, source);
    }

    /// <summary>
    /// Handles resolve filter popup max points.
    /// </summary>
    private int ResolveFilterPopupMaxPoints()
    {
        return int.TryParse(SelectedStartSeasonPoints, out var parsedMaxPoints)
            ? Math.Max(parsedMaxPoints, 200)
            : 200;
    }

    /// <summary>
    /// Handles clone popup options for current points.
    /// </summary>
    private UnitFilterPopupOptions ClonePopupOptionsForCurrentPoints(UnitFilterPopupOptions source)
    {
        return new UnitFilterPopupOptions
        {
            Classification = [.. source.Classification],
            Characteristics = [.. source.Characteristics],
            Skills = [.. source.Skills],
            Equipment = [.. source.Equipment],
            Weapons = [.. source.Weapons],
            Ammo = [.. source.Ammo],
            MinPoints = source.MinPoints,
            MaxPoints = ResolveFilterPopupMaxPoints()
        };
    }

    /// <summary>
    /// Handles get prepared popup options for current points.
    /// </summary>
    private UnitFilterPopupOptions GetPreparedPopupOptionsForCurrentPoints()
    {
        if (_preparedUnitFilterPopupOptions is null)
        {
            return new UnitFilterPopupOptions
            {
                MinPoints = 0,
                MaxPoints = ResolveFilterPopupMaxPoints()
            };
        }

        return ClonePopupOptionsForCurrentPoints(_preparedUnitFilterPopupOptions);
    }

}


