using System.Text.Json;
using System.Text.RegularExpressions;
using InfinityMercsApp.Services;
using InfinityMercsApp.Views.Common;
using Svg.Skia;
using FactionRecord = InfinityMercsApp.Domain.Models.Metadata.Faction;

namespace InfinityMercsApp.Views.CohesiveCompany;

public partial class CCArmyFactionSelectionPage
{
    private Task<List<string>> EvaluateValidCoreFireteamsForFactionAsync(
        FactionRecord faction,
        int maxCost,
        CancellationToken cancellationToken)
    {
        var validCoreFireteams = new List<string>();
        var snapshot = GetFactionSnapshotFromProvider(faction.Id, cancellationToken);
        if (snapshot is null || string.IsNullOrWhiteSpace(snapshot.FireteamChartJson))
        {
            return Task.FromResult(validCoreFireteams);
        }

        var skillsLookup = CompanyUnitDetailsShared.BuildIdNameLookup(snapshot.FiltersJson, "skills");
        var charsLookup = CompanyUnitDetailsShared.BuildIdNameLookup(snapshot.FiltersJson, "chars");
        var equipLookup = CompanyUnitDetailsShared.BuildIdNameLookup(snapshot.FiltersJson, "equip");
        var weaponsLookup = CompanyUnitDetailsShared.BuildIdNameLookup(snapshot.FiltersJson, "weapons");
        var ammoLookup = CompanyUnitDetailsShared.BuildIdNameLookup(snapshot.FiltersJson, "ammunition");
        var typeLookup = CompanyUnitDetailsShared.BuildIdNameLookup(snapshot.FiltersJson, "type");
        var categoryLookup = CompanyUnitDetailsShared.BuildIdNameLookup(snapshot.FiltersJson, "category");

        var units = GetResumeByFactionMercsOnlyFromProvider(faction.Id, cancellationToken);
        var sourceUnits = units.Select(unit => new ArmyUnitSelectionItem
            {
                Id = unit.UnitId,
                SourceFactionId = faction.Id,
                Slug = unit.Slug,
                Name = unit.Name,
                Type = unit.Type,
                IsCharacter = IsCharacterCategory(unit, categoryLookup)
            })
            .ToList();

        var teams = new Dictionary<string, CompanyTeamAggregate>(StringComparer.OrdinalIgnoreCase);
        MergeFireteamEntries(snapshot.FireteamChartJson, teams);
        foreach (var team in teams.Values
                     .Where(x => x.Core > 0)
                     .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase))
        {
            var hasLieutenantProfileAfterFilters = false;
            foreach (var unitLimit in team.UnitLimits)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var matchedUnit = CompanyTeamMatchingWorkflow.ResolveUnitForTeamEntry(
                    unitLimit.Key,
                    unitLimit.Value.Slug,
                    sourceUnits);
                if (matchedUnit is null || matchedUnit.IsCharacter)
                {
                    continue;
                }

                if (!CompanyUnitDetailsShared.MatchesClassificationFilter(_activeUnitFilter, matchedUnit.Type, typeLookup))
                {
                    continue;
                }

                var unitRecord = GetUnitFromProvider(faction.Id, matchedUnit.Id, cancellationToken);
                if (string.IsNullOrWhiteSpace(unitRecord?.ProfileGroupsJson))
                {
                    continue;
                }

                var requiresFtoProfile = IsFtoLabel(unitLimit.Key);
                var hasAnyVisibleProfile = CompanyUnitFilterService.UnitHasVisibleOptionWithFilter(
                    unitRecord.ProfileGroupsJson,
                    skillsLookup,
                    charsLookup,
                    equipLookup,
                    weaponsLookup,
                    ammoLookup,
                    _activeUnitFilter,
                    requireLieutenant: false,
                    requireZeroSwc: true,
                    maxCost: maxCost,
                    optionNamePredicate: requiresFtoProfile ? IsFtoLabel : null);
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
                    _activeUnitFilter,
                    requireLieutenant: true,
                    requireZeroSwc: true,
                    maxCost: maxCost,
                    optionNamePredicate: requiresFtoProfile ? IsFtoLabel : null);

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

