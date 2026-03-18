using System.Text.RegularExpressions;
using InfinityMercsApp.Services;
using InfinityMercsApp.Views.Common;
using Svg.Skia;
using FactionRecord = InfinityMercsApp.Domain.Models.Metadata.Faction;

namespace InfinityMercsApp.Views.CohesiveCompany;

public partial class CohesiveCompanySelectionPage
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

        return Task.FromResult(
            CohesiveCompanyTeamEligibilityWorkflow.EvaluateValidCoreFireteams(
                snapshot.FireteamChartJson,
                units,
                skillsLookup,
                charsLookup,
                equipLookup,
                weaponsLookup,
                ammoLookup,
                typeLookup,
                _filterState.ActiveUnitFilter,
                maxCost,
                IsFtoLabel,
                unit => IsCharacterCategory(unit, categoryLookup),
                unitId => GetUnitFromProvider(faction.Id, unitId, cancellationToken),
                (fireteamJson, teams) => MergeFireteamEntries(fireteamJson, teams)));
    }

    private static HashSet<string> ParseValidCoreFireteams(string? json)
    {
        return CohesiveCompanyTeamEligibilityWorkflow.ParseValidCoreFireteams(json);
    }

    private void ResetMercsCompany()
    {
        ResetMercsCompanyCore(
            MercsCompanyEntries,
            UpdateMercsCompanyTotal,
            ReevaluateTrackedFireteamLevel);
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
                Console.Error.WriteLine($"CompanySelectionPage tracked fireteam icon load failed: {ex.Message}");
            }
        }

        TrackedFireteamLevelCanvas?.InvalidateSurface();
    }
}



