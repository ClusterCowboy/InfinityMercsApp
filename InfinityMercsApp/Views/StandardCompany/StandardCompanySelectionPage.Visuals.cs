using System.Globalization;
using System.Text;
using System.Text.Json;
using InfinityMercsApp.Domain.Utilities;
using InfinityMercsApp.Views.Controls;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using Svg.Skia;
using ArmyUnitRecord = InfinityMercsApp.Domain.Models.Army.Unit;
using InfinityMercsApp.Views.Templates.NewCompany;
using FactionRecord = InfinityMercsApp.Domain.Models.Metadata.Faction;
using InfinityMercsApp.Views.Templates.UICommon;

namespace InfinityMercsApp.Views.StandardCompany;


/// <summary>
/// Visual layer concerns: icon loading, paint handlers, and faction-theme color application.
/// </summary>
public partial class StandardCompanySelectionPage
{
    /// <summary>
    /// Handles load slot icon async.
    /// </summary>
    private async Task LoadSlotIconAsync(int slotIndex, string? cachedPath, string? packagedPath)
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

            if (slotIndex == 0)
            {
                FactionSlotSelectorView.LeftSlotPicture?.Dispose();
                FactionSlotSelectorView.LeftSlotPicture = null;
            }
            else
            {
                FactionSlotSelectorView.RightSlotPicture?.Dispose();
                FactionSlotSelectorView.RightSlotPicture = null;
            }

            if (stream is null)
            {
                return;
            }

            await using (stream)
            {
                var svg = new SKSvg();
                var picture = svg.Load(stream);
                if (slotIndex == 0)
                {
                    FactionSlotSelectorView.LeftSlotPicture = picture;
                }
                else
                {
                    FactionSlotSelectorView.RightSlotPicture = picture;
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ArmyFactionSelectionPage slot icon load failed: {ex.Message}");
            if (slotIndex == 0)
            {
                FactionSlotSelectorView.LeftSlotPicture = null;
            }
            else
            {
                FactionSlotSelectorView.RightSlotPicture = null;
            }
        }

    }

