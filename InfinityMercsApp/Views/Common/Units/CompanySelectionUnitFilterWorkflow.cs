using InfinityMercsApp.Services;
using InfinityMercsApp.Views.Controls;
using MercsArmyListEntry = InfinityMercsApp.Domain.Models.Army.MercsArmyListEntry;

namespace InfinityMercsApp.Views.Common;

internal static class CompanySelectionUnitFilterWorkflow
{
    internal static UnitFilterPopupView? TryOpenUnitFilterPopup(
        UnitFilterPopupOptions options,
        UnitFilterCriteria activeUnitFilter,
        bool lieutenantOnlyUnits,
        bool teamsView,
        double popupHeight,
        EventHandler<UnitFilterCriteria> onFilterArmyApplied,
        EventHandler onCloseRequested,
        ContentView popupHost,
        VisualElement popupOverlay,
        Action<string>? logError = null)
    {
        try
        {
            var popup = new UnitFilterPopupView(
                options,
                activeUnitFilter,
                lieutenantOnlyUnits: lieutenantOnlyUnits,
                teamsView: teamsView);
            popup.HeightRequest = popupHeight;
            popup.FilterArmyApplied += onFilterArmyApplied;
            popup.CloseRequested += onCloseRequested;
            popupHost.HeightRequest = popupHeight;
            popupHost.Content = popup;
            popupOverlay.IsVisible = true;
            return popup;
        }
        catch (Exception ex)
        {
            logError?.Invoke($"ArmyFactionSelectionPage filter popup open failed: {ex.Message}");
            return null;
        }
    }

    internal static UnitFilterPopupView? CloseUnitFilterPopup(
        UnitFilterPopupView? requestedPopup,
        UnitFilterPopupView? activePopup,
        EventHandler<UnitFilterCriteria> onFilterArmyApplied,
        EventHandler onCloseRequested,
        ContentView popupHost,
        VisualElement popupOverlay)
    {
        var target = requestedPopup ?? activePopup;
        if (target is not null)
        {
            target.FilterArmyApplied -= onFilterArmyApplied;
            target.CloseRequested -= onCloseRequested;
        }

        popupHost.Content = null;
        popupHost.HeightRequest = -1;
        popupOverlay.IsVisible = false;
        return null;
    }

    internal static UnitFilterCriteria ApplyCriteriaFromPopup(
        UnitFilterCriteria? criteria,
        Action<bool> setLieutenantOnlyUnits,
        Action<bool> setTeamsView)
    {
        var resolved = criteria ?? UnitFilterCriteria.None;
        if (criteria is not null)
        {
            setLieutenantOnlyUnits(criteria.LieutenantOnlyUnits);
            setTeamsView(criteria.TeamsView);
        }

        return resolved;
    }

    internal static double ResolveUnitFilterPopupHeight(Page page)
    {
        var pageHeight = page.Height > 0
            ? page.Height
            : page.Window?.Height ?? Application.Current?.Windows.FirstOrDefault()?.Page?.Height ?? 0;
        if (pageHeight <= 0)
        {
            return 800;
        }

        return pageHeight * 0.9;
    }

    internal static int ResolveFilterPopupMaxPoints(string selectedStartSeasonPoints)
    {
        return int.TryParse(selectedStartSeasonPoints, out var parsedMaxPoints)
            ? Math.Max(parsedMaxPoints, 200)
            : 200;
    }

    internal static UnitFilterPopupOptions ClonePopupOptionsForCurrentPoints(UnitFilterPopupOptions source, int maxPoints)
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
            MaxPoints = maxPoints
        };
    }

    internal static UnitFilterPopupOptions GetPreparedPopupOptionsForCurrentPoints(
        UnitFilterPopupOptions? preparedOptions,
        int maxPoints)
    {
        if (preparedOptions is null)
        {
            return new UnitFilterPopupOptions
            {
                MinPoints = 0,
                MaxPoints = maxPoints
            };
        }

        return ClonePopupOptionsForCurrentPoints(preparedOptions, maxPoints);
    }

    internal static async Task<UnitFilterPopupOptions> BuildUnitFilterPopupOptionsAsync<TFaction>(
        bool showRightSelectionBox,
        TFaction? leftSlotFaction,
        TFaction? rightSlotFaction,
        Func<TFaction, int> readFactionId,
        Func<int, CancellationToken, string?> getFiltersJsonByFactionId,
        Func<IReadOnlyCollection<int>, CancellationToken, Task<IReadOnlyList<MercsArmyListEntry>>> getMergedMercsArmyListAsync,
        int maxPoints,
        Action<UnitFilterPopupOptions> setPreparedOptions,
        Action<string>? log = null,
        CancellationToken cancellationToken = default)
        where TFaction : class
    {
        var classification = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var characteristics = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var skills = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var equipment = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var weapons = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var ammo = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var sourceFactions = CompanyUnitDetailsShared.BuildUnitSourceFactions(
            showRightSelectionBox,
            leftSlotFaction,
            rightSlotFaction,
            readFactionId);
        var sourceFactionIds = sourceFactions
            .Select(readFactionId)
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
            var filtersJson = getFiltersJsonByFactionId(factionId, cancellationToken);
            if (string.IsNullOrWhiteSpace(filtersJson))
            {
                continue;
            }

            CompanySelectionSharedUtilities.MergeLookup(typeLookup, CompanyUnitDetailsShared.BuildIdNameLookup(filtersJson, "type"));
            CompanySelectionSharedUtilities.MergeLookup(charsLookup, CompanyUnitDetailsShared.BuildIdNameLookup(filtersJson, "chars"));
            CompanySelectionSharedUtilities.MergeLookup(skillsLookup, CompanyUnitDetailsShared.BuildIdNameLookup(filtersJson, "skills"));
            CompanySelectionSharedUtilities.MergeLookup(equipLookup, CompanyUnitDetailsShared.BuildIdNameLookup(filtersJson, "equip"));
            CompanySelectionSharedUtilities.MergeLookup(weaponsLookup, CompanyUnitDetailsShared.BuildIdNameLookup(filtersJson, "weapons"));
            CompanySelectionSharedUtilities.MergeLookup(ammoLookup, CompanyUnitDetailsShared.BuildIdNameLookup(filtersJson, "ammunition"));
        }

        var mergedMercsList = await getMergedMercsArmyListAsync(sourceFactionIds, cancellationToken);
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
            MaxPoints = maxPoints
        };

        setPreparedOptions(options);
        log?.Invoke(
            $"ArmyFactionSelectionPage filter options: class={options.Classification.Count}, chars={options.Characteristics.Count}, skills={options.Skills.Count}, equip={options.Equipment.Count}, weapons={options.Weapons.Count}, ammo={options.Ammo.Count}.");
        return ClonePopupOptionsForCurrentPoints(options, maxPoints);
    }
}


