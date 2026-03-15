using System.Text.Json;
using InfinityMercsApp.ViewModels;

namespace InfinityMercsApp.Views.StandardCompany;

internal sealed class StandardCompanyProfileBuildRequest
{
    public required JsonElement ProfileGroupsRoot { get; init; }
    public required bool ForceLieutenant { get; init; }
    public required bool ShowTacticalAwarenessIcon { get; init; }
    public required IReadOnlyDictionary<int, string> WeaponsLookup { get; init; }
    public required IReadOnlyDictionary<int, string> EquipLookup { get; init; }
    public required IReadOnlyDictionary<int, string> SkillsLookup { get; init; }
    public required IReadOnlyDictionary<int, string> PeripheralLookup { get; init; }
    public required Func<JsonElement, bool> IsControllerGroup { get; init; }
    public required Func<JsonElement, JsonElement, IReadOnlyList<JsonElement>> GetDisplayPeripheralEntriesForOption { get; init; }
    public required Func<IEnumerable<JsonElement>, IReadOnlyDictionary<int, string>, List<string>> GetOrderedDisplayNames { get; init; }
    public required Func<IEnumerable<JsonElement>, IReadOnlyDictionary<int, string>, List<string>> GetCountedDisplayNames { get; init; }
    public required Func<JsonElement, string> ReadOptionSwc { get; init; }
    public required Func<string, bool> IsPositiveSwc { get; init; }
    public required Func<string, bool> IsMeleeWeaponName { get; init; }
    public required Func<JsonElement, JsonElement, string> ReadAdjustedOptionCost { get; init; }
    public required Func<string, int> ParseCostValue { get; init; }
    public required Func<JsonElement, string> ReadOptionCost { get; init; }
    public required Func<string, JsonElement?> TryFindPeripheralProfile { get; init; }
    public required Func<string, JsonElement, PeripheralMercsCompanyStats?> BuildPeripheralStatBlock { get; init; }
    public required Func<string, int?> TryGetPeripheralUnitCost { get; init; }
    public required Func<IReadOnlyList<string>, (bool Success, string Name, int Count)> TryBuildSinglePeripheralDisplay { get; init; }
    public required Func<string?, string> ExtractFirstPeripheralName { get; init; }
    public required Func<string, string> NormalizePeripheralNameForDedupe { get; init; }
    public required Func<IEnumerable<string>, int> GetPeripheralTotalCount { get; init; }
    public required Func<JsonElement, bool> IsLieutenantOption { get; init; }
    public required Func<int?, int?, string> FormatMoveValue { get; init; }
    public required Func<PeripheralMercsCompanyStats?, string> BuildPeripheralSubtitle { get; init; }
}

