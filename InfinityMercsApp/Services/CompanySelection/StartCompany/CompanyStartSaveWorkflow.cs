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
    public Func<TEntry, TCaptainStats?>? TryGetPreconfiguredCaptainStats { get; init; }
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

        var improvedCaptainStats = request.TryGetPreconfiguredCaptainStats?.Invoke(captainEntry);
        if (improvedCaptainStats is null)
        {
            improvedCaptainStats = await CompanyCaptainWorkflowService.ShowCaptainConfigurationAsync<TCaptainStats>(
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
        }
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
            Entries = entries
                .Select((entry, entryIndex) => new
                {
                    EntryIndex = entryIndex,
                    Name = entry.Name,
                    BaseUnitName = entry.Name,
                    CustomName = entry.IsLieutenant ? normalizedCaptainName : "Trooper",
                    UnitTypeCode = string.IsNullOrWhiteSpace(entry.UnitTypeCode)
                        ? string.Empty
                        : entry.UnitTypeCode.Trim().ToUpperInvariant(),
                    ProfileKey = entry.ProfileKey,
                    SourceFactionId = entry.SourceFactionId,
                    SourceUnitId = entry.SourceUnitId,
                    Cost = entry.CostValue,
                    IsLieutenant = entry.IsLieutenant,
                    SavedEquipment = entry.SavedEquipment,
                    SavedSkills = entry.SavedSkills,
                    SavedRangedWeapons = entry.SavedRangedWeapons,
                    SavedCcWeapons = entry.SavedCcWeapons,
                    Subtitle = entry.Subtitle,
                    CachedLogoPath = entry.CachedLogoPath,
                    PackagedLogoPath = entry.PackagedLogoPath,
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
                })
                .ToList()
        };

        var filePath = Path.Combine(saveDir, fileName);
        await File.WriteAllTextAsync(
            filePath,
            JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));

        await request.NavigateToCompanyViewerAsync(filePath);
    }
}


