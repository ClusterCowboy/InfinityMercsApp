using InfinityMercsApp.Views.Common;

namespace InfinityMercsApp.Views.InspiringCompany;

public partial class InspiringCompanySelectionPage
{
    protected override string CompanyTypeLabel => "Inspiring Leader";

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
