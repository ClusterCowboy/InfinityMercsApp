using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Text;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using InfinityMercsApp.Domain.Utilities;
using InfinityMercsApp.Infrastructure.Providers;
using InfinityMercsApp.Services;
using InfinityMercsApp.Views.Controls;
using FactionRecord = InfinityMercsApp.Domain.Models.Metadata.Faction;
using Resume = InfinityMercsApp.Domain.Models.Army.Resume;

namespace InfinityMercsApp.ViewModels;

public partial class ViewerViewModel
{
    public async Task LoadSpecificUnitAsync(
        int sourceFactionId,
        int sourceUnitId,
        string unitName,
        string? cachedLogoPath = null,
        string? packagedLogoPath = null,
        CancellationToken cancellationToken = default)
    {
        SelectedFaction = new ViewerFactionItem
        {
            Id = sourceFactionId,
            Name = string.Empty
        };

        SelectedUnit = new ViewerUnitItem
        {
            Id = sourceUnitId,
            Name = unitName,
            CachedLogoPath = cachedLogoPath,
            PackagedLogoPath = packagedLogoPath
        };

        await LoadProfilesForSelectedUnitAsync(cancellationToken);
    }

    public async Task LoadSpecificConfigurationAsync(
        int sourceFactionId,
        int sourceUnitId,
        string unitName,
        string profileKey,
        bool isLieutenant,
        string? savedSkills = null,
        string? savedEquipment = null,
        string? savedRangedWeapons = null,
        string? savedCcWeapons = null,
        string? cachedLogoPath = null,
        string? packagedLogoPath = null,
        CancellationToken cancellationToken = default)
    {
        await LoadSpecificUnitAsync(
            sourceFactionId,
            sourceUnitId,
            unitName,
            cachedLogoPath,
            packagedLogoPath,
            cancellationToken);

        if (string.IsNullOrWhiteSpace(profileKey))
        {
            return;
        }

        var matchedProfile = Profiles
            .FirstOrDefault(x =>
                ProfileKeysMatch(x.ProfileKey, profileKey) &&
                x.IsLieutenant == isLieutenant);

        matchedProfile ??= Profiles
            .FirstOrDefault(x => ProfileKeysMatch(x.ProfileKey, profileKey));

        matchedProfile ??= FindFallbackProfileMatch(
            Profiles,
            profileKey,
            isLieutenant,
            savedSkills,
            savedEquipment,
            savedRangedWeapons,
            savedCcWeapons);

        if (matchedProfile is null)
        {
            ResetUnitStats();
            UnitNameHeading = unitName;
            EquipmentSummary = "Equipment: -";
            SpecialSkillsSummary = "Special Skills: -";
            Profiles.Clear();
            ProfilesStatus = "Saved configuration not found for this unit.";
            return;
        }

        Profiles.Clear();
        Profiles.Add(matchedProfile);
        ApplySelectedProfileTopSummaries(matchedProfile);

        ProfilesStatus = "1 configuration loaded.";
    }

    public void ApplySelectedProfileTopSummaries(ViewerProfileItem matchedProfile)
    {
        var mergedEquipment = MergeSummaryAndUnique(EquipmentSummary, matchedProfile.UniqueEquipment);
        var mergedSkills = MergeSummaryAndUnique(SpecialSkillsSummary, matchedProfile.UniqueSkills);
        var mergedEquipmentValues = ExtractSummaryValues(mergedEquipment).ToList();
        var mergedSkillValues = ExtractSummaryValues(mergedSkills).ToList();

        EquipmentSummary = $"Equipment: {mergedEquipment}";
        SpecialSkillsSummary = $"Special Skills: {mergedSkills}";

        EquipmentSummaryFormatted = BuildNamedSummaryFormatted(
            "Equipment",
            mergedEquipmentValues,
            _currentEquipmentLookup,
            _currentEquipmentLinks,
            Color.FromArgb("#06B6D4"));
        SpecialSkillsSummaryFormatted = BuildNamedSummaryFormatted(
            "Special Skills",
            mergedSkillValues,
            _currentSkillsLookup,
            _currentSkillsLinks,
            Color.FromArgb("#F59E0B"));
    }

