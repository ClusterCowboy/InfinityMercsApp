using SkiaSharp.Views.Maui.Controls;

namespace InfinityMercsApp.Views.Common;

internal static class CompanySelectionUnitSelectionUiWorkflow
{
    internal static void ApplyHeaderFilterButtonSizes(
        object? sender,
        Border? inactiveButtonBorder,
        SKCanvasView? inactiveIconCanvas,
        Border? activeButtonBorder,
        SKCanvasView? activeIconCanvas,
        Action<Border?, SKCanvasView?, double> applyFilterButtonSize)
    {
        if (sender is not Border border || border.Height <= 0)
        {
            return;
        }

        var iconButtonSize = border.Height * 0.8;
        applyFilterButtonSize(inactiveButtonBorder, inactiveIconCanvas, iconButtonSize);
        applyFilterButtonSize(activeButtonBorder, activeIconCanvas, iconButtonSize);
    }
}


