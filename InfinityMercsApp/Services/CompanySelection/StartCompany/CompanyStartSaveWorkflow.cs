using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using InfinityMercsApp.Infrastructure.Providers;
using InfinityMercsApp.Services;
using InfinityMercsApp.Views.Common.UICommon;

namespace InfinityMercsApp.Views.Common;

internal sealed class CompanyStartSaveRequest<TFaction, TEntry, TCaptainStats>
    where TFaction : class, ICompanySourceFaction
    where TEntry : class, ICompanyMercsEntry
    where TCaptainStats : CompanySavedImprovedCaptainStatsBase
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
        where TCaptainStats : CompanySavedImprovedCaptainStatsBase
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
        var captainPopupSkillLines = SplitCodes(captainEntry.SavedSkills);
        if (captainEntry.IsLieutenant &&
            !captainPopupSkillLines.Any(x =>
                string.Equals(x, "Lt", StringComparison.OrdinalIgnoreCase) ||
                x.Contains("lieutenant", StringComparison.OrdinalIgnoreCase)))
        {
            captainPopupSkillLines.Add("Lieutenant");
        }

        var captainPopupSkills = captainPopupSkillLines.Count == 0
            ? "-"
            : string.Join(Environment.NewLine, captainPopupSkillLines);

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
                UnitSkills = captainPopupSkills,
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
            SourceFactions = sourceFactions
                .Select(faction => new
                {
                    FactionId = faction.Id,
                    FactionName = faction.Name
                })
                .ToList(),
            Entries = BuildSerializedEntries(entries, normalizedCaptainName, improvedCaptainStats, request.ArmyDataService)
        };

        var filePath = Path.Combine(saveDir, fileName);
        await File.WriteAllTextAsync(
            filePath,
            JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            }));

        await request.NavigateToCompanyViewerAsync(filePath);
    }

    internal static async Task<string> RunWithProvidedCaptainStatsAsync<TFaction, TEntry, TCaptainStats>(
        CompanyStartSaveRequest<TFaction, TEntry, TCaptainStats> request,
        TCaptainStats improvedCaptainStats,
        string? outputFilePath = null,
        bool navigateToViewer = false)
        where TFaction : class, ICompanySourceFaction
        where TEntry : class, ICompanyMercsEntry
        where TCaptainStats : CompanySavedImprovedCaptainStatsBase
    {
        var entries = request.MercsCompanyEntries as IList<TEntry> ?? request.MercsCompanyEntries.ToList();
        var factions = request.Factions as IList<TFaction> ?? request.Factions.ToList();
        var captainEntry = entries.FirstOrDefault(x => x.IsLieutenant) ?? entries.FirstOrDefault();
        if (captainEntry is null)
        {
            throw new InvalidOperationException("Add at least one unit before starting a company.");
        }

        var sourceFactions = CompanyUnitDetailsShared.BuildUnitSourceFactions(
            request.ShowRightSelectionBox,
            request.LeftSlotFaction,
            request.RightSlotFaction,
            faction => faction.Id);
        var now = DateTimeOffset.UtcNow;
        var companyName = request.CompanyName.Trim();
        var safeFileName = Regex.Replace(companyName.ToLowerInvariant(), @"[^a-z0-9]+", "-").Trim('-');
        if (string.IsNullOrWhiteSpace(safeFileName))
        {
            safeFileName = "company";
        }

        var saveDir = Path.Combine(FileSystem.Current.AppDataDirectory, "MercenaryRecords");
        Directory.CreateDirectory(saveDir);

        string filePath;
        string fileName;
        int companyIndex;
        if (!string.IsNullOrWhiteSpace(outputFilePath))
        {
            filePath = outputFilePath;
            var outputDir = Path.GetDirectoryName(outputFilePath);
            if (!string.IsNullOrWhiteSpace(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            fileName = Path.GetFileName(outputFilePath);
            companyIndex = ParseCompanyIndexFromFileName(outputFilePath);
        }
        else
        {
            companyIndex = CompanySelectionSharedUtilities.GetNextCompanyIndex(saveDir, companyName, safeFileName);
            fileName = $"{safeFileName}-{companyIndex:D4}.json";
            filePath = Path.Combine(saveDir, fileName);
        }

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
            SourceFactions = sourceFactions
                .Select(faction => new
                {
                    FactionId = faction.Id,
                    FactionName = faction.Name
                })
                .ToList(),
            Entries = BuildSerializedEntries(entries, normalizedCaptainName, improvedCaptainStats, request.ArmyDataService)
        };

        await File.WriteAllTextAsync(
            filePath,
            JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            }));

        if (navigateToViewer)
        {
            await request.NavigateToCompanyViewerAsync(filePath);
        }

        return filePath;
    }

    private static List<SerializedCompanyEntry> BuildSerializedEntries<TEntry>(
        IList<TEntry> entries,
        string normalizedCaptainName,
        CompanySavedImprovedCaptainStatsBase improvedCaptainStats,
        IArmyDataService armyDataService)
        where TEntry : class, ICompanyMercsEntry
    {
        var result = new List<SerializedCompanyEntry>(entries.Count * 2);
        var codeIdLookupCache = new Dictionary<(int FactionId, string Section), Dictionary<string, int>>();
        var extrasIdLookupCache = new Dictionary<int, Dictionary<string, int>>();
        var entryIndex = 0;

        foreach (var entry in entries)
        {
            var primaryEntryIndex = entryIndex++;
            result.Add(CreatePrimaryEntry(
                entry,
                primaryEntryIndex,
                normalizedCaptainName,
                improvedCaptainStats,
                armyDataService,
                codeIdLookupCache,
                extrasIdLookupCache));

            if (!entry.HasPeripheralStatBlock)
            {
                continue;
            }

            result.Add(CreatePeripheralEntry(
                entry,
                entryIndex++,
                primaryEntryIndex,
                armyDataService,
                codeIdLookupCache,
                extrasIdLookupCache));
        }

        return result;
    }

    private static SerializedCompanyEntry CreatePrimaryEntry<TEntry>(
        TEntry entry,
        int entryIndex,
        string normalizedCaptainName,
        CompanySavedImprovedCaptainStatsBase improvedCaptainStats,
        IArmyDataService armyDataService,
        Dictionary<(int FactionId, string Section), Dictionary<string, int>> codeIdLookupCache,
        Dictionary<int, Dictionary<string, int>> extrasIdLookupCache)
        where TEntry : class, ICompanyMercsEntry
    {
        var baseMov = ReadStatValue(entry.Subtitle, "MOV", entry.UnitMoveDisplay);
        var baseCc = ReadStatValue(entry.Subtitle, "CC");
        var baseBs = ReadStatValue(entry.Subtitle, "BS");
        var basePh = ReadStatValue(entry.Subtitle, "PH");
        var baseWip = ReadStatValue(entry.Subtitle, "WIP");
        var baseArm = ReadStatValue(entry.Subtitle, "ARM");
        var baseBts = ReadStatValue(entry.Subtitle, "BTS");
        var baseVitaOrStr = ReadVitaOrStrValue(entry.Subtitle);
        var baseS = ReadStatValue(entry.Subtitle, "S");

        var currentCc = baseCc;
        var currentBs = baseBs;
        var currentPh = basePh;
        var currentWip = baseWip;
        var currentArm = baseArm;
        var currentBts = baseBts;
        var currentVitaOrStr = baseVitaOrStr;

        var baseSkillNames = SplitCodes(entry.SavedSkills);
        var currentSkillNames = SplitCodes(entry.SavedSkills);
        var baseCharacteristicNames = SplitCodes(entry.SavedCharacteristics);
        var currentCharacteristicNames = SplitCodes(entry.SavedCharacteristics);
        var baseEquipmentNames = SplitCodes(entry.SavedEquipment);
        var currentEquipmentNames = SplitCodes(entry.SavedEquipment);
        var baseWeaponNames = SplitCodes(string.Join(Environment.NewLine, [entry.SavedRangedWeapons, entry.SavedCcWeapons]));
        var currentWeaponNames = SplitCodes(string.Join(Environment.NewLine, [entry.SavedRangedWeapons, entry.SavedCcWeapons]));

        // Persist Lieutenant explicitly for founded companies when this entry is the Lieutenant.
        if (entry.IsLieutenant &&
            !currentSkillNames.Any(x => string.Equals(x?.Trim(), "Lieutenant", StringComparison.OrdinalIgnoreCase)))
        {
            currentSkillNames.Add("Lieutenant");
        }

        if (entry.IsLieutenant && improvedCaptainStats.IsEnabled)
        {
            currentCc = BuildCaptainCurrentStat(baseCc, improvedCaptainStats.CcTier, improvedCaptainStats.CcBonus);
            currentBs = BuildCaptainCurrentStat(baseBs, improvedCaptainStats.BsTier, improvedCaptainStats.BsBonus);
            currentPh = BuildCaptainCurrentStat(basePh, improvedCaptainStats.PhTier, improvedCaptainStats.PhBonus);
            currentWip = BuildCaptainCurrentStat(baseWip, improvedCaptainStats.WipTier, improvedCaptainStats.WipBonus);
            currentArm = BuildCaptainCurrentStat(baseArm, improvedCaptainStats.ArmTier, improvedCaptainStats.ArmBonus);
            currentBts = BuildCaptainCurrentStat(baseBts, improvedCaptainStats.BtsTier, improvedCaptainStats.BtsBonus);
            currentVitaOrStr = BuildCaptainCurrentStat(baseVitaOrStr, improvedCaptainStats.VitalityTier, improvedCaptainStats.VitalityBonus);

            currentWeaponNames = AppendChoices(
                currentWeaponNames,
                improvedCaptainStats.WeaponChoice1,
                improvedCaptainStats.WeaponChoice2,
                improvedCaptainStats.WeaponChoice3);
            currentSkillNames = AppendChoices(
                currentSkillNames,
                improvedCaptainStats.SkillChoice1,
                improvedCaptainStats.SkillChoice2,
                improvedCaptainStats.SkillChoice3);
            currentEquipmentNames = AppendChoices(
                currentEquipmentNames,
                improvedCaptainStats.EquipmentChoice1,
                improvedCaptainStats.EquipmentChoice2,
                improvedCaptainStats.EquipmentChoice3);
        }

        var baseLookupFactions = new[] { entry.SourceFactionId };
        var currentLookupFactions = entry.IsLieutenant && improvedCaptainStats.OptionFactionId > 0
            ? new[] { entry.SourceFactionId, improvedCaptainStats.OptionFactionId }
            : new[] { entry.SourceFactionId };

        var baseSkillResolution = ResolveCodes(armyDataService, codeIdLookupCache, extrasIdLookupCache, baseSkillNames, "skills", baseLookupFactions);
        var currentSkillResolution = ResolveCodes(armyDataService, codeIdLookupCache, extrasIdLookupCache, currentSkillNames, "skills", currentLookupFactions);
        var baseCharacteristicResolution = ResolveCodes(armyDataService, codeIdLookupCache, extrasIdLookupCache, baseCharacteristicNames, "chars", baseLookupFactions);
        var currentCharacteristicResolution = ResolveCodes(armyDataService, codeIdLookupCache, extrasIdLookupCache, currentCharacteristicNames, "chars", currentLookupFactions);
        var baseEquipmentResolution = ResolveCodes(armyDataService, codeIdLookupCache, extrasIdLookupCache, baseEquipmentNames, "equip", baseLookupFactions);
        var currentEquipmentResolution = ResolveCodes(armyDataService, codeIdLookupCache, extrasIdLookupCache, currentEquipmentNames, "equip", currentLookupFactions);
        var baseWeaponResolution = ResolveCodes(armyDataService, codeIdLookupCache, extrasIdLookupCache, baseWeaponNames, "weapons", baseLookupFactions);
        var currentWeaponResolution = ResolveCodes(armyDataService, codeIdLookupCache, extrasIdLookupCache, currentWeaponNames, "weapons", currentLookupFactions);
        var resolvedUnitTypeCode = ResolveUnitTypeCode(entry, armyDataService);

        var customSkills = baseSkillResolution.CustomNames
            .Concat(currentSkillResolution.CustomNames)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var customCharacteristics = baseCharacteristicResolution.CustomNames
            .Concat(currentCharacteristicResolution.CustomNames)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var customEquipment = baseEquipmentResolution.CustomNames
            .Concat(currentEquipmentResolution.CustomNames)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var customWeapons = baseWeaponResolution.CustomNames
            .Concat(currentWeaponResolution.CustomNames)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new SerializedCompanyEntry
        {
            EntryIndex = entryIndex,
            Name = entry.Name,
            BaseUnitName = string.IsNullOrWhiteSpace(entry.BaseUnitName) ? entry.Name : entry.BaseUnitName,
            CustomName = entry.IsLieutenant ? normalizedCaptainName : entry.Name,
            UnitTypeCode = resolvedUnitTypeCode,
            ProfileKey = entry.ProfileKey,
            SourceFactionId = entry.SourceFactionId,
            SourceUnitId = entry.SourceUnitId,
            LogoSourceFactionId = entry.LogoSourceFactionId,
            LogoSourceUnitId = entry.LogoSourceUnitId,
            IsPeripheralUnit = false,
            ParentEntryIndex = null,
            Cost = entry.CostValue,
            IsLieutenant = entry.IsLieutenant,
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
            ExperiencePoints = Math.Max(0, entry.ExperiencePoints),
            BaseProfileHumanReadable = string.IsNullOrWhiteSpace(entry.BaseUnitName) ? entry.Name : entry.BaseUnitName,
            IsCaptain = entry.IsLieutenant,
            FactionId = entry.SourceFactionId,
            ProfileId = entry.SourceUnitId,
            Renown = 0,
            BaseCr = entry.CostValue,
            CalculatedCr = entry.CostValue,
            BaseMov = baseMov,
            BaseCc = baseCc,
            BaseBs = baseBs,
            BasePh = basePh,
            BaseWip = baseWip,
            BaseArm = baseArm,
            BaseBts = baseBts,
            BaseVitaOrStr = baseVitaOrStr,
            BaseS = baseS,
            CurrentMov = baseMov,
            CurrentCc = currentCc,
            CurrentBs = currentBs,
            CurrentPh = currentPh,
            CurrentWip = currentWip,
            CurrentArm = currentArm,
            CurrentBts = currentBts,
            CurrentVitaOrStr = currentVitaOrStr,
            CurrentS = baseS,
            Xp = Math.Max(0, entry.ExperiencePoints),
            BaseSkillCodes = baseSkillResolution.Codes,
            CurrentSkillCodes = currentSkillResolution.Codes,
            BaseCharacteristicCodes = baseCharacteristicResolution.Codes,
            CurrentCharacteristicCodes = currentCharacteristicResolution.Codes,
            BaseEquipmentCodes = baseEquipmentResolution.Codes,
            CurrentEquipmentCodes = currentEquipmentResolution.Codes,
            BaseWeaponCodes = baseWeaponResolution.Codes,
            CurrentWeaponCodes = currentWeaponResolution.Codes,
            CustomSkills = customSkills,
            CustomCharacteristics = customCharacteristics,
            CustomEquipment = customEquipment,
            CustomWeapons = customWeapons
        };
    }

    private static SerializedCompanyEntry CreatePeripheralEntry<TEntry>(
        TEntry entry,
        int entryIndex,
        int parentEntryIndex,
        IArmyDataService armyDataService,
        Dictionary<(int FactionId, string Section), Dictionary<string, int>> codeIdLookupCache,
        Dictionary<int, Dictionary<string, int>> extrasIdLookupCache)
        where TEntry : class, ICompanyMercsEntry
    {
        var peripheralName = ResolvePeripheralEntryName(entry.PeripheralNameHeading, entry.Name);
        var peripheralSkillResolution = ResolveCodes(
            armyDataService,
            codeIdLookupCache,
            extrasIdLookupCache,
            SplitCodes(entry.SavedPeripheralSkills),
            "skills",
            [entry.SourceFactionId]);
        var peripheralEquipmentResolution = ResolveCodes(
            armyDataService,
            codeIdLookupCache,
            extrasIdLookupCache,
            SplitCodes(entry.SavedPeripheralEquipment),
            "equip",
            [entry.SourceFactionId]);
        var peripheralCharacteristicResolution = ResolveCodes(
            armyDataService,
            codeIdLookupCache,
            extrasIdLookupCache,
            SplitCodes(entry.SavedPeripheralCharacteristics),
            "chars",
            [entry.SourceFactionId]);
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
            ExperiencePoints = 0,
            BaseProfileHumanReadable = peripheralName,
            IsCaptain = false,
            FactionId = entry.SourceFactionId,
            ProfileId = entry.SourceUnitId,
            Renown = 0,
            BaseCr = 0,
            CalculatedCr = 0,
            BaseMov = entry.PeripheralMov,
            BaseCc = entry.PeripheralCc,
            BaseBs = entry.PeripheralBs,
            BasePh = entry.PeripheralPh,
            BaseWip = entry.PeripheralWip,
            BaseArm = entry.PeripheralArm,
            BaseBts = entry.PeripheralBts,
            BaseVitaOrStr = entry.PeripheralVitality,
            BaseS = entry.PeripheralS,
            CurrentMov = entry.PeripheralMov,
            CurrentCc = entry.PeripheralCc,
            CurrentBs = entry.PeripheralBs,
            CurrentPh = entry.PeripheralPh,
            CurrentWip = entry.PeripheralWip,
            CurrentArm = entry.PeripheralArm,
            CurrentBts = entry.PeripheralBts,
            CurrentVitaOrStr = entry.PeripheralVitality,
            CurrentS = entry.PeripheralS,
            Xp = 0,
            BaseSkillCodes = peripheralSkillResolution.Codes,
            CurrentSkillCodes = peripheralSkillResolution.Codes,
            BaseCharacteristicCodes = peripheralCharacteristicResolution.Codes,
            CurrentCharacteristicCodes = peripheralCharacteristicResolution.Codes,
            BaseEquipmentCodes = peripheralEquipmentResolution.Codes,
            CurrentEquipmentCodes = peripheralEquipmentResolution.Codes,
            BaseWeaponCodes = [],
            CurrentWeaponCodes = [],
            CustomSkills = peripheralSkillResolution.CustomNames,
            CustomCharacteristics = peripheralCharacteristicResolution.CustomNames,
            CustomEquipment = peripheralEquipmentResolution.CustomNames,
            CustomWeapons = []
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

    private static string ReadStatValue(string? subtitle, string label, string fallback = "-")
    {
        if (string.IsNullOrWhiteSpace(subtitle))
        {
            return NormalizeStatValue(fallback);
        }

        var parts = subtitle.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var part in parts)
        {
            if (!part.StartsWith(label + " ", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var value = part[(label.Length + 1)..].Trim();
            return NormalizeStatValue(value);
        }

        return NormalizeStatValue(fallback);
    }

    private static string ReadVitaOrStrValue(string? subtitle)
    {
        var vita = ReadStatValue(subtitle, "VITA", string.Empty);
        if (!string.IsNullOrWhiteSpace(vita) && vita != "-")
        {
            return vita;
        }

        var str = ReadStatValue(subtitle, "STR", string.Empty);
        return NormalizeStatValue(str);
    }

    private static string NormalizeStatValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "-";
        }

        return value.Trim();
    }

    private static List<string> SplitCodes(string? line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return [];
        }

        return line
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x) && x != "-")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string BuildCaptainCurrentStat(string baseValue, int tier, int bonus)
    {
        if (!int.TryParse(baseValue, out var parsedBase))
        {
            return baseValue;
        }

        var adjustedValue = parsedBase + bonus;
        return adjustedValue.ToString(CultureInfo.InvariantCulture);
    }

    private static List<string> AppendChoices(List<string> existing, params string[] choices)
    {
        var result = existing
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .ToList();

        foreach (var choice in choices)
        {
            if (string.IsNullOrWhiteSpace(choice))
            {
                continue;
            }

            var normalized = choice.Trim();
            if (normalized == "-")
            {
                continue;
            }

            if (!result.Any(x => string.Equals(x, normalized, StringComparison.OrdinalIgnoreCase)))
            {
                result.Add(normalized);
            }
        }

        return result;
    }

    private static (List<CompanySavedCodeRef> Codes, List<string> CustomNames) ResolveCodes(
        IArmyDataService armyDataService,
        Dictionary<(int FactionId, string Section), Dictionary<string, int>> codeIdLookupCache,
        Dictionary<int, Dictionary<string, int>> extrasIdLookupCache,
        IEnumerable<string> names,
        string section,
        IEnumerable<int> factionIds)
    {
        var distinctFactionIds = factionIds
            .Where(x => x > 0)
            .Distinct()
            .ToList();
        var resolved = new List<CompanySavedCodeRef>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var custom = new List<string>();
        var seenCustom = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var name in names.Where(x => !string.IsNullOrWhiteSpace(x)))
        {
            var trimmedName = name.Trim();
            var fullyConverted = false;
            var resolvedId = TryResolveCodeId(
                armyDataService,
                codeIdLookupCache,
                NormalizeLookupKey(trimmedName),
                section,
                distinctFactionIds);

            if (resolvedId.HasValue)
            {
                var code = new CompanySavedCodeRef { Id = resolvedId.Value };
                var key = BuildCodeRefKey(code);
                if (seen.Add(key))
                {
                    resolved.Add(code);
                }

                fullyConverted = true;
                continue;
            }

            var parsed = TrySplitBaseAndExtraNames(trimmedName);
            if (parsed is null)
            {
                fullyConverted = false;
            }
            else
            {
                var baseResolvedId = TryResolveCodeId(
                    armyDataService,
                    codeIdLookupCache,
                    NormalizeLookupKey(parsed.Value.BaseName),
                    section,
                    distinctFactionIds);

                if (!baseResolvedId.HasValue)
                {
                    fullyConverted = false;
                }
                else
                {
                    var unresolvedExtra = false;
                    var extraIds = new List<int>();
                    foreach (var extraName in parsed.Value.ExtraNames)
                    {
                        var resolvedExtraId = TryResolveExtraId(
                            armyDataService,
                            extrasIdLookupCache,
                            extraName,
                            distinctFactionIds);
                        if (!resolvedExtraId.HasValue)
                        {
                            unresolvedExtra = true;
                            continue;
                        }

                        extraIds.Add(resolvedExtraId.Value);
                    }

                    var distinctExtraIds = extraIds
                        .Distinct()
                        .OrderBy(x => x)
                        .ToList();
                    var parsedCode = new CompanySavedCodeRef
                    {
                        Id = baseResolvedId.Value,
                        Extra = distinctExtraIds.Count == 0 ? null : distinctExtraIds
                    };

                    var parsedKey = BuildCodeRefKey(parsedCode);
                    if (seen.Add(parsedKey))
                    {
                        resolved.Add(parsedCode);
                    }

                    fullyConverted = !unresolvedExtra;
                }
            }

            // Keep custom text only when conversion was incomplete.
            if (!fullyConverted && seenCustom.Add(trimmedName))
            {
                custom.Add(trimmedName);
            }
        }

        return (resolved, custom);
    }

    private static int? TryResolveCodeId(
        IArmyDataService armyDataService,
        Dictionary<(int FactionId, string Section), Dictionary<string, int>> codeIdLookupCache,
        string normalizedName,
        string section,
        IReadOnlyList<int> factionIds)
    {
        foreach (var factionId in factionIds)
        {
            var lookup = GetCodeIdLookupForFaction(armyDataService, codeIdLookupCache, factionId, section);
            if (lookup.TryGetValue(normalizedName, out var id))
            {
                return id;
            }
        }

        return null;
    }

    private static int? TryResolveExtraId(
        IArmyDataService armyDataService,
        Dictionary<int, Dictionary<string, int>> extrasIdLookupCache,
        string rawName,
        IReadOnlyList<int> factionIds)
    {
        if (string.IsNullOrWhiteSpace(rawName))
        {
            return null;
        }

        var normalizedCandidates = BuildExtraLookupCandidates(rawName);
        if (normalizedCandidates.Count == 0)
        {
            return null;
        }

        foreach (var factionId in factionIds)
        {
            var lookup = GetExtraIdLookupForFaction(armyDataService, extrasIdLookupCache, factionId);
            foreach (var normalizedName in normalizedCandidates)
            {
                if (lookup.TryGetValue(normalizedName, out var id))
                {
                    return id;
                }
            }
        }

        return null;
    }

    private static Dictionary<string, int> GetCodeIdLookupForFaction(
        IArmyDataService armyDataService,
        Dictionary<(int FactionId, string Section), Dictionary<string, int>> codeIdLookupCache,
        int factionId,
        string section)
    {
        var key = (FactionId: factionId, Section: section);
        if (codeIdLookupCache.TryGetValue(key, out var existing))
        {
            return existing;
        }

        var snapshot = armyDataService.GetFactionSnapshot(factionId);
        var idToName = CompanyUnitDetailsShared.BuildIdNameLookup(snapshot?.FiltersJson, section);
        var nameToId = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var (id, name) in idToName)
        {
            var normalized = NormalizeLookupKey(name);
            if (string.IsNullOrWhiteSpace(normalized) || nameToId.ContainsKey(normalized))
            {
                continue;
            }

            nameToId[normalized] = id;
        }

        codeIdLookupCache[key] = nameToId;
        return nameToId;
    }

    private static Dictionary<string, int> GetExtraIdLookupForFaction(
        IArmyDataService armyDataService,
        Dictionary<int, Dictionary<string, int>> extrasIdLookupCache,
        int factionId)
    {
        if (extrasIdLookupCache.TryGetValue(factionId, out var existing))
        {
            return existing;
        }

        var snapshot = armyDataService.GetFactionSnapshot(factionId);
        var idToName = CompanyUnitDetailsShared.BuildIdNameLookup(snapshot?.FiltersJson, "extras");
        var nameToId = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var (id, name) in idToName)
        {
            var normalized = NormalizeLookupKey(name);
            if (string.IsNullOrWhiteSpace(normalized) || nameToId.ContainsKey(normalized))
            {
                continue;
            }

            nameToId[normalized] = id;
        }

        extrasIdLookupCache[factionId] = nameToId;
        return nameToId;
    }

    private static (string BaseName, List<string> ExtraNames)? TrySplitBaseAndExtraNames(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        var openParenIndex = trimmed.IndexOf(" (", StringComparison.Ordinal);
        if (openParenIndex <= 0 || !trimmed.EndsWith(')'))
        {
            return null;
        }

        var baseName = trimmed[..openParenIndex].Trim();
        var inside = trimmed[(openParenIndex + 2)..^1].Trim();
        if (string.IsNullOrWhiteSpace(baseName) || string.IsNullOrWhiteSpace(inside))
        {
            return null;
        }

        var extras = SplitTopLevelExtras(inside);
        if (extras.Count == 0)
        {
            return null;
        }

        return (baseName, extras);
    }

    private static List<string> SplitTopLevelExtras(string value)
    {
        var result = new List<string>();
        if (string.IsNullOrWhiteSpace(value))
        {
            return result;
        }

        var depth = 0;
        var tokenStart = 0;
        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];
            switch (c)
            {
                case '(':
                    depth++;
                    break;
                case ')':
                    if (depth > 0)
                    {
                        depth--;
                    }

                    break;
                case ',' when depth == 0:
                {
                    var token = value[tokenStart..i].Trim();
                    if (!string.IsNullOrWhiteSpace(token))
                    {
                        result.Add(token);
                    }

                    tokenStart = i + 1;
                    break;
                }
            }
        }

        var last = value[tokenStart..].Trim();
        if (!string.IsNullOrWhiteSpace(last))
        {
            result.Add(last);
        }

        return result;
    }

    private static string BuildCodeRefKey(CompanySavedCodeRef value)
    {
        var extra = value.Extra is null || value.Extra.Count == 0
            ? string.Empty
            : string.Join(",", value.Extra.OrderBy(x => x));
        return $"{value.Id}|{extra}";
    }

    private static string NormalizeLookupKey(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = Regex.Replace(value.Trim(), @"\s+", " ");
        return normalized.ToLowerInvariant();
    }

    private static List<string> BuildExtraLookupCandidates(string rawValue)
    {
        var candidates = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void Add(string? value)
        {
            var normalized = NormalizeLookupKey(value);
            if (string.IsNullOrWhiteSpace(normalized) || !seen.Add(normalized))
            {
                return;
            }

            candidates.Add(normalized);
        }

        Add(rawValue);
        Add(CompanySelectionSharedUtilities.ConvertDistanceText(rawValue, showUnitsInInches: true));
        Add(CompanySelectionSharedUtilities.ConvertDistanceText(rawValue, showUnitsInInches: false));

        return candidates;
    }

    private static string ResolveUnitTypeCode<TEntry>(TEntry entry, IArmyDataService armyDataService)
        where TEntry : class, ICompanyMercsEntry
    {
        if (!string.IsNullOrWhiteSpace(entry.UnitTypeCode))
        {
            return entry.UnitTypeCode.Trim().ToUpperInvariant();
        }

        var fromSubtitle = CompanyStartSharedState.ExtractUnitTypeCode(entry.Subtitle);
        if (!string.IsNullOrWhiteSpace(fromSubtitle))
        {
            return fromSubtitle.Trim().ToUpperInvariant();
        }

        if (entry.SourceFactionId <= 0 || entry.SourceUnitId <= 0)
        {
            return string.Empty;
        }

        var resume = armyDataService
            .GetResumeByFactionMercsOnly(entry.SourceFactionId)
            .FirstOrDefault(x => x.UnitId == entry.SourceUnitId);
        if (resume?.Type is not int resumeType)
        {
            return string.Empty;
        }

        var snapshot = armyDataService.GetFactionSnapshot(entry.SourceFactionId);
        var typeLookup = CompanyUnitDetailsShared.BuildIdNameLookup(snapshot?.FiltersJson, "type");
        if (!typeLookup.TryGetValue(resumeType, out var typeName) || string.IsNullOrWhiteSpace(typeName))
        {
            return string.Empty;
        }

        return typeName.Trim().ToUpperInvariant();
    }

    private static int ParseCompanyIndexFromFileName(string filePath)
    {
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);
        if (string.IsNullOrWhiteSpace(fileNameWithoutExtension))
        {
            return 1;
        }

        var match = Regex.Match(fileNameWithoutExtension, @"-(\d+)$");
        return match.Success && int.TryParse(match.Groups[1].Value, out var parsed) && parsed > 0
            ? parsed
            : 1;
    }

    private sealed class SerializedCompanyEntry
    {
        public int EntryIndex { get; init; }
        [JsonPropertyName("Name")]
        public string Name { get; init; } = string.Empty;
        [JsonPropertyName("Base Profile (Human Readable)")]
        public string BaseProfileHumanReadable { get; init; } = string.Empty;
        [JsonPropertyName("IsCaptain")]
        public bool IsCaptain { get; init; }
        [JsonPropertyName("FactionId")]
        public int FactionId { get; init; }
        [JsonPropertyName("ProfileId")]
        public int ProfileId { get; init; }
        [JsonPropertyName("Renown")]
        public int Renown { get; init; }
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
        [JsonPropertyName("Current MOV")]
        public string CurrentMov { get; init; } = "-";
        [JsonPropertyName("Current CC")]
        public string CurrentCc { get; init; } = "-";
        [JsonPropertyName("Current BS")]
        public string CurrentBs { get; init; } = "-";
        [JsonPropertyName("Current PH")]
        public string CurrentPh { get; init; } = "-";
        [JsonPropertyName("Current WIP")]
        public string CurrentWip { get; init; } = "-";
        [JsonPropertyName("Current ARM")]
        public string CurrentArm { get; init; } = "-";
        [JsonPropertyName("Current BTS")]
        public string CurrentBts { get; init; } = "-";
        [JsonPropertyName("Current VITA/STR")]
        public string CurrentVitaOrStr { get; init; } = "-";
        [JsonPropertyName("Current S")]
        public string CurrentS { get; init; } = "-";
        [JsonPropertyName("XP")]
        public int Xp { get; init; }
        [JsonPropertyName("BaseSkillCodes")]
        public List<CompanySavedCodeRef> BaseSkillCodes { get; init; } = [];
        [JsonPropertyName("CurrentSkillCodes")]
        public List<CompanySavedCodeRef> CurrentSkillCodes { get; init; } = [];
        [JsonPropertyName("BaseCharacteristicCodes")]
        public List<CompanySavedCodeRef> BaseCharacteristicCodes { get; init; } = [];
        [JsonPropertyName("CurrentCharacteristicCodes")]
        public List<CompanySavedCodeRef> CurrentCharacteristicCodes { get; init; } = [];
        [JsonPropertyName("BaseEquipmentCodes")]
        public List<CompanySavedCodeRef> BaseEquipmentCodes { get; init; } = [];
        [JsonPropertyName("CurrentEquipmentCodes")]
        public List<CompanySavedCodeRef> CurrentEquipmentCodes { get; init; } = [];
        [JsonPropertyName("BaseWeaponCodes")]
        public List<CompanySavedCodeRef> BaseWeaponCodes { get; init; } = [];
        [JsonPropertyName("CurrentWeaponCodes")]
        public List<CompanySavedCodeRef> CurrentWeaponCodes { get; init; } = [];
        [JsonPropertyName("Custom Weapons")]
        public List<string> CustomWeapons { get; init; } = [];
        [JsonPropertyName("Custom Skills")]
        public List<string> CustomSkills { get; init; } = [];
        [JsonPropertyName("Custom Characteristics")]
        public List<string> CustomCharacteristics { get; init; } = [];
        [JsonPropertyName("Custom Equipment")]
        public List<string> CustomEquipment { get; init; } = [];
        [JsonPropertyName("Rank1Skill")]
        public string Rank1Skill { get; init; } = string.Empty;
        [JsonPropertyName("Rank2Skill")]
        public string Rank2Skill { get; init; } = string.Empty;
        [JsonPropertyName("Rank3Skill")]
        public string Rank3Skill { get; init; } = string.Empty;
        [JsonPropertyName("Rank4Skill")]
        public string Rank4Skill { get; init; } = string.Empty;
        [JsonPropertyName("Rank5Skill")]
        public string Rank5Skill { get; init; } = string.Empty;
        [JsonPropertyName("Rank6Skill")]
        public string Rank6Skill { get; init; } = string.Empty;
        [JsonPropertyName("Rank7Skill")]
        public string Rank7Skill { get; init; } = string.Empty;
        [JsonPropertyName("EquippedPrimary")]
        public string EquippedPrimary { get; init; } = string.Empty;
        [JsonPropertyName("EquippedSecondary")]
        public string EquippedSecondary { get; init; } = string.Empty;
        [JsonPropertyName("EquippedSideArm")]
        public string EquippedSideArm { get; init; } = string.Empty;
        [JsonPropertyName("EquippedAccessories")]
        public string EquippedAccessories { get; init; } = string.Empty;
        [JsonPropertyName("EquippedRoles")]
        public string EquippedRoles { get; init; } = string.Empty;
        [JsonPropertyName("EquippedArmor")]
        public string EquippedArmor { get; init; } = string.Empty;
        [JsonPropertyName("EquippedAugment")]
        public string EquippedAugment { get; init; } = string.Empty;
        [JsonPropertyName("EquippedOther")]
        public string EquippedOther { get; init; } = string.Empty;

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


