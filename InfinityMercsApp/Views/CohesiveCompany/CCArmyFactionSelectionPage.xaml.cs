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
using InfinityMercsApp.Views.Templates.UICommon;
using Microsoft.Maui.Devices;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using Svg.Skia;
using InfinityMercsApp.Views;
using InfinityMercsApp.Views.Templates.NewCompany;
using ArmyFactionRecord = InfinityMercsApp.Domain.Models.Army.Faction;
using ArmyResumeRecord = InfinityMercsApp.Domain.Models.Army.Resume;
using ArmySpecopsEquipRecord = InfinityMercsApp.Domain.Models.Army.SpecopsEquipment;
using ArmySpecopsSkillRecord = InfinityMercsApp.Domain.Models.Army.SpecopsSkill;
using ArmySpecopsUnitRecord = InfinityMercsApp.Domain.Models.Army.SpecopsUnit;
using ArmySpecopsWeaponRecord = InfinityMercsApp.Domain.Models.Army.SpecopsWeapon;
using ArmyUnitRecord = InfinityMercsApp.Domain.Models.Army.Unit;
using CCFactionFireteamValidityRecord = InfinityMercsApp.Domain.Models.Army.CCFactionFireteamValidityRecord;
using FactionRecord = InfinityMercsApp.Domain.Models.Metadata.Faction;
using MercsArmyListEntry = InfinityMercsApp.Domain.Models.Army.MercsArmyListEntry;

namespace InfinityMercsApp.Views.CohesiveCompany;

public partial class CCArmyFactionSelectionPage : CompanySelectionPageBase, IUnitDisplayIconState, IUnitDisplayStatState
{
    private sealed class TeamAggregate
    {
        public TeamAggregate(string name)
        {
            Name = name;
        }

        public string Name { get; }
        public int Duo { get; private set; }
        public int Haris { get; private set; }
        public int Core { get; private set; }
        public Dictionary<string, (int Min, int Max, string? Slug, bool MinAsterisk)> UnitLimits { get; } = new(StringComparer.OrdinalIgnoreCase);

        public void AddCounts(int duo, int haris, int core)
        {
            Duo += duo;
            Haris += haris;
            Core += core;
        }

        public void MergeUnitLimit(string unitName, int min, int max, string? slug, bool minAsterisk)
        {
            if (UnitLimits.TryGetValue(unitName, out var existing))
            {
                UnitLimits[unitName] = (
                    Math.Min(existing.Min, min),
                    Math.Max(existing.Max, max),
                    string.IsNullOrWhiteSpace(existing.Slug) ? slug : existing.Slug,
                    existing.MinAsterisk || minAsterisk);
                return;
            }

            UnitLimits[unitName] = (min, max, slug, minAsterisk);
        }
    }

    private const double UnitNameHeadingMaxFontSize = 24d;
    private const double UnitNameHeadingMinFontSize = 11d;
    private const double UnitNameHeadingFontStep = 0.5d;
    private const int CharacterCategoryId = 10;

    private readonly ArmySourceSelectionMode _mode;
    private readonly IArmyDataService _armyDataService;
    private readonly ISpecOpsProvider _specOpsProvider;
    private readonly FactionLogoCacheService? _factionLogoCacheService;
    private readonly IAppSettingsProvider? _appSettingsProvider;
    private readonly FactionSlotSelectionState<ArmyFactionSelectionItem> _factionSelectionState = new();    private SKPicture? _filterIconPicture;
    private string _companyName = "Company Name";
    private readonly Command _startCompanyCommand;
    private bool _showCompanyNameValidationError;
    private Color _companyNameBorderColor = Color.FromArgb("#6B7280");
    private int _activeSlotIndex;
    private bool _loaded;
    private bool _lieutenantOnlyUnits;
    private bool _showFireteams = true;
    private bool _isFactionSelectionActive = true;
    private string _pageHeading = string.Empty;
    private ArmyUnitSelectionItem? _selectedUnit;
    private bool _restrictSelectedUnitProfilesToFto;
    private string _profilesStatus = "Select a unit.";
    private bool _summaryHighlightLieutenant;
    private bool _areTeamEntriesReady;
    private UnitFilterCriteria _activeUnitFilter = UnitFilterCriteria.None;
    private UnitFilterPopupView? _activeUnitFilterPopup;
    private UnitFilterPopupOptions? _preparedUnitFilterPopupOptions;
    private readonly Dictionary<int, HashSet<string>> _validCoreFireteamsByFaction = new();
    private string _trackedFireteamName = string.Empty;
    private int _trackedFireteamLevel;
    private SKPicture? _trackedFireteamLevelPicture;
    private bool _isUpdatingTrackedTeamSelection;