    /// <summary>
    /// Handles load header icons async.
    /// </summary>
    private async Task LoadHeaderIconsAsync()
    {
        UnitDisplayConfigurationsView.RegularOrderIconPicture?.Dispose();
        UnitDisplayConfigurationsView.RegularOrderIconPicture = null;
        UnitDisplayConfigurationsView.IrregularOrderIconPicture?.Dispose();
        UnitDisplayConfigurationsView.IrregularOrderIconPicture = null;
        UnitDisplayConfigurationsView.ImpetuousIconPicture?.Dispose();
        UnitDisplayConfigurationsView.ImpetuousIconPicture = null;
        UnitDisplayConfigurationsView.TacticalAwarenessIconPicture?.Dispose();
        UnitDisplayConfigurationsView.TacticalAwarenessIconPicture = null;
        UnitDisplayConfigurationsView.CubeIconPicture?.Dispose();
        UnitDisplayConfigurationsView.CubeIconPicture = null;
        UnitDisplayConfigurationsView.Cube2IconPicture?.Dispose();
        UnitDisplayConfigurationsView.Cube2IconPicture = null;
        UnitDisplayConfigurationsView.HackableIconPicture?.Dispose();
        UnitDisplayConfigurationsView.HackableIconPicture = null;
        UnitDisplayConfigurationsView.PeripheralIconPicture?.Dispose();
        UnitDisplayConfigurationsView.PeripheralIconPicture = null;
        _filterIconPicture?.Dispose();
        _filterIconPicture = null;

        try
        {
            await using var regularStream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/CBIcons/regular.svg");
            var regularSvg = new SKSvg();
            UnitDisplayConfigurationsView.RegularOrderIconPicture = regularSvg.Load(regularStream);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ArmyFactionSelectionPage regular order icon load failed: {ex.Message}");
        }

        try
        {
            await using var irregularStream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/CBIcons/irregular.svg");
            var irregularSvg = new SKSvg();
            UnitDisplayConfigurationsView.IrregularOrderIconPicture = irregularSvg.Load(irregularStream);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ArmyFactionSelectionPage irregular order icon load failed: {ex.Message}");
        }

        try
        {
            await using var impetuousStream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/CBIcons/impetuous.svg");
            var impetuousSvg = new SKSvg();
            UnitDisplayConfigurationsView.ImpetuousIconPicture = impetuousSvg.Load(impetuousStream);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ArmyFactionSelectionPage impetuous icon load failed: {ex.Message}");
        }

        try
        {
            await using var tacticalStream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/CBIcons/tactical.svg");
            var tacticalSvg = new SKSvg();
            UnitDisplayConfigurationsView.TacticalAwarenessIconPicture = tacticalSvg.Load(tacticalStream);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ArmyFactionSelectionPage tactical awareness icon load failed: {ex.Message}");
        }

        try
        {
            await using var cubeStream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/CBIcons/cube.svg");
            var cubeSvg = new SKSvg();
            UnitDisplayConfigurationsView.CubeIconPicture = cubeSvg.Load(cubeStream);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ArmyFactionSelectionPage cube icon load failed: {ex.Message}");
        }

        try
        {
            await using var cube2Stream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/CBIcons/cube2.svg");
            var cube2Svg = new SKSvg();
            UnitDisplayConfigurationsView.Cube2IconPicture = cube2Svg.Load(cube2Stream);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ArmyFactionSelectionPage cube2 icon load failed: {ex.Message}");
        }

        try
        {
            await using var hackableStream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/CBIcons/hackable.svg");
            var hackableSvg = new SKSvg();
            UnitDisplayConfigurationsView.HackableIconPicture = hackableSvg.Load(hackableStream);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ArmyFactionSelectionPage hackable icon load failed: {ex.Message}");
        }

        try
        {
            await using var peripheralStream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/CBIcons/peripheral.svg");
            var peripheralSvg = new SKSvg();
            UnitDisplayConfigurationsView.PeripheralIconPicture = peripheralSvg.Load(peripheralStream);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ArmyFactionSelectionPage peripheral icon load failed: {ex.Message}");
        }

        try
        {
            await using var filterStream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/NonCBIcons/noun-filter.svg");
            var filterSvg = new SKSvg();
            _filterIconPicture = filterSvg.Load(filterStream);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ArmyFactionSelectionPage filter icon load failed: {ex.Message}");
        }

        UnitDisplayConfigurationsView.InvalidateHeaderIconsCanvas();
        UnitSelectionFilterCanvasInactive.InvalidateSurface();
        UnitSelectionFilterCanvasActive.InvalidateSurface();
        UnitDisplayConfigurationsView.InvalidatePeripheralHeaderIconCanvas();
    }

