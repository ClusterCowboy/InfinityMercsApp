using System.Globalization;
using System.Text.Json;

namespace InfinityMercsApp.Views.StandardCompany;

/// <summary>
/// Captain configuration workflow and option resolution.
/// </summary>
public partial class StandardCompanySelectionPage
{
    /// <summary>
    /// Builds captain upgrade context and opens the captain configuration popup.
    /// </summary>
    private async Task<SavedImprovedCaptainStats?> ShowCaptainConfigurationAsync(MercsCompanyEntry captainEntry, CancellationToken cancellationToken = default)
    {
        var sourceFactionId = ResolveCaptainSourceFactionIdForPopup(captainEntry.SourceFactionId);
        var optionFactionId = ResolveCaptainOptionFactionIdForPopup(sourceFactionId);
        var options = await LoadCaptainUpgradeOptionsAsync(optionFactionId, cancellationToken);

        if (options.IsEmpty && optionFactionId != sourceFactionId)
        {
            options = await LoadCaptainUpgradeOptionsAsync(sourceFactionId, cancellationToken);
            optionFactionId = sourceFactionId;
        }

        var unitInfo = new CaptainUnitPopupInfo
        {
            Name = captainEntry.Name,
            Cost = captainEntry.CostValue,
            Statline = captainEntry.Subtitle ?? "-",
            RangedWeapons = captainEntry.SavedRangedWeapons,
            CcWeapons = captainEntry.SavedCcWeapons,
            Skills = captainEntry.SavedSkills,
            Equipment = captainEntry.SavedEquipment,
            CachedLogoPath = captainEntry.CachedLogoPath,
            PackagedLogoPath = captainEntry.PackagedLogoPath
        };

        var context = new CaptainUpgradePopupContext
        {
            Unit = unitInfo,
            OptionFactionId = optionFactionId,
            OptionFactionName = await ResolveCaptainOptionFactionNameForPopupAsync(sourceFactionId, optionFactionId, cancellationToken),
            WeaponOptions = options.Weapons,
            SkillOptions = options.Skills,
            EquipmentOptions = options.Equipment
        };

        return await ConfigureCaptainPopupPage.ShowAsync(Navigation, context);
    }

    /// <summary>
    /// Resolves the captain's source faction id, falling back to the first selected source faction.
    /// </summary>
    private int ResolveCaptainSourceFactionIdForPopup(int fallbackSourceFactionId)
    {
        if (fallbackSourceFactionId > 0)
        {
            return fallbackSourceFactionId;
        }

        var firstSource = GetUnitSourceFactions().FirstOrDefault();
        return firstSource?.Id ?? fallbackSourceFactionId;
    }

    /// <summary>
    /// Resolves the faction used to query captain upgrade options.
    /// </summary>
    private int ResolveCaptainOptionFactionIdForPopup(int sourceFactionId)
    {
        if (sourceFactionId <= 0)
        {
            return sourceFactionId;
        }

        var sourceFaction = Factions.FirstOrDefault(x => x.Id == sourceFactionId);
        if (sourceFaction is null)
        {
            return sourceFactionId;
        }

        return sourceFaction.ParentId > 0 ? sourceFaction.ParentId : sourceFactionId;
    }

    /// <summary>
    /// Resolves a display name for the faction shown in the captain options popup.
    /// </summary>
    private Task<string> ResolveCaptainOptionFactionNameForPopupAsync(
        int sourceFactionId,
        int optionFactionId,
        CancellationToken cancellationToken)
    {
        if (sourceFactionId > 0)
        {
            var sourceName = Factions.FirstOrDefault(x => x.Id == sourceFactionId)?.Name;
            if (!string.IsNullOrWhiteSpace(sourceName))
            {
                return Task.FromResult(sourceName);
            }
        }

        if (optionFactionId > 0)
        {
            var optionName = Factions.FirstOrDefault(x => x.Id == optionFactionId)?.Name;
            if (!string.IsNullOrWhiteSpace(optionName))
            {
                return Task.FromResult(optionName);
            }
        }

        if (sourceFactionId > 0)
        {
            var sourceFaction = _armyDataService.GetMetadataFactionById(sourceFactionId);
            if (!string.IsNullOrWhiteSpace(sourceFaction?.Name))
            {
                return Task.FromResult(sourceFaction.Name);
            }
        }

        if (optionFactionId > 0)
        {
            var optionFaction = _armyDataService.GetMetadataFactionById(optionFactionId);
            if (!string.IsNullOrWhiteSpace(optionFaction?.Name))
            {
                return Task.FromResult(optionFaction.Name);
            }
        }

        var resolved = optionFactionId > 0
            ? $"Faction {optionFactionId}"
            : sourceFactionId > 0
                ? $"Faction {sourceFactionId}"
                : "Faction";
        return Task.FromResult(resolved);
    }

