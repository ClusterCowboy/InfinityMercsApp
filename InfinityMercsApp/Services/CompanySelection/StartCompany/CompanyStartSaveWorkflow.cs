using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using InfinityMercsApp.Infrastructure.Providers;
using InfinityMercsApp.Services;
using InfinityMercsApp.Views.Common.UICommon;

namespace InfinityMercsApp.Views.Common;

internal sealed class CompanyStartSaveRequest<TFaction, TEntry, TCaptainStats>
    where TFaction : class, ICompanySourceFaction
    where TEntry : class, ICompanyMercsEntry
    where TCaptainStats : class
{
    public required string CompanyName { get; init; }
    public required string CompanyType { get; init; }
    public required IEnumerable<TEntry> MercsCompanyEntries { get; init; }
    public required bool ShowRightSelectionBox { get; init; }
    public required TFaction? LeftSlotFaction { get; init; }
    public required TFaction? RightSlotFaction { get; init; }
    public required IEnumerable<TFaction> Factions { get; init; }
    public required IArmyDataService ArmyDataService { get; init; }
    public required ISpecOpsProvider SpecOpsProvider { get; init; }
    public required INavigation Navigation { get; init; }
    public required bool ShowUnitsInInches { get; init; }
    public required string SelectedStartSeasonPoints { get; init; }
    public required string SeasonPointsCapText { get; init; }
    public required Func<int, string?> TryGetMetadataFactionName { get; init; }
    public required Func<TCaptainStats, string?> ReadCaptainName { get; init; }
    public required Func<string, string, string, Task> DisplayAlertAsync { get; init; }
    public required Func<string, Task> NavigateToCompanyViewerAsync { get; init; }
}

