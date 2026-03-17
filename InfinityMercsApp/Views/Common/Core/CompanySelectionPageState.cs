using InfinityMercsApp.Views.Controls;

namespace InfinityMercsApp.Views.Common;

public interface ICompanySelectionVisibilityState
{
    string SelectedStartSeasonPoints { get; }
    string SeasonPointsCapText { get; }
    bool LieutenantOnlyUnits { get; }
    UnitFilterCriteria ActiveUnitFilter { get; }
}