    /// <summary>
    /// Loads captain upgrade options for the provided faction from spec-ops metadata.
    /// </summary>
    private async Task<CaptainUpgradeOptionSet> LoadCaptainUpgradeOptionsAsync(int factionId, CancellationToken cancellationToken)
    {
        if (factionId <= 0)
        {
            return CaptainUpgradeOptionSet.Empty;
        }

        try
        {
            var snapshot = _armyDataService.GetFactionSnapshot(factionId, cancellationToken);
            var skillLookup = BuildIdNameLookup(snapshot?.FiltersJson, "skills");
            var equipLookup = BuildIdNameLookup(snapshot?.FiltersJson, "equip");
            var weaponLookup = BuildIdNameLookup(snapshot?.FiltersJson, "weapons");
            var extrasLookup = BuildExtrasLookup(snapshot?.FiltersJson);

            var skillRecords = await _specOpsProvider.GetSpecopsSkillsByFactionAsync(factionId, cancellationToken);
            var equipRecords = await _specOpsProvider.GetSpecopsEquipsByFactionAsync(factionId, cancellationToken);
            var weaponRecords = await _specOpsProvider.GetSpecopsWeaponsByFactionAsync(factionId, cancellationToken);

            var skills = skillRecords
                .OrderBy(x => x.EntryOrder)
                .Select(x => ResolveCaptainSpecopsChoiceLabel(skillLookup, x.SkillId, x.Exp, "Skill", x.ExtrasJson, extrasLookup, ShowUnitsInInches))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var equipment = equipRecords
                .OrderBy(x => x.EntryOrder)
                .Select(x => ResolveCaptainSpecopsChoiceLabel(equipLookup, x.EquipmentId, x.Exp, "Equipment", x.ExtrasJson, extrasLookup, ShowUnitsInInches))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var weapons = weaponRecords
                .OrderBy(x => x.EntryOrder)
                .Select(x => ResolveCaptainSpecopsChoiceLabel(weaponLookup, x.WeaponId, x.Exp, "Weapon", null, extrasLookup, ShowUnitsInInches))
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
            Console.Error.WriteLine($"ArmyFactionSelectionPage LoadCaptainUpgradeOptionsAsync failed for faction {factionId}: {ex.Message}");
            return CaptainUpgradeOptionSet.Empty;
        }
    }

    /// <summary>
    /// Builds a display label for a captain spec-ops choice and includes extras when present.
    /// </summary>
    private static string ResolveCaptainSpecopsChoiceLabel(
        IReadOnlyDictionary<int, string> lookup,
        int id,
        int points,
        string label,
        string? extrasJson,
        IReadOnlyDictionary<int, ExtraDefinition> extrasLookup,
        bool showUnitsInInches)
    {
        var prefix = $"({Math.Max(0, points)}) - ";
        var extrasSuffix = BuildCaptainExtrasSuffix(extrasJson, extrasLookup, showUnitsInInches);
        if (lookup.TryGetValue(id, out var value) && !string.IsNullOrWhiteSpace(value))
        {
            return $"{prefix}{value.Trim()}{extrasSuffix}";
        }

        return $"{prefix}{label} {id}{extrasSuffix}";
    }

    /// <summary>
    /// Builds the optional extras suffix for a captain option display label.
    /// </summary>
    private static string BuildCaptainExtrasSuffix(
        string? extrasJson,
        IReadOnlyDictionary<int, ExtraDefinition> extrasLookup,
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
                var parsedId = TryParseCaptainExtraId(element);
                if (!parsedId.HasValue)
                {
                    continue;
                }

                if (extrasLookup.TryGetValue(parsedId.Value, out var resolved) && !string.IsNullOrWhiteSpace(resolved.Name))
                {
                    extras.Add(FormatExtraDisplay(resolved, showUnitsInInches));
                }
                else
                {
                    extras.Add(parsedId.Value.ToString(CultureInfo.InvariantCulture));
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

    /// <summary>
    /// Attempts to parse a spec-ops extra id from number, string, or object payload shapes.
    /// </summary>
    private static int? TryParseCaptainExtraId(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out var numberId))
        {
            return numberId;
        }

        if (element.ValueKind == JsonValueKind.String && int.TryParse(element.GetString(), out var stringId))
        {
            return stringId;
        }

        if (element.ValueKind == JsonValueKind.Object &&
            element.TryGetProperty("id", out var idElement))
        {
            if (idElement.ValueKind == JsonValueKind.Number && idElement.TryGetInt32(out var objectNumberId))
            {
                return objectNumberId;
            }

            if (idElement.ValueKind == JsonValueKind.String && int.TryParse(idElement.GetString(), out var objectStringId))
            {
                return objectStringId;
            }
        }

        return null;
    }
}