    public CCArmyFactionSelectionPage(
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
        Title = "Choose your sectorial:";
        PageHeading = "Choose your sectorial:";

        _armyDataService = armyDataService;
        _specOpsProvider = SpecOpsProvider;
        _factionLogoCacheService = FactionLogoCacheService;
        _appSettingsProvider = AppSettingsProvider;

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
        SelectTeamAllowedProfileCommand = new Command<ArmyTeamUnitLimitItem>(OnTeamAllowedProfileSelected);
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

    public bool ShowRightSelectionBox => false;
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
            if (_showCompanyNameValidationError && IsCompanyNameValid(value))
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
            OnPropertyChanged(nameof(ShowUnitsList));
            OnPropertyChanged(nameof(ShowTeamsList));
        }
    }

    public string TrackedFireteamNameDisplay =>
        string.IsNullOrWhiteSpace(_trackedFireteamName) ? "Select fireteam" : _trackedFireteamName;

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

    public bool AreTeamEntriesReady
    {
        get => _areTeamEntriesReady;
        private set
        {
            if (_areTeamEntriesReady == value)
            {
                return;
            }

            _areTeamEntriesReady = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ShowTeamsList));
        }
    }

    public bool ShowUnitsList => !TeamsView;
    public bool ShowTeamsList => TeamsView && IsUnitSelectionActive && AreTeamEntriesReady;

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

    private async Task LoadFactionsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var factions = _armyDataService
                .GetMetadataFactions(includeDiscontinued: true, cancellationToken)
                .ToList();

            if (_factionLogoCacheService is not null)
            {
                await _factionLogoCacheService.CacheFactionLogosFromRecordsAsync(factions, cancellationToken);
            }

            IEnumerable<FactionRecord> filtered = factions;
            if (_mode == ArmySourceSelectionMode.VanillaFactions)
            {
                filtered = filtered.Where(x => x.Id == x.ParentId);
            }
            else
            {
                filtered = filtered.Where(x => x.Id != x.ParentId);
            }

            // Hide Non-Aligned Armies from this selector.
            filtered = filtered.Where(x => !IsNonAlignedArmyName(x.Name));

            // If both variants exist, keep only the non-all-caps "Contracted Back-Up".
            filtered = CollapseContractedBackUpVariants(filtered);

            var maxCost = int.TryParse(SelectedStartSeasonPoints, out var parsedLimit) ? parsedLimit : 0;
            var ordered = filtered.OrderBy(x => x.Name).ToList();
            var cacheFilterKey = BuildCCFactionValidityFilterKey(maxCost);
            var visibleFactions = new List<FactionRecord>();
            var factionIds = ordered.Select(x => x.Id).ToList();
            var cachedRows = await _specOpsProvider.GetCCFactionFireteamValidityAsync(
                cacheFilterKey,
                factionIds,
                cancellationToken);
            var cachedByFaction = cachedRows
                .GroupBy(x => x.FactionId)
                .ToDictionary(x => x.Key, x => x.OrderByDescending(r => r.EvaluatedAtUnixSeconds).First());

            var factionsToEvaluate = ordered
                .Where(faction =>
                    !cachedByFaction.TryGetValue(faction.Id, out var row) ||
                    string.IsNullOrWhiteSpace(row.ValidCoreFireteamsJson))
                .ToList();

            foreach (var faction in factionsToEvaluate)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var validCoreFireteamNames = await EvaluateValidCoreFireteamsForFactionAsync(faction, maxCost, cancellationToken);
                var hasValidCoreFireteams = validCoreFireteamNames.Count > 0;
                var validCoreFireteamsJson = JsonSerializer.Serialize(validCoreFireteamNames);
                await _specOpsProvider.UpsertCCFactionFireteamValidityAsync(
                    faction.Id,
                    cacheFilterKey,
                    hasValidCoreFireteams,
                    validCoreFireteamsJson,
                    cancellationToken);

                cachedByFaction[faction.Id] = new CCFactionFireteamValidityRecord
                {
                    FactionId = faction.Id,
                    FilterKey = cacheFilterKey,
                    HasValidCoreFireteams = hasValidCoreFireteams,
                    ValidCoreFireteamsJson = validCoreFireteamsJson,
                    EvaluatedAtUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                };
            }

            _validCoreFireteamsByFaction.Clear();
            foreach (var row in cachedByFaction.Values)
            {
                var validTeams = ParseValidCoreFireteams(row.ValidCoreFireteamsJson);
                _validCoreFireteamsByFaction[row.FactionId] = validTeams;
            }

            var validFactionIds = cachedByFaction.Values
                .Where(x => x.HasValidCoreFireteams)
                .Select(x => x.FactionId)
                .ToHashSet();

            foreach (var faction in ordered)
            {
                if (validFactionIds.Contains(faction.Id))
                {
                    visibleFactions.Add(faction);
                }
            }

            var items = visibleFactions
                .Select(faction => new ArmyFactionSelectionItem
                {
                    Id = faction.Id,
                    ParentId = faction.ParentId,
                    Name = faction.Name,
                    CachedLogoPath = _factionLogoCacheService?.TryGetCachedLogoPath(faction.Id),
                    PackagedLogoPath = _factionLogoCacheService?.GetPackagedFactionLogoPath(faction.Id)
                        ?? $"SVGCache/factions/{faction.Id}.svg"
                })
                .ToList();

            Factions.Clear();
            foreach (var faction in items)
            {
                Factions.Add(faction);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ArmyFactionSelectionPage LoadFactionsAsync failed: {ex.Message}");
        }
    }

    private static IEnumerable<FactionRecord> CollapseContractedBackUpVariants(IEnumerable<FactionRecord> factions)
    {
        var list = factions.ToList();
        var contracted = list
            .Where(x => IsContractedBackUpName(x.Name))
            .ToList();

        if (contracted.Count <= 1)
        {
            return list;
        }

        var preferred = contracted.FirstOrDefault(x => !LooksAllCaps(x.Name))
            ?? contracted.First();

        return list.Where(x => !IsContractedBackUpName(x.Name) || x.Id == preferred.Id);
    }

    private static bool IsNonAlignedArmyName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        return name.Trim().Equals("Non-Aligned Armies", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsContractedBackUpName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        var normalized = Regex.Replace(name.Trim(), @"[\s\-]+", " ").ToLowerInvariant();
        return normalized == "contracted back up";
    }

    private static bool LooksAllCaps(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var letters = value.Where(char.IsLetter).ToArray();
        if (letters.Length == 0)
        {
            return false;
        }

        return letters.All(char.IsUpper);
    }

    private void SetSelectedFaction(ArmyFactionSelectionItem item)
    {
        if (_factionSelectionState.SelectedFaction == item)
        {
            AssignSelectedFactionToActiveSlot(item);
            return;
        }

        if (_factionSelectionState.SelectedFaction is not null)
        {
            _factionSelectionState.SelectedFaction.IsSelected = false;
        }

        _factionSelectionState.SelectedFaction = item;
        _factionSelectionState.SelectedFaction.IsSelected = true;
        AssignSelectedFactionToActiveSlot(item);
    }

    private void AssignSelectedFactionToActiveSlot(ArmyFactionSelectionItem item)
    {
        if (ShowRightSelectionBox && IsDuplicateSelectionForActiveSlot(item))
        {
            Console.WriteLine($"[ArmyFactionSelectionPage] Duplicate selection blocked for faction {item.Id} ({item.Name}).");
            return;
        }

        var factionChanged = false;
        if (_activeSlotIndex == 0 || !ShowRightSelectionBox)
        {
            factionChanged = _factionSelectionState.LeftSlotFaction?.Id != item.Id;
            _factionSelectionState.LeftSlotFaction = item;
            FactionSlotSelectorView.LeftSlotText = item.Name;
            _ = LoadSlotIconAsync(0, item.CachedLogoPath, item.PackagedLogoPath);
        }
        else
        {
            factionChanged = _factionSelectionState.RightSlotFaction?.Id != item.Id;
            _factionSelectionState.RightSlotFaction = item;
            FactionSlotSelectorView.RightSlotText = item.Name;
            _ = LoadSlotIconAsync(1, item.CachedLogoPath, item.PackagedLogoPath);
        }

        AutoSelectEmptySlot();
        if (factionChanged)
        {
            ResetMercsCompany();
            _ = LoadUnitsForActiveSlotAsync();
        }

        // In CC flow, selecting a faction should immediately advance to unit selection.
        IsFactionSelectionActive = false;
    }

    private string BuildCCFactionValidityFilterKey(int maxCost)
    {
        var filterQuery = _activeUnitFilter.ToQuery();
        var termsKey = string.Join(";",
            filterQuery.Terms
                .OrderBy(term => term.Field)
                .Select(term => $"{term.Field}:{term.MatchMode}:{string.Join(",", term.Values.OrderBy(value => value, StringComparer.OrdinalIgnoreCase))}"));

        return string.Join("|",
            "cc-core-v2",
            $"pts:{maxCost}",
            $"lt:{(LieutenantOnlyUnits ? 1 : 0)}",
            $"terms:{termsKey}",
            $"min:{_activeUnitFilter.MinPoints?.ToString() ?? string.Empty}",
            $"max:{_activeUnitFilter.MaxPoints?.ToString() ?? string.Empty}",
            $"filterlt:{(_activeUnitFilter.LieutenantOnlyUnits ? 1 : 0)}");
    }

    private Task<List<string>> EvaluateValidCoreFireteamsForFactionAsync(
        FactionRecord faction,
        int maxCost,
        CancellationToken cancellationToken)
    {
        var validCoreFireteams = new List<string>();
        var snapshot = GetFactionSnapshotFromProvider(faction.Id, cancellationToken);
        if (snapshot is null || string.IsNullOrWhiteSpace(snapshot.FireteamChartJson))
        {
            return Task.FromResult(validCoreFireteams);
        }

        var skillsLookup = BuildIdNameLookup(snapshot.FiltersJson, "skills");
        var charsLookup = BuildIdNameLookup(snapshot.FiltersJson, "chars");
        var equipLookup = BuildIdNameLookup(snapshot.FiltersJson, "equip");
        var weaponsLookup = BuildIdNameLookup(snapshot.FiltersJson, "weapons");
        var ammoLookup = BuildIdNameLookup(snapshot.FiltersJson, "ammunition");
        var typeLookup = BuildIdNameLookup(snapshot.FiltersJson, "type");
        var categoryLookup = BuildIdNameLookup(snapshot.FiltersJson, "category");

        var units = GetResumeByFactionMercsOnlyFromProvider(faction.Id, cancellationToken);
        var sourceUnits = units.Select(unit => new ArmyUnitSelectionItem
            {
                Id = unit.UnitId,
                SourceFactionId = faction.Id,
                Slug = unit.Slug,
                Name = unit.Name,
                Type = unit.Type,
                IsCharacter = IsCharacterCategory(unit, categoryLookup)
            })
            .ToList();

        var teams = new Dictionary<string, TeamAggregate>(StringComparer.OrdinalIgnoreCase);
        MergeFireteamEntries(snapshot.FireteamChartJson, teams);
        foreach (var team in teams.Values
                     .Where(x => x.Core > 0)
                     .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase))
        {
            var hasLieutenantProfileAfterFilters = false;
            foreach (var unitLimit in team.UnitLimits)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var matchedUnit = ResolveUnitForTeamEntry(unitLimit.Key, unitLimit.Value.Slug, sourceUnits);
                if (matchedUnit is null || matchedUnit.IsCharacter)
                {
                    continue;
                }

                if (!MatchesClassificationFilter(matchedUnit, typeLookup))
                {
                    continue;
                }

                var unitRecord = GetUnitFromProvider(faction.Id, matchedUnit.Id, cancellationToken);
                if (string.IsNullOrWhiteSpace(unitRecord?.ProfileGroupsJson))
                {
                    continue;
                }

                var requiresFtoProfile = IsFtoLabel(unitLimit.Key);
                var hasAnyVisibleProfile = UnitHasVisibleOptionWithFilter(
                    unitRecord.ProfileGroupsJson,
                    skillsLookup,
                    charsLookup,
                    equipLookup,
                    weaponsLookup,
                    ammoLookup,
                    _activeUnitFilter,
                    requireLieutenant: false,
                    requireZeroSwc: true,
                    maxCost: maxCost,
                    requireFto: requiresFtoProfile);
                if (!hasAnyVisibleProfile)
                {
                    continue;
                }

                var hasVisibleLieutenantProfile = UnitHasVisibleOptionWithFilter(
                    unitRecord.ProfileGroupsJson,
                    skillsLookup,
                    charsLookup,
                    equipLookup,
                    weaponsLookup,
                    ammoLookup,
                    _activeUnitFilter,
                    requireLieutenant: true,
                    requireZeroSwc: true,
                    maxCost: maxCost,
                    requireFto: requiresFtoProfile);

                if (!hasVisibleLieutenantProfile)
                {
                    continue;
                }

                hasLieutenantProfileAfterFilters = true;
                break;
            }

            if (hasLieutenantProfileAfterFilters)
            {
                validCoreFireteams.Add(team.Name);
            }
        }

        var normalized = validCoreFireteams
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();
        return Task.FromResult(normalized);
    }

    private static HashSet<string> ParseValidCoreFireteams(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            var names = JsonSerializer.Deserialize<List<string>>(json) ?? [];
            return new HashSet<string>(
                names.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()),
                StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private void ResetMercsCompany()
    {
        if (MercsCompanyEntries.Count == 0)
        {
            UpdateMercsCompanyTotal();
            ReevaluateTrackedFireteamLevel();
            return;
        }

        MercsCompanyEntries.Clear();
        UpdateMercsCompanyTotal();
        ReevaluateTrackedFireteamLevel();
    }

    private void OnTeamTrackingRadioButtonCheckedChanged(object? sender, CheckedChangedEventArgs e)
    {
        if (_isUpdatingTrackedTeamSelection || !e.Value)
        {
            return;
        }

        if (sender is not RadioButton radioButton || radioButton.BindingContext is not ArmyTeamListItem team)
        {
            return;
        }

        if (!team.ShowTrackingRadioButton)
        {
            return;
        }

        SetTrackedFireteamSelection(team.Name);
    }

    private void RestoreTrackedFireteamSelection(string? trackedFireteamName)
    {
        if (string.IsNullOrWhiteSpace(trackedFireteamName))
        {
            SetTrackedFireteamSelection(GetDefaultTrackedFireteamName());
            return;
        }

        var matchingTeam = TeamEntries.FirstOrDefault(x =>
            x.ShowTrackingRadioButton &&
            string.Equals(x.Name, trackedFireteamName, StringComparison.OrdinalIgnoreCase));
        SetTrackedFireteamSelection(matchingTeam?.Name ?? GetDefaultTrackedFireteamName());
    }

    private string GetDefaultTrackedFireteamName()
    {
        var firstVisibleTeam = TeamEntries.FirstOrDefault(x => x.ShowTrackingRadioButton && x.IsVisible);
        if (firstVisibleTeam is not null)
        {
            return firstVisibleTeam.Name;
        }

        var firstTrackableTeam = TeamEntries.FirstOrDefault(x => x.ShowTrackingRadioButton);
        return firstTrackableTeam?.Name ?? string.Empty;
    }

    private void SetTrackedFireteamSelection(string? teamName)
    {
        var normalizedTeamName = teamName?.Trim() ?? string.Empty;
        var hasSelection = !string.IsNullOrWhiteSpace(normalizedTeamName);

        _isUpdatingTrackedTeamSelection = true;
        try
        {
            foreach (var team in TeamEntries)
            {
                team.IsTrackedTeam = team.ShowTrackingRadioButton &&
                                     hasSelection &&
                                     string.Equals(team.Name, normalizedTeamName, StringComparison.OrdinalIgnoreCase);
            }
        }
        finally
        {
            _isUpdatingTrackedTeamSelection = false;
        }

        if (string.Equals(_trackedFireteamName, normalizedTeamName, StringComparison.Ordinal))
        {
            ReevaluateTrackedFireteamLevel();
            return;
        }

        _trackedFireteamName = normalizedTeamName;
        OnPropertyChanged(nameof(TrackedFireteamNameDisplay));
        ReevaluateTrackedFireteamLevel();
    }

    private void ReevaluateTrackedFireteamLevel()
    {
        var evaluatedLevel = EvaluateTrackedFireteamLevel();
        OnTrackedFireteamLevelEvaluated(evaluatedLevel);
    }

    private int EvaluateTrackedFireteamLevel()
    {
        if (string.IsNullOrWhiteSpace(_trackedFireteamName))
        {
            return 0;
        }

        var trackedTeam = TeamEntries.FirstOrDefault(x =>
            x.ShowTrackingRadioButton &&
            string.Equals(x.Name, _trackedFireteamName, StringComparison.OrdinalIgnoreCase));
        if (trackedTeam is null)
        {
            return 0;
        }

        var allowedNames = BuildTrackedTeamAllowedNameSet(trackedTeam);
        if (allowedNames.Count == 0)
        {
            return 0;
        }

        var matchingTrooperCount = 0;
        foreach (var entry in MercsCompanyEntries)
        {
            if (IsTrackedTeamMatch(entry, trackedTeam, allowedNames))
            {
                matchingTrooperCount++;
            }
        }

        return Math.Clamp(matchingTrooperCount, 0, 6);
    }

    private static bool IsTrackedTeamMatch(
        MercsCompanyEntry entry,
        ArmyTeamListItem trackedTeam,
        HashSet<string> allowedNames)
    {
        if (trackedTeam.AllowedProfiles.Any(x =>
                x.ResolvedUnitId.HasValue &&
                x.ResolvedSourceFactionId.HasValue &&
                x.ResolvedUnitId.Value == entry.SourceUnitId &&
                x.ResolvedSourceFactionId.Value == entry.SourceFactionId))
        {
            return true;
        }

        var candidateNames = new HashSet<string>(StringComparer.Ordinal);
        AddAllowedNameCandidate(candidateNames, entry.BaseUnitName);
        AddAllowedNameCandidate(candidateNames, entry.Name);

        return candidateNames.Any(allowedNames.Contains);
    }

    private static HashSet<string> BuildTrackedTeamAllowedNameSet(ArmyTeamListItem trackedTeam)
    {
        var allowedNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var allowedProfile in trackedTeam.AllowedProfiles)
        {
            AddAllowedNameCandidate(allowedNames, allowedProfile.Name);

            foreach (Match match in Regex.Matches(allowedProfile.Name ?? string.Empty, @"\(([^)]*)\)"))
            {
                var groupValue = match.Groups[1].Value;
                foreach (var alias in groupValue.Split([',', '/', ';', '|'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
                {
                    AddAllowedNameCandidate(allowedNames, alias);
                }
            }
        }

        return allowedNames;
    }

    private static void AddAllowedNameCandidate(HashSet<string> target, string? rawCandidate)
    {
        if (string.IsNullOrWhiteSpace(rawCandidate))
        {
            return;
        }

        var withoutParens = Regex.Replace(rawCandidate, @"\([^)]*\)", " ");
        var normalized = NormalizeTeamUnitName(withoutParens);
        if (!string.IsNullOrWhiteSpace(normalized))
        {
            target.Add(normalized);
        }
    }

    // Hook point for full fireteam-level evaluation logic. Provide an evaluated value in [1..6].
    private void OnTrackedFireteamLevelEvaluated(int fireteamLevel)
    {
        SetTrackedFireteamLevel(fireteamLevel);
    }

    private void SetTrackedFireteamLevel(int fireteamLevel)
    {
        var normalizedLevel = Math.Clamp(fireteamLevel, 0, 6);
        if (_trackedFireteamLevel == normalizedLevel)
        {
            return;
        }

        _trackedFireteamLevel = normalizedLevel;
        _ = LoadTrackedFireteamLevelIconAsync(_trackedFireteamLevel);
    }

    private async Task LoadTrackedFireteamLevelIconAsync(int fireteamLevel)
    {
        _trackedFireteamLevelPicture?.Dispose();
        _trackedFireteamLevelPicture = null;

        if (fireteamLevel >= 1 && fireteamLevel <= 6)
        {
            try
            {
                var iconPath = $"SVGCache/NonCBIcons/Fireteam/noun-team-{fireteamLevel}.svg";
                await using var stream = await FileSystem.Current.OpenAppPackageFileAsync(iconPath);
                var svg = new SKSvg();
                _trackedFireteamLevelPicture = svg.Load(stream);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"ArmyFactionSelectionPage tracked fireteam icon load failed: {ex.Message}");
            }
        }

        TrackedFireteamLevelCanvas?.InvalidateSurface();
    }

    private bool IsDuplicateSelectionForActiveSlot(ArmyFactionSelectionItem item)
    {
        if (!ShowRightSelectionBox)
        {
            return false;
        }

        if (_activeSlotIndex == 0)
        {
            return _factionSelectionState.RightSlotFaction is not null
                && _factionSelectionState.RightSlotFaction.Id == item.Id
                && (_factionSelectionState.LeftSlotFaction is null || _factionSelectionState.LeftSlotFaction.Id != item.Id);
        }

        return _factionSelectionState.LeftSlotFaction is not null
            && _factionSelectionState.LeftSlotFaction.Id == item.Id
            && (_factionSelectionState.RightSlotFaction is null || _factionSelectionState.RightSlotFaction.Id != item.Id);
    }

    private void AutoSelectEmptySlot()
    {
        if (!ShowRightSelectionBox)
        {
            SetActiveSlot(0);
            return;
        }

        var leftEmpty = _factionSelectionState.LeftSlotFaction is null;
        var rightEmpty = _factionSelectionState.RightSlotFaction is null;

        if (leftEmpty && !rightEmpty)
        {
            SetActiveSlot(0);
            return;
        }

        if (rightEmpty && !leftEmpty)
        {
            SetActiveSlot(1);
        }
    }

    private void SetActiveSlot(int index)
    {
        _activeSlotIndex = index == 1 && ShowRightSelectionBox ? 1 : 0;
        FactionSlotSelectorView.ApplyActiveSlotBorders(_activeSlotIndex);
    }


    private async Task LoadUnitsForActiveSlotAsync(CancellationToken cancellationToken = default)
    {
        var trackedFireteamNameToRestore = _trackedFireteamName;
        _preparedUnitFilterPopupOptions = null;
        AreTeamEntriesReady = false;
        Units.Clear();
        TeamEntries.Clear();
        _selectedUnit = null;
        ResetUnitDetails();

        var factions = GetUnitSourceFactions();
        if (factions.Count == 0)
        {
            return;
        }

        try
        {
            var mergedUnits = new Dictionary<string, ArmyUnitSelectionItem>(StringComparer.OrdinalIgnoreCase);
            var mergedTeams = new Dictionary<string, TeamAggregate>(StringComparer.OrdinalIgnoreCase);
            var wildcardUnitLimits = new Dictionary<string, (int Min, int Max, string? Slug, bool MinAsterisk)>(StringComparer.OrdinalIgnoreCase);

            foreach (var faction in factions)
            {
                var units = GetResumeByFactionMercsOnlyFromProvider(faction.Id, cancellationToken);
                var resumeByUnitId = units
                    .GroupBy(x => x.UnitId)
                    .ToDictionary(x => x.Key, x => x.First());
                var specopsUnits = await _specOpsProvider.GetSpecopsUnitsByFactionAsync(faction.Id, cancellationToken);
                var specopsByUnitId = specopsUnits
                    .GroupBy(x => x.UnitId)
                    .ToDictionary(x => x.Key, x => x.First());
                var snapshot = GetFactionSnapshotFromProvider(faction.Id, cancellationToken);
                var typeLookup = BuildIdNameLookup(snapshot?.FiltersJson, "type");
                var categoryLookup = BuildIdNameLookup(snapshot?.FiltersJson, "category");
                MergeFireteamEntries(snapshot?.FireteamChartJson, mergedTeams);

                if (_factionLogoCacheService is not null)
                {
                    await _factionLogoCacheService.CacheUnitLogosFromRecordsAsync(faction.Id, units, cancellationToken);
                }

                foreach (var unit in units)
                {
                    var key = unit.Name.Trim();
                    if (string.IsNullOrWhiteSpace(key) || mergedUnits.ContainsKey(key))
                    {
                        continue;
                    }

                    mergedUnits[key] = new ArmyUnitSelectionItem
                    {
                        Id = unit.UnitId,
                        SourceFactionId = faction.Id,
                        Slug = unit.Slug,
                        Name = unit.Name,
                        Type = unit.Type,
                        IsCharacter = IsCharacterCategory(unit, categoryLookup),
                        Subtitle = BuildUnitSubtitle(unit, typeLookup, categoryLookup),
                        IsSpecOps = false,
                        CachedLogoPath = _factionLogoCacheService?.TryGetCachedUnitLogoPath(faction.Id, unit.UnitId),
                        PackagedLogoPath = _factionLogoCacheService?.GetPackagedUnitLogoPath(faction.Id, unit.UnitId)
                            ?? $"SVGCache/units/{faction.Id}-{unit.UnitId}.svg"
                    };
                }

                foreach (var specopsUnit in specopsUnits.OrderBy(x => x.EntryOrder))
                {
                    var baseName = string.IsNullOrWhiteSpace(specopsUnit.Name)
                        ? units.FirstOrDefault(x => x.UnitId == specopsUnit.UnitId)?.Name ?? $"Unit {specopsUnit.UnitId}"
                        : specopsUnit.Name.Trim();
                    var key = $"{baseName} - Spec Ops";
                    if (string.IsNullOrWhiteSpace(key) || mergedUnits.ContainsKey(key))
                    {
                        continue;
                    }

                    mergedUnits[key] = new ArmyUnitSelectionItem
                    {
                        Id = specopsUnit.UnitId,
                        SourceFactionId = faction.Id,
                        Slug = specopsUnit.Slug,
                        Name = key,
                        Type = resumeByUnitId.TryGetValue(specopsUnit.UnitId, out var resumeUnit) ? resumeUnit.Type : null,
                        IsCharacter = resumeByUnitId.TryGetValue(specopsUnit.UnitId, out var characterUnit) &&
                                      IsCharacterCategory(characterUnit, categoryLookup),
                        Subtitle = resumeByUnitId.TryGetValue(specopsUnit.UnitId, out var subtitleUnit)
                            ? BuildUnitSubtitle(subtitleUnit, typeLookup, categoryLookup)
                            : "Spec Ops",
                        IsSpecOps = true,
                        CachedLogoPath = _factionLogoCacheService?.TryGetCachedUnitLogoPath(faction.Id, specopsUnit.UnitId),
                        PackagedLogoPath = _factionLogoCacheService?.GetPackagedUnitLogoPath(faction.Id, specopsUnit.UnitId)
                            ?? $"SVGCache/units/{faction.Id}-{specopsUnit.UnitId}.svg"
                    };
                }
            }

            foreach (var unit in ArmyUnitSort.OrderByUnitTypeAndName(mergedUnits.Values, x => x.Type, x => x.Name))
            {
                Units.Add(unit);
            }

            var validCoreTeamNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var faction in factions)
            {
                if (_validCoreFireteamsByFaction.TryGetValue(faction.Id, out var cachedValidTeamNames))
                {
                    foreach (var teamName in cachedValidTeamNames)
                    {
                        validCoreTeamNames.Add(teamName);
                    }
                }
            }

            foreach (var team in mergedTeams.Values
                         .Where(x => x.Core > 0 &&
                                     (validCoreTeamNames.Count == 0 || validCoreTeamNames.Contains(x.Name)))
                         .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase))
            {
                var nonCharacterUnitLimits = FilterCharacterUnitLimits(team.UnitLimits, mergedUnits.Values);
                var nonCharacterNonWildcardUnitLimits = FilterWildcardUnitLimits(nonCharacterUnitLimits);
                var allowedProfiles = BuildAllowedTeamProfiles(nonCharacterNonWildcardUnitLimits, mergedUnits.Values);
                if (allowedProfiles.Count == 0)
                {
                    continue;
                }

                TeamEntries.Add(new ArmyTeamListItem
                {
                    Name = team.Name,
                    TeamCountsText = $"C: {team.Core}",
                    IsExpanded = true,
                    AllowedProfiles = new ObservableCollection<ArmyTeamUnitLimitItem>(allowedProfiles)
                });
            }

            foreach (var team in mergedTeams.Values)
            {
                var isWildcardTeam = IsWildcardTeamName(team.Name);
                var nonCharacterUnitLimits = FilterCharacterUnitLimits(team.UnitLimits, mergedUnits.Values);
                foreach (var entry in nonCharacterUnitLimits)
                {
                    var unitName = entry.Key;
                    var value = entry.Value;
                    if (!isWildcardTeam && !IsWildcardEntry(unitName, value.Slug))
                    {
                        continue;
                    }

                    if (wildcardUnitLimits.TryGetValue(unitName, out var existing))
                    {
                        wildcardUnitLimits[unitName] = (
                            Math.Min(existing.Min, value.Min),
                            Math.Max(existing.Max, value.Max),
                            string.IsNullOrWhiteSpace(existing.Slug) ? value.Slug : existing.Slug,
                            existing.MinAsterisk || value.MinAsterisk);
                    }
                    else
                    {
                        wildcardUnitLimits[unitName] = value;
                    }
                }
            }

            if (wildcardUnitLimits.Count > 0)
            {
                var wildcardAllowedProfiles = wildcardUnitLimits
                    .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
                    .Select(x => BuildTeamUnitLimitItem(
                        x.Key,
                        x.Value.MinAsterisk ? "*" : x.Value.Min.ToString(),
                        x.Value.Max.ToString(),
                        x.Value.Slug,
                        mergedUnits.Values))
                    .Where(x => !x.IsCharacter)
                    .ToList();

                if (wildcardAllowedProfiles.Count > 0)
                {
                    TeamEntries.Add(new ArmyTeamListItem
                    {
                        Name = "Wildcards",
                        TeamCountsText = string.Empty,
                        IsWildcardBucket = true,
                        IsExpanded = true,
                        AllowedProfiles = new ObservableCollection<ArmyTeamUnitLimitItem>(wildcardAllowedProfiles)
                    });
                }
            }

            await ApplyUnitVisibilityFiltersAsync(cancellationToken);
            await BuildUnitFilterPopupOptionsAsync(cancellationToken);
            RestoreTrackedFireteamSelection(trackedFireteamNameToRestore);
            AreTeamEntriesReady = true;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ArmyFactionSelectionPage LoadUnitsForActiveSlotAsync failed: {ex.Message}");
            RestoreTrackedFireteamSelection(string.Empty);
            AreTeamEntriesReady = false;
        }
    }


    private void SetSelectedUnit(ArmyUnitSelectionItem item, bool restrictProfilesToFto = false)
    {
        Console.WriteLine($"ArmyFactionSelectionPage SetSelectedUnit requested: id={item.Id}, faction={item.SourceFactionId}, name='{item.Name}'.");
        var selectionContextChanged = _restrictSelectedUnitProfilesToFto != restrictProfilesToFto;
        _restrictSelectedUnitProfilesToFto = restrictProfilesToFto;
        if (_selectedUnit == item)
        {
            if (selectionContextChanged)
            {
                Console.WriteLine("ArmyFactionSelectionPage SetSelectedUnit context changed; reloading selected unit details.");
                _ = LoadSelectedUnitDetailsAsync();
            }
            else
            {
                Console.WriteLine("ArmyFactionSelectionPage SetSelectedUnit skipped (same item instance).");
            }
            return;
        }

        if (_selectedUnit is not null)
        {
            _selectedUnit.IsSelected = false;
        }

        _selectedUnit = item;
        _selectedUnit.IsSelected = true;
        Console.WriteLine($"ArmyFactionSelectionPage selected unit now: id={_selectedUnit.Id}, faction={_selectedUnit.SourceFactionId}, name='{_selectedUnit.Name}'.");
        _ = LoadSelectedUnitLogoAsync(item);
        _ = LoadSelectedUnitDetailsAsync();
    }

    private void AddProfileToMercsCompany(ViewerProfileItem? profile)
    {
        if (profile is null || _selectedUnit is null)
        {
            return;
        }

        // Block add for configurations currently marked unavailable (points, lieutenant, AVA, etc).
        if (!profile.IsVisible || profile.IsLieutenantBlocked)
        {
            return;
        }

        var combinedEquipment = MergeCommonAndUnique(UnitDisplayConfigurationsView.SelectedUnitCommonEquipment, profile.UniqueEquipment);
        var combinedSkills = MergeCommonAndUnique(UnitDisplayConfigurationsView.SelectedUnitCommonSkills, profile.UniqueSkills);
        var combinedEquipmentText = JoinOrDash(combinedEquipment);
        var combinedSkillsText = JoinOrDash(combinedSkills);
        var currentUnitMove = FormatMoveValue(UnitMoveFirstCm, UnitMoveSecondCm);
        var statline = $"MOV {UnitMov} | CC {UnitCc} | BS {UnitBs} | PH {UnitPh} | WIP {UnitWip} | ARM {UnitArm} | BTS {UnitBts} | {UnitVitalityHeader} {UnitVitality} | S {UnitS}";
        var peripheralStats = BuildMercsCompanyPeripheralStats(profile);
        var entry = new MercsCompanyEntry
        {
            Name = profile.Name,
            BaseUnitName = _selectedUnit.Name,
            NameFormatted = profile.NameFormatted ?? BuildNameFormatted(profile.Name),
            Subtitle = statline,
            UnitTypeCode = ExtractUnitTypeCode(_selectedUnit.Subtitle),
            CostDisplay = $"C {profile.Cost}",
            CostValue = ParseCostValue(profile.Cost),
            IsLieutenant = profile.IsLieutenant,
            ProfileKey = profile.ProfileKey,
            SourceUnitId = _selectedUnit.Id,
            SourceFactionId = _selectedUnit.SourceFactionId,
            CachedLogoPath = _selectedUnit.CachedLogoPath,
            PackagedLogoPath = _selectedUnit.PackagedLogoPath,
            SavedEquipment = combinedEquipmentText,
            SavedSkills = combinedSkillsText,
            SavedRangedWeapons = profile.RangedWeapons,
            SavedCcWeapons = profile.MeleeWeapons,
            ExperiencePoints = 0,
            EquipmentLineFormatted = BuildMercsCompanyLineFormatted("Equipment", combinedEquipmentText, Color.FromArgb("#06B6D4")),
            HasEquipmentLine = combinedEquipment.Count > 0,
            SkillsLineFormatted = BuildMercsCompanyLineFormatted("Skills", combinedSkillsText, Color.FromArgb("#F59E0B")),
            HasSkillsLine = combinedSkills.Count > 0,
            RangedLineFormatted = BuildMercsCompanyLineFormatted("Ranged Weapons", profile.RangedWeapons, Color.FromArgb("#EF4444")),
            CcLineFormatted = BuildMercsCompanyLineFormatted("CC Weapons", profile.MeleeWeapons, Color.FromArgb("#22C55E")),
            HasPeripheralStatBlock = peripheralStats is not null,
            PeripheralNameHeading = peripheralStats?.NameHeading ?? string.Empty,
            PeripheralMov = peripheralStats is null ? "-" : FormatMoveValue(peripheralStats.MoveFirstCm, peripheralStats.MoveSecondCm),
            PeripheralCc = peripheralStats?.Cc ?? "-",
            PeripheralBs = peripheralStats?.Bs ?? "-",
            PeripheralPh = peripheralStats?.Ph ?? "-",
            PeripheralWip = peripheralStats?.Wip ?? "-",
            PeripheralArm = peripheralStats?.Arm ?? "-",
            PeripheralBts = peripheralStats?.Bts ?? "-",
            PeripheralVitalityHeader = peripheralStats?.VitalityHeader ?? "VITA",
            PeripheralVitality = peripheralStats?.Vitality ?? "-",
            PeripheralS = peripheralStats?.S ?? "-",
            PeripheralAva = peripheralStats?.Ava ?? "-",
            SavedPeripheralEquipment = peripheralStats?.Equipment ?? "-",
            SavedPeripheralSkills = peripheralStats?.Skills ?? "-",
            PeripheralEquipmentLineFormatted = BuildMercsCompanyLineFormatted("Equipment", peripheralStats?.Equipment, Color.FromArgb("#06B6D4")),
            HasPeripheralEquipmentLine = peripheralStats is not null && !string.IsNullOrWhiteSpace(peripheralStats.Equipment) && peripheralStats.Equipment != "-",
            PeripheralSkillsLineFormatted = BuildMercsCompanyLineFormatted("Skills", peripheralStats?.Skills, Color.FromArgb("#F59E0B")),
            HasPeripheralSkillsLine = peripheralStats is not null && !string.IsNullOrWhiteSpace(peripheralStats.Skills) && peripheralStats.Skills != "-",
            UnitMoveFirstCm = UnitMoveFirstCm,
            UnitMoveSecondCm = UnitMoveSecondCm,
            UnitMoveDisplay = currentUnitMove,
            PeripheralMoveFirstCm = peripheralStats?.MoveFirstCm,
            PeripheralMoveSecondCm = peripheralStats?.MoveSecondCm
        };

        if (entry.IsLieutenant)
        {
            MercsCompanyEntries.Insert(0, entry);
        }
        else
        {
            MercsCompanyEntries.Add(entry);
        }

        UpdateMercsCompanyTotal();
        ReevaluateTrackedFireteamLevel();
        ApplyLieutenantVisualStates();
        _ = ApplyUnitVisibilityFiltersAsync();
    }

    private PeripheralMercsCompanyStats? BuildMercsCompanyPeripheralStats(ViewerProfileItem profile)
    {
        var peripheralName = ExtractFirstPeripheralName(profile.Peripherals);
        if (string.IsNullOrWhiteSpace(peripheralName) || string.IsNullOrWhiteSpace(UnitDisplayConfigurationsView.SelectedUnitProfileGroupsJson))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(UnitDisplayConfigurationsView.SelectedUnitProfileGroupsJson);
            if (!TryFindPeripheralStatElement(doc.RootElement, peripheralName, out var peripheralProfile))
            {
                return null;
            }

            return BuildPeripheralStatBlock(peripheralName, peripheralProfile, UnitDisplayConfigurationsView.SelectedUnitFiltersJson);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ArmyFactionSelectionPage BuildMercsCompanyPeripheralStats failed: {ex.Message}");
            return null;
        }
    }

    private void RemoveMercsCompanyEntry(MercsCompanyEntry? entry)
    {
        if (entry is null)
        {
            return;
        }

        MercsCompanyEntries.Remove(entry);
        UpdateMercsCompanyTotal();
        ReevaluateTrackedFireteamLevel();
        ApplyLieutenantVisualStates();
        _ = ApplyUnitVisibilityFiltersAsync();
    }

    private async Task SelectMercsCompanyEntryAsync(MercsCompanyEntry? entry, CancellationToken cancellationToken = default)
    {
        if (entry is null)
        {
            return;
        }

        try
        {
            // Prefer existing unit item (even if hidden), otherwise build a temporary item
            // so details can load regardless of current list visibility filters.
            var unitItem = Units.FirstOrDefault(x =>
                x.Id == entry.SourceUnitId &&
                x.SourceFactionId == entry.SourceFactionId);

            if (unitItem is null)
            {
                var unitRecord = GetUnitFromProvider(entry.SourceFactionId, entry.SourceUnitId, cancellationToken);
                var unitName = !string.IsNullOrWhiteSpace(unitRecord?.Name)
                    ? unitRecord.Name
                    : entry.Name;

                unitItem = new ArmyUnitSelectionItem
                {
                    Id = entry.SourceUnitId,
                    SourceFactionId = entry.SourceFactionId,
                    Name = unitName,
                    CachedLogoPath = entry.CachedLogoPath,
                    PackagedLogoPath = entry.PackagedLogoPath,
                    Subtitle = null,
                    IsVisible = false
                };
            }

            SetSelectedUnit(unitItem);
            // Force-refresh details/configurations even if the selected unit instance
            // did not change (SetSelectedUnit can short-circuit on same instance).
            await LoadSelectedUnitDetailsAsync(cancellationToken);
            IsFactionSelectionActive = false;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ArmyFactionSelectionPage SelectMercsCompanyEntryAsync failed: {ex.Message}");
        }
    }

    private void ApplyLieutenantVisualStates()
    {
        var hasActiveLieutenant = MercsCompanyEntries.Any(x => x.IsLieutenant);
        var pointsLimit = int.TryParse(SelectedStartSeasonPoints, out var parsedLimit) ? parsedLimit : 0;
        var currentPoints = int.TryParse(SeasonPointsCapText, out var parsedPoints) ? parsedPoints : 0;
        var pointsRemaining = pointsLimit - currentPoints;
        var avaLimit = ParseAvaLimit(UnitAva);
        var selectedUnitCountInCompany = _selectedUnit is null
            ? 0
            : MercsCompanyEntries.Count(x =>
                x.SourceUnitId == _selectedUnit.Id &&
                x.SourceFactionId == _selectedUnit.SourceFactionId);
        var avaReached = avaLimit.HasValue && selectedUnitCountInCompany >= avaLimit.Value;
        var visibleProfiles = 0;

        foreach (var profile in Profiles)
        {
            var profileCost = ParseCostValue(profile.Cost);
            var overRemainingPoints = profileCost > pointsRemaining;
            var belowMinFilterPoints = _activeUnitFilter.MinPoints.HasValue && profileCost < _activeUnitFilter.MinPoints.Value;
            var aboveMaxFilterPoints = _activeUnitFilter.MaxPoints.HasValue && profileCost > _activeUnitFilter.MaxPoints.Value;
            var lieutenantFilteredOut = LieutenantOnlyUnits && !profile.IsLieutenant;

            profile.IsVisible = !lieutenantFilteredOut &&
                                !overRemainingPoints &&
                                !belowMinFilterPoints &&
                                !aboveMaxFilterPoints;
            profile.IsLieutenantBlocked =
                (hasActiveLieutenant && profile.IsLieutenant) ||
                overRemainingPoints ||
                avaReached;

            if (profile.IsVisible)
            {
                visibleProfiles++;
            }
        }

        ProfilesStatus = visibleProfiles == 0
            ? "No configurations found for this unit."
            : $"{visibleProfiles} configurations loaded.";

        UpdatePeripheralStatBlockFromVisibleProfiles();
        UpdateSeasonValidationState();
    }

    private static int? ParseAvaLimit(string? ava)
    {
        return CompanySelectionSharedUtilities.ParseAvaLimit(ava);
    }

    private async Task ApplyUnitVisibilityFiltersAsync(CancellationToken cancellationToken = default)
    {
        AreTeamEntriesReady = false;
        if (Units.Count == 0)
        {
            return;
        }

        try
        {
            var pointsLimit = int.TryParse(SelectedStartSeasonPoints, out var parsedLimit) ? parsedLimit : 0;
            var currentPoints = int.TryParse(SeasonPointsCapText, out var parsedPoints) ? parsedPoints : 0;
            var pointsRemaining = pointsLimit - currentPoints;

            var factions = GetUnitSourceFactions();
            var skillsLookupByFaction = new Dictionary<int, IReadOnlyDictionary<int, string>>();
            var typeLookupByFaction = new Dictionary<int, IReadOnlyDictionary<int, string>>();
            var charsLookupByFaction = new Dictionary<int, IReadOnlyDictionary<int, string>>();
            var equipLookupByFaction = new Dictionary<int, IReadOnlyDictionary<int, string>>();
            var weaponsLookupByFaction = new Dictionary<int, IReadOnlyDictionary<int, string>>();
            var ammoLookupByFaction = new Dictionary<int, IReadOnlyDictionary<int, string>>();
            var specopsByFaction = new Dictionary<int, Dictionary<int, ArmySpecopsUnitRecord>>();
            foreach (var faction in factions)
            {
                var snapshot = GetFactionSnapshotFromProvider(faction.Id, cancellationToken);
                skillsLookupByFaction[faction.Id] = BuildIdNameLookup(snapshot?.FiltersJson, "skills");
                typeLookupByFaction[faction.Id] = BuildIdNameLookup(snapshot?.FiltersJson, "type");
                charsLookupByFaction[faction.Id] = BuildIdNameLookup(snapshot?.FiltersJson, "chars");
                equipLookupByFaction[faction.Id] = BuildIdNameLookup(snapshot?.FiltersJson, "equip");
                weaponsLookupByFaction[faction.Id] = BuildIdNameLookup(snapshot?.FiltersJson, "weapons");
                ammoLookupByFaction[faction.Id] = BuildIdNameLookup(snapshot?.FiltersJson, "ammunition");
                var specopsUnits = await _specOpsProvider.GetSpecopsUnitsByFactionAsync(faction.Id, cancellationToken);
                specopsByFaction[faction.Id] = specopsUnits
                    .GroupBy(x => x.UnitId)
                    .ToDictionary(x => x.Key, x => x.First());
            }

            foreach (var unit in Units)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!skillsLookupByFaction.TryGetValue(unit.SourceFactionId, out var skillsLookup))
                {
                    unit.IsVisible = false;
                    continue;
                }
                typeLookupByFaction.TryGetValue(unit.SourceFactionId, out var typeLookup);
                charsLookupByFaction.TryGetValue(unit.SourceFactionId, out var charsLookup);
                equipLookupByFaction.TryGetValue(unit.SourceFactionId, out var equipLookup);
                weaponsLookupByFaction.TryGetValue(unit.SourceFactionId, out var weaponsLookup);
                ammoLookupByFaction.TryGetValue(unit.SourceFactionId, out var ammoLookup);

                if (!MatchesClassificationFilter(unit, typeLookup ?? new Dictionary<int, string>()))
                {
                    unit.IsVisible = false;
                    continue;
                }

                var unitRecord = GetUnitFromProvider(unit.SourceFactionId, unit.Id, cancellationToken);
                var profileGroupsJson = unitRecord?.ProfileGroupsJson;
                if (specopsByFaction.TryGetValue(unit.SourceFactionId, out var specopsUnitsById) &&
                    specopsUnitsById.TryGetValue(unit.Id, out var specopsUnit))
                {
                    if (unit.IsSpecOps || string.IsNullOrWhiteSpace(profileGroupsJson))
                    {
                        profileGroupsJson = specopsUnit.ProfileGroupsJson;
                    }
                }

                unit.IsVisible = UnitHasVisibleOptionWithFilter(
                    profileGroupsJson,
                    skillsLookup,
                    charsLookup ?? new Dictionary<int, string>(),
                    equipLookup ?? new Dictionary<int, string>(),
                    weaponsLookup ?? new Dictionary<int, string>(),
                    ammoLookup ?? new Dictionary<int, string>(),
                    _activeUnitFilter,
                    requireLieutenant: LieutenantOnlyUnits && !unit.IsSpecOps,
                    requireZeroSwc: true,
                    maxCost: pointsRemaining);
            }

            if (_selectedUnit is not null && !_selectedUnit.IsVisible)
            {
                _selectedUnit.IsSelected = false;
                _selectedUnit = null;
                ResetUnitDetails();
            }
            else if (_selectedUnit is not null)
            {
                // Recompute configuration visibility for the selected unit after filter changes.
                ApplyLieutenantVisualStates();
            }

            RefreshTeamEntryVisibility();
            AreTeamEntriesReady = true;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ArmyFactionSelectionPage ApplyUnitVisibilityFiltersAsync failed: {ex.Message}");
            AreTeamEntriesReady = false;
        }
    }

    private void RefreshTeamEntryVisibility()
    {
        if (TeamEntries.Count == 0)
        {
            return;
        }

        var visibleUnitNames = new HashSet<string>(
            Units.Where(x => x.IsVisible).Select(x => x.Name),
            StringComparer.OrdinalIgnoreCase);
        var visibleUnitSlugs = new HashSet<string>(
            Units.Where(x => x.IsVisible)
                .Select(x => x.Slug?.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))!
                .Select(x => x!),
            StringComparer.OrdinalIgnoreCase);

        var visibleNormalizedNames = Units
            .Where(x => x.IsVisible)
            .Select(x => NormalizeTeamUnitName(x.Name))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        foreach (var team in TeamEntries)
        {
            var visibleAllowedCount = 0;
            foreach (var allowed in team.AllowedProfiles)
            {
                if (allowed.IsCharacter)
                {
                    allowed.IsVisible = false;
                    continue;
                }

                var isVisible = IsVisibleTeamAllowedProfile(
                    allowed.Name,
                    allowed.Slug,
                    visibleUnitNames,
                    visibleUnitSlugs,
                    visibleNormalizedNames,
                    treatWildcardAsAlwaysVisible: !team.IsWildcardBucket,
                    visibleUnits: Units.Where(x => x.IsVisible).ToList(),
                    resolvedUnitId: allowed.ResolvedUnitId,
                    resolvedSourceFactionId: allowed.ResolvedSourceFactionId);
                allowed.IsVisible = isVisible;
                if (isVisible)
                {
                    visibleAllowedCount++;
                }
            }

            team.IsVisible = visibleAllowedCount > 0;
            team.IsExpanded = true;
        }
    }

    private static bool IsVisibleTeamAllowedProfile(
        string? allowedProfileName,
        string? allowedProfileSlug,
        HashSet<string> visibleUnitNames,
        HashSet<string> visibleUnitSlugs,
        IReadOnlyList<string> visibleNormalizedNames,
        bool treatWildcardAsAlwaysVisible = true,
        IReadOnlyList<ArmyUnitSelectionItem>? visibleUnits = null,
        int? resolvedUnitId = null,
        int? resolvedSourceFactionId = null)
    {
        if (treatWildcardAsAlwaysVisible && IsWildcardEntry(allowedProfileName, allowedProfileSlug))
        {
            return true;
        }

        // For wildcard bucket rows, only show entries that resolve to an actual visible unit.
        // This prevents false positives from comment text like "(Orc Troops, Bolts)".
        if (!treatWildcardAsAlwaysVisible)
        {
            if (!resolvedUnitId.HasValue || !resolvedSourceFactionId.HasValue || visibleUnits is null)
            {
                return false;
            }

            return visibleUnits.Any(x =>
                x.Id == resolvedUnitId.Value &&
                x.SourceFactionId == resolvedSourceFactionId.Value);
        }

        if (resolvedUnitId.HasValue && resolvedSourceFactionId.HasValue && visibleUnits is not null)
        {
            return visibleUnits.Any(x =>
                x.Id == resolvedUnitId.Value &&
                x.SourceFactionId == resolvedSourceFactionId.Value);
        }

        if (!string.IsNullOrWhiteSpace(allowedProfileSlug) &&
            (visibleUnitSlugs.Contains(allowedProfileSlug.Trim()) ||
             ContainsEquivalentSlug(visibleUnitSlugs, allowedProfileSlug)))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(allowedProfileName))
        {
            return false;
        }

        if (visibleUnitNames.Contains(allowedProfileName))
        {
            return true;
        }

        var normalizedAllowed = NormalizeTeamUnitName(allowedProfileName);
        if (string.IsNullOrWhiteSpace(normalizedAllowed))
        {
            return false;
        }

        foreach (var normalizedVisible in visibleNormalizedNames)
        {
            if (string.Equals(normalizedAllowed, normalizedVisible, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static Dictionary<string, (int Min, int Max, string? Slug, bool MinAsterisk)> FilterCharacterUnitLimits(
        Dictionary<string, (int Min, int Max, string? Slug, bool MinAsterisk)> unitLimits,
        IEnumerable<ArmyUnitSelectionItem> sourceUnits)
    {
        var filtered = new Dictionary<string, (int Min, int Max, string? Slug, bool MinAsterisk)>(StringComparer.OrdinalIgnoreCase);

        foreach (var unitLimit in unitLimits)
        {
            var matchedUnit = ResolveUnitForTeamEntry(unitLimit.Key, unitLimit.Value.Slug, sourceUnits);
            if (matchedUnit?.IsCharacter == true)
            {
                continue;
            }

            filtered[unitLimit.Key] = unitLimit.Value;
        }

        return filtered;
    }

    private static Dictionary<string, (int Min, int Max, string? Slug, bool MinAsterisk)> FilterWildcardUnitLimits(
        Dictionary<string, (int Min, int Max, string? Slug, bool MinAsterisk)> unitLimits)
    {
        var filtered = new Dictionary<string, (int Min, int Max, string? Slug, bool MinAsterisk)>(StringComparer.OrdinalIgnoreCase);

        foreach (var unitLimit in unitLimits)
        {
            if (IsWildcardEntry(unitLimit.Key, unitLimit.Value.Slug))
            {
                continue;
            }

            filtered[unitLimit.Key] = unitLimit.Value;
        }

        return filtered;
    }

    private static List<ArmyTeamUnitLimitItem> BuildAllowedTeamProfiles(
        Dictionary<string, (int Min, int Max, string? Slug, bool MinAsterisk)> unitLimits,
        IEnumerable<ArmyUnitSelectionItem> sourceUnits)
    {
        var merged = new Dictionary<string, (string Name, int Min, int Max, string? Slug, bool MinAsterisk, bool IsWildcard)>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in unitLimits)
        {
            var name = entry.Key;
            var value = entry.Value;
            var isWildcard = IsWildcardEntry(name, value.Slug);
            var key = isWildcard ? "__WILDCARD__" : name.Trim();
            var normalizedName = isWildcard ? "Wildcards" : name;

            if (merged.TryGetValue(key, out var existing))
            {
                merged[key] = (
                    existing.Name,
                    Math.Min(existing.Min, value.Min),
                    Math.Max(existing.Max, value.Max),
                    string.IsNullOrWhiteSpace(existing.Slug) ? value.Slug : existing.Slug,
                    existing.MinAsterisk || value.MinAsterisk,
                    true == existing.IsWildcard || isWildcard);
                continue;
            }

            merged[key] = (
                normalizedName,
                value.Min,
                value.Max,
                value.Slug,
                value.MinAsterisk,
                isWildcard);
        }

        return merged.Values
            .OrderBy(x => x.IsWildcard ? 1 : 0)
            .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .Select(x => BuildTeamUnitLimitItem(
                x.Name,
                x.MinAsterisk ? "*" : x.Min.ToString(),
                x.Max.ToString(),
                x.Slug,
                sourceUnits))
            .ToList();
    }

    private static ArmyTeamUnitLimitItem BuildTeamUnitLimitItem(
        string displayName,
        string min,
        string max,
        string? slug,
        IEnumerable<ArmyUnitSelectionItem> sourceUnits)
    {
        var matched = ResolveUnitForTeamEntry(displayName, slug, sourceUnits);
        return new ArmyTeamUnitLimitItem
        {
            Name = displayName,
            Min = min,
            Max = max,
            Slug = slug,
            IsCharacter = matched?.IsCharacter ?? false,
            CachedLogoPath = matched?.CachedLogoPath,
            PackagedLogoPath = matched?.PackagedLogoPath,
            Subtitle = matched?.Subtitle,
            ResolvedUnitId = matched?.Id,
            ResolvedSourceFactionId = matched?.SourceFactionId
        };
    }

    private static ArmyUnitSelectionItem? ResolveUnitForTeamEntry(
        string? allowedProfileName,
        string? allowedProfileSlug,
        IEnumerable<ArmyUnitSelectionItem> sourceUnits)
    {
        var reinforcementOnly = IsReinforcementTeamEntry(allowedProfileName);
        var preferredSourceUnits = reinforcementOnly
            ? sourceUnits.Where(IsReinforcementUnit).ToList()
            : sourceUnits.ToList();
        var fallbackSourceUnits = reinforcementOnly
            ? sourceUnits.Where(x => !IsReinforcementUnit(x)).ToList()
            : [];

        if (!string.IsNullOrWhiteSpace(allowedProfileSlug))
        {
            var slugMatch = preferredSourceUnits.FirstOrDefault(x =>
                !string.IsNullOrWhiteSpace(x.Slug) &&
                string.Equals(x.Slug.Trim(), allowedProfileSlug.Trim(), StringComparison.Ordinal));
            if (slugMatch is not null)
            {
                return slugMatch;
            }

            // Rule 3 fallback: when first slug lookup misses, retry with case-insensitive compare.
            slugMatch = preferredSourceUnits.FirstOrDefault(x =>
                !string.IsNullOrWhiteSpace(x.Slug) &&
                string.Equals(x.Slug.Trim(), allowedProfileSlug.Trim(), StringComparison.OrdinalIgnoreCase));
            if (slugMatch is not null)
            {
                return slugMatch;
            }

            // Final fallback for inconsistent reinforcement slug prefixes in data (e.g. arjuna-unit vs reinf-arjuna-unit).
            var normalizedAllowedSlug = NormalizeSlugForLookup(allowedProfileSlug);
            if (!string.IsNullOrWhiteSpace(normalizedAllowedSlug))
            {
                slugMatch = preferredSourceUnits.FirstOrDefault(x =>
                    !string.IsNullOrWhiteSpace(x.Slug) &&
                    string.Equals(NormalizeSlugForLookup(x.Slug), normalizedAllowedSlug, StringComparison.Ordinal));
                if (slugMatch is not null)
                {
                    return slugMatch;
                }
            }

            if (!string.IsNullOrWhiteSpace(allowedProfileName))
            {
                var slugFallbackNameMatch = preferredSourceUnits.FirstOrDefault(x =>
                    string.Equals(x.Name, allowedProfileName, StringComparison.OrdinalIgnoreCase));
                if (slugFallbackNameMatch is not null)
                {
                    return slugFallbackNameMatch;
                }
            }

            // Final fallback when no reinforcement record exists for the entry.
            if (reinforcementOnly)
            {
                slugMatch = fallbackSourceUnits.FirstOrDefault(x =>
                    !string.IsNullOrWhiteSpace(x.Slug) &&
                    string.Equals(x.Slug.Trim(), allowedProfileSlug.Trim(), StringComparison.OrdinalIgnoreCase));
                if (slugMatch is not null)
                {
                    return slugMatch;
                }
            }

            return null;
        }

        if (string.IsNullOrWhiteSpace(allowedProfileName))
        {
            return null;
        }

        var exactNameMatch = preferredSourceUnits.FirstOrDefault(x =>
            string.Equals(x.Name, allowedProfileName, StringComparison.OrdinalIgnoreCase));
        if (exactNameMatch is not null)
        {
            return exactNameMatch;
        }

        var normalizedAllowed = NormalizeTeamUnitName(allowedProfileName);
        if (string.IsNullOrWhiteSpace(normalizedAllowed))
        {
            return null;
        }

        foreach (var unit in preferredSourceUnits)
        {
            var normalizedUnit = NormalizeTeamUnitName(unit.Name);
            if (string.IsNullOrWhiteSpace(normalizedUnit))
            {
                continue;
            }

            if (string.Equals(normalizedAllowed, normalizedUnit, StringComparison.Ordinal))
            {
                return unit;
            }
        }

        if (reinforcementOnly)
        {
            var fallbackNameMatch = fallbackSourceUnits.FirstOrDefault(x =>
                string.Equals(x.Name, allowedProfileName, StringComparison.OrdinalIgnoreCase));
            if (fallbackNameMatch is not null)
            {
                return fallbackNameMatch;
            }
        }

        return null;
    }

    private static bool IsWildcardEntry(string? name, string? slug)
    {
        return ContainsWildcardToken(name) || ContainsWildcardToken(slug);
    }

    private static bool ContainsWildcardToken(string? value)
    {
        return !string.IsNullOrWhiteSpace(value) &&
               (value.IndexOf("wildcard", StringComparison.OrdinalIgnoreCase) >= 0 ||
                value.IndexOf("wild", StringComparison.OrdinalIgnoreCase) >= 0);
    }

    private static bool IsWildcardTeamName(string? teamName)
    {
        return ContainsWildcardToken(teamName);
    }

    private static bool ContainsEquivalentSlug(HashSet<string> visibleUnitSlugs, string allowedProfileSlug)
    {
        var normalizedAllowed = NormalizeSlugForLookup(allowedProfileSlug);
        if (string.IsNullOrWhiteSpace(normalizedAllowed))
        {
            return false;
        }

        foreach (var visibleSlug in visibleUnitSlugs)
        {
            if (string.Equals(NormalizeSlugForLookup(visibleSlug), normalizedAllowed, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static string NormalizeTeamUnitName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return string.Empty;
        }

        var formD = name.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(formD.Length);
        foreach (var c in formD)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(c))
            {
                builder.Append(char.ToUpperInvariant(c));
            }
        }

        return builder.ToString();
    }

    private static string NormalizeSlugForLookup(string? slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return string.Empty;
        }

        var lowered = slug.Trim().ToLowerInvariant().Replace('_', '-');
        var builder = new StringBuilder(lowered.Length);
        var previousWasDash = false;
        foreach (var c in lowered)
        {
            if (char.IsLetterOrDigit(c))
            {
                builder.Append(c);
                previousWasDash = false;
            }
            else if (!previousWasDash)
            {
                builder.Append('-');
                previousWasDash = true;
            }
        }

        var normalized = builder.ToString().Trim('-');
        if (normalized.StartsWith("reinf-", StringComparison.Ordinal))
        {
            normalized = normalized["reinf-".Length..];
        }

        if (normalized.EndsWith("-reinf", StringComparison.Ordinal))
        {
            normalized = normalized[..^"-reinf".Length];
        }

        return normalized;
    }

    private static bool IsReinforcementTeamEntry(string? allowedProfileName)
    {
        return !string.IsNullOrWhiteSpace(allowedProfileName) &&
               allowedProfileName.IndexOf("REINF", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool IsFtoLabel(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return Regex.IsMatch(value, @"\bFTO\b", RegexOptions.IgnoreCase);
    }

    private static bool IsReinforcementUnit(ArmyUnitSelectionItem unit)
    {
        return (!string.IsNullOrWhiteSpace(unit.Name) &&
                unit.Name.IndexOf("REINF", StringComparison.OrdinalIgnoreCase) >= 0) ||
               (!string.IsNullOrWhiteSpace(unit.Slug) &&
                unit.Slug.IndexOf("reinf", StringComparison.OrdinalIgnoreCase) >= 0);
    }

    private bool IsCompanyNameValid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return !string.Equals(value.Trim(), "Company Name", StringComparison.OrdinalIgnoreCase);
    }

    private void SetCompanyNameValidationError(bool showError)
    {
        ShowCompanyNameValidationError = showError;
        CompanyNameBorderColor = showError ? Color.FromArgb("#EF4444") : Color.FromArgb("#6B7280");
    }

    protected override async Task StartCompanyAsync()
    {
        if (!IsCompanyNameValid(CompanyName))
        {
            SetCompanyNameValidationError(true);
            return;
        }

        SetCompanyNameValidationError(false);

        try
        {
            var captainEntry = MercsCompanyEntries.FirstOrDefault(x => x.IsLieutenant) ?? MercsCompanyEntries.FirstOrDefault();
            if (captainEntry is null)
            {
                await DisplayAlert("Save Failed", "Add at least one unit before starting a company.", "OK");
                return;
            }

            var improvedCaptainStats = await CompanyCaptainWorkflowService.ShowCaptainConfigurationAsync<SavedImprovedCaptainStats>(
                new CompanyCaptainWorkflowRequest
                {
                    Navigation = Navigation,
                    FallbackSourceFactionId = captainEntry.SourceFactionId,
                    FirstSourceFactionId = GetUnitSourceFactions().FirstOrDefault()?.Id,
                    UnitName = captainEntry.Name,
                    UnitCost = captainEntry.CostValue,
                    UnitStatline = captainEntry.Subtitle ?? "-",
                    UnitRangedWeapons = captainEntry.SavedRangedWeapons,
                    UnitCcWeapons = captainEntry.SavedCcWeapons,
                    UnitSkills = captainEntry.SavedSkills,
                    UnitEquipment = captainEntry.SavedEquipment,
                    UnitCachedLogoPath = captainEntry.CachedLogoPath,
                    UnitPackagedLogoPath = captainEntry.PackagedLogoPath,
                    TryGetParentFactionId = factionId => Factions.FirstOrDefault(x => x.Id == factionId)?.ParentId,
                    TryGetFactionName = factionId => Factions.FirstOrDefault(x => x.Id == factionId)?.Name,
                    TryGetMetadataFactionName = factionId => _armyDataService.GetMetadataFactionById(factionId)?.Name,
                    ArmyDataService = _armyDataService,
                    SpecOpsProvider = _specOpsProvider,
                    ShowUnitsInInches = ShowUnitsInInches
                });
            if (improvedCaptainStats is null)
            {
                return;
            }

            var now = DateTimeOffset.UtcNow;
            var companyName = CompanyName.Trim();
            var companyType = GetCompanyTypeLabel();
            var safeFileName = Regex.Replace(companyName.ToLowerInvariant(), @"[^a-z0-9]+", "-").Trim('-');
            if (string.IsNullOrWhiteSpace(safeFileName))
            {
                safeFileName = "company";
            }

            var saveDir = Path.Combine(FileSystem.Current.AppDataDirectory, "MercenaryRecords");
            Directory.CreateDirectory(saveDir);
            var companyIndex = GetNextCompanyIndex(saveDir, companyName, safeFileName);
            var fileName = $"{safeFileName}-{companyIndex:D4}.json";

            var payload = new SavedCompanyFile
            {
                CompanyName = companyName,
                CompanyType = companyType,
                CompanyIdentifier = ComputeCompanyIdentifier(fileName),
                CompanyIndex = companyIndex,
                CreatedUtc = now.ToString("O", CultureInfo.InvariantCulture),
                PointsLimit = int.TryParse(SelectedStartSeasonPoints, out var pointsLimit) ? pointsLimit : 0,
                CurrentPoints = int.TryParse(SeasonPointsCapText, out var currentPoints) ? currentPoints : 0,
                ImprovedCaptainStats = improvedCaptainStats,
                SourceFactions = GetUnitSourceFactions()
                    .Select(faction => new SavedCompanyFaction
                    {
                        FactionId = faction.Id,
                        FactionName = faction.Name
                    })
                    .ToList(),
                Entries = MercsCompanyEntries.Select((entry, entryIndex) => new SavedCompanyEntry
                {
                    EntryIndex = entryIndex,
                    Name = entry.Name,
                    BaseUnitName = entry.Name,
                    CustomName = entry.IsLieutenant
                        ? (string.IsNullOrWhiteSpace(improvedCaptainStats.CaptainName) ? "Captain" : improvedCaptainStats.CaptainName.Trim())
                        : "Trooper",
                    UnitTypeCode = string.IsNullOrWhiteSpace(entry.UnitTypeCode)
                        ? string.Empty
                        : entry.UnitTypeCode.Trim().ToUpperInvariant(),
                    ProfileKey = entry.ProfileKey,
                    SourceFactionId = entry.SourceFactionId,
                    SourceUnitId = entry.SourceUnitId,
                    Cost = entry.CostValue,
                    IsLieutenant = entry.IsLieutenant,
                    SavedEquipment = entry.SavedEquipment,
                    SavedSkills = entry.SavedSkills,
                    SavedRangedWeapons = entry.SavedRangedWeapons,
                    SavedCcWeapons = entry.SavedCcWeapons,
                    HasPeripheralStatBlock = entry.HasPeripheralStatBlock,
                    PeripheralNameHeading = entry.PeripheralNameHeading,
                    PeripheralMov = entry.PeripheralMov,
                    PeripheralCc = entry.PeripheralCc,
                    PeripheralBs = entry.PeripheralBs,
                    PeripheralPh = entry.PeripheralPh,
                    PeripheralWip = entry.PeripheralWip,
                    PeripheralArm = entry.PeripheralArm,
                    PeripheralBts = entry.PeripheralBts,
                    PeripheralVitalityHeader = entry.PeripheralVitalityHeader,
                    PeripheralVitality = entry.PeripheralVitality,
                    PeripheralS = entry.PeripheralS,
                    PeripheralAva = entry.PeripheralAva,
                    SavedPeripheralEquipment = entry.SavedPeripheralEquipment,
                    SavedPeripheralSkills = entry.SavedPeripheralSkills,
                    ExperiencePoints = Math.Max(0, entry.ExperiencePoints)
                }).ToList()
            };

            var filePath = Path.Combine(saveDir, fileName);
            await File.WriteAllTextAsync(
                filePath,
                JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));

            var encodedPath = Uri.EscapeDataString(filePath);
            await Shell.Current.GoToAsync($"//{nameof(CompanyViewerPage)}?companyFilePath={encodedPath}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ArmyFactionSelectionPage StartCompanyAsync failed: {ex}");
            await DisplayAlert("Save Failed", ex.Message, "OK");
        }
    }

    private static string ExtractUnitTypeCode(string? subtitle)
    {
        if (string.IsNullOrWhiteSpace(subtitle))
        {
            return string.Empty;
        }

        var firstToken = subtitle
            .Split([' ', '-', '\u2013', '\u2014'], StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault();
        return string.IsNullOrWhiteSpace(firstToken) ? string.Empty : firstToken.Trim().ToUpperInvariant();
    }

    private void UpdateMercsCompanyTotal()
    {
        var totalCost = MercsCompanyEntries.Sum(x => x.CostValue);
        SeasonPointsCapText = totalCost.ToString();
    }

    private void RefreshMercsCompanyEntryDistanceDisplays()
    {
        foreach (var entry in MercsCompanyEntries)
        {
            var moveDisplay = FormatMoveValue(entry.UnitMoveFirstCm, entry.UnitMoveSecondCm);
            entry.UnitMoveDisplay = moveDisplay;
            entry.Subtitle = ReplaceSubtitleMoveDisplay(entry.Subtitle, moveDisplay);

            if (entry.HasPeripheralStatBlock)
            {
                entry.PeripheralMov = FormatMoveValue(entry.PeripheralMoveFirstCm, entry.PeripheralMoveSecondCm);
            }
        }
    }

    private static string ComputeCompanyIdentifier(string fileName)
    {
        return CompanySelectionSharedUtilities.ComputeCompanyIdentifier(fileName);
    }

    private static int GetNextCompanyIndex(string saveDir, string companyName, string safeFileName)
    {
        return CompanySelectionSharedUtilities.GetNextCompanyIndex(saveDir, companyName, safeFileName);
    }

    private string GetCompanyTypeLabel()
    {
        return "Cohesive Company";
    }

    private void UpdateSeasonValidationState()
    {
        var hasLieutenant = MercsCompanyEntries.Any(x => x.IsLieutenant);
        var pointsLimit = int.TryParse(SelectedStartSeasonPoints, out var parsedLimit) ? parsedLimit : 0;
        var currentPoints = int.TryParse(SeasonPointsCapText, out var parsedPoints) ? parsedPoints : 0;
        var shouldShowCheck = hasLieutenant && currentPoints <= pointsLimit;
        IsCompanyValid = shouldShowCheck;
    }

    private static int ParseCostValue(string? cost)
    {
        return CompanySelectionSharedUtilities.ParseCostValue(cost);
    }

    private async Task LoadSelectedUnitDetailsAsync(CancellationToken cancellationToken = default)
    {
        ResetUnitDetails(clearLogo: false, resetHeaderColors: false);
        if (_selectedUnit is null)
        {
            Console.Error.WriteLine("ArmyFactionSelectionPage LoadSelectedUnitDetailsAsync aborted: selected unit or accessor missing.");
            return;
        }

        try
        {
            Console.WriteLine($"ArmyFactionSelectionPage LoadSelectedUnitDetailsAsync started: id={_selectedUnit.Id}, faction={_selectedUnit.SourceFactionId}, name='{_selectedUnit.Name}'.");
            UnitNameHeading = _selectedUnit.Name;
            var unit = GetUnitFromProvider(_selectedUnit.SourceFactionId, _selectedUnit.Id, cancellationToken);
            ArmySpecopsUnitRecord? specopsUnit = null;
            if (_selectedUnit.IsSpecOps || unit is null)
            {
                var specopsUnits = await _specOpsProvider.GetSpecopsUnitsByFactionAsync(_selectedUnit.SourceFactionId, cancellationToken);
                specopsUnit = specopsUnits.FirstOrDefault(x => x.UnitId == _selectedUnit.Id);
            }
            var treatAsSpecOps = _selectedUnit.IsSpecOps || (unit is null && specopsUnit is not null);
            await ApplyUnitHeaderColorsAsync(_selectedUnit.SourceFactionId, unit, cancellationToken);

            var profileGroupsJson = unit?.ProfileGroupsJson;
            if (treatAsSpecOps && !string.IsNullOrWhiteSpace(specopsUnit?.ProfileGroupsJson))
            {
                profileGroupsJson = specopsUnit.ProfileGroupsJson;
            }
            else if (string.IsNullOrWhiteSpace(profileGroupsJson))
            {
                profileGroupsJson = specopsUnit?.ProfileGroupsJson;
            }

            var snapshot = GetFactionSnapshotFromProvider(_selectedUnit.SourceFactionId, cancellationToken);
            if (string.IsNullOrWhiteSpace(profileGroupsJson))
            {
                Console.Error.WriteLine($"ArmyFactionSelectionPage: profile groups not found for faction={_selectedUnit.SourceFactionId}, unit={_selectedUnit.Id}.");
                return;
            }

            var equipLookup = BuildIdNameLookup(snapshot?.FiltersJson, "equip");
            var skillsLookup = BuildIdNameLookup(snapshot?.FiltersJson, "skills");
            var charsLookup = BuildIdNameLookup(snapshot?.FiltersJson, "chars");
            var displayNameContext = CompanyUnitDetailDisplayNameContext.Create(snapshot?.FiltersJson, ShowUnitsInInches, TryParseId);
            UnitDisplayConfigurationsView.SelectedUnitProfileGroupsJson = profileGroupsJson;
            UnitDisplayConfigurationsView.SelectedUnitFiltersJson = snapshot?.FiltersJson;
            await ApplyGlobalDisplayUnitsPreferenceAsync(cancellationToken);
            if (!string.IsNullOrWhiteSpace(profileGroupsJson))
            {
                using var doc = JsonDocument.Parse(profileGroupsJson);
                var options = EnumerateOptions(doc.RootElement).ToList();
                var visibleOptions = options
                    .Where(option => !IsPositiveSwc(ReadOptionSwc(option)))
                    .Where(option => !treatAsSpecOps && LieutenantOnlyUnits ? IsLieutenantOption(option, skillsLookup) : true)
                    .ToList();
                PopulateUnitStatsFromFirstProfile(doc.RootElement);
                var orderTraits = ParseUnitOrderTraits(doc.RootElement);
                ShowIrregularOrderIcon = orderTraits.HasIrregular;
                ShowRegularOrderIcon = !orderTraits.HasIrregular && orderTraits.HasRegular;
                ShowImpetuousIcon = orderTraits.HasImpetuous;
                ShowTacticalAwarenessIcon = orderTraits.HasTacticalAwareness;
                var techTraits = ParseUnitTechTraits(doc.RootElement, equipLookup, skillsLookup, charsLookup);
                ShowCubeIcon = techTraits.HasCube;
                ShowCube2Icon = techTraits.HasCube2;
                ShowHackableIcon = techTraits.HasHackable;

                var stableEquipFromProfiles = displayNameContext.ComputeCommonDisplayNamesFromProfiles(profileGroupsJson, "equip", equipLookup);
                var stableEquipFromVisibleOptions = new List<string>();
                if (visibleOptions.Count > 0)
                {
                    stableEquipFromVisibleOptions = displayNameContext.IntersectDisplayNamesWithIncludes(doc.RootElement, visibleOptions, "equip", equipLookup);
                }
                var stableEquip = stableEquipFromProfiles
                    .Concat(stableEquipFromVisibleOptions)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var stableSkillsFromProfiles = displayNameContext.ComputeCommonDisplayNamesFromProfiles(profileGroupsJson, "skills", skillsLookup);
                var stableSkillsFromVisibleOptions = new List<string>();
                if (visibleOptions.Count > 0)
                {
                    stableSkillsFromVisibleOptions = displayNameContext.IntersectDisplayNamesWithIncludes(doc.RootElement, visibleOptions, "skills", skillsLookup);
                }
                var stableSkills = stableSkillsFromProfiles
                    .Concat(stableSkillsFromVisibleOptions)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                stableSkills = treatAsSpecOps
                    ? EnsureLieutenantSkill(stableSkills)
                    : stableSkills.Where(x => !x.Contains("lieutenant", StringComparison.OrdinalIgnoreCase)).ToList();
                UnitDisplayConfigurationsView.SelectedUnitCommonEquipment = stableEquip;
                UnitDisplayConfigurationsView.SelectedUnitCommonSkills = stableSkills;
                _summaryHighlightLieutenant = treatAsSpecOps;
                Console.WriteLine(
                    $"ArmyFactionSelectionPage summary extraction: unit='{_selectedUnit.Name}', options={visibleOptions.Count}, " +
                    $"commonEquip={stableEquip.Count}, commonSkills={stableSkills.Count}.");

                EquipmentSummary = $"Equipment: {(stableEquip.Count == 0 ? "-" : string.Join(", ", stableEquip))}";
                SpecialSkillsSummary = $"Special Skills: {(stableSkills.Count == 0 ? "-" : string.Join(", ", stableSkills))}";
                RefreshSummaryFormatted();
                PopulateProfilesFromProfileGroups(doc.RootElement, snapshot?.FiltersJson, forceLieutenant: treatAsSpecOps);
                UpdatePeripheralStatBlockFromVisibleProfiles();
                Console.WriteLine($"ArmyFactionSelectionPage LoadSelectedUnitDetailsAsync completed: heading='{UnitNameHeading}', MOV='{UnitMov}', equipment='{EquipmentSummary}'.");
                return;
            }
            else
            {
                Console.Error.WriteLine($"ArmyFactionSelectionPage: profileGroups missing for faction={_selectedUnit.SourceFactionId}, unit={_selectedUnit.Id}.");
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ArmyFactionSelectionPage LoadSelectedUnitDetailsAsync failed: {ex}");
        }
    }

    private async Task LoadSelectedUnitLogoAsync(ArmyUnitSelectionItem item)
    {
        UnitDisplayConfigurationsView.SelectedUnitPicture?.Dispose();
        UnitDisplayConfigurationsView.SelectedUnitPicture = null;

        try
        {
            Stream? stream = await OpenBestUnitLogoStreamAsync(item);

            if (stream is null)
            {
                UnitDisplayConfigurationsView.InvalidateSelectedUnitCanvas();
                return;
            }

            await using (stream)
            {
                var svg = new SKSvg();
                UnitDisplayConfigurationsView.SelectedUnitPicture = svg.Load(stream);
                if (UnitDisplayConfigurationsView.SelectedUnitPicture is null)
                {
                    Console.Error.WriteLine($"ArmyFactionSelectionPage selected logo parse failed: unit='{item.Name}', id={item.Id}, faction={item.SourceFactionId}.");
                }
                else
                {
                    var bounds = UnitDisplayConfigurationsView.SelectedUnitPicture.CullRect;
                    Console.WriteLine($"ArmyFactionSelectionPage selected logo loaded: unit='{item.Name}', bounds=({bounds.Left},{bounds.Top},{bounds.Right},{bounds.Bottom}).");
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ArmyFactionSelectionPage LoadSelectedUnitLogoAsync failed: {ex.Message}");
            UnitDisplayConfigurationsView.SelectedUnitPicture = null;
        }

        UnitDisplayConfigurationsView.InvalidateSelectedUnitCanvas();
    }

    private void PopulateProfilesFromProfileGroups(JsonElement profileGroupsRoot, string? filtersJson, bool forceLieutenant = false)
    {
        Profiles.Clear();
        ProfilesStatus = "Loading profiles...";

        if (profileGroupsRoot.ValueKind != JsonValueKind.Array)
        {
            ProfilesStatus = "No profiles found for this unit.";
            return;
        }

        var weaponsLookup = BuildIdNameLookup(filtersJson, "weapons");
        var equipLookup = BuildIdNameLookup(filtersJson, "equip");
        var skillsLookup = BuildIdNameLookup(filtersJson, "skills");
        var peripheralLookup = BuildIdNameLookup(filtersJson, "peripheral");
        var displayNameContext = CompanyUnitDetailDisplayNameContext.Create(filtersJson, ShowUnitsInInches, TryParseId);

        var buildRequest = new CompanyProfileBuildRequest<PeripheralMercsCompanyStats>
        {
            ProfileGroupsRoot = profileGroupsRoot,
            ForceLieutenant = forceLieutenant,
            ShowTacticalAwarenessIcon = ShowTacticalAwarenessIcon,
            WeaponsLookup = weaponsLookup,
            EquipLookup = equipLookup,
            SkillsLookup = skillsLookup,
            PeripheralLookup = peripheralLookup,
            IsControllerGroup = group => IsControllerGroup(profileGroupsRoot, group),
            ShouldIncludeOption = (_, _, optionName) => !_restrictSelectedUnitProfilesToFto || IsFtoLabel(optionName),
            GetOptionEntriesWithIncludes = (option, propertyName) => GetOptionEntriesWithIncludes(profileGroupsRoot, option, propertyName),
            GetDisplayPeripheralEntriesForOption = (group, option) => GetDisplayPeripheralEntriesForOption(profileGroupsRoot, group, option),
            GetOrderedDisplayNames = (entries, lookup) => displayNameContext.GetOrderedIdDisplayNamesFromEntries(entries, lookup),
            GetCountedDisplayNames = (entries, lookup) => displayNameContext.GetCountedDisplayNamesFromEntries(entries, lookup),
            ReadOptionSwc = ReadOptionSwc,
            IsPositiveSwc = IsPositiveSwc,
            IsMeleeWeaponName = IsMeleeWeaponName,
            ReadAdjustedOptionCost = (group, option) => ReadAdjustedOptionCost(profileGroupsRoot, group, option),
            ParseCostValue = ParseCostValue,
            ReadOptionCost = ReadOptionCost,
            TryFindPeripheralProfile = peripheralName =>
                TryFindPeripheralStatElement(profileGroupsRoot, peripheralName, out var peripheralProfile)
                    ? peripheralProfile
                    : (JsonElement?)null,
            BuildPeripheralStatBlock = (peripheralName, peripheralProfile) => BuildPeripheralStatBlock(peripheralName, peripheralProfile, filtersJson),
            TryGetPeripheralUnitCost = peripheralName =>
                TryGetPeripheralUnitCost(profileGroupsRoot, peripheralName, out var peripheralCost)
                    ? peripheralCost
                    : (int?)null,
            TryBuildSinglePeripheralDisplay = peripheralNames =>
            {
                var success = TryBuildSinglePeripheralDisplay(peripheralNames, out var peripheralName, out var peripheralCount);
                return (success, peripheralName, peripheralCount);
            },
            ExtractFirstPeripheralName = ExtractFirstPeripheralName,
            NormalizePeripheralNameForDedupe = NormalizePeripheralNameForDedupe,
            GetPeripheralTotalCount = GetPeripheralTotalCount,
            IsLieutenantOption = option => IsLieutenantOption(option, skillsLookup),
            FormatMoveValue = FormatMoveValue,
            BuildPeripheralSubtitle = BuildPeripheralSubtitle,
            ReadPeripheralNameHeading = stats => stats?.NameHeading ?? string.Empty,
            ReadPeripheralMoveFirstCm = stats => stats?.MoveFirstCm,
            ReadPeripheralMoveSecondCm = stats => stats?.MoveSecondCm,
            ReadPeripheralCc = stats => stats?.Cc ?? "-",
            ReadPeripheralBs = stats => stats?.Bs ?? "-",
            ReadPeripheralPh = stats => stats?.Ph ?? "-",
            ReadPeripheralWip = stats => stats?.Wip ?? "-",
            ReadPeripheralArm = stats => stats?.Arm ?? "-",
            ReadPeripheralBts = stats => stats?.Bts ?? "-",
            ReadPeripheralVitalityHeader = stats => stats?.VitalityHeader ?? "VITA",
            ReadPeripheralVitality = stats => stats?.Vitality ?? "-",
            ReadPeripheralS = stats => stats?.S ?? "-",
            ReadPeripheralAva = stats => stats?.Ava ?? "-",
            ReadPeripheralEquipment = stats => stats?.Equipment ?? "-",
            ReadPeripheralSkills = stats => stats?.Skills ?? "-"
        };

        var profileCoordinator = new CompanyProfileCoordinator();
        foreach (var profileItem in profileCoordinator.BuildProfiles(buildRequest))
        {
            Profiles.Add(profileItem);
        }

        ApplyLieutenantVisualStates();
    }

    private static int ReadEntryQuantity(JsonElement entry)
    {
        return CompanyProfileOptionService.ReadEntryQuantity(entry);
    }

    private static bool IsMeleeWeaponName(string name)
    {
        return CompanyProfileTextService.IsMeleeWeaponName(name);
    }

    private static string JoinOrDash(IEnumerable<string> values)
    {
        return CompanyProfileTextService.JoinOrDash(values);
    }

    private static List<string> EnsureLieutenantSkill(IEnumerable<string> skills)
    {
        return CompanyProfileTextService.EnsureLieutenantSkill(skills);
    }

    private static string ReadOptionSwc(JsonElement option)
    {
        return CompanyProfileOptionService.ReadOptionSwc(option);
    }

    private static string ReadOptionCost(JsonElement option)
    {
        return CompanyProfileOptionService.ReadOptionCost(option);
    }

    private static string ReadAdjustedOptionCost(JsonElement profileGroupsRoot, JsonElement group, JsonElement option)
    {
        return CompanyProfileOptionService.ReadAdjustedOptionCost(profileGroupsRoot, group, option);
    }

    private static IEnumerable<JsonElement> GetDisplayPeripheralEntriesForOption(
        JsonElement profileGroupsRoot,
        JsonElement group,
        JsonElement option)
    {
        return CompanyProfileOptionService.GetDisplayPeripheralEntriesForOption(profileGroupsRoot, group, option);
    }

    private static bool IsControllerGroup(JsonElement profileGroupsRoot, JsonElement group)
    {
        return CompanyProfileOptionService.IsControllerGroup(profileGroupsRoot, group);
    }

    private static int ReadOptionMinis(JsonElement option)
    {
        return CompanyProfileOptionService.ReadOptionMinis(option);
    }

    private static FormattedString BuildNameFormatted(string name)
    {
        return CompanyProfileTextService.BuildNameFormatted(name);
    }

    private static FormattedString BuildListFormattedString(IEnumerable<string> values, Color textColor, bool highlightLieutenantPurple = false)
    {
        return CompanyProfileTextService.BuildListFormattedString(values, textColor, highlightLieutenantPurple);
    }

    private async Task<Stream?> OpenBestUnitLogoStreamAsync(ArmyUnitSelectionItem item)
    {
        Console.WriteLine($"ArmyFactionSelectionPage logo resolve start: unit='{item.Name}', id={item.Id}, faction={item.SourceFactionId}.");
        foreach (var cachedPath in BuildUnitCachedPathCandidates(item))
        {
            if (string.IsNullOrWhiteSpace(cachedPath))
            {
                continue;
            }

            var exists = File.Exists(cachedPath);
            Console.WriteLine($"ArmyFactionSelectionPage logo cached candidate: '{cachedPath}', exists={exists}.");
            if (!string.IsNullOrWhiteSpace(cachedPath) && File.Exists(cachedPath))
            {
                Console.WriteLine($"ArmyFactionSelectionPage logo using cached: '{cachedPath}'.");
                return File.OpenRead(cachedPath);
            }
        }

        foreach (var packagedPath in BuildUnitPackagedPathCandidates(item))
        {
            if (string.IsNullOrWhiteSpace(packagedPath))
            {
                continue;
            }

            try
            {
                var stream = await FileSystem.Current.OpenAppPackageFileAsync(packagedPath);
                Console.WriteLine($"ArmyFactionSelectionPage logo using packaged: '{packagedPath}'.");
                return stream;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"ArmyFactionSelectionPage logo packaged candidate failed: '{packagedPath}': {ex.Message}");
            }
        }

        Console.Error.WriteLine($"ArmyFactionSelectionPage logo resolve failed: unit='{item.Name}', id={item.Id}, faction={item.SourceFactionId}.");
        return null;
    }

    private IEnumerable<string?> BuildUnitCachedPathCandidates(ArmyUnitSelectionItem item)
    {
        yield return item.CachedLogoPath;

        if (_factionLogoCacheService is not null)
        {
            yield return _factionLogoCacheService.GetCachedUnitLogoPath(item.SourceFactionId, item.Id);

            if (_factionSelectionState.LeftSlotFaction is not null)
            {
                yield return _factionLogoCacheService.GetCachedUnitLogoPath(_factionSelectionState.LeftSlotFaction.Id, item.Id);
            }

            if (_factionSelectionState.RightSlotFaction is not null)
            {
                yield return _factionLogoCacheService.GetCachedUnitLogoPath(_factionSelectionState.RightSlotFaction.Id, item.Id);
            }

            yield return _factionLogoCacheService.GetCachedLogoPath(item.SourceFactionId);
        }
    }

    private IEnumerable<string?> BuildUnitPackagedPathCandidates(ArmyUnitSelectionItem item)
    {
        yield return item.PackagedLogoPath;

        if (_factionLogoCacheService is not null)
        {
            yield return _factionLogoCacheService.GetPackagedUnitLogoPath(item.SourceFactionId, item.Id);

            if (_factionSelectionState.LeftSlotFaction is not null)
            {
                yield return _factionLogoCacheService.GetPackagedUnitLogoPath(_factionSelectionState.LeftSlotFaction.Id, item.Id);
            }

            if (_factionSelectionState.RightSlotFaction is not null)
            {
                yield return _factionLogoCacheService.GetPackagedUnitLogoPath(_factionSelectionState.RightSlotFaction.Id, item.Id);
            }

            yield return _factionLogoCacheService.GetPackagedFactionLogoPath(item.SourceFactionId);
        }
        else
        {
            yield return $"SVGCache/units/{item.SourceFactionId}-{item.Id}.svg";
            if (_factionSelectionState.LeftSlotFaction is not null)
            {
                yield return $"SVGCache/units/{_factionSelectionState.LeftSlotFaction.Id}-{item.Id}.svg";
            }

            if (_factionSelectionState.RightSlotFaction is not null)
            {
                yield return $"SVGCache/units/{_factionSelectionState.RightSlotFaction.Id}-{item.Id}.svg";
            }

            yield return $"SVGCache/factions/{item.SourceFactionId}.svg";
        }
    }

    private List<ArmyFactionSelectionItem> GetUnitSourceFactions()
    {
        if (!ShowRightSelectionBox)
        {
            return _factionSelectionState.LeftSlotFaction is null ? [] : [_factionSelectionState.LeftSlotFaction];
        }

        var list = new List<ArmyFactionSelectionItem>(2);
        if (_factionSelectionState.LeftSlotFaction is not null)
        {
            list.Add(_factionSelectionState.LeftSlotFaction);
        }

        if (_factionSelectionState.RightSlotFaction is not null && (_factionSelectionState.LeftSlotFaction is null || _factionSelectionState.RightSlotFaction.Id != _factionSelectionState.LeftSlotFaction.Id))
        {
            list.Add(_factionSelectionState.RightSlotFaction);
        }

        return list;
    }

    private static List<string> BuildConfigurationSkillNames(IEnumerable<string> rawSkillNames)
    {
        return CompanyProfileTextService.BuildConfigurationSkillNames(rawSkillNames);
    }

    private static Dictionary<int, string> BuildIdNameLookup(string? filtersJson, string sectionName)
    {
        return CompanySelectionSharedUtilities.BuildIdNameLookup(filtersJson, sectionName);
    }

    private static void MergeFireteamEntries(
        string? fireteamChartJson,
        Dictionary<string, TeamAggregate> target)
    {
        if (string.IsNullOrWhiteSpace(fireteamChartJson))
        {
            return;
        }

        try
        {
            using var doc = JsonDocument.Parse(fireteamChartJson);
            if (!doc.RootElement.TryGetProperty("teams", out var teamsElement) ||
                teamsElement.ValueKind != JsonValueKind.Array)
            {
                return;
            }

            foreach (var teamElement in teamsElement.EnumerateArray())
            {
                var name = ReadString(teamElement, "name", string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                var (duo, haris, core) = ReadTeamTypeCounts(teamElement);
                if (!target.TryGetValue(name, out var aggregate))
                {
                    aggregate = new TeamAggregate(name);
                    target[name] = aggregate;
                }

                aggregate.AddCounts(duo, haris, core);

                foreach (var limit in ReadTeamUnitLimits(teamElement))
                {
                    aggregate.MergeUnitLimit(limit.Name, limit.Min, limit.Max, limit.Slug, limit.MinAsterisk);
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ArmyFactionSelectionPage MergeFireteamEntries failed: {ex.Message}");
        }
    }

    private static (int Duo, int Haris, int Core) ReadTeamTypeCounts(JsonElement teamElement)
    {
        var duo = 0;
        var haris = 0;
        var core = 0;

        if (!teamElement.TryGetProperty("type", out var typeElement) || typeElement.ValueKind != JsonValueKind.Array)
        {
            return (duo, haris, core);
        }

        foreach (var type in typeElement.EnumerateArray())
        {
            if (type.ValueKind != JsonValueKind.String)
            {
                continue;
            }

            var value = type.GetString();
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            if (value.Equals("DUO", StringComparison.OrdinalIgnoreCase))
            {
                duo++;
            }
            else if (value.Equals("HARIS", StringComparison.OrdinalIgnoreCase))
            {
                haris++;
            }
            else if (value.Equals("CORE", StringComparison.OrdinalIgnoreCase))
            {
                core++;
            }
        }

        return (duo, haris, core);
    }

    private static List<(string Name, int Min, int Max, string? Slug, bool MinAsterisk)> ReadTeamUnitLimits(JsonElement teamElement)
    {
        var results = new List<(string Name, int Min, int Max, string? Slug, bool MinAsterisk)>();
        if (!teamElement.TryGetProperty("units", out var unitsElement) || unitsElement.ValueKind != JsonValueKind.Array)
        {
            return results;
        }

        foreach (var unitElement in unitsElement.EnumerateArray())
        {
            var name = ReadString(unitElement, "name", string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                name = ReadString(unitElement, "slug", "Unknown");
            }

            var comment = ReadString(unitElement, "comment", string.Empty).Trim();
            var slug = ReadString(unitElement, "slug", string.Empty).Trim();
            var displayName = name;
            if (!string.IsNullOrWhiteSpace(comment))
            {
                displayName = $"{name} {comment}".Trim();
            }

            var min = ReadInt(unitElement, "min", 0);
            var max = ReadInt(unitElement, "max", 0);
            var minAsterisk = HasAsteriskMin(unitElement) || ReadBool(unitElement, "required", false);
            results.Add((displayName, min, max, string.IsNullOrWhiteSpace(slug) ? null : slug, minAsterisk));
        }

        return results;
    }

    private static bool HasAsteriskMin(JsonElement element)
    {
        return CompanySelectionSharedUtilities.HasAsteriskMin(element);
    }

    private static string ReadString(JsonElement element, string propertyName, string fallback)
    {
        return CompanySelectionSharedUtilities.ReadString(element, propertyName, fallback);
    }

    private static int ReadInt(JsonElement element, string propertyName, int fallback)
    {
        return CompanySelectionSharedUtilities.ReadInt(element, propertyName, fallback);
    }

    private static bool ReadBool(JsonElement element, string propertyName, bool fallback)
    {
        return CompanySelectionSharedUtilities.ReadBool(element, propertyName, fallback);
    }

    private static bool UnitHasLieutenantOption(string? profileGroupsJson, IReadOnlyDictionary<int, string> skillsLookup)
    {
        return CompanySelectionSharedUtilities.UnitHasLieutenantOption(profileGroupsJson, skillsLookup);
    }

    private static bool UnitHasVisibleOption(
        string? profileGroupsJson,
        IReadOnlyDictionary<int, string> skillsLookup,
        bool requireLieutenant,
        bool requireZeroSwc,
        int? maxCost = null)
    {
        if (string.IsNullOrWhiteSpace(profileGroupsJson))
        {
            return false;
        }

        try
        {
            using var doc = JsonDocument.Parse(profileGroupsJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
            {
                return false;
            }

            foreach (var group in doc.RootElement.EnumerateArray())
            {
                if (!group.TryGetProperty("options", out var optionsElement) || optionsElement.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var option in optionsElement.EnumerateArray())
                {
                    if (requireLieutenant && !IsLieutenantOption(option, skillsLookup))
                    {
                        continue;
                    }

                    if (requireZeroSwc && IsPositiveSwc(ReadOptionSwc(option)))
                    {
                        continue;
                    }

                    if (maxCost.HasValue && ParseCostValue(ReadAdjustedOptionCost(doc.RootElement, group, option)) > maxCost.Value)
                    {
                        continue;
                    }

                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ArmyFactionSelectionPage UnitHasVisibleOption failed: {ex.Message}");
        }

        return false;
    }

    private static bool UnitHasVisibleOptionWithFilter(
        string? profileGroupsJson,
        IReadOnlyDictionary<int, string> skillsLookup,
        IReadOnlyDictionary<int, string> charsLookup,
        IReadOnlyDictionary<int, string> equipLookup,
        IReadOnlyDictionary<int, string> weaponsLookup,
        IReadOnlyDictionary<int, string> ammoLookup,
        UnitFilterCriteria criteria,
        bool requireLieutenant,
        bool requireZeroSwc,
        int? maxCost = null,
        bool requireFto = false)
    {
        if (string.IsNullOrWhiteSpace(profileGroupsJson))
        {
            return false;
        }

        var filterQuery = criteria.ToQuery();

        try
        {
            using var doc = JsonDocument.Parse(profileGroupsJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
            {
                return false;
            }

            foreach (var group in doc.RootElement.EnumerateArray())
            {
                if (!group.TryGetProperty("options", out var optionsElement) || optionsElement.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var option in optionsElement.EnumerateArray())
                {
                    if (requireFto)
                    {
                        var optionName = option.TryGetProperty("name", out var optionNameElement) &&
                                         optionNameElement.ValueKind == JsonValueKind.String
                            ? optionNameElement.GetString()
                            : null;
                        if (!IsFtoLabel(optionName))
                        {
                            continue;
                        }
                    }

                    if (requireLieutenant && !IsLieutenantOption(option, skillsLookup))
                    {
                        continue;
                    }

                    if (requireZeroSwc && IsPositiveSwc(ReadOptionSwc(option)))
                    {
                        continue;
                    }

                    var optionCost = ParseCostValue(ReadAdjustedOptionCost(doc.RootElement, group, option));
                    if (maxCost.HasValue && optionCost > maxCost.Value)
                    {
                        continue;
                    }

                    if (filterQuery.MinPoints.HasValue && optionCost < filterQuery.MinPoints.Value)
                    {
                        continue;
                    }

                    if (filterQuery.MaxPoints.HasValue && optionCost > filterQuery.MaxPoints.Value)
                    {
                        continue;
                    }

                    if (!OptionMatchesUnitFilter(
                            doc.RootElement,
                            group,
                            option,
                            charsLookup,
                            skillsLookup,
                            equipLookup,
                            weaponsLookup,
                            ammoLookup,
                            filterQuery))
                    {
                        continue;
                    }

                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ArmyFactionSelectionPage UnitHasVisibleOptionWithFilter failed: {ex.Message}");
        }

        return false;
    }

    private static bool OptionMatchesUnitFilter(
        JsonElement profileGroupsRoot,
        JsonElement profileGroup,
        JsonElement option,
        IReadOnlyDictionary<int, string> charsLookup,
        IReadOnlyDictionary<int, string> skillsLookup,
        IReadOnlyDictionary<int, string> equipLookup,
        IReadOnlyDictionary<int, string> weaponsLookup,
        IReadOnlyDictionary<int, string> ammoLookup,
        UnitFilterQuery filterQuery)
    {
        foreach (var term in filterQuery.Terms)
        {
            if (term.Field == UnitFilterField.Classification || term.Values.Count == 0)
            {
                continue;
            }

            var matches = term.Field switch
            {
                UnitFilterField.Characteristics => TermMatchesOptionOrGroup(
                    term,
                    profileGroupsRoot,
                    profileGroup,
                    option,
                    "chars",
                    charsLookup),
                UnitFilterField.Skills => TermMatchesOptionOrGroup(
                    term,
                    profileGroupsRoot,
                    profileGroup,
                    option,
                    "skills",
                    skillsLookup),
                UnitFilterField.Equipment => TermMatchesOptionOrGroup(
                    term,
                    profileGroupsRoot,
                    profileGroup,
                    option,
                    "equip",
                    equipLookup),
                UnitFilterField.Weapons => TermMatchesOptionOrGroup(
                    term,
                    profileGroupsRoot,
                    profileGroup,
                    option,
                    "weapons",
                    weaponsLookup),
                UnitFilterField.Ammo => TermMatchesOptionOnly(
                    term,
                    profileGroupsRoot,
                    option,
                    ammoLookup,
                    ["ammunition", "ammo"]),
                _ => true
            };

            if (!matches)
            {
                return false;
            }
        }

        return true;
    }

    private static bool TermMatchesOptionOrGroup(
        UnitFilterTerm term,
        JsonElement profileGroupsRoot,
        JsonElement profileGroup,
        JsonElement option,
        string propertyName,
        IReadOnlyDictionary<int, string> lookup)
    {
        return term.MatchMode == UnitFilterMatchMode.All
            ? term.Values.All(value => OptionOrGroupContainsLookupName(profileGroupsRoot, profileGroup, option, propertyName, lookup, value))
            : term.Values.Any(value => OptionOrGroupContainsLookupName(profileGroupsRoot, profileGroup, option, propertyName, lookup, value));
    }

    private static bool TermMatchesOptionOnly(
        UnitFilterTerm term,
        JsonElement profileGroupsRoot,
        JsonElement option,
        IReadOnlyDictionary<int, string> lookup,
        IEnumerable<string> propertyNames)
    {
        return term.MatchMode == UnitFilterMatchMode.All
            ? term.Values.All(value => OptionContainsAnyLookupName(profileGroupsRoot, option, propertyNames, lookup, value))
            : term.Values.Any(value => OptionContainsAnyLookupName(profileGroupsRoot, option, propertyNames, lookup, value));
    }

    private static bool OptionOrGroupContainsLookupName(
        JsonElement profileGroupsRoot,
        JsonElement profileGroup,
        JsonElement option,
        string propertyName,
        IReadOnlyDictionary<int, string> lookup,
        string expectedValue)
    {
        return OptionContainsLookupName(profileGroupsRoot, option, propertyName, lookup, expectedValue) ||
               GroupProfilesContainLookupName(profileGroup, propertyName, lookup, expectedValue);
    }

    private static bool OptionContainsAnyLookupName(
        JsonElement profileGroupsRoot,
        JsonElement option,
        IEnumerable<string> propertyNames,
        IReadOnlyDictionary<int, string> lookup,
        string expectedValue)
    {
        foreach (var propertyName in propertyNames)
        {
            if (OptionContainsLookupName(profileGroupsRoot, option, propertyName, lookup, expectedValue))
            {
                return true;
            }
        }

        return false;
    }

    private static bool OptionContainsLookupName(
        JsonElement profileGroupsRoot,
        JsonElement option,
        string propertyName,
        IReadOnlyDictionary<int, string> lookup,
        string expectedValue)
    {
        if (lookup.Count == 0 || string.IsNullOrWhiteSpace(expectedValue))
        {
            return false;
        }

        foreach (var entry in GetOptionEntriesWithIncludes(profileGroupsRoot, option, propertyName))
        {
            if (!TryParseId(entry, out var id) || !lookup.TryGetValue(id, out var name))
            {
                continue;
            }

            if (string.Equals(name, expectedValue, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool GroupProfilesContainLookupName(
        JsonElement profileGroup,
        string propertyName,
        IReadOnlyDictionary<int, string> lookup,
        string expectedValue)
    {
        if (lookup.Count == 0 || string.IsNullOrWhiteSpace(expectedValue))
        {
            return false;
        }

        if (!profileGroup.TryGetProperty("profiles", out var profilesElement) || profilesElement.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        foreach (var profile in profilesElement.EnumerateArray())
        {
            if (!profile.TryGetProperty(propertyName, out var valuesElement) || valuesElement.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var entry in valuesElement.EnumerateArray())
            {
                if (!TryParseId(entry, out var id) || !lookup.TryGetValue(id, out var name))
                {
                    continue;
                }

                if (string.Equals(name, expectedValue, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool MatchesClassificationFilter(
        ArmyUnitSelectionItem unit,
        IReadOnlyDictionary<int, string> typeLookup)
    {
        var classificationTerm = _activeUnitFilter.ToQuery().GetTerm(UnitFilterField.Classification);
        if (classificationTerm is null || classificationTerm.Values.Count == 0)
        {
            return true;
        }

        if (!unit.Type.HasValue || typeLookup.Count == 0)
        {
            return false;
        }

        if (!typeLookup.TryGetValue(unit.Type.Value, out var typeName))
        {
            return false;
        }

        return classificationTerm.MatchMode == UnitFilterMatchMode.All
            ? classificationTerm.Values.All(value => string.Equals(typeName, value, StringComparison.OrdinalIgnoreCase))
            : classificationTerm.Values.Any(value => string.Equals(typeName, value, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsLieutenantOption(JsonElement option, IReadOnlyDictionary<int, string> skillsLookup)
    {
        return CompanySelectionSharedUtilities.IsLieutenantOption(option, skillsLookup);
    }

    private static bool HasLieutenantOrder(JsonElement option)
    {
        return CompanySelectionSharedUtilities.HasLieutenantOrder(option);
    }

    private static bool IsPositiveSwc(string swc)
    {
        return CompanySelectionSharedUtilities.IsPositiveSwc(swc);
    }

    private static bool TryParseId(JsonElement element, out int id)
    {
        return CompanySelectionSharedUtilities.TryParseId(element, out id);
    }

    private static string BuildUnitSubtitle(
        ArmyResumeRecord unit,
        IReadOnlyDictionary<int, string> typeLookup,
        IReadOnlyDictionary<int, string> categoryLookup)
    {
        var typeName = unit.Type.HasValue && typeLookup.TryGetValue(unit.Type.Value, out var t)
            ? t
            : (unit.Type?.ToString() ?? "?");

        var categoryName = unit.Category.HasValue && categoryLookup.TryGetValue(unit.Category.Value, out var c)
            ? c
            : (unit.Category?.ToString() ?? "?");

        return $"{typeName} - {categoryName}";
    }

    private static bool IsCharacterCategory(ArmyResumeRecord unit, IReadOnlyDictionary<int, string> categoryLookup)
    {
        if (unit.Category.HasValue && unit.Category.Value == CharacterCategoryId)
        {
            return true;
        }

        if (unit.Category.HasValue && categoryLookup.TryGetValue(unit.Category.Value, out var categoryName))
        {
            return !string.IsNullOrWhiteSpace(categoryName) &&
                   categoryName.Contains("character", StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    private void ResetUnitDetails(bool clearLogo = true, bool resetHeaderColors = true)
    {
        UnitNameHeading = "Select a unit";
        if (resetHeaderColors)
        {
            ApplyUnitHeaderColorsByVanillaFactionName(null);
        }
        if (clearLogo)
        {
            Console.WriteLine("ArmyFactionSelectionPage ResetUnitDetails: clearing selected unit logo.");
        UnitDisplayConfigurationsView.SelectedUnitPicture?.Dispose();
        UnitDisplayConfigurationsView.SelectedUnitPicture = null;
        UnitDisplayConfigurationsView.InvalidateSelectedUnitCanvas();
    }
        UnitDisplayConfigurationsView.SelectedUnitProfileGroupsJson = null;
        UnitDisplayConfigurationsView.SelectedUnitFiltersJson = null;
        ResetUnitStatsOnly();
        EquipmentSummary = "Equipment: -";
        SpecialSkillsSummary = "Special Skills: -";
        UnitDisplayConfigurationsView.SelectedUnitCommonEquipment = [];
        UnitDisplayConfigurationsView.SelectedUnitCommonSkills = [];
        _summaryHighlightLieutenant = false;
        RefreshSummaryFormatted();
        Profiles.Clear();
        ProfilesStatus = "Select a unit.";
        ShowRegularOrderIcon = false;
        ShowIrregularOrderIcon = false;
        ShowImpetuousIcon = false;
        ShowTacticalAwarenessIcon = false;
        ShowCubeIcon = false;
        ShowCube2Icon = false;
        ShowHackableIcon = false;
    }

    private static List<string> MergeCommonAndUnique(IEnumerable<string> commonValues, string? uniqueValues)
    {
        return CompanyProfileTextService.MergeCommonAndUnique(commonValues, uniqueValues);
    }

    private void ResetUnitStatsOnly()
    {
        UnitMoveFirstCm = null;
        UnitMoveSecondCm = null;
        UnitMov = "-";
        UnitCc = "-";
        UnitBs = "-";
        UnitPh = "-";
        UnitWip = "-";
        UnitArm = "-";
        UnitBts = "-";
        UnitVitalityHeader = "VITA";
        UnitVitality = "-";
        UnitS = "-";
        UnitAva = "-";
        ResetPeripheralStatsOnly();
    }

    private void ResetPeripheralStatsOnly()
    {
        PeripheralMoveFirstCm = null;
        PeripheralMoveSecondCm = null;
        HasPeripheralStatBlock = false;
        PeripheralNameHeading = string.Empty;
        PeripheralMov = "-";
        PeripheralCc = "-";
        PeripheralBs = "-";
        PeripheralPh = "-";
        PeripheralWip = "-";
        PeripheralArm = "-";
        PeripheralBts = "-";
        PeripheralVitalityHeader = "VITA";
        PeripheralVitality = "-";
        PeripheralS = "-";
        PeripheralAva = "-";
        PeripheralEquipment = "-";
        PeripheralSkills = "-";
        PeripheralEquipmentFormatted = BuildNamedSummaryFormatted("Equipment", Array.Empty<string>(), Color.FromArgb("#06B6D4"));
        PeripheralSkillsFormatted = BuildNamedSummaryFormatted("Skills", Array.Empty<string>(), Color.FromArgb("#F59E0B"));
    }

    private static IEnumerable<JsonElement> EnumerateOptions(JsonElement profileGroupsRoot)
    {
        return CompanySelectionSharedUtilities.EnumerateOptions(profileGroupsRoot);
    }

    private static bool TryGetFirstProfileGroup(string? profileGroupsJson, out JsonElement group)
    {
        return CompanySelectionSharedUtilities.TryGetFirstProfileGroup(profileGroupsJson, out group);
    }

    private void PopulateUnitStatsFromFirstProfile(JsonElement profileGroupsArray)
    {
        ResetUnitStatsOnly();

        if (profileGroupsArray.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        JsonElement? firstProfile = null;
        foreach (var group in profileGroupsArray.EnumerateArray())
        {
            if (!group.TryGetProperty("profiles", out var profilesElement) || profilesElement.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var profile in profilesElement.EnumerateArray())
            {
                firstProfile = profile;
                break;
            }

            if (firstProfile.HasValue)
            {
                break;
            }
        }

        if (!firstProfile.HasValue)
        {
            var firstOption = EnumerateOptions(profileGroupsArray).FirstOrDefault();
            if (firstOption.ValueKind != JsonValueKind.Object)
            {
                return;
            }

            PopulateUnitStatsFromElement(firstOption);
            return;
        }

        PopulateUnitStatsFromElement(firstProfile.Value);
    }

    private void PopulateUnitStatsFromElement(JsonElement selectedElement)
    {
        var unitMove = _armyDataService.ReadMoveValue(selectedElement);
        UnitMoveFirstCm = unitMove.FirstCm;
        UnitMoveSecondCm = unitMove.SecondCm;
        UnitMov = unitMove.DisplayValue;
        UnitCc = ReadIntAsString(selectedElement, "cc");
        UnitBs = ReadIntAsString(selectedElement, "bs");
        UnitPh = ReadIntAsString(selectedElement, "ph");
        UnitWip = ReadIntAsString(selectedElement, "wip");
        UnitArm = ReadIntAsString(selectedElement, "arm");
        UnitBts = ReadIntAsString(selectedElement, "bts");
        UnitS = ReadIntAsString(selectedElement, "s");
        UnitAva = ReadAvaAsString(selectedElement);
        var (vitalityHeader, vitalityValue) = ReadVitality(selectedElement);
        UnitVitalityHeader = vitalityHeader;
        UnitVitality = vitalityValue;
    }

    private static string ReadMoveFromProfile(JsonElement profile)
    {
        return CompanySelectionSharedUtilities.ReadMoveFromProfile(profile);
    }

    private string FormatMoveValue(int? firstCm, int? secondCm)
    {
        return _armyDataService.FormatMoveValue(firstCm, secondCm);
    }

    private static string ReplaceSubtitleMoveDisplay(string? subtitle, string moveDisplay)
    {
        return CompanySelectionSharedUtilities.ReplaceSubtitleMoveDisplay(subtitle, moveDisplay);
    }

    private void UpdateUnitMoveDisplay()
    {
        UnitMov = _armyDataService.FormatMoveValue(UnitMoveFirstCm, UnitMoveSecondCm);
    }

    private void UpdatePeripheralMoveDisplay()
    {
        PeripheralMov = _armyDataService.FormatMoveValue(PeripheralMoveFirstCm, PeripheralMoveSecondCm);
    }

    private void PopulatePeripheralStatsFromElement(JsonElement selectedElement, string peripheralName)
    {
        var peripheralStats = BuildPeripheralStatBlock(peripheralName, selectedElement, UnitDisplayConfigurationsView.SelectedUnitFiltersJson);
        if (peripheralStats is null)
        {
            return;
        }

        ApplyPeripheralStatBlock(peripheralStats);
    }

    private void UpdatePeripheralStatBlockFromVisibleProfiles()
    {
        ResetPeripheralStatsOnly();

        if (string.IsNullOrWhiteSpace(UnitDisplayConfigurationsView.SelectedUnitProfileGroupsJson))
        {
            return;
        }

        var firstPeripheralProfile = Profiles.FirstOrDefault(x => x.IsVisible && x.HasPeripherals);
        if (firstPeripheralProfile is null)
        {
            return;
        }

        var peripheralName = ExtractFirstPeripheralName(firstPeripheralProfile.Peripherals);
        if (string.IsNullOrWhiteSpace(peripheralName))
        {
            return;
        }

        try
        {
            using var doc = JsonDocument.Parse(UnitDisplayConfigurationsView.SelectedUnitProfileGroupsJson);
            if (!TryFindPeripheralStatElement(doc.RootElement, peripheralName, out var peripheralProfile))
            {
                return;
            }

            PopulatePeripheralStatsFromElement(peripheralProfile, peripheralName);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ArmyFactionSelectionPage UpdatePeripheralStatBlockFromVisibleProfiles failed: {ex.Message}");
        }
    }

    private PeripheralMercsCompanyStats? BuildPeripheralStatBlock(string peripheralName, JsonElement peripheralProfile, string? filtersJson)
    {
        if (string.IsNullOrWhiteSpace(peripheralName))
        {
            return null;
        }

        var equipLookup = BuildIdNameLookup(filtersJson, "equip");
        var skillsLookup = BuildIdNameLookup(filtersJson, "skills");
        var displayNameContext = CompanyUnitDetailDisplayNameContext.Create(filtersJson, ShowUnitsInInches, TryParseId);
        var peripheralMove = _armyDataService.ReadMoveValue(peripheralProfile);
        var moveFirstCm = peripheralMove.FirstCm;
        var moveSecondCm = peripheralMove.SecondCm;
        var equipmentNames = displayNameContext.GetOrderedIdDisplayNamesFromEntries(GetContainerEntries(peripheralProfile, "equip"), equipLookup);
        var skillNames = BuildConfigurationSkillNames(
            displayNameContext.GetOrderedIdDisplayNamesFromEntries(GetContainerEntries(peripheralProfile, "skills"), skillsLookup));
        var (vitalityHeader, vitalityValue) = ReadVitality(peripheralProfile);

        return new PeripheralMercsCompanyStats
        {
            NameHeading = $"Peripheral: {peripheralName}",
            MoveFirstCm = moveFirstCm,
            MoveSecondCm = moveSecondCm,
            Mov = ReadMoveFromProfile(peripheralProfile),
            Cc = ReadIntAsString(peripheralProfile, "cc"),
            Bs = ReadIntAsString(peripheralProfile, "bs"),
            Ph = ReadIntAsString(peripheralProfile, "ph"),
            Wip = ReadIntAsString(peripheralProfile, "wip"),
            Arm = ReadIntAsString(peripheralProfile, "arm"),
            Bts = ReadIntAsString(peripheralProfile, "bts"),
            VitalityHeader = vitalityHeader,
            Vitality = vitalityValue,
            S = ReadIntAsString(peripheralProfile, "s"),
            Ava = ReadAvaAsString(peripheralProfile),
            Equipment = JoinOrDash(equipmentNames),
            Skills = JoinOrDash(skillNames)
        };
    }

    private void ApplyPeripheralStatBlock(PeripheralMercsCompanyStats peripheralStats)
    {
        PeripheralMoveFirstCm = peripheralStats.MoveFirstCm;
        PeripheralMoveSecondCm = peripheralStats.MoveSecondCm;
        UpdatePeripheralMoveDisplay();
        PeripheralNameHeading = peripheralStats.NameHeading;
        PeripheralCc = peripheralStats.Cc;
        PeripheralBs = peripheralStats.Bs;
        PeripheralPh = peripheralStats.Ph;
        PeripheralWip = peripheralStats.Wip;
        PeripheralArm = peripheralStats.Arm;
        PeripheralBts = peripheralStats.Bts;
        PeripheralVitalityHeader = peripheralStats.VitalityHeader;
        PeripheralVitality = peripheralStats.Vitality;
        PeripheralS = peripheralStats.S;
        PeripheralAva = peripheralStats.Ava;
        PeripheralEquipment = peripheralStats.Equipment;
        PeripheralSkills = peripheralStats.Skills;
        PeripheralEquipmentFormatted = BuildNamedSummaryFormatted("Equipment", SplitDisplayLine(PeripheralEquipment), Color.FromArgb("#06B6D4"));
        PeripheralSkillsFormatted = BuildNamedSummaryFormatted("Skills", SplitDisplayLine(PeripheralSkills), Color.FromArgb("#F59E0B"));
        HasPeripheralStatBlock = true;
    }

    private static string ExtractFirstPeripheralName(string? peripheralsText)
    {
        return CompanySelectionSharedUtilities.ExtractFirstPeripheralName(peripheralsText);
    }

    private static string NormalizePeripheralNameForDedupe(string? value)
    {
        return CompanySelectionSharedUtilities.NormalizePeripheralNameForDedupe(value);
    }

    private static int GetPeripheralTotalCount(IEnumerable<string> peripheralNames)
    {
        return CompanySelectionSharedUtilities.GetPeripheralTotalCount(peripheralNames);
    }

    private static bool TryBuildSinglePeripheralDisplay(
        IReadOnlyList<string> peripheralNames,
        out string peripheralName,
        out int peripheralCount)
    {
        peripheralName = string.Empty;
        peripheralCount = 0;

        if (peripheralNames.Count != 1)
        {
            return false;
        }

        var only = peripheralNames[0];
        if (string.IsNullOrWhiteSpace(only) || only == "-")
        {
            return false;
        }

        var match = Regex.Match(only, @"^(.*)\((\d+)\)\s*$");
        if (!match.Success || !int.TryParse(match.Groups[2].Value, out peripheralCount))
        {
            return false;
        }

        peripheralName = match.Groups[1].Value.Trim();
        if (string.IsNullOrWhiteSpace(peripheralName))
        {
            return false;
        }

        return peripheralCount > 0;
    }

    private static bool TryGetPeripheralUnitCost(JsonElement profileGroupsRoot, string peripheralName, out int peripheralUnitCost)
    {
        peripheralUnitCost = 0;
        if (profileGroupsRoot.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        var expected = NormalizeComparisonToken(peripheralName);
        if (string.IsNullOrWhiteSpace(expected))
        {
            return false;
        }

        foreach (var group in profileGroupsRoot.EnumerateArray())
        {
            var groupIsc = group.TryGetProperty("isc", out var groupIscElement) && groupIscElement.ValueKind == JsonValueKind.String
                ? groupIscElement.GetString() ?? string.Empty
                : string.Empty;
            var groupMatch = NormalizeComparisonToken(groupIsc) == expected;

            if (!groupMatch &&
                group.TryGetProperty("profiles", out var profilesElement) &&
                profilesElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var profile in profilesElement.EnumerateArray())
                {
                    var profileName = profile.TryGetProperty("name", out var profileNameElement) && profileNameElement.ValueKind == JsonValueKind.String
                        ? profileNameElement.GetString() ?? string.Empty
                        : string.Empty;
                    if (NormalizeComparisonToken(profileName) == expected)
                    {
                        groupMatch = true;
                        break;
                    }
                }
            }

            if (!groupMatch &&
                group.TryGetProperty("options", out var matchOptionsElement) &&
                matchOptionsElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var option in matchOptionsElement.EnumerateArray())
                {
                    var optionName = option.TryGetProperty("name", out var optionNameElement) && optionNameElement.ValueKind == JsonValueKind.String
                        ? optionNameElement.GetString() ?? string.Empty
                        : string.Empty;
                    if (NormalizeComparisonToken(optionName) == expected)
                    {
                        groupMatch = true;
                        break;
                    }
                }
            }

            if (!groupMatch ||
                !group.TryGetProperty("options", out var optionsElement) ||
                optionsElement.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var option in optionsElement.EnumerateArray())
            {
                var optionCost = ParseCostValue(ReadOptionCost(option));
                if (optionCost <= 0)
                {
                    continue;
                }

                var minis = Math.Max(1, ReadOptionMinis(option));
                peripheralUnitCost = Math.Max(1, optionCost / minis);
                return true;
            }
        }

        return false;
    }

    private static bool HasStatFields(JsonElement element)
    {
        return CompanySelectionSharedUtilities.HasStatFields(element);
    }

    private void PopulatePeripheralStatBlock(
        JsonElement profileGroupsRoot,
        string? filtersJson,
        bool forceLieutenant,
        IReadOnlyDictionary<int, string> skillsLookup)
    {
        ResetPeripheralStatsOnly();

        if (profileGroupsRoot.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        var peripheralLookup = BuildIdNameLookup(filtersJson, "peripheral");
        var hasControllerGroups = profileGroupsRoot.EnumerateArray().Any(group => IsControllerGroup(profileGroupsRoot, group));

        foreach (var group in profileGroupsRoot.EnumerateArray())
        {
            if (hasControllerGroups && !IsControllerGroup(profileGroupsRoot, group))
            {
                continue;
            }

            if (!group.TryGetProperty("options", out var optionsElement) || optionsElement.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var option in optionsElement.EnumerateArray())
            {
                if (IsPositiveSwc(ReadOptionSwc(option)))
                {
                    continue;
                }

                if (!forceLieutenant && LieutenantOnlyUnits && !IsLieutenantOption(option, skillsLookup))
                {
                    continue;
                }

                var peripheralEntries = GetDisplayPeripheralEntriesForOption(profileGroupsRoot, group, option).ToList();
                foreach (var entry in peripheralEntries)
                {
                    if (!TryParseId(entry, out var peripheralId))
                    {
                        continue;
                    }

                    var peripheralName = peripheralLookup.TryGetValue(peripheralId, out var resolvedName)
                        ? resolvedName
                        : peripheralId.ToString(CultureInfo.InvariantCulture);

                    if (!TryFindPeripheralStatElement(profileGroupsRoot, peripheralName, out var peripheralProfile))
                    {
                        continue;
                    }

                    PopulatePeripheralStatsFromElement(peripheralProfile, peripheralName);
                    return;
                }
            }
        }
    }

    private static bool TryFindPeripheralStatElement(
        JsonElement profileGroupsRoot,
        string peripheralName,
        out JsonElement profile)
    {
        profile = default;
        var expected = NormalizeComparisonToken(peripheralName);
        if (string.IsNullOrWhiteSpace(expected) || profileGroupsRoot.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        foreach (var group in profileGroupsRoot.EnumerateArray())
        {
            var groupIsc = group.TryGetProperty("isc", out var groupIscElement) && groupIscElement.ValueKind == JsonValueKind.String
                ? groupIscElement.GetString() ?? string.Empty
                : string.Empty;
            var normalizedGroupIsc = NormalizeComparisonToken(groupIsc);

            if (group.TryGetProperty("profiles", out var profilesElement) && profilesElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var candidate in profilesElement.EnumerateArray())
                {
                    var profileName = candidate.TryGetProperty("name", out var nameElement) && nameElement.ValueKind == JsonValueKind.String
                        ? nameElement.GetString() ?? string.Empty
                        : string.Empty;
                    var normalizedProfileName = NormalizeComparisonToken(profileName);
                    if (normalizedProfileName == expected || normalizedGroupIsc == expected)
                    {
                        profile = candidate;
                        return true;
                    }
                }
            }

            if (group.TryGetProperty("options", out var optionsElement) && optionsElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var candidateOption in optionsElement.EnumerateArray())
                {
                    var optionName = candidateOption.TryGetProperty("name", out var optionNameElement) && optionNameElement.ValueKind == JsonValueKind.String
                        ? optionNameElement.GetString() ?? string.Empty
                        : string.Empty;
                    var normalizedOptionName = NormalizeComparisonToken(optionName);
                    if (normalizedOptionName == expected)
                    {
                        if (group.TryGetProperty("profiles", out var optionMatchedProfiles) &&
                            optionMatchedProfiles.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var optionMatchedProfile in optionMatchedProfiles.EnumerateArray())
                            {
                                if (HasStatFields(optionMatchedProfile))
                                {
                                    profile = optionMatchedProfile;
                                    return true;
                                }
                            }
                        }

                        if (HasStatFields(candidateOption))
                        {
                            profile = candidateOption;
                            return true;
                        }
                    }
                }
            }

            if (normalizedGroupIsc == expected &&
                group.TryGetProperty("profiles", out var groupProfilesElement) &&
                groupProfilesElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var fallbackProfile in groupProfilesElement.EnumerateArray())
                {
                    profile = fallbackProfile;
                    return true;
                }
            }
        }

        return false;
    }

    private static string NormalizeComparisonToken(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return Regex.Replace(value, @"[^a-z0-9]", string.Empty, RegexOptions.IgnoreCase).ToLowerInvariant();
    }

    private static IEnumerable<JsonElement> GetContainerEntries(JsonElement container, string propertyName)
    {
        return CompanySelectionSharedUtilities.GetContainerEntries(container, propertyName);
    }

    private static (bool HasRegular, bool HasIrregular, bool HasImpetuous, bool HasTacticalAwareness) ParseUnitOrderTraits(JsonElement profileGroupsArray)
    {
        var hasRegular = false;
        var hasIrregular = false;
        var hasImpetuous = false;
        var optionsSeen = 0;
        var tacticalOptions = 0;

        if (profileGroupsArray.ValueKind != JsonValueKind.Array)
        {
            return (false, false, false, false);
        }

        foreach (var group in profileGroupsArray.EnumerateArray())
        {
            if (!group.TryGetProperty("options", out var optionsElement) || optionsElement.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var option in optionsElement.EnumerateArray())
            {
                optionsSeen++;
                var optionHasTactical = false;
                if (!option.TryGetProperty("orders", out var ordersElement) || ordersElement.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var order in ordersElement.EnumerateArray())
                {
                    if (!order.TryGetProperty("type", out var typeElement) || typeElement.ValueKind != JsonValueKind.String)
                    {
                        continue;
                    }

                    var type = typeElement.GetString();
                    if (string.IsNullOrWhiteSpace(type))
                    {
                        continue;
                    }

                    if (string.Equals(type, "REGULAR", StringComparison.OrdinalIgnoreCase))
                    {
                        hasRegular = true;
                    }
                    else if (string.Equals(type, "IRREGULAR", StringComparison.OrdinalIgnoreCase))
                    {
                        hasIrregular = true;
                    }
                    else if (string.Equals(type, "IMPETUOUS", StringComparison.OrdinalIgnoreCase))
                    {
                        hasImpetuous = true;
                    }
                    else if (string.Equals(type, "TACTICAL", StringComparison.OrdinalIgnoreCase))
                    {
                        optionHasTactical = true;
                    }
                }

                if (optionHasTactical)
                {
                    tacticalOptions++;
                }
            }
        }

        var hasUnitWideTacticalAwareness = optionsSeen > 0 && tacticalOptions == optionsSeen;
        return (hasRegular, hasIrregular, hasImpetuous, hasUnitWideTacticalAwareness);
    }

    private static bool HasTacticalAwarenessOrder(JsonElement option)
    {
        if (!option.TryGetProperty("orders", out var ordersElement) || ordersElement.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        foreach (var order in ordersElement.EnumerateArray())
        {
            if (!order.TryGetProperty("type", out var typeElement) || typeElement.ValueKind != JsonValueKind.String)
            {
                continue;
            }

            if (string.Equals(typeElement.GetString(), "TACTICAL", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static string ReadIntAsString(JsonElement option, string propertyName)
    {
        return CompanySelectionSharedUtilities.ReadIntAsString(option, propertyName);
    }

    private static string ReadNumericString(JsonElement element)
    {
        return CompanySelectionSharedUtilities.ReadNumericString(element);
    }

    private static string ReadMove(JsonElement option)
    {
        return CompanySelectionSharedUtilities.ReadMove(option);
    }

    private static (string Header, string Value) ReadVitality(JsonElement option)
    {
        var str = ReadIntAsString(option, "str");
        if (str != "-")
        {
            return ("STR", str);
        }

        var w = ReadIntAsString(option, "w");
        if (w != "-")
        {
            return ("VITA", w);
        }

        return ("VITA", ReadIntAsString(option, "vita"));
    }

    private static string ReadAvaAsString(JsonElement option)
    {
        if (!TryGetPropertyFlexible(option, "ava", out var avaElement))
        {
            return "-";
        }

        int value;
        if (avaElement.ValueKind == JsonValueKind.Number && avaElement.TryGetInt32(out value))
        {
            return value switch
            {
                < 0 => "-",
                255 => "T",
                _ => value.ToString()
            };
        }

        if (avaElement.ValueKind == JsonValueKind.String && int.TryParse(avaElement.GetString(), out value))
        {
            return value switch
            {
                < 0 => "-",
                255 => "T",
                _ => value.ToString()
            };
        }

        return "-";
    }

    private static bool TryGetPropertyFlexible(JsonElement element, string propertyName, out JsonElement value)
    {
        return CompanySelectionSharedUtilities.TryGetPropertyFlexible(element, propertyName, out value);
    }

    private Task ApplyGlobalDisplayUnitsPreferenceAsync(CancellationToken cancellationToken = default)
    {
        if (_appSettingsProvider is null)
        {
            return Task.CompletedTask;
        }

        try
        {
            var showInches = GetShowUnitsInInchesFromProvider(cancellationToken);
            if (ShowUnitsInInches == showInches)
            {
                return Task.CompletedTask;
            }

            ShowUnitsInInches = showInches;
            UpdateUnitMoveDisplay();
            UpdatePeripheralMoveDisplay();
            RefreshMercsCompanyEntryDistanceDisplays();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ArmyFactionSelectionPage ApplyGlobalDisplayUnitsPreferenceAsync failed: {ex.Message}");
        }

        return Task.CompletedTask;
    }

    private static List<string> ComputeCommonNamesFromProfiles(
        string? profileGroupsJson,
        string propertyName,
        IReadOnlyDictionary<int, string> lookup)
    {
        if (string.IsNullOrWhiteSpace(profileGroupsJson))
        {
            return [];
        }

        HashSet<string>? common = null;
        try
        {
            using var doc = JsonDocument.Parse(profileGroupsJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            foreach (var group in doc.RootElement.EnumerateArray())
            {
                if (!group.TryGetProperty("profiles", out var profilesElement) || profilesElement.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var profile in profilesElement.EnumerateArray())
                {
                    var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    if (profile.TryGetProperty(propertyName, out var arr) && arr.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var entry in arr.EnumerateArray())
                        {
                            if (TryParseId(entry, out var id) && lookup.TryGetValue(id, out var name) && !string.IsNullOrWhiteSpace(name))
                            {
                                set.Add(name);
                            }
                        }
                    }

                    if (common is null)
                    {
                        common = set;
                    }
                    else
                    {
                        common.IntersectWith(set);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ArmyFactionSelectionPage ComputeCommonNamesFromProfiles failed for '{propertyName}': {ex.Message}");
            return [];
        }

        if (common is null || common.Count == 0)
        {
            return [];
        }

        return common.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static (bool HasCube, bool HasCube2, bool HasHackable) ParseUnitTechTraits(
        JsonElement profileGroupsArray,
        IReadOnlyDictionary<int, string> equipLookup,
        IReadOnlyDictionary<int, string> skillsLookup,
        IReadOnlyDictionary<int, string> charsLookup)
    {
        var hasCube = false;
        var hasCube2 = false;
        var hasHackable = false;

        if (profileGroupsArray.ValueKind != JsonValueKind.Array)
        {
            return (false, false, false);
        }

        foreach (var group in profileGroupsArray.EnumerateArray())
        {
            if (group.TryGetProperty("profiles", out var profilesElement) && profilesElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var profile in profilesElement.EnumerateArray())
                {
                    ApplyTechTraitsFromContainer(profile, "equip", equipLookup, ref hasCube, ref hasCube2, ref hasHackable);
                    ApplyTechTraitsFromContainer(profile, "skills", skillsLookup, ref hasCube, ref hasCube2, ref hasHackable);
                    ApplyTechTraitsFromContainer(profile, "chars", charsLookup, ref hasCube, ref hasCube2, ref hasHackable);
                }
            }

            if (group.TryGetProperty("options", out var optionsElement) && optionsElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var option in optionsElement.EnumerateArray())
                {
                    ApplyTechTraitsFromContainer(option, "equip", equipLookup, ref hasCube, ref hasCube2, ref hasHackable);
                    ApplyTechTraitsFromContainer(option, "skills", skillsLookup, ref hasCube, ref hasCube2, ref hasHackable);
                    ApplyTechTraitsFromContainer(option, "chars", charsLookup, ref hasCube, ref hasCube2, ref hasHackable);
                }
            }
        }

        return (hasCube, hasCube2, hasHackable);
    }

    private static void ApplyTechTraitsFromContainer(
        JsonElement container,
        string propertyName,
        IReadOnlyDictionary<int, string> lookup,
        ref bool hasCube,
        ref bool hasCube2,
        ref bool hasHackable)
    {
        if (!container.TryGetProperty(propertyName, out var arr) || arr.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var entry in arr.EnumerateArray())
        {
            if (!TryParseId(entry, out var id) || !lookup.TryGetValue(id, out var name))
            {
                continue;
            }

            ApplyTechTraitName(name, ref hasCube, ref hasCube2, ref hasHackable);
        }
    }

    private static void ApplyTechTraitName(string name, ref bool hasCube, ref bool hasCube2, ref bool hasHackable)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        var normalized = NormalizeTokenText(name);

        if (Regex.IsMatch(normalized, @"\bhackable\b", RegexOptions.IgnoreCase) &&
            !Regex.IsMatch(normalized, @"\b(non[\s-]*hackable|not[\s-]*hackable)\b", RegexOptions.IgnoreCase))
        {
            hasHackable = true;
        }

        var hasNegativeCube = Regex.IsMatch(
            normalized,
            @"\b(no[\s-]*cube|without[\s-]*cube|cube[\s-]*none)\b",
            RegexOptions.IgnoreCase);

        if (hasNegativeCube)
        {
            return;
        }

        var isCube2 = Regex.IsMatch(
            normalized,
            @"\bcube\s*2(?:\.0)?\b|\bcube2(?:\.0)?\b",
            RegexOptions.IgnoreCase);

        if (isCube2)
        {
            hasCube2 = true;
            return;
        }

        if (Regex.IsMatch(normalized, @"\bcube\b", RegexOptions.IgnoreCase))
        {
            hasCube = true;
        }
    }

    private static string NormalizeTokenText(string value)
    {
        var lowered = value.ToLowerInvariant();
        var sb = new StringBuilder(lowered.Length);
        foreach (var c in lowered)
        {
            if (char.IsLetterOrDigit(c) || c == '.')
            {
                sb.Append(c);
            }
            else
            {
                sb.Append(' ');
            }
        }

        return Regex.Replace(sb.ToString(), @"\s+", " ").Trim();
    }

    private static FormattedString BuildNamedSummaryFormatted(string label, IEnumerable<string> values, Color accentColor, bool highlightLieutenantPurple = false)
    {
        return CompanyProfileTextService.BuildNamedSummaryFormatted(label, values, accentColor, highlightLieutenantPurple);
    }

    private static FormattedString BuildMercsCompanyLineFormatted(string label, string? value, Color accentColor)
    {
        return CompanyProfileTextService.BuildMercsCompanyLineFormatted(label, value, accentColor);
    }

    private static string BuildPeripheralSubtitle(PeripheralMercsCompanyStats? peripheralStats)
    {
        if (peripheralStats is null)
        {
            return "-";
        }

        return $"MOV {peripheralStats.Mov} | CC {peripheralStats.Cc} | BS {peripheralStats.Bs} | PH {peripheralStats.Ph} | WIP {peripheralStats.Wip} | ARM {peripheralStats.Arm} | BTS {peripheralStats.Bts} | {peripheralStats.VitalityHeader} {peripheralStats.Vitality} | S {peripheralStats.S} | AVA {peripheralStats.Ava}";
    }

    private static List<string> SplitDisplayLine(string? text)
    {
        return CompanyProfileTextService.SplitDisplayLine(text);
    }

    private static List<string> IntersectNamedIds(
        IReadOnlyList<JsonElement> options,
        string propertyName,
        IReadOnlyDictionary<int, string> lookup)
    {
        HashSet<int>? intersection = null;
        foreach (var option in options)
        {
            var ids = new HashSet<int>();
            if (option.TryGetProperty(propertyName, out var arr) && arr.ValueKind == JsonValueKind.Array)
            {
                foreach (var entry in arr.EnumerateArray())
                {
                    if (TryParseId(entry, out var id))
                    {
                        ids.Add(id);
                    }
                }
            }

            if (intersection is null)
            {
                intersection = ids;
            }
            else
            {
                intersection.IntersectWith(ids);
            }
        }

        if (intersection is null || intersection.Count == 0)
        {
            return [];
        }

        return intersection
            .Where(lookup.ContainsKey)
            .Select(id => lookup[id])
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static List<string> IntersectNamedIdsWithIncludes(
        JsonElement profileGroupsRoot,
        IReadOnlyList<JsonElement> options,
        string propertyName,
        IReadOnlyDictionary<int, string> lookup)
    {
        HashSet<int>? intersection = null;
        foreach (var option in options)
        {
            var ids = new HashSet<int>();
            foreach (var entry in GetOptionEntriesWithIncludes(profileGroupsRoot, option, propertyName))
            {
                if (TryParseId(entry, out var id))
                {
                    ids.Add(id);
                }
            }

            if (intersection is null)
            {
                intersection = ids;
            }
            else
            {
                intersection.IntersectWith(ids);
            }
        }

        if (intersection is null || intersection.Count == 0)
        {
            return [];
        }

        return intersection
            .Where(lookup.ContainsKey)
            .Select(id => lookup[id])
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static IEnumerable<JsonElement> GetOptionEntriesWithIncludes(
        JsonElement profileGroupsRoot,
        JsonElement option,
        string propertyName)
    {
        return CompanyProfileOptionService.GetOptionEntriesWithIncludes(profileGroupsRoot, option, propertyName);
    }

    private static string ConvertDistanceText(string distanceText, bool showUnitsInInches)
    {
        return CompanySelectionSharedUtilities.ConvertDistanceText(distanceText, showUnitsInInches);
    }

    private void OnTeamAllowedProfileSelected(ArmyTeamUnitLimitItem? teamItem)
    {
        if (teamItem is null)
        {
            Console.Error.WriteLine("ArmyFactionSelectionPage OnTeamAllowedProfileTappedFromView: no team item binding context.");
            return;
        }

        ArmyUnitSelectionItem? resolved = null;
        if (teamItem.ResolvedUnitId.HasValue && teamItem.ResolvedSourceFactionId.HasValue)
        {
            resolved = Units.FirstOrDefault(x =>
                x.Id == teamItem.ResolvedUnitId.Value &&
                x.SourceFactionId == teamItem.ResolvedSourceFactionId.Value);
        }

        resolved ??= Units.FirstOrDefault(x =>
            !string.IsNullOrWhiteSpace(teamItem.Slug) &&
            !string.IsNullOrWhiteSpace(x.Slug) &&
            string.Equals(x.Slug.Trim(), teamItem.Slug.Trim(), StringComparison.OrdinalIgnoreCase));

        resolved ??= Units.FirstOrDefault(x =>
            string.Equals(x.Name, teamItem.Name, StringComparison.OrdinalIgnoreCase));

        if (resolved is null)
        {
            Console.Error.WriteLine(
                $"ArmyFactionSelectionPage OnTeamAllowedProfileTappedFromView: unable to resolve unit for team entry '{teamItem.Name}'.");
            return;
        }

        SetSelectedUnit(resolved, restrictProfilesToFto: IsFtoLabel(teamItem.Name));
    }

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

    private void OnUnitSelectionFilterCanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        DrawSlotPicture(_filterIconPicture, e);
    }

    /// <summary>
    /// Renders the peripheral icon in mercs-company entry rows.
    /// </summary>
    private void OnPeripheralIconCanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        DrawSlotPicture(UnitDisplayConfigurationsView.PeripheralIconPicture, e);
    }

    private void OnTrackedFireteamLevelCanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        DrawSlotPicture(_trackedFireteamLevelPicture, e);
    }

    private static void ApplyFilterButtonSize(Border? buttonBorder, SKCanvasView? iconCanvas, double iconButtonSize)
    {
        CompanySelectionSharedUtilities.ApplyFilterButtonSize(buttonBorder, iconCanvas, iconButtonSize);
    }

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

    private void ApplyUnitHeaderColorsByVanillaFactionName(string? vanillaFactionName)
    {
        var (primary, secondary) = GetFactionTheme(vanillaFactionName);
        UnitHeaderPrimaryColor = primary;
        UnitHeaderSecondaryColor = secondary;
        UnitHeaderPrimaryTextColor = IsLightColor(primary) ? Colors.Black : Colors.White;
        UnitHeaderSecondaryTextColor = IsLightColor(secondary) ? Colors.Black : Colors.White;
        RefreshSummaryFormatted();
    }

    private void RefreshSummaryFormatted()
    {
        var (equipmentAccent, skillsAccent) = GetSummaryAccentColorsForSecondaryBackground(UnitHeaderSecondaryColor);
        EquipmentSummaryFormatted = BuildNamedSummaryFormatted("Equipment", UnitDisplayConfigurationsView.SelectedUnitCommonEquipment, equipmentAccent);
        SpecialSkillsSummaryFormatted = BuildNamedSummaryFormatted(
            "Special Skills",
            UnitDisplayConfigurationsView.SelectedUnitCommonSkills,
            skillsAccent,
            highlightLieutenantPurple: _summaryHighlightLieutenant);
    }

    private static (Color EquipmentAccent, Color SkillsAccent) GetSummaryAccentColorsForSecondaryBackground(Color secondaryBackground)
    {
        return IsLightColor(secondaryBackground)
            ? (UnitDisplayConfigurationsView.EquipmentAccentOnLightSecondary, UnitDisplayConfigurationsView.SkillsAccentOnLightSecondary)
            : (UnitDisplayConfigurationsView.EquipmentAccentOnDarkSecondary, UnitDisplayConfigurationsView.SkillsAccentOnDarkSecondary);
    }

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

    private static bool IsThemeFactionName(string? factionName)
    {
        return CompanySelectionSharedUtilities.IsThemeFactionName(factionName);
    }

    private static IReadOnlyList<int> ParseFactionIds(string? factionsJson)
    {
        return CompanySelectionSharedUtilities.ParseFactionIds(factionsJson);
    }

    private static string NormalizeFactionName(string? value)
    {
        return CompanySelectionSharedUtilities.NormalizeFactionName(value);
    }

    private static bool IsLightColor(Color color)
    {
        return CompanySelectionSharedUtilities.IsLightColor(color);
    }

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

    private static void DrawPictureInRect(SKCanvas canvas, SKPicture picture, SKRect destination)
    {
        CompanySelectionSharedUtilities.DrawPictureInRect(canvas, picture, destination);
    }

    private static void DrawSlotPicture(SKPicture? picture, SKPaintSurfaceEventArgs e)
    {
        CompanySelectionSharedUtilities.DrawSlotPicture(picture, e);
    }

    private static void DrawSlotBorder(SKPaintSurfaceEventArgs e, SKColor borderColor)
    {
        CompanySelectionSharedUtilities.DrawSlotBorder(e, borderColor);
    }

    private ArmyFactionRecord? GetFactionSnapshotFromProvider(int factionId, CancellationToken cancellationToken = default)
    {
        return _armyDataService.GetFactionSnapshot(factionId, cancellationToken);
    }

    private IReadOnlyList<ArmyResumeRecord> GetResumeByFactionMercsOnlyFromProvider(int factionId, CancellationToken cancellationToken = default)
    {
        return _armyDataService.GetResumeByFactionMercsOnly(factionId, cancellationToken);
    }

    private ArmyUnitRecord? GetUnitFromProvider(int factionId, int unitId, CancellationToken cancellationToken = default)
    {
        return _armyDataService.GetUnit(factionId, unitId, cancellationToken);
    }

    private async Task<IReadOnlyList<MercsArmyListEntry>> GetMergedMercsArmyListFromQueryAccessorAsync(
        IReadOnlyCollection<int> factionIds,
        CancellationToken cancellationToken = default)
    {
        return await _armyDataService.GetMergedMercsArmyListAsync(factionIds, cancellationToken);
    }

    private bool GetShowUnitsInInchesFromProvider(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return _appSettingsProvider?.GetShowUnitsInInches() ?? false;
    }

}

public class ArmyFactionSelectionItem : BaseViewModel, IViewerListItem
{
    public int Id { get; init; }

    public int ParentId { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? CachedLogoPath { get; init; }

    public string? PackagedLogoPath { get; init; }

    public string? Subtitle => null;

    public bool HasSubtitle => false;

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value)
            {
                return;
            }

            _isSelected = value;
            OnPropertyChanged();
        }
    }
}

public class ArmyUnitSelectionItem : BaseViewModel, IViewerListItem
{
    public int Id { get; init; }
    public int SourceFactionId { get; init; }
    public string? Slug { get; init; }

    public int? Type { get; init; }
    public bool IsCharacter { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? CachedLogoPath { get; init; }

    public string? PackagedLogoPath { get; init; }

    public string? Subtitle { get; init; }
    public bool IsSpecOps { get; init; }

    public bool HasSubtitle => !string.IsNullOrWhiteSpace(Subtitle);

    private bool _isVisible = true;
    public bool IsVisible
    {
        get => _isVisible;
        set
        {
            if (_isVisible == value)
            {
                return;
            }

            _isVisible = value;
            OnPropertyChanged();
        }
    }

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value)
            {
                return;
            }

            _isSelected = value;
            OnPropertyChanged();
        }
    }
}

public class ArmyTeamListItem : BaseViewModel
{
    public string Name { get; init; } = string.Empty;
    public string TeamCountsText { get; init; } = string.Empty;
    public bool HasTeamCounts => !string.IsNullOrWhiteSpace(TeamCountsText);
    public bool IsWildcardBucket { get; init; }
    public bool ShowTrackingRadioButton => !IsWildcardBucket;
    public ObservableCollection<ArmyTeamUnitLimitItem> AllowedProfiles { get; init; } = [];

    private bool _isVisible = true;
    public bool IsVisible
    {
        get => _isVisible;
        set
        {
            if (_isVisible == value)
            {
                return;
            }

            _isVisible = value;
            OnPropertyChanged();
        }
    }

    private bool _isExpanded;
    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (_isExpanded == value)
            {
                return;
            }

            _isExpanded = value;
            OnPropertyChanged();
        }
    }

    private bool _isTrackedTeam;
    public bool IsTrackedTeam
    {
        get => _isTrackedTeam;
        set
        {
            if (_isTrackedTeam == value)
            {
                return;
            }

            _isTrackedTeam = value;
            OnPropertyChanged();
        }
    }
}

public class ArmyTeamUnitLimitItem : BaseViewModel, IViewerListItem
{
    public string Name { get; init; } = string.Empty;
    public string Min { get; init; } = "0";
    public string Max { get; init; } = "0";
    public string? Slug { get; init; }
    public bool IsCharacter { get; init; }
    public int? ResolvedUnitId { get; init; }
    public int? ResolvedSourceFactionId { get; init; }
    public string? CachedLogoPath { get; init; }
    public string? PackagedLogoPath { get; init; }
    public string? Subtitle { get; init; }
    public bool HasSubtitle => !string.IsNullOrWhiteSpace(Subtitle);

    private bool _isVisible = true;
    public bool IsVisible
    {
        get => _isVisible;
        set
        {
            if (_isVisible == value)
            {
                return;
            }

            _isVisible = value;
            OnPropertyChanged();
        }
    }

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value)
            {
                return;
            }

            _isSelected = value;
            OnPropertyChanged();
        }
    }
}

public class MercsCompanyEntry : BaseViewModel, IViewerListItem
{
    public string Name { get; init; } = string.Empty;
    public string BaseUnitName { get; init; } = string.Empty;
    public FormattedString? NameFormatted { get; init; }
    public string CostDisplay { get; init; } = string.Empty;
    public int CostValue { get; init; }
    public string ProfileKey { get; init; } = string.Empty;
    public bool IsLieutenant { get; init; }
    public int SourceUnitId { get; init; }
    public int SourceFactionId { get; init; }

    public string? CachedLogoPath { get; init; }

    public string? PackagedLogoPath { get; init; }

    private string? _subtitle;
    public string? Subtitle
    {
        get => _subtitle;
        set
        {
            if (_subtitle == value)
            {
                return;
            }

            _subtitle = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasSubtitle));
        }
    }
    public string UnitTypeCode { get; init; } = string.Empty;
    public string SavedEquipment { get; init; } = "-";
    public string SavedSkills { get; init; } = "-";
    public string SavedRangedWeapons { get; init; } = "-";
    public string SavedCcWeapons { get; init; } = "-";
    public int? UnitMoveFirstCm { get; init; }
    public int? UnitMoveSecondCm { get; init; }
    public string UnitMoveDisplay { get; set; } = "-";
    public bool HasPeripheralStatBlock { get; init; }
    public string PeripheralNameHeading { get; init; } = string.Empty;
    private string _peripheralMov = "-";
    public string PeripheralMov
    {
        get => _peripheralMov;
        set
        {
            if (_peripheralMov == value)
            {
                return;
            }

            _peripheralMov = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(PeripheralSubtitle));
        }
    }
    public string PeripheralCc { get; init; } = "-";
    public string PeripheralBs { get; init; } = "-";
    public string PeripheralPh { get; init; } = "-";
    public string PeripheralWip { get; init; } = "-";
    public string PeripheralArm { get; init; } = "-";
    public string PeripheralBts { get; init; } = "-";
    public string PeripheralVitalityHeader { get; init; } = "VITA";
    public string PeripheralVitality { get; init; } = "-";
    public string PeripheralS { get; init; } = "-";
    public string PeripheralAva { get; init; } = "-";
    public int? PeripheralMoveFirstCm { get; init; }
    public int? PeripheralMoveSecondCm { get; init; }
    public string SavedPeripheralEquipment { get; init; } = "-";
    public string SavedPeripheralSkills { get; init; } = "-";
    public string PeripheralSubtitle => $"MOV {PeripheralMov} | CC {PeripheralCc} | BS {PeripheralBs} | PH {PeripheralPh} | WIP {PeripheralWip} | ARM {PeripheralArm} | BTS {PeripheralBts} | {PeripheralVitalityHeader} {PeripheralVitality} | S {PeripheralS} | AVA {PeripheralAva}";

    public bool HasSubtitle => !string.IsNullOrWhiteSpace(Subtitle);

    public FormattedString EquipmentLineFormatted { get; init; } = new();
    public bool HasEquipmentLine { get; init; }
    public FormattedString SkillsLineFormatted { get; init; } = new();
    public bool HasSkillsLine { get; init; }
    public FormattedString RangedLineFormatted { get; init; } = new();
    public FormattedString CcLineFormatted { get; init; } = new();
    public FormattedString PeripheralEquipmentLineFormatted { get; init; } = new();
    public bool HasPeripheralEquipmentLine { get; init; }
    public FormattedString PeripheralSkillsLineFormatted { get; init; } = new();
    public bool HasPeripheralSkillsLine { get; init; }
    private int _experiencePoints;
    public int ExperiencePoints
    {
        get => _experiencePoints;
        set
        {
            var normalized = Math.Max(0, value);
            if (_experiencePoints == normalized)
            {
                return;
            }

            _experiencePoints = normalized;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ExperienceRankName));
        }
    }

    public string ExperienceRankName => UnitExperienceRanks.GetRankName(ExperiencePoints);

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value)
            {
                return;
            }

            _isSelected = value;
            OnPropertyChanged();
        }
    }
}

public sealed class SavedCompanyFile
{
    public string CompanyName { get; init; } = string.Empty;
    public string CompanyType { get; init; } = string.Empty;
    public string CompanyIdentifier { get; init; } = string.Empty;
    public int CompanyIndex { get; init; }
    public string CreatedUtc { get; init; } = string.Empty;
    public int PointsLimit { get; init; }
    public int CurrentPoints { get; init; }
    public SavedImprovedCaptainStats ImprovedCaptainStats { get; init; } = new();
    public List<SavedCompanyFaction> SourceFactions { get; init; } = [];
    public List<SavedCompanyEntry> Entries { get; init; } = [];
}

public sealed class SavedImprovedCaptainStats
{
    public bool IsEnabled { get; init; }
    public string CaptainName { get; init; } = string.Empty;
    public int CcTier { get; init; }
    public int BsTier { get; init; }
    public int PhTier { get; init; }
    public int WipTier { get; init; }
    public int ArmTier { get; init; }
    public int BtsTier { get; init; }
    public int VitalityTier { get; init; }
    public int CcBonus { get; init; }
    public int BsBonus { get; init; }
    public int PhBonus { get; init; }
    public int WipBonus { get; init; }
    public int ArmBonus { get; init; }
    public int BtsBonus { get; init; }
    public int VitalityBonus { get; init; }
    public string WeaponChoice1 { get; init; } = string.Empty;
    public string WeaponChoice2 { get; init; } = string.Empty;
    public string WeaponChoice3 { get; init; } = string.Empty;
    public string SkillChoice1 { get; init; } = string.Empty;
    public string SkillChoice2 { get; init; } = string.Empty;
    public string SkillChoice3 { get; init; } = string.Empty;
    public string EquipmentChoice1 { get; init; } = string.Empty;
    public string EquipmentChoice2 { get; init; } = string.Empty;
    public string EquipmentChoice3 { get; init; } = string.Empty;
    public int OptionFactionId { get; init; }
    public string OptionFactionName { get; init; } = string.Empty;
}

public sealed class SavedCompanyFaction
{
    public int FactionId { get; init; }
    public string FactionName { get; init; } = string.Empty;
}

public sealed class SavedCompanyEntry
{
    public int EntryIndex { get; init; }
    public string Name { get; init; } = string.Empty;
    public string BaseUnitName { get; set; } = string.Empty;
    public string CustomName { get; set; } = string.Empty;
    public string UnitTypeCode { get; set; } = string.Empty;
    public string ProfileKey { get; init; } = string.Empty;
    public int SourceFactionId { get; init; }
    public int SourceUnitId { get; init; }
    public int Cost { get; init; }
    public bool IsLieutenant { get; init; }
    public string SavedEquipment { get; init; } = "-";
    public string SavedSkills { get; init; } = "-";
    public string SavedRangedWeapons { get; init; } = "-";
    public string SavedCcWeapons { get; init; } = "-";
    public bool HasPeripheralStatBlock { get; init; }
    public string PeripheralNameHeading { get; init; } = string.Empty;
    public string PeripheralMov { get; init; } = "-";
    public string PeripheralCc { get; init; } = "-";
    public string PeripheralBs { get; init; } = "-";
    public string PeripheralPh { get; init; } = "-";
    public string PeripheralWip { get; init; } = "-";
    public string PeripheralArm { get; init; } = "-";
    public string PeripheralBts { get; init; } = "-";
    public string PeripheralVitalityHeader { get; init; } = "VITA";
    public string PeripheralVitality { get; init; } = "-";
    public string PeripheralS { get; init; } = "-";
    public string PeripheralAva { get; init; } = "-";
    public string SavedPeripheralEquipment { get; init; } = "-";
    public string SavedPeripheralSkills { get; init; } = "-";
    public int ExperiencePoints { get; init; }
    public string ExperienceRankName => UnitExperienceRanks.GetRankName(ExperiencePoints);
}

sealed class PeripheralMercsCompanyStats
{
    public string NameHeading { get; init; } = string.Empty;
    public int? MoveFirstCm { get; init; }
    public int? MoveSecondCm { get; init; }
    public string Mov { get; init; } = "-";
    public string Cc { get; init; } = "-";
    public string Bs { get; init; } = "-";
    public string Ph { get; init; } = "-";
    public string Wip { get; init; } = "-";
    public string Arm { get; init; } = "-";
    public string Bts { get; init; } = "-";
    public string VitalityHeader { get; init; } = "VITA";
    public string Vitality { get; init; } = "-";
    public string S { get; init; } = "-";
    public string Ava { get; init; } = "-";
    public string Equipment { get; init; } = "-";
    public string Skills { get; init; } = "-";
}

public sealed class CaptainUpgradeOptionSet
{
    public static CaptainUpgradeOptionSet Empty { get; } = new();
    public List<string> Weapons { get; init; } = [];
    public List<string> Skills { get; init; } = [];
    public List<string> Equipment { get; init; } = [];
    public bool IsEmpty => Weapons.Count == 0 && Skills.Count == 0 && Equipment.Count == 0;
}

public static class UnitExperienceRanks
{
    private static readonly (int MinXp, string RankName)[] OrderedRanks =
    [
        (5, "Newbie"),
        (10, "Hired Gun"),
        (30, "Hitman"),
        (50, "Operative"),
        (75, "Veteran"),
        (105, "Officer"),
        (140, "Legend")
    ];

    public static string GetRankName(int experiencePoints)
    {
        var normalized = Math.Max(0, experiencePoints);
        for (var i = OrderedRanks.Length - 1; i >= 0; i--)
        {
            if (normalized >= OrderedRanks[i].MinXp)
            {
                return OrderedRanks[i].RankName;
            }
        }

        return "Unranked";
    }

    public static int GetRankLevel(int experiencePoints)
    {
        var normalized = Math.Max(0, experiencePoints);
        for (var i = OrderedRanks.Length - 1; i >= 0; i--)
        {
            if (normalized >= OrderedRanks[i].MinXp)
            {
                return i + 1;
            }
        }

        return 0;
    }
}

















