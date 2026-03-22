using InfinityMercsApp.Views.Common;

namespace InfinityMercsApp.Views.AirborneCompany;

/// <summary>
/// Company start/save workflow.
/// </summary>
public partial class AirborneCompanySelectionPage
{
    protected override string CompanyTypeLabel => "Airborne Company";

    protected override async Task StartCompanyAsync()
    {
        await ExecuteStartCompanyAsync<ArmyFactionSelectionItem, MercsCompanyEntry, SavedImprovedCaptainStats>(
            CompanyName,
            MercsCompanyEntries,
            false,
            _factionSelectionState.LeftSlotFaction,
            null,
            Factions,
            _specOpsProvider,
            ShowUnitsInInches,
            SelectedStartSeasonPoints,
            SeasonPointsCapText,
            factionId => _armyDataService.GetMetadataFactionById(factionId)?.Name,
            stats => stats.CaptainName);
    }

    private void UpdateMercsCompanyTotal()
    {
        SeasonPointsCapText = ComputeMercsCompanyTotalCostText(MercsCompanyEntries);
    }

    private void RefreshMercsCompanyEntryDistanceDisplays()
    {
        CompanySelectionPageBase.RefreshMercsCompanyEntryDistanceDisplays(MercsCompanyEntries, FormatMoveValue);
    }

    private void UpdateSeasonValidationState()
    {
        IsCompanyValid = IsCompanySeasonValid(
            MercsCompanyEntries,
            SelectedStartSeasonPoints,
            SeasonPointsCapText);
    }
}
