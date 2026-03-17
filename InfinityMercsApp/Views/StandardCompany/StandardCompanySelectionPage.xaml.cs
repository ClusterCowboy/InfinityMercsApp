using System.Collections.ObjectModel;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows.Input;
using InfinityMercsApp.Domain.Utilities;
using InfinityMercsApp.Infrastructure.Providers;
using InfinityMercsApp.Services;
using InfinityMercsApp.ViewModels;
using InfinityMercsApp.Views.Controls;
using InfinityMercsApp.Views.Common;
using Microsoft.Maui.Devices;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using Svg.Skia;
using ArmyFactionRecord = InfinityMercsApp.Domain.Models.Army.Faction;
using ArmyResumeRecord = InfinityMercsApp.Domain.Models.Army.Resume;
using ArmySpecopsEquipRecord = InfinityMercsApp.Domain.Models.Army.SpecopsEquipment;
using ArmySpecopsSkillRecord = InfinityMercsApp.Domain.Models.Army.SpecopsSkill;
using ArmySpecopsUnitRecord = InfinityMercsApp.Domain.Models.Army.SpecopsUnit;
using ArmySpecopsWeaponRecord = InfinityMercsApp.Domain.Models.Army.SpecopsWeapon;
using ArmyUnitRecord = InfinityMercsApp.Domain.Models.Army.Unit;
using FactionRecord = InfinityMercsApp.Domain.Models.Metadata.Faction;
using MercsArmyListEntry = InfinityMercsApp.Domain.Models.Army.MercsArmyListEntry;
using InfinityMercsApp.Views.Common.UICommon;

namespace InfinityMercsApp.Views.StandardCompany;

public partial class StandardCompanySelectionPage : CompanySelectionPageBase, ICompanySelectionVisibilityState
{
    private readonly ArmySourceSelectionMode _mode;
    private readonly IArmyDataService _armyDataService;
    private readonly ISpecOpsProvider _specOpsProvider;
    private readonly FactionLogoCacheService? _factionLogoCacheService;
    private readonly CompanyProfileCoordinator _profileCoordinator;
    private readonly FactionSlotSelectionState<ArmyFactionSelectionItem> _factionSelectionState = new();
    private string _companyName = "Company Name";
    private readonly Command _startCompanyCommand;
    private bool _showCompanyNameValidationError;
    private Color _companyNameBorderColor = Color.FromArgb("#6B7280");
    private int _activeSlotIndex;
    private bool _loaded;
    private bool _lieutenantOnlyUnits;
    private bool _showFireteams;
    private bool _isFactionSelectionActive = true;
    private string _pageHeading = string.Empty;
    private ArmyUnitSelectionItem? _selectedUnit;
    private string _profilesStatus = "Select a unit.";
    private bool _summaryHighlightLieutenant;
    private UnitFilterCriteria _activeUnitFilter = UnitFilterCriteria.None;
    private UnitFilterPopupView? _activeUnitFilterPopup;
    private UnitFilterPopupOptions? _preparedUnitFilterPopupOptions;