internal static class CompanyStartSaveWorkflow
{
    internal static async Task RunAsync<TFaction, TEntry, TCaptainStats>(CompanyStartSaveRequest<TFaction, TEntry, TCaptainStats> request)
        where TFaction : class, ICompanySourceFaction
        where TEntry : class, ICompanyMercsEntry
        where TCaptainStats : class
    {
        var entries = request.MercsCompanyEntries as IList<TEntry> ?? request.MercsCompanyEntries.ToList();
        var factions = request.Factions as IList<TFaction> ?? request.Factions.ToList();
        var captainEntry = entries.FirstOrDefault(x => x.IsLieutenant) ?? entries.FirstOrDefault();
        if (captainEntry is null)
        {
            await request.DisplayAlertAsync("Save Failed", "Add at least one unit before starting a company.", "OK");
            return;
        }

        var sourceFactions = CompanyUnitDetailsShared.BuildUnitSourceFactions(
            request.ShowRightSelectionBox,
            request.LeftSlotFaction,
            request.RightSlotFaction,
            faction => faction.Id);
        var firstSourceFactionId = sourceFactions.FirstOrDefault()?.Id;

        var improvedCaptainStats = await CompanyCaptainWorkflowService.ShowCaptainConfigurationAsync<TCaptainStats>(
            new CompanyCaptainWorkflowRequest
            {
                Navigation = request.Navigation,
                FallbackSourceFactionId = captainEntry.SourceFactionId,
                FirstSourceFactionId = firstSourceFactionId,
                UnitName = captainEntry.Name,
                UnitCost = captainEntry.CostValue,
                UnitStatline = captainEntry.Subtitle ?? "-",
                UnitRangedWeapons = captainEntry.SavedRangedWeapons,
                UnitCcWeapons = captainEntry.SavedCcWeapons,
                UnitSkills = captainEntry.SavedSkills,
                UnitEquipment = captainEntry.SavedEquipment,
                UnitCachedLogoPath = captainEntry.CachedLogoPath,
                UnitPackagedLogoPath = captainEntry.PackagedLogoPath,
                TryGetParentFactionId = factionId => factions.FirstOrDefault(x => x.Id == factionId)?.ParentId,
                TryGetFactionName = factionId => factions.FirstOrDefault(x => x.Id == factionId)?.Name,
                TryGetMetadataFactionName = request.TryGetMetadataFactionName,
                ArmyDataService = request.ArmyDataService,
                SpecOpsProvider = request.SpecOpsProvider,
                ShowUnitsInInches = request.ShowUnitsInInches
            });
        if (improvedCaptainStats is null)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var companyName = request.CompanyName.Trim();
        var safeFileName = Regex.Replace(companyName.ToLowerInvariant(), @"[^a-z0-9]+", "-").Trim('-');
        if (string.IsNullOrWhiteSpace(safeFileName))
        {
            safeFileName = "company";
        }

        var saveDir = Path.Combine(FileSystem.Current.AppDataDirectory, "MercenaryRecords");
        Directory.CreateDirectory(saveDir);
        var companyIndex = CompanySelectionSharedUtilities.GetNextCompanyIndex(saveDir, companyName, safeFileName);
        var fileName = $"{safeFileName}-{companyIndex:D4}.json";

        var captainName = request.ReadCaptainName(improvedCaptainStats);
        var normalizedCaptainName = string.IsNullOrWhiteSpace(captainName) ? "Captain" : captainName.Trim();
        var startSeasonPoints = int.TryParse(request.SelectedStartSeasonPoints, out var parsedStartSeasonPoints)
            ? parsedStartSeasonPoints
            : 0;
        var payload = new
        {
            CompanyName = companyName,
            CompanyType = request.CompanyType,
            CompanyIdentifier = CompanySelectionSharedUtilities.ComputeCompanyIdentifier(fileName),
            CompanyIndex = companyIndex,
            CreatedUtc = now.ToString("O", CultureInfo.InvariantCulture),
            StartSeasonPoints = startSeasonPoints,
            PointsLimit = startSeasonPoints,
            CurrentPoints = int.TryParse(request.SeasonPointsCapText, out var currentPoints) ? currentPoints : 0,
            ImprovedCaptainStats = improvedCaptainStats,
            SourceFactions = sourceFactions
                .Select(faction => new
                {
                    FactionId = faction.Id,
                    FactionName = faction.Name
                })
                .ToList(),
            Entries = BuildSerializedEntries(entries, normalizedCaptainName)
        };

        var filePath = Path.Combine(saveDir, fileName);
        await File.WriteAllTextAsync(
            filePath,
            JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));

