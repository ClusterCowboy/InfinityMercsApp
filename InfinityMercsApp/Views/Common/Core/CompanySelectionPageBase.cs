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

    protected Command CreateSelectFactionCommand<TFaction>(Action<TFaction> setSelectedFaction)
        where TFaction : class
    {
        return CompanySelectionPageInteractionWorkflow.CreateSelectFactionCommand(setSelectedFaction);
    }

    protected Command CreateSelectUnitCommand<TUnit>(
        Action<TUnit> setSelectedUnit,
        Func<TUnit, int> readUnitId,
        Func<TUnit, int> readSourceFactionId,
        Func<TUnit, string?> readUnitName)
        where TUnit : class
    {
        return CompanySelectionPageInteractionWorkflow.CreateSelectUnitCommand(
            setSelectedUnit,
            readUnitId,
            readSourceFactionId,
            readUnitName);
    }

    protected Command CreateStartCompanyCommand(Func<Task> startCompanyAsync, Func<bool> canExecute)
    {
        return CompanySelectionPageInteractionWorkflow.CreateStartCompanyCommand(startCompanyAsync, canExecute);
    }

    protected void WireFactionSlotTapHandlers(Action<int> setActiveSlot, Func<bool> showRightSelectionBox)
    {
        CompanySelectionPageInteractionWorkflow.WireFactionSlotTapHandlers(
            FactionSlotSelectorViewForVisuals,
            setActiveSlot,
            showRightSelectionBox);
    }

    protected void FinalizePageInitialization(Action setInitialActiveSlot)
    {
        CompanySelectionPageInteractionWorkflow.FinalizePageInitialization(
            () => BindingContext = this,
            setInitialActiveSlot,
            RefreshSummaryFormatted,
            LoadHeaderIconsAsync);
    }

    protected void HandleTeamAllowedProfileSelected<TTeamItem, TUnit>(
        TTeamItem? teamItem,
        IEnumerable<TUnit> units,
        Action<TUnit, bool> applySelection,
        Func<TTeamItem, bool>? shouldRestrictProfiles = null)
        where TTeamItem : CompanyTeamUnitLimitItemBase
        where TUnit : CompanyUnitSelectionItemBase
    {
        CompanySelectionPageInteractionWorkflow.HandleTeamAllowedProfileSelected(
            teamItem,
            units,
            applySelection,
            shouldRestrictProfiles);
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
        CompanySelectionPageInteractionWorkflow.WireUnitDisplayEvents(
            unitDisplayView,
            onHeaderIconsPaintSurface,
            onSelectedUnitPaintSurface,
            onPeripheralIconPaintSurface,
            onProfileTacticalPaintSurface,
            onUnitNameHeadingSizeChanged);
    }

    protected abstract Task LoadFactionsAsync();
    protected abstract Task LoadUnitsForActiveSlotAsync();
    protected abstract Task LoadSelectedUnitDetailsAsync();
    protected abstract Task StartCompanyAsync();
}