    public StandardCompanySelectionPage(
        ArmySourceSelectionMode mode,
        IMetadataProvider? metadataProvider,
        IFactionProvider? factionProvider,
        ISpecOpsProvider specOpsProvider,
        ICohesiveCompanyFactionQueryProvider cohesiveCompanyFactionQueryProvider,
        FactionLogoCacheService? factionLogoCacheService,
        IAppSettingsProvider? appSettingsProvider,
        IArmyDataService armyDataService)
        : base(mode, metadataProvider, factionProvider, specOpsProvider, cohesiveCompanyFactionQueryProvider, factionLogoCacheService, appSettingsProvider)
    {
        InitializeComponent();
        FactionSlotSelectorView.LeftSlotTapped += (_, _) => SetActiveSlot(0);
        FactionSlotSelectorView.RightSlotTapped += (_, _) =>
        {
            if (ShowRightSelectionBox)
            {
                SetActiveSlot(1);
            }
        };
        _mode = Mode;
        Title = _mode == ArmySourceSelectionMode.VanillaFactions
            ? "Choose your faction:"
            : "Choose your sectorials";
        PageHeading = _mode == ArmySourceSelectionMode.VanillaFactions
            ? "Choose your faction:"
            : "Choose your sectorials";

        _armyDataService = armyDataService;
        _specOpsProvider = SpecOpsProvider;
        _factionLogoCacheService = FactionLogoCacheService;
        _profileCoordinator = new CompanyProfileCoordinator();

        SelectFactionCommand = new Command<ArmyFactionSelectionItem>(item =>
        {
            if (item is null)
            {
                return;
            }

            SetSelectedFaction(item);
        });
        SelectUnitCommand = new Command<ArmyUnitSelectionItem>(item =>
        {
            if (item is null)
            {
                Console.Error.WriteLine("ArmyFactionSelectionPage SelectUnitCommand invoked with null item.");
                return;
            }

            Console.WriteLine($"ArmyFactionSelectionPage SelectUnitCommand: id={item.Id}, faction={item.SourceFactionId}, name='{item.Name}'.");
            SetSelectedUnit(item);
        });
        AddProfileToMercsCompanyCommand = new Command<ViewerProfileItem>(AddProfileToMercsCompany);
        RemoveMercsCompanyEntryCommand = new Command<MercsCompanyEntry>(RemoveMercsCompanyEntry);
        SelectMercsCompanyEntryCommand = new Command<MercsCompanyEntry>(entry => _ = SelectMercsCompanyEntryAsync(entry));
        SelectTeamAllowedProfileCommand = new Command<ArmyTeamUnitLimitItem>(teamItem =>
            HandleTeamAllowedProfileSelected(
                teamItem,
                Units,
                (resolved, _) => SetSelectedUnit(resolved)));
        _startCompanyCommand = new Command(async () => await StartCompanyAsync(), () => IsCompanyValid);
        StartCompanyCommand = _startCompanyCommand;

        BindingContext = this;
        SetActiveSlot(0);
        RefreshSummaryFormatted();
        _ = LoadHeaderIconsAsync();
    }

    public ObservableCollection<ArmyFactionSelectionItem> Factions { get; } = [];
    public ObservableCollection<ArmyUnitSelectionItem> Units { get; } = [];
    public ObservableCollection<ArmyTeamListItem> TeamEntries { get; } = [];
    public ObservableCollection<ViewerProfileItem> Profiles { get; } = [];
    public ObservableCollection<MercsCompanyEntry> MercsCompanyEntries { get; } = [];

    public ICommand SelectFactionCommand { get; }
    public ICommand SelectUnitCommand { get; }
    public ICommand AddProfileToMercsCompanyCommand { get; }
    public ICommand RemoveMercsCompanyEntryCommand { get; }
    public ICommand SelectMercsCompanyEntryCommand { get; }
    public ICommand SelectTeamAllowedProfileCommand { get; }
    public ICommand StartCompanyCommand { get; }

    public bool ShowRightSelectionBox => _mode == ArmySourceSelectionMode.Sectorials;
    public string PageHeading
    {
        get => _pageHeading;
        private set
        {
            if (_pageHeading == value)
            {
                return;
            }

            _pageHeading = value;
            OnPropertyChanged();
        }
    }

    public string SelectedStartSeasonPoints
    {
        get => SeasonStartPointsView.SelectedStartSeasonPoints;
        set
        {
            if (SeasonStartPointsView.SelectedStartSeasonPoints == value)
            {
                return;
            }

            SeasonStartPointsView.SelectedStartSeasonPoints = value;
            OnPropertyChanged();
            UpdateSeasonValidationState();
            ApplyLieutenantVisualStates();
            _ = ApplyUnitVisibilityFiltersAsync();
        }
    }

    public string SeasonPointsCapText
    {
        get => SeasonStartPointsView.SeasonPointsCapText;
        set
        {
            if (SeasonStartPointsView.SeasonPointsCapText == value)
            {
                return;
            }

            SeasonStartPointsView.SeasonPointsCapText = value;
            OnPropertyChanged();
            UpdateSeasonValidationState();
            ApplyLieutenantVisualStates();
            _ = ApplyUnitVisibilityFiltersAsync();
        }
    }

