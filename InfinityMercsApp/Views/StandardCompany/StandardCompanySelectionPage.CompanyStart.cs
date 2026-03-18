using InfinityMercsApp.Views.Common;

namespace InfinityMercsApp.Views.StandardCompany;

/// <summary>
/// Company start/save workflow.
/// </summary>
public partial class StandardCompanySelectionPage
{
    protected override string CompanyTypeLabel => _mode switch
    {
        ArmySourceSelectionMode.VanillaFactions => "Standard Company - Vanilla",
        ArmySourceSelectionMode.Sectorials => "Standard Company - Sectorial",
        _ => "Unknown Company Type"
    };

    /// <summary>
    /// Handles start company async.
    /// </summary>
    protected override async Task StartCompanyAsync()
    {
        await ExecuteStartCompanyAsync<ArmyFactionSelectionItem, MercsCompanyEntry, SavedImprovedCaptainStats>(
            CompanyName,
            MercsCompanyEntries,
            ShowRightSelectionBox,
            _factionSelectionState.LeftSlotFaction,
            _factionSelectionState.RightSlotFaction,
            Factions,
            _specOpsProvider,
            ShowUnitsInInches,
            SelectedStartSeasonPoints,
            SeasonPointsCapText,
            factionId => _armyDataService.GetMetadataFactionById(factionId)?.Name,
            stats => stats.CaptainName);
    }

    /// <summary>
    /// Handles update mercs company total.
    /// </summary>
    private void UpdateMercsCompanyTotal()
    {
        SeasonPointsCapText = ComputeMercsCompanyTotalCostText(MercsCompanyEntries);
    }

    /// <summary>
    /// Handles refresh mercs company entry distance displays.
    /// </summary>
    private void RefreshMercsCompanyEntryDistanceDisplays()
    {
        CompanySelectionPageBase.RefreshMercsCompanyEntryDistanceDisplays(MercsCompanyEntries, FormatMoveValue);
    }

    /// <summary>
    /// Handles update season validation state.
    /// </summary>
    private void UpdateSeasonValidationState()
    {
        IsCompanyValid = IsCompanySeasonValid(
            MercsCompanyEntries,
            SelectedStartSeasonPoints,
            SeasonPointsCapText);
    }
}


