using InfinityMercsApp.Views.Controls;
using InfinityMercsApp.Views.Common;
using SkiaSharp;
using SkiaSharp.Views.Maui;

namespace InfinityMercsApp.Views.CohesiveCompany;

/// <summary>
/// Visual layer concerns: icon loading, paint handlers, and faction-theme color application.
/// </summary>
public partial class CohesiveCompanySelectionPage
{
    private void OnTrackedFireteamLevelCanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        CompanySelectionVisualUiWorkflow.DrawSlotPicture(_trackedFireteamLevelPicture, e);
    }

    private void OnIrregularIconCanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        CompanySelectionVisualUiWorkflow.DrawSlotPicture(UnitDisplayConfigurationsViewForVisuals.IrregularOrderIconPicture, e);
    }

    private void OnRegularModifierIconCanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        CompanySelectionVisualUiWorkflow.DrawSlotPicture(UnitDisplayConfigurationsViewForVisuals.RegularOrderIconPicture, e);
    }
}



