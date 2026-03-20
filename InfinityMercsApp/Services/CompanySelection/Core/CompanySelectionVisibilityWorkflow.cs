using InfinityMercsApp.Services;
using InfinityMercsApp.Views.Controls;
using ArmyFactionRecord = InfinityMercsApp.Domain.Models.Army.Faction;
using ArmySpecopsUnitRecord = InfinityMercsApp.Domain.Models.Army.SpecopsUnit;
using ArmyUnitRecord = InfinityMercsApp.Domain.Models.Army.Unit;

namespace InfinityMercsApp.Views.Common;

internal static class CompanySelectionVisibilityWorkflow
{
    internal static int ComputePointsRemaining(string selectedStartSeasonPoints, string seasonPointsCapText)
    {
        var pointsLimit = int.TryParse(selectedStartSeasonPoints, out var parsedLimit) ? parsedLimit : 0;
        var currentPoints = int.TryParse(seasonPointsCapText, out var parsedPoints) ? parsedPoints : 0;
        return pointsLimit - currentPoints;
    }

    internal static int ComputePointsRemaining(ICompanySelectionVisibilityState state)
    {
        return ComputePointsRemaining(state.SelectedStartSeasonPoints, state.SeasonPointsCapText);
    }

    internal static async Task<CompanyUnitVisibilityLookupContext> BuildUnitVisibilityLookupContextAsync<TFaction>(
        IEnumerable<TFaction> factions,
        Func<TFaction, int> readFactionId,
        Func<int, CancellationToken, ArmyFactionRecord?> getFactionSnapshot,
        Func<int, CancellationToken, Task<IReadOnlyList<ArmySpecopsUnitRecord>>> getSpecopsUnitsByFactionAsync,
        CancellationToken cancellationToken)
    {
        var context = new CompanyUnitVisibilityLookupContext();
        foreach (var faction in factions)
        {
            var factionId = readFactionId(faction);
            var snapshot = getFactionSnapshot(factionId, cancellationToken);
            context.SkillsByFactionId[factionId] = CompanyUnitDetailsShared.BuildIdNameLookup(snapshot?.FiltersJson, "skills");
            context.TypeByFactionId[factionId] = CompanyUnitDetailsShared.BuildIdNameLookup(snapshot?.FiltersJson, "type");
            context.CharsByFactionId[factionId] = CompanyUnitDetailsShared.BuildIdNameLookup(snapshot?.FiltersJson, "chars");
            context.EquipByFactionId[factionId] = CompanyUnitDetailsShared.BuildIdNameLookup(snapshot?.FiltersJson, "equip");
            context.WeaponsByFactionId[factionId] = CompanyUnitDetailsShared.BuildIdNameLookup(snapshot?.FiltersJson, "weapons");
            context.AmmoByFactionId[factionId] = CompanyUnitDetailsShared.BuildIdNameLookup(snapshot?.FiltersJson, "ammunition");

            var specopsUnits = await getSpecopsUnitsByFactionAsync(factionId, cancellationToken);
            context.SpecopsByFactionId[factionId] = specopsUnits
                .GroupBy(x => x.UnitId)
                .ToDictionary(x => x.Key, x => x.First());
        }

        return context;
    }

    internal static void ApplyUnitVisibility<TUnit>(
        IEnumerable<TUnit> units,
        CompanyUnitVisibilityLookupContext lookupContext,
        UnitFilterCriteria activeUnitFilter,
        bool lieutenantOnlyUnits,
        int pointsRemaining,
        Func<int, int, CancellationToken, ArmyUnitRecord?> getUnitByFactionAndId,
        CancellationToken cancellationToken)
        where TUnit : CompanyUnitSelectionItemBase
    {
        foreach (var unit in units)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!lookupContext.SkillsByFactionId.TryGetValue(unit.SourceFactionId, out var skillsLookup))
            {
                unit.IsVisible = false;
                continue;
            }

