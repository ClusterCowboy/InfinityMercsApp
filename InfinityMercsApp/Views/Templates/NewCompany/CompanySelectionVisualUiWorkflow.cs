using InfinityMercsApp.Infrastructure.Providers;
using InfinityMercsApp.Services;
using InfinityMercsApp.Views.Controls;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

namespace InfinityMercsApp.Views.Templates.NewCompany;

internal static class CompanySelectionVisualUiWorkflow
{
    internal static void DrawFilterIcon(SKPicture? filterIconPicture, SKPaintSurfaceEventArgs e)
    {
        DrawSlotPicture(filterIconPicture, e);
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
