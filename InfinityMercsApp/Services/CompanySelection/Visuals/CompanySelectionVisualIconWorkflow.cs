using InfinityMercsApp.Views.Controls;
using SkiaSharp;
using Svg.Skia;

namespace InfinityMercsApp.Views.Common;

internal static class CompanySelectionVisualIconWorkflow
{
    internal static async Task LoadSlotIconAsync(
        int slotIndex,
        string? cachedPath,
        string? packagedPath,
        FactionSlotSelectorView factionSlotSelectorView,
        Action<string> logError)
    {
        try
        {
            Stream? stream = null;
            if (!string.IsNullOrWhiteSpace(cachedPath) && File.Exists(cachedPath))
            {
                stream = File.OpenRead(cachedPath);
            }
            else if (!string.IsNullOrWhiteSpace(packagedPath))
            {
                stream = await FileSystem.Current.OpenAppPackageFileAsync(packagedPath);
            }

            ClearSlotPicture(factionSlotSelectorView, slotIndex);
            if (stream is null)
            {
                return;
            }

            await using (stream)
            {
                var svg = new SKSvg();
                var picture = svg.Load(stream);
                SetSlotPicture(factionSlotSelectorView, slotIndex, picture);
            }
        }
        catch (Exception ex)
        {
            logError($"CompanySelectionPage slot icon load failed: {ex.Message}");
            SetSlotPicture(factionSlotSelectorView, slotIndex, null);
        }
    }

    internal static async Task<SKPicture?> LoadHeaderIconsAsync(
        UnitDisplayConfigurationsView unitDisplayConfigurationsView,
        SKPicture? filterIconPicture,
        Action invalidateUnitSelectionFilters,
        Action<string> logError)
    {
        unitDisplayConfigurationsView.RegularOrderIconPicture?.Dispose();
        unitDisplayConfigurationsView.RegularOrderIconPicture = null;
        unitDisplayConfigurationsView.IrregularOrderIconPicture?.Dispose();
        unitDisplayConfigurationsView.IrregularOrderIconPicture = null;
        unitDisplayConfigurationsView.ImpetuousIconPicture?.Dispose();
        unitDisplayConfigurationsView.ImpetuousIconPicture = null;
        unitDisplayConfigurationsView.TacticalAwarenessIconPicture?.Dispose();
        unitDisplayConfigurationsView.TacticalAwarenessIconPicture = null;
        unitDisplayConfigurationsView.CubeIconPicture?.Dispose();
        unitDisplayConfigurationsView.CubeIconPicture = null;
        unitDisplayConfigurationsView.Cube2IconPicture?.Dispose();
        unitDisplayConfigurationsView.Cube2IconPicture = null;
        unitDisplayConfigurationsView.HackableIconPicture?.Dispose();
        unitDisplayConfigurationsView.HackableIconPicture = null;
        unitDisplayConfigurationsView.PeripheralIconPicture?.Dispose();
        unitDisplayConfigurationsView.PeripheralIconPicture = null;
        filterIconPicture?.Dispose();
        filterIconPicture = null;

        unitDisplayConfigurationsView.RegularOrderIconPicture = await LoadPictureAsync("SVGCache/CBIcons/regular.svg", "regular order", logError);
        unitDisplayConfigurationsView.IrregularOrderIconPicture = await LoadPictureAsync("SVGCache/CBIcons/irregular.svg", "irregular order", logError);
        unitDisplayConfigurationsView.ImpetuousIconPicture = await LoadPictureAsync("SVGCache/CBIcons/impetuous.svg", "impetuous", logError);
        unitDisplayConfigurationsView.TacticalAwarenessIconPicture = await LoadPictureAsync("SVGCache/CBIcons/tactical.svg", "tactical awareness", logError);
        unitDisplayConfigurationsView.CubeIconPicture = await LoadPictureAsync("SVGCache/CBIcons/cube.svg", "cube", logError);
        unitDisplayConfigurationsView.Cube2IconPicture = await LoadPictureAsync("SVGCache/CBIcons/cube2.svg", "cube2", logError);
        unitDisplayConfigurationsView.HackableIconPicture = await LoadPictureAsync("SVGCache/CBIcons/hackable.svg", "hackable", logError);
        unitDisplayConfigurationsView.PeripheralIconPicture = await LoadPictureAsync("SVGCache/CBIcons/peripheral.svg", "peripheral", logError);
        filterIconPicture = await LoadPictureAsync("SVGCache/NonCBIcons/noun-filter.svg", "filter", logError);

        unitDisplayConfigurationsView.InvalidateHeaderIconsCanvas();
        invalidateUnitSelectionFilters();
        unitDisplayConfigurationsView.InvalidatePeripheralHeaderIconCanvas();
        return filterIconPicture;
    }

    private static void ClearSlotPicture(FactionSlotSelectorView factionSlotSelectorView, int slotIndex)
    {
        if (slotIndex == 0)
        {
            factionSlotSelectorView.LeftSlotPicture?.Dispose();
            factionSlotSelectorView.LeftSlotPicture = null;
            return;
        }

        factionSlotSelectorView.RightSlotPicture?.Dispose();
        factionSlotSelectorView.RightSlotPicture = null;
    }

    private static void SetSlotPicture(FactionSlotSelectorView factionSlotSelectorView, int slotIndex, SKPicture? picture)
    {
        if (slotIndex == 0)
        {
            factionSlotSelectorView.LeftSlotPicture = picture;
            return;
        }

        factionSlotSelectorView.RightSlotPicture = picture;
    }

    private static async Task<SKPicture?> LoadPictureAsync(string packagedPath, string label, Action<string> logError)
    {
        try
        {
            await using var stream = await FileSystem.Current.OpenAppPackageFileAsync(packagedPath);
            var svg = new SKSvg();
            return svg.Load(stream);
        }
        catch (Exception ex)
        {
            logError($"CompanySelectionPage {label} icon load failed: {ex.Message}");
            return null;
        }
    }
}


