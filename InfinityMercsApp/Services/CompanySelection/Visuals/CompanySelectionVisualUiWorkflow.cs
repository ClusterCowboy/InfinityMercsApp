using InfinityMercsApp.Infrastructure.Providers;
using InfinityMercsApp.Services;
using InfinityMercsApp.Views.Controls;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

namespace InfinityMercsApp.Views.Common;

internal static class CompanySelectionVisualUiWorkflow
{
    private static readonly SKColor ActiveFilterYellow = new(255, 220, 0, 178);

    internal static void DrawFilterIcon(SKPicture? filterIconPicture, bool isFilterActive, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        if (filterIconPicture is null)
        {
            return;
        }

        var bounds = new SKRect(0, 0, e.Info.Width, e.Info.Height);

        if (isFilterActive)
        {
            using var tintPaint = new SKPaint
            {
                ColorFilter = SKColorFilter.CreateBlendMode(ActiveFilterYellow, SKBlendMode.SrcIn)
            };
            canvas.SaveLayer(bounds, tintPaint);
            CompanySelectionSharedUtilities.DrawPictureInRect(canvas, filterIconPicture, bounds);
            canvas.Restore();
        }
        else
        {
            CompanySelectionSharedUtilities.DrawPictureInRect(canvas, filterIconPicture, bounds);
        }
    }

    internal static void DrawPeripheralIcon(UnitDisplayConfigurationsView unitDisplayConfigurationsView, SKPaintSurfaceEventArgs e)
    {
        DrawSlotPicture(unitDisplayConfigurationsView.PeripheralIconPicture, e);
    }

    internal static void DrawSlotPicture(SKPicture? picture, SKPaintSurfaceEventArgs e)
    {
        CompanySelectionSharedUtilities.DrawSlotPicture(picture, e);
    }

    internal static void ApplyFilterButtonSize(Border? buttonBorder, SKCanvasView? iconCanvas, double iconButtonSize)
    {
        CompanySelectionSharedUtilities.ApplyFilterButtonSize(buttonBorder, iconCanvas, iconButtonSize);
    }

    internal static void UpdateUnitNameHeadingFontSize(UnitDisplayConfigurationsView? unitDisplayConfigurationsView)
    {
        unitDisplayConfigurationsView?.RefreshUnitHeadingFontSize();
    }

    internal static void ApplyHeaderColors(
        string? vanillaFactionName,
        UnitDisplayConfigurationsView unitDisplayConfigurationsView,
        Action<Color> setPrimaryColor,
        Action<Color> setSecondaryColor,
        Action<Color> setPrimaryTextColor,
        Action<Color> setSecondaryTextColor,
        Action refreshSummary)
    {
        var colors = CompanySelectionVisualThemeWorkflow.GetHeaderColors(
            vanillaFactionName,
            UnitDisplayConfigurationsView.DefaultHeaderPrimaryColor,
            UnitDisplayConfigurationsView.DefaultHeaderSecondaryColor);
        setPrimaryColor(colors.Primary);
        setSecondaryColor(colors.Secondary);
        setPrimaryTextColor(colors.PrimaryText);
        setSecondaryTextColor(colors.SecondaryText);
        refreshSummary();
    }

    internal static void ApplySummaryFormatted(
        UnitDisplayConfigurationsView unitDisplayConfigurationsView,
        Color unitHeaderSecondaryColor,
        bool highlightLieutenant,
        Action<FormattedString> setEquipmentSummaryFormatted,
        Action<FormattedString> setSkillsSummaryFormatted)
    {
        var summaries = CompanySelectionVisualThemeWorkflow.BuildSummaryFormatted(
            unitDisplayConfigurationsView,
            unitHeaderSecondaryColor,
            highlightLieutenant);
        setEquipmentSummaryFormatted(summaries.EquipmentSummary);
        setSkillsSummaryFormatted(summaries.SkillsSummary);
    }

    internal static bool GetShowUnitsInInchesFromProvider(
        IAppSettingsProvider? appSettingsProvider,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return appSettingsProvider?.GetShowUnitsInInches() ?? false;
    }
}


