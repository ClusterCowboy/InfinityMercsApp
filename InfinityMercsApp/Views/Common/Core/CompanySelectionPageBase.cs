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
    private bool _isFactionSelectionActive = true;
    private bool _lieutenantOnlyUnits;
    private bool _showFireteams;
    private string _profilesStatus = "Select a unit.";

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
        _showFireteams = DefaultTeamsView;
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
    protected virtual bool DefaultTeamsView => false;
    protected virtual bool RequireTeamEntriesReadyForTeamsList => false;
    protected virtual bool AreTeamEntriesReadyForTeamsList => true;
    protected abstract void ApplyLieutenantVisualStatesFromBase();
    protected abstract Task ApplyUnitVisibilityFiltersFromBaseAsync();

    public bool IsFactionSelectionActive
    {
        get => _isFactionSelectionActive;
        set
        {
            if (_isFactionSelectionActive == value)
            {
                return;
            }

            _isFactionSelectionActive = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsUnitSelectionActive));
            OnPropertyChanged(nameof(ShowUnitsList));
            OnPropertyChanged(nameof(ShowTeamsList));
        }
    }

    public bool IsUnitSelectionActive => !_isFactionSelectionActive;

    public bool LieutenantOnlyUnits
    {
        get => _lieutenantOnlyUnits;
        set
        {
            if (_lieutenantOnlyUnits == value)
            {
                return;
            }

            _lieutenantOnlyUnits = value;
            OnPropertyChanged();
            ApplyLieutenantVisualStatesFromBase();
            _ = ApplyUnitVisibilityFiltersFromBaseAsync();
        }
    }

    public bool TeamsView
    {
        get => _showFireteams;
        set
        {
            if (_showFireteams == value)
            {
                return;
            }

            _showFireteams = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ShowUnitsList));
            OnPropertyChanged(nameof(ShowTeamsList));
        }
    }

    public bool ShowUnitsList => !TeamsView;

    public bool ShowTeamsList =>
        TeamsView &&
        (!RequireTeamEntriesReadyForTeamsList ||
         (IsUnitSelectionActive && AreTeamEntriesReadyForTeamsList));

    public string ProfilesStatus
    {
        get => _profilesStatus;
        protected set
        {
            if (_profilesStatus == value)
            {
                return;
            }

            _profilesStatus = value;
            OnPropertyChanged();
        }
    }

    public string UnitNameHeading
    {
        get => UnitDisplayConfigurationsViewForVisuals.UnitNameHeading;
        protected set
        {
            if (UnitDisplayConfigurationsViewForVisuals.UnitNameHeading == value)
            {
                return;
            }

            UnitDisplayConfigurationsViewForVisuals.UnitNameHeading = value;
            UpdateUnitNameHeadingFontSize();
        }
    }

    public double UnitNameHeadingFontSize
    {
        get => UnitDisplayConfigurationsViewForVisuals.UnitNameHeadingFontSize;
        protected set
        {
            if (Math.Abs(UnitDisplayConfigurationsViewForVisuals.UnitNameHeadingFontSize - value) < 0.01d)
            {
                return;
            }

            UnitDisplayConfigurationsViewForVisuals.UnitNameHeadingFontSize = value;
        }
    }

    public Color UnitHeaderPrimaryColor
    {
        get => UnitDisplayConfigurationsViewForVisuals.UnitHeaderPrimaryColor;
        protected set
        {
            if (UnitDisplayConfigurationsViewForVisuals.UnitHeaderPrimaryColor == value)
            {
                return;
            }

            UnitDisplayConfigurationsViewForVisuals.UnitHeaderPrimaryColor = value;
        }
    }

    public Color UnitHeaderSecondaryColor
    {
        get => UnitDisplayConfigurationsViewForVisuals.UnitHeaderSecondaryColor;
        protected set
        {
            if (UnitDisplayConfigurationsViewForVisuals.UnitHeaderSecondaryColor == value)
            {
                return;
            }

            UnitDisplayConfigurationsViewForVisuals.UnitHeaderSecondaryColor = value;
        }
    }

    public Color UnitHeaderPrimaryTextColor
    {
        get => UnitDisplayConfigurationsViewForVisuals.UnitHeaderPrimaryTextColor;
        protected set
        {
            if (UnitDisplayConfigurationsViewForVisuals.UnitHeaderPrimaryTextColor == value)
            {
                return;
            }

            UnitDisplayConfigurationsViewForVisuals.UnitHeaderPrimaryTextColor = value;
        }
    }

    public Color UnitHeaderSecondaryTextColor
    {
        get => UnitDisplayConfigurationsViewForVisuals.UnitHeaderSecondaryTextColor;
        protected set
        {
            if (UnitDisplayConfigurationsViewForVisuals.UnitHeaderSecondaryTextColor == value)
            {
                return;
            }

            UnitDisplayConfigurationsViewForVisuals.UnitHeaderSecondaryTextColor = value;
        }
    }

    public string UnitMov { get => UnitDisplayConfigurationsViewForVisuals.UnitMov; protected set => UnitDisplayConfigurationsViewForVisuals.UnitMov = value; }
    public string UnitCc { get => UnitDisplayConfigurationsViewForVisuals.UnitCc; protected set => UnitDisplayConfigurationsViewForVisuals.UnitCc = value; }
    public string UnitBs { get => UnitDisplayConfigurationsViewForVisuals.UnitBs; protected set => UnitDisplayConfigurationsViewForVisuals.UnitBs = value; }
    public string UnitPh { get => UnitDisplayConfigurationsViewForVisuals.UnitPh; protected set => UnitDisplayConfigurationsViewForVisuals.UnitPh = value; }
    public string UnitWip { get => UnitDisplayConfigurationsViewForVisuals.UnitWip; protected set => UnitDisplayConfigurationsViewForVisuals.UnitWip = value; }
    public string UnitArm { get => UnitDisplayConfigurationsViewForVisuals.UnitArm; protected set => UnitDisplayConfigurationsViewForVisuals.UnitArm = value; }
    public string UnitBts { get => UnitDisplayConfigurationsViewForVisuals.UnitBts; protected set => UnitDisplayConfigurationsViewForVisuals.UnitBts = value; }
    public string UnitVitalityHeader { get => UnitDisplayConfigurationsViewForVisuals.UnitVitalityHeader; protected set => UnitDisplayConfigurationsViewForVisuals.UnitVitalityHeader = value; }
    public string UnitVitality { get => UnitDisplayConfigurationsViewForVisuals.UnitVitality; protected set => UnitDisplayConfigurationsViewForVisuals.UnitVitality = value; }
    public string UnitS { get => UnitDisplayConfigurationsViewForVisuals.UnitS; protected set => UnitDisplayConfigurationsViewForVisuals.UnitS = value; }
    public string UnitAva { get => UnitDisplayConfigurationsViewForVisuals.UnitAva; protected set => UnitDisplayConfigurationsViewForVisuals.UnitAva = value; }
    public bool HasPeripheralStatBlock { get => UnitDisplayConfigurationsViewForVisuals.HasPeripheralStatBlock; protected set => UnitDisplayConfigurationsViewForVisuals.HasPeripheralStatBlock = value; }
    public string PeripheralNameHeading { get => UnitDisplayConfigurationsViewForVisuals.PeripheralNameHeading; protected set => UnitDisplayConfigurationsViewForVisuals.PeripheralNameHeading = value; }
    public string PeripheralMov { get => UnitDisplayConfigurationsViewForVisuals.PeripheralMov; protected set => UnitDisplayConfigurationsViewForVisuals.PeripheralMov = value; }
    public string PeripheralCc { get => UnitDisplayConfigurationsViewForVisuals.PeripheralCc; protected set => UnitDisplayConfigurationsViewForVisuals.PeripheralCc = value; }
    public string PeripheralBs { get => UnitDisplayConfigurationsViewForVisuals.PeripheralBs; protected set => UnitDisplayConfigurationsViewForVisuals.PeripheralBs = value; }
    public string PeripheralPh { get => UnitDisplayConfigurationsViewForVisuals.PeripheralPh; protected set => UnitDisplayConfigurationsViewForVisuals.PeripheralPh = value; }
    public string PeripheralWip { get => UnitDisplayConfigurationsViewForVisuals.PeripheralWip; protected set => UnitDisplayConfigurationsViewForVisuals.PeripheralWip = value; }
    public string PeripheralArm { get => UnitDisplayConfigurationsViewForVisuals.PeripheralArm; protected set => UnitDisplayConfigurationsViewForVisuals.PeripheralArm = value; }
    public string PeripheralBts { get => UnitDisplayConfigurationsViewForVisuals.PeripheralBts; protected set => UnitDisplayConfigurationsViewForVisuals.PeripheralBts = value; }
    public string PeripheralVitalityHeader { get => UnitDisplayConfigurationsViewForVisuals.PeripheralVitalityHeader; protected set => UnitDisplayConfigurationsViewForVisuals.PeripheralVitalityHeader = value; }
    public string PeripheralVitality { get => UnitDisplayConfigurationsViewForVisuals.PeripheralVitality; protected set => UnitDisplayConfigurationsViewForVisuals.PeripheralVitality = value; }
    public string PeripheralS { get => UnitDisplayConfigurationsViewForVisuals.PeripheralS; protected set => UnitDisplayConfigurationsViewForVisuals.PeripheralS = value; }
    public string PeripheralAva { get => UnitDisplayConfigurationsViewForVisuals.PeripheralAva; protected set => UnitDisplayConfigurationsViewForVisuals.PeripheralAva = value; }

    public string PeripheralEquipment
    {
        get => UnitDisplayConfigurationsViewForVisuals.PeripheralEquipment;
        protected set
        {
            if (UnitDisplayConfigurationsViewForVisuals.PeripheralEquipment == value)
            {
                return;
            }

            UnitDisplayConfigurationsViewForVisuals.PeripheralEquipment = value;
        }
    }

    public string PeripheralSkills
    {
        get => UnitDisplayConfigurationsViewForVisuals.PeripheralSkills;
        protected set
        {
            if (UnitDisplayConfigurationsViewForVisuals.PeripheralSkills == value)
            {
                return;
            }

            UnitDisplayConfigurationsViewForVisuals.PeripheralSkills = value;
        }
    }

    public string EquipmentSummary
    {
        get => UnitDisplayConfigurationsViewForVisuals.EquipmentSummary;
        protected set
        {
            if (UnitDisplayConfigurationsViewForVisuals.EquipmentSummary != value)
            {
                UnitDisplayConfigurationsViewForVisuals.EquipmentSummary = value;
            }
        }
    }

    public string SpecialSkillsSummary
    {
        get => UnitDisplayConfigurationsViewForVisuals.SpecialSkillsSummary;
        protected set
        {
            if (UnitDisplayConfigurationsViewForVisuals.SpecialSkillsSummary != value)
            {
                UnitDisplayConfigurationsViewForVisuals.SpecialSkillsSummary = value;
            }
        }
    }

    public FormattedString EquipmentSummaryFormatted
    {
        get => UnitDisplayConfigurationsViewForVisuals.EquipmentSummaryFormatted;
        protected set => UnitDisplayConfigurationsViewForVisuals.EquipmentSummaryFormatted = value;
    }

    public FormattedString SpecialSkillsSummaryFormatted
    {
        get => UnitDisplayConfigurationsViewForVisuals.SpecialSkillsSummaryFormatted;
        protected set => UnitDisplayConfigurationsViewForVisuals.SpecialSkillsSummaryFormatted = value;
    }

    public FormattedString PeripheralEquipmentFormatted
    {
        get => UnitDisplayConfigurationsViewForVisuals.PeripheralEquipmentFormatted;
        protected set => UnitDisplayConfigurationsViewForVisuals.PeripheralEquipmentFormatted = value;
    }

    public FormattedString PeripheralSkillsFormatted
    {
        get => UnitDisplayConfigurationsViewForVisuals.PeripheralSkillsFormatted;
        protected set => UnitDisplayConfigurationsViewForVisuals.PeripheralSkillsFormatted = value;
    }

    public bool HasPeripheralEquipment => UnitDisplayConfigurationsViewForVisuals.HasPeripheralEquipment;
    public bool HasPeripheralSkills => UnitDisplayConfigurationsViewForVisuals.HasPeripheralSkills;
    public bool HasAnyTopHeaderIcons => ShowRegularOrderIcon || ShowIrregularOrderIcon || ShowImpetuousIcon || ShowTacticalAwarenessIcon;
    public bool HasAnyBottomHeaderIcons => ShowCubeIcon || ShowCube2Icon || ShowHackableIcon;
    public bool HasAnyHeaderIcons => HasAnyTopHeaderIcons || HasAnyBottomHeaderIcons;

    public bool ShowRegularOrderIcon
    {
        get => UnitDisplayConfigurationsViewForVisuals.ShowRegularOrderIcon;
        protected set => SetAndNotifyUnitHeaderIconFlag(
            () => UnitDisplayConfigurationsViewForVisuals.ShowRegularOrderIcon,
            x => UnitDisplayConfigurationsViewForVisuals.ShowRegularOrderIcon = x,
            value);
    }

    public bool ShowIrregularOrderIcon
    {
        get => UnitDisplayConfigurationsViewForVisuals.ShowIrregularOrderIcon;
        protected set => SetAndNotifyUnitHeaderIconFlag(
            () => UnitDisplayConfigurationsViewForVisuals.ShowIrregularOrderIcon,
            x => UnitDisplayConfigurationsViewForVisuals.ShowIrregularOrderIcon = x,
            value);
    }

    public bool ShowImpetuousIcon
    {
        get => UnitDisplayConfigurationsViewForVisuals.ShowImpetuousIcon;
        protected set => SetAndNotifyUnitHeaderIconFlag(
            () => UnitDisplayConfigurationsViewForVisuals.ShowImpetuousIcon,
            x => UnitDisplayConfigurationsViewForVisuals.ShowImpetuousIcon = x,
            value);
    }

    public bool ShowTacticalAwarenessIcon
    {
        get => UnitDisplayConfigurationsViewForVisuals.ShowTacticalAwarenessIcon;
        protected set => SetAndNotifyUnitHeaderIconFlag(
            () => UnitDisplayConfigurationsViewForVisuals.ShowTacticalAwarenessIcon,
            x => UnitDisplayConfigurationsViewForVisuals.ShowTacticalAwarenessIcon = x,
            value);
    }

    public bool ShowCubeIcon
    {
        get => UnitDisplayConfigurationsViewForVisuals.ShowCubeIcon;
        protected set => SetAndNotifyUnitHeaderIconFlag(
            () => UnitDisplayConfigurationsViewForVisuals.ShowCubeIcon,
            x => UnitDisplayConfigurationsViewForVisuals.ShowCubeIcon = x,
            value);
    }

    public bool ShowCube2Icon
    {
        get => UnitDisplayConfigurationsViewForVisuals.ShowCube2Icon;
        protected set => SetAndNotifyUnitHeaderIconFlag(
            () => UnitDisplayConfigurationsViewForVisuals.ShowCube2Icon,
            x => UnitDisplayConfigurationsViewForVisuals.ShowCube2Icon = x,
            value);
    }

    public bool ShowHackableIcon
    {
        get => UnitDisplayConfigurationsViewForVisuals.ShowHackableIcon;
        protected set => SetAndNotifyUnitHeaderIconFlag(
            () => UnitDisplayConfigurationsViewForVisuals.ShowHackableIcon,
            x => UnitDisplayConfigurationsViewForVisuals.ShowHackableIcon = x,
            value);
    }

    protected bool ShowUnitsInInches
    {
        get => UnitDisplayConfigurationsViewForVisuals.ShowUnitsInInches;
        set => UnitDisplayConfigurationsViewForVisuals.ShowUnitsInInches = value;
    }

    protected int? UnitMoveFirstCm
    {
        get => UnitDisplayConfigurationsViewForVisuals.UnitMoveFirstCm;
        set => UnitDisplayConfigurationsViewForVisuals.UnitMoveFirstCm = value;
    }

    protected int? UnitMoveSecondCm
    {
        get => UnitDisplayConfigurationsViewForVisuals.UnitMoveSecondCm;
        set => UnitDisplayConfigurationsViewForVisuals.UnitMoveSecondCm = value;
    }

    protected int? PeripheralMoveFirstCm
    {
        get => UnitDisplayConfigurationsViewForVisuals.PeripheralMoveFirstCm;
        set => UnitDisplayConfigurationsViewForVisuals.PeripheralMoveFirstCm = value;
    }

    protected int? PeripheralMoveSecondCm
    {
        get => UnitDisplayConfigurationsViewForVisuals.PeripheralMoveSecondCm;
        set => UnitDisplayConfigurationsViewForVisuals.PeripheralMoveSecondCm = value;
    }

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

    protected bool SetAndNotifyUnitHeaderIconFlag(
        Func<bool> readCurrent,
        Action<bool> writeCurrent,
        bool value)
    {
        if (readCurrent() == value)
        {
            return false;
        }

        writeCurrent(value);
        OnPropertyChanged();
        OnPropertyChanged("HasAnyTopHeaderIcons");
        OnPropertyChanged("HasAnyBottomHeaderIcons");
        OnPropertyChanged("HasAnyHeaderIcons");
        UnitDisplayConfigurationsViewForVisuals.InvalidateHeaderIconsCanvas();
        return true;
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


