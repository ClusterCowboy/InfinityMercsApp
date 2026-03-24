using System.Text.Json;
using System.Text.Json.Serialization;
using InfinityMercsApp.Infrastructure.Providers;
using InfinityMercsApp.Services;
using InfinityMercsApp.Views.Common;

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
            var idNameLookupCache = new Dictionary<(int FactionId, string Section), Dictionary<int, string>>();
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
                    var reformEntries = BuildReformEntries(source, idNameLookupCache);
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
        DebugSavedCompanyFile source,
        Dictionary<(int FactionId, string Section), Dictionary<int, string>> idNameLookupCache)
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
            var savedSkills = ResolveDisplayLines(
                factionId,
                "skills",
                entry.CurrentSkillCodes,
                entry.CustomSkills,
                idNameLookupCache);
            var savedEquipment = ResolveDisplayLines(
                factionId,
                "equip",
                entry.CurrentEquipmentCodes,
                entry.CustomEquipment,
                idNameLookupCache);
            var savedCharacteristics = ResolveDisplayLines(
                factionId,
                "chars",
                entry.CurrentCharacteristicCodes,
                entry.CustomCharacteristics,
                idNameLookupCache);
            var weaponLines = ResolveDisplayLinesAsList(
                factionId,
                "weapons",
                entry.CurrentWeaponCodes,
                entry.CustomWeapons,
                idNameLookupCache);
            var savedRangedWeapons = CompanyProfileTextService.JoinOrDash(
                weaponLines.Where(x => !CompanyProfileTextService.IsMeleeWeaponName(x)));
            var savedCcWeapons = CompanyProfileTextService.JoinOrDash(
                weaponLines.Where(CompanyProfileTextService.IsMeleeWeaponName));

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
                SavedEquipment = savedEquipment,
                SavedSkills = savedSkills,
                SavedCharacteristics = savedCharacteristics,
                SavedRangedWeapons = savedRangedWeapons,
                SavedCcWeapons = savedCcWeapons,
                HasPeripheralStatBlock = entry.HasPeripheralStatBlock,
                PeripheralNameHeading = entry.PeripheralNameHeading,
                PeripheralMov = entry.PeripheralMov,
                PeripheralCc = entry.PeripheralCc,
                PeripheralBs = entry.PeripheralBs,
                PeripheralPh = entry.PeripheralPh,
                PeripheralWip = entry.PeripheralWip,
                PeripheralArm = entry.PeripheralArm,
                PeripheralBts = entry.PeripheralBts,
                PeripheralVitalityHeader = entry.PeripheralVitalityHeader,
                PeripheralVitality = entry.PeripheralVitality,
                PeripheralS = entry.PeripheralS,
                PeripheralAva = entry.PeripheralAva,
                SavedPeripheralEquipment = entry.SavedPeripheralEquipment,
                SavedPeripheralSkills = entry.SavedPeripheralSkills,
                SavedPeripheralCharacteristics = entry.SavedPeripheralCharacteristics,
                ExperiencePoints = Math.Max(0, entry.ExperiencePoints),
                UnitMoveDisplay = NormalizeStat(entry.BaseMov),
                Subtitle = subtitle
            });
        }

        return result;
    }

    private string ResolveDisplayLines(
        int factionId,
        string section,
        IReadOnlyList<CompanySavedCodeRef>? codeRefs,
        IReadOnlyList<string>? custom,
        Dictionary<(int FactionId, string Section), Dictionary<int, string>> idNameLookupCache)
    {
        var lines = ResolveDisplayLinesAsList(factionId, section, codeRefs, custom, idNameLookupCache);
        return CompanyProfileTextService.JoinOrDash(lines);
    }

    private List<string> ResolveDisplayLinesAsList(
        int factionId,
        string section,
        IReadOnlyList<CompanySavedCodeRef>? codeRefs,
        IReadOnlyList<string>? custom,
        Dictionary<(int FactionId, string Section), Dictionary<int, string>> idNameLookupCache)
    {
        var result = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var sectionLookup = GetIdNameLookup(factionId, section, idNameLookupCache);
        var extrasLookup = GetIdNameLookup(factionId, "extras", idNameLookupCache);

        if (codeRefs is not null)
        {
            foreach (var codeRef in codeRefs)
            {
                if (!sectionLookup.TryGetValue(codeRef.Id, out var name) || string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                var display = name.Trim();
                if (codeRef.Extra is { Count: > 0 })
                {
                    var extras = codeRef.Extra
                        .Select(id => extrasLookup.TryGetValue(id, out var extraName) ? extraName?.Trim() : null)
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();
                    if (extras.Count > 0)
                    {
                        display = $"{display} ({string.Join(", ", extras)})";
                    }
                }

                if (seen.Add(display))
                {
                    result.Add(display);
                }
            }
        }

        if (custom is not null)
        {
            foreach (var customValue in custom.Where(x => !string.IsNullOrWhiteSpace(x)))
            {
                var trimmed = customValue.Trim();
                if (seen.Add(trimmed))
                {
                    result.Add(trimmed);
                }
            }
        }

        return result;
    }

    private Dictionary<int, string> GetIdNameLookup(
        int factionId,
        string section,
        Dictionary<(int FactionId, string Section), Dictionary<int, string>> idNameLookupCache)
    {
        var key = (FactionId: factionId, Section: section);
        if (idNameLookupCache.TryGetValue(key, out var existing))
        {
            return existing;
        }

        var snapshot = _armyDataService.GetFactionSnapshot(factionId);
        var lookup = CompanySelectionSharedUtilities.BuildIdNameLookup(snapshot?.FiltersJson, section);
        idNameLookupCache[key] = lookup;
        return lookup;
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
