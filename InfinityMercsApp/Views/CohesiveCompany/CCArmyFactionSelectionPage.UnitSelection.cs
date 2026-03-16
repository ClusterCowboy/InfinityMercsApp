using InfinityMercsApp.Services;
using InfinityMercsApp.Views.Controls;
using InfinityMercsApp.Views.Templates.NewCompany;

namespace InfinityMercsApp.Views.CohesiveCompany;

public partial class CCArmyFactionSelectionPage
{
    private void OnFactionSelectionHeaderTapped(object? sender, TappedEventArgs e)
    {
        AreTeamEntriesReady = false;
        IsFactionSelectionActive = true;
    }

    private void OnUnitSelectionHeaderTapped(object? sender, TappedEventArgs e)
    {
        IsFactionSelectionActive = false;
        if (_factionSelectionState.SelectedFaction is not null || _factionSelectionState.LeftSlotFaction is not null || _factionSelectionState.RightSlotFaction is not null)
        {
            _ = LoadUnitsForActiveSlotAsync();
        }
    }

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

    private void OnUnitFilterPopupCloseRequested(object? sender, EventArgs e)
    {
        CloseUnitFilterPopup(sender as UnitFilterPopupView);
    }

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

    private double ResolveUnitFilterPopupHeight()
    {
        var pageHeight = Height > 0 ? Height : Window?.Height ?? Application.Current?.Windows.FirstOrDefault()?.Page?.Height ?? 0;
        if (pageHeight <= 0)
        {
            return 800;
        }

        return pageHeight * 0.9;
    }

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
            var snapshot = GetFactionSnapshotFromProvider(factionId, cancellationToken);
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

        var mergedMercsList = await GetMergedMercsArmyListFromQueryAccessorAsync(sourceFactionIds, cancellationToken);
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

    private static void MergeLookup(Dictionary<int, string> target, IReadOnlyDictionary<int, string> source)
    {
        CompanySelectionSharedUtilities.MergeLookup(target, source);
    }

    private int ResolveFilterPopupMaxPoints()
    {
        return int.TryParse(SelectedStartSeasonPoints, out var parsedMaxPoints)
            ? Math.Max(parsedMaxPoints, 200)
            : 200;
    }

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
