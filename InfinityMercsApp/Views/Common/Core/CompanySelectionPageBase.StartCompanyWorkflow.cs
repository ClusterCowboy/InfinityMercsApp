using InfinityMercsApp.Infrastructure.Providers;
using InfinityMercsApp.Services;
using InfinityMercsApp.Views;

namespace InfinityMercsApp.Views.Common;

public abstract partial class CompanySelectionPageBase
{
    /// <summary>
    /// Extracts the short unit type code (e.g. "LT", "HVT") from a subtitle string.
    /// Returns an empty string when the subtitle is null or contains no recognisable code.
    /// </summary>
    protected static string ExtractUnitTypeCode(string? subtitle)
    {
        return CompanyStartSharedState.ExtractUnitTypeCode(subtitle);
    }

    /// <summary>
    /// Applies or clears the company name validation error visual by updating the border colour
    /// and validation-error flag through the provided setter delegates.
    /// </summary>
    protected static void ApplyCompanyNameValidationError(
        bool showError,
        Action<bool> setShowCompanyNameValidationError,
        Action<Color> setCompanyNameBorderColor)
    {
        CompanyStartSharedState.ApplyCompanyNameValidationError(
            showError,
            setShowCompanyNameValidationError,
            setCompanyNameBorderColor);
    }

    /// <summary>
    /// Convenience overload that routes the validation error to the abstract setters
    /// defined by the concrete page, avoiding the need to pass delegates explicitly.
    /// </summary>
    protected void SetCompanyNameValidationError(bool showError)
    {
        ApplyCompanyNameValidationError(
            showError,
            SetShowCompanyNameValidationError,
            SetCompanyNameBorderColor);
    }

    /// <summary>
    /// Computes the display text for the total points cost of all roster entries.
    /// </summary>
    protected static string ComputeMercsCompanyTotalCostText<TEntry>(IEnumerable<TEntry> entries)
        where TEntry : class, ICompanyMercsEntry
    {
        return CompanyStartSharedState.ComputeTotalCostText(entries);
    }

    /// <summary>
    /// Re-evaluates the movement distance display strings for all roster entries,
    /// applying the current inches/cm preference via <paramref name="formatMoveValue"/>.
    /// </summary>
    protected static void RefreshMercsCompanyEntryDistanceDisplays<TEntry>(
        IEnumerable<TEntry> entries,
        Func<int?, int?, string> formatMoveValue)
        where TEntry : class, ICompanyMercsEntry
    {
        CompanyStartSharedState.RefreshMercsCompanyEntryDistanceDisplays(entries, formatMoveValue);
    }

    /// <summary>
    /// Returns <c>true</c> when all roster entries fit within the selected season points cap.
    /// </summary>
    protected static bool IsCompanySeasonValid<TEntry>(
        IEnumerable<TEntry> entries,
        string selectedStartSeasonPoints,
        string seasonPointsCapText)
        where TEntry : class, ICompanyMercsEntry
    {
        return CompanyStartSharedState.IsSeasonValid(entries, selectedStartSeasonPoints, seasonPointsCapText);
    }

    /// <summary>
    /// Orchestrates the full "found company" save workflow: validates the company name,
    /// serialises the roster to disk, and navigates to the company viewer on success.
    /// Displays an error alert and logs on failure.
    /// </summary>
    protected async Task ExecuteStartCompanyAsync<TFaction, TEntry, TCaptainStats>(
        string? companyName,
        IEnumerable<TEntry> mercsCompanyEntries,
        bool showRightSelectionBox,
        TFaction? leftSlotFaction,
        TFaction? rightSlotFaction,
        IEnumerable<TFaction> factions,
        ISpecOpsProvider specOpsProvider,
        bool showUnitsInInches,
        string selectedStartSeasonPoints,
        string seasonPointsCapText,
        Func<int, string?> tryGetMetadataFactionName,
        Func<TCaptainStats, string?> readCaptainName)
        where TFaction : class, ICompanySourceFaction
        where TEntry : class, ICompanyMercsEntry
        where TCaptainStats : class
    {
        await CompanyStartExecutionWorkflow.ExecuteAsync<TFaction, TEntry, TCaptainStats>(
            new CompanyStartExecutionRequest<TFaction, TEntry, TCaptainStats>
            {
                CompanyName = companyName,
                SetCompanyNameValidationError = SetCompanyNameValidationError,
                BuildSaveRequest = () => new CompanyStartSaveRequest<TFaction, TEntry, TCaptainStats>
                {
                    CompanyName = (companyName ?? string.Empty).Trim(),
                    CompanyType = CompanyTypeLabel,
                    MercsCompanyEntries = mercsCompanyEntries,
                    ShowRightSelectionBox = showRightSelectionBox,
                    LeftSlotFaction = leftSlotFaction,
                    RightSlotFaction = rightSlotFaction,
                    Factions = factions,
                    ArmyDataService = ArmyDataService,
                    SpecOpsProvider = specOpsProvider,
                    Navigation = Navigation,
                    ShowUnitsInInches = showUnitsInInches,
                    SelectedStartSeasonPoints = selectedStartSeasonPoints,
                    SeasonPointsCapText = seasonPointsCapText,
                    TryGetMetadataFactionName = tryGetMetadataFactionName,
                    ReadCaptainName = readCaptainName,
                    DisplayAlertAsync = (title, message, cancel) => DisplayAlert(title, message, cancel),
                    // URI-encode the file path so it can be passed safely as a Shell query parameter.
                    NavigateToCompanyViewerAsync = async filePath =>
                    {
                        var encodedPath = Uri.EscapeDataString(filePath);
                        await Shell.Current.GoToAsync($"//{nameof(CompanyViewerPage)}?companyFilePath={encodedPath}");
                    }
                },
                HandleFailureAsync = async ex =>
                {
                    Console.Error.WriteLine($"CompanySelectionPage StartCompanyAsync failed: {ex}");
                    await DisplayAlert("Save Failed", ex.Message, "OK");
                }
            });
    }
}