    public void SetOrderTypeIconState(bool showRegular, bool showIrregular)
    {
        ShowRegularOrderIcon = showRegular;
        ShowIrregularOrderIcon = showIrregular;
    }

    public void SetTechTraitIconState(bool showCube, bool showCube2, bool showHackable)
    {
        ShowCubeIcon = showCube;
        ShowCube2Icon = showCube2;
        ShowHackableIcon = showHackable;
    }

    public void ApplyTacticalAwarenessOverride(bool hasTacticalAwareness)
    {
        ShowTacticalAwarenessIcon = ShowTacticalAwarenessIcon || hasTacticalAwareness;
    }

    public void ApplyHackableOverrideFromCurrentConfiguration(string? currentEquipment, string? currentSkills)
    {
        var hasHackableFromCurrentState = ContainsHackableFromCurrentState(currentEquipment, currentSkills);
        ShowHackableIcon = ShowHackableIcon || hasHackableFromCurrentState;
    }

    public void ApplyAugmentStatOverride(string statName, string value)
    {
        switch (statName.ToUpperInvariant())
        {
            case "CC":   UnitCc       = value; break;
            case "BS":   UnitBs       = value; break;
            case "PH":   UnitPh       = value; break;
            case "WIP":  UnitWip      = value; break;
            case "ARM":  UnitArm      = value; break;
            case "BTS":  UnitBts      = value; break;
            case "S":    UnitS        = value; break;
            case "VITA": UnitVitality = value; break;
            case "STR":  UnitVitality = value; break;
        }
    }

    public void ApplyCaptainStatBonuses(
        int ccBonus,
        int bsBonus,
        int phBonus,
        int wipBonus,
        int armBonus,
        int btsBonus,
        int vitalityBonus)
    {
        UnitCc = ApplyNumericBonus(UnitCc, ccBonus);
        UnitBs = ApplyNumericBonus(UnitBs, bsBonus);
        UnitPh = ApplyNumericBonus(UnitPh, phBonus);
        UnitWip = ApplyNumericBonus(UnitWip, wipBonus);
        UnitArm = ApplyNumericBonus(UnitArm, armBonus);
        UnitBts = ApplyNumericBonus(UnitBts, btsBonus);
        UnitVitality = ApplyNumericBonus(UnitVitality, vitalityBonus);
    }

    public void ApplySavedUnitSnapshot(
        string unitNameHeading,
        string mov,
        string cc,
        string bs,
        string ph,
        string wip,
        string arm,
        string bts,
        string vitalityHeader,
        string vitality,
        string s,
        bool isLieutenant)
    {
        UnitNameHeading = string.IsNullOrWhiteSpace(unitNameHeading) ? "Unit" : unitNameHeading;
        if (TryParseSavedMoveToCentimeters(mov, out var firstCm, out var secondCm))
        {
            _unitMoveFirstCm = firstCm;
            _unitMoveSecondCm = secondCm;
            UpdateUnitMoveDisplay();
        }
        else
        {
            _unitMoveFirstCm = null;
            _unitMoveSecondCm = null;
            UnitMov = string.IsNullOrWhiteSpace(mov) ? "-" : mov;
        }

        UnitCc = string.IsNullOrWhiteSpace(cc) ? "-" : cc;
        UnitBs = string.IsNullOrWhiteSpace(bs) ? "-" : bs;
        UnitPh = string.IsNullOrWhiteSpace(ph) ? "-" : ph;
        UnitWip = string.IsNullOrWhiteSpace(wip) ? "-" : wip;
        UnitArm = string.IsNullOrWhiteSpace(arm) ? "-" : arm;
        UnitBts = string.IsNullOrWhiteSpace(bts) ? "-" : bts;
        UnitVitalityHeader = string.IsNullOrWhiteSpace(vitalityHeader) ? "VITA" : vitalityHeader;
        UnitVitality = string.IsNullOrWhiteSpace(vitality) ? "-" : vitality;
        UnitS = string.IsNullOrWhiteSpace(s) ? "-" : s;
        UnitAva = "-";

        EquipmentSummary = "Equipment: -";
        SpecialSkillsSummary = "Special Skills: -";
        EquipmentSummaryFormatted = BuildNamedSummaryFormatted(
            "Equipment",
            [],
            new Dictionary<int, string>(),
            new Dictionary<int, string>(),
            Color.FromArgb("#06B6D4"));
        SpecialSkillsSummaryFormatted = BuildNamedSummaryFormatted(
            "Special Skills",
            [],
            new Dictionary<int, string>(),
            new Dictionary<int, string>(),
            Color.FromArgb("#F59E0B"));

        ShowRegularOrderIcon = !isLieutenant;
        ShowIrregularOrderIcon = false;
        ShowImpetuousIcon = false;
        ShowTacticalAwarenessIcon = false;
        ShowCubeIcon = false;
        ShowCube2Icon = false;
        ShowHackableIcon = false;
    }

