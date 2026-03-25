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
    // Loaded asynchronously at startup; used by both filter canvas variants.
    private SKPicture? _filterIconPicture;

    // True when the unit search/filter is active; drives which filter icon canvas is shown.
    private bool _isUnitFilterActive;

    private bool _showFactionStrip = true;

    // When true, only lieutenant-eligible units are shown in the unit list.
    private bool _lieutenantOnlyUnits;

    // Toggles between the flat unit list and the fireteam composition view.
    private bool _showFireteams;

    private string _profilesStatus = "Select a unit.";

    /// <summary>
    /// Initialises the base page, storing all injected providers and seeding the fireteam view
    /// toggle from the subclass's <see cref="DefaultTeamsView"/> preference.
    /// </summary>
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
        // Unit list is always the active selection surface.
        _showFireteams = false;
    }

    /// <summary>Gets the mode (standard vs cohesive) that controls which factions and rules apply.</summary>
    protected ArmySourceSelectionMode Mode { get; }
    protected IMetadataProvider? MetadataProvider { get; }
    protected IFactionProvider? FactionProvider { get; }
    protected ISpecOpsProvider SpecOpsProvider { get; }
    protected ICohesiveCompanyFactionQueryProvider CohesiveCompanyFactionQueryProvider { get; }
    protected FactionLogoCacheService? FactionLogoCacheService { get; }
    protected IAppSettingsProvider? AppSettingsProvider { get; }

    /// <summary>Provides army data queries; implemented by each concrete page's DI setup.</summary>
    protected abstract IArmyDataService ArmyDataService { get; }

    // The following abstract members expose the concrete XAML-bound controls to base class logic
    // without the base class needing to know the specific view hierarchy of each subclass.
    protected abstract FactionSlotSelectorView FactionSlotSelectorViewForVisuals { get; }
    protected abstract UnitDisplayConfigurationsView UnitDisplayConfigurationsViewForVisuals { get; }
    protected abstract SKCanvasView UnitSelectionFilterCanvasInactiveForVisuals { get; }
    protected abstract SKCanvasView UnitSelectionFilterCanvasActiveForVisuals { get; }

    /// <summary>Whether the equipment/skills summary should bold lieutenant entries.</summary>
    protected abstract bool SummaryHighlightLieutenantForVisuals { get; }
    protected abstract Color UnitHeaderSecondaryColorForVisuals { get; }
    protected abstract void SetUnitHeaderPrimaryColorForVisuals(Color value);
    protected abstract void SetUnitHeaderSecondaryColorForVisuals(Color value);
    protected abstract void SetUnitHeaderPrimaryTextColorForVisuals(Color value);
    protected abstract void SetUnitHeaderSecondaryTextColorForVisuals(Color value);
    protected abstract void SetEquipmentSummaryFormattedForVisuals(FormattedString value);
    protected abstract void SetSpecialSkillsSummaryFormattedForVisuals(FormattedString value);

    /// <summary>Human-readable label for the company type, used when saving the company file.</summary>
    protected abstract string CompanyTypeLabel { get; }
    protected abstract void SetShowCompanyNameValidationError(bool value);
    protected abstract void SetCompanyNameBorderColor(Color value);

    /// <summary>
    /// When <c>true</c>, the page opens in fireteam view rather than flat unit list.
    /// Defaults to <c>false</c>; override in cohesive-company pages.
    /// </summary>
    protected virtual bool DefaultTeamsView => false;

    /// <summary>
    /// When <c>true</c>, the teams list is only shown after the team entries collection is ready.
    /// Defaults to <c>false</c>; cohesive pages override this to prevent empty-list flicker.
    /// </summary>
    protected virtual bool RequireTeamEntriesReadyForTeamsList => false;

    /// <summary>
    /// Indicates whether the team entries collection has been fully populated.
    /// Always <c>true</c> in the base; overridden in pages that build entries asynchronously.
    /// </summary>
    protected virtual bool AreTeamEntriesReadyForTeamsList => true;

    protected abstract void ApplyLieutenantVisualStatesFromBase();
    protected abstract Task ApplyUnitVisibilityFiltersFromBaseAsync();

    /// <summary>
    /// Updates the filter-active flag and forces both filter icon canvases to repaint
    /// so the active/inactive states reflect the new value immediately.
    /// </summary>
    protected void SetIsUnitFilterActive(bool value)
    {
        _isUnitFilterActive = value;
        UnitSelectionFilterCanvasInactiveForVisuals.InvalidateSurface();
        UnitSelectionFilterCanvasActiveForVisuals.InvalidateSurface();
    }

    /// <summary>Controls visibility of the top horizontal faction strip.</summary>
    public bool ShowFactionStrip
    {
        get => _showFactionStrip;
        set
        {
            if (_showFactionStrip == value)
            {
                return;
            }

            _showFactionStrip = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// When set to <c>true</c>, only lieutenant-eligible units are shown in the list
    /// and the lieutenant visual states are refreshed immediately.
    /// </summary>
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

    /// <summary>
    /// Toggles between the flat unit list and the fireteam composition view.
    /// Notifies <see cref="ShowUnitsList"/> and <see cref="ShowTeamsList"/> on change.
    /// </summary>
    public bool TeamsView
    {
        get => _showFireteams;
        set
        {
            // Team-selection mode is retired; keep unit list active.
            const bool normalizedValue = false;
            if (_showFireteams == normalizedValue)
            {
                return;
            }

            _showFireteams = normalizedValue;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ShowUnitsList));
            OnPropertyChanged(nameof(ShowTeamsList));
        }
    }

    /// <summary><c>true</c> because unit selection is always visible.</summary>
    public bool ShowUnitsList => true;

    /// <summary>
    /// <c>true</c> when the fireteam list should be visible.
    /// Guards against showing the list before team entries are populated when
    /// <see cref="RequireTeamEntriesReadyForTeamsList"/> is active.
    /// </summary>
    public bool ShowTeamsList => false;

    /// <summary>
    /// Status text displayed beneath the profile list (e.g. "Select a unit." or profile load errors).
    /// Bound to the UI; only notifies bindings when the value actually changes.
    /// </summary>
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

    /// <summary>
    /// The unit name shown as the primary heading in the detail panel.
    /// Automatically recalculates the font size when set so long names scale down gracefully.
    /// </summary>
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

    /// <summary>
    /// Font size for the unit name heading, adjusted dynamically based on name length.
    /// Uses a small epsilon comparison to avoid spurious property-change notifications for floating-point deltas.
    /// </summary>
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
    // Aggregate icon visibility flags used to conditionally show/hide the icon rows in the header.
    public bool HasAnyTopHeaderIcons => ShowRegularOrderIcon || ShowIrregularOrderIcon || ShowImpetuousIcon || ShowTacticalAwarenessIcon;
    public bool HasAnyBottomHeaderIcons => ShowCubeIcon || ShowCube2Icon || ShowHackableIcon;
    public bool HasAnyHeaderIcons => HasAnyTopHeaderIcons || HasAnyBottomHeaderIcons;

    // Each icon property delegates through SetAndNotifyUnitHeaderIconFlag so that any change
    // also triggers the aggregate HasAny* notifications and invalidates the icon canvas.
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

    /// <summary>
    /// Creates a bindable Command that invokes the faction-selection handler,
    /// logging the selection for diagnostics.
    /// </summary>
    protected Command CreateSelectFactionCommand<TFaction>(Action<TFaction> setSelectedFaction)
        where TFaction : class
    {
        return CompanySelectionPageInteractionWorkflow.CreateSelectFactionCommand(setSelectedFaction);
    }

    /// <summary>
    /// Creates a bindable Command that invokes the unit-selection handler,
    /// logging the unit identity for diagnostics.
    /// </summary>
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

    /// <summary>
    /// Creates a bindable Command that wraps the async start-company flow,
    /// wiring up the <paramref name="canExecute"/> guard to prevent double-submission.
    /// </summary>
    protected Command CreateStartCompanyCommand(Func<Task> startCompanyAsync, Func<bool> canExecute)
    {
        return CompanySelectionPageInteractionWorkflow.CreateStartCompanyCommand(startCompanyAsync, canExecute);
    }

    /// <summary>
    /// Subscribes tap handlers to both faction slot buttons so tapping a slot updates
    /// the active slot index and respects the right-slot visibility state.
    /// </summary>
    protected void WireFactionSlotTapHandlers(Action<int> setActiveSlot, Func<bool> showRightSelectionBox)
    {
        CompanySelectionPageInteractionWorkflow.WireFactionSlotTapHandlers(
            FactionSlotSelectorViewForVisuals,
            setActiveSlot,
            showRightSelectionBox);
    }

    /// <summary>
    /// Completes page setup by setting the BindingContext, selecting the initial faction slot,
    /// and kicking off the summary and header icon loads.
    /// Should be called at the end of each concrete page's constructor or OnAppearing.
    /// </summary>
    protected void FinalizePageInitialization(Action setInitialActiveSlot)
    {
        CompanySelectionPageInteractionWorkflow.FinalizePageInitialization(
            () => BindingContext = this,
            setInitialActiveSlot,
            RefreshSummaryFormatted,
            LoadHeaderIconsAsync);
    }

    /// <summary>
    /// Handles a tap on a team-allowed-profile item, scrolling the unit list to the matching unit
    /// and optionally restricting the profile selection to a subset of eligible profiles.
    /// </summary>
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

    /// <summary>Loads the faction list appropriate for the current mode and populates the faction picker.</summary>
    protected abstract Task LoadFactionsAsync();

    /// <summary>Loads units for whichever faction slot is currently active.</summary>
    protected abstract Task LoadUnitsForActiveSlotAsync();

    /// <summary>Loads the full detail data (profiles, equipment, skills) for the currently selected unit.</summary>
    protected abstract Task LoadSelectedUnitDetailsAsync();

    /// <summary>Validates the company and persists it to disk, then navigates to the company viewer.</summary>
    protected abstract Task StartCompanyAsync();
}


