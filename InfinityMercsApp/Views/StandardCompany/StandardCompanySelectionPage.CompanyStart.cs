using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using InfinityMercsApp.Views.Templates.NewCompany;
using InfinityMercsApp.Views.Templates.UICommon;

namespace InfinityMercsApp.Views.StandardCompany;

/// <summary>
/// Company start/save workflow.
/// </summary>
public partial class StandardCompanySelectionPage
{
    /// <summary>
    /// Handles is company name valid.
    /// </summary>
    private bool IsCompanyNameValid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return !string.Equals(value.Trim(), "Company Name", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Handles set company name validation error.
    /// </summary>
    private void SetCompanyNameValidationError(bool showError)
    {
        ShowCompanyNameValidationError = showError;
        CompanyNameBorderColor = showError ? Color.FromArgb("#EF4444") : Color.FromArgb("#6B7280");
    }

    /// <summary>
    /// Handles start company async.
    /// </summary>
    protected override async Task StartCompanyAsync()
    {
        if (!IsCompanyNameValid(CompanyName))
        {
            SetCompanyNameValidationError(true);
            return;
        }

        SetCompanyNameValidationError(false);

        try
        {
            var captainEntry = MercsCompanyEntries.FirstOrDefault(x => x.IsLieutenant) ?? MercsCompanyEntries.FirstOrDefault();
            if (captainEntry is null)
            {
                await DisplayAlert("Save Failed", "Add at least one unit before starting a company.", "OK");
                return;
            }

            var improvedCaptainStats = await CompanyCaptainWorkflowService.ShowCaptainConfigurationAsync<SavedImprovedCaptainStats>(
                new CompanyCaptainWorkflowRequest
                {
                    Navigation = Navigation,
                    FallbackSourceFactionId = captainEntry.SourceFactionId,
                    FirstSourceFactionId = GetUnitSourceFactions().FirstOrDefault()?.Id,
                    UnitName = captainEntry.Name,
                    UnitCost = captainEntry.CostValue,
                    UnitStatline = captainEntry.Subtitle ?? "-",
                    UnitRangedWeapons = captainEntry.SavedRangedWeapons,
                    UnitCcWeapons = captainEntry.SavedCcWeapons,
                    UnitSkills = captainEntry.SavedSkills,
                    UnitEquipment = captainEntry.SavedEquipment,
                    UnitCachedLogoPath = captainEntry.CachedLogoPath,
                    UnitPackagedLogoPath = captainEntry.PackagedLogoPath,
                    TryGetParentFactionId = factionId => Factions.FirstOrDefault(x => x.Id == factionId)?.ParentId,
                    TryGetFactionName = factionId => Factions.FirstOrDefault(x => x.Id == factionId)?.Name,
                    TryGetMetadataFactionName = factionId => _armyDataService.GetMetadataFactionById(factionId)?.Name,
                    ArmyDataService = _armyDataService,
                    SpecOpsProvider = _specOpsProvider,
                    ShowUnitsInInches = ShowUnitsInInches
                });
            if (improvedCaptainStats is null)
            {
                return;
            }

            var now = DateTimeOffset.UtcNow;
            var companyName = CompanyName.Trim();
            var companyType = GetCompanyTypeLabel();
            var safeFileName = Regex.Replace(companyName.ToLowerInvariant(), @"[^a-z0-9]+", "-").Trim('-');
            if (string.IsNullOrWhiteSpace(safeFileName))
            {
                safeFileName = "company";
            }

            var saveDir = Path.Combine(FileSystem.Current.AppDataDirectory, "MercenaryRecords");
            Directory.CreateDirectory(saveDir);
            var companyIndex = GetNextCompanyIndex(saveDir, companyName, safeFileName);
            var fileName = $"{safeFileName}-{companyIndex:D4}.json";

            var payload = new SavedCompanyFile
            {
                CompanyName = companyName,
                CompanyType = companyType,
                CompanyIdentifier = ComputeCompanyIdentifier(fileName),
                CompanyIndex = companyIndex,
                CreatedUtc = now.ToString("O", CultureInfo.InvariantCulture),
                PointsLimit = int.TryParse(SelectedStartSeasonPoints, out var pointsLimit) ? pointsLimit : 0,
                CurrentPoints = int.TryParse(SeasonPointsCapText, out var currentPoints) ? currentPoints : 0,
                ImprovedCaptainStats = improvedCaptainStats,
                SourceFactions = GetUnitSourceFactions()
                    .Select(faction => new SavedCompanyFaction
                    {
                        FactionId = faction.Id,
                        FactionName = faction.Name
                    })
                    .ToList(),
                Entries = MercsCompanyEntries.Select((entry, entryIndex) => new SavedCompanyEntry
                {
                    EntryIndex = entryIndex,
                    Name = entry.Name,
                    BaseUnitName = entry.Name,
                    CustomName = entry.IsLieutenant
                        ? (string.IsNullOrWhiteSpace(improvedCaptainStats.CaptainName) ? "Captain" : improvedCaptainStats.CaptainName.Trim())
                        : "Trooper",
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
                }).ToList()
            };

            var filePath = Path.Combine(saveDir, fileName);
            await File.WriteAllTextAsync(
                filePath,
                JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));

            var encodedPath = Uri.EscapeDataString(filePath);
            await Shell.Current.GoToAsync($"//{nameof(CompanyViewerPage)}?companyFilePath={encodedPath}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ArmyFactionSelectionPage StartCompanyAsync failed: {ex}");
            await DisplayAlert("Save Failed", ex.Message, "OK");
        }
    }

