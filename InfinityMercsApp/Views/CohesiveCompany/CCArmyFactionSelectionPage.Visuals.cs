using InfinityMercsApp.Views.Controls;
using InfinityMercsApp.Views.Templates.NewCompany;
using SkiaSharp;
using SkiaSharp.Views.Maui;

namespace InfinityMercsApp.Views.CohesiveCompany;

/// <summary>
/// Visual layer concerns: icon loading, paint handlers, and faction-theme color application.
/// </summary>
public partial class CCArmyFactionSelectionPage
{
    private void OnTrackedFireteamLevelCanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        CompanySelectionVisualUiWorkflow.DrawSlotPicture(_trackedFireteamLevelPicture, e);
    }
}
