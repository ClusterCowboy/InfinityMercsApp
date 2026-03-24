using System.Text.Json;
using System.Text.Json.Serialization;
using InfinityMercsApp.Infrastructure.Providers;
using InfinityMercsApp.Services;
using InfinityMercsApp.Views.Common;
using ArmyUnitRecord = InfinityMercsApp.Domain.Models.Army.Unit;

namespace InfinityMercsApp.Views;

public partial class DebugPage : ContentPage
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly string[] ReformTestCompanyFiles =
    [
        "combinedaaa-0001.json",
        "ehruah-0001.json",
        "heather-the-feather-0001.json",
        "skots-0001.json",
        "the-road-crew-0001.json",
        "ziggity-0001.json"
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
            var saveDir = Path.Combine(FileSystem.Current.AppDataDirectory, "MercenaryRecords");
            Directory.CreateDirectory(saveDir);
            var summaryLines = new List<string>();

            foreach (var fileName in ReformTestCompanyFiles)
            {
                var filePath = Path.Combine(saveDir, fileName);
                if (!File.Exists(filePath))
                {
                    summaryLines.Add($"{fileName}: missing");
                    continue;
                }

                try
                {
                    var json = await File.ReadAllTextAsync(filePath);
                    var source = JsonSerializer.Deserialize<DebugSavedCompanyFile>(json, JsonOptions);
                    if (source is null)
                    {
                        summaryLines.Add($"{fileName}: failed (unable to parse)");
                        continue;
                    }

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

                    File.Delete(filePath);

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
                HasPeripheralStatBlock = dbPayload.HasPeripheralStatBlock,
                PeripheralNameHeading = dbPayload.PeripheralNameHeading,
                PeripheralMov = dbPayload.PeripheralMov,
                PeripheralCc = dbPayload.PeripheralCc,
                PeripheralBs = dbPayload.PeripheralBs,
                PeripheralPh = dbPayload.PeripheralPh,
                PeripheralWip = dbPayload.PeripheralWip,
                PeripheralArm = dbPayload.PeripheralArm,
                PeripheralBts = dbPayload.PeripheralBts,
                PeripheralVitalityHeader = dbPayload.PeripheralVitalityHeader,
                PeripheralVitality = dbPayload.PeripheralVitality,
                PeripheralS = dbPayload.PeripheralS,
                PeripheralAva = dbPayload.PeripheralAva,
                SavedPeripheralEquipment = dbPayload.SavedPeripheralEquipment,
                SavedPeripheralSkills = dbPayload.SavedPeripheralSkills,
                SavedPeripheralCharacteristics = dbPayload.SavedPeripheralCharacteristics,
                ExperiencePoints = Math.Max(0, entry.ExperiencePoints),
                UnitMoveDisplay = NormalizeStat(entry.BaseMov),
                Subtitle = subtitle
            });
        }

        return result;
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
            SavedPeripheralCharacteristics = peripheralStats?.Characteristics ?? "-"
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
                Characteristics = commonResult.Characteristics
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
        public int ExperiencePoints { get; init; }
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