    /// <summary>
    /// Handles on unit selection filter canvas paint surface.
    /// </summary>
    private void OnUnitSelectionFilterCanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        DrawSlotPicture(_filterIconPicture, e);
    }

    /// <summary>
    /// Handles on peripheral icon canvas paint surface.
    /// </summary>
    private void OnPeripheralIconCanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        DrawSlotPicture(UnitDisplayConfigurationsView.PeripheralIconPicture, e);
    }

    /// <summary>
    /// Handles apply filter button size.
    /// </summary>
    private static void ApplyFilterButtonSize(Border? buttonBorder, SKCanvasView? iconCanvas, double iconButtonSize)
    {
        CompanySelectionSharedUtilities.ApplyFilterButtonSize(buttonBorder, iconCanvas, iconButtonSize);
    }

    /// <summary>
    /// Handles update unit name heading font size.
    /// </summary>
    private void UpdateUnitNameHeadingFontSize()
    {
        UnitDisplayConfigurationsView?.RefreshUnitHeadingFontSize();
    }

    bool IUnitDisplayIconState.ShowRegularOrderIcon => ShowRegularOrderIcon;
    bool IUnitDisplayIconState.ShowIrregularOrderIcon => ShowIrregularOrderIcon;
    bool IUnitDisplayIconState.ShowImpetuousIcon => ShowImpetuousIcon;
    bool IUnitDisplayIconState.ShowTacticalAwarenessIcon => ShowTacticalAwarenessIcon;
    bool IUnitDisplayIconState.ShowCubeIcon => ShowCubeIcon;
    bool IUnitDisplayIconState.ShowCube2Icon => ShowCube2Icon;
    bool IUnitDisplayIconState.ShowHackableIcon => ShowHackableIcon;
    double IUnitDisplayIconState.UnitHeadingMaxFontSize => UnitNameHeadingMaxFontSize;
    double IUnitDisplayIconState.UnitHeadingMinFontSize => UnitNameHeadingMinFontSize;
    double IUnitDisplayIconState.UnitHeadingFontStep => UnitNameHeadingFontStep;
    void IUnitDisplayIconState.ApplyUnitHeadingFontSize(double size) => UnitNameHeadingFontSize = size;
    bool IUnitDisplayStatState.ShowUnitsInInches => ShowUnitsInInches;
    int? IUnitDisplayStatState.UnitMoveFirstCm => UnitMoveFirstCm;
    int? IUnitDisplayStatState.UnitMoveSecondCm => UnitMoveSecondCm;
    int? IUnitDisplayStatState.PeripheralMoveFirstCm => PeripheralMoveFirstCm;
    int? IUnitDisplayStatState.PeripheralMoveSecondCm => PeripheralMoveSecondCm;
    void IUnitDisplayStatState.ApplyUnitMoveDisplay(string value) => UnitMov = value;
    void IUnitDisplayStatState.ApplyPeripheralMoveDisplay(string value) => PeripheralMov = value;

    /// <summary>
    /// Handles apply unit header colors async.
    /// </summary>
    private async Task ApplyUnitHeaderColorsAsync(int sourceFactionId, ArmyUnitRecord? unit, CancellationToken cancellationToken)
    {
        string? factionName;
        if (_mode == ArmySourceSelectionMode.Sectorials)
        {
            // In sectorial mode, always color by the sectorial lineage the unit was generated from.
            factionName = await ResolveVanillaFactionNameAsync(sourceFactionId, cancellationToken);
        }
        else
        {
            factionName = await ResolveUnitVanillaFactionNameAsync(sourceFactionId, unit?.FactionsJson, cancellationToken);
        }

        ApplyUnitHeaderColorsByVanillaFactionName(factionName);
    }

    /// <summary>
    /// Handles resolve unit vanilla faction name async.
    /// </summary>
    private async Task<string?> ResolveUnitVanillaFactionNameAsync(int sourceFactionId, string? unitFactionsJson, CancellationToken cancellationToken)
    {
        foreach (var factionId in ParseFactionIds(unitFactionsJson))
        {
            var candidateName = await ResolveVanillaFactionNameAsync(factionId, cancellationToken);
            if (IsThemeFactionName(candidateName))
            {
                return candidateName;
            }
        }

        return await ResolveVanillaFactionNameAsync(sourceFactionId, cancellationToken);
    }

    /// <summary>
    /// Handles resolve vanilla faction name async.
    /// </summary>
    private Task<string?> ResolveVanillaFactionNameAsync(int sourceFactionId, CancellationToken cancellationToken)
    {
        if (sourceFactionId <= 0)
        {
            return Task.FromResult<string?>(null);
        }

        var source = _armyDataService.GetMetadataFactionById(sourceFactionId);
        FactionRecord? current = source is null
            ? null
            : new FactionRecord
            {
                Id = source.Id,
                ParentId = source.ParentId,
                Name = source.Name,
                Slug = source.Slug,
                Discontinued = source.Discontinued,
                Logo = source.Logo
            };

        var safety = 0;
        while (current is not null && safety < 8)
        {
            // Prefer the first recognized themed faction while walking up the lineage.
            if (IsThemeFactionName(current.Name))
            {
                return Task.FromResult<string?>(current.Name);
            }

            if (current.ParentId <= 0)
            {
                break;
            }

            var parentRecord = _armyDataService.GetMetadataFactionById(current.ParentId);
            FactionRecord? parent = parentRecord is null
                ? null
                : new FactionRecord
                {
                    Id = parentRecord.Id,
                    ParentId = parentRecord.ParentId,
                    Name = parentRecord.Name,
                    Slug = parentRecord.Slug,
                    Discontinued = parentRecord.Discontinued,
                    Logo = parentRecord.Logo
                };

            if (parent is null || parent.Id == current.Id)
            {
                break;
            }

            current = parent;
            safety++;
        }

        // Reinforcement families in metadata can point to intermediate parent ids that are not present.
        // Fall back to id-family inference so reinforcement factions inherit their base faction theme.
        var inferredThemeName = InferThemeFactionNameFromFactionId(sourceFactionId)
            ?? (current is not null ? InferThemeFactionNameFromFactionId(current.Id) : null);
        if (!string.IsNullOrWhiteSpace(inferredThemeName))
        {
            return Task.FromResult<string?>(inferredThemeName);
        }

        return Task.FromResult(current?.Name);
    }

    /// <summary>
    /// Handles apply unit header colors by vanilla faction name.
    /// </summary>
    private void ApplyUnitHeaderColorsByVanillaFactionName(string? vanillaFactionName)
    {
        var (primary, secondary) = GetFactionTheme(vanillaFactionName);
        UnitHeaderPrimaryColor = primary;
        UnitHeaderSecondaryColor = secondary;
        UnitHeaderPrimaryTextColor = IsLightColor(primary) ? Colors.Black : Colors.White;
        UnitHeaderSecondaryTextColor = IsLightColor(secondary) ? Colors.Black : Colors.White;
        RefreshSummaryFormatted();
    }

    /// <summary>
    /// Handles refresh summary formatted.
    /// </summary>
    private void RefreshSummaryFormatted()
    {
        var (equipmentAccent, skillsAccent) = GetSummaryAccentColorsForSecondaryBackground(UnitHeaderSecondaryColor);
        EquipmentSummaryFormatted = CompanyProfileTextService.BuildNamedSummaryFormatted("Equipment", UnitDisplayConfigurationsView.SelectedUnitCommonEquipment, equipmentAccent);
        SpecialSkillsSummaryFormatted = CompanyProfileTextService.BuildNamedSummaryFormatted(
            "Special Skills",
            UnitDisplayConfigurationsView.SelectedUnitCommonSkills,
            skillsAccent,
            highlightLieutenantPurple: _summaryHighlightLieutenant);
    }

    /// <summary>
    /// Handles get summary accent colors for secondary background.
    /// </summary>
    private static (Color EquipmentAccent, Color SkillsAccent) GetSummaryAccentColorsForSecondaryBackground(Color secondaryBackground)
    {
        return IsLightColor(secondaryBackground)
            ? (UnitDisplayConfigurationsView.EquipmentAccentOnLightSecondary, UnitDisplayConfigurationsView.SkillsAccentOnLightSecondary)
            : (UnitDisplayConfigurationsView.EquipmentAccentOnDarkSecondary, UnitDisplayConfigurationsView.SkillsAccentOnDarkSecondary);
    }

    /// <summary>
    /// Handles get faction theme.
    /// </summary>
    private static (Color Primary, Color Secondary) GetFactionTheme(string? factionName)
    {
        var key = NormalizeFactionName(factionName);
        return key switch
        {
            "panoceania" => (Color.FromArgb("#239ac2"), Color.FromArgb("#006a91")),
            "yujing" => (Color.FromArgb("#ff9000"), Color.FromArgb("#995601")),
            "ariadna" => (Color.FromArgb("#007d27"), Color.FromArgb("#005825")),
            "haqqislam" => (Color.FromArgb("#e6da9b"), Color.FromArgb("#8a835d")),
            "nomads" => (Color.FromArgb("#ce181e"), Color.FromArgb("#7c0e13")),
            "combinedarmy" => (Color.FromArgb("#400b5f"), Color.FromArgb("#260739")),
            "aleph" => (Color.FromArgb("#aea6bb"), Color.FromArgb("#696471")),
            "tohaa" => (Color.FromArgb("#3b3b3b"), Color.FromArgb("#252525")),
            "nonalignedarmy" => (Color.FromArgb("#728868"), Color.FromArgb("#728868")),
            "o12" => (Color.FromArgb("#005470"), Color.FromArgb("#dead33")),
            "jsa" => (Color.FromArgb("#a6112b"), Color.FromArgb("#757575")),
            _ => (UnitDisplayConfigurationsView.DefaultHeaderPrimaryColor, UnitDisplayConfigurationsView.DefaultHeaderSecondaryColor)
        };
    }

    /// <summary>
    /// Handles is theme faction name.
    /// </summary>
    private static bool IsThemeFactionName(string? factionName)
    {
        return CompanySelectionSharedUtilities.IsThemeFactionName(factionName);
    }

    /// <summary>
    /// Handles parse faction ids.
    /// </summary>
    private static IReadOnlyList<int> ParseFactionIds(string? factionsJson)
    {
        return CompanySelectionSharedUtilities.ParseFactionIds(factionsJson);
    }

    /// <summary>
    /// Handles normalize faction name.
    /// </summary>
    private static string NormalizeFactionName(string? value)
    {
        return CompanySelectionSharedUtilities.NormalizeFactionName(value);
    }

    /// <summary>
    /// Handles is light color.
    /// </summary>
    private static bool IsLightColor(Color color)
    {
        return CompanySelectionSharedUtilities.IsLightColor(color);
    }

    /// <summary>
    /// Handles infer theme faction name from faction id.
    /// </summary>
    private static string? InferThemeFactionNameFromFactionId(int factionId)
    {
        if (factionId <= 0)
        {
            return null;
        }

        var family = factionId / 100;
        return family switch
        {
            1 => "PanOceania",
            2 => "Yu Jing",
            3 => "Ariadna",
            4 => "Haqqislam",
            5 => "Nomads",
            6 => "Combined Army",
            7 => "Aleph",
            8 => "Tohaa",
            9 => "Non-Aligned Armies",
            10 => "O-12",
            11 => "JSA",
            _ => null
        };
    }

    /// <summary>
    /// Handles draw picture in rect.
    /// </summary>
    private static void DrawPictureInRect(SKCanvas canvas, SKPicture picture, SKRect destination)
    {
        CompanySelectionSharedUtilities.DrawPictureInRect(canvas, picture, destination);
    }

    /// <summary>
    /// Handles draw slot picture.
    /// </summary>
    private static void DrawSlotPicture(SKPicture? picture, SKPaintSurfaceEventArgs e)
    {
        CompanySelectionSharedUtilities.DrawSlotPicture(picture, e);
    }

    /// <summary>
    /// Handles draw slot border.
    /// </summary>
    private static void DrawSlotBorder(SKPaintSurfaceEventArgs e, SKColor borderColor)
    {
        CompanySelectionSharedUtilities.DrawSlotBorder(e, borderColor);
    }

    /// <summary>
    /// Handles get show units in inches from provider.
    /// </summary>
    private bool GetShowUnitsInInchesFromProvider(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return _appSettingsProvider?.GetShowUnitsInInches() ?? false;
    }
}



