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
    public async Task LoadProfilesForSelectedUnitAsync(CancellationToken cancellationToken = default)
    {
        await ApplyGlobalDisplayUnitsPreferenceAsync(cancellationToken);

        Profiles.Clear();
        UnitNameHeading = SelectedUnit?.Name ?? "Select a unit";
        EquipmentSummary = "Equipment: -";
        SpecialSkillsSummary = "Special Skills: -";
        EquipmentSummaryFormatted = BuildNamedSummaryFormatted(
            "Equipment",
            [],
            equipLookup: null,
            links: null,
            Color.FromArgb("#06B6D4"));
        SpecialSkillsSummaryFormatted = BuildNamedSummaryFormatted(
            "Special Skills",
            [],
            equipLookup: null,
            links: null,
            Color.FromArgb("#F59E0B"));

        if (SelectedFaction is null || SelectedUnit is null)
        {
            ProfilesStatus = "Select a unit.";
            return;
        }

        if (_armyDataService is null)
        {
            ProfilesStatus = "Army data service unavailable.";
            return;
        }

        try
        {
            ProfilesStatus = "Loading profiles...";
            var unit = _armyDataService.GetUnit(SelectedFaction.Id, SelectedUnit.Id, cancellationToken);
            var snapshot = _armyDataService.GetFactionSnapshot(SelectedFaction.Id, cancellationToken);
            var equipLookup = BuildIdNameLookup(snapshot?.FiltersJson, "equip");
            var equipLinks = BuildIdLinkLookup(snapshot?.FiltersJson, "equip");
            var skillsLookup = BuildIdNameLookup(snapshot?.FiltersJson, "skills");
            var skillsLinks = BuildIdLinkLookup(snapshot?.FiltersJson, "skills");
            var charsLookup = BuildIdNameLookup(snapshot?.FiltersJson, "chars");
            var charsLinks = BuildIdLinkLookup(snapshot?.FiltersJson, "chars");
            var weaponsLookup = BuildIdNameLookup(snapshot?.FiltersJson, "weapons");
            var weaponsLinks = BuildIdLinkLookup(snapshot?.FiltersJson, "weapons");
            var extrasLookup = BuildExtrasLookup(snapshot?.FiltersJson);
            var peripheralLookup = BuildIdNameLookup(snapshot?.FiltersJson, "peripheral");
            var peripheralLinks = BuildIdLinkLookup(snapshot?.FiltersJson, "peripheral");
            _currentEquipmentLookup = equipLookup;
            _currentEquipmentLinks = equipLinks;
            _currentSkillsLookup = skillsLookup;
            _currentSkillsLinks = skillsLinks;

            if (unit is null || string.IsNullOrWhiteSpace(unit.ProfileGroupsJson))
            {
                ProfilesStatus = "No profiles found for this unit.";
                return;
            }

            using var doc = JsonDocument.Parse(unit.ProfileGroupsJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
            {
                ProfilesStatus = "No profiles found for this unit.";
                return;
            }

            PopulateUnitStatsFromFirstProfile(doc.RootElement);
            var orderTraits = ParseUnitOrderTraits(doc.RootElement);
            ShowIrregularOrderIcon = orderTraits.HasIrregular;
            ShowRegularOrderIcon = !orderTraits.HasIrregular && orderTraits.HasRegular;
            ShowImpetuousIcon = orderTraits.HasImpetuous;
            ShowTacticalAwarenessIcon = orderTraits.HasTacticalAwareness;
            ImpetuousIconUrl = orderTraits.HasImpetuous
                ? TryResolveFirstLinkByPredicate(
                    skillsLookup,
                    skillsLinks,
                    name => name.Contains("impetuous", StringComparison.OrdinalIgnoreCase))
                : null;
            TacticalAwarenessIconUrl = orderTraits.HasTacticalAwareness
                ? TryResolveFirstLinkByPredicate(
                    skillsLookup,
                    skillsLinks,
                    name => name.Contains("tactical", StringComparison.OrdinalIgnoreCase))
                : null;
            var techTraits = ParseUnitTechTraits(doc.RootElement, equipLookup, skillsLookup, charsLookup);
            ShowCubeIcon = techTraits.HasCube;
            ShowCube2Icon = techTraits.HasCube2;
            ShowHackableIcon = techTraits.HasHackable;
            CubeIconUrl = techTraits.HasCube
                ? TryResolveFirstLinkByPredicate(
                    charsLookup,
                    charsLinks,
                    name =>
                    {
                        var normalized = NormalizeTokenText(name);
                        return Regex.IsMatch(normalized, @"\bcube\b", RegexOptions.IgnoreCase) &&
                               !Regex.IsMatch(normalized, @"\bcube\s*2(?:\.0)?\b|\bcube2(?:\.0)?\b", RegexOptions.IgnoreCase);
                    })
                  ?? TryResolveFirstLinkByPredicate(
                    equipLookup,
                    equipLinks,
                    name =>
                    {
                        var normalized = NormalizeTokenText(name);
                        return Regex.IsMatch(normalized, @"\bcube\b", RegexOptions.IgnoreCase) &&
                               !Regex.IsMatch(normalized, @"\bcube\s*2(?:\.0)?\b|\bcube2(?:\.0)?\b", RegexOptions.IgnoreCase);
                    })
                  ?? TryResolveFirstLinkByPredicate(
                    skillsLookup,
                    skillsLinks,
                    name =>
                    {
                        var normalized = NormalizeTokenText(name);
                        return Regex.IsMatch(normalized, @"\bcube\b", RegexOptions.IgnoreCase) &&
                               !Regex.IsMatch(normalized, @"\bcube\s*2(?:\.0)?\b|\bcube2(?:\.0)?\b", RegexOptions.IgnoreCase);
                    })
                : null;
            Cube2IconUrl = techTraits.HasCube2
                ? TryResolveFirstLinkByPredicate(
                    charsLookup,
                    charsLinks,
                    name => Regex.IsMatch(NormalizeTokenText(name), @"\bcube\s*2(?:\.0)?\b|\bcube2(?:\.0)?\b", RegexOptions.IgnoreCase))
                  ?? TryResolveFirstLinkByPredicate(
                    equipLookup,
                    equipLinks,
                    name => Regex.IsMatch(NormalizeTokenText(name), @"\bcube\s*2(?:\.0)?\b|\bcube2(?:\.0)?\b", RegexOptions.IgnoreCase))
                  ?? TryResolveFirstLinkByPredicate(
                    skillsLookup,
                    skillsLinks,
                    name => Regex.IsMatch(NormalizeTokenText(name), @"\bcube\s*2(?:\.0)?\b|\bcube2(?:\.0)?\b", RegexOptions.IgnoreCase))
                : null;
            HackableIconUrl = techTraits.HasHackable
                ? TryResolveFirstLinkByPredicate(
                    charsLookup,
                    charsLinks,
                    name => name.Contains("hackable", StringComparison.OrdinalIgnoreCase))
                  ?? TryResolveFirstLinkByPredicate(
                    equipLookup,
                    equipLinks,
                    name => name.Contains("hackable", StringComparison.OrdinalIgnoreCase))
                  ?? TryResolveFirstLinkByPredicate(
                    skillsLookup,
                    skillsLinks,
                    name => name.Contains("hackable", StringComparison.OrdinalIgnoreCase))
                : null;

            HashSet<string>? commonEquipNames = null;
            HashSet<string>? commonSkillNames = null;
            var profileCount = 0;
            var hasControllerGroups = doc.RootElement.EnumerateArray().Any(usageGroup => IsControllerGroup(doc.RootElement, usageGroup));

            var seenConfigurations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var group in doc.RootElement.EnumerateArray())
            {
                if (hasControllerGroups && !IsControllerGroup(doc.RootElement, group))
                {
                    continue;
                }

                var groupName = group.TryGetProperty("isc", out var iscElement) && iscElement.ValueKind == JsonValueKind.String
                    ? iscElement.GetString() ?? string.Empty
                    : string.Empty;

                if (!group.TryGetProperty("profiles", out var profilesElement) || profilesElement.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var profile in profilesElement.EnumerateArray())
                {
                    var profileEquip = GetOrderedIdDisplayNames(profile, "equip", equipLookup, extrasLookup, ShowUnitsInInches)
                        .Select(x => x.Name)
                        .ToHashSet(StringComparer.OrdinalIgnoreCase);
                    var profileSkills = GetOrderedIdDisplayNames(profile, "skills", skillsLookup, extrasLookup, ShowUnitsInInches)
                        .Select(x => x.Name)
                        .ToHashSet(StringComparer.OrdinalIgnoreCase);

                    if (commonEquipNames is null)
                    {
                        commonEquipNames = new HashSet<string>(profileEquip, StringComparer.OrdinalIgnoreCase);
                    }
                    else
                    {
                        commonEquipNames.IntersectWith(profileEquip);
                    }

                    if (commonSkillNames is null)
                    {
                        commonSkillNames = new HashSet<string>(profileSkills, StringComparer.OrdinalIgnoreCase);
                    }
                    else
                    {
                        commonSkillNames.IntersectWith(profileSkills);
                    }

                    profileCount++;
                }

                if (!group.TryGetProperty("options", out var optionsElement) || optionsElement.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var option in optionsElement.EnumerateArray())
                {
                    if (LieutenantOnlyUnits && !IsLieutenantOption(option, skillsLookup))
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

                    var displayName = BuildOptionDisplayName(option, optionName, equipLookup, skillsLookup);
                    var optionWeapons = GetOrderedIdDisplayNamesFromEntries(
                            GetOptionEntriesWithIncludes(doc.RootElement, option, "weapons"),
                            weaponsLookup,
                            extrasLookup,
                            ShowUnitsInInches);
                    var rangedWeaponEntries = optionWeapons.Where(x => !IsMeleeWeaponName(x.Name)).ToList();
                    var meleeWeaponEntries = optionWeapons.Where(x => IsMeleeWeaponName(x.Name)).ToList();
                    var rangedWeapons = JoinOrDash(rangedWeaponEntries.Select(x => x.Name));
                    var meleeWeapons = JoinOrDash(meleeWeaponEntries.Select(x => x.Name));

                    var uniqueEquipmentEntries = GetOrderedIdDisplayNamesFromEntries(
                                GetOptionEntriesWithIncludes(doc.RootElement, option, "equip"),
                                equipLookup,
                                extrasLookup,
                                ShowUnitsInInches)
                            .ToList();
                    var uniqueEquipment = JoinOrDash(uniqueEquipmentEntries.Select(x => x.Name));

                    var optionSkillsEntries = BuildConfigurationSkillEntries(
                            GetOrderedIdDisplayNamesFromEntries(
                                GetOptionEntriesWithIncludes(doc.RootElement, option, "skills"),
                                skillsLookup,
                                extrasLookup,
                                ShowUnitsInInches))
                        .ToList();
                    var uniqueSkillsEntries = optionSkillsEntries;
                    var uniqueSkills = JoinOrDash(uniqueSkillsEntries.Select(x => x.Name));

                    var peripheralEntries = GetCountedIdDisplayNamesFromEntries(
                                GetDisplayPeripheralEntriesForOption(doc.RootElement, group, option),
                                peripheralLookup,
                                extrasLookup,
                                ShowUnitsInInches)
                            .ToList();
                    var peripherals = JoinOrDash(peripheralEntries.Select(x => x.Name));
                    var swc = ReadOptionSwc(option);
                    var cost = ReadAdjustedOptionCost(doc.RootElement, group, option);
                    var isLieutenant = IsLieutenantOption(option, skillsLookup);
                    var profileKey = $"{groupName}|{optionName}|{cost}|{swc}|lt:{(isLieutenant ? 1 : 0)}";

                    if (MercsOnlyUnits && IsPositiveSwc(swc))
                    {
                        continue;
                    }

                    var dedupeKey = $"{groupName}|{displayName}|{rangedWeapons}|{meleeWeapons}|{uniqueEquipment}|{uniqueSkills}|{peripherals}|{swc}|{cost}";
                    if (!seenConfigurations.Add(dedupeKey))
                    {
                        continue;
                    }

                    var rangedLines = BuildLinkedLines(rangedWeaponEntries, weaponsLinks);
                    var meleeLines = BuildLinkedLines(meleeWeaponEntries, weaponsLinks);
                    var uniqueEquipmentLines = BuildLinkedLines(uniqueEquipmentEntries, equipLinks);
                    var uniqueSkillsLines = BuildLinkedLines(uniqueSkillsEntries, skillsLinks);
                    var peripheralLines = BuildLinkedLines(peripheralEntries.Select(x => (x.Id, x.Name)).ToList(), peripheralLinks);

                    Profiles.Add(new ViewerProfileItem
                    {
                        GroupName = groupName,
                        Name = displayName,
                        ProfileKey = profileKey,
                        IsLieutenant = isLieutenant,
                        NameFormatted = BuildNameFormatted(displayName),
                        RangedWeapons = rangedWeapons,
                        RangedWeaponsFormatted = BuildLinkedFormattedString(rangedLines, Color.FromArgb("#EF4444")),
                        MeleeWeapons = meleeWeapons,
                        MeleeWeaponsFormatted = BuildLinkedFormattedString(meleeLines, Color.FromArgb("#22C55E")),
                        UniqueEquipment = uniqueEquipment,
                        UniqueEquipmentFormatted = BuildLinkedFormattedString(uniqueEquipmentLines, Color.FromArgb("#06B6D4")),
                        UniqueSkills = uniqueSkills,
                        UniqueSkillsFormatted = BuildLinkedFormattedString(uniqueSkillsLines, Color.FromArgb("#F59E0B")),
                        Peripherals = peripherals,
                        PeripheralsFormatted = BuildLinkedFormattedString(peripheralLines, Color.FromArgb("#FACC15")),
                        Swc = swc,
                        SwcDisplay = MercsOnlyUnits ? string.Empty : $"SWC {swc}",
                        Cost = cost,
                        ShowProfileTacticalAwarenessIcon = !ShowTacticalAwarenessIcon &&
                                                           optionSkillsEntries.Any(x => x.Name.Contains("tactical awareness", StringComparison.OrdinalIgnoreCase))
                    });
                }
            }

            var stableEquip = profileCount > 0 ? (IEnumerable<string>)(commonEquipNames ?? []) : [];
            var stableSkills = profileCount > 0 ? (IEnumerable<string>)(commonSkillNames ?? []) : [];
            EquipmentSummary = BuildNamedSummary("Equipment", stableEquip);
            SpecialSkillsSummary = BuildNamedSummary("Special Skills", stableSkills);
            EquipmentSummaryFormatted = BuildNamedSummaryFormatted(
                "Equipment",
                stableEquip,
                equipLookup,
                equipLinks,
                Color.FromArgb("#06B6D4"));
            SpecialSkillsSummaryFormatted = BuildNamedSummaryFormatted(
                "Special Skills",
                stableSkills,
                skillsLookup,
                skillsLinks,
                Color.FromArgb("#F59E0B"));
            ProfilesStatus = Profiles.Count == 0 ? "No configurations found for this unit." : $"{Profiles.Count} configurations loaded.";
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"LoadProfilesForSelectedUnitAsync failed: {ex.Message}");
            ProfilesStatus = $"Failed to load profiles: {ex.Message}";
        }
    }

    private Task ApplyGlobalDisplayUnitsPreferenceAsync(CancellationToken cancellationToken = default)
    {
        if (_appSettingsProvider is null)
        {
            return Task.CompletedTask;
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var showInches = _appSettingsProvider.GetShowUnitsInInches();
            if (_showUnitsInInches == showInches)
            {
                return Task.CompletedTask;
            }

            _showUnitsInInches = showInches;
            OnPropertyChanged(nameof(ShowUnitsInInches));
            OnPropertyChanged(nameof(ShowUnitsInCentimeters));
            UpdateUnitMoveDisplay();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ApplyGlobalDisplayUnitsPreferenceAsync failed: {ex.Message}");
        }

        return Task.CompletedTask;
    }
}