internal sealed class StandardCompanyProfileCoordinator
{
    public IReadOnlyList<ViewerProfileItem> BuildProfiles(StandardCompanyProfileBuildRequest request)
    {
        if (request.ProfileGroupsRoot.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var profiles = new List<ViewerProfileItem>();
        var equipUsageCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var skillUsageCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var hasControllerGroups = request.ProfileGroupsRoot.EnumerateArray().Any(request.IsControllerGroup);

        foreach (var group in request.ProfileGroupsRoot.EnumerateArray())
        {
            if (hasControllerGroups && !request.IsControllerGroup(group))
            {
                continue;
            }

            if (!group.TryGetProperty("options", out var optionsElement) || optionsElement.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var option in optionsElement.EnumerateArray())
            {
                if (request.IsPositiveSwc(request.ReadOptionSwc(option)))
                {
                    continue;
                }

                foreach (var name in request.GetOrderedDisplayNames(
                             StandardCompanyProfileOptionService.GetOptionEntriesWithIncludes(request.ProfileGroupsRoot, option, "equip"),
                             request.EquipLookup))
                {
                    equipUsageCounts[name] = equipUsageCounts.TryGetValue(name, out var count) ? count + 1 : 1;
                }

                var optionSkillNames = StandardCompanyProfileTextService.BuildConfigurationSkillNames(
                    request.GetOrderedDisplayNames(
                        StandardCompanyProfileOptionService.GetOptionEntriesWithIncludes(request.ProfileGroupsRoot, option, "skills"),
                        request.SkillsLookup));
                foreach (var name in optionSkillNames)
                {
                    skillUsageCounts[name] = skillUsageCounts.TryGetValue(name, out var count) ? count + 1 : 1;
                }
            }
        }

        var bestConfigurationByKey = new Dictionary<string, (int Index, int PeripheralCount)>(StringComparer.OrdinalIgnoreCase);

        foreach (var group in request.ProfileGroupsRoot.EnumerateArray())
        {
            if (hasControllerGroups && !request.IsControllerGroup(group))
            {
                continue;
            }

            var groupName = group.TryGetProperty("isc", out var iscElement) && iscElement.ValueKind == JsonValueKind.String
                ? iscElement.GetString() ?? string.Empty
                : string.Empty;

            if (!group.TryGetProperty("options", out var optionsElement) || optionsElement.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var option in optionsElement.EnumerateArray())
            {
                var swc = request.ReadOptionSwc(option);
                if (request.IsPositiveSwc(swc))
                {
                    continue;
                }

                var optionName = option.TryGetProperty("name", out var nameElement) && nameElement.ValueKind == JsonValueKind.String
                    ? nameElement.GetString() ?? string.Empty
                    : string.Empty;
                if (string.IsNullOrWhiteSpace(optionName))
                {
                    optionName = groupName;
                }

                optionName = BuildOptionDisplayName(option, optionName, request.EquipLookup, request.SkillsLookup);

                var optionWeapons = request.GetOrderedDisplayNames(
                    StandardCompanyProfileOptionService.GetOptionEntriesWithIncludes(request.ProfileGroupsRoot, option, "weapons"),
                    request.WeaponsLookup);
                var rangedWeaponNames = optionWeapons.Where(x => !request.IsMeleeWeaponName(x)).ToList();
                var meleeWeaponNames = optionWeapons.Where(request.IsMeleeWeaponName).ToList();

                var optionEquipmentNames = request.GetOrderedDisplayNames(
                        StandardCompanyProfileOptionService.GetOptionEntriesWithIncludes(request.ProfileGroupsRoot, option, "equip"),
                        request.EquipLookup)
                    .ToList();
                var uniqueEquipmentNames = optionEquipmentNames
                    .Where(x => equipUsageCounts.TryGetValue(x, out var c) && c == 1)
                    .ToList();
                if (uniqueEquipmentNames.Count == 0)
                {
                    uniqueEquipmentNames = optionEquipmentNames
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                        .ToList();
                }

                var optionSkillNames = StandardCompanyProfileTextService.BuildConfigurationSkillNames(
                    request.GetOrderedDisplayNames(
                        StandardCompanyProfileOptionService.GetOptionEntriesWithIncludes(request.ProfileGroupsRoot, option, "skills"),
                        request.SkillsLookup));
                var uniqueSkillsNames = optionSkillNames
                    .Where(x => skillUsageCounts.TryGetValue(x, out var c) && c == 1)
                    .ToList();
                if (uniqueSkillsNames.Count == 0)
                {
                    uniqueSkillsNames = optionSkillNames
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                        .ToList();
                }

                var peripheralNames = request.GetCountedDisplayNames(
                    request.GetDisplayPeripheralEntriesForOption(group, option),
                    request.PeripheralLookup);
                var firstPeripheralName = peripheralNames.FirstOrDefault();

                PeripheralMercsCompanyStats? peripheralStats = null;
                JsonElement peripheralProfile = default;
                var hasPeripheralProfile = false;

                if (!string.IsNullOrWhiteSpace(firstPeripheralName))
                {
                    var extractedPeripheralName = request.ExtractFirstPeripheralName(firstPeripheralName);
                    var foundPeripheralProfile = request.TryFindPeripheralProfile(extractedPeripheralName);
                    if (foundPeripheralProfile.HasValue)
                    {
                        hasPeripheralProfile = true;
                        peripheralProfile = foundPeripheralProfile.Value;
                        peripheralStats = request.BuildPeripheralStatBlock(extractedPeripheralName, peripheralProfile);
                    }
                }

                var cost = request.ReadAdjustedOptionCost(group, option);
                var displayPeripheralNames = peripheralNames;
                var displayCost = cost;

                var singlePeripheral = request.TryBuildSinglePeripheralDisplay(peripheralNames);
                if (singlePeripheral.Success && singlePeripheral.Count > 1)
                {
                    displayPeripheralNames = [$"{singlePeripheral.Name} (1)"];

                    if (hasPeripheralProfile)
                    {
                        var peripheralCost = request.TryGetPeripheralUnitCost(singlePeripheral.Name)
                            ?? request.ParseCostValue(request.ReadOptionCost(peripheralProfile));
                        var baseCost = request.ParseCostValue(cost);
                        if (peripheralCost > 0 && baseCost > 0)
                        {
                            var removedPeripheralCount = singlePeripheral.Count - 1;
                            displayCost = Math.Max(0, baseCost - (removedPeripheralCount * peripheralCost))
                                .ToString(System.Globalization.CultureInfo.InvariantCulture);
                        }
                    }
                }

                var normalizedPeripheralNames = peripheralNames
                    .Select(request.NormalizePeripheralNameForDedupe)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .ToList();
                var peripheralCount = request.GetPeripheralTotalCount(peripheralNames);
                var dedupeKey =
                    $"{groupName}|{optionName}|{string.Join("|", rangedWeaponNames)}|{string.Join("|", meleeWeaponNames)}|{string.Join("|", uniqueEquipmentNames)}|{string.Join("|", uniqueSkillsNames)}|{string.Join("|", normalizedPeripheralNames)}|{swc}";
                var hasExisting = bestConfigurationByKey.TryGetValue(dedupeKey, out var existingConfiguration);
                if (hasExisting && peripheralCount >= existingConfiguration.PeripheralCount)
                {
                    continue;
                }

                var isLieutenant = request.ForceLieutenant || request.IsLieutenantOption(option);
                var profileKey = $"{groupName}|{optionName}|{displayCost}|{swc}|lt:{(isLieutenant ? 1 : 0)}";
                var profileItem = new ViewerProfileItem
                {
                    GroupName = groupName,
                    Name = optionName,
                    ProfileKey = profileKey,
                    IsLieutenant = isLieutenant,
                    NameFormatted = StandardCompanyProfileTextService.BuildNameFormatted(optionName),
                    RangedWeapons = StandardCompanyProfileTextService.JoinOrDash(rangedWeaponNames),
                    RangedWeaponsFormatted = StandardCompanyProfileTextService.BuildListFormattedString(rangedWeaponNames, Color.FromArgb("#EF4444")),
                    MeleeWeapons = StandardCompanyProfileTextService.JoinOrDash(meleeWeaponNames),
                    MeleeWeaponsFormatted = StandardCompanyProfileTextService.BuildListFormattedString(meleeWeaponNames, Color.FromArgb("#22C55E")),
                    UniqueEquipment = StandardCompanyProfileTextService.JoinOrDash(uniqueEquipmentNames),
                    UniqueEquipmentFormatted = StandardCompanyProfileTextService.BuildListFormattedString(uniqueEquipmentNames, Color.FromArgb("#06B6D4")),
                    UniqueSkills = StandardCompanyProfileTextService.JoinOrDash(uniqueSkillsNames),
                    UniqueSkillsFormatted = StandardCompanyProfileTextService.BuildListFormattedString(
                        uniqueSkillsNames,
                        Color.FromArgb("#F59E0B"),
                        highlightLieutenantPurple: request.ForceLieutenant),
                    Peripherals = StandardCompanyProfileTextService.JoinOrDash(displayPeripheralNames),
                    PeripheralsFormatted = StandardCompanyProfileTextService.BuildListFormattedString(displayPeripheralNames, Color.FromArgb("#FACC15")),
                    HasPeripheralStatBlock = peripheralStats is not null,
                    PeripheralNameHeading = peripheralStats?.NameHeading ?? string.Empty,
                    PeripheralMov = peripheralStats is null ? "-" : request.FormatMoveValue(peripheralStats.MoveFirstCm, peripheralStats.MoveSecondCm),
                    PeripheralCc = peripheralStats?.Cc ?? "-",
                    PeripheralBs = peripheralStats?.Bs ?? "-",
                    PeripheralPh = peripheralStats?.Ph ?? "-",
                    PeripheralWip = peripheralStats?.Wip ?? "-",
                    PeripheralArm = peripheralStats?.Arm ?? "-",
                    PeripheralBts = peripheralStats?.Bts ?? "-",
                    PeripheralVitalityHeader = peripheralStats?.VitalityHeader ?? "VITA",
                    PeripheralVitality = peripheralStats?.Vitality ?? "-",
                    PeripheralS = peripheralStats?.S ?? "-",
                    PeripheralAva = peripheralStats?.Ava ?? "-",
                    PeripheralSubtitle = request.BuildPeripheralSubtitle(peripheralStats),
                    PeripheralEquipmentLineFormatted = StandardCompanyProfileTextService.BuildMercsCompanyLineFormatted("Equipment", peripheralStats?.Equipment, Color.FromArgb("#06B6D4")),
                    HasPeripheralEquipmentLine = peripheralStats is not null && !string.IsNullOrWhiteSpace(peripheralStats.Equipment) && peripheralStats.Equipment != "-",
                    PeripheralSkillsLineFormatted = StandardCompanyProfileTextService.BuildMercsCompanyLineFormatted("Skills", peripheralStats?.Skills, Color.FromArgb("#F59E0B")),
                    HasPeripheralSkillsLine = peripheralStats is not null && !string.IsNullOrWhiteSpace(peripheralStats.Skills) && peripheralStats.Skills != "-",
                    Swc = swc,
                    SwcDisplay = $"SWC {swc}",
                    Cost = displayCost,
                    ShowProfileTacticalAwarenessIcon = !request.ShowTacticalAwarenessIcon &&
                                                       optionSkillNames.Any(x => x.Contains("tactical awareness", StringComparison.OrdinalIgnoreCase))
                };

                if (hasExisting)
                {
                    profiles[existingConfiguration.Index] = profileItem;
                    bestConfigurationByKey[dedupeKey] = (existingConfiguration.Index, peripheralCount);
                    continue;
                }

                profiles.Add(profileItem);
                bestConfigurationByKey[dedupeKey] = (profiles.Count - 1, peripheralCount);
            }
        }

        return profiles;
    }

    private static string BuildOptionDisplayName(
        JsonElement option,
        string baseName,
        IReadOnlyDictionary<int, string> equipLookup,
        IReadOnlyDictionary<int, string> skillsLookup)
    {
        var details = new List<string>();
        var normalizedBase = baseName.ToLowerInvariant();

        foreach (var skillName in GetOrderedNames(option, "skills", skillsLookup))
        {
            if (IsNameDetailTag(skillName) && !normalizedBase.Contains(skillName.ToLowerInvariant()))
            {
                details.Add(skillName);
            }
        }

        foreach (var equipName in GetOrderedNames(option, "equip", equipLookup))
        {
            if (IsNameDetailTag(equipName) && !normalizedBase.Contains(equipName.ToLowerInvariant()))
            {
                details.Add(equipName);
            }
        }

        if (option.TryGetProperty("orders", out var ordersElement) && ordersElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var order in ordersElement.EnumerateArray())
            {
                if (!order.TryGetProperty("type", out var typeElement) || typeElement.ValueKind != JsonValueKind.String)
                {
                    continue;
                }

                var type = typeElement.GetString();
                if (string.Equals(type, "LIEUTENANT", StringComparison.OrdinalIgnoreCase) &&
                    !normalizedBase.Contains("lieutenant"))
                {
                    details.Add("Lieutenant");
                }
            }
        }

        var distinctDetails = details
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (distinctDetails.Count == 0)
        {
            return baseName;
        }

        return $"{baseName} ({string.Join(", ", distinctDetails)})";
    }

