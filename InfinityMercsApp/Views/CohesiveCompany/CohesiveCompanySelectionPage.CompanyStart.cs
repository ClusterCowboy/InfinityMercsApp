using InfinityMercsApp.Views.Common.NewCompany;

namespace InfinityMercsApp.Views.CohesiveCompany;

public partial class CCArmyFactionSelectionPage
{
    protected override string CompanyTypeLabel => "Cohesive Company";

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