    /// <summary>
    /// Handles extract unit type code.
    /// </summary>
    private static string ExtractUnitTypeCode(string? subtitle)
    {
        if (string.IsNullOrWhiteSpace(subtitle))
        {
            return string.Empty;
        }

        var firstToken = subtitle
            .Split([' ', '-', '–', '—'], StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault();
        return string.IsNullOrWhiteSpace(firstToken) ? string.Empty : firstToken.Trim().ToUpperInvariant();
    }

    /// <summary>
    /// Handles update mercs company total.
    /// </summary>
    private void UpdateMercsCompanyTotal()
    {
        var totalCost = MercsCompanyEntries.Sum(x => x.CostValue);
        SeasonPointsCapText = totalCost.ToString();
    }

    /// <summary>
    /// Handles refresh mercs company entry distance displays.
    /// </summary>
    private void RefreshMercsCompanyEntryDistanceDisplays()
    {
        foreach (var entry in MercsCompanyEntries)
        {
            var moveDisplay = FormatMoveValue(entry.UnitMoveFirstCm, entry.UnitMoveSecondCm);
            entry.UnitMoveDisplay = moveDisplay;
            entry.Subtitle = ReplaceSubtitleMoveDisplay(entry.Subtitle, moveDisplay);

            if (entry.HasPeripheralStatBlock)
            {
                entry.PeripheralMov = FormatMoveValue(entry.PeripheralMoveFirstCm, entry.PeripheralMoveSecondCm);
            }
        }
    }

    /// <summary>
    /// Handles compute company identifier.
    /// </summary>
    private static string ComputeCompanyIdentifier(string fileName)
    {
        return CompanySelectionSharedUtilities.ComputeCompanyIdentifier(fileName);
    }

    /// <summary>
    /// Handles get next company index.
    /// </summary>
    private static int GetNextCompanyIndex(string saveDir, string companyName, string safeFileName)
    {
        return CompanySelectionSharedUtilities.GetNextCompanyIndex(saveDir, companyName, safeFileName);
    }

    /// <summary>
    /// Handles get company type label.
    /// </summary>
    private string GetCompanyTypeLabel()
    {
        return _mode switch
        {
            ArmySourceSelectionMode.VanillaFactions => "Standard Company - Vanilla",
            ArmySourceSelectionMode.Sectorials => "Standard Company - Sectorial",
            _ => "Unknown Company Type"
        };
    }

    /// <summary>
    /// Handles update season validation state.
    /// </summary>
    private void UpdateSeasonValidationState()
    {
        var hasLieutenant = MercsCompanyEntries.Any(x => x.IsLieutenant);
        var pointsLimit = int.TryParse(SelectedStartSeasonPoints, out var parsedLimit) ? parsedLimit : 0;
        var currentPoints = int.TryParse(SeasonPointsCapText, out var parsedPoints) ? parsedPoints : 0;
        var shouldShowCheck = hasLieutenant && currentPoints <= pointsLimit;
        IsCompanyValid = shouldShowCheck;
    }

    /// <summary>
    /// Handles parse cost value.
    /// </summary>
    private static int ParseCostValue(string? cost)
    {
        return CompanySelectionSharedUtilities.ParseCostValue(cost);
    }
}



