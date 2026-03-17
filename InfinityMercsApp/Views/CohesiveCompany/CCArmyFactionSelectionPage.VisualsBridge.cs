using InfinityMercsApp.Services;
using InfinityMercsApp.Views.Controls;
using SkiaSharp.Views.Maui.Controls;

namespace InfinityMercsApp.Views.CohesiveCompany;

public partial class CCArmyFactionSelectionPage
{
    protected override IArmyDataService ArmyDataService => _armyDataService;
    protected override FactionSlotSelectorView FactionSlotSelectorViewForVisuals => FactionSlotSelectorView;
    protected override UnitDisplayConfigurationsView UnitDisplayConfigurationsViewForVisuals => UnitDisplayConfigurationsView;
    protected override SKCanvasView UnitSelectionFilterCanvasInactiveForVisuals => UnitSelectionFilterCanvasInactive;
    protected override SKCanvasView UnitSelectionFilterCanvasActiveForVisuals => UnitSelectionFilterCanvasActive;
    protected override bool SummaryHighlightLieutenantForVisuals => _summaryHighlightLieutenant;
    protected override Color UnitHeaderSecondaryColorForVisuals => UnitHeaderSecondaryColor;
    protected override void SetUnitHeaderPrimaryColorForVisuals(Color value) => UnitHeaderPrimaryColor = value;
    protected override void SetUnitHeaderSecondaryColorForVisuals(Color value) => UnitHeaderSecondaryColor = value;
    protected override void SetUnitHeaderPrimaryTextColorForVisuals(Color value) => UnitHeaderPrimaryTextColor = value;
    protected override void SetUnitHeaderSecondaryTextColorForVisuals(Color value) => UnitHeaderSecondaryTextColor = value;
    protected override void SetEquipmentSummaryFormattedForVisuals(FormattedString value) => EquipmentSummaryFormatted = value;
    protected override void SetSpecialSkillsSummaryFormattedForVisuals(FormattedString value) => SpecialSkillsSummaryFormatted = value;
}
