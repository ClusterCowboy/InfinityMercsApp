using System.Globalization;
using System.Text.Json;
using InfinityMercsApp.Domain.Models.Army;
using InfinityMercsApp.Infrastructure.Providers;
using InfinityMercsApp.Services;
using InfinityMercsApp.Views.Common.NewCompany;

namespace InfinityMercsApp.Views.Common.Captain;

internal static class CaptainPopupInputBuilder
{
    internal static int ResolveSourceFactionId(int fallbackSourceFactionId, int? firstSelectedSourceFactionId)
    {
        if (fallbackSourceFactionId > 0)
        {
            return fallbackSourceFactionId;
        }

        return firstSelectedSourceFactionId ?? fallbackSourceFactionId;
    }

    internal static int ResolveOptionFactionId(int sourceFactionId, int? sourceFactionParentId)
    {
        if (sourceFactionId <= 0)
        {
            return sourceFactionId;
        }

        return sourceFactionParentId.GetValueOrDefault() > 0
            ? sourceFactionParentId.GetValueOrDefault()
            : sourceFactionId;
    }

    internal static string ResolveOptionFactionName(
        int sourceFactionId,
        int optionFactionId,
        string? sourceFactionName,
        string? optionFactionName,
        string? metadataSourceFactionName,
        string? metadataOptionFactionName)
    {
        if (!string.IsNullOrWhiteSpace(sourceFactionName))
        {
            return sourceFactionName;
        }

        if (!string.IsNullOrWhiteSpace(optionFactionName))
        {
            return optionFactionName;
        }

        if (!string.IsNullOrWhiteSpace(metadataSourceFactionName))
        {
            return metadataSourceFactionName;
        }

        if (!string.IsNullOrWhiteSpace(metadataOptionFactionName))
        {
            return metadataOptionFactionName;
        }

        return optionFactionId > 0
            ? $"Faction {optionFactionId}"
            : sourceFactionId > 0
                ? $"Faction {sourceFactionId}"
                : "Faction";
    }