    private static List<string> GetOrderedNames(
        JsonElement container,
        string propertyName,
        IReadOnlyDictionary<int, string> lookup)
    {
        if (!container.TryGetProperty(propertyName, out var arr) || arr.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var names = new List<string>();
        foreach (var entry in arr.EnumerateArray())
        {
            if (!TryParseId(entry, out var id))
            {
                continue;
            }

            if (lookup.TryGetValue(id, out var resolvedName) && !string.IsNullOrWhiteSpace(resolvedName))
            {
                names.Add(resolvedName);
            }
        }

        return names
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static bool IsNameDetailTag(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return value.Contains("forward observer", StringComparison.OrdinalIgnoreCase) ||
               value.Contains("hacker", StringComparison.OrdinalIgnoreCase) ||
               value.Contains("hacking device", StringComparison.OrdinalIgnoreCase) ||
               value.Contains("specialist", StringComparison.OrdinalIgnoreCase) ||
               value.Contains("paramedic", StringComparison.OrdinalIgnoreCase) ||
               value.Contains("doctor", StringComparison.OrdinalIgnoreCase) ||
               value.Contains("engineer", StringComparison.OrdinalIgnoreCase) ||
               value.Contains("lieutenant", StringComparison.OrdinalIgnoreCase) ||
               value.Contains("nco", StringComparison.OrdinalIgnoreCase) ||
               value.Contains("chain of command", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryParseId(JsonElement element, out int id)
    {
        if (element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out var numberId))
        {
            id = numberId;
            return true;
        }

        if (element.ValueKind == JsonValueKind.String &&
            int.TryParse(element.GetString(), out var stringId))
        {
            id = stringId;
            return true;
        }

        if (element.ValueKind == JsonValueKind.Object &&
            element.TryGetProperty("id", out var idElement))
        {
            return TryParseId(idElement, out id);
        }

        id = 0;
        return false;
    }
}
