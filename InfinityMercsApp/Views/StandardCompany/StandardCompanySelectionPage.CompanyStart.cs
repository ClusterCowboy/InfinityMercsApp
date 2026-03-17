using InfinityMercsApp.Views.Templates.NewCompany;

namespace InfinityMercsApp.Views.StandardCompany;

/// <summary>
/// Company start/save workflow.
/// </summary>
public partial class StandardCompanySelectionPage
{
    /// <summary>
    /// Handles is company name valid.
    /// </summary>
    private static bool IsCompanyNameValid(string? value)
    {
        return CompanyStartSharedState.IsCompanyNameValid(value);
    }

    /// <summary>
    /// Handles set company name validation error.
    /// </summary>
    private void SetCompanyNameValidationError(bool showError)
    {
        ShowCompanyNameValidationError = showError;
        CompanyNameBorderColor = CompanyStartSharedState.GetCompanyNameBorderColor(showError);
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
            await CompanyStartSaveWorkflow.RunAsync<ArmyFactionSelectionItem, MercsCompanyEntry, SavedImprovedCaptainStats>(
                new CompanyStartSaveRequest<ArmyFactionSelectionItem, MercsCompanyEntry, SavedImprovedCaptainStats>
                {
                    CompanyName = CompanyName.Trim(),
                    CompanyType = GetCompanyTypeLabel(),
                    MercsCompanyEntries = MercsCompanyEntries,
                    ShowRightSelectionBox = ShowRightSelectionBox,
                    LeftSlotFaction = _factionSelectionState.LeftSlotFaction,
                    RightSlotFaction = _factionSelectionState.RightSlotFaction,
                    Factions = Factions,
                    ArmyDataService = _armyDataService,
                    SpecOpsProvider = _specOpsProvider,
                    Navigation = Navigation,
                    ShowUnitsInInches = ShowUnitsInInches,
                    SelectedStartSeasonPoints = SelectedStartSeasonPoints,
                    SeasonPointsCapText = SeasonPointsCapText,
                    TryGetMetadataFactionName = factionId => _armyDataService.GetMetadataFactionById(factionId)?.Name,
                    ReadCaptainName = stats => stats.CaptainName,
                    DisplayAlertAsync = (title, message, cancel) => DisplayAlert(title, message, cancel),
                    NavigateToCompanyViewerAsync = async filePath =>
                    {
                        var encodedPath = Uri.EscapeDataString(filePath);
                        await Shell.Current.GoToAsync($"//{nameof(CompanyViewerPage)}?companyFilePath={encodedPath}");
                    }
                });
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
        return CompanyStartSharedState.ExtractUnitTypeCode(subtitle);
    }

    /// <summary>
    /// Handles update mercs company total.
    /// </summary>
    private void UpdateMercsCompanyTotal()
    {
        SeasonPointsCapText = CompanyStartSharedState.ComputeTotalCostText(MercsCompanyEntries);
    }

    /// <summary>
    /// Handles refresh mercs company entry distance displays.
    /// </summary>
    private void RefreshMercsCompanyEntryDistanceDisplays()
    {
        CompanyStartSharedState.RefreshMercsCompanyEntryDistanceDisplays(MercsCompanyEntries, FormatMoveValue);
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
        IsCompanyValid = CompanyStartSharedState.IsSeasonValid(
            MercsCompanyEntries,
            SelectedStartSeasonPoints,
            SeasonPointsCapText);
    }
}