    private static bool TryParseSavedMoveToCentimeters(string? moveText, out int? firstCm, out int? secondCm)
    {
        firstCm = null;
        secondCm = null;

        if (string.IsNullOrWhiteSpace(moveText))
        {
            return false;
        }

        var text = moveText.Trim();
        var simple = Regex.Match(text, @"^(?<a>\d+)\s*[-/]\s*(?<b>\d+)$", RegexOptions.IgnoreCase);
        if (simple.Success &&
            int.TryParse(simple.Groups["a"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var aSimple) &&
            int.TryParse(simple.Groups["b"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var bSimple))
        {
            var isLikelyInches = aSimple <= 8 && bSimple <= 8;
            firstCm = isLikelyInches
                ? (int)Math.Round(aSimple * 2.5, MidpointRounding.AwayFromZero)
                : aSimple;
            secondCm = isLikelyInches
                ? (int)Math.Round(bSimple * 2.5, MidpointRounding.AwayFromZero)
                : bSimple;
            return true;
        }

        var tokenized = Regex.Match(
            text,
            @"^(?<a>\d+)\s*(?<au>cm|""|in|inch|inches)?\s*[-/]\s*(?<b>\d+)\s*(?<bu>cm|""|in|inch|inches)?$",
            RegexOptions.IgnoreCase);
        if (!tokenized.Success ||
            !int.TryParse(tokenized.Groups["a"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var a) ||
            !int.TryParse(tokenized.Groups["b"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var b))
        {
            return false;
        }

        var aUnit = tokenized.Groups["au"].Success ? tokenized.Groups["au"].Value.Trim().ToLowerInvariant() : string.Empty;
        var bUnit = tokenized.Groups["bu"].Success ? tokenized.Groups["bu"].Value.Trim().ToLowerInvariant() : string.Empty;
        var hasExplicitUnits = !string.IsNullOrWhiteSpace(aUnit) || !string.IsNullOrWhiteSpace(bUnit);
        var useInches = hasExplicitUnits
            ? aUnit is "\"" or "in" or "inch" or "inches" || bUnit is "\"" or "in" or "inch" or "inches"
            : (a <= 8 && b <= 8);

        firstCm = useInches
            ? (int)Math.Round(a * 2.5, MidpointRounding.AwayFromZero)
            : a;
        secondCm = useInches
            ? (int)Math.Round(b * 2.5, MidpointRounding.AwayFromZero)
            : b;
        return true;
    }

    private static string MergeSummaryAndUnique(string summaryLine, string uniqueValues)
    {
        var merged = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var part in ExtractSummaryValues(summaryLine))
        {
            merged.Add(part);
        }

        foreach (var part in ExtractSummaryValues(uniqueValues))
        {
            merged.Add(part);
        }

        if (merged.Count == 0)
        {
            return "-";
        }

        var normalized = NormalizeLtOrderSummaryEntries(merged);
        return string.Join(", ", normalized.OrderBy(x => x, StringComparer.OrdinalIgnoreCase));
    }

    private static IReadOnlyCollection<string> NormalizeLtOrderSummaryEntries(IEnumerable<string> values)
    {
        var normalized = values
            .Select(x => Regex.Replace(
                x,
                @"\blieutenant\b([^\n\r]*\(\s*\+(\d+)\s*)orders?(\s*\))",
                "Lieutenant$1Lt Order$3",
                RegexOptions.IgnoreCase))
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x) && x != "-")
            .ToList();

        var hasLieutenantContext = normalized.Any(x =>
            x.Contains("lt order", StringComparison.OrdinalIgnoreCase) ||
            x.Contains("lieutenant order", StringComparison.OrdinalIgnoreCase) ||
            x.Contains("lieutenant", StringComparison.OrdinalIgnoreCase));

        if (hasLieutenantContext)
        {
            for (var i = 0; i < normalized.Count; i++)
            {
                normalized[i] = Regex.Replace(
                    normalized[i],
                    @"\+(\d+)\s*(?:regular\s*)?orders?\b",
                    "+$1 Lt Order",
                    RegexOptions.IgnoreCase);
            }
        }

        var detailedLtBonuses = new HashSet<int>();
        foreach (var value in normalized)
        {
            var detailMatches = Regex.Matches(
                value,
                @"\blieutenant\b[^\n\r]*\(\s*\+(\d+)\s*(?:lt|lieutenant)?\s*orders?\s*\)",
                RegexOptions.IgnoreCase);
            foreach (Match match in detailMatches)
            {
                if (match.Groups.Count < 2 || !int.TryParse(match.Groups[1].Value, out var parsed))
                {
                    continue;
                }

                detailedLtBonuses.Add(Math.Max(0, parsed));
            }
        }

        var deduped = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var value in normalized)
        {
            var standaloneMatch = Regex.Match(
                value,
                @"^\+(\d+)\s*(?:lt|lieutenant)\s*orders?\s*$",
                RegexOptions.IgnoreCase);
            if (standaloneMatch.Success &&
                standaloneMatch.Groups.Count >= 2 &&
                int.TryParse(standaloneMatch.Groups[1].Value, out var standaloneBonus) &&
                detailedLtBonuses.Contains(Math.Max(0, standaloneBonus)))
            {
                continue;
            }

            deduped.Add(value);
        }

        return deduped;
    }