            lookupContext.TypeByFactionId.TryGetValue(unit.SourceFactionId, out var typeLookup);
            lookupContext.CharsByFactionId.TryGetValue(unit.SourceFactionId, out var charsLookup);
            lookupContext.EquipByFactionId.TryGetValue(unit.SourceFactionId, out var equipLookup);
            lookupContext.WeaponsByFactionId.TryGetValue(unit.SourceFactionId, out var weaponsLookup);
            lookupContext.AmmoByFactionId.TryGetValue(unit.SourceFactionId, out var ammoLookup);

            if (!CompanyUnitDetailsShared.MatchesClassificationFilter(activeUnitFilter, unit.Type, typeLookup ?? new Dictionary<int, string>()))
            {
                unit.IsVisible = false;
                continue;
            }

            var unitRecord = getUnitByFactionAndId(unit.SourceFactionId, unit.Id, cancellationToken);
            var profileGroupsJson = unitRecord?.ProfileGroupsJson;
            if (lookupContext.SpecopsByFactionId.TryGetValue(unit.SourceFactionId, out var specopsUnitsById) &&
                specopsUnitsById.TryGetValue(unit.Id, out var specopsUnit))
            {
                if (unit.IsSpecOps || string.IsNullOrWhiteSpace(profileGroupsJson))
                {
                    profileGroupsJson = specopsUnit.ProfileGroupsJson;
                }
            }

            unit.IsVisible = CompanyUnitFilterService.UnitHasVisibleOptionWithFilter(
                profileGroupsJson,
                skillsLookup,
                charsLookup ?? new Dictionary<int, string>(),
                equipLookup ?? new Dictionary<int, string>(),
                weaponsLookup ?? new Dictionary<int, string>(),
                ammoLookup ?? new Dictionary<int, string>(),
                activeUnitFilter,
                requireLieutenant: lieutenantOnlyUnits && !unit.IsSpecOps,
                requireZeroSwc: true,
                maxCost: pointsRemaining);
        }
    }

    internal static TUnit? RefreshSelectedUnitVisibility<TUnit>(
        TUnit? selectedUnit,
        Action resetUnitDetails,
        Action applyLieutenantVisualStates)
        where TUnit : CompanyUnitSelectionItemBase
    {
        if (selectedUnit is not null && !selectedUnit.IsVisible)
        {
            selectedUnit.IsSelected = false;
            resetUnitDetails();
            return null;
        }

        if (selectedUnit is not null)
        {
            applyLieutenantVisualStates();
        }

        return selectedUnit;
    }

    internal static void RefreshTeamEntryVisibility<TTeam, TAllowed, TUnit>(
        IEnumerable<TTeam> teamEntries,
        IEnumerable<TUnit> units)
        where TTeam : CompanyTeamListItemBase<TAllowed>
        where TAllowed : CompanyTeamUnitLimitItemBase
        where TUnit : CompanyUnitSelectionItemBase
    {
        var teamList = teamEntries as IReadOnlyList<TTeam> ?? teamEntries.ToList();
        var unitList = units as IReadOnlyList<TUnit> ?? units.ToList();
        CompanyTeamVisibilityWorkflow.RefreshTeamEntryVisibility<TTeam, TAllowed, TUnit>(
            teamList,
            unitList,
            team => team.AllowedProfiles,
            team => team.IsWildcardBucket,
            (team, value) => team.IsVisible = value,
            (team, value) => team.IsExpanded = value,
            allowed => allowed.IsCharacter,
            (allowed, value) => allowed.IsVisible = value,
            allowed => allowed.Name,
            allowed => allowed.Slug,
            allowed => allowed.ResolvedUnitId,
            allowed => allowed.ResolvedSourceFactionId,
            unit => unit.IsVisible,
            unit => unit.Id,
            unit => unit.SourceFactionId,
            unit => unit.Name,
            unit => unit.Slug);
    }
}