        var normalized = validCoreFireteams
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();
        return Task.FromResult(normalized);
    }

    private static HashSet<string> ParseValidCoreFireteams(string? json)
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

    private void ResetMercsCompany()
    {
        if (MercsCompanyEntries.Count == 0)
        {
            UpdateMercsCompanyTotal();
            ReevaluateTrackedFireteamLevel();
            return;
        }

        MercsCompanyEntries.Clear();
        UpdateMercsCompanyTotal();
        ReevaluateTrackedFireteamLevel();
    }

    private void OnTeamTrackingRadioButtonCheckedChanged(object? sender, CheckedChangedEventArgs e)
    {
        if (_isUpdatingTrackedTeamSelection || !e.Value)
        {
            return;
        }

        if (sender is not RadioButton radioButton || radioButton.BindingContext is not ArmyTeamListItem team)
        {
            return;
        }

        if (!team.ShowTrackingRadioButton)
        {
            return;
        }

        SetTrackedFireteamSelection(team.Name);
    }

    private void RestoreTrackedFireteamSelection(string? trackedFireteamName)
    {
        if (string.IsNullOrWhiteSpace(trackedFireteamName))
        {
            SetTrackedFireteamSelection(GetDefaultTrackedFireteamName());
            return;
        }

        var matchingTeam = TeamEntries.FirstOrDefault(x =>
            x.ShowTrackingRadioButton &&
            string.Equals(x.Name, trackedFireteamName, StringComparison.OrdinalIgnoreCase));
        SetTrackedFireteamSelection(matchingTeam?.Name ?? GetDefaultTrackedFireteamName());
    }

    private string GetDefaultTrackedFireteamName()
    {
        var firstVisibleTeam = TeamEntries.FirstOrDefault(x => x.ShowTrackingRadioButton && x.IsVisible);
        if (firstVisibleTeam is not null)
        {
            return firstVisibleTeam.Name;
        }

        var firstTrackableTeam = TeamEntries.FirstOrDefault(x => x.ShowTrackingRadioButton);
        return firstTrackableTeam?.Name ?? string.Empty;
    }

    private void SetTrackedFireteamSelection(string? teamName)
    {
        var normalizedTeamName = teamName?.Trim() ?? string.Empty;
        var hasSelection = !string.IsNullOrWhiteSpace(normalizedTeamName);

        _isUpdatingTrackedTeamSelection = true;
        try
        {
            foreach (var team in TeamEntries)
            {
                team.IsTrackedTeam = team.ShowTrackingRadioButton &&
                                     hasSelection &&
                                     string.Equals(team.Name, normalizedTeamName, StringComparison.OrdinalIgnoreCase);
            }
        }
        finally
        {
            _isUpdatingTrackedTeamSelection = false;
        }

        if (string.Equals(_trackedFireteamName, normalizedTeamName, StringComparison.Ordinal))
        {
            ReevaluateTrackedFireteamLevel();
            return;
        }

        _trackedFireteamName = normalizedTeamName;
        OnPropertyChanged(nameof(TrackedFireteamNameDisplay));
        ReevaluateTrackedFireteamLevel();
    }

    private void ReevaluateTrackedFireteamLevel()
    {
        var evaluatedLevel = EvaluateTrackedFireteamLevel();
        OnTrackedFireteamLevelEvaluated(evaluatedLevel);
    }

    private int EvaluateTrackedFireteamLevel()
    {
        if (string.IsNullOrWhiteSpace(_trackedFireteamName))
        {
            return 0;
        }

        var trackedTeam = TeamEntries.FirstOrDefault(x =>
            x.ShowTrackingRadioButton &&
            string.Equals(x.Name, _trackedFireteamName, StringComparison.OrdinalIgnoreCase));
        if (trackedTeam is null)
        {
            return 0;
        }

        var allowedNames = BuildTrackedTeamAllowedNameSet(trackedTeam);
        if (allowedNames.Count == 0)
        {
            return 0;
        }

        var matchingTrooperCount = 0;
        foreach (var entry in MercsCompanyEntries)
        {
            if (IsTrackedTeamMatch(entry, trackedTeam, allowedNames))
            {
                matchingTrooperCount++;
            }
        }

        return Math.Clamp(matchingTrooperCount, 0, 6);
    }

    private static bool IsTrackedTeamMatch(
        MercsCompanyEntry entry,
        ArmyTeamListItem trackedTeam,
        HashSet<string> allowedNames)
    {
        if (trackedTeam.AllowedProfiles.Any(x =>
                x.ResolvedUnitId.HasValue &&
                x.ResolvedSourceFactionId.HasValue &&
                x.ResolvedUnitId.Value == entry.SourceUnitId &&
                x.ResolvedSourceFactionId.Value == entry.SourceFactionId))
        {
            return true;
        }

        var candidateNames = new HashSet<string>(StringComparer.Ordinal);
        AddAllowedNameCandidate(candidateNames, entry.BaseUnitName);
        AddAllowedNameCandidate(candidateNames, entry.Name);

        return candidateNames.Any(allowedNames.Contains);
    }

    private static HashSet<string> BuildTrackedTeamAllowedNameSet(ArmyTeamListItem trackedTeam)
    {
        var allowedNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var allowedProfile in trackedTeam.AllowedProfiles)
        {
            AddAllowedNameCandidate(allowedNames, allowedProfile.Name);

            foreach (Match match in Regex.Matches(allowedProfile.Name ?? string.Empty, @"\(([^)]*)\)"))
            {
                var groupValue = match.Groups[1].Value;
                foreach (var alias in groupValue.Split([',', '/', ';', '|'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
                {
                    AddAllowedNameCandidate(allowedNames, alias);
                }
            }
        }

        return allowedNames;
    }

    private static void AddAllowedNameCandidate(HashSet<string> target, string? rawCandidate)
    {
        if (string.IsNullOrWhiteSpace(rawCandidate))
        {
            return;
        }

        var withoutParens = Regex.Replace(rawCandidate, @"\([^)]*\)", " ");
        var normalized = CompanyTeamMatchingWorkflow.NormalizeTeamUnitName(withoutParens);
        if (!string.IsNullOrWhiteSpace(normalized))
        {
            target.Add(normalized);
        }
    }

    // Hook point for full fireteam-level evaluation logic. Provide an evaluated value in [1..6].
    private void OnTrackedFireteamLevelEvaluated(int fireteamLevel)
    {
        SetTrackedFireteamLevel(fireteamLevel);
    }

    private void SetTrackedFireteamLevel(int fireteamLevel)
    {
        var normalizedLevel = Math.Clamp(fireteamLevel, 0, 6);
        if (_trackedFireteamLevel == normalizedLevel)
        {
            return;
        }

        _trackedFireteamLevel = normalizedLevel;
        _ = LoadTrackedFireteamLevelIconAsync(_trackedFireteamLevel);
    }

    private async Task LoadTrackedFireteamLevelIconAsync(int fireteamLevel)
    {
        _trackedFireteamLevelPicture?.Dispose();
        _trackedFireteamLevelPicture = null;

        if (fireteamLevel >= 1 && fireteamLevel <= 6)
        {
            try
            {
                var iconPath = $"SVGCache/NonCBIcons/Fireteam/noun-team-{fireteamLevel}.svg";
                await using var stream = await FileSystem.Current.OpenAppPackageFileAsync(iconPath);
                var svg = new SKSvg();
                _trackedFireteamLevelPicture = svg.Load(stream);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"ArmyFactionSelectionPage tracked fireteam icon load failed: {ex.Message}");
            }
        }

        TrackedFireteamLevelCanvas?.InvalidateSurface();
    }
}