        await request.NavigateToCompanyViewerAsync(filePath);
    }

    private static List<SerializedCompanyEntry> BuildSerializedEntries<TEntry>(
        IList<TEntry> entries,
        string normalizedCaptainName)
        where TEntry : class, ICompanyMercsEntry
    {
        var result = new List<SerializedCompanyEntry>(entries.Count * 2);
        var entryIndex = 0;

        foreach (var entry in entries)
        {
            var primaryEntryIndex = entryIndex++;
            result.Add(CreatePrimaryEntry(entry, primaryEntryIndex, normalizedCaptainName));

            if (!entry.HasPeripheralStatBlock)
            {
                continue;
            }

            result.Add(CreatePeripheralEntry(entry, entryIndex++, primaryEntryIndex));
        }

        return result;
    }

    private static SerializedCompanyEntry CreatePrimaryEntry<TEntry>(
        TEntry entry,
        int entryIndex,
        string normalizedCaptainName)
        where TEntry : class, ICompanyMercsEntry
    {
        return new SerializedCompanyEntry
        {
            EntryIndex = entryIndex,
            Name = entry.Name,
            BaseUnitName = entry.Name,
            CustomName = entry.IsLieutenant ? normalizedCaptainName : entry.Name,
            UnitTypeCode = string.IsNullOrWhiteSpace(entry.UnitTypeCode)
                ? string.Empty
                : entry.UnitTypeCode.Trim().ToUpperInvariant(),
            ProfileKey = entry.ProfileKey,
            SourceFactionId = entry.SourceFactionId,
            SourceUnitId = entry.SourceUnitId,
            LogoSourceFactionId = entry.LogoSourceFactionId,
            LogoSourceUnitId = entry.LogoSourceUnitId,
            IsPeripheralUnit = false,
            ParentEntryIndex = null,
            Cost = entry.CostValue,
            IsLieutenant = entry.IsLieutenant,
            SavedEquipment = entry.SavedEquipment,
            SavedSkills = entry.SavedSkills,
            SavedRangedWeapons = entry.SavedRangedWeapons,
            SavedCcWeapons = entry.SavedCcWeapons,
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
            ExperiencePoints = Math.Max(0, entry.ExperiencePoints)
        };
    }

    private static SerializedCompanyEntry CreatePeripheralEntry<TEntry>(
        TEntry entry,
        int entryIndex,
        int parentEntryIndex)
        where TEntry : class, ICompanyMercsEntry
    {
        var peripheralName = ResolvePeripheralEntryName(entry.PeripheralNameHeading, entry.Name);
        return new SerializedCompanyEntry
        {
            EntryIndex = entryIndex,
            Name = peripheralName,
            BaseUnitName = peripheralName,
            CustomName = peripheralName,
            UnitTypeCode = "PERIPHERAL",
            ProfileKey = $"{entry.ProfileKey}|peripheral",
            SourceFactionId = entry.SourceFactionId,
            SourceUnitId = entry.SourceUnitId,
            LogoSourceFactionId = entry.LogoSourceFactionId,
            LogoSourceUnitId = entry.LogoSourceUnitId,
            IsPeripheralUnit = true,
            ParentEntryIndex = parentEntryIndex,
            Cost = 0,
            IsLieutenant = false,
            SavedEquipment = entry.SavedPeripheralEquipment,
            SavedSkills = entry.SavedPeripheralSkills,
            SavedRangedWeapons = "-",
            SavedCcWeapons = "-",
            HasPeripheralStatBlock = false,
            PeripheralNameHeading = string.Empty,
            PeripheralMov = "-",
            PeripheralCc = "-",
            PeripheralBs = "-",
            PeripheralPh = "-",
            PeripheralWip = "-",
            PeripheralArm = "-",
            PeripheralBts = "-",
            PeripheralVitalityHeader = "VITA",
            PeripheralVitality = "-",
            PeripheralS = "-",
            PeripheralAva = "-",
            SavedPeripheralEquipment = "-",
            SavedPeripheralSkills = "-",
            ExperiencePoints = 0
        };
    }

    private static string ResolvePeripheralEntryName(string? peripheralHeading, string fallbackUnitName)
    {
        if (!string.IsNullOrWhiteSpace(peripheralHeading))
        {
            var heading = peripheralHeading.Trim();
            const string prefix = "Peripheral:";
            if (heading.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                heading = heading[prefix.Length..].Trim();
            }

            if (!string.IsNullOrWhiteSpace(heading))
            {
                return heading;
            }
        }

        return $"{fallbackUnitName} Peripheral";
    }

    private sealed class SerializedCompanyEntry
    {
        public int EntryIndex { get; init; }
        public string Name { get; init; } = string.Empty;
        public string BaseUnitName { get; init; } = string.Empty;
        public string CustomName { get; init; } = string.Empty;
        public string UnitTypeCode { get; init; } = string.Empty;
        public string ProfileKey { get; init; } = string.Empty;
        public int SourceFactionId { get; init; }
        public int SourceUnitId { get; init; }
        public int LogoSourceFactionId { get; init; }
        public int LogoSourceUnitId { get; init; }
        public bool IsPeripheralUnit { get; init; }
        public int? ParentEntryIndex { get; init; }
        public int Cost { get; init; }
        public bool IsLieutenant { get; init; }
        public string SavedEquipment { get; init; } = "-";
        public string SavedSkills { get; init; } = "-";
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
        public int ExperiencePoints { get; init; }
    }
}