    internal static async Task<CaptainUpgradeOptionSet> LoadUpgradeOptionsAsync(
        IArmyDataService armyDataService,
        ISpecOpsProvider specOpsProvider,
        int factionId,
        bool showUnitsInInches,
        CancellationToken cancellationToken)
    {
        if (factionId <= 0)
        {
            return CaptainUpgradeOptionSet.Empty;
        }

        try
        {
            var snapshot = armyDataService.GetFactionSnapshot(factionId, cancellationToken);
            var filtersJson = snapshot?.FiltersJson;
            var skillLookup = CompanySelectionSharedUtilities.BuildIdNameLookup(filtersJson, "skills");
            var equipLookup = CompanySelectionSharedUtilities.BuildIdNameLookup(filtersJson, "equip");
            var weaponLookup = CompanySelectionSharedUtilities.BuildIdNameLookup(filtersJson, "weapons");
            var extrasLookup = BuildExtrasLookup(filtersJson);

            var skillRecords = await specOpsProvider.GetSpecopsSkillsByFactionAsync(factionId, cancellationToken);
            var equipRecords = await specOpsProvider.GetSpecopsEquipsByFactionAsync(factionId, cancellationToken);
            var weaponRecords = await specOpsProvider.GetSpecopsWeaponsByFactionAsync(factionId, cancellationToken);

            var skills = skillRecords
                .OrderBy(x => x.EntryOrder)
                .Select(x => ResolveSpecopsChoiceLabel(skillLookup, x.SkillId, x.Exp, "Skill", x.ExtrasJson, extrasLookup, showUnitsInInches))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var equipment = equipRecords
                .OrderBy(x => x.EntryOrder)
                .Select(x => ResolveSpecopsChoiceLabel(equipLookup, x.EquipmentId, x.Exp, "Equipment", x.ExtrasJson, extrasLookup, showUnitsInInches))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var weapons = weaponRecords
                .OrderBy(x => x.EntryOrder)
                .Select(x => ResolveSpecopsChoiceLabel(weaponLookup, x.WeaponId, x.Exp, "Weapon", null, extrasLookup, showUnitsInInches))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return new CaptainUpgradeOptionSet
            {
                Weapons = weapons,
                Skills = skills,
                Equipment = equipment
            };
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"CaptainPopupInputBuilder LoadUpgradeOptionsAsync failed for faction {factionId}: {ex.Message}");
            return CaptainUpgradeOptionSet.Empty;
        }
    }

    private static Dictionary<int, CaptainExtraDefinition> BuildExtrasLookup(string? filtersJson)
    {
        var map = new Dictionary<int, CaptainExtraDefinition>();
        if (string.IsNullOrWhiteSpace(filtersJson))
        {
            return map;
        }

        try
        {
            using var doc = JsonDocument.Parse(filtersJson);
            if (!doc.RootElement.TryGetProperty("extra", out var section) || section.ValueKind != JsonValueKind.Array)
            {
                return map;
            }

            foreach (var entry in section.EnumerateArray())
            {
                if (!TryParseCaptainExtraId(entry, out var id))
                {
                    continue;
                }

                if (!entry.TryGetProperty("name", out var nameElement) || nameElement.ValueKind != JsonValueKind.String)
                {
                    continue;
                }

                var name = nameElement.GetString();
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                var type = entry.TryGetProperty("type", out var typeElement) && typeElement.ValueKind == JsonValueKind.String
                    ? typeElement.GetString() ?? string.Empty
                    : string.Empty;

                map[id] = new CaptainExtraDefinition(name, type);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"CaptainPopupInputBuilder BuildExtrasLookup failed: {ex.Message}");
        }

        return map;
    }

    private static string ResolveSpecopsChoiceLabel(
        IReadOnlyDictionary<int, string> lookup,
        int id,
        int points,
        string label,
        string? extrasJson,
        IReadOnlyDictionary<int, CaptainExtraDefinition> extrasLookup,
        bool showUnitsInInches)
    {
        var prefix = $"({Math.Max(0, points)}) - ";
        var extrasSuffix = BuildExtrasSuffix(extrasJson, extrasLookup, showUnitsInInches);
        if (lookup.TryGetValue(id, out var value) && !string.IsNullOrWhiteSpace(value))
        {
            return $"{prefix}{value.Trim()}{extrasSuffix}";
        }

        return $"{prefix}{label} {id}{extrasSuffix}";
    }

    private static string BuildExtrasSuffix(
        string? extrasJson,
        IReadOnlyDictionary<int, CaptainExtraDefinition> extrasLookup,
        bool showUnitsInInches)
    {
        if (string.IsNullOrWhiteSpace(extrasJson))
        {
            return string.Empty;
        }

        try
        {
            using var doc = JsonDocument.Parse(extrasJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
            {
                return string.Empty;
            }

            var extras = new List<string>();
            foreach (var element in doc.RootElement.EnumerateArray())
            {
                if (!TryParseCaptainExtraId(element, out var parsedId))
                {
                    continue;
                }

                if (extrasLookup.TryGetValue(parsedId, out var resolved) && !string.IsNullOrWhiteSpace(resolved.Name))
                {
                    extras.Add(FormatExtraDisplay(resolved, showUnitsInInches));
                }
                else
                {
                    extras.Add(parsedId.ToString(CultureInfo.InvariantCulture));
                }
            }

            if (extras.Count == 0)
            {
                return string.Empty;
            }

            return $" ({string.Join(", ", extras.Distinct(StringComparer.OrdinalIgnoreCase))})";
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string FormatExtraDisplay(CaptainExtraDefinition definition, bool showUnitsInInches)
    {
        if (!string.Equals(definition.Type, "DISTANCE", StringComparison.OrdinalIgnoreCase))
        {
            return definition.Name;
        }

        return CompanySelectionSharedUtilities.ConvertDistanceText(definition.Name, showUnitsInInches);
    }

    private static bool TryParseCaptainExtraId(JsonElement element, out int id)
    {
        if (element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out var numberId))
        {
            id = numberId;
            return true;
        }

        if (element.ValueKind == JsonValueKind.String && int.TryParse(element.GetString(), out var stringId))
        {
            id = stringId;
            return true;
        }

        if (element.ValueKind == JsonValueKind.Object &&
            element.TryGetProperty("id", out var idElement))
        {
            return TryParseCaptainExtraId(idElement, out id);
        }

        id = 0;
        return false;
    }

    private sealed record CaptainExtraDefinition(string Name, string Type);
}

