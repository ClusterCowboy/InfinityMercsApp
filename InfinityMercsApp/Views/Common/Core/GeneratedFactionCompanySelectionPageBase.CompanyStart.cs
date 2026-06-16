using InfinityMercsApp.Views.Common;

namespace InfinityMercsApp.Views.Common;

/// <summary>
/// Company start/save workflow.
/// </summary>
public abstract partial class GeneratedFactionCompanySelectionPageBase
{
    protected void UpdateMercsCompanyTotal()
    {
        SeasonPointsCapText = ComputeMercsCompanyTotalCostText(MercsCompanyEntries);
    }

    protected void RefreshMercsCompanyEntryDistanceDisplays()
    {
        CompanySelectionPageBase.RefreshMercsCompanyEntryDistanceDisplays(MercsCompanyEntries, FormatMoveValue);
    }

    protected void UpdateSeasonValidationState()
    {
        IsCompanyValid = IsCompanySeasonValid(
            MercsCompanyEntries,
            SelectedStartSeasonPoints,
            SeasonPointsCapText);
    }
}
