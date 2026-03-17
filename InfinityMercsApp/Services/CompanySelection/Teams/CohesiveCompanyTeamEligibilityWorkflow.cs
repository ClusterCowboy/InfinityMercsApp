using System.Text.Json;
using InfinityMercsApp.Services;
using InfinityMercsApp.Views.Controls;
using ArmyResumeRecord = InfinityMercsApp.Domain.Models.Army.Resume;
using ArmyUnitRecord = InfinityMercsApp.Domain.Models.Army.Unit;

namespace InfinityMercsApp.Views.Common;

internal static class CohesiveCompanyTeamEligibilityWorkflow
{
    private sealed class EligibilitySourceUnit
    {
        public required int Id { get; init; }
        public required string? Name { get; init; }
        public required string? Slug { get; init; }
        public required int? Type { get; init; }
        public required bool IsCharacter { get; init; }
    }

    internal static List<string> EvaluateValidCoreFireteams(
        string? fireteamChartJson,
        IReadOnlyList<ArmyResumeRecord> units,
        IReadOnlyDictionary<int, string> skillsLookup,
        IReadOnlyDictionary<int, string> charsLookup,
        IReadOnlyDictionary<int, string> equipLookup,
        IReadOnlyDictionary<int, string> weaponsLookup,
        IReadOnlyDictionary<int, string> ammoLookup,
        IReadOnlyDictionary<int, string> typeLookup,
        UnitFilterCriteria activeUnitFilter,
        int maxCost,
        Func<string?, bool> isFtoLabel,
        Func<ArmyResumeRecord, bool> isCharacterCategory,
        Func<int, ArmyUnitRecord?> getUnitById,
        Action<string?, Dictionary<string, CompanyTeamAggregate>> mergeFireteamEntries)
    {
        var sourceUnits = units.Select(unit => new EligibilitySourceUnit
            {
                Id = unit.UnitId,
                Name = unit.Name,
                Slug = unit.Slug,
                Type = unit.Type,
                IsCharacter = isCharacterCategory(unit)
            })
            .ToList();

        var teams = new Dictionary<string, CompanyTeamAggregate>(StringComparer.OrdinalIgnoreCase);
        mergeFireteamEntries(fireteamChartJson, teams);

        var validCoreFireteams = new List<string>();
        foreach (var team in teams.Values
                     .Where(x => x.Core > 0)
                     .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase))
        {
            var hasLieutenantProfileAfterFilters = false;
            foreach (var unitLimit in team.UnitLimits)
            {
                var matchedUnit = CompanyTeamMatchingWorkflow.ResolveUnitForTeamEntry(
                    unitLimit.Key,
                    unitLimit.Value.Slug,
                    sourceUnits,
                    x => x.Name,
                    x => x.Slug);
                if (matchedUnit is null || matchedUnit.IsCharacter)
                {
                    continue;
                }

                if (!CompanyUnitDetailsShared.MatchesClassificationFilter(activeUnitFilter, matchedUnit.Type, typeLookup))
                {
                    continue;
                }

                var unitRecord = getUnitById(matchedUnit.Id);
                if (string.IsNullOrWhiteSpace(unitRecord?.ProfileGroupsJson))
                {
                    continue;
                }

                var requiresFtoProfile = isFtoLabel(unitLimit.Key);
                var hasAnyVisibleProfile = CompanyUnitFilterService.UnitHasVisibleOptionWithFilter(
                    unitRecord.ProfileGroupsJson,
                    skillsLookup,
                    charsLookup,
                    equipLookup,
                    weaponsLookup,
                    ammoLookup,
                    activeUnitFilter,
                    requireLieutenant: false,
                    requireZeroSwc: true,
                    maxCost: maxCost,
                    optionNamePredicate: requiresFtoProfile ? isFtoLabel : null);
                if (!hasAnyVisibleProfile)
                {
                    continue;
                }

                var hasVisibleLieutenantProfile = CompanyUnitFilterService.UnitHasVisibleOptionWithFilter(
                    unitRecord.ProfileGroupsJson,
                    skillsLookup,
                    charsLookup,
                    equipLookup,
                    weaponsLookup,
                    ammoLookup,
                    activeUnitFilter,
                    requireLieutenant: true,
                    requireZeroSwc: true,
                    maxCost: maxCost,
                    optionNamePredicate: requiresFtoProfile ? isFtoLabel : null);
                if (!hasVisibleLieutenantProfile)
                {
                    continue;
                }

                hasLieutenantProfileAfterFilters = true;
                break;
            }

            if (hasLieutenantProfileAfterFilters)
            {
                validCoreFireteams.Add(team.Name);
            }
        }

        return validCoreFireteams
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    internal static HashSet<string> ParseValidCoreFireteams(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            var names = JsonSerializer.Deserialize<List<string>>(json) ?? [];
            return new HashSet<string>(
                names.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()),
                StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }
    }
}
