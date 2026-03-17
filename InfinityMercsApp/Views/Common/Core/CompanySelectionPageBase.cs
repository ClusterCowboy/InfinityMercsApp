using InfinityMercsApp.Infrastructure.Providers;
using InfinityMercsApp.Services;
using InfinityMercsApp.Views;
using InfinityMercsApp.Views.Controls;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using ArmyFactionRecord = InfinityMercsApp.Domain.Models.Army.Faction;
using ArmyResumeRecord = InfinityMercsApp.Domain.Models.Army.Resume;
using ArmyUnitRecord = InfinityMercsApp.Domain.Models.Army.Unit;
using MercsArmyListEntry = InfinityMercsApp.Domain.Models.Army.MercsArmyListEntry;

namespace InfinityMercsApp.Views.Common;

/// <summary>
/// Shared abstract base class for company selection pages.
/// This centralizes service resolution and shared UI wiring while allowing
/// each concrete page to keep its own state models and mode-specific behavior.
/// </summary>
public abstract partial class CompanySelectionPageBase : ContentPage
{
    private SKPicture? _filterIconPicture;

    protected CompanySelectionPageBase(
        ArmySourceSelectionMode mode,
        IMetadataProvider? metadataProvider,
        IFactionProvider? factionProvider,
        ISpecOpsProvider specOpsProvider,
        ICohesiveCompanyFactionQueryProvider cohesiveCompanyFactionQueryProvider,
        FactionLogoCacheService? factionLogoCacheService,
        IAppSettingsProvider? appSettingsProvider)
    {
        Mode = mode;
        MetadataProvider = metadataProvider;
        FactionProvider = factionProvider;
        SpecOpsProvider = specOpsProvider;
        CohesiveCompanyFactionQueryProvider = cohesiveCompanyFactionQueryProvider;
        FactionLogoCacheService = factionLogoCacheService;
        AppSettingsProvider = appSettingsProvider;
    }

    protected ArmySourceSelectionMode Mode { get; }
    protected IMetadataProvider? MetadataProvider { get; }
    protected IFactionProvider? FactionProvider { get; }
    protected ISpecOpsProvider SpecOpsProvider { get; }
    protected ICohesiveCompanyFactionQueryProvider CohesiveCompanyFactionQueryProvider { get; }
    protected FactionLogoCacheService? FactionLogoCacheService { get; }
    protected IAppSettingsProvider? AppSettingsProvider { get; }

    protected abstract IArmyDataService ArmyDataService { get; }
    protected abstract FactionSlotSelectorView FactionSlotSelectorViewForVisuals { get; }
    protected abstract UnitDisplayConfigurationsView UnitDisplayConfigurationsViewForVisuals { get; }
    protected abstract SKCanvasView UnitSelectionFilterCanvasInactiveForVisuals { get; }
    protected abstract SKCanvasView UnitSelectionFilterCanvasActiveForVisuals { get; }
    protected abstract bool SummaryHighlightLieutenantForVisuals { get; }
    protected abstract Color UnitHeaderSecondaryColorForVisuals { get; }
    protected abstract void SetUnitHeaderPrimaryColorForVisuals(Color value);
    protected abstract void SetUnitHeaderSecondaryColorForVisuals(Color value);
    protected abstract void SetUnitHeaderPrimaryTextColorForVisuals(Color value);
    protected abstract void SetUnitHeaderSecondaryTextColorForVisuals(Color value);
    protected abstract void SetEquipmentSummaryFormattedForVisuals(FormattedString value);
    protected abstract void SetSpecialSkillsSummaryFormattedForVisuals(FormattedString value);
    protected abstract string CompanyTypeLabel { get; }
    protected abstract void SetShowCompanyNameValidationError(bool value);
    protected abstract void SetCompanyNameBorderColor(Color value);

    protected async Task LoadSlotIconAsync(int slotIndex, string? cachedPath, string? packagedPath)
    {
        await CompanySelectionVisualIconWorkflow.LoadSlotIconAsync(
            slotIndex,
            cachedPath,
            packagedPath,
            FactionSlotSelectorViewForVisuals,
            message => Console.Error.WriteLine(message));
    }

