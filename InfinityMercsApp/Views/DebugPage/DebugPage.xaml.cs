using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using InfinityMercsApp.Infrastructure.Providers;
using InfinityMercsApp.Services;
using InfinityMercsApp.ViewModels;
using InfinityMercsApp.Views.Common;
using ArmyUnitRecord = InfinityMercsApp.Domain.Models.Army.Unit;

namespace InfinityMercsApp.Views;

public partial class DebugPage : ContentPage
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
    private const string DebugManifestFileName = "DebugTestCompanyManifest.json";
    private static readonly string[] AdditionalReformCompanyFileNames =
    [
        "test-correg-tag-0001.json",
        "test-nw-tag-lt-0001.json"
    ];

    private readonly IArmyDataService _armyDataService;
    private readonly ISpecOpsProvider _specOpsProvider;
    private bool _isReforming;

    public DebugPage(IArmyDataService armyDataService, ISpecOpsProvider specOpsProvider)
    {
        _armyDataService = armyDataService;
        _specOpsProvider = specOpsProvider;
        InitializeComponent();
    }

    private async void OnReformTestCompaniesClicked(object? sender, EventArgs e)
    {
        if (_isReforming)
        {
            return;
        }

        _isReforming = true;
        var triggerButton = sender as Button;
        if (triggerButton is not null)
        {
            triggerButton.IsEnabled = false;
        }

        try
        {
            var manifest = await LoadManifestAsync();
            var manifestCompanies = manifest?.Companies?.ToList() ?? [];
            foreach (var additionalFile in AdditionalReformCompanyFileNames)
            {
                if (manifestCompanies.Any(x => string.Equals(x.FileName?.Trim(), additionalFile, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                manifestCompanies.Add(new DebugTestCompanyManifestEntry
                {
                    FileName = additionalFile
                });
            }

            if (manifestCompanies.Count == 0)
            {
                await DisplayAlert(
                    "Reform Test Companies",
                    $"No companies found in manifest '{DebugManifestFileName}'.",
                    "OK");
                return;
            }

            var saveDir = Path.Combine(FileSystem.Current.AppDataDirectory, "MercenaryRecords");
            Directory.CreateDirectory(saveDir);
            var summaryLines = new List<string>();

            foreach (var manifestCompany in manifestCompanies)
            {
                var fileName = manifestCompany.FileName?.Trim();
                if (string.IsNullOrWhiteSpace(fileName))
                {
                    summaryLines.Add("manifest entry: skipped (missing file name)");
                    continue;
                }

                var filePath = Path.Combine(saveDir, fileName);
                var source = manifestCompany.CompanyData ?? await TryLoadSavedCompanyFileAsync(filePath);
                if (source is null)
                {
                    summaryLines.Add($"{fileName}: skipped (missing company data)");
                    continue;
                }

                try
                {
                    var sourceFactions = BuildSourceFactions(source);
                    var reformEntries = BuildReformEntries(source);
                    if (reformEntries.Count == 0)
                    {
                        summaryLines.Add($"{fileName}: skipped (no primary entries)");
                        continue;
                    }

                    var request = new CompanyStartSaveRequest<DebugSourceFaction, DebugMercsEntry, DebugCaptainStats>
                    {
                        CompanyName = source.CompanyName,
                        CompanyType = source.CompanyType,
                        MercsCompanyEntries = reformEntries,
                        ShowRightSelectionBox = sourceFactions.Count > 1,
                        LeftSlotFaction = sourceFactions.Count > 0 ? sourceFactions[0] : null,
                        RightSlotFaction = sourceFactions.Count > 1 ? sourceFactions[1] : null,
                        Factions = sourceFactions,
                        ArmyDataService = _armyDataService,
                        SpecOpsProvider = _specOpsProvider,
                        Navigation = Navigation,
                        ShowUnitsInInches = false,
                        SelectedStartSeasonPoints = source.StartSeasonPoints.ToString(),
                        SeasonPointsCapText = source.CurrentPoints.ToString(),
                        TryGetMetadataFactionName = _ => null,
                        ReadCaptainName = stats => stats.CaptainName,
                        DisplayAlertAsync = async (title, message, cancel) => await DisplayAlert(title, message, cancel),
                        NavigateToCompanyViewerAsync = _ => Task.CompletedTask
                    };

                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }

                    await CompanyStartSaveWorkflow.RunWithProvidedCaptainStatsAsync(
                        request,
                        source.ImprovedCaptainStats ?? new DebugCaptainStats(),
                        outputFilePath: filePath,
                        navigateToViewer: false);

                    summaryLines.Add($"{fileName}: reformed");
                }
                catch (Exception ex)
                {
                    summaryLines.Add($"{fileName}: failed ({ex.Message})");
                }
            }

            var message = summaryLines.Count == 0
                ? "No files processed."
                : string.Join(Environment.NewLine, summaryLines);
            await DisplayAlert("Reform Test Companies", message, "OK");
        }
        finally
        {
            if (triggerButton is not null)
            {
                triggerButton.IsEnabled = true;
            }

            _isReforming = false;
        }
    }

    private List<DebugSourceFaction> BuildSourceFactions(DebugSavedCompanyFile source)
    {
        var factions = source.SourceFactions
            .Where(x => x.FactionId > 0)
            .Select(x => new DebugSourceFaction
            {
                Id = x.FactionId,
                ParentId = 0,
                Name = string.IsNullOrWhiteSpace(x.FactionName) ? $"Faction {x.FactionId}" : x.FactionName
            })
            .ToList();

        if (factions.Count > 0)
        {
            return factions;
        }

        return source.Entries
            .Where(x => !x.IsPeripheralUnit && x.SourceFactionId > 0)
            .Select(x => x.SourceFactionId)
            .Distinct()
            .Select(id => new DebugSourceFaction
            {
                Id = id,
                ParentId = 0,
                Name = $"Faction {id}"
            })
            .ToList();
    }

    private List<DebugMercsEntry> BuildReformEntries(
        DebugSavedCompanyFile source)
    {
        var sourcePeripheralByParentIndex = source.Entries
            .Where(x => x.IsPeripheralUnit && x.ParentEntryIndex.HasValue)
            .GroupBy(x => x.ParentEntryIndex!.Value)
            .ToDictionary(g => g.Key, g => g.First());

        var primaryEntries = source.Entries
            .Where(x => !x.IsPeripheralUnit)
            .OrderBy(x => x.EntryIndex)
            .ToList();
        var result = new List<DebugMercsEntry>(primaryEntries.Count);

        foreach (var entry in primaryEntries)
        {
            var factionId = entry.SourceFactionId > 0 ? entry.SourceFactionId : entry.FactionId;
            var sourceUnitId = entry.SourceUnitId > 0 ? entry.SourceUnitId : entry.ProfileId;
            if (!TryBuildDbDerivedProfilePayload(
                    factionId,
                    sourceUnitId,
                    entry.ProfileKey,
                    entry.IsLieutenant || entry.IsCaptain,
                    out var dbPayload))
            {
                continue;
            }

            var baseUnitName = string.IsNullOrWhiteSpace(entry.BaseUnitName)
                ? (string.IsNullOrWhiteSpace(entry.BaseProfileHumanReadable) ? entry.Name : entry.BaseProfileHumanReadable)
                : entry.BaseUnitName;
            var subtitle = BuildSubtitleFromBaseStats(entry);
            var hasSourcePeripheral = sourcePeripheralByParentIndex.TryGetValue(entry.EntryIndex, out var sourcePeripheralEntry);

            var peripheralNameHeading = dbPayload.PeripheralNameHeading;
            var peripheralMov = dbPayload.PeripheralMov;
            var peripheralCc = dbPayload.PeripheralCc;
            var peripheralBs = dbPayload.PeripheralBs;
            var peripheralPh = dbPayload.PeripheralPh;
            var peripheralWip = dbPayload.PeripheralWip;
            var peripheralArm = dbPayload.PeripheralArm;
            var peripheralBts = dbPayload.PeripheralBts;
            var peripheralVitalityHeader = dbPayload.PeripheralVitalityHeader;
            var peripheralVitality = dbPayload.PeripheralVitality;
            var peripheralS = dbPayload.PeripheralS;
            var peripheralAva = dbPayload.PeripheralAva;
            var savedPeripheralEquipment = dbPayload.SavedPeripheralEquipment;
            var savedPeripheralSkills = dbPayload.SavedPeripheralSkills;
            var savedPeripheralCharacteristics = dbPayload.SavedPeripheralCharacteristics;
            var savedPeripheralRangedWeapons = dbPayload.SavedPeripheralRangedWeapons;
            var savedPeripheralCcWeapons = dbPayload.SavedPeripheralCcWeapons;

            if (sourcePeripheralEntry is not null)
            {
                var sourcePeripheralName = !string.IsNullOrWhiteSpace(sourcePeripheralEntry.CustomName)
                    ? sourcePeripheralEntry.CustomName.Trim()
                    : (!string.IsNullOrWhiteSpace(sourcePeripheralEntry.BaseUnitName)
                        ? sourcePeripheralEntry.BaseUnitName.Trim()
                        : sourcePeripheralEntry.Name.Trim());

                peripheralNameHeading = string.IsNullOrWhiteSpace(sourcePeripheralName)
                    ? peripheralNameHeading
                    : $"Peripheral: {sourcePeripheralName}";
                peripheralMov = NormalizeStat(sourcePeripheralEntry.CurrentMov);
                peripheralCc = NormalizeStat(sourcePeripheralEntry.CurrentCc);
                peripheralBs = NormalizeStat(sourcePeripheralEntry.CurrentBs);
                peripheralPh = NormalizeStat(sourcePeripheralEntry.CurrentPh);
                peripheralWip = NormalizeStat(sourcePeripheralEntry.CurrentWip);
                peripheralArm = NormalizeStat(sourcePeripheralEntry.CurrentArm);
                peripheralBts = NormalizeStat(sourcePeripheralEntry.CurrentBts);
                peripheralVitality = NormalizeStat(sourcePeripheralEntry.CurrentVitaOrStr);
                peripheralS = NormalizeStat(sourcePeripheralEntry.CurrentS);
                peripheralVitalityHeader = InferVitalityHeaderForUnitType(sourcePeripheralEntry.UnitTypeCode);
                savedPeripheralEquipment = ResolveSavedCodes(sourcePeripheralEntry, "equip");
                savedPeripheralSkills = ResolveSavedCodes(sourcePeripheralEntry, "skills");
                savedPeripheralCharacteristics = ResolveSavedCodes(sourcePeripheralEntry, "chars");
                (savedPeripheralRangedWeapons, savedPeripheralCcWeapons) = ResolveSavedWeapons(sourcePeripheralEntry);
            }

            result.Add(new DebugMercsEntry
            {
                Name = string.IsNullOrWhiteSpace(entry.Name) ? baseUnitName : entry.Name,
                BaseUnitName = baseUnitName,
                CostValue = entry.CalculatedCr > 0 ? entry.CalculatedCr : entry.BaseCr,
                IsLieutenant = entry.IsLieutenant || entry.IsCaptain,
                UnitTypeCode = entry.UnitTypeCode,
                ProfileKey = entry.ProfileKey,
                SourceFactionId = factionId,
                SourceUnitId = sourceUnitId,
                LogoSourceFactionId = entry.LogoSourceFactionId > 0 ? entry.LogoSourceFactionId : factionId,
                LogoSourceUnitId = entry.LogoSourceUnitId > 0 ? entry.LogoSourceUnitId : sourceUnitId,
                SavedEquipment = dbPayload.SavedEquipment,
                SavedSkills = dbPayload.SavedSkills,
                SavedCharacteristics = dbPayload.SavedCharacteristics,
                SavedRangedWeapons = dbPayload.SavedRangedWeapons,
                SavedCcWeapons = dbPayload.SavedCcWeapons,
                HasPeripheralStatBlock = dbPayload.HasPeripheralStatBlock || hasSourcePeripheral,
                PeripheralNameHeading = peripheralNameHeading,
                PeripheralMov = peripheralMov,
                PeripheralCc = peripheralCc,
                PeripheralBs = peripheralBs,
                PeripheralPh = peripheralPh,
                PeripheralWip = peripheralWip,
                PeripheralArm = peripheralArm,
                PeripheralBts = peripheralBts,
                PeripheralVitalityHeader = peripheralVitalityHeader,
                PeripheralVitality = peripheralVitality,
                PeripheralS = peripheralS,
                PeripheralAva = peripheralAva,
                SavedPeripheralEquipment = savedPeripheralEquipment,
                SavedPeripheralSkills = savedPeripheralSkills,
                SavedPeripheralCharacteristics = savedPeripheralCharacteristics,
                SavedPeripheralRangedWeapons = savedPeripheralRangedWeapons,
                SavedPeripheralCcWeapons = savedPeripheralCcWeapons,
                ExperiencePoints = Math.Max(0, entry.ExperiencePoints),
                UnitMoveDisplay = NormalizeStat(entry.BaseMov),
                Subtitle = subtitle
            });
        }

        return result;
    }

    private static async Task<DebugTestCompanyManifest?> LoadManifestAsync()
    {
        static async Task<string?> TryReadAssetAsync(string assetPath)
        {
            try
            {
                await using var stream = await FileSystem.Current.OpenAppPackageFileAsync(assetPath);
                using var reader = new StreamReader(stream);
                return await reader.ReadToEndAsync();
            }
            catch
            {
                return null;
            }
        }

        var rawJson = await TryReadAssetAsync(DebugManifestFileName)
                      ?? await TryReadAssetAsync($"Resources/Raw/{DebugManifestFileName}");
        if (string.IsNullOrWhiteSpace(rawJson))
        {
            return null;
        }

        return JsonSerializer.Deserialize<DebugTestCompanyManifest>(rawJson, JsonOptions);
    }

    private static async Task<DebugSavedCompanyFile?> TryLoadSavedCompanyFileAsync(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                return null;
            }

            var json = await File.ReadAllTextAsync(filePath);
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            return JsonSerializer.Deserialize<DebugSavedCompanyFile>(json, JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    private bool TryBuildDbDerivedProfilePayload(
        int factionId,
        int sourceUnitId,
        string? savedProfileKey,
        bool isLieutenant,
        out DbDerivedProfilePayload payload)
    {
        payload = new DbDerivedProfilePayload();
        if (factionId <= 0 || sourceUnitId <= 0)
        {
            return false;
        }

        var unit = _armyDataService.GetUnit(factionId, sourceUnitId);
        if (unit is null || string.IsNullOrWhiteSpace(unit.ProfileGroupsJson))
        {
            return false;
        }

        var snapshot = _armyDataService.GetFactionSnapshot(factionId);
        var filtersJson = snapshot?.FiltersJson;
        var equipLookup = CompanyUnitDetailsShared.BuildIdNameLookup(filtersJson, "equip");
        var skillsLookup = CompanyUnitDetailsShared.BuildIdNameLookup(filtersJson, "skills");
        var displayNameContext = CompanyUnitDetailDisplayNameContext.Create(
            filtersJson,
            showUnitsInInches: false,
            CompanySelectionSharedUtilities.TryParseId);

        using var doc = JsonDocument.Parse(unit.ProfileGroupsJson);
        var profileGroupsRoot = doc.RootElement;
        var options = CompanySelectionSharedUtilities
            .EnumerateOptions(profileGroupsRoot)
            .Where(option => !CompanySelectionSharedUtilities.IsPositiveSwc(CompanyProfileOptionService.ReadOptionSwc(option)))
            .ToList();

        var stableEquipFromProfiles = displayNameContext.ComputeCommonDisplayNamesFromProfiles(unit.ProfileGroupsJson, "equip", equipLookup);
        var stableEquipFromVisibleOptions = options.Count > 0
            ? displayNameContext.IntersectDisplayNamesWithIncludes(profileGroupsRoot, options, "equip", equipLookup)
            : [];
        var stableEquip = stableEquipFromProfiles
            .Concat(stableEquipFromVisibleOptions)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var stableSkillsFromProfiles = displayNameContext.ComputeCommonDisplayNamesFromProfiles(unit.ProfileGroupsJson, "skills", skillsLookup);
        var stableSkillsFromVisibleOptions = options.Count > 0
            ? displayNameContext.IntersectDisplayNamesWithIncludes(profileGroupsRoot, options, "skills", skillsLookup)
            : [];
        var stableSkills = stableSkillsFromProfiles
            .Concat(stableSkillsFromVisibleOptions)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();

        stableSkills = stableSkills
            .Where(x => !x.Contains("lieutenant", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var profiles = CompanyProfilePopulationWorkflowService.BuildProfiles(
            new CompanyProfilePopulationRequest<PeripheralMercsCompanyStats>
            {
                ProfileGroupsRoot = profileGroupsRoot,
                FiltersJson = filtersJson,
                ForceLieutenant = false,
                ShowTacticalAwarenessIcon = false,
                ShowUnitsInInches = false,
                TryParseId = CompanySelectionSharedUtilities.TryParseId,
                BuildIdNameLookup = CompanyUnitDetailsShared.BuildIdNameLookup,
                ShouldIncludeOption = (_, _, _) => true,
                ParseCostValue = CompanyUnitFilterService.ParseCostValue,
                TryFindPeripheralProfile = peripheralName =>
                    CompanyPeripheralProfileSelectionService.TryFindPeripheralStatElement(profileGroupsRoot, peripheralName, out var peripheralProfile)
                        ? peripheralProfile
                        : (JsonElement?)null,
                BuildPeripheralStatBlock = (peripheralName, peripheralProfile) =>
                    BuildPeripheralStatBlock(peripheralName, peripheralProfile, filtersJson),
                TryGetPeripheralUnitCost = peripheralName =>
                    CompanyUnitFilterService.TryGetPeripheralUnitCost(profileGroupsRoot, peripheralName, out var peripheralCost)
                        ? peripheralCost
                        : (int?)null,
                TryBuildSinglePeripheralDisplay = peripheralNames =>
                {
                    var success = CompanyUnitDetailsShared.TryBuildSinglePeripheralDisplay(peripheralNames, out var peripheralName, out var peripheralCount);
                    return (success, peripheralName, peripheralCount);
                },
                ExtractFirstPeripheralName = CompanyUnitDetailsShared.ExtractFirstPeripheralName,
                NormalizePeripheralNameForDedupe = CompanyUnitDetailsShared.NormalizePeripheralNameForDedupe,
                GetPeripheralTotalCount = CompanyUnitDetailsShared.GetPeripheralTotalCount,
                IsLieutenantOption = option => CompanySelectionSharedUtilities.IsLieutenantOption(option, skillsLookup),
                FormatMoveValue = _armyDataService.FormatMoveValue,
                BuildPeripheralSubtitle = stats => stats is null
                    ? "-"
                    : CompanyUnitDetailsShared.BuildPeripheralSubtitle(
                        stats.Mov,
                        stats.Cc,
                        stats.Bs,
                        stats.Ph,
                        stats.Wip,
                        stats.Arm,
                        stats.Bts,
                        stats.VitalityHeader,
                        stats.Vitality,
                        stats.S,
                        stats.Ava),
                ReadPeripheralNameHeading = stats => stats?.NameHeading ?? string.Empty,
                ReadPeripheralMoveFirstCm = stats => stats?.MoveFirstCm,
                ReadPeripheralMoveSecondCm = stats => stats?.MoveSecondCm,
                ReadPeripheralCc = stats => stats?.Cc ?? "-",
                ReadPeripheralBs = stats => stats?.Bs ?? "-",
                ReadPeripheralPh = stats => stats?.Ph ?? "-",
                ReadPeripheralWip = stats => stats?.Wip ?? "-",
                ReadPeripheralArm = stats => stats?.Arm ?? "-",
                ReadPeripheralBts = stats => stats?.Bts ?? "-",
                ReadPeripheralVitalityHeader = stats => stats?.VitalityHeader ?? "VITA",
                ReadPeripheralVitality = stats => stats?.Vitality ?? "-",
                ReadPeripheralS = stats => stats?.S ?? "-",
                ReadPeripheralAva = stats => stats?.Ava ?? "-",
                ReadPeripheralEquipment = stats => stats?.Equipment ?? "-",
                ReadPeripheralSkills = stats => stats?.Skills ?? "-"
            });

        if (profiles.Count == 0)
        {
            return false;
        }

        var selectedProfile = profiles.FirstOrDefault(x =>
            !string.IsNullOrWhiteSpace(savedProfileKey) &&
            string.Equals(x.ProfileKey, savedProfileKey, StringComparison.OrdinalIgnoreCase));
        selectedProfile ??= TryFindProfileByLooseKeyMatch(profiles, savedProfileKey);
        selectedProfile ??= profiles.FirstOrDefault(x => x.IsLieutenant == isLieutenant);
        selectedProfile ??= profiles.FirstOrDefault();
        if (selectedProfile is null)
        {
            return false;
        }

        var combinedEquipment = CompanyProfileTextService.MergeCommonAndUnique(stableEquip, selectedProfile.UniqueEquipment);
        var combinedSkills = CompanyProfileTextService.MergeCommonAndUnique(stableSkills, selectedProfile.UniqueSkills);
        var combinedCharacteristics = CompanyProfileTextService.SplitDisplayLine(selectedProfile.Characteristics).ToList();

        var peripheralStats = CompanySelectionRosterWorkflow.BuildMercsCompanyPeripheralStats<PeripheralMercsCompanyStats>(
            selectedProfile,
            unit.ProfileGroupsJson,
            filtersJson,
            (peripheralName, peripheralProfile, selectedFiltersJson) => BuildPeripheralStatBlock(peripheralName, peripheralProfile, selectedFiltersJson));

        payload = new DbDerivedProfilePayload
        {
            SavedEquipment = CompanyProfileTextService.JoinOrDash(combinedEquipment),
            SavedSkills = CompanyProfileTextService.JoinOrDash(combinedSkills),
            SavedCharacteristics = CompanyProfileTextService.JoinOrDash(combinedCharacteristics),
            SavedRangedWeapons = selectedProfile.RangedWeapons,
            SavedCcWeapons = selectedProfile.MeleeWeapons,
            HasPeripheralStatBlock = selectedProfile.HasPeripheralStatBlock || peripheralStats is not null,
            PeripheralNameHeading = peripheralStats?.NameHeading ?? selectedProfile.PeripheralNameHeading,
            PeripheralMov = peripheralStats?.Mov ?? selectedProfile.PeripheralMov,
            PeripheralCc = peripheralStats?.Cc ?? selectedProfile.PeripheralCc,
            PeripheralBs = peripheralStats?.Bs ?? selectedProfile.PeripheralBs,
            PeripheralPh = peripheralStats?.Ph ?? selectedProfile.PeripheralPh,
            PeripheralWip = peripheralStats?.Wip ?? selectedProfile.PeripheralWip,
            PeripheralArm = peripheralStats?.Arm ?? selectedProfile.PeripheralArm,
            PeripheralBts = peripheralStats?.Bts ?? selectedProfile.PeripheralBts,
            PeripheralVitalityHeader = peripheralStats?.VitalityHeader ?? selectedProfile.PeripheralVitalityHeader,
            PeripheralVitality = peripheralStats?.Vitality ?? selectedProfile.PeripheralVitality,
            PeripheralS = peripheralStats?.S ?? selectedProfile.PeripheralS,
            PeripheralAva = peripheralStats?.Ava ?? selectedProfile.PeripheralAva,
            SavedPeripheralEquipment = peripheralStats?.Equipment ?? "-",
            SavedPeripheralSkills = peripheralStats?.Skills ?? "-",
            SavedPeripheralCharacteristics = peripheralStats?.Characteristics ?? "-",
            SavedPeripheralRangedWeapons = peripheralStats?.RangedWeapons ?? "-",
            SavedPeripheralCcWeapons = peripheralStats?.CcWeapons ?? "-"
        };
        return true;
    }

    private PeripheralMercsCompanyStats? BuildPeripheralStatBlock(string peripheralName, JsonElement peripheralProfile, string? filtersJson)
    {
        return CompanyUnitDetailsShared.BuildPeripheralStatBlock(
            peripheralName,
            peripheralProfile,
            filtersJson,
            showUnitsInInches: false,
            element =>
            {
                var move = _armyDataService.ReadMoveValue(element);
                return (move.FirstCm, move.SecondCm);
            },
            commonResult => new PeripheralMercsCompanyStats
            {
                NameHeading = commonResult.NameHeading,
                MoveFirstCm = commonResult.MoveFirstCm,
                MoveSecondCm = commonResult.MoveSecondCm,
                Mov = commonResult.Mov,
                Cc = commonResult.Cc,
                Bs = commonResult.Bs,
                Ph = commonResult.Ph,
                Wip = commonResult.Wip,
                Arm = commonResult.Arm,
                Bts = commonResult.Bts,
                VitalityHeader = commonResult.VitalityHeader,
                Vitality = commonResult.Vitality,
                S = commonResult.S,
                Ava = commonResult.Ava,
                Equipment = commonResult.Equipment,
                Skills = commonResult.Skills,
                Characteristics = commonResult.Characteristics,
                RangedWeapons = commonResult.RangedWeapons,
                CcWeapons = commonResult.CcWeapons
            });
    }

    private static string BuildSubtitleFromBaseStats(DebugSavedEntry entry)
    {
        return
            $"MOV {NormalizeStat(entry.BaseMov)} | CC {NormalizeStat(entry.BaseCc)} | BS {NormalizeStat(entry.BaseBs)} | PH {NormalizeStat(entry.BasePh)} | WIP {NormalizeStat(entry.BaseWip)} | ARM {NormalizeStat(entry.BaseArm)} | BTS {NormalizeStat(entry.BaseBts)} | VITA {NormalizeStat(entry.BaseVitaOrStr)} | S {NormalizeStat(entry.BaseS)}";
    }

    private static string NormalizeStat(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();
    }

    private static string InferVitalityHeaderForUnitType(string? unitTypeCode)
    {
        var normalized = unitTypeCode?.Trim().ToUpperInvariant();
        return normalized is "TAG" or "REM" or "PERIPHERAL"
            ? "STR"
            : "VITA";
    }

    private (string SavedRangedWeapons, string SavedCcWeapons) ResolveSavedWeapons(DebugSavedEntry entry)
    {
        var factionId = entry.SourceFactionId > 0 ? entry.SourceFactionId : entry.FactionId;
        if (factionId <= 0)
        {
            return ("-", "-");
        }

        var snapshot = _armyDataService.GetFactionSnapshot(factionId);
        var weaponLookup = CompanyUnitDetailsShared.BuildIdNameLookup(snapshot?.FiltersJson, "weapons");
        var extrasLookup = CompanyUnitDetailsShared.BuildIdNameLookup(snapshot?.FiltersJson, "extras");

        var resolved = new List<string>();
        foreach (var code in entry.CurrentWeaponCodes)
        {
            if (code is null || code.Id <= 0 || !weaponLookup.TryGetValue(code.Id, out var name) || string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            var displayName = name.Trim();
            var extras = (code.Extra ?? [])
                .Distinct()
                .Select(extraId => extrasLookup.TryGetValue(extraId, out var extraName) ? extraName?.Trim() : null)
                .Where(extraName => !string.IsNullOrWhiteSpace(extraName))
                .Cast<string>()
                .ToList();
            if (extras.Count > 0)
            {
                displayName = $"{displayName} ({string.Join(", ", extras)})";
            }

            resolved.Add(displayName);
        }

        foreach (var customName in entry.CustomWeapons.Where(x => !string.IsNullOrWhiteSpace(x)))
        {
            resolved.Add(customName.Trim());
        }

        resolved = resolved
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var ranged = resolved.Where(x => !CompanyProfileTextService.IsMeleeWeaponName(x)).ToList();
        var cc = resolved.Where(CompanyProfileTextService.IsMeleeWeaponName).ToList();
        return (CompanyProfileTextService.JoinOrDash(ranged), CompanyProfileTextService.JoinOrDash(cc));
    }

    private string ResolveSavedCodes(DebugSavedEntry entry, string sectionName)
    {
        var factionId = entry.SourceFactionId > 0 ? entry.SourceFactionId : entry.FactionId;
        if (factionId <= 0)
        {
            return "-";
        }

        var snapshot = _armyDataService.GetFactionSnapshot(factionId);
        var sectionLookup = CompanyUnitDetailsShared.BuildIdNameLookup(snapshot?.FiltersJson, sectionName);
        var extrasLookup = CompanyUnitDetailsShared.BuildIdNameLookup(snapshot?.FiltersJson, "extras");

        IEnumerable<CompanySavedCodeRef> codes = sectionName switch
        {
            "skills" => entry.CurrentSkillCodes,
            "equip" => entry.CurrentEquipmentCodes,
            "chars" => entry.CurrentCharacteristicCodes,
            _ => []
        };

        IEnumerable<string> custom = sectionName switch
        {
            "skills" => entry.CustomSkills,
            "equip" => entry.CustomEquipment,
            "chars" => entry.CustomCharacteristics,
            _ => []
        };

        var resolved = new List<string>();
        foreach (var code in codes)
        {
            if (code is null || code.Id <= 0 || !sectionLookup.TryGetValue(code.Id, out var name) || string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            var displayName = name.Trim();
            var extras = (code.Extra ?? [])
                .Distinct()
                .Select(extraId => extrasLookup.TryGetValue(extraId, out var extraName) ? extraName?.Trim() : null)
                .Where(extraName => !string.IsNullOrWhiteSpace(extraName))
                .Cast<string>()
                .ToList();
            if (extras.Count > 0)
            {
                displayName = $"{displayName} ({string.Join(", ", extras)})";
            }

            resolved.Add(displayName);
        }

        foreach (var customName in custom.Where(x => !string.IsNullOrWhiteSpace(x)))
        {
            resolved.Add(customName.Trim());
        }

        resolved = resolved
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        return CompanyProfileTextService.JoinOrDash(resolved);
    }

    private static ViewerProfileItem? TryFindProfileByLooseKeyMatch(
        IEnumerable<ViewerProfileItem> profiles,
        string? savedProfileKey)
    {
        if (string.IsNullOrWhiteSpace(savedProfileKey))
        {
            return null;
        }

        var match = Regex.Match(
            savedProfileKey.Trim(),
            @"^\s*(?:[^|]*\|)?(?<name>[^|]+)\|(?<cost>\d+)\|[^|]*\|lt:(?<lt>[01])\s*$",
            RegexOptions.IgnoreCase);
        if (!match.Success)
        {
            return null;
        }

        var optionName = match.Groups["name"].Value.Trim();
        if (string.IsNullOrWhiteSpace(optionName))
        {
            return null;
        }

        var hasCost = int.TryParse(match.Groups["cost"].Value, out var cost);
        var hasLt = int.TryParse(match.Groups["lt"].Value, out var ltValue);
        var isLieutenant = hasLt && ltValue > 0;

        return profiles.FirstOrDefault(profile =>
        {
            if (!string.Equals(profile.Name?.Trim(), optionName, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (hasLt && profile.IsLieutenant != isLieutenant)
            {
                return false;
            }

            if (!hasCost)
            {
                return true;
            }

            return TryParseFirstInteger(profile.Cost, out var parsedCost) && parsedCost == cost;
        });
    }

    private static bool TryParseFirstInteger(string? value, out int parsed)
    {
        parsed = 0;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var match = Regex.Match(value, @"\d+");
        return match.Success && int.TryParse(match.Value, out parsed);
    }

    private sealed class DbDerivedProfilePayload
    {
        public string SavedEquipment { get; init; } = "-";
        public string SavedSkills { get; init; } = "-";
        public string SavedCharacteristics { get; init; } = "-";
        public string SavedRangedWeapons { get; init; } = "-";
        public string SavedCcWeapons { get; init; } = "-";
        public bool HasPeripheralStatBlock { get; init; }
        public string PeripheralNameHeading { get; init; } = string.Empty;
        public string PeripheralMov { get; init; } = "-";
        public string PeripheralCc { get; init; } = "-";
        public string PeripheralBs { get; init; } = "-";
        public string PeripheralPh { get; init; } = "-";
        public string PeripheralWip { get; init; } = "-";
        public string PeripheralArm { get; init; } = "-";
        public string PeripheralBts { get; init; } = "-";
        public string PeripheralVitalityHeader { get; init; } = "VITA";
        public string PeripheralVitality { get; init; } = "-";
        public string PeripheralS { get; init; } = "-";
        public string PeripheralAva { get; init; } = "-";
        public string SavedPeripheralEquipment { get; init; } = "-";
        public string SavedPeripheralSkills { get; init; } = "-";
        public string SavedPeripheralCharacteristics { get; init; } = "-";
        public string SavedPeripheralRangedWeapons { get; init; } = "-";
        public string SavedPeripheralCcWeapons { get; init; } = "-";
    }

    private sealed class DebugSourceFaction : ICompanySourceFaction
    {
        public int Id { get; init; }
        public int ParentId { get; init; }
        public string Name { get; init; } = string.Empty;
    }

    private sealed class DebugMercsEntry : ICompanyMercsEntry
    {
        public string Name { get; init; } = string.Empty;
        public string BaseUnitName { get; init; } = string.Empty;
        public int CostValue { get; init; }
        public bool IsLieutenant { get; init; }
        public string UnitTypeCode { get; init; } = string.Empty;
        public string ProfileKey { get; init; } = string.Empty;
        public int SourceFactionId { get; init; }
        public int SourceUnitId { get; init; }
        public int LogoSourceFactionId { get; init; }
        public int LogoSourceUnitId { get; init; }
        public string SavedEquipment { get; init; } = "-";
        public string SavedSkills { get; init; } = "-";
        public string SavedCharacteristics { get; init; } = "-";
        public string SavedRangedWeapons { get; init; } = "-";
        public string SavedCcWeapons { get; init; } = "-";
        public bool HasPeripheralStatBlock { get; init; }
        public string PeripheralNameHeading { get; init; } = string.Empty;
        public string PeripheralMov { get; set; } = "-";
        public string PeripheralCc { get; init; } = "-";
        public string PeripheralBs { get; init; } = "-";
        public string PeripheralPh { get; init; } = "-";
        public string PeripheralWip { get; init; } = "-";
        public string PeripheralArm { get; init; } = "-";
        public string PeripheralBts { get; init; } = "-";
        public string PeripheralVitalityHeader { get; init; } = "VITA";
        public string PeripheralVitality { get; init; } = "-";
        public string PeripheralS { get; init; } = "-";
        public string PeripheralAva { get; init; } = "-";
        public string SavedPeripheralEquipment { get; init; } = "-";
        public string SavedPeripheralSkills { get; init; } = "-";
        public string SavedPeripheralCharacteristics { get; init; } = "-";
        public string SavedPeripheralRangedWeapons { get; init; } = "-";
        public string SavedPeripheralCcWeapons { get; init; } = "-";
        public int ExperiencePoints { get; init; }
        public List<CompanyTrooperPerkState> Perks { get; init; } = [];
        public int? UnitMoveFirstCm { get; init; }
        public int? UnitMoveSecondCm { get; init; }
        public string UnitMoveDisplay { get; set; } = "-";
        public string? Subtitle { get; set; } = string.Empty;
        public int? PeripheralMoveFirstCm { get; init; }
        public int? PeripheralMoveSecondCm { get; init; }
        public string? CachedLogoPath { get; init; }
        public string? PackagedLogoPath { get; init; }
    }

    private sealed class DebugSavedCompanyFile : CompanySavedCompanyFileBase<DebugCaptainStats, DebugSavedCompanyFaction, DebugSavedEntry>
    {
    }

    private sealed class DebugTestCompanyManifest
    {
        [JsonPropertyName("generatedUtc")]
        public string? GeneratedUtc { get; init; }

        [JsonPropertyName("source")]
        public string? Source { get; init; }

        [JsonPropertyName("companies")]
        public List<DebugTestCompanyManifestEntry> Companies { get; init; } = [];
    }

    private sealed class DebugTestCompanyManifestEntry
    {
        [JsonPropertyName("fileName")]
        public string? FileName { get; init; }

        [JsonPropertyName("companyData")]
        public DebugSavedCompanyFile? CompanyData { get; init; }
    }

    private sealed class DebugSavedCompanyFaction : CompanySavedCompanyFactionBase
    {
    }

    private sealed class DebugCaptainStats : CompanySavedImprovedCaptainStatsBase
    {
    }

    private sealed class DebugSavedEntry : CompanySavedCompanyEntryBase
    {
        [JsonPropertyName("Base Profile (Human Readable)")]
        public string BaseProfileHumanReadable { get; init; } = string.Empty;
        [JsonPropertyName("IsCaptain")]
        public bool IsCaptain { get; init; }
        [JsonPropertyName("FactionId")]
        public int FactionId { get; init; }
        [JsonPropertyName("ProfileId")]
        public int ProfileId { get; init; }
        [JsonPropertyName("Base CR")]
        public int BaseCr { get; init; }
        [JsonPropertyName("Calculated CR")]
        public int CalculatedCr { get; init; }
        [JsonPropertyName("Base MOV")]
        public string BaseMov { get; init; } = "-";
        [JsonPropertyName("Base CC")]
        public string BaseCc { get; init; } = "-";
        [JsonPropertyName("Base BS")]
        public string BaseBs { get; init; } = "-";
        [JsonPropertyName("Base PH")]
        public string BasePh { get; init; } = "-";
        [JsonPropertyName("Base WIP")]
        public string BaseWip { get; init; } = "-";
        [JsonPropertyName("Base ARM")]
        public string BaseArm { get; init; } = "-";
        [JsonPropertyName("Base BTS")]
        public string BaseBts { get; init; } = "-";
        [JsonPropertyName("Base VITA/STR")]
        public string BaseVitaOrStr { get; init; } = "-";
        [JsonPropertyName("Base S")]
        public string BaseS { get; init; } = "-";
    }
}
