using InfinityMercsApp.Views.Controls;

namespace InfinityMercsApp.Views.Common;

public sealed class CompanySelectionFilterState
{
    public UnitFilterCriteria ActiveUnitFilter { get; set; } = UnitFilterCriteria.None;
    public UnitFilterPopupView? ActiveUnitFilterPopup { get; set; }
    public UnitFilterPopupOptions? PreparedUnitFilterPopupOptions { get; set; }
}