    protected async Task LoadHeaderIconsAsync()
    {
        _filterIconPicture = await CompanySelectionVisualIconWorkflow.LoadHeaderIconsAsync(
            UnitDisplayConfigurationsViewForVisuals,
            _filterIconPicture,
            () =>
            {
                UnitSelectionFilterCanvasInactiveForVisuals.InvalidateSurface();
                UnitSelectionFilterCanvasActiveForVisuals.InvalidateSurface();
            },
            message => Console.Error.WriteLine(message));
    }

    protected void OnUnitSelectionFilterCanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        CompanySelectionVisualUiWorkflow.DrawFilterIcon(_filterIconPicture, e);
    }

    protected void OnPeripheralIconCanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        CompanySelectionVisualUiWorkflow.DrawPeripheralIcon(UnitDisplayConfigurationsViewForVisuals, e);
    }

    protected static void ApplyFilterButtonSize(Border? buttonBorder, SKCanvasView? iconCanvas, double iconButtonSize)
    {
        CompanySelectionVisualUiWorkflow.ApplyFilterButtonSize(buttonBorder, iconCanvas, iconButtonSize);
    }

    protected void UpdateUnitNameHeadingFontSize()
    {
        CompanySelectionVisualUiWorkflow.UpdateUnitNameHeadingFontSize(UnitDisplayConfigurationsViewForVisuals);
    }

    protected async Task ApplyUnitHeaderColorsAsync(int sourceFactionId, ArmyUnitRecord? unit, CancellationToken cancellationToken)
    {
        var factionName = await CompanySelectionVisualThemeWorkflow.ResolveThemeFactionNameAsync(
            Mode,
            ArmyDataService,
            sourceFactionId,
            unit?.FactionsJson,
            cancellationToken);
        ApplyUnitHeaderColorsByVanillaFactionName(factionName);
    }

    protected void ApplyUnitHeaderColorsByVanillaFactionName(string? vanillaFactionName)
    {
        CompanySelectionVisualUiWorkflow.ApplyHeaderColors(
            vanillaFactionName,
            UnitDisplayConfigurationsViewForVisuals,
            SetUnitHeaderPrimaryColorForVisuals,
            SetUnitHeaderSecondaryColorForVisuals,
            SetUnitHeaderPrimaryTextColorForVisuals,
            SetUnitHeaderSecondaryTextColorForVisuals,
            RefreshSummaryFormatted);
    }

    protected void RefreshSummaryFormatted()
    {
        CompanySelectionVisualUiWorkflow.ApplySummaryFormatted(
            UnitDisplayConfigurationsViewForVisuals,
            UnitHeaderSecondaryColorForVisuals,
            SummaryHighlightLieutenantForVisuals,
            SetEquipmentSummaryFormattedForVisuals,
            SetSpecialSkillsSummaryFormattedForVisuals);
    }

    protected bool GetShowUnitsInInchesFromProvider(CancellationToken cancellationToken = default)
    {
        return CompanySelectionVisualUiWorkflow.GetShowUnitsInInchesFromProvider(AppSettingsProvider, cancellationToken);
    }

    protected ArmyFactionRecord? GetFactionSnapshotFromProvider(int factionId, CancellationToken cancellationToken = default)
    {
        return ArmyDataService.GetFactionSnapshot(factionId, cancellationToken);
    }

    protected IReadOnlyList<ArmyResumeRecord> GetResumeByFactionMercsOnlyFromProvider(int factionId, CancellationToken cancellationToken = default)
    {
        return ArmyDataService.GetResumeByFactionMercsOnly(factionId, cancellationToken);
    }

    protected ArmyUnitRecord? GetUnitFromProvider(int factionId, int unitId, CancellationToken cancellationToken = default)
    {
        return ArmyDataService.GetUnit(factionId, unitId, cancellationToken);
    }

    protected async Task<IReadOnlyList<MercsArmyListEntry>> GetMergedMercsArmyListFromQueryAccessorAsync(
        IReadOnlyCollection<int> factionIds,
        CancellationToken cancellationToken = default)
    {
        return await ArmyDataService.GetMergedMercsArmyListAsync(factionIds, cancellationToken);
    }

    protected static string ExtractUnitTypeCode(string? subtitle)
    {
        return CompanyStartSharedState.ExtractUnitTypeCode(subtitle);
    }

    protected static void ApplyCompanyNameValidationError(
        bool showError,
        Action<bool> setShowCompanyNameValidationError,
        Action<Color> setCompanyNameBorderColor)
    {
        CompanyStartSharedState.ApplyCompanyNameValidationError(
            showError,
            setShowCompanyNameValidationError,
            setCompanyNameBorderColor);
    }

    protected void SetCompanyNameValidationError(bool showError)
    {
        ApplyCompanyNameValidationError(
            showError,
            SetShowCompanyNameValidationError,
            SetCompanyNameBorderColor);
    }

    protected Command CreateSelectFactionCommand<TFaction>(Action<TFaction> setSelectedFaction)
        where TFaction : class
    {
        return new Command<TFaction>(item =>
        {
            if (item is null)
            {
                return;
            }

            setSelectedFaction(item);
        });
    }

    protected Command CreateSelectUnitCommand<TUnit>(
        Action<TUnit> setSelectedUnit,
        Func<TUnit, int> readUnitId,
        Func<TUnit, int> readSourceFactionId,
        Func<TUnit, string?> readUnitName)
        where TUnit : class
    {
        return new Command<TUnit>(item =>
        {
            if (item is null)
            {
                Console.Error.WriteLine("CompanySelectionPage SelectUnitCommand invoked with null item.");
                return;
            }

            Console.WriteLine(
                $"CompanySelectionPage SelectUnitCommand: id={readUnitId(item)}, faction={readSourceFactionId(item)}, name='{readUnitName(item)}'.");
            setSelectedUnit(item);
        });
    }

    protected Command CreateStartCompanyCommand(Func<Task> startCompanyAsync, Func<bool> canExecute)
    {
        return new Command(async () => await startCompanyAsync(), canExecute);
    }

    protected void WireFactionSlotTapHandlers(Action<int> setActiveSlot, Func<bool> showRightSelectionBox)
    {
        FactionSlotSelectorViewForVisuals.LeftSlotTapped += (_, _) => setActiveSlot(0);
        FactionSlotSelectorViewForVisuals.RightSlotTapped += (_, _) =>
        {
            if (showRightSelectionBox())
            {
                setActiveSlot(1);
            }
        };
    }

    protected void FinalizePageInitialization(Action setInitialActiveSlot)
    {
        BindingContext = this;
        setInitialActiveSlot();
        RefreshSummaryFormatted();
        _ = LoadHeaderIconsAsync();
    }

    protected static string ComputeMercsCompanyTotalCostText<TEntry>(IEnumerable<TEntry> entries)
        where TEntry : class, ICompanyMercsEntry
    {
        return CompanyStartSharedState.ComputeTotalCostText(entries);
    }

    protected static void RefreshMercsCompanyEntryDistanceDisplays<TEntry>(
        IEnumerable<TEntry> entries,
        Func<int?, int?, string> formatMoveValue)
        where TEntry : class, ICompanyMercsEntry
    {
        CompanyStartSharedState.RefreshMercsCompanyEntryDistanceDisplays(entries, formatMoveValue);
    }

    protected static bool IsCompanySeasonValid<TEntry>(
        IEnumerable<TEntry> entries,
        string selectedStartSeasonPoints,
        string seasonPointsCapText)
        where TEntry : class, ICompanyMercsEntry
    {
        return CompanyStartSharedState.IsSeasonValid(entries, selectedStartSeasonPoints, seasonPointsCapText);
    }

    protected void HandleTeamAllowedProfileSelected<TTeamItem, TUnit>(
        TTeamItem? teamItem,
        IEnumerable<TUnit> units,
        Action<TUnit, bool> applySelection,
        Func<TTeamItem, bool>? shouldRestrictProfiles = null)
        where TTeamItem : CompanyTeamUnitLimitItemBase
        where TUnit : CompanyUnitSelectionItemBase
    {
        if (teamItem is null)
        {
            Console.Error.WriteLine("CompanySelectionPage OnTeamAllowedProfileTappedFromView: no team item binding context.");
            return;
        }

        var unitList = units as IReadOnlyList<TUnit> ?? units.ToList();
        var resolved = CompanyTeamSelectionWorkflow.ResolveSelectedTeamUnit<TUnit>(
            unitList,
            teamItem.ResolvedUnitId,
            teamItem.ResolvedSourceFactionId,
            teamItem.Slug,
            teamItem.Name,
            x => x.Id,
            x => x.SourceFactionId,
            x => x.Slug,
            x => x.Name);

        if (resolved is null)
        {
            Console.Error.WriteLine(
                $"CompanySelectionPage OnTeamAllowedProfileTappedFromView: unable to resolve unit for team entry '{teamItem.Name}'.");
            return;
        }

        var restrictProfiles = shouldRestrictProfiles?.Invoke(teamItem) ?? false;
        applySelection(resolved, restrictProfiles);
    }

    protected async Task ExecuteStartCompanyAsync<TFaction, TEntry, TCaptainStats>(
        string? companyName,
        IEnumerable<TEntry> mercsCompanyEntries,
        bool showRightSelectionBox,
        TFaction? leftSlotFaction,
        TFaction? rightSlotFaction,
        IEnumerable<TFaction> factions,
        ISpecOpsProvider specOpsProvider,
        bool showUnitsInInches,
        string selectedStartSeasonPoints,
        string seasonPointsCapText,
        Func<int, string?> tryGetMetadataFactionName,
        Func<TCaptainStats, string?> readCaptainName)
        where TFaction : class, ICompanySourceFaction
        where TEntry : class, ICompanyMercsEntry
        where TCaptainStats : class
    {
        await CompanyStartExecutionWorkflow.ExecuteAsync<TFaction, TEntry, TCaptainStats>(
            new CompanyStartExecutionRequest<TFaction, TEntry, TCaptainStats>
            {
                CompanyName = companyName,
                SetCompanyNameValidationError = SetCompanyNameValidationError,
                BuildSaveRequest = () => new CompanyStartSaveRequest<TFaction, TEntry, TCaptainStats>
                {
                    CompanyName = (companyName ?? string.Empty).Trim(),
                    CompanyType = CompanyTypeLabel,
                    MercsCompanyEntries = mercsCompanyEntries,
                    ShowRightSelectionBox = showRightSelectionBox,
                    LeftSlotFaction = leftSlotFaction,
                    RightSlotFaction = rightSlotFaction,
                    Factions = factions,
                    ArmyDataService = ArmyDataService,
                    SpecOpsProvider = specOpsProvider,
                    Navigation = Navigation,
                    ShowUnitsInInches = showUnitsInInches,
                    SelectedStartSeasonPoints = selectedStartSeasonPoints,
                    SeasonPointsCapText = seasonPointsCapText,
                    TryGetMetadataFactionName = tryGetMetadataFactionName,
                    ReadCaptainName = readCaptainName,
                    DisplayAlertAsync = (title, message, cancel) => DisplayAlert(title, message, cancel),
                    NavigateToCompanyViewerAsync = async filePath =>
                    {
                        var encodedPath = Uri.EscapeDataString(filePath);
                        await Shell.Current.GoToAsync($"//{nameof(CompanyViewerPage)}?companyFilePath={encodedPath}");
                    }
                },
                HandleFailureAsync = async ex =>
                {
                    Console.Error.WriteLine($"CompanySelectionPage StartCompanyAsync failed: {ex}");
                    await DisplayAlert("Save Failed", ex.Message, "OK");
                }
            });
    }

    /// <summary>
    /// Wires shared UnitDisplayConfigurationsView events to page handlers.
    /// Derived pages should call this once after InitializeComponent.
    /// </summary>
    protected void WireUnitDisplayEvents(
        UnitDisplayConfigurationsView unitDisplayView,
        EventHandler<SKPaintSurfaceEventArgs> onHeaderIconsPaintSurface,
        EventHandler<SKPaintSurfaceEventArgs> onSelectedUnitPaintSurface,
        EventHandler<SKPaintSurfaceEventArgs> onPeripheralIconPaintSurface,
        EventHandler<SKPaintSurfaceEventArgs> onProfileTacticalPaintSurface,
        EventHandler<EventArgs> onUnitNameHeadingSizeChanged)
    {
        unitDisplayView.HeaderIconsCanvasPaintSurface += onHeaderIconsPaintSurface;
        unitDisplayView.SelectedUnitCanvasPaintSurface += onSelectedUnitPaintSurface;
        unitDisplayView.PeripheralIconCanvasPaintSurface += onPeripheralIconPaintSurface;
        unitDisplayView.ProfileTacticalIconCanvasPaintSurface += onProfileTacticalPaintSurface;
        unitDisplayView.UnitNameHeadingSizeChanged += onUnitNameHeadingSizeChanged;
    }

    protected abstract Task LoadFactionsAsync();
    protected abstract Task LoadUnitsForActiveSlotAsync();
    protected abstract Task LoadSelectedUnitDetailsAsync();
    protected abstract Task StartCompanyAsync();
}