    private static IEnumerable<string> ExtractSummaryValues(string? summaryText)
    {
        if (string.IsNullOrWhiteSpace(summaryText))
        {
            return [];
        }

        var payload = summaryText;
        var colonIndex = summaryText.IndexOf(':');
        if (colonIndex >= 0 && colonIndex < summaryText.Length - 1)
        {
            payload = summaryText[(colonIndex + 1)..];
        }

        return CompanyProfileTextService.SplitDisplayLine(payload)
            .Where(x => !string.Equals(x, "-", StringComparison.Ordinal));
    }

    private static string ApplyNumericBonus(string value, int bonus)
    {
        if (bonus == 0)
        {
            return value;
        }

        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? (parsed + bonus).ToString(CultureInfo.InvariantCulture)
            : value;
    }

    private static bool ContainsHackableFromCurrentState(string? equipmentText, string? skillsText)
    {
        static IEnumerable<string> SplitLines(string? value) =>
            CompanyProfileTextService.SplitDisplayLine(value);

        foreach (var line in SplitLines(equipmentText).Concat(SplitLines(skillsText)))
        {
            var normalized = NormalizeTokenText(line);
            if (Regex.IsMatch(normalized, @"\bhacking\s*device\b", RegexOptions.IgnoreCase) ||
                Regex.IsMatch(normalized, @"\bkiller\s*hacking\s*device\b", RegexOptions.IgnoreCase) ||
                Regex.IsMatch(normalized, @"\bevo\s*hacking\s*device\b", RegexOptions.IgnoreCase) ||
                Regex.IsMatch(normalized, @"\bhacking\s*device\s*plus\b|\bhd\s*\+\b", RegexOptions.IgnoreCase) ||
                Regex.IsMatch(normalized, @"\bkhd\b|\bevo\s*hd\b", RegexOptions.IgnoreCase))
            {
                return true;
            }

            if (Regex.IsMatch(normalized, @"\bhackable\b", RegexOptions.IgnoreCase) &&
                !Regex.IsMatch(normalized, @"\b(non[\s-]*hackable|not[\s-]*hackable)\b", RegexOptions.IgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ProfileKeysMatch(string candidateKey, string requestedKey)
    {
        if (string.Equals(candidateKey, requestedKey, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return string.Equals(
            BuildLegacyProfileKey(candidateKey),
            BuildLegacyProfileKey(requestedKey),
            StringComparison.OrdinalIgnoreCase);
    }

    private static ViewerProfileItem? FindFallbackProfileMatch(
        IEnumerable<ViewerProfileItem> profiles,
        string requestedKey,
        bool requestedIsLieutenant,
        string? savedSkills = null,
        string? savedEquipment = null,
        string? savedRangedWeapons = null,
        string? savedCcWeapons = null)
    {
        if (!TryParseProfileKey(requestedKey, out var requestedGroup, out _, out var requestedCost, out var requestedSwc, out var requestedLt))
        {
            return null;
        }

        var parsedProfiles = profiles
            .Select(profile =>
            {
                var parsed = TryParseProfileKey(profile.ProfileKey, out var group, out _, out var cost, out var swc, out var lt);
                return new
                {
                    Profile = profile,
                    Parsed = parsed,
                    Group = group,
                    Cost = cost,
                    Swc = swc,
                    Lt = lt
                };
            })
            .Where(x => x.Parsed)
            .ToList();

        if (parsedProfiles.Count == 0)
        {
            return null;
        }

        // Preferred fallback: stable key parts that are less likely to drift than the display option name.
        var ltTarget = requestedLt.HasValue ? (requestedLt.Value ? 1 : 0) : (requestedIsLieutenant ? 1 : 0);

        var strict = parsedProfiles
            .Where(x =>
                string.Equals(x.Group, requestedGroup, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(x.Cost, requestedCost, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(x.Swc, requestedSwc, StringComparison.OrdinalIgnoreCase) &&
                x.Lt.HasValue &&
                (x.Lt.Value ? 1 : 0) == ltTarget)
            .Select(x => x.Profile)
            .ToList();
        if (strict.Count == 1)
        {
            return strict[0];
        }

        var byGroupCostSwc = parsedProfiles
            .Where(x =>
                string.Equals(x.Group, requestedGroup, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(x.Cost, requestedCost, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(x.Swc, requestedSwc, StringComparison.OrdinalIgnoreCase))
            .Select(x => x.Profile)
            .ToList();
        if (byGroupCostSwc.Count == 1)
        {
            return byGroupCostSwc[0];
        }

        var byCostSwcLt = parsedProfiles
            .Where(x =>
                string.Equals(x.Cost, requestedCost, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(x.Swc, requestedSwc, StringComparison.OrdinalIgnoreCase) &&
                x.Lt.HasValue &&
                (x.Lt.Value ? 1 : 0) == ltTarget)
            .Select(x => x.Profile)
            .ToList();
        if (byCostSwcLt.Count == 1)
        {
            return byCostSwcLt[0];
        }

        var byCostSwc = parsedProfiles
            .Where(x =>
                string.Equals(x.Cost, requestedCost, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(x.Swc, requestedSwc, StringComparison.OrdinalIgnoreCase))
            .Select(x => x.Profile)
            .ToList();
        if (byCostSwc.Count == 1)
        {
            return byCostSwc[0];
        }

        var ambiguousCandidates = byGroupCostSwc.Count > 1
            ? byGroupCostSwc
            : byCostSwc;
        if (ambiguousCandidates.Count <= 1)
        {
            return null;
        }

        return FindBestCandidateBySavedLines(
            ambiguousCandidates,
            savedSkills,
            savedEquipment,
            savedRangedWeapons,
            savedCcWeapons);
    }

    private static ViewerProfileItem? FindBestCandidateBySavedLines(
        IReadOnlyList<ViewerProfileItem> candidates,
        string? savedSkills,
        string? savedEquipment,
        string? savedRangedWeapons,
        string? savedCcWeapons)
    {
        var savedSkillSet = SplitSavedLines(savedSkills);
        var savedEquipmentSet = SplitSavedLines(savedEquipment);
        var savedRangedSet = SplitSavedLines(savedRangedWeapons);
        var savedCcSet = SplitSavedLines(savedCcWeapons);

        var ranked = candidates
            .Select(profile => new
            {
                Profile = profile,
                Score = ComputeCandidateScore(profile, savedSkillSet, savedEquipmentSet, savedRangedSet, savedCcSet)
            })
            .OrderByDescending(x => x.Score)
            .ToList();

        if (ranked.Count == 0)
        {
            return null;
        }

        if (ranked[0].Score <= 0)
        {
            return null;
        }

        if (ranked.Count > 1 && ranked[0].Score == ranked[1].Score)
        {
            return null;
        }

        return ranked[0].Profile;
    }

    private static int ComputeCandidateScore(
        ViewerProfileItem profile,
        HashSet<string> savedSkillSet,
        HashSet<string> savedEquipmentSet,
        HashSet<string> savedRangedSet,
        HashSet<string> savedCcSet)
    {
        var score = 0;
        score += CountMatches(savedSkillSet, SplitSavedLines(profile.UniqueSkills));
        score += CountMatches(savedEquipmentSet, SplitSavedLines(profile.UniqueEquipment));
        score += CountMatches(savedRangedSet, SplitSavedLines(profile.RangedWeapons));
        score += CountMatches(savedCcSet, SplitSavedLines(profile.MeleeWeapons));
        return score;
    }

    private static int CountMatches(HashSet<string> left, HashSet<string> right)
    {
        if (left.Count == 0 || right.Count == 0)
        {
            return 0;
        }

        var matches = 0;
        foreach (var item in left)
        {
            if (right.Contains(item))
            {
                matches++;
            }
        }

        return matches;
    }

    private static HashSet<string> SplitSavedLines(string? text)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(text))
        {
            return set;
        }

        foreach (var line in CompanyProfileTextService.SplitDisplayLine(text))
        {
            if (string.IsNullOrWhiteSpace(line) || line == "-")
            {
                continue;
            }

            set.Add(line.Trim());
        }

        return set;
    }

    private static bool TryParseProfileKey(
        string profileKey,
        out string groupName,
        out string optionName,
        out string cost,
        out string swc,
        out bool? isLieutenant)
    {
        groupName = string.Empty;
        optionName = string.Empty;
        cost = string.Empty;
        swc = string.Empty;
        isLieutenant = null;

        if (string.IsNullOrWhiteSpace(profileKey))
        {
            return false;
        }

        var parts = profileKey.Split('|');
        if (parts.Length < 4)
        {
            return false;
        }

        groupName = parts[0].Trim();
        optionName = parts[1].Trim();
        cost = parts[2].Trim();
        swc = parts[3].Trim();

        if (parts.Length >= 5)
        {
            var ltPart = parts[4].Trim();
            if (ltPart.StartsWith("lt:", StringComparison.OrdinalIgnoreCase))
            {
                var ltValue = ltPart[3..].Trim();
                if (ltValue == "1")
                {
                    isLieutenant = true;
                }
                else if (ltValue == "0")
                {
                    isLieutenant = false;
                }
            }
        }

        return !string.IsNullOrWhiteSpace(groupName) &&
               !string.IsNullOrWhiteSpace(cost) &&
               !string.IsNullOrWhiteSpace(swc);
    }

    private static string BuildLegacyProfileKey(string profileKey)
    {
        if (string.IsNullOrWhiteSpace(profileKey))
        {
            return string.Empty;
        }

        var parts = profileKey.Split('|');
        if (parts.Length < 3)
        {
            return profileKey;
        }

        return $"{parts[0]}|{parts[1]}|{parts[2]}";
    }

}
