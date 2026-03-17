using InfinityMercsApp.Infrastructure.Providers;
using InfinityMercsApp.Services;
using InfinityMercsApp.Views;

namespace InfinityMercsApp.Views.Common;

public abstract partial class CompanySelectionPageBase
{
    protected static string ExtractUnitTypeCode(string? subtitle)
    {
        return CompanyStartSharedState.ExtractUnitTypeCode(subtitle);
    }

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

    protected void SetCompanyNameValidationError(bool showError)
    {
        ApplyCompanyNameValidationError(
            showError,
            SetShowCompanyNameValidationError,
            SetCompanyNameBorderColor);
    }

    protected static string ComputeMercsCompanyTotalCostText<TEntry>(IEnumerable<TEntry> entries)
        where TEntry : class, ICompanyMercsEntry
    {
        return CompanyStartSharedState.ComputeTotalCostText(entries);
    }

    protected static void RefreshMercsCompanyEntryDistanceDisplays<TEntry>(
        IEnumerable<TEntry> entries,
        Func<int?, int?, string> formatMoveValue)
        where TEntry : class, ICompanyMercsEntry
    {
        CompanyStartSharedState.RefreshMercsCompanyEntryDistanceDisplays(entries, formatMoveValue);
    }

    protected static bool IsCompanySeasonValid<TEntry>(
        IEnumerable<TEntry> entries,
        string selectedStartSeasonPoints,
        string seasonPointsCapText)
        where TEntry : class, ICompanyMercsEntry
    {
        return CompanyStartSharedState.IsSeasonValid(entries, selectedStartSeasonPoints, seasonPointsCapText);
    }

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