    public string CompanyName
    {
        get => _companyName;
        set
        {
            if (_companyName == value)
            {
                return;
            }

            _companyName = value;
            OnPropertyChanged();
            if (_showCompanyNameValidationError && CompanyStartSharedState.IsCompanyNameValid(value))
            {
                SetCompanyNameValidationError(false);
            }
        }
    }

    public bool IsCompanyValid
    {
        get => SeasonStartPointsView.IsCompanyValid;
        private set
        {
            if (SeasonStartPointsView.IsCompanyValid == value)
            {
                return;
            }

            SeasonStartPointsView.IsCompanyValid = value;
            OnPropertyChanged();
            _startCompanyCommand.ChangeCanExecute();
        }
    }

    public bool ShowCompanyNameValidationError
    {
        get => _showCompanyNameValidationError;
        private set
        {
            if (_showCompanyNameValidationError == value)
            {
                return;
            }

            _showCompanyNameValidationError = value;
            OnPropertyChanged();
        }
    }

    public Color CompanyNameBorderColor
    {
        get => _companyNameBorderColor;
        private set
        {
            if (_companyNameBorderColor == value)
            {
                return;
            }

            _companyNameBorderColor = value;
            OnPropertyChanged();
        }
    }
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
        }
    }

    public bool IsUnitSelectionActive => !_isFactionSelectionActive;

    public string UnitNameHeading
    {
        get => UnitDisplayConfigurationsView.UnitNameHeading;
        private set
        {
            if (UnitDisplayConfigurationsView.UnitNameHeading == value)
            {
                return;
            }

            UnitDisplayConfigurationsView.UnitNameHeading = value;
            UpdateUnitNameHeadingFontSize();
        }
    }
    public double UnitNameHeadingFontSize
    {
        get => UnitDisplayConfigurationsView.UnitNameHeadingFontSize;
        private set
        {
            if (Math.Abs(UnitDisplayConfigurationsView.UnitNameHeadingFontSize - value) < 0.01d)
            {
                return;
            }

            UnitDisplayConfigurationsView.UnitNameHeadingFontSize = value;
        }
    }
    public Color UnitHeaderPrimaryColor
    {
        get => UnitDisplayConfigurationsView.UnitHeaderPrimaryColor;
        private set
        {
            if (UnitDisplayConfigurationsView.UnitHeaderPrimaryColor == value)
            {
                return;
            }

            UnitDisplayConfigurationsView.UnitHeaderPrimaryColor = value;
        }
    }
    public Color UnitHeaderSecondaryColor
    {
        get => UnitDisplayConfigurationsView.UnitHeaderSecondaryColor;
        private set
        {
            if (UnitDisplayConfigurationsView.UnitHeaderSecondaryColor == value)
            {
                return;
            }

            UnitDisplayConfigurationsView.UnitHeaderSecondaryColor = value;
        }
    }
    public Color UnitHeaderPrimaryTextColor
    {
        get => UnitDisplayConfigurationsView.UnitHeaderPrimaryTextColor;
        private set
        {
            if (UnitDisplayConfigurationsView.UnitHeaderPrimaryTextColor == value)
            {
                return;
            }

            UnitDisplayConfigurationsView.UnitHeaderPrimaryTextColor = value;
        }
    }
    public Color UnitHeaderSecondaryTextColor
    {
        get => UnitDisplayConfigurationsView.UnitHeaderSecondaryTextColor;
        private set
        {
            if (UnitDisplayConfigurationsView.UnitHeaderSecondaryTextColor == value)
            {
                return;
            }

            UnitDisplayConfigurationsView.UnitHeaderSecondaryTextColor = value;
        }
    }
    public string UnitMov { get => UnitDisplayConfigurationsView.UnitMov; private set => UnitDisplayConfigurationsView.UnitMov = value; }
    public string UnitCc { get => UnitDisplayConfigurationsView.UnitCc; private set => UnitDisplayConfigurationsView.UnitCc = value; }
    public string UnitBs { get => UnitDisplayConfigurationsView.UnitBs; private set => UnitDisplayConfigurationsView.UnitBs = value; }
    public string UnitPh { get => UnitDisplayConfigurationsView.UnitPh; private set => UnitDisplayConfigurationsView.UnitPh = value; }
    public string UnitWip { get => UnitDisplayConfigurationsView.UnitWip; private set => UnitDisplayConfigurationsView.UnitWip = value; }
    public string UnitArm { get => UnitDisplayConfigurationsView.UnitArm; private set => UnitDisplayConfigurationsView.UnitArm = value; }
    public string UnitBts { get => UnitDisplayConfigurationsView.UnitBts; private set => UnitDisplayConfigurationsView.UnitBts = value; }
    public string UnitVitalityHeader { get => UnitDisplayConfigurationsView.UnitVitalityHeader; private set => UnitDisplayConfigurationsView.UnitVitalityHeader = value; }
    public string UnitVitality { get => UnitDisplayConfigurationsView.UnitVitality; private set => UnitDisplayConfigurationsView.UnitVitality = value; }
    public string UnitS { get => UnitDisplayConfigurationsView.UnitS; private set => UnitDisplayConfigurationsView.UnitS = value; }
    public string UnitAva { get => UnitDisplayConfigurationsView.UnitAva; private set => UnitDisplayConfigurationsView.UnitAva = value; }
    public bool HasPeripheralStatBlock { get => UnitDisplayConfigurationsView.HasPeripheralStatBlock; private set => UnitDisplayConfigurationsView.HasPeripheralStatBlock = value; }
    public string PeripheralNameHeading { get => UnitDisplayConfigurationsView.PeripheralNameHeading; private set => UnitDisplayConfigurationsView.PeripheralNameHeading = value; }
    public string PeripheralMov { get => UnitDisplayConfigurationsView.PeripheralMov; private set => UnitDisplayConfigurationsView.PeripheralMov = value; }
    public string PeripheralCc { get => UnitDisplayConfigurationsView.PeripheralCc; private set => UnitDisplayConfigurationsView.PeripheralCc = value; }
    public string PeripheralBs { get => UnitDisplayConfigurationsView.PeripheralBs; private set => UnitDisplayConfigurationsView.PeripheralBs = value; }
    public string PeripheralPh { get => UnitDisplayConfigurationsView.PeripheralPh; private set => UnitDisplayConfigurationsView.PeripheralPh = value; }
    public string PeripheralWip { get => UnitDisplayConfigurationsView.PeripheralWip; private set => UnitDisplayConfigurationsView.PeripheralWip = value; }
    public string PeripheralArm { get => UnitDisplayConfigurationsView.PeripheralArm; private set => UnitDisplayConfigurationsView.PeripheralArm = value; }
    public string PeripheralBts { get => UnitDisplayConfigurationsView.PeripheralBts; private set => UnitDisplayConfigurationsView.PeripheralBts = value; }
    public string PeripheralVitalityHeader { get => UnitDisplayConfigurationsView.PeripheralVitalityHeader; private set => UnitDisplayConfigurationsView.PeripheralVitalityHeader = value; }
    public string PeripheralVitality { get => UnitDisplayConfigurationsView.PeripheralVitality; private set => UnitDisplayConfigurationsView.PeripheralVitality = value; }
    public string PeripheralS { get => UnitDisplayConfigurationsView.PeripheralS; private set => UnitDisplayConfigurationsView.PeripheralS = value; }
    public string PeripheralAva { get => UnitDisplayConfigurationsView.PeripheralAva; private set => UnitDisplayConfigurationsView.PeripheralAva = value; }
    public string PeripheralEquipment
    {
        get => UnitDisplayConfigurationsView.PeripheralEquipment;
        private set
        {
            if (UnitDisplayConfigurationsView.PeripheralEquipment == value)
            {
                return;
            }

            UnitDisplayConfigurationsView.PeripheralEquipment = value;
        }
    }
    public string PeripheralSkills
    {
        get => UnitDisplayConfigurationsView.PeripheralSkills;
        private set
        {
            if (UnitDisplayConfigurationsView.PeripheralSkills == value)
            {
                return;
            }

            UnitDisplayConfigurationsView.PeripheralSkills = value;
        }
    }
    public string EquipmentSummary { get => UnitDisplayConfigurationsView.EquipmentSummary; private set { if (UnitDisplayConfigurationsView.EquipmentSummary != value) { UnitDisplayConfigurationsView.EquipmentSummary = value; } } }
    public string SpecialSkillsSummary { get => UnitDisplayConfigurationsView.SpecialSkillsSummary; private set { if (UnitDisplayConfigurationsView.SpecialSkillsSummary != value) { UnitDisplayConfigurationsView.SpecialSkillsSummary = value; } } }
    public string ProfilesStatus { get => _profilesStatus; private set { if (_profilesStatus != value) { _profilesStatus = value; OnPropertyChanged(); } } }
    public FormattedString EquipmentSummaryFormatted { get => UnitDisplayConfigurationsView.EquipmentSummaryFormatted; private set => UnitDisplayConfigurationsView.EquipmentSummaryFormatted = value; }
    public FormattedString SpecialSkillsSummaryFormatted { get => UnitDisplayConfigurationsView.SpecialSkillsSummaryFormatted; private set => UnitDisplayConfigurationsView.SpecialSkillsSummaryFormatted = value; }
    public FormattedString PeripheralEquipmentFormatted { get => UnitDisplayConfigurationsView.PeripheralEquipmentFormatted; private set => UnitDisplayConfigurationsView.PeripheralEquipmentFormatted = value; }
    public FormattedString PeripheralSkillsFormatted { get => UnitDisplayConfigurationsView.PeripheralSkillsFormatted; private set => UnitDisplayConfigurationsView.PeripheralSkillsFormatted = value; }
    public bool HasPeripheralEquipment => UnitDisplayConfigurationsView.HasPeripheralEquipment;
    public bool HasPeripheralSkills => UnitDisplayConfigurationsView.HasPeripheralSkills;
    public bool HasAnyTopHeaderIcons => ShowRegularOrderIcon || ShowIrregularOrderIcon || ShowImpetuousIcon || ShowTacticalAwarenessIcon;
    public bool HasAnyBottomHeaderIcons => ShowCubeIcon || ShowCube2Icon || ShowHackableIcon;
    public bool HasAnyHeaderIcons => HasAnyTopHeaderIcons || HasAnyBottomHeaderIcons;

    public bool ShowRegularOrderIcon
    {
        get => UnitDisplayConfigurationsView.ShowRegularOrderIcon;
        private set
        {
            if (UnitDisplayConfigurationsView.ShowRegularOrderIcon == value)
            {
                return;
            }

            UnitDisplayConfigurationsView.ShowRegularOrderIcon = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasAnyTopHeaderIcons));
            OnPropertyChanged(nameof(HasAnyHeaderIcons));
            UnitDisplayConfigurationsView.InvalidateHeaderIconsCanvas();
        }
    }

    public bool ShowIrregularOrderIcon
    {
        get => UnitDisplayConfigurationsView.ShowIrregularOrderIcon;
        private set
        {
            if (UnitDisplayConfigurationsView.ShowIrregularOrderIcon == value)
            {
                return;
            }

            UnitDisplayConfigurationsView.ShowIrregularOrderIcon = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasAnyTopHeaderIcons));
            OnPropertyChanged(nameof(HasAnyHeaderIcons));
            UnitDisplayConfigurationsView.InvalidateHeaderIconsCanvas();
        }
    }

    public bool ShowImpetuousIcon
    {
        get => UnitDisplayConfigurationsView.ShowImpetuousIcon;
        private set
        {
            if (UnitDisplayConfigurationsView.ShowImpetuousIcon == value)
            {
                return;
            }

            UnitDisplayConfigurationsView.ShowImpetuousIcon = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasAnyTopHeaderIcons));
            OnPropertyChanged(nameof(HasAnyHeaderIcons));
            UnitDisplayConfigurationsView.InvalidateHeaderIconsCanvas();
        }
    }

    public bool ShowTacticalAwarenessIcon
    {
        get => UnitDisplayConfigurationsView.ShowTacticalAwarenessIcon;
        private set
        {
            if (UnitDisplayConfigurationsView.ShowTacticalAwarenessIcon == value)
            {
                return;
            }

            UnitDisplayConfigurationsView.ShowTacticalAwarenessIcon = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasAnyTopHeaderIcons));
            OnPropertyChanged(nameof(HasAnyHeaderIcons));
            UnitDisplayConfigurationsView.InvalidateHeaderIconsCanvas();
        }
    }

    public bool ShowCubeIcon
    {
        get => UnitDisplayConfigurationsView.ShowCubeIcon;
        private set
        {
            if (UnitDisplayConfigurationsView.ShowCubeIcon == value)
            {
                return;
            }

            UnitDisplayConfigurationsView.ShowCubeIcon = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasAnyBottomHeaderIcons));
            OnPropertyChanged(nameof(HasAnyHeaderIcons));
            UnitDisplayConfigurationsView.InvalidateHeaderIconsCanvas();
        }
    }

    public bool ShowCube2Icon
    {
        get => UnitDisplayConfigurationsView.ShowCube2Icon;
        private set
        {
            if (UnitDisplayConfigurationsView.ShowCube2Icon == value)
            {
                return;
            }

            UnitDisplayConfigurationsView.ShowCube2Icon = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasAnyBottomHeaderIcons));
            OnPropertyChanged(nameof(HasAnyHeaderIcons));
            UnitDisplayConfigurationsView.InvalidateHeaderIconsCanvas();
        }
    }

    public bool ShowHackableIcon
    {
        get => UnitDisplayConfigurationsView.ShowHackableIcon;
        private set
        {
            if (UnitDisplayConfigurationsView.ShowHackableIcon == value)
            {
                return;
            }

            UnitDisplayConfigurationsView.ShowHackableIcon = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasAnyBottomHeaderIcons));
            OnPropertyChanged(nameof(HasAnyHeaderIcons));
            UnitDisplayConfigurationsView.InvalidateHeaderIconsCanvas();
        }
    }

    private bool ShowUnitsInInches
    {
        get => UnitDisplayConfigurationsView.ShowUnitsInInches;
        set => UnitDisplayConfigurationsView.ShowUnitsInInches = value;
    }

    private int? UnitMoveFirstCm
    {
        get => UnitDisplayConfigurationsView.UnitMoveFirstCm;
        set => UnitDisplayConfigurationsView.UnitMoveFirstCm = value;
    }

    private int? UnitMoveSecondCm
    {
        get => UnitDisplayConfigurationsView.UnitMoveSecondCm;
        set => UnitDisplayConfigurationsView.UnitMoveSecondCm = value;
    }

    private int? PeripheralMoveFirstCm
    {
        get => UnitDisplayConfigurationsView.PeripheralMoveFirstCm;
        set => UnitDisplayConfigurationsView.PeripheralMoveFirstCm = value;
    }

    private int? PeripheralMoveSecondCm
    {
        get => UnitDisplayConfigurationsView.PeripheralMoveSecondCm;
        set => UnitDisplayConfigurationsView.PeripheralMoveSecondCm = value;
    }

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
            ApplyLieutenantVisualStates();
            _ = ApplyUnitVisibilityFiltersAsync();
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
    public bool ShowTeamsList => TeamsView;

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_loaded)
        {
            return;
        }

        _loaded = true;
        await LoadFactionsAsync();
    }

    protected override Task LoadFactionsAsync()
    {
        return LoadFactionsAsync(CancellationToken.None);
    }

    protected override Task LoadUnitsForActiveSlotAsync()
    {
        return LoadUnitsForActiveSlotAsync(CancellationToken.None);
    }

    protected override Task LoadSelectedUnitDetailsAsync()
    {
        return LoadSelectedUnitDetailsAsync(CancellationToken.None);
    }

    UnitFilterCriteria ICompanySelectionVisibilityState.ActiveUnitFilter => _activeUnitFilter;

}




