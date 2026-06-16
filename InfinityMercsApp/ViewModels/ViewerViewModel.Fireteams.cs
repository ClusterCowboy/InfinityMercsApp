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
    private void ResetFireteamCounts()
    {
        FireteamDuoCount = "-";
        FireteamHarisCount = "-";
        FireteamCoreCount = "-";
        Fireteams.Clear();
        FireteamsStatus = "No fireteams available.";
    }

    private void UpdateFireteamCounts(string? fireteamChartJson)
    {
        FireteamDuoCount = "-";
        FireteamHarisCount = "-";
        FireteamCoreCount = "-";
        if (string.IsNullOrWhiteSpace(fireteamChartJson))
        {
            return;
        }

        try
        {
            using var doc = JsonDocument.Parse(fireteamChartJson);
            if (!doc.RootElement.TryGetProperty("spec", out var specElement) ||
                specElement.ValueKind != JsonValueKind.Object)
            {
                return;
            }

            FireteamDuoCount = ReadFireteamCount(specElement, "DUO");
            FireteamHarisCount = ReadFireteamCount(specElement, "HARIS");
            FireteamCoreCount = ReadFireteamCount(specElement, "CORE");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"UpdateFireteamCounts failed: {ex.Message}");
        }
    }

    private void UpdateFireteamTeams(
        string? fireteamChartJson,
        bool mercsOnlyFilterEnabled,
        IReadOnlySet<string>? allowedUnitSlugs = null,
        IReadOnlySet<string>? allowedUnitNames = null)
    {
        Fireteams.Clear();
        FireteamsStatus = "No fireteams available.";

        if (string.IsNullOrWhiteSpace(fireteamChartJson))
        {
            return;
        }

        try
        {
            using var doc = JsonDocument.Parse(fireteamChartJson);
            if (!doc.RootElement.TryGetProperty("teams", out var teamsElement) ||
                teamsElement.ValueKind != JsonValueKind.Array)
            {
                return;
            }

            foreach (var teamElement in teamsElement.EnumerateArray())
            {
                var teamName = ReadString(teamElement, "name", "Unnamed Team");
                var teamTypes = ReadTeamTypes(teamElement);
                var unitLimits = ReadUnitLimits(teamElement);
                if (mercsOnlyFilterEnabled)
                {
                    unitLimits = unitLimits
                        .Where(x => IsAllowedFireteamUnit(x, allowedUnitSlugs, allowedUnitNames))
                        .ToList();
                    if (unitLimits.Count == 0)
                    {
                        continue;
                    }
                }

                Fireteams.Add(new FireteamTeamItem
                {
                    Name = teamName,
                    TeamTypes = string.IsNullOrWhiteSpace(teamTypes) ? "-" : teamTypes,
                    UnitLimits = unitLimits
                });
            }

            FireteamsStatus = Fireteams.Count == 0
                ? "No fireteams available."
                : $"{Fireteams.Count} fireteams loaded.";
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"UpdateFireteamTeams failed: {ex.Message}");
            FireteamsStatus = $"Failed to parse fireteams: {ex.Message}";
        }
    }

    private static List<FireteamUnitLimitItem> ReadUnitLimits(JsonElement teamElement)
    {
        var results = new List<FireteamUnitLimitItem>();
        if (!teamElement.TryGetProperty("units", out var unitsElement) || unitsElement.ValueKind != JsonValueKind.Array)
        {
            return results;
        }

        foreach (var unitElement in unitsElement.EnumerateArray())
        {
            var unitName = ReadString(unitElement, "name", string.Empty);
            if (string.IsNullOrWhiteSpace(unitName))
            {
                unitName = ReadString(unitElement, "slug", "Unknown");
            }
            var unitSlug = ReadString(unitElement, "slug", string.Empty);

            var min = TryReadIntProperty(unitElement, "min", out var minValue) ? minValue : 0;
            var max = TryReadIntProperty(unitElement, "max", out var maxValue) ? maxValue : 0;

            results.Add(new FireteamUnitLimitItem
            {
                Name = unitName,
                Slug = unitSlug,
                Min = min.ToString(CultureInfo.InvariantCulture),
                Max = max.ToString(CultureInfo.InvariantCulture)
            });
        }

        return results;
    }

    private static string ReadTeamTypes(JsonElement teamElement)
    {
        if (!teamElement.TryGetProperty("type", out var typeElement))
        {
            return string.Empty;
        }

        if (typeElement.ValueKind == JsonValueKind.Array)
        {
            var types = typeElement.EnumerateArray()
                .Where(x => x.ValueKind == JsonValueKind.String)
                .Select(x => x.GetString())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x!.Trim());

            return string.Join(", ", types);
        }

        if (typeElement.ValueKind == JsonValueKind.String)
        {
            return typeElement.GetString() ?? string.Empty;
        }

        return string.Empty;
    }

    private static bool TryReadIntProperty(JsonElement element, string propertyName, out int value)
    {
        if (!element.TryGetProperty(propertyName, out var propertyValue))
        {
            value = 0;
            return false;
        }

        return TryReadInt(propertyValue, out value);
    }

    private static string ReadString(JsonElement element, string propertyName, string fallback)
    {
        if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.String)
        {
            return fallback;
        }

        return property.GetString() ?? fallback;
    }

    private static bool IsAllowedFireteamUnit(
        FireteamUnitLimitItem unit,
        IReadOnlySet<string>? allowedUnitSlugs,
        IReadOnlySet<string>? allowedUnitNames)
    {
        if (!string.IsNullOrWhiteSpace(unit.Slug) &&
            allowedUnitSlugs is not null &&
            allowedUnitSlugs.Contains(unit.Slug))
        {
            return true;
        }

        if (allowedUnitNames is not null &&
            allowedUnitNames.Contains(NormalizeFireteamUnitName(unit.Name)))
        {
            return true;
        }

        return false;
    }

    private static string NormalizeFireteamUnitName(string value)
    {
        return Regex.Replace(value, @"\s+", " ").Trim().ToUpperInvariant();
    }

    private static string ReadFireteamCount(JsonElement specElement, string key)
    {
        foreach (var property in specElement.EnumerateObject())
        {
            if (!string.Equals(property.Name, key, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (TryReadInt(property.Value, out var rawValue))
            {
                return rawValue > 5 ? "T" : rawValue.ToString(CultureInfo.InvariantCulture);
            }

            return "-";
        }

        return "-";
    }

    private static bool TryReadInt(JsonElement element, out int value)
    {
        if (element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out value))
        {
            return true;
        }

        if (element.ValueKind == JsonValueKind.String &&
            int.TryParse(element.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
        {
            return true;
        }

        value = 0;
        return false;
    }

    private void ApplyFactionFilter()
    {
        IEnumerable<ViewerFactionItem> filtered = _allFactions;
        if (_factionFilterMode == FactionFilterMode.Factions)
        {
            filtered = filtered.Where(x => x.ParentId <= 0 || x.Id == x.ParentId);
        }
        else if (_factionFilterMode == FactionFilterMode.Sectorials)
        {
            filtered = filtered.Where(x => x.ParentId > 0 && x.Id != x.ParentId);
        }

        var filteredList = filtered.ToList();

        Factions.Clear();
        foreach (var faction in filteredList)
        {
            Factions.Add(faction);
        }

        if (SelectedFaction is not null && !filteredList.Contains(SelectedFaction))
        {
            SelectedFaction = null;
            Units.Clear();
            UnitsStatus = "Select a faction.";
        }
    }

    private void ResetUnitDetails()
    {
        Profiles.Clear();
        ProfilesStatus = "Select a unit.";
        UnitNameHeading = "Select a unit";
        ResetUnitStats();
        EquipmentSummary = "Equipment: -";
        SpecialSkillsSummary = "Special Skills: -";
        _currentEquipmentLookup = new Dictionary<int, string>();
        _currentEquipmentLinks = new Dictionary<int, string>();
        _currentSkillsLookup = new Dictionary<int, string>();
        _currentSkillsLinks = new Dictionary<int, string>();
    }

    private void ResetUnitStats()
    {
        _unitMoveFirstCm = null;
        _unitMoveSecondCm = null;
        ShowRegularOrderIcon = false;
        ShowIrregularOrderIcon = false;
        ShowImpetuousIcon = false;
        ShowTacticalAwarenessIcon = false;
        ShowCubeIcon = false;
        ShowCube2Icon = false;
        ShowHackableIcon = false;
        ImpetuousIconUrl = null;
        TacticalAwarenessIconUrl = null;
        CubeIconUrl = null;
        Cube2IconUrl = null;
        HackableIconUrl = null;
        UnitMov = "-";
        UnitCc = "-";
        UnitBs = "-";
        UnitPh = "-";
        UnitWip = "-";
        UnitArm = "-";
        UnitBts = "-";
        UnitVitalityHeader = "VITA";
        UnitVitality = "-";
        UnitS = "-";
        UnitAva = "-";
    }

    private static string BuildUnitSubtitle(
        Resume unit,
        IReadOnlyDictionary<int, string> typeLookup,
        IReadOnlyDictionary<int, string> categoryLookup)
    {
        var typeName = unit.Type.HasValue && typeLookup.TryGetValue(unit.Type.Value, out var t)
            ? t
            : (unit.Type?.ToString() ?? "?");

        var categoryName = unit.Category.HasValue && categoryLookup.TryGetValue(unit.Category.Value, out var c)
            ? c
            : (unit.Category?.ToString() ?? "?");

        return $"{typeName} - {categoryName}";
    }

    private static Dictionary<int, string> BuildIdNameLookup(string? filtersJson, string sectionName)
    {
        var map = new Dictionary<int, string>();
        if (string.IsNullOrWhiteSpace(filtersJson))
        {
            return map;
        }

        try
        {
            using var doc = JsonDocument.Parse(filtersJson);
            if (!doc.RootElement.TryGetProperty(sectionName, out var section) || section.ValueKind != JsonValueKind.Array)
            {
                return map;
            }

            foreach (var entry in section.EnumerateArray())
            {
                if (!entry.TryGetProperty("id", out var idElement))
                {
                    continue;
                }

                int id;
                if (idElement.ValueKind == JsonValueKind.Number && idElement.TryGetInt32(out var intId))
                {
                    id = intId;
                }
                else if (idElement.ValueKind == JsonValueKind.String && int.TryParse(idElement.GetString(), out var stringId))
                {
                    id = stringId;
                }
                else
                {
                    continue;
                }

                if (!entry.TryGetProperty("name", out var nameElement) || nameElement.ValueKind != JsonValueKind.String)
                {
                    continue;
                }

                var name = nameElement.GetString();
                if (!string.IsNullOrWhiteSpace(name))
                {
                    map[id] = name;
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"BuildIdNameLookup failed for section '{sectionName}': {ex.Message}");
        }

        return map;
    }

    private static void CollectIdsFromArrayProperty(JsonElement container, string propertyName, HashSet<int> target)
    {
        if (!container.TryGetProperty(propertyName, out var arr) || arr.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var entry in arr.EnumerateArray())
        {
            if (TryParseId(entry, out var id))
            {
                target.Add(id);
            }
        }
    }

    private static HashSet<int> CollectIdsFromArrayProperty(JsonElement container, string propertyName)
    {
        var ids = new HashSet<int>();
        CollectIdsFromArrayProperty(container, propertyName, ids);
        return ids;
    }

    private static bool TryParseId(JsonElement element, out int id)
    {
        id = 0;
        if (element.ValueKind == JsonValueKind.Number)
        {
            return element.TryGetInt32(out id);
        }

        if (element.ValueKind == JsonValueKind.String)
        {
            return int.TryParse(element.GetString(), out id);
        }

        if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty("id", out var idElement))
        {
            return TryParseId(idElement, out id);
        }

        return false;
    }

}
