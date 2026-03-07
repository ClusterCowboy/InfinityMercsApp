using System.Collections.ObjectModel;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows.Input;
using InfinityMercsApp.Data.Database;
using InfinityMercsApp.Services;
using InfinityMercsApp.ViewModels;
using InfinityMercsApp.Views.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Devices;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using Svg.Skia;

namespace InfinityMercsApp.Views;

public partial class ArmyFactionSelectionPage : ContentPage
{
    private readonly record struct ExtraDefinition(string Name, string Type);
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

    private const int MaxHeaderIcons = 6;
    private const float IconSize = 24f;
    private const float IconVerticalGap = 5f;
    private const double UnitNameHeadingMaxFontSize = 24d;
    private const double UnitNameHeadingMinFontSize = 11d;
    private const double UnitNameHeadingFontStep = 0.5d;
    private static readonly Color DefaultHeaderPrimaryColor = Color.FromArgb("#B91C1C");
    private static readonly Color DefaultHeaderSecondaryColor = Color.FromArgb("#7F1D1D");
    private static readonly Color EquipmentAccentOnDarkSecondary = Color.FromArgb("#67E8F9");
    private static readonly Color SkillsAccentOnDarkSecondary = Color.FromArgb("#FDE68A");
    private static readonly Color EquipmentAccentOnLightSecondary = Color.FromArgb("#0B5563");
    private static readonly Color SkillsAccentOnLightSecondary = Color.FromArgb("#7C2D12");
    private static readonly Color ActiveBorder = Color.FromArgb("#2563EB");
    private static readonly Color InactiveBorder = Color.FromArgb("#9CA3AF");
    private static readonly Dictionary<int, int> UnitTypeSortOrder = new()
    {
        [1] = 0, // LI
        [2] = 1, // MI
        [3] = 2, // HI
        [4] = 3, // TAG
        [5] = 4, // REM
        [6] = 5, // SK
        [7] = 6  // WB
    };

    private readonly ArmySourceSelectionMode _mode;
    private readonly IMetadataAccessor? _metadataAccessor;
    private readonly IArmyDataAccessor? _armyDataAccessor;
    private readonly FactionLogoCacheService? _factionLogoCacheService;
    private readonly AppSettingsService? _appSettingsService;
    private ArmyFactionSelectionItem? _selectedFaction;
    private ArmyFactionSelectionItem? _leftSlotFaction;
    private ArmyFactionSelectionItem? _rightSlotFaction;
    private SKPicture? _leftSlotPicture;
    private SKPicture? _rightSlotPicture;
    private SKPicture? _selectedUnitPicture;
    private SKPicture? _regularOrderIconPicture;
    private SKPicture? _irregularOrderIconPicture;
    private SKPicture? _impetuousIconPicture;
    private SKPicture? _tacticalAwarenessIconPicture;
    private SKPicture? _cubeIconPicture;
    private SKPicture? _cube2IconPicture;
    private SKPicture? _hackableIconPicture;
    private SKPicture? _peripheralIconPicture;
    private SKPicture? _filterIconPicture;
    private SKPicture? _seasonCheckIconPicture;
    private SKPicture? _seasonXIconPicture;
    private bool _showSeasonCheckIcon;
    private bool _isCompanyValid;
    private string _companyName = "Company Name";
    private readonly Command _startCompanyCommand;
    private bool _showCompanyNameValidationError;
    private Color _companyNameBorderColor = Color.FromArgb("#6B7280");
    private int _activeSlotIndex;
    private bool _loaded;
    private bool _lieutenantOnlyUnits;
    private bool _teamsView;
    private bool _isFactionSelectionActive = true;
    private string _pageHeading = string.Empty;
    private string _selectedStartSeasonPoints = "75";
    private string _seasonPointsCapText = "0";
    private ArmyUnitSelectionItem? _selectedUnit;
    private string _leftSlotText = string.Empty;
    private string _rightSlotText = string.Empty;
    private Color _leftSlotBorderColor = ActiveBorder;
    private Color _rightSlotBorderColor = InactiveBorder;
    private string _unitNameHeading = "Select a unit";
    private string _unitMov = "-";
    private string _unitCc = "-";
    private string _unitBs = "-";
    private string _unitPh = "-";
    private string _unitWip = "-";
    private string _unitArm = "-";
    private string _unitBts = "-";
    private string _unitVitalityHeader = "VITA";
    private string _unitVitality = "-";
    private string _unitS = "-";
    private string _unitAva = "-";
    private bool _hasPeripheralStatBlock;
    private string _peripheralNameHeading = string.Empty;
    private string _peripheralMov = "-";
    private string _peripheralCc = "-";
    private string _peripheralBs = "-";
    private string _peripheralPh = "-";
    private string _peripheralWip = "-";
    private string _peripheralArm = "-";
    private string _peripheralBts = "-";
    private string _peripheralVitalityHeader = "VITA";
    private string _peripheralVitality = "-";
    private string _peripheralS = "-";
    private string _peripheralAva = "-";
    private string _peripheralEquipment = "-";
    private string _peripheralSkills = "-";
    private double _unitNameHeadingFontSize = UnitNameHeadingMaxFontSize;
    private Color _unitHeaderPrimaryColor = DefaultHeaderPrimaryColor;
    private Color _unitHeaderSecondaryColor = DefaultHeaderSecondaryColor;
    private Color _unitHeaderPrimaryTextColor = Colors.White;
    private Color _unitHeaderSecondaryTextColor = Colors.White;
    private string _equipmentSummary = "Equipment: -";
    private string _specialSkillsSummary = "Special Skills: -";
    private string _profilesStatus = "Select a unit.";
    private FormattedString _equipmentSummaryFormatted = new();
    private FormattedString _specialSkillsSummaryFormatted = new();
    private FormattedString _peripheralEquipmentFormatted = new();
    private FormattedString _peripheralSkillsFormatted = new();
    private bool _showRegularOrderIcon;
    private bool _showIrregularOrderIcon;
    private bool _showImpetuousIcon;
    private bool _showTacticalAwarenessIcon;
    private bool _showCubeIcon;
    private bool _showCube2Icon;
    private bool _showHackableIcon;
    private bool _summaryHighlightLieutenant;
    private bool _showUnitsInInches = true;
    private int? _unitMoveFirstCm;
    private int? _unitMoveSecondCm;
    private int? _peripheralMoveFirstCm;
    private int? _peripheralMoveSecondCm;
    private string? _selectedUnitProfileGroupsJson;
    private string? _selectedUnitFiltersJson;
    private List<string> _selectedUnitCommonEquipment = [];
    private List<string> _selectedUnitCommonSkills = [];
    private UnitFilterCriteria _activeUnitFilter = UnitFilterCriteria.None;
    private UnitFilterPopupPage? _activeUnitFilterPopup;

    public ArmyFactionSelectionPage(ArmySourceSelectionMode mode)
    {
        InitializeComponent();
        _mode = mode;
        Title = _mode == ArmySourceSelectionMode.VanillaFactions
            ? "Choose your faction:"
            : "Choose your sectorials";
        PageHeading = _mode == ArmySourceSelectionMode.VanillaFactions
            ? "Choose your faction:"
            : "Choose your sectorials";

        var services = Application.Current?.Handler?.MauiContext?.Services;
        _metadataAccessor = services?.GetService<IMetadataAccessor>();
        _armyDataAccessor = services?.GetService<IArmyDataAccessor>();
        _factionLogoCacheService = services?.GetService<FactionLogoCacheService>();
        _appSettingsService = services?.GetService<AppSettingsService>();

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
        _startCompanyCommand = new Command(async () => await StartCompanyAsync(), () => IsCompanyValid);
        StartCompanyCommand = _startCompanyCommand;

        BindingContext = this;
        SetActiveSlot(0);
        RefreshSummaryFormatted();
        _ = LoadHeaderIconsAsync();
        _ = LoadSeasonValidationIconsAsync();
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
        get => _selectedStartSeasonPoints;
        set
        {
            if (_selectedStartSeasonPoints == value)
            {
                return;
            }

            _selectedStartSeasonPoints = value;
            OnPropertyChanged();
            UpdateSeasonValidationState();
            ApplyLieutenantVisualStates();
            _ = ApplyUnitVisibilityFiltersAsync();
        }
    }

    public string SeasonPointsCapText
    {
        get => _seasonPointsCapText;
        set
        {
            if (_seasonPointsCapText == value)
            {
                return;
            }

            _seasonPointsCapText = value;
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
        get => _isCompanyValid;
        private set
        {
            if (_isCompanyValid == value)
            {
                return;
            }

            _isCompanyValid = value;
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
        get => _unitNameHeading;
        private set
        {
            if (_unitNameHeading == value)
            {
                return;
            }

            _unitNameHeading = value;
            OnPropertyChanged();
            UpdateUnitNameHeadingFontSize();
        }
    }
    public double UnitNameHeadingFontSize
    {
        get => _unitNameHeadingFontSize;
        private set
        {
            if (Math.Abs(_unitNameHeadingFontSize - value) < 0.01d)
            {
                return;
            }

            _unitNameHeadingFontSize = value;
            OnPropertyChanged();
        }
    }
    public Color UnitHeaderPrimaryColor
    {
        get => _unitHeaderPrimaryColor;
        private set
        {
            if (_unitHeaderPrimaryColor == value)
            {
                return;
            }

            _unitHeaderPrimaryColor = value;
            OnPropertyChanged();
        }
    }
    public Color UnitHeaderSecondaryColor
    {
        get => _unitHeaderSecondaryColor;
        private set
        {
            if (_unitHeaderSecondaryColor == value)
            {
                return;
            }

            _unitHeaderSecondaryColor = value;
            OnPropertyChanged();
        }
    }
    public Color UnitHeaderPrimaryTextColor
    {
        get => _unitHeaderPrimaryTextColor;
        private set
        {
            if (_unitHeaderPrimaryTextColor == value)
            {
                return;
            }

            _unitHeaderPrimaryTextColor = value;
            OnPropertyChanged();
        }
    }
    public Color UnitHeaderSecondaryTextColor
    {
        get => _unitHeaderSecondaryTextColor;
        private set
        {
            if (_unitHeaderSecondaryTextColor == value)
            {
                return;
            }

            _unitHeaderSecondaryTextColor = value;
            OnPropertyChanged();
        }
    }
    public string UnitMov { get => _unitMov; private set { if (_unitMov != value) { _unitMov = value; OnPropertyChanged(); } } }
    public string UnitCc { get => _unitCc; private set { if (_unitCc != value) { _unitCc = value; OnPropertyChanged(); } } }
    public string UnitBs { get => _unitBs; private set { if (_unitBs != value) { _unitBs = value; OnPropertyChanged(); } } }
    public string UnitPh { get => _unitPh; private set { if (_unitPh != value) { _unitPh = value; OnPropertyChanged(); } } }
    public string UnitWip { get => _unitWip; private set { if (_unitWip != value) { _unitWip = value; OnPropertyChanged(); } } }
    public string UnitArm { get => _unitArm; private set { if (_unitArm != value) { _unitArm = value; OnPropertyChanged(); } } }
    public string UnitBts { get => _unitBts; private set { if (_unitBts != value) { _unitBts = value; OnPropertyChanged(); } } }
    public string UnitVitalityHeader { get => _unitVitalityHeader; private set { if (_unitVitalityHeader != value) { _unitVitalityHeader = value; OnPropertyChanged(); } } }
    public string UnitVitality { get => _unitVitality; private set { if (_unitVitality != value) { _unitVitality = value; OnPropertyChanged(); } } }
    public string UnitS { get => _unitS; private set { if (_unitS != value) { _unitS = value; OnPropertyChanged(); } } }
    public string UnitAva { get => _unitAva; private set { if (_unitAva != value) { _unitAva = value; OnPropertyChanged(); } } }
    public bool HasPeripheralStatBlock { get => _hasPeripheralStatBlock; private set { if (_hasPeripheralStatBlock != value) { _hasPeripheralStatBlock = value; OnPropertyChanged(); } } }
    public string PeripheralNameHeading { get => _peripheralNameHeading; private set { if (_peripheralNameHeading != value) { _peripheralNameHeading = value; OnPropertyChanged(); } } }
    public string PeripheralMov { get => _peripheralMov; private set { if (_peripheralMov != value) { _peripheralMov = value; OnPropertyChanged(); } } }
    public string PeripheralCc { get => _peripheralCc; private set { if (_peripheralCc != value) { _peripheralCc = value; OnPropertyChanged(); } } }
    public string PeripheralBs { get => _peripheralBs; private set { if (_peripheralBs != value) { _peripheralBs = value; OnPropertyChanged(); } } }
    public string PeripheralPh { get => _peripheralPh; private set { if (_peripheralPh != value) { _peripheralPh = value; OnPropertyChanged(); } } }
    public string PeripheralWip { get => _peripheralWip; private set { if (_peripheralWip != value) { _peripheralWip = value; OnPropertyChanged(); } } }
    public string PeripheralArm { get => _peripheralArm; private set { if (_peripheralArm != value) { _peripheralArm = value; OnPropertyChanged(); } } }
    public string PeripheralBts { get => _peripheralBts; private set { if (_peripheralBts != value) { _peripheralBts = value; OnPropertyChanged(); } } }
    public string PeripheralVitalityHeader { get => _peripheralVitalityHeader; private set { if (_peripheralVitalityHeader != value) { _peripheralVitalityHeader = value; OnPropertyChanged(); } } }
    public string PeripheralVitality { get => _peripheralVitality; private set { if (_peripheralVitality != value) { _peripheralVitality = value; OnPropertyChanged(); } } }
    public string PeripheralS { get => _peripheralS; private set { if (_peripheralS != value) { _peripheralS = value; OnPropertyChanged(); } } }
    public string PeripheralAva { get => _peripheralAva; private set { if (_peripheralAva != value) { _peripheralAva = value; OnPropertyChanged(); } } }
    public string PeripheralEquipment
    {
        get => _peripheralEquipment;
        private set
        {
            if (_peripheralEquipment == value)
            {
                return;
            }

            _peripheralEquipment = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasPeripheralEquipment));
        }
    }
    public string PeripheralSkills
    {
        get => _peripheralSkills;
        private set
        {
            if (_peripheralSkills == value)
            {
                return;
            }

            _peripheralSkills = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasPeripheralSkills));
        }
    }
    public string EquipmentSummary { get => _equipmentSummary; private set { if (_equipmentSummary != value) { _equipmentSummary = value; OnPropertyChanged(); } } }
    public string SpecialSkillsSummary { get => _specialSkillsSummary; private set { if (_specialSkillsSummary != value) { _specialSkillsSummary = value; OnPropertyChanged(); } } }
    public string ProfilesStatus { get => _profilesStatus; private set { if (_profilesStatus != value) { _profilesStatus = value; OnPropertyChanged(); } } }
    public FormattedString EquipmentSummaryFormatted { get => _equipmentSummaryFormatted; private set { _equipmentSummaryFormatted = value; OnPropertyChanged(); } }
    public FormattedString SpecialSkillsSummaryFormatted { get => _specialSkillsSummaryFormatted; private set { _specialSkillsSummaryFormatted = value; OnPropertyChanged(); } }
    public FormattedString PeripheralEquipmentFormatted { get => _peripheralEquipmentFormatted; private set { _peripheralEquipmentFormatted = value; OnPropertyChanged(); } }
    public FormattedString PeripheralSkillsFormatted { get => _peripheralSkillsFormatted; private set { _peripheralSkillsFormatted = value; OnPropertyChanged(); } }
    public bool HasPeripheralEquipment => !string.IsNullOrWhiteSpace(PeripheralEquipment) && PeripheralEquipment != "-";
    public bool HasPeripheralSkills => !string.IsNullOrWhiteSpace(PeripheralSkills) && PeripheralSkills != "-";
    public bool HasAnyTopHeaderIcons => ShowRegularOrderIcon || ShowIrregularOrderIcon || ShowImpetuousIcon || ShowTacticalAwarenessIcon;
    public bool HasAnyBottomHeaderIcons => ShowCubeIcon || ShowCube2Icon || ShowHackableIcon;
    public bool HasAnyHeaderIcons => HasAnyTopHeaderIcons || HasAnyBottomHeaderIcons;

    public bool ShowRegularOrderIcon
    {
        get => _showRegularOrderIcon;
        private set
        {
            if (_showRegularOrderIcon == value)
            {
                return;
            }

            _showRegularOrderIcon = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasAnyTopHeaderIcons));
            OnPropertyChanged(nameof(HasAnyHeaderIcons));
            HeaderIconsCanvas.InvalidateSurface();
        }
    }

    public bool ShowIrregularOrderIcon
    {
        get => _showIrregularOrderIcon;
        private set
        {
            if (_showIrregularOrderIcon == value)
            {
                return;
            }

            _showIrregularOrderIcon = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasAnyTopHeaderIcons));
            OnPropertyChanged(nameof(HasAnyHeaderIcons));
            HeaderIconsCanvas.InvalidateSurface();
        }
    }

    public bool ShowImpetuousIcon
    {
        get => _showImpetuousIcon;
        private set
        {
            if (_showImpetuousIcon == value)
            {
                return;
            }

            _showImpetuousIcon = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasAnyTopHeaderIcons));
            OnPropertyChanged(nameof(HasAnyHeaderIcons));
            HeaderIconsCanvas.InvalidateSurface();
        }
    }

    public bool ShowTacticalAwarenessIcon
    {
        get => _showTacticalAwarenessIcon;
        private set
        {
            if (_showTacticalAwarenessIcon == value)
            {
                return;
            }

            _showTacticalAwarenessIcon = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasAnyTopHeaderIcons));
            OnPropertyChanged(nameof(HasAnyHeaderIcons));
            HeaderIconsCanvas.InvalidateSurface();
        }
    }

    public bool ShowCubeIcon
    {
        get => _showCubeIcon;
        private set
        {
            if (_showCubeIcon == value)
            {
                return;
            }

            _showCubeIcon = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasAnyBottomHeaderIcons));
            OnPropertyChanged(nameof(HasAnyHeaderIcons));
            HeaderIconsCanvas.InvalidateSurface();
        }
    }

    public bool ShowCube2Icon
    {
        get => _showCube2Icon;
        private set
        {
            if (_showCube2Icon == value)
            {
                return;
            }

            _showCube2Icon = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasAnyBottomHeaderIcons));
            OnPropertyChanged(nameof(HasAnyHeaderIcons));
            HeaderIconsCanvas.InvalidateSurface();
        }
    }

    public bool ShowHackableIcon
    {
        get => _showHackableIcon;
        private set
        {
            if (_showHackableIcon == value)
            {
                return;
            }

            _showHackableIcon = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasAnyBottomHeaderIcons));
            OnPropertyChanged(nameof(HasAnyHeaderIcons));
            HeaderIconsCanvas.InvalidateSurface();
        }
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
        get => _teamsView;
        set
        {
            if (_teamsView == value)
            {
                return;
            }

            _teamsView = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ShowUnitsList));
            OnPropertyChanged(nameof(ShowTeamsList));
        }
    }

    public bool ShowUnitsList => !TeamsView;
    public bool ShowTeamsList => TeamsView;

    public string LeftSlotText
    {
        get => _leftSlotText;
        set
        {
            if (_leftSlotText == value)
            {
                return;
            }

            _leftSlotText = value;
            OnPropertyChanged();
        }
    }

    public string RightSlotText
    {
        get => _rightSlotText;
        set
        {
            if (_rightSlotText == value)
            {
                return;
            }

            _rightSlotText = value;
            OnPropertyChanged();
        }
    }

    public Color LeftSlotBorderColor
    {
        get => _leftSlotBorderColor;
        set
        {
            if (_leftSlotBorderColor == value)
            {
                return;
            }

            _leftSlotBorderColor = value;
            OnPropertyChanged();
        }
    }

    public Color RightSlotBorderColor
    {
        get => _rightSlotBorderColor;
        set
        {
            if (_rightSlotBorderColor == value)
            {
                return;
            }

            _rightSlotBorderColor = value;
            OnPropertyChanged();
        }
    }

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

    private async Task LoadFactionsAsync(CancellationToken cancellationToken = default)
    {
        if (_metadataAccessor is null)
        {
            Console.Error.WriteLine("ArmyFactionSelectionPage metadata service unavailable.");
            return;
        }

        try
        {
            var factions = await _metadataAccessor.GetFactionsAsync(includeDiscontinued: true, cancellationToken);

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

            var items = filtered
                .OrderBy(x => x.Name)
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
        if (_selectedFaction == item)
        {
            AssignSelectedFactionToActiveSlot(item);
            return;
        }

        if (_selectedFaction is not null)
        {
            _selectedFaction.IsSelected = false;
        }

        _selectedFaction = item;
        _selectedFaction.IsSelected = true;
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
            factionChanged = _leftSlotFaction?.Id != item.Id;
            _leftSlotFaction = item;
            LeftSlotText = item.Name;
            _ = LoadSlotIconAsync(0, item.CachedLogoPath, item.PackagedLogoPath);
        }
        else
        {
            factionChanged = _rightSlotFaction?.Id != item.Id;
            _rightSlotFaction = item;
            RightSlotText = item.Name;
            _ = LoadSlotIconAsync(1, item.CachedLogoPath, item.PackagedLogoPath);
        }

        AutoSelectEmptySlot();
        if (factionChanged)
        {
            ResetMercsCompany();
            _ = LoadUnitsForActiveSlotAsync();
        }
    }

    private void ResetMercsCompany()
    {
        if (MercsCompanyEntries.Count == 0)
        {
            UpdateMercsCompanyTotal();
            return;
        }

        MercsCompanyEntries.Clear();
        UpdateMercsCompanyTotal();
    }

    private bool IsDuplicateSelectionForActiveSlot(ArmyFactionSelectionItem item)
    {
        if (!ShowRightSelectionBox)
        {
            return false;
        }

        if (_activeSlotIndex == 0)
        {
            return _rightSlotFaction is not null
                && _rightSlotFaction.Id == item.Id
                && (_leftSlotFaction is null || _leftSlotFaction.Id != item.Id);
        }

        return _leftSlotFaction is not null
            && _leftSlotFaction.Id == item.Id
            && (_rightSlotFaction is null || _rightSlotFaction.Id != item.Id);
    }

    private void AutoSelectEmptySlot()
    {
        if (!ShowRightSelectionBox)
        {
            SetActiveSlot(0);
            return;
        }

        var leftEmpty = _leftSlotFaction is null;
        var rightEmpty = _rightSlotFaction is null;

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
        LeftSlotBorderColor = _activeSlotIndex == 0 ? ActiveBorder : InactiveBorder;
        RightSlotBorderColor = _activeSlotIndex == 1 ? ActiveBorder : InactiveBorder;
    }

    private void OnLeftSlotTapped(object? sender, TappedEventArgs e)
    {
        SetActiveSlot(0);
    }

    private void OnRightSlotTapped(object? sender, TappedEventArgs e)
    {
        if (!ShowRightSelectionBox)
        {
            return;
        }

        SetActiveSlot(1);
    }

    private void OnFactionSelectionHeaderTapped(object? sender, TappedEventArgs e)
    {
        IsFactionSelectionActive = true;
    }

    private void OnUnitSelectionHeaderTapped(object? sender, TappedEventArgs e)
    {
        IsFactionSelectionActive = false;
    }

    private async void OnUnitSelectionFilterButtonTapped(object? sender, TappedEventArgs e)
    {
        try
        {
            var options = await BuildUnitFilterPopupOptionsAsync();
            var popup = new UnitFilterPopupPage(
                options,
                _activeUnitFilter,
                lieutenantOnlyUnits: LieutenantOnlyUnits,
                teamsView: TeamsView);
            popup.FilterArmyApplied += OnFilterArmyApplied;
            popup.CloseRequested += OnUnitFilterPopupCloseRequested;
            _activeUnitFilterPopup = popup;
            UnitFilterPopupHost.Content = popup;
            UnitFilterOverlay.IsVisible = true;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ArmyFactionSelectionPage filter popup open failed: {ex.Message}");
        }
    }

    private void OnFilterArmyApplied(object? sender, UnitFilterCriteria criteria)
    {
        _activeUnitFilter = criteria ?? UnitFilterCriteria.None;
        if (criteria is not null)
        {
            LieutenantOnlyUnits = criteria.LieutenantOnlyUnits;
            TeamsView = criteria.TeamsView;
        }
        CloseUnitFilterPopup(sender as UnitFilterPopupPage);
        _ = ApplyUnitVisibilityFiltersAsync();
    }

    private void OnUnitFilterPopupCloseRequested(object? sender, EventArgs e)
    {
        CloseUnitFilterPopup(sender as UnitFilterPopupPage);
    }

    private void CloseUnitFilterPopup(UnitFilterPopupPage? popup)
    {
        var target = popup ?? _activeUnitFilterPopup;
        if (target is not null)
        {
            target.FilterArmyApplied -= OnFilterArmyApplied;
            target.CloseRequested -= OnUnitFilterPopupCloseRequested;
        }

        _activeUnitFilterPopup = null;
        UnitFilterPopupHost.Content = null;
        UnitFilterOverlay.IsVisible = false;
    }

    private void OnUnitSelectionHeaderBorderSizeChanged(object? sender, EventArgs e)
    {
        if (sender is not Border border || border.Height <= 0)
        {
            return;
        }

        var iconButtonSize = border.Height * 0.8;
        ApplyFilterButtonSize(UnitSelectionFilterButtonInactive, UnitSelectionFilterCanvasInactive, iconButtonSize);
        ApplyFilterButtonSize(UnitSelectionFilterButtonActive, UnitSelectionFilterCanvasActive, iconButtonSize);
    }

    private async Task<UnitFilterPopupOptions> BuildUnitFilterPopupOptionsAsync(CancellationToken cancellationToken = default)
    {
        var classification = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var characteristics = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var skills = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var equipment = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var weapons = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var ammo = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (_armyDataAccessor is not null)
        {
            var sourceFactions = GetUnitSourceFactions();
            var processedFactionIds = new HashSet<int>();
            foreach (var faction in sourceFactions)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!processedFactionIds.Add(faction.Id))
                {
                    continue;
                }

                var snapshot = await _armyDataAccessor.GetFactionSnapshotAsync(faction.Id, cancellationToken);
                var filtersJson = snapshot?.FiltersJson;
                if (string.IsNullOrWhiteSpace(filtersJson))
                {
                    continue;
                }

                var typeLookup = BuildIdNameLookup(filtersJson, "type");
                var charsLookup = BuildIdNameLookup(filtersJson, "chars");
                var skillsLookup = BuildIdNameLookup(filtersJson, "skills");
                var equipLookup = BuildIdNameLookup(filtersJson, "equip");
                var weaponsLookup = BuildIdNameLookup(filtersJson, "weapons");
                var ammoLookup = BuildIdNameLookup(filtersJson, "ammunition");

                var specopsByUnitId = (await _armyDataAccessor.GetSpecopsUnitsByFactionAsync(faction.Id, cancellationToken))
                    .GroupBy(x => x.UnitId)
                    .ToDictionary(x => x.Key, x => x.First());
                var factionUnits = Units.Where(x => x.SourceFactionId == faction.Id).ToList();
                var candidateUnits = factionUnits;

                foreach (var unit in candidateUnits)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (unit.Type.HasValue &&
                        typeLookup.TryGetValue(unit.Type.Value, out var typeName) &&
                        !string.IsNullOrWhiteSpace(typeName))
                    {
                        classification.Add(typeName.Trim());
                    }

                    var unitRecord = await _armyDataAccessor.GetUnitAsync(faction.Id, unit.Id, cancellationToken);
                    var profileGroupsJson = unitRecord?.ProfileGroupsJson;
                    if (string.IsNullOrWhiteSpace(profileGroupsJson) &&
                        specopsByUnitId.TryGetValue(unit.Id, out var specopsUnit))
                    {
                        profileGroupsJson = specopsUnit.ProfileGroupsJson;
                    }

                    if (string.IsNullOrWhiteSpace(profileGroupsJson))
                    {
                        continue;
                    }

                    AddFilterOptionsFromVisibleProfilesAndOptions(
                        profileGroupsJson,
                        charsLookup,
                        skillsLookup,
                        equipLookup,
                        weaponsLookup,
                        ammoLookup,
                        requireLieutenant: false,
                        requireZeroSwc: false,
                        maxCost: null,
                        characteristics,
                        skills,
                        equipment,
                        weapons,
                        ammo);
                }

                // Fallback: if model profile parsing yielded no values for a category,
                // populate from faction lookup so pickers never appear empty.
                if (characteristics.Count == 0)
                {
                    AddLookupNames(charsLookup, characteristics);
                }

                if (skills.Count == 0)
                {
                    AddLookupNames(skillsLookup, skills);
                }

                if (equipment.Count == 0)
                {
                    AddLookupNames(equipLookup, equipment);
                }

                if (weapons.Count == 0)
                {
                    AddLookupNames(weaponsLookup, weapons);
                }

                if (ammo.Count == 0)
                {
                    AddLookupNames(ammoLookup, ammo);
                }
            }
        }

        var maxPoints = int.TryParse(SelectedStartSeasonPoints, out var parsedMaxPoints) ? Math.Max(parsedMaxPoints, 200) : 200;
        Console.WriteLine($"ArmyFactionSelectionPage filter options: class={classification.Count}, chars={characteristics.Count}, skills={skills.Count}, equip={equipment.Count}, weapons={weapons.Count}, ammo={ammo.Count}.");
        return new UnitFilterPopupOptions
        {
            Classification = classification.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList(),
            Characteristics = characteristics.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList(),
            Skills = skills.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList(),
            Equipment = equipment.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList(),
            Weapons = weapons.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList(),
            Ammo = ammo.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList(),
            MinPoints = 0,
            MaxPoints = maxPoints
        };
    }

    private static void AddFilterOptionsFromVisibleProfilesAndOptions(
        string profileGroupsJson,
        IReadOnlyDictionary<int, string> charsLookup,
        IReadOnlyDictionary<int, string> skillsLookup,
        IReadOnlyDictionary<int, string> equipLookup,
        IReadOnlyDictionary<int, string> weaponsLookup,
        IReadOnlyDictionary<int, string> ammoLookup,
        bool requireLieutenant,
        bool requireZeroSwc,
        int? maxCost,
        HashSet<string> characteristics,
        HashSet<string> skills,
        HashSet<string> equipment,
        HashSet<string> weapons,
        HashSet<string> ammo)
    {
        try
        {
            using var doc = JsonDocument.Parse(profileGroupsJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
            {
                return;
            }

            foreach (var group in doc.RootElement.EnumerateArray())
            {
                var groupHasVisibleOption = false;
                if (group.TryGetProperty("options", out var optionsElement) && optionsElement.ValueKind == JsonValueKind.Array)
                {
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

                        var optionCost = ParseCostValue(ReadAdjustedOptionCost(doc.RootElement, group, option));
                        if (maxCost.HasValue && optionCost > maxCost.Value)
                        {
                            continue;
                        }

                        groupHasVisibleOption = true;
                        AddLookupValuesFromEntries(GetOptionEntriesWithIncludes(doc.RootElement, option, "chars"), charsLookup, characteristics);
                        AddLookupValuesFromEntries(GetOptionEntriesWithIncludes(doc.RootElement, option, "skills"), skillsLookup, skills);
                        AddLookupValuesFromEntries(GetOptionEntriesWithIncludes(doc.RootElement, option, "equip"), equipLookup, equipment);
                        AddLookupValuesFromEntries(GetOptionEntriesWithIncludes(doc.RootElement, option, "weapons"), weaponsLookup, weapons);
                        AddLookupValuesFromEntries(GetOptionEntriesWithIncludes(doc.RootElement, option, "ammunition"), ammoLookup, ammo);
                        AddLookupValuesFromEntries(GetOptionEntriesWithIncludes(doc.RootElement, option, "ammo"), ammoLookup, ammo);
                    }
                }

                if (!groupHasVisibleOption)
                {
                    continue;
                }

                if (!group.TryGetProperty("profiles", out var profilesElement) || profilesElement.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var profile in profilesElement.EnumerateArray())
                {
                    AddLookupValuesFromContainerArray(profile, "chars", charsLookup, characteristics);
                    AddLookupValuesFromContainerArray(profile, "skills", skillsLookup, skills);
                    AddLookupValuesFromContainerArray(profile, "equip", equipLookup, equipment);
                    AddLookupValuesFromContainerArray(profile, "weapons", weaponsLookup, weapons);
                    AddLookupValuesFromContainerArray(profile, "ammunition", ammoLookup, ammo);
                    AddLookupValuesFromContainerArray(profile, "ammo", ammoLookup, ammo);
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ArmyFactionSelectionPage AddFilterOptionsFromVisibleProfilesAndOptions failed: {ex.Message}");
        }
    }

    private static void AddLookupValuesFromContainerArray(
        JsonElement container,
        string propertyName,
        IReadOnlyDictionary<int, string> lookup,
        HashSet<string> target)
    {
        if (!container.TryGetProperty(propertyName, out var arrayElement) || arrayElement.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        AddLookupValuesFromEntries(arrayElement.EnumerateArray(), lookup, target);
    }

    private static void AddLookupValuesFromEntries(
        IEnumerable<JsonElement> entries,
        IReadOnlyDictionary<int, string> lookup,
        HashSet<string> target)
    {
        foreach (var entry in entries)
        {
            if (!TryParseId(entry, out var id) || !lookup.TryGetValue(id, out var name) || string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            target.Add(name.Trim());
        }
    }

    private static void AddLookupNames(IReadOnlyDictionary<int, string> lookup, HashSet<string> target)
    {
        foreach (var value in lookup.Values)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            target.Add(value.Trim());
        }
    }

    private async Task LoadUnitsForActiveSlotAsync(CancellationToken cancellationToken = default)
    {
        Units.Clear();
        TeamEntries.Clear();
        _selectedUnit = null;
        ResetUnitDetails();
        if (_armyDataAccessor is null)
        {
            return;
        }

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
                var units = await _armyDataAccessor.GetResumeByFactionMercsOnlyAsync(faction.Id, cancellationToken);
                var resumeByUnitId = units
                    .GroupBy(x => x.UnitId)
                    .ToDictionary(x => x.Key, x => x.First());
                var specopsUnits = await _armyDataAccessor.GetSpecopsUnitsByFactionAsync(faction.Id, cancellationToken);
                var specopsByUnitId = specopsUnits
                    .GroupBy(x => x.UnitId)
                    .ToDictionary(x => x.Key, x => x.First());
                var snapshot = await _armyDataAccessor.GetFactionSnapshotAsync(faction.Id, cancellationToken);
                var typeLookup = BuildIdNameLookup(snapshot?.FiltersJson, "type");
                var categoryLookup = BuildIdNameLookup(snapshot?.FiltersJson, "category");
                var skillsLookup = BuildIdNameLookup(snapshot?.FiltersJson, "skills");
                MergeFireteamEntries(snapshot?.FireteamChartJson, mergedTeams);

                var filteredIds = new HashSet<int>();
                foreach (var unit in units)
                {
                    var unitRecord = await _armyDataAccessor.GetUnitAsync(faction.Id, unit.UnitId, cancellationToken);
                    var profileGroupsJson = unitRecord?.ProfileGroupsJson;
                    if (string.IsNullOrWhiteSpace(profileGroupsJson) &&
                        specopsByUnitId.TryGetValue(unit.UnitId, out var specopsUnit))
                    {
                        profileGroupsJson = specopsUnit.ProfileGroupsJson;
                    }

                    if (UnitHasVisibleOption(
                            profileGroupsJson,
                            skillsLookup,
                            requireLieutenant: false,
                            requireZeroSwc: true))
                    {
                        filteredIds.Add(unit.UnitId);
                    }
                }
                units = units.Where(x => filteredIds.Contains(x.UnitId)).ToList();

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

            foreach (var unit in mergedUnits.Values
                .OrderBy(x => GetUnitTypeSortIndex(x.Type))
                .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase))
            {
                Units.Add(unit);
            }

            foreach (var team in mergedTeams.Values
                         .Where(x => x.Duo > 0)
                         .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase))
            {
                var allowedProfiles = BuildAllowedTeamProfiles(team.UnitLimits, mergedUnits.Values);
                if (ShouldSkipTeamEntry(allowedProfiles))
                {
                    continue;
                }

                TeamEntries.Add(new ArmyTeamListItem
                {
                    Name = team.Name,
                    TeamCountsText = $"D: {team.Duo}",
                    IsExpanded = true,
                    AllowedProfiles = new ObservableCollection<ArmyTeamUnitLimitItem>(allowedProfiles)
                });
            }

            foreach (var team in mergedTeams.Values)
            {
                var isWildcardTeam = IsWildcardTeamName(team.Name);
                foreach (var entry in team.UnitLimits)
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
                    .ToList();

                TeamEntries.Add(new ArmyTeamListItem
                {
                    Name = "Wildcards",
                    TeamCountsText = string.Empty,
                    IsWildcardBucket = true,
                    IsExpanded = true,
                    AllowedProfiles = new ObservableCollection<ArmyTeamUnitLimitItem>(wildcardAllowedProfiles)
                });
            }

            await ApplyUnitVisibilityFiltersAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ArmyFactionSelectionPage LoadUnitsForActiveSlotAsync failed: {ex.Message}");
        }
    }

    private void SetSelectedUnit(ArmyUnitSelectionItem item)
    {
        Console.WriteLine($"ArmyFactionSelectionPage SetSelectedUnit requested: id={item.Id}, faction={item.SourceFactionId}, name='{item.Name}'.");
        if (_selectedUnit == item)
        {
            Console.WriteLine("ArmyFactionSelectionPage SetSelectedUnit skipped (same item instance).");
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

        var combinedEquipment = MergeCommonAndUnique(_selectedUnitCommonEquipment, profile.UniqueEquipment);
        var combinedSkills = MergeCommonAndUnique(_selectedUnitCommonSkills, profile.UniqueSkills);
        var combinedEquipmentText = JoinOrDash(combinedEquipment);
        var combinedSkillsText = JoinOrDash(combinedSkills);
        var currentUnitMove = FormatMoveValue(_unitMoveFirstCm, _unitMoveSecondCm);
        var statline = $"MOV {UnitMov} | CC {UnitCc} | BS {UnitBs} | PH {UnitPh} | WIP {UnitWip} | ARM {UnitArm} | BTS {UnitBts} | {UnitVitalityHeader} {UnitVitality} | S {UnitS}";
        var peripheralStats = BuildMercsCompanyPeripheralStats(profile);
        var entry = new MercsCompanyEntry
        {
            Name = profile.Name,
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
            UnitMoveFirstCm = _unitMoveFirstCm,
            UnitMoveSecondCm = _unitMoveSecondCm,
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
        ApplyLieutenantVisualStates();
        _ = ApplyUnitVisibilityFiltersAsync();
    }

    private PeripheralMercsCompanyStats? BuildMercsCompanyPeripheralStats(ViewerProfileItem profile)
    {
        var peripheralName = ExtractFirstPeripheralName(profile.Peripherals);
        if (string.IsNullOrWhiteSpace(peripheralName) || string.IsNullOrWhiteSpace(_selectedUnitProfileGroupsJson))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(_selectedUnitProfileGroupsJson);
            if (!TryFindPeripheralStatElement(doc.RootElement, peripheralName, out var peripheralProfile))
            {
                return null;
            }

            return BuildPeripheralStatBlock(peripheralName, peripheralProfile, _selectedUnitFiltersJson);
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
                var unitRecord = await _armyDataAccessor!.GetUnitAsync(entry.SourceFactionId, entry.SourceUnitId, cancellationToken);
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
        if (string.IsNullOrWhiteSpace(ava))
        {
            return null;
        }

        var trimmed = ava.Trim();
        if (trimmed is "-" or "T")
        {
            return null;
        }

        if (!int.TryParse(trimmed, out var parsed))
        {
            return null;
        }

        // App convention: 255 means Total (no cap).
        return parsed >= 255 ? null : parsed;
    }

    private async Task ApplyUnitVisibilityFiltersAsync(CancellationToken cancellationToken = default)
    {
        if (_armyDataAccessor is null || Units.Count == 0)
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
                var snapshot = await _armyDataAccessor.GetFactionSnapshotAsync(faction.Id, cancellationToken);
                skillsLookupByFaction[faction.Id] = BuildIdNameLookup(snapshot?.FiltersJson, "skills");
                typeLookupByFaction[faction.Id] = BuildIdNameLookup(snapshot?.FiltersJson, "type");
                charsLookupByFaction[faction.Id] = BuildIdNameLookup(snapshot?.FiltersJson, "chars");
                equipLookupByFaction[faction.Id] = BuildIdNameLookup(snapshot?.FiltersJson, "equip");
                weaponsLookupByFaction[faction.Id] = BuildIdNameLookup(snapshot?.FiltersJson, "weapons");
                ammoLookupByFaction[faction.Id] = BuildIdNameLookup(snapshot?.FiltersJson, "ammunition");
                var specopsUnits = await _armyDataAccessor.GetSpecopsUnitsByFactionAsync(faction.Id, cancellationToken);
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

                var unitRecord = await _armyDataAccessor.GetUnitAsync(unit.SourceFactionId, unit.Id, cancellationToken);
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
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ArmyFactionSelectionPage ApplyUnitVisibilityFiltersAsync failed: {ex.Message}");
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

            var hideSingleMaxOne = team.IsWildcardBucket
                ? false
                : ShouldSkipVisibleTeamEntry(team.AllowedProfiles);
            team.IsVisible = visibleAllowedCount > 0 && !hideSingleMaxOne;
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
            visibleUnitSlugs.Contains(allowedProfileSlug.Trim()))
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
        if (!string.IsNullOrWhiteSpace(allowedProfileSlug))
        {
            var slugMatch = sourceUnits.FirstOrDefault(x =>
                !string.IsNullOrWhiteSpace(x.Slug) &&
                string.Equals(x.Slug.Trim(), allowedProfileSlug.Trim(), StringComparison.OrdinalIgnoreCase));
            if (slugMatch is not null)
            {
                return slugMatch;
            }

            // If a slug is present but not found in currently visible/source units,
            // do not fall back to fuzzy name matching (prevents false matches via comments).
            return null;
        }

        if (string.IsNullOrWhiteSpace(allowedProfileName))
        {
            return null;
        }

        var exactNameMatch = sourceUnits.FirstOrDefault(x =>
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

        foreach (var unit in sourceUnits)
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

        return null;
    }

    private static bool ShouldSkipTeamEntry(IReadOnlyList<ArmyTeamUnitLimitItem> allowedProfiles)
    {
        if (allowedProfiles.Count != 1)
        {
            return false;
        }

        var only = allowedProfiles[0];
        if (IsWildcardEntry(only.Name, only.Slug))
        {
            return false;
        }

        return string.Equals(only.Max?.Trim(), "1", StringComparison.Ordinal);
    }

    private static bool ShouldSkipVisibleTeamEntry(IReadOnlyList<ArmyTeamUnitLimitItem> allowedProfiles)
    {
        var visible = allowedProfiles.Where(x => x.IsVisible).ToList();
        if (visible.Count != 1)
        {
            return false;
        }

        var only = visible[0];
        if (IsWildcardEntry(only.Name, only.Slug))
        {
            return false;
        }

        return string.Equals(only.Max?.Trim(), "1", StringComparison.Ordinal);
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

    private async Task StartCompanyAsync()
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

            var improvedCaptainStats = await ShowImprovedCaptainConfigurationAsync(captainEntry);
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

    private async Task<SavedImprovedCaptainStats?> ShowImprovedCaptainConfigurationAsync(MercsCompanyEntry captainEntry, CancellationToken cancellationToken = default)
    {
        var sourceFactionId = ResolveCaptainSourceFactionId(captainEntry.SourceFactionId);
        var optionFactionId = ResolveCaptainOptionFactionId(sourceFactionId);
        var options = await LoadCaptainUpgradeOptionsAsync(optionFactionId, cancellationToken);

        if (options.IsEmpty && optionFactionId != sourceFactionId)
        {
            options = await LoadCaptainUpgradeOptionsAsync(sourceFactionId, cancellationToken);
            optionFactionId = sourceFactionId;
        }

        var unitInfo = new CaptainUnitPopupInfo
        {
            Name = captainEntry.Name,
            Cost = captainEntry.CostValue,
            Statline = captainEntry.Subtitle ?? "-",
            RangedWeapons = captainEntry.SavedRangedWeapons,
            CcWeapons = captainEntry.SavedCcWeapons,
            Skills = captainEntry.SavedSkills,
            Equipment = captainEntry.SavedEquipment,
            CachedLogoPath = captainEntry.CachedLogoPath,
            PackagedLogoPath = captainEntry.PackagedLogoPath
        };

        var context = new CaptainUpgradePopupContext
        {
            Unit = unitInfo,
            OptionFactionId = optionFactionId,
            OptionFactionName = await ResolveCaptainOptionFactionNameAsync(sourceFactionId, optionFactionId, cancellationToken),
            WeaponOptions = options.Weapons,
            SkillOptions = options.Skills,
            EquipmentOptions = options.Equipment
        };

        return await ConfigureCaptainPopupPage.ShowAsync(Navigation, context);
    }

    private static string ExtractUnitTypeCode(string? subtitle)
    {
        if (string.IsNullOrWhiteSpace(subtitle))
        {
            return string.Empty;
        }

        var firstToken = subtitle
            .Split([' ', '-', '–', '—'], StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault();
        return string.IsNullOrWhiteSpace(firstToken) ? string.Empty : firstToken.Trim().ToUpperInvariant();
    }

    private int ResolveCaptainSourceFactionId(int fallbackSourceFactionId)
    {
        if (fallbackSourceFactionId > 0)
        {
            return fallbackSourceFactionId;
        }

        var firstSource = GetUnitSourceFactions().FirstOrDefault();
        return firstSource?.Id ?? fallbackSourceFactionId;
    }

    private int ResolveCaptainOptionFactionId(int sourceFactionId)
    {
        if (sourceFactionId <= 0)
        {
            return sourceFactionId;
        }

        var sourceFaction = Factions.FirstOrDefault(x => x.Id == sourceFactionId);
        if (sourceFaction is null)
        {
            return sourceFactionId;
        }

        return sourceFaction.ParentId > 0 ? sourceFaction.ParentId : sourceFactionId;
    }

    private async Task<string> ResolveCaptainOptionFactionNameAsync(
        int sourceFactionId,
        int optionFactionId,
        CancellationToken cancellationToken)
    {
        if (sourceFactionId > 0)
        {
            var sourceName = Factions.FirstOrDefault(x => x.Id == sourceFactionId)?.Name;
            if (!string.IsNullOrWhiteSpace(sourceName))
            {
                return sourceName;
            }
        }

        if (optionFactionId > 0)
        {
            var optionName = Factions.FirstOrDefault(x => x.Id == optionFactionId)?.Name;
            if (!string.IsNullOrWhiteSpace(optionName))
            {
                return optionName;
            }
        }

        if (_metadataAccessor is not null)
        {
            if (sourceFactionId > 0)
            {
                var sourceFaction = await _metadataAccessor.GetFactionByIdAsync(sourceFactionId, cancellationToken);
                if (!string.IsNullOrWhiteSpace(sourceFaction?.Name))
                {
                    return sourceFaction.Name;
                }
            }

            if (optionFactionId > 0)
            {
                var optionFaction = await _metadataAccessor.GetFactionByIdAsync(optionFactionId, cancellationToken);
                if (!string.IsNullOrWhiteSpace(optionFaction?.Name))
                {
                    return optionFaction.Name;
                }
            }
        }

        return optionFactionId > 0
            ? $"Faction {optionFactionId}"
            : sourceFactionId > 0
                ? $"Faction {sourceFactionId}"
                : "Faction";
    }

    private async Task<CaptainUpgradeOptionSet> LoadCaptainUpgradeOptionsAsync(int factionId, CancellationToken cancellationToken)
    {
        if (_armyDataAccessor is null || factionId <= 0)
        {
            return CaptainUpgradeOptionSet.Empty;
        }

        try
        {
            var snapshot = await _armyDataAccessor.GetFactionSnapshotAsync(factionId, cancellationToken);
            var skillLookup = BuildIdNameLookup(snapshot?.FiltersJson, "skills");
            var equipLookup = BuildIdNameLookup(snapshot?.FiltersJson, "equip");
            var weaponLookup = BuildIdNameLookup(snapshot?.FiltersJson, "weapons");
            var extrasLookup = BuildExtrasLookup(snapshot?.FiltersJson);

            var skillRecords = await _armyDataAccessor.GetSpecopsSkillsByFactionAsync(factionId, cancellationToken);
            var equipRecords = await _armyDataAccessor.GetSpecopsEquipsByFactionAsync(factionId, cancellationToken);
            var weaponRecords = await _armyDataAccessor.GetSpecopsWeaponsByFactionAsync(factionId, cancellationToken);

            var skills = skillRecords
                .OrderBy(x => x.EntryOrder)
                .Select(x => ResolveSpecopsChoiceLabel(skillLookup, x.SkillId, x.Exp, "Skill", x.ExtrasJson, extrasLookup, _showUnitsInInches))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var equipment = equipRecords
                .OrderBy(x => x.EntryOrder)
                .Select(x => ResolveSpecopsChoiceLabel(equipLookup, x.EquipId, x.Exp, "Equipment", x.ExtrasJson, extrasLookup, _showUnitsInInches))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var weapons = weaponRecords
                .OrderBy(x => x.EntryOrder)
                .Select(x => ResolveSpecopsChoiceLabel(weaponLookup, x.WeaponId, x.Exp, "Weapon", null, extrasLookup, _showUnitsInInches))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return new CaptainUpgradeOptionSet
            {
                Weapons = weapons,
                Skills = skills,
                Equipment = equipment
            };
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ArmyFactionSelectionPage LoadCaptainUpgradeOptionsAsync failed for faction {factionId}: {ex.Message}");
            return CaptainUpgradeOptionSet.Empty;
        }
    }

    private static string ResolveSpecopsChoiceLabel(
        IReadOnlyDictionary<int, string> lookup,
        int id,
        int points,
        string label,
        string? extrasJson,
        IReadOnlyDictionary<int, ExtraDefinition> extrasLookup,
        bool showUnitsInInches)
    {
        var prefix = $"({Math.Max(0, points)}) - ";
        var extrasSuffix = BuildExtrasSuffix(extrasJson, extrasLookup, showUnitsInInches);
        if (lookup.TryGetValue(id, out var value) && !string.IsNullOrWhiteSpace(value))
        {
            return $"{prefix}{value.Trim()}{extrasSuffix}";
        }

        return $"{prefix}{label} {id}{extrasSuffix}";
    }

    private static string BuildExtrasSuffix(
        string? extrasJson,
        IReadOnlyDictionary<int, ExtraDefinition> extrasLookup,
        bool showUnitsInInches)
    {
        if (string.IsNullOrWhiteSpace(extrasJson))
        {
            return string.Empty;
        }

        try
        {
            using var doc = JsonDocument.Parse(extrasJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
            {
                return string.Empty;
            }

            var extras = new List<string>();
            foreach (var element in doc.RootElement.EnumerateArray())
            {
                var parsedId = TryParseExtraId(element);
                if (!parsedId.HasValue)
                {
                    continue;
                }

                if (extrasLookup.TryGetValue(parsedId.Value, out var resolved) && !string.IsNullOrWhiteSpace(resolved.Name))
                {
                    extras.Add(FormatExtraDisplay(resolved, showUnitsInInches));
                }
                else
                {
                    extras.Add(parsedId.Value.ToString(CultureInfo.InvariantCulture));
                }
            }

            if (extras.Count == 0)
            {
                return string.Empty;
            }

            return $" ({string.Join(", ", extras.Distinct(StringComparer.OrdinalIgnoreCase))})";
        }
        catch
        {
            return string.Empty;
        }
    }

    private static int? TryParseExtraId(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out var numberId))
        {
            return numberId;
        }

        if (element.ValueKind == JsonValueKind.String && int.TryParse(element.GetString(), out var stringId))
        {
            return stringId;
        }

        if (element.ValueKind == JsonValueKind.Object &&
            element.TryGetProperty("id", out var idElement))
        {
            if (idElement.ValueKind == JsonValueKind.Number && idElement.TryGetInt32(out var objectNumberId))
            {
                return objectNumberId;
            }

            if (idElement.ValueKind == JsonValueKind.String && int.TryParse(idElement.GetString(), out var objectStringId))
            {
                return objectStringId;
            }
        }

        return null;
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
        var bytes = Encoding.UTF8.GetBytes(fileName);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }

    private static int GetNextCompanyIndex(string saveDir, string companyName, string safeFileName)
    {
        var maxIndex = 0;
        var files = Directory.EnumerateFiles(saveDir, "*.json");

        foreach (var file in files)
        {
            try
            {
                var json = File.ReadAllText(file);
                using var doc = JsonDocument.Parse(json);
                if (!doc.RootElement.TryGetProperty(nameof(SavedCompanyFile.CompanyName), out var nameElement) ||
                    !string.Equals(nameElement.GetString(), companyName, StringComparison.Ordinal))
                {
                    continue;
                }

                if (doc.RootElement.TryGetProperty(nameof(SavedCompanyFile.CompanyIndex), out var indexElement) &&
                    indexElement.ValueKind == JsonValueKind.Number &&
                    indexElement.TryGetInt32(out var parsedIndex))
                {
                    maxIndex = Math.Max(maxIndex, parsedIndex);
                    continue;
                }
            }
            catch
            {
                // Ignore malformed records and continue.
            }

            var baseName = Path.GetFileNameWithoutExtension(file);
            var match = Regex.Match(baseName, $"^{Regex.Escape(safeFileName)}-(\\d+)$", RegexOptions.IgnoreCase);
            if (match.Success && int.TryParse(match.Groups[1].Value, out var fallbackIndex))
            {
                maxIndex = Math.Max(maxIndex, fallbackIndex);
            }
        }

        return maxIndex + 1;
    }

    private string GetCompanyTypeLabel()
    {
        return _mode switch
        {
            ArmySourceSelectionMode.VanillaFactions => "Standard Company - Vanilla",
            ArmySourceSelectionMode.Sectorials => "Standard Company - Sectorial",
            _ => "Unknown Company Type"
        };
    }

    private void UpdateSeasonValidationState()
    {
        var hasLieutenant = MercsCompanyEntries.Any(x => x.IsLieutenant);
        var pointsLimit = int.TryParse(SelectedStartSeasonPoints, out var parsedLimit) ? parsedLimit : 0;
        var currentPoints = int.TryParse(SeasonPointsCapText, out var parsedPoints) ? parsedPoints : 0;
        var shouldShowCheck = hasLieutenant && currentPoints <= pointsLimit;
        IsCompanyValid = shouldShowCheck;

        if (_showSeasonCheckIcon == shouldShowCheck)
        {
            return;
        }

        _showSeasonCheckIcon = shouldShowCheck;
        SeasonValidationCanvas.InvalidateSurface();
    }

    private static int ParseCostValue(string? cost)
    {
        if (string.IsNullOrWhiteSpace(cost))
        {
            return 0;
        }

        if (int.TryParse(cost, out var parsed))
        {
            return parsed;
        }

        var match = Regex.Match(cost, "\\d+");
        return match.Success && int.TryParse(match.Value, out var fallback) ? fallback : 0;
    }

    private async Task LoadSelectedUnitDetailsAsync(CancellationToken cancellationToken = default)
    {
        ResetUnitDetails(clearLogo: false, resetHeaderColors: false);
        if (_selectedUnit is null || _armyDataAccessor is null)
        {
            Console.Error.WriteLine("ArmyFactionSelectionPage LoadSelectedUnitDetailsAsync aborted: selected unit or accessor missing.");
            return;
        }

        try
        {
            Console.WriteLine($"ArmyFactionSelectionPage LoadSelectedUnitDetailsAsync started: id={_selectedUnit.Id}, faction={_selectedUnit.SourceFactionId}, name='{_selectedUnit.Name}'.");
            UnitNameHeading = _selectedUnit.Name;
            var unit = await _armyDataAccessor.GetUnitAsync(_selectedUnit.SourceFactionId, _selectedUnit.Id, cancellationToken);
            ArmySpecopsUnitRecord? specopsUnit = null;
            if (_selectedUnit.IsSpecOps || unit is null)
            {
                var specopsUnits = await _armyDataAccessor.GetSpecopsUnitsByFactionAsync(_selectedUnit.SourceFactionId, cancellationToken);
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

            var snapshot = await _armyDataAccessor.GetFactionSnapshotAsync(_selectedUnit.SourceFactionId, cancellationToken);
            if (string.IsNullOrWhiteSpace(profileGroupsJson))
            {
                Console.Error.WriteLine($"ArmyFactionSelectionPage: profile groups not found for faction={_selectedUnit.SourceFactionId}, unit={_selectedUnit.Id}.");
                return;
            }

            var equipLookup = BuildIdNameLookup(snapshot?.FiltersJson, "equip");
            var skillsLookup = BuildIdNameLookup(snapshot?.FiltersJson, "skills");
            var charsLookup = BuildIdNameLookup(snapshot?.FiltersJson, "chars");
            var extrasLookup = BuildExtrasLookup(snapshot?.FiltersJson);
            _selectedUnitProfileGroupsJson = profileGroupsJson;
            _selectedUnitFiltersJson = snapshot?.FiltersJson;
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

                var stableEquipFromProfiles = ComputeCommonDisplayNamesFromProfiles(
                    profileGroupsJson,
                    "equip",
                    equipLookup,
                    extrasLookup,
                    _showUnitsInInches);
                var stableEquipFromVisibleOptions = new List<string>();
                if (visibleOptions.Count > 0)
                {
                    stableEquipFromVisibleOptions = IntersectDisplayNamesWithIncludes(
                        doc.RootElement,
                        visibleOptions,
                        "equip",
                        equipLookup,
                        extrasLookup,
                        _showUnitsInInches);
                }
                var stableEquip = stableEquipFromProfiles
                    .Concat(stableEquipFromVisibleOptions)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var stableSkillsFromProfiles = ComputeCommonDisplayNamesFromProfiles(
                    profileGroupsJson,
                    "skills",
                    skillsLookup,
                    extrasLookup,
                    _showUnitsInInches);
                var stableSkillsFromVisibleOptions = new List<string>();
                if (visibleOptions.Count > 0)
                {
                    stableSkillsFromVisibleOptions = IntersectDisplayNamesWithIncludes(
                        doc.RootElement,
                        visibleOptions,
                        "skills",
                        skillsLookup,
                        extrasLookup,
                        _showUnitsInInches);
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
                _selectedUnitCommonEquipment = stableEquip;
                _selectedUnitCommonSkills = stableSkills;
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
        _selectedUnitPicture?.Dispose();
        _selectedUnitPicture = null;

        try
        {
            Stream? stream = await OpenBestUnitLogoStreamAsync(item);

            if (stream is null)
            {
                SelectedUnitCanvas.InvalidateSurface();
                return;
            }

            await using (stream)
            {
                var svg = new SKSvg();
                _selectedUnitPicture = svg.Load(stream);
                if (_selectedUnitPicture is null)
                {
                    Console.Error.WriteLine($"ArmyFactionSelectionPage selected logo parse failed: unit='{item.Name}', id={item.Id}, faction={item.SourceFactionId}.");
                }
                else
                {
                    var bounds = _selectedUnitPicture.CullRect;
                    Console.WriteLine($"ArmyFactionSelectionPage selected logo loaded: unit='{item.Name}', bounds=({bounds.Left},{bounds.Top},{bounds.Right},{bounds.Bottom}).");
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ArmyFactionSelectionPage LoadSelectedUnitLogoAsync failed: {ex.Message}");
            _selectedUnitPicture = null;
        }

        SelectedUnitCanvas.InvalidateSurface();
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
        var extrasLookup = BuildExtrasLookup(filtersJson);

        var equipUsageCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var skillUsageCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
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

                foreach (var name in GetOrderedIdDisplayNamesFromEntries(
                             GetOptionEntriesWithIncludes(profileGroupsRoot, option, "equip"),
                             equipLookup,
                             extrasLookup,
                             _showUnitsInInches))
                {
                    equipUsageCounts[name] = equipUsageCounts.TryGetValue(name, out var count) ? count + 1 : 1;
                }

                var optionSkillNames = BuildConfigurationSkillNames(
                    GetOrderedIdDisplayNamesFromEntries(
                        GetOptionEntriesWithIncludes(profileGroupsRoot, option, "skills"),
                        skillsLookup,
                        extrasLookup,
                        _showUnitsInInches));
                foreach (var name in optionSkillNames)
                {
                    skillUsageCounts[name] = skillUsageCounts.TryGetValue(name, out var count) ? count + 1 : 1;
                }
            }
        }

        var bestConfigurationByKey = new Dictionary<string, (int Index, int PeripheralCount)>(StringComparer.OrdinalIgnoreCase);

        foreach (var group in profileGroupsRoot.EnumerateArray())
        {
            if (hasControllerGroups && !IsControllerGroup(profileGroupsRoot, group))
            {
                continue;
            }

            var groupName = group.TryGetProperty("isc", out var iscElement) && iscElement.ValueKind == JsonValueKind.String
                ? iscElement.GetString() ?? string.Empty
                : string.Empty;

            if (!group.TryGetProperty("options", out var optionsElement) || optionsElement.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var option in optionsElement.EnumerateArray())
            {
                var swc = ReadOptionSwc(option);
                if (IsPositiveSwc(swc))
                {
                    continue;
                }

                var optionName = option.TryGetProperty("name", out var nameElement) && nameElement.ValueKind == JsonValueKind.String
                    ? nameElement.GetString() ?? string.Empty
                    : string.Empty;
                if (string.IsNullOrWhiteSpace(optionName))
                {
                    optionName = groupName;
                }
                optionName = BuildOptionDisplayName(option, optionName, equipLookup, skillsLookup);

                var optionWeapons = GetOrderedIdDisplayNamesFromEntries(
                    GetOptionEntriesWithIncludes(profileGroupsRoot, option, "weapons"),
                    weaponsLookup,
                    extrasLookup,
                    _showUnitsInInches);
                var rangedWeaponNames = optionWeapons.Where(x => !IsMeleeWeaponName(x)).ToList();
                var meleeWeaponNames = optionWeapons.Where(IsMeleeWeaponName).ToList();

                var optionEquipmentNames = GetOrderedIdDisplayNamesFromEntries(
                        GetOptionEntriesWithIncludes(profileGroupsRoot, option, "equip"),
                        equipLookup,
                        extrasLookup,
                        _showUnitsInInches)
                    .ToList();
                var uniqueEquipmentNames = optionEquipmentNames
                    .Where(x => equipUsageCounts.TryGetValue(x, out var c) && c == 1)
                    .ToList();
                if (uniqueEquipmentNames.Count == 0)
                {
                    uniqueEquipmentNames = optionEquipmentNames
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                        .ToList();
                }

                var optionSkillsNames = BuildConfigurationSkillNames(
                    GetOrderedIdDisplayNamesFromEntries(
                        GetOptionEntriesWithIncludes(profileGroupsRoot, option, "skills"),
                        skillsLookup,
                        extrasLookup,
                        _showUnitsInInches));
                var uniqueSkillsNames = optionSkillsNames
                    .Where(x => skillUsageCounts.TryGetValue(x, out var c) && c == 1)
                    .ToList();
                if (uniqueSkillsNames.Count == 0)
                {
                    uniqueSkillsNames = optionSkillsNames
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                        .ToList();
                }

                var peripheralNames = GetCountedDisplayNamesFromEntries(
                    GetDisplayPeripheralEntriesForOption(profileGroupsRoot, group, option),
                    peripheralLookup,
                    extrasLookup,
                    _showUnitsInInches);
                var firstPeripheralName = peripheralNames.FirstOrDefault();
                PeripheralMercsCompanyStats? peripheralStats = null;
                if (!string.IsNullOrWhiteSpace(firstPeripheralName) &&
                    TryFindPeripheralStatElement(profileGroupsRoot, ExtractFirstPeripheralName(firstPeripheralName), out var peripheralProfile))
                {
                    peripheralStats = BuildPeripheralStatBlock(ExtractFirstPeripheralName(firstPeripheralName), peripheralProfile, filtersJson);
                }

                var cost = ReadAdjustedOptionCost(profileGroupsRoot, group, option);

                var normalizedPeripheralNames = peripheralNames
                    .Select(NormalizePeripheralNameForDedupe)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .ToList();
                var peripheralCount = GetPeripheralTotalCount(peripheralNames);
                var dedupeKey = $"{groupName}|{optionName}|{string.Join("|", rangedWeaponNames)}|{string.Join("|", meleeWeaponNames)}|{string.Join("|", uniqueEquipmentNames)}|{string.Join("|", uniqueSkillsNames)}|{string.Join("|", normalizedPeripheralNames)}|{swc}";
                var hasExisting = bestConfigurationByKey.TryGetValue(dedupeKey, out var existingConfiguration);
                if (hasExisting && peripheralCount >= existingConfiguration.PeripheralCount)
                {
                    continue;
                }

                var isLieutenant = forceLieutenant || IsLieutenantOption(option, skillsLookup);
                var profileKey = $"{groupName}|{optionName}|{cost}|{swc}|lt:{(isLieutenant ? 1 : 0)}";
                var profileItem = new ViewerProfileItem
                {
                    GroupName = groupName,
                    Name = optionName,
                    ProfileKey = profileKey,
                    IsLieutenant = isLieutenant,
                    NameFormatted = BuildNameFormatted(optionName),
                    RangedWeapons = JoinOrDash(rangedWeaponNames),
                    RangedWeaponsFormatted = BuildListFormattedString(rangedWeaponNames, Color.FromArgb("#EF4444")),
                    MeleeWeapons = JoinOrDash(meleeWeaponNames),
                    MeleeWeaponsFormatted = BuildListFormattedString(meleeWeaponNames, Color.FromArgb("#22C55E")),
                    UniqueEquipment = JoinOrDash(uniqueEquipmentNames),
                    UniqueEquipmentFormatted = BuildListFormattedString(uniqueEquipmentNames, Color.FromArgb("#06B6D4")),
                    UniqueSkills = JoinOrDash(uniqueSkillsNames),
                    UniqueSkillsFormatted = BuildListFormattedString(
                        uniqueSkillsNames,
                        Color.FromArgb("#F59E0B"),
                        highlightLieutenantPurple: forceLieutenant),
                    Peripherals = JoinOrDash(peripheralNames),
                    PeripheralsFormatted = BuildListFormattedString(peripheralNames, Color.FromArgb("#FACC15")),
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
                    PeripheralSubtitle = BuildPeripheralSubtitle(peripheralStats),
                    PeripheralEquipmentLineFormatted = BuildMercsCompanyLineFormatted("Equipment", peripheralStats?.Equipment, Color.FromArgb("#06B6D4")),
                    HasPeripheralEquipmentLine = peripheralStats is not null && !string.IsNullOrWhiteSpace(peripheralStats.Equipment) && peripheralStats.Equipment != "-",
                    PeripheralSkillsLineFormatted = BuildMercsCompanyLineFormatted("Skills", peripheralStats?.Skills, Color.FromArgb("#F59E0B")),
                    HasPeripheralSkillsLine = peripheralStats is not null && !string.IsNullOrWhiteSpace(peripheralStats.Skills) && peripheralStats.Skills != "-",
                    Swc = swc,
                    SwcDisplay = $"SWC {swc}",
                    Cost = cost
                };

                if (hasExisting)
                {
                    Profiles[existingConfiguration.Index] = profileItem;
                    bestConfigurationByKey[dedupeKey] = (existingConfiguration.Index, peripheralCount);
                    continue;
                }

                Profiles.Add(profileItem);
                bestConfigurationByKey[dedupeKey] = (Profiles.Count - 1, peripheralCount);
            }
        }

        ApplyLieutenantVisualStates();
    }

    private static List<string> GetOrderedIdDisplayNamesFromEntries(
        IEnumerable<JsonElement> entries,
        IReadOnlyDictionary<int, string> lookup,
        IReadOnlyDictionary<int, ExtraDefinition> extrasLookup,
        bool showUnitsInInches)
    {
        var names = new List<string>();
        foreach (var entry in entries)
        {
            if (!TryParseId(entry, out var id))
            {
                continue;
            }

            var baseName = lookup.TryGetValue(id, out var resolvedName) ? resolvedName : id.ToString();
            names.Add(BuildEntryDisplayName(baseName, entry, extrasLookup, showUnitsInInches));
        }

        return names
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static List<string> GetCountedDisplayNamesFromEntries(
        IEnumerable<JsonElement> entries,
        IReadOnlyDictionary<int, string> lookup,
        IReadOnlyDictionary<int, ExtraDefinition> extrasLookup,
        bool showUnitsInInches)
    {
        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in entries)
        {
            if (!TryParseId(entry, out var id))
            {
                continue;
            }

            var baseName = lookup.TryGetValue(id, out var resolvedName) ? resolvedName : id.ToString();
            var displayName = BuildEntryDisplayName(baseName, entry, extrasLookup, showUnitsInInches);
            if (string.IsNullOrWhiteSpace(displayName))
            {
                continue;
            }

            var quantity = ReadEntryQuantity(entry);
            counts[displayName] = counts.TryGetValue(displayName, out var existing)
                ? existing + quantity
                : quantity;
        }

        return counts
            .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
            .Select(x => $"{x.Key} ({x.Value})")
            .ToList();
    }

    private static int ReadEntryQuantity(JsonElement entry)
    {
        if (entry.ValueKind != JsonValueKind.Object)
        {
            return 1;
        }

        if (entry.TryGetProperty("q", out var quantityElement))
        {
            if (quantityElement.ValueKind == JsonValueKind.Number && quantityElement.TryGetInt32(out var quantityNumber))
            {
                return Math.Max(1, quantityNumber);
            }

            if (quantityElement.ValueKind == JsonValueKind.String &&
                int.TryParse(quantityElement.GetString(), System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var quantityText))
            {
                return Math.Max(1, quantityText);
            }
        }

        return 1;
    }

    private static bool IsMeleeWeaponName(string name)
    {
        return Regex.IsMatch(
            name,
            @"\bccw\b|\bda ccw\b|\bap ccw\b|\bknife\b|\bsword\b|\bmonofilament\b|\bviral ccw\b|\bpistols?\b|\bclose combat weapon\b|\bcc\s*weapon\b|\bc\.?\s*c\.?\s*weapon\b|\bpara\s*cc\s*weapon\b",
            RegexOptions.IgnoreCase);
    }

    private static string JoinOrDash(IEnumerable<string> values)
    {
        var list = values.Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
        return list.Count == 0 ? "-" : string.Join(Environment.NewLine, list);
    }

    private static List<string> EnsureLieutenantSkill(IEnumerable<string> skills)
    {
        var list = skills
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (!list.Any(x => x.Contains("lieutenant", StringComparison.OrdinalIgnoreCase)))
        {
            list.Add("Lieutenant");
        }

        return list
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string ReadOptionSwc(JsonElement option)
    {
        if (option.TryGetProperty("swc", out var swcElement))
        {
            if (swcElement.ValueKind == JsonValueKind.Number && swcElement.TryGetDecimal(out var swcNumber))
            {
                return swcNumber.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }

            if (swcElement.ValueKind == JsonValueKind.String)
            {
                return swcElement.GetString() ?? "-";
            }
        }

        return "-";
    }

    private static string ReadOptionCost(JsonElement option)
    {
        if (option.TryGetProperty("points", out var pointsElement))
        {
            if (pointsElement.ValueKind == JsonValueKind.Number && pointsElement.TryGetInt32(out var intCost))
            {
                return intCost.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }

            if (pointsElement.ValueKind == JsonValueKind.String)
            {
                var points = pointsElement.GetString();
                return string.IsNullOrWhiteSpace(points) ? "-" : points;
            }
        }

        if (option.TryGetProperty("cost", out var costElement))
        {
            if (costElement.ValueKind == JsonValueKind.Number && costElement.TryGetInt32(out var costNumber))
            {
                return costNumber.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }

            if (costElement.ValueKind == JsonValueKind.String)
            {
                var cost = costElement.GetString();
                return string.IsNullOrWhiteSpace(cost) ? "-" : cost;
            }
        }

        if (option.TryGetProperty("pts", out var ptsElement))
        {
            if (ptsElement.ValueKind == JsonValueKind.Number && ptsElement.TryGetInt32(out var ptsNumber))
            {
                return ptsNumber.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }

            if (ptsElement.ValueKind == JsonValueKind.String)
            {
                var points = ptsElement.GetString();
                return string.IsNullOrWhiteSpace(points) ? "-" : points;
            }
        }

        return "-";
    }

    private static string ReadAdjustedOptionCost(JsonElement profileGroupsRoot, JsonElement group, JsonElement option)
    {
        var baseCostText = ReadOptionCost(option);
        if (!int.TryParse(baseCostText, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var baseCost))
        {
            return baseCostText;
        }

        var totalPeripheralCount = GetDisplayPeripheralEntriesForOption(profileGroupsRoot, group, option)
            .Sum(ReadEntryQuantity);
        if (totalPeripheralCount <= 1)
        {
            return baseCostText;
        }

        var minis = ReadOptionMinis(option);
        if (minis <= 1 || minis <= totalPeripheralCount)
        {
            return baseCostText;
        }

        if (baseCost <= 0 || baseCost % minis != 0)
        {
            return baseCostText;
        }

        var removedPeripheralCount = totalPeripheralCount - 1;
        var perModelCost = baseCost / minis;
        var deduction = removedPeripheralCount * perModelCost;
        var adjustedCost = Math.Max(0, baseCost - deduction);
        return adjustedCost.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    private static IEnumerable<JsonElement> GetControllerPeripheralEntries(JsonElement group)
    {
        if (!group.TryGetProperty("profiles", out var profilesElement) || profilesElement.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var collected = new List<JsonElement>();
        foreach (var profile in profilesElement.EnumerateArray())
        {
            if (profile.TryGetProperty("peripheral", out var peripheralElement) &&
                peripheralElement.ValueKind == JsonValueKind.Array &&
                peripheralElement.GetArrayLength() > 0)
            {
                collected.AddRange(peripheralElement.EnumerateArray().ToList());
            }
        }

        return collected;
    }

    private static HashSet<int> GetControllerPeripheralIds(JsonElement group)
    {
        var ids = new HashSet<int>();
        foreach (var entry in GetControllerPeripheralEntries(group))
        {
            if (TryParseId(entry, out var id))
            {
                ids.Add(id);
            }
        }

        return ids;
    }

    private static IEnumerable<JsonElement> GetFilteredOptionPeripheralEntries(
        JsonElement profileGroupsRoot,
        JsonElement group,
        JsonElement option)
    {
        var allowedIds = GetControllerPeripheralIds(group);
        var optionEntries = GetOptionEntriesWithIncludes(profileGroupsRoot, option, "peripheral").ToList();

        if (allowedIds.Count == 0)
        {
            return optionEntries;
        }

        return optionEntries
            .Where(entry => TryParseId(entry, out var id) && allowedIds.Contains(id))
            .ToList();
    }

    private static IEnumerable<JsonElement> GetDisplayPeripheralEntriesForOption(
        JsonElement profileGroupsRoot,
        JsonElement group,
        JsonElement option)
    {
        var optionEntries = GetFilteredOptionPeripheralEntries(profileGroupsRoot, group, option).ToList();
        if (optionEntries.Count > 0)
        {
            return optionEntries;
        }

        return GetControllerPeripheralEntries(group).ToList();
    }

    private static bool IsControllerGroup(JsonElement profileGroupsRoot, JsonElement group)
    {
        if (GetControllerPeripheralIds(group).Count > 0)
        {
            return true;
        }

        if (!group.TryGetProperty("options", out var optionsElement) || optionsElement.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        foreach (var option in optionsElement.EnumerateArray())
        {
            if (GetOptionEntriesWithIncludes(profileGroupsRoot, option, "peripheral").Any())
            {
                return true;
            }
        }

        return false;
    }

    private static int ReadOptionMinis(JsonElement option)
    {
        if (!option.TryGetProperty("minis", out var minisElement))
        {
            return 0;
        }

        if (minisElement.ValueKind == JsonValueKind.Number && minisElement.TryGetInt32(out var minisNumber))
        {
            return Math.Max(0, minisNumber);
        }

        if (minisElement.ValueKind == JsonValueKind.String &&
            int.TryParse(minisElement.GetString(), System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var minisText))
        {
            return Math.Max(0, minisText);
        }

        return 0;
    }

    private static string BuildOptionDisplayName(
        JsonElement option,
        string baseName,
        IReadOnlyDictionary<int, string> equipLookup,
        IReadOnlyDictionary<int, string> skillsLookup)
    {
        var details = new List<string>();
        var normalizedBase = baseName.ToLowerInvariant();

        foreach (var skillName in GetOrderedNames(option, "skills", skillsLookup))
        {
            if (IsNameDetailTag(skillName) && !normalizedBase.Contains(skillName.ToLowerInvariant()))
            {
                details.Add(skillName);
            }
        }

        foreach (var equipName in GetOrderedNames(option, "equip", equipLookup))
        {
            if (IsNameDetailTag(equipName) && !normalizedBase.Contains(equipName.ToLowerInvariant()))
            {
                details.Add(equipName);
            }
        }

        if (option.TryGetProperty("orders", out var ordersElement) && ordersElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var order in ordersElement.EnumerateArray())
            {
                if (!order.TryGetProperty("type", out var typeElement) || typeElement.ValueKind != JsonValueKind.String)
                {
                    continue;
                }

                var type = typeElement.GetString();
                if (string.Equals(type, "LIEUTENANT", StringComparison.OrdinalIgnoreCase) &&
                    !normalizedBase.Contains("lieutenant"))
                {
                    details.Add("Lieutenant");
                }
            }
        }

        var distinctDetails = details
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (distinctDetails.Count == 0)
        {
            return baseName;
        }

        return $"{baseName} ({string.Join(", ", distinctDetails)})";
    }

    private static bool IsNameDetailTag(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return value.Contains("forward observer", StringComparison.OrdinalIgnoreCase) ||
               value.Contains("hacker", StringComparison.OrdinalIgnoreCase) ||
               value.Contains("hacking device", StringComparison.OrdinalIgnoreCase) ||
               value.Contains("specialist", StringComparison.OrdinalIgnoreCase) ||
               value.Contains("paramedic", StringComparison.OrdinalIgnoreCase) ||
               value.Contains("doctor", StringComparison.OrdinalIgnoreCase) ||
               value.Contains("engineer", StringComparison.OrdinalIgnoreCase) ||
               value.Contains("lieutenant", StringComparison.OrdinalIgnoreCase) ||
               value.Contains("nco", StringComparison.OrdinalIgnoreCase) ||
               value.Contains("chain of command", StringComparison.OrdinalIgnoreCase);
    }

    private static List<string> GetOrderedNames(
        JsonElement container,
        string propertyName,
        IReadOnlyDictionary<int, string> lookup)
    {
        if (!container.TryGetProperty(propertyName, out var arr) || arr.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var names = new List<string>();
        foreach (var entry in arr.EnumerateArray())
        {
            if (!TryParseId(entry, out var id))
            {
                continue;
            }

            if (lookup.TryGetValue(id, out var resolvedName) && !string.IsNullOrWhiteSpace(resolvedName))
            {
                names.Add(resolvedName);
            }
        }

        return names
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static FormattedString BuildNameFormatted(string name)
    {
        var formatted = new FormattedString();
        if (string.IsNullOrWhiteSpace(name))
        {
            formatted.Spans.Add(new Span { Text = "-" });
            return formatted;
        }

        var match = Regex.Match(name, "(lieutenant)", RegexOptions.IgnoreCase);
        if (!match.Success)
        {
            formatted.Spans.Add(new Span { Text = name });
            return formatted;
        }

        if (match.Index > 0)
        {
            formatted.Spans.Add(new Span { Text = name[..match.Index] });
        }

        formatted.Spans.Add(new Span
        {
            Text = name.Substring(match.Index, match.Length),
            TextColor = Color.FromArgb("#C084FC")
        });

        var suffixStart = match.Index + match.Length;
        if (suffixStart < name.Length)
        {
            formatted.Spans.Add(new Span { Text = name[suffixStart..] });
        }

        return formatted;
    }

    private static FormattedString BuildListFormattedString(IEnumerable<string> values, Color textColor, bool highlightLieutenantPurple = false)
    {
        var formatted = new FormattedString();
        var lines = values.Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
        if (lines.Count == 0)
        {
            formatted.Spans.Add(new Span { Text = "-", TextColor = textColor });
            return formatted;
        }

        for (var i = 0; i < lines.Count; i++)
        {
            if (i > 0)
            {
                formatted.Spans.Add(new Span { Text = Environment.NewLine, TextColor = textColor });
            }

            var lineColor = highlightLieutenantPurple && lines[i].Contains("lieutenant", StringComparison.OrdinalIgnoreCase)
                ? Color.FromArgb("#C084FC")
                : textColor;
            formatted.Spans.Add(new Span { Text = lines[i], TextColor = lineColor });
        }

        return formatted;
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

            if (_leftSlotFaction is not null)
            {
                yield return _factionLogoCacheService.GetCachedUnitLogoPath(_leftSlotFaction.Id, item.Id);
            }

            if (_rightSlotFaction is not null)
            {
                yield return _factionLogoCacheService.GetCachedUnitLogoPath(_rightSlotFaction.Id, item.Id);
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

            if (_leftSlotFaction is not null)
            {
                yield return _factionLogoCacheService.GetPackagedUnitLogoPath(_leftSlotFaction.Id, item.Id);
            }

            if (_rightSlotFaction is not null)
            {
                yield return _factionLogoCacheService.GetPackagedUnitLogoPath(_rightSlotFaction.Id, item.Id);
            }

            yield return _factionLogoCacheService.GetPackagedFactionLogoPath(item.SourceFactionId);
        }
        else
        {
            yield return $"SVGCache/units/{item.SourceFactionId}-{item.Id}.svg";
            if (_leftSlotFaction is not null)
            {
                yield return $"SVGCache/units/{_leftSlotFaction.Id}-{item.Id}.svg";
            }

            if (_rightSlotFaction is not null)
            {
                yield return $"SVGCache/units/{_rightSlotFaction.Id}-{item.Id}.svg";
            }

            yield return $"SVGCache/factions/{item.SourceFactionId}.svg";
        }
    }

    private List<ArmyFactionSelectionItem> GetUnitSourceFactions()
    {
        if (!ShowRightSelectionBox)
        {
            return _leftSlotFaction is null ? [] : [_leftSlotFaction];
        }

        var list = new List<ArmyFactionSelectionItem>(2);
        if (_leftSlotFaction is not null)
        {
            list.Add(_leftSlotFaction);
        }

        if (_rightSlotFaction is not null && (_leftSlotFaction is null || _rightSlotFaction.Id != _leftSlotFaction.Id))
        {
            list.Add(_rightSlotFaction);
        }

        return list;
    }

    private static bool IsCommonSpecOpsSkill(string? skillName)
    {
        if (string.IsNullOrWhiteSpace(skillName))
        {
            return false;
        }

        return skillName.Contains("lieutenant", StringComparison.OrdinalIgnoreCase) ||
               skillName.Contains("infinity spec-ops", StringComparison.OrdinalIgnoreCase) ||
               skillName.Contains("infinity spec ops", StringComparison.OrdinalIgnoreCase);
    }

    private static List<string> BuildConfigurationSkillNames(IEnumerable<string> rawSkillNames)
    {
        var result = new List<string>();
        foreach (var rawName in rawSkillNames)
        {
            if (string.IsNullOrWhiteSpace(rawName))
            {
                continue;
            }

            var skillName = rawName.Trim();
            if (!IsCommonSpecOpsSkill(skillName))
            {
                result.Add(skillName);
                continue;
            }

            var lieutenantDetail = ExtractLieutenantSkillDetail(skillName);
            if (!string.IsNullOrWhiteSpace(lieutenantDetail))
            {
                result.Add(lieutenantDetail);
            }
        }

        return result
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string? ExtractLieutenantSkillDetail(string skillName)
    {
        if (!skillName.Contains("lieutenant", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var detail = Regex.Replace(skillName, "lieutenant", string.Empty, RegexOptions.IgnoreCase).Trim();
        if (string.IsNullOrWhiteSpace(detail))
        {
            return null;
        }

        detail = detail.Trim('(', ')', '[', ']', '-', ':', ',', ';', ' ');
        return string.IsNullOrWhiteSpace(detail) ? null : detail;
    }

    private static Dictionary<int, string> BuildIdNameLookup(string? filtersJson, string sectionName)
    {
        var map = new Dictionary<int, string>();
        if (string.IsNullOrWhiteSpace(filtersJson))
        {
            return map;
        }

        try
        {
            using var doc = JsonDocument.Parse(filtersJson);
            if (!doc.RootElement.TryGetProperty(sectionName, out var section) || section.ValueKind != JsonValueKind.Array)
            {
                return map;
            }

            foreach (var entry in section.EnumerateArray())
            {
                if (!entry.TryGetProperty("id", out var idElement))
                {
                    continue;
                }

                int id;
                if (idElement.ValueKind == JsonValueKind.Number && idElement.TryGetInt32(out var intId))
                {
                    id = intId;
                }
                else if (idElement.ValueKind == JsonValueKind.String && int.TryParse(idElement.GetString(), out var stringId))
                {
                    id = stringId;
                }
                else
                {
                    continue;
                }

                if (!entry.TryGetProperty("name", out var nameElement) || nameElement.ValueKind != JsonValueKind.String)
                {
                    continue;
                }

                var name = nameElement.GetString();
                if (!string.IsNullOrWhiteSpace(name))
                {
                    map[id] = name;
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ArmyFactionSelectionPage BuildIdNameLookup failed for '{sectionName}': {ex.Message}");
        }

        return map;
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
        if (!element.TryGetProperty("min", out var value) || value.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        var text = value.GetString();
        return string.Equals(text?.Trim(), "*", StringComparison.Ordinal);
    }

    private static string ReadString(JsonElement element, string propertyName, string fallback)
    {
        if (!element.TryGetProperty(propertyName, out var value) || value.ValueKind != JsonValueKind.String)
        {
            return fallback;
        }

        var text = value.GetString();
        return string.IsNullOrWhiteSpace(text) ? fallback : text;
    }

    private static int ReadInt(JsonElement element, string propertyName, int fallback)
    {
        if (!element.TryGetProperty(propertyName, out var value))
        {
            return fallback;
        }

        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number))
        {
            return number;
        }

        if (value.ValueKind == JsonValueKind.String && int.TryParse(value.GetString(), out var parsed))
        {
            return parsed;
        }

        return fallback;
    }

    private static bool ReadBool(JsonElement element, string propertyName, bool fallback)
    {
        if (!element.TryGetProperty(propertyName, out var value))
        {
            return fallback;
        }

        if (value.ValueKind == JsonValueKind.True)
        {
            return true;
        }

        if (value.ValueKind == JsonValueKind.False)
        {
            return false;
        }

        if (value.ValueKind == JsonValueKind.String && bool.TryParse(value.GetString(), out var parsed))
        {
            return parsed;
        }

        return fallback;
    }

    private static bool UnitHasLieutenantOption(string? profileGroupsJson, IReadOnlyDictionary<int, string> skillsLookup)
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
                    if (IsLieutenantOption(option, skillsLookup))
                    {
                        return true;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ArmyFactionSelectionPage UnitHasLieutenantOption failed: {ex.Message}");
        }

        return false;
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

                    var optionCost = ParseCostValue(ReadAdjustedOptionCost(doc.RootElement, group, option));
                    if (maxCost.HasValue && optionCost > maxCost.Value)
                    {
                        continue;
                    }

                    if (criteria.MinPoints.HasValue && optionCost < criteria.MinPoints.Value)
                    {
                        continue;
                    }

                    if (criteria.MaxPoints.HasValue && optionCost > criteria.MaxPoints.Value)
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
                            criteria))
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
        UnitFilterCriteria criteria)
    {
        if (!string.IsNullOrWhiteSpace(criteria.Characteristics) &&
            !OptionOrGroupContainsLookupName(profileGroupsRoot, profileGroup, option, "chars", charsLookup, criteria.Characteristics))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(criteria.Skills) &&
            !OptionOrGroupContainsLookupName(profileGroupsRoot, profileGroup, option, "skills", skillsLookup, criteria.Skills))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(criteria.Equipment) &&
            !OptionOrGroupContainsLookupName(profileGroupsRoot, profileGroup, option, "equip", equipLookup, criteria.Equipment))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(criteria.Weapons) &&
            !OptionOrGroupContainsLookupName(profileGroupsRoot, profileGroup, option, "weapons", weaponsLookup, criteria.Weapons))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(criteria.Ammo) &&
            !OptionContainsAnyLookupName(profileGroupsRoot, option, ["ammunition", "ammo"], ammoLookup, criteria.Ammo))
        {
            return false;
        }

        return true;
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
        if (string.IsNullOrWhiteSpace(_activeUnitFilter.Classification))
        {
            return true;
        }

        if (!unit.Type.HasValue || typeLookup.Count == 0)
        {
            return false;
        }

        return typeLookup.TryGetValue(unit.Type.Value, out var typeName) &&
               string.Equals(typeName, _activeUnitFilter.Classification, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsLieutenantOption(JsonElement option, IReadOnlyDictionary<int, string> skillsLookup)
    {
        if (HasLieutenantOrder(option))
        {
            return true;
        }

        if (option.TryGetProperty("name", out var nameElement) &&
            nameElement.ValueKind == JsonValueKind.String &&
            nameElement.GetString()?.Contains("lieutenant", StringComparison.OrdinalIgnoreCase) == true)
        {
            return true;
        }

        if (!option.TryGetProperty("skills", out var skillsElement) || skillsElement.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        foreach (var entry in skillsElement.EnumerateArray())
        {
            if (!TryParseId(entry, out var id))
            {
                continue;
            }

            if (skillsLookup.TryGetValue(id, out var name) &&
                name.Contains("lieutenant", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasLieutenantOrder(JsonElement option)
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

            if (string.Equals(typeElement.GetString(), "LIEUTENANT", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsPositiveSwc(string swc)
    {
        if (string.IsNullOrWhiteSpace(swc) || swc == "-")
        {
            return false;
        }

        return decimal.TryParse(
                   swc,
                   System.Globalization.NumberStyles.Number,
                   System.Globalization.CultureInfo.InvariantCulture,
                   out var value)
               && value > 0m;
    }

    private static bool TryParseId(JsonElement element, out int id)
    {
        if (element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out var numberId))
        {
            id = numberId;
            return true;
        }

        if (element.ValueKind == JsonValueKind.String &&
            int.TryParse(element.GetString(), out var stringId))
        {
            id = stringId;
            return true;
        }

        if (element.ValueKind == JsonValueKind.Object &&
            element.TryGetProperty("id", out var idElement))
        {
            return TryParseId(idElement, out id);
        }

        id = 0;
        return false;
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

    private static int GetUnitTypeSortIndex(int? unitType)
    {
        if (!unitType.HasValue)
        {
            return int.MaxValue - 1;
        }

        return UnitTypeSortOrder.TryGetValue(unitType.Value, out var sortIndex)
            ? sortIndex
            : int.MaxValue;
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
        _selectedUnitPicture?.Dispose();
        _selectedUnitPicture = null;
        SelectedUnitCanvas.InvalidateSurface();
    }
        _selectedUnitProfileGroupsJson = null;
        _selectedUnitFiltersJson = null;
        ResetUnitStatsOnly();
        EquipmentSummary = "Equipment: -";
        SpecialSkillsSummary = "Special Skills: -";
        _selectedUnitCommonEquipment = [];
        _selectedUnitCommonSkills = [];
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
        var merged = new List<string>();
        foreach (var value in commonValues)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                merged.Add(value.Trim());
            }
        }

        if (!string.IsNullOrWhiteSpace(uniqueValues) && uniqueValues != "-")
        {
            var uniqueParts = uniqueValues
                .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .Where(x => !string.Equals(x, "-", StringComparison.Ordinal))
                .Select(x => x.Trim());
            merged.AddRange(uniqueParts);
        }

        return merged
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private void ResetUnitStatsOnly()
    {
        _unitMoveFirstCm = null;
        _unitMoveSecondCm = null;
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
        _peripheralMoveFirstCm = null;
        _peripheralMoveSecondCm = null;
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
        if (profileGroupsRoot.ValueKind != JsonValueKind.Array)
        {
            yield break;
        }

        foreach (var group in profileGroupsRoot.EnumerateArray())
        {
            if (!group.TryGetProperty("options", out var options) || options.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var option in options.EnumerateArray())
            {
                yield return option.Clone();
            }
        }
    }

    private static bool TryGetFirstProfileGroup(string? profileGroupsJson, out JsonElement group)
    {
        group = default;
        if (string.IsNullOrWhiteSpace(profileGroupsJson))
        {
            return false;
        }

        try
        {
            using var doc = JsonDocument.Parse(profileGroupsJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Array || doc.RootElement.GetArrayLength() == 0)
            {
                return false;
            }

            group = doc.RootElement[0].Clone();
            return true;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ArmyFactionSelectionPage TryGetFirstProfileGroup failed: {ex.Message}");
            return false;
        }
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
        (_unitMoveFirstCm, _unitMoveSecondCm) = ParseMoveValues(selectedElement);
        UpdateUnitMoveDisplay();
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
        if (TryGetPropertyFlexible(profile, "move", out var moveElement) && moveElement.ValueKind == JsonValueKind.Array)
        {
            var moveParts = moveElement.EnumerateArray()
                .Select(ReadNumericString)
                .Where(x => !string.IsNullOrWhiteSpace(x) && x != "-")
                .ToList();

            if (moveParts.Count > 0)
            {
                return string.Join("-", moveParts);
            }
        }

        return ReadMove(profile);
    }

    private string FormatMoveValue(int? firstCm, int? secondCm)
    {
        if (!firstCm.HasValue || !secondCm.HasValue)
        {
            return "-";
        }

        if (_showUnitsInInches)
        {
            var first = (int)Math.Round(firstCm.Value / 2.5, MidpointRounding.AwayFromZero);
            var second = (int)Math.Round(secondCm.Value / 2.5, MidpointRounding.AwayFromZero);
            return $"{first}-{second}";
        }

        return $"{firstCm.Value}-{secondCm.Value}";
    }

    private static string ReplaceSubtitleMoveDisplay(string? subtitle, string moveDisplay)
    {
        if (string.IsNullOrWhiteSpace(subtitle))
        {
            return $"MOV {moveDisplay}";
        }

        return Regex.Replace(
            subtitle,
            @"(?<=\bMOV\s)[^|]+",
            moveDisplay,
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    private void UpdateUnitMoveDisplay()
    {
        UnitMov = FormatMoveValue(_unitMoveFirstCm, _unitMoveSecondCm);
    }

    private void UpdatePeripheralMoveDisplay()
    {
        PeripheralMov = FormatMoveValue(_peripheralMoveFirstCm, _peripheralMoveSecondCm);
    }

    private void PopulatePeripheralStatsFromElement(JsonElement selectedElement, string peripheralName)
    {
        var peripheralStats = BuildPeripheralStatBlock(peripheralName, selectedElement, _selectedUnitFiltersJson);
        if (peripheralStats is null)
        {
            return;
        }

        ApplyPeripheralStatBlock(peripheralStats);
    }

    private void UpdatePeripheralStatBlockFromVisibleProfiles()
    {
        ResetPeripheralStatsOnly();

        if (string.IsNullOrWhiteSpace(_selectedUnitProfileGroupsJson))
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
            using var doc = JsonDocument.Parse(_selectedUnitProfileGroupsJson);
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
        var extrasLookup = BuildExtrasLookup(filtersJson);
        var (moveFirstCm, moveSecondCm) = ParseMoveValues(peripheralProfile);
        var equipmentNames = GetOrderedIdDisplayNamesFromEntries(
            GetContainerEntries(peripheralProfile, "equip"),
            equipLookup,
            extrasLookup,
            _showUnitsInInches);
        var skillNames = BuildConfigurationSkillNames(
            GetOrderedIdDisplayNamesFromEntries(
                GetContainerEntries(peripheralProfile, "skills"),
                skillsLookup,
                extrasLookup,
                _showUnitsInInches));
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
        _peripheralMoveFirstCm = peripheralStats.MoveFirstCm;
        _peripheralMoveSecondCm = peripheralStats.MoveSecondCm;
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
        if (string.IsNullOrWhiteSpace(peripheralsText) || peripheralsText == "-")
        {
            return string.Empty;
        }

        var firstEntry = peripheralsText
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault();
        if (string.IsNullOrWhiteSpace(firstEntry))
        {
            return string.Empty;
        }

        return Regex.Replace(firstEntry, @"\s*\(\d+\)\s*$", string.Empty).Trim();
    }

    private static string NormalizePeripheralNameForDedupe(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value == "-")
        {
            return string.Empty;
        }

        return Regex.Replace(value, @"\s*\(\d+\)\s*$", string.Empty).Trim();
    }

    private static int GetPeripheralTotalCount(IEnumerable<string> peripheralNames)
    {
        var total = 0;
        foreach (var name in peripheralNames)
        {
            if (string.IsNullOrWhiteSpace(name) || name == "-")
            {
                continue;
            }

            var match = Regex.Match(name, @"\((\d+)\)\s*$");
            if (match.Success && int.TryParse(match.Groups[1].Value, out var parsedCount))
            {
                total += Math.Max(0, parsedCount);
            }
            else
            {
                total += 1;
            }
        }

        return total;
    }

    private static bool HasStatFields(JsonElement element)
    {
        return element.ValueKind == JsonValueKind.Object &&
               (element.TryGetProperty("cc", out _) ||
                element.TryGetProperty("bs", out _) ||
                element.TryGetProperty("ph", out _) ||
                element.TryGetProperty("wip", out _) ||
                element.TryGetProperty("arm", out _) ||
                element.TryGetProperty("bts", out _));
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
        if (!TryGetPropertyFlexible(container, propertyName, out var entriesElement) ||
            entriesElement.ValueKind != JsonValueKind.Array)
        {
            yield break;
        }

        foreach (var entry in entriesElement.EnumerateArray())
        {
            yield return entry.Clone();
        }
    }

    private static (int? firstCm, int? secondCm) ParseMoveValues(JsonElement element)
    {
        if (!TryGetPropertyFlexible(element, "move", out var moveElement) &&
            !TryGetPropertyFlexible(element, "mov", out moveElement))
        {
            return (null, null);
        }

        if (moveElement.ValueKind == JsonValueKind.String)
        {
            var parts = (moveElement.GetString() ?? string.Empty)
                .Split('-', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .Select(x => int.TryParse(x, out var parsed) ? (int?)parsed : null)
                .Where(x => x.HasValue)
                .Select(x => x!.Value)
                .ToList();

            return parts.Count >= 2 ? (parts[0], parts[1]) : (null, null);
        }

        if (moveElement.ValueKind != JsonValueKind.Array)
        {
            return (null, null);
        }

        var values = moveElement.EnumerateArray()
            .Where(x => x.ValueKind == JsonValueKind.Number && x.TryGetInt32(out _))
            .Select(x => x.GetInt32())
            .ToList();

        return values.Count >= 2 ? (values[0], values[1]) : (null, null);
    }

    private static (bool HasRegular, bool HasIrregular, bool HasImpetuous, bool HasTacticalAwareness) ParseUnitOrderTraits(JsonElement profileGroupsArray)
    {
        var hasRegular = false;
        var hasIrregular = false;
        var hasImpetuous = false;
        var hasTacticalAwareness = false;

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
                        hasTacticalAwareness = true;
                    }
                }
            }
        }

        return (hasRegular, hasIrregular, hasImpetuous, hasTacticalAwareness);
    }

    private static string ReadIntAsString(JsonElement option, string propertyName)
    {
        if (!TryGetPropertyFlexible(option, propertyName, out var element))
        {
            return "-";
        }

        if (element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out var value))
        {
            return value.ToString();
        }

        if (element.ValueKind == JsonValueKind.String && int.TryParse(element.GetString(), out var parsed))
        {
            return parsed.ToString();
        }

        return "-";
    }

    private static string ReadNumericString(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out var numericValue))
        {
            return numericValue.ToString();
        }

        if (element.ValueKind == JsonValueKind.String && int.TryParse(element.GetString(), out var parsedValue))
        {
            return parsedValue.ToString();
        }

        return "-";
    }

    private static string ReadMove(JsonElement option)
    {
        if (!TryGetPropertyFlexible(option, "mov", out var movElement))
        {
            return "-";
        }

        if (movElement.ValueKind == JsonValueKind.Array)
        {
            var parts = movElement.EnumerateArray()
                .Select(x => x.ValueKind == JsonValueKind.Number && x.TryGetInt32(out var n) ? n.ToString() : x.GetString())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();
            return parts.Count == 0 ? "-" : string.Join("-", parts);
        }

        if (movElement.ValueKind == JsonValueKind.String)
        {
            return movElement.GetString() ?? "-";
        }

        return "-";
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
        var variants = new[]
        {
            propertyName,
            propertyName.ToLowerInvariant(),
            propertyName.ToUpperInvariant(),
            char.ToUpperInvariant(propertyName[0]) + propertyName.Substring(1).ToLowerInvariant()
        };

        foreach (var variant in variants.Distinct(StringComparer.Ordinal))
        {
            if (element.TryGetProperty(variant, out value))
            {
                return true;
            }
        }

        foreach (var containerName in new[] { "stats", "Stats", "attributes", "Attributes", "attrs", "Attrs" })
        {
            if (!element.TryGetProperty(containerName, out var container) || container.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            foreach (var variant in variants.Distinct(StringComparer.Ordinal))
            {
                if (container.TryGetProperty(variant, out value))
                {
                    return true;
                }
            }
        }

        value = default;
        return false;
    }

    private async Task ApplyGlobalDisplayUnitsPreferenceAsync(CancellationToken cancellationToken = default)
    {
        if (_appSettingsService is null)
        {
            return;
        }

        try
        {
            var showInches = await _appSettingsService.GetShowUnitsInInchesAsync(cancellationToken);
            if (_showUnitsInInches == showInches)
            {
                return;
            }

            _showUnitsInInches = showInches;
            UpdateUnitMoveDisplay();
            UpdatePeripheralMoveDisplay();
            RefreshMercsCompanyEntryDistanceDisplays();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ArmyFactionSelectionPage ApplyGlobalDisplayUnitsPreferenceAsync failed: {ex.Message}");
        }
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
        var list = values
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var formatted = new FormattedString();
        formatted.Spans.Add(new Span { Text = $"{label}: " });
        if (list.Count == 0)
        {
            formatted.Spans.Add(new Span { Text = "-" });
            return formatted;
        }

        for (var i = 0; i < list.Count; i++)
        {
            if (i > 0)
            {
                formatted.Spans.Add(new Span { Text = ", " });
            }

            formatted.Spans.Add(new Span
            {
                Text = list[i],
                TextColor = highlightLieutenantPurple && list[i].Contains("lieutenant", StringComparison.OrdinalIgnoreCase)
                    ? Color.FromArgb("#C084FC")
                    : accentColor
            });
        }

        return formatted;
    }

    private static FormattedString BuildMercsCompanyLineFormatted(string label, string? value, Color accentColor)
    {
        var normalized = string.IsNullOrWhiteSpace(value)
            ? "-"
            : value
                .Replace("\r\n", ", ", StringComparison.Ordinal)
                .Replace("\n", ", ", StringComparison.Ordinal)
                .Replace("\r", ", ", StringComparison.Ordinal);

        var formatted = new FormattedString();
        formatted.Spans.Add(new Span
        {
            Text = $"{label}: "
        });
        formatted.Spans.Add(new Span
        {
            Text = normalized,
            TextColor = accentColor
        });
        return formatted;
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
        if (string.IsNullOrWhiteSpace(text) || text == "-")
        {
            return [];
        }

        return text
            .Replace("\r\n", ",", StringComparison.Ordinal)
            .Replace('\r', ',')
            .Replace('\n', ',')
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => !string.IsNullOrWhiteSpace(x) && x != "-")
            .ToList();
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

    private static List<string> ComputeCommonDisplayNamesFromProfiles(
        string? profileGroupsJson,
        string propertyName,
        IReadOnlyDictionary<int, string> lookup,
        IReadOnlyDictionary<int, ExtraDefinition> extrasLookup,
        bool showUnitsInInches)
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
                            if (!TryParseId(entry, out var id))
                            {
                                continue;
                            }

                            var baseName = lookup.TryGetValue(id, out var resolvedName) ? resolvedName : id.ToString();
                            set.Add(BuildEntryDisplayName(baseName, entry, extrasLookup, showUnitsInInches));
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
            Console.Error.WriteLine($"ArmyFactionSelectionPage ComputeCommonDisplayNamesFromProfiles failed for '{propertyName}': {ex.Message}");
            return [];
        }

        if (common is null || common.Count == 0)
        {
            return [];
        }

        return common
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

    private static List<string> IntersectDisplayNamesWithIncludes(
        JsonElement profileGroupsRoot,
        IReadOnlyList<JsonElement> options,
        string propertyName,
        IReadOnlyDictionary<int, string> lookup,
        IReadOnlyDictionary<int, ExtraDefinition> extrasLookup,
        bool showUnitsInInches)
    {
        HashSet<string>? intersection = null;
        foreach (var option in options)
        {
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in GetOptionEntriesWithIncludes(profileGroupsRoot, option, propertyName))
            {
                if (!TryParseId(entry, out var id))
                {
                    continue;
                }

                var baseName = lookup.TryGetValue(id, out var resolvedName) ? resolvedName : id.ToString();
                names.Add(BuildEntryDisplayName(baseName, entry, extrasLookup, showUnitsInInches));
            }

            if (intersection is null)
            {
                intersection = names;
            }
            else
            {
                intersection.IntersectWith(names);
            }
        }

        if (intersection is null || intersection.Count == 0)
        {
            return [];
        }

        return intersection
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static IEnumerable<JsonElement> GetOptionEntriesWithIncludes(
        JsonElement profileGroupsRoot,
        JsonElement option,
        string propertyName)
    {
        var collected = new List<JsonElement>();
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        CollectOptionEntriesWithIncludes(profileGroupsRoot, option, propertyName, collected, visited, null);
        return collected;
    }

    private static void CollectOptionEntriesWithIncludes(
        JsonElement profileGroupsRoot,
        JsonElement option,
        string propertyName,
        List<JsonElement> target,
        HashSet<string> visited,
        (int GroupId, int OptionId)? includeRef)
    {
        var key = includeRef.HasValue
            ? $"{includeRef.Value.GroupId}:{includeRef.Value.OptionId}"
            : option.GetRawText().GetHashCode().ToString();
        if (!visited.Add(key))
        {
            return;
        }

        if (option.TryGetProperty(propertyName, out var arr) && arr.ValueKind == JsonValueKind.Array)
        {
            foreach (var entry in arr.EnumerateArray())
            {
                target.Add(entry);
            }
        }

        if (!option.TryGetProperty("includes", out var includesElement) || includesElement.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var include in includesElement.EnumerateArray())
        {
            if (include.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            if (!TryParseIncludeReference(include, out var includeGroupId, out var includeOptionId))
            {
                continue;
            }

            var includedOption = FindIncludedOption(profileGroupsRoot, includeGroupId, includeOptionId);
            if (includedOption.HasValue)
            {
                CollectOptionEntriesWithIncludes(
                    profileGroupsRoot,
                    includedOption.Value,
                    propertyName,
                    target,
                    visited,
                    (includeGroupId, includeOptionId));
            }
        }
    }

    private static bool TryParseIncludeReference(JsonElement include, out int groupId, out int optionId)
    {
        groupId = 0;
        optionId = 0;

        if (!include.TryGetProperty("group", out var groupElement) || !TryParseId(groupElement, out groupId))
        {
            return false;
        }

        if (!include.TryGetProperty("option", out var optionElement) || !TryParseId(optionElement, out optionId))
        {
            return false;
        }

        return true;
    }

    private static JsonElement? FindIncludedOption(JsonElement profileGroupsRoot, int groupId, int optionId)
    {
        if (profileGroupsRoot.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (var group in profileGroupsRoot.EnumerateArray())
        {
            if (!group.TryGetProperty("id", out var groupIdElement) ||
                !TryParseId(groupIdElement, out var parsedGroupId) ||
                parsedGroupId != groupId)
            {
                continue;
            }

            if (!group.TryGetProperty("options", out var optionsElement) || optionsElement.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var option in optionsElement.EnumerateArray())
            {
                if (!option.TryGetProperty("id", out var optionIdElement) ||
                    !TryParseId(optionIdElement, out var parsedOptionId) ||
                    parsedOptionId != optionId)
                {
                    continue;
                }

                return option;
            }
        }

        return null;
    }

    private static Dictionary<int, ExtraDefinition> BuildExtrasLookup(string? filtersJson)
    {
        var map = new Dictionary<int, ExtraDefinition>();
        if (string.IsNullOrWhiteSpace(filtersJson))
        {
            return map;
        }

        try
        {
            using var doc = JsonDocument.Parse(filtersJson);
            if (!doc.RootElement.TryGetProperty("extras", out var section) || section.ValueKind != JsonValueKind.Array)
            {
                return map;
            }

            foreach (var entry in section.EnumerateArray())
            {
                if (!entry.TryGetProperty("id", out var idElement) || !TryParseId(idElement, out var id))
                {
                    continue;
                }

                if (!entry.TryGetProperty("name", out var nameElement) || nameElement.ValueKind != JsonValueKind.String)
                {
                    continue;
                }

                var name = nameElement.GetString();
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                var type = entry.TryGetProperty("type", out var typeElement) && typeElement.ValueKind == JsonValueKind.String
                    ? (typeElement.GetString() ?? string.Empty)
                    : string.Empty;

                map[id] = new ExtraDefinition(name, type);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ArmyFactionSelectionPage BuildExtrasLookup failed: {ex.Message}");
        }

        return map;
    }

    private static string BuildEntryDisplayName(
        string baseName,
        JsonElement entry,
        IReadOnlyDictionary<int, ExtraDefinition> extrasLookup,
        bool showUnitsInInches)
    {
        if (entry.ValueKind != JsonValueKind.Object)
        {
            return baseName;
        }

        if (!entry.TryGetProperty("extra", out var extraElement) || extraElement.ValueKind != JsonValueKind.Array)
        {
            return baseName;
        }

        var extras = new List<string>();
        foreach (var extraEntry in extraElement.EnumerateArray())
        {
            if (!TryParseId(extraEntry, out var extraId))
            {
                continue;
            }

            if (extrasLookup.TryGetValue(extraId, out var definition) &&
                !string.IsNullOrWhiteSpace(definition.Name))
            {
                extras.Add(FormatExtraDisplay(definition, showUnitsInInches));
            }
            else
            {
                extras.Add(extraId.ToString());
            }
        }

        var distinctExtras = extras
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return distinctExtras.Count == 0
            ? baseName
            : $"{baseName} ({string.Join(", ", distinctExtras)})";
    }

    private static string FormatExtraDisplay(ExtraDefinition definition, bool showUnitsInInches)
    {
        if (!string.Equals(definition.Type, "DISTANCE", StringComparison.OrdinalIgnoreCase))
        {
            return definition.Name;
        }

        return ConvertDistanceText(definition.Name, showUnitsInInches);
    }

    private static string ConvertDistanceText(string distanceText, bool showUnitsInInches)
    {
        if (string.IsNullOrWhiteSpace(distanceText))
        {
            return distanceText;
        }

        var match = Regex.Match(distanceText, @"([+-]?)(\d+(?:\.\d+)?)(?:\s*(?:cm|""|in|inch|inches))?", RegexOptions.IgnoreCase);
        if (!match.Success)
        {
            return distanceText;
        }

        var sign = match.Groups[1].Value;
        var numberText = match.Groups[2].Value;
        if (!double.TryParse(numberText, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var cm))
        {
            return distanceText;
        }

        string replacement;
        if (showUnitsInInches)
        {
            var inches = (int)Math.Round(cm / 2.5, MidpointRounding.AwayFromZero);
            replacement = $"{sign}{inches}\"";
        }
        else
        {
            var roundedCm = Math.Round(cm, 2, MidpointRounding.AwayFromZero);
            var cmText = Math.Abs(roundedCm - Math.Round(roundedCm)) < 0.001
                ? ((int)Math.Round(roundedCm)).ToString(System.Globalization.CultureInfo.InvariantCulture)
                : roundedCm.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
            replacement = $"{sign}{cmText}cm";
        }

        return distanceText.Remove(match.Index, match.Length).Insert(match.Index, replacement);
    }

    private void OnUnitListItemTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not Element element || element.BindingContext is not ArmyUnitSelectionItem item)
        {
            return;
        }

        SetSelectedUnit(item);
    }

    private void OnUnitItemTappedFromView(object? sender, EventArgs e)
    {
        if (sender is not FactionListItemView view || view.BindingContext is not ArmyUnitSelectionItem item)
        {
            Console.Error.WriteLine("ArmyFactionSelectionPage OnUnitItemTappedFromView: no unit binding context.");
            return;
        }

        SetSelectedUnit(item);
    }

    private void OnTeamAllowedProfileTappedFromView(object? sender, EventArgs e)
    {
        if (sender is not FactionListItemView view || view.BindingContext is not ArmyTeamUnitLimitItem teamItem)
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

        SetSelectedUnit(resolved);
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
                _leftSlotPicture?.Dispose();
                _leftSlotPicture = null;
            }
            else
            {
                _rightSlotPicture?.Dispose();
                _rightSlotPicture = null;
            }

            if (stream is null)
            {
                InvalidateSlotCanvas(slotIndex);
                return;
            }

            await using (stream)
            {
                var svg = new SKSvg();
                var picture = svg.Load(stream);
                if (slotIndex == 0)
                {
                    _leftSlotPicture = picture;
                }
                else
                {
                    _rightSlotPicture = picture;
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ArmyFactionSelectionPage slot icon load failed: {ex.Message}");
            if (slotIndex == 0)
            {
                _leftSlotPicture = null;
            }
            else
            {
                _rightSlotPicture = null;
            }
        }

        InvalidateSlotCanvas(slotIndex);
    }

    private async Task LoadHeaderIconsAsync()
    {
        _regularOrderIconPicture?.Dispose();
        _regularOrderIconPicture = null;
        _irregularOrderIconPicture?.Dispose();
        _irregularOrderIconPicture = null;
        _impetuousIconPicture?.Dispose();
        _impetuousIconPicture = null;
        _tacticalAwarenessIconPicture?.Dispose();
        _tacticalAwarenessIconPicture = null;
        _cubeIconPicture?.Dispose();
        _cubeIconPicture = null;
        _cube2IconPicture?.Dispose();
        _cube2IconPicture = null;
        _hackableIconPicture?.Dispose();
        _hackableIconPicture = null;
        _peripheralIconPicture?.Dispose();
        _peripheralIconPicture = null;
        _filterIconPicture?.Dispose();
        _filterIconPicture = null;

        try
        {
            await using var regularStream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/CBIcons/regular.svg");
            var regularSvg = new SKSvg();
            _regularOrderIconPicture = regularSvg.Load(regularStream);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ArmyFactionSelectionPage regular order icon load failed: {ex.Message}");
        }

        try
        {
            await using var irregularStream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/CBIcons/irregular.svg");
            var irregularSvg = new SKSvg();
            _irregularOrderIconPicture = irregularSvg.Load(irregularStream);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ArmyFactionSelectionPage irregular order icon load failed: {ex.Message}");
        }

        try
        {
            await using var impetuousStream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/CBIcons/impetuous.svg");
            var impetuousSvg = new SKSvg();
            _impetuousIconPicture = impetuousSvg.Load(impetuousStream);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ArmyFactionSelectionPage impetuous icon load failed: {ex.Message}");
        }

        try
        {
            await using var tacticalStream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/NonCBIcons/noun-double-arrows-7302616.svg");
            var tacticalSvg = new SKSvg();
            _tacticalAwarenessIconPicture = tacticalSvg.Load(tacticalStream);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ArmyFactionSelectionPage tactical awareness icon load failed: {ex.Message}");
        }

        try
        {
            await using var cubeStream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/CBIcons/cube.svg");
            var cubeSvg = new SKSvg();
            _cubeIconPicture = cubeSvg.Load(cubeStream);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ArmyFactionSelectionPage cube icon load failed: {ex.Message}");
        }

        try
        {
            await using var cube2Stream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/CBIcons/cube2.svg");
            var cube2Svg = new SKSvg();
            _cube2IconPicture = cube2Svg.Load(cube2Stream);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ArmyFactionSelectionPage cube2 icon load failed: {ex.Message}");
        }

        try
        {
            await using var hackableStream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/CBIcons/hackable.svg");
            var hackableSvg = new SKSvg();
            _hackableIconPicture = hackableSvg.Load(hackableStream);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ArmyFactionSelectionPage hackable icon load failed: {ex.Message}");
        }

        try
        {
            await using var peripheralStream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/CBIcons/peripheral.svg");
            var peripheralSvg = new SKSvg();
            _peripheralIconPicture = peripheralSvg.Load(peripheralStream);
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

        HeaderIconsCanvas.InvalidateSurface();
        UnitSelectionFilterCanvasInactive.InvalidateSurface();
        UnitSelectionFilterCanvasActive.InvalidateSurface();
        PeripheralHeaderIconCanvas?.InvalidateSurface();
    }

    private async Task LoadSeasonValidationIconsAsync()
    {
        _seasonCheckIconPicture?.Dispose();
        _seasonCheckIconPicture = null;
        _seasonXIconPicture?.Dispose();
        _seasonXIconPicture = null;

        try
        {
            await using var checkStream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/NonCBIcons/noun-check-3612574.svg");
            var checkSvg = new SKSvg();
            _seasonCheckIconPicture = checkSvg.Load(checkStream);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ArmyFactionSelectionPage season check icon load failed: {ex.Message}");
        }

        try
        {
            await using var xStream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/NonCBIcons/noun-x-1890844.svg");
            var xSvg = new SKSvg();
            _seasonXIconPicture = xSvg.Load(xStream);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ArmyFactionSelectionPage season x icon load failed: {ex.Message}");
        }

        UpdateSeasonValidationState();
        SeasonValidationCanvas.InvalidateSurface();
    }

    private void InvalidateSlotCanvas(int slotIndex)
    {
        if (slotIndex == 0)
        {
            LeftSlotCanvas.InvalidateSurface();
            return;
        }

        RightSlotCanvas.InvalidateSurface();
    }

    private void OnLeftSlotCanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        DrawSlotPicture(_leftSlotPicture, e);
    }

    private void OnRightSlotCanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        DrawSlotPicture(_rightSlotPicture, e);
    }

    private void OnSelectedUnitCanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        if (_selectedUnitPicture is null)
        {
            Console.Error.WriteLine($"ArmyFactionSelectionPage selected canvas paint: no picture (canvas={e.Info.Width}x{e.Info.Height}).");
        }
        else
        {
            var b = _selectedUnitPicture.CullRect;
            Console.WriteLine($"ArmyFactionSelectionPage selected canvas paint: canvas={e.Info.Width}x{e.Info.Height}, bounds=({b.Left},{b.Top},{b.Right},{b.Bottom}).");
        }

        DrawSlotPicture(_selectedUnitPicture, e);
    }

    private void OnHeaderIconsCanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Transparent);
        var pictures = BuildHeaderIconPictures();
        DrawIconColumn(canvas, e.Info, pictures);
    }

    private void OnSeasonValidationCanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var icon = _showSeasonCheckIcon ? _seasonCheckIconPicture : _seasonXIconPicture;
        DrawSlotPicture(icon, e);
    }

    private void OnUnitSelectionFilterCanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        DrawSlotPicture(_filterIconPicture, e);
    }

    private void OnPeripheralIconCanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        DrawSlotPicture(_peripheralIconPicture, e);
    }

    private void OnUnitNameHeadingLabelSizeChanged(object? sender, EventArgs e)
    {
        UpdateUnitNameHeadingFontSize();
    }

    private List<SKPicture> BuildTopIconPictures()
    {
        var pictures = new List<SKPicture>(3);
        var orderTypePicture = ShowIrregularOrderIcon ? _irregularOrderIconPicture : _regularOrderIconPicture;
        if ((ShowRegularOrderIcon || ShowIrregularOrderIcon) && orderTypePicture is not null)
        {
            pictures.Add(orderTypePicture);
        }

        if (ShowImpetuousIcon && _impetuousIconPicture is not null)
        {
            pictures.Add(_impetuousIconPicture);
        }

        if (ShowTacticalAwarenessIcon && _tacticalAwarenessIconPicture is not null)
        {
            pictures.Add(_tacticalAwarenessIconPicture);
        }

        return pictures;
    }

    private List<SKPicture> BuildBottomIconPictures()
    {
        var pictures = new List<SKPicture>(3);
        if (ShowCubeIcon && _cubeIconPicture is not null)
        {
            pictures.Add(_cubeIconPicture);
        }

        if (ShowCube2Icon && _cube2IconPicture is not null)
        {
            pictures.Add(_cube2IconPicture);
        }

        if (ShowHackableIcon && _hackableIconPicture is not null)
        {
            pictures.Add(_hackableIconPicture);
        }

        return pictures;
    }

    private List<SKPicture> BuildHeaderIconPictures()
    {
        var pictures = BuildTopIconPictures();
        pictures.AddRange(BuildBottomIconPictures());
        if (pictures.Count <= MaxHeaderIcons)
        {
            return pictures;
        }

        return pictures.Take(MaxHeaderIcons).ToList();
    }

    private static void DrawIconColumn(SKCanvas canvas, SKImageInfo info, IReadOnlyList<SKPicture> pictures)
    {
        if (pictures.Count == 0)
        {
            return;
        }

        var drawCount = pictures.Count;
        var totalGap = (drawCount - 1) * IconVerticalGap;
        var maxIconSizeFromHeight = (info.Height - totalGap) / drawCount;
        var iconSize = Math.Max(1f, Math.Min(IconSize, maxIconSizeFromHeight));
        var totalHeight = (drawCount * iconSize) + totalGap;
        var startY = (info.Height - totalHeight) / 2f;
        if (startY < 0f)
        {
            startY = 0f;
        }

        for (var i = 0; i < drawCount; i++)
        {
            var x = (info.Width - iconSize) / 2f;
            var y = startY + (i * (iconSize + IconVerticalGap));
            var destination = new SKRect(x, y, x + iconSize, y + iconSize);
            DrawPictureInRect(canvas, pictures[i], destination);
        }
    }

    private static void ApplyFilterButtonSize(Border? buttonBorder, SKCanvasView? iconCanvas, double iconButtonSize)
    {
        if (buttonBorder is null || iconCanvas is null || iconButtonSize <= 0)
        {
            return;
        }

        buttonBorder.WidthRequest = iconButtonSize;
        buttonBorder.HeightRequest = iconButtonSize;
        iconCanvas.WidthRequest = iconButtonSize;
        iconCanvas.HeightRequest = iconButtonSize;
    }

    private void UpdateUnitNameHeadingFontSize()
    {
        if (UnitNameHeadingLabel is null)
        {
            return;
        }

        var availableWidth = UnitNameHeadingLabel.Width;
        if (availableWidth <= 0)
        {
            UnitNameHeadingFontSize = UnitNameHeadingMaxFontSize;
            return;
        }

        var measuredWidth = double.PositiveInfinity;
        for (var size = UnitNameHeadingMaxFontSize; size >= UnitNameHeadingMinFontSize; size -= UnitNameHeadingFontStep)
        {
            UnitNameHeadingFontSize = size;
            measuredWidth = UnitNameHeadingLabel.Measure(double.PositiveInfinity, double.PositiveInfinity).Width;
            if (measuredWidth <= availableWidth)
            {
                return;
            }
        }

        UnitNameHeadingFontSize = UnitNameHeadingMinFontSize;
    }

    private async Task ApplyUnitHeaderColorsAsync(int sourceFactionId, ArmyUnitRecord? unit, CancellationToken cancellationToken)
    {
        if (_metadataAccessor is null)
        {
            ApplyUnitHeaderColorsByVanillaFactionName(null);
            return;
        }

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

    private async Task<string?> ResolveVanillaFactionNameAsync(int sourceFactionId, CancellationToken cancellationToken)
    {
        if (_metadataAccessor is null || sourceFactionId <= 0)
        {
            return null;
        }

        var current = await _metadataAccessor.GetFactionByIdAsync(sourceFactionId, cancellationToken);
        var safety = 0;
        while (current is not null && safety < 8)
        {
            // Prefer the first recognized themed faction while walking up the lineage.
            if (IsThemeFactionName(current.Name))
            {
                return current.Name;
            }

            if (current.ParentId <= 0)
            {
                break;
            }

            var parent = await _metadataAccessor.GetFactionByIdAsync(current.ParentId, cancellationToken);
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
            return inferredThemeName;
        }

        return current?.Name;
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
        EquipmentSummaryFormatted = BuildNamedSummaryFormatted("Equipment", _selectedUnitCommonEquipment, equipmentAccent);
        SpecialSkillsSummaryFormatted = BuildNamedSummaryFormatted(
            "Special Skills",
            _selectedUnitCommonSkills,
            skillsAccent,
            highlightLieutenantPurple: _summaryHighlightLieutenant);
    }

    private static (Color EquipmentAccent, Color SkillsAccent) GetSummaryAccentColorsForSecondaryBackground(Color secondaryBackground)
    {
        return IsLightColor(secondaryBackground)
            ? (EquipmentAccentOnLightSecondary, SkillsAccentOnLightSecondary)
            : (EquipmentAccentOnDarkSecondary, SkillsAccentOnDarkSecondary);
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
            _ => (DefaultHeaderPrimaryColor, DefaultHeaderSecondaryColor)
        };
    }

    private static bool IsThemeFactionName(string? factionName)
    {
        var key = NormalizeFactionName(factionName);
        return key is
            "panoceania" or
            "yujing" or
            "ariadna" or
            "haqqislam" or
            "nomads" or
            "combinedarmy" or
            "aleph" or
            "tohaa" or
            "nonalignedarmy" or
            "o12" or
            "jsa";
    }

    private static IReadOnlyList<int> ParseFactionIds(string? factionsJson)
    {
        if (string.IsNullOrWhiteSpace(factionsJson))
        {
            return Array.Empty<int>();
        }

        try
        {
            using var doc = JsonDocument.Parse(factionsJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
            {
                return Array.Empty<int>();
            }

            var ids = new List<int>();
            foreach (var element in doc.RootElement.EnumerateArray())
            {
                if (element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out var numericId))
                {
                    ids.Add(numericId);
                    continue;
                }

                if (element.ValueKind == JsonValueKind.String &&
                    int.TryParse(element.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var stringId))
                {
                    ids.Add(stringId);
                }
            }

            return ids;
        }
        catch
        {
            return Array.Empty<int>();
        }
    }

    private static string NormalizeFactionName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var cleaned = value.Trim().ToLowerInvariant();
        cleaned = cleaned.Replace("reinforcements", string.Empty, StringComparison.Ordinal)
                         .Replace("reinforcement", string.Empty, StringComparison.Ordinal)
                         .Trim();
        return cleaned switch
        {
            "yu jing" => "yujing",
            "combined army" => "combinedarmy",
            "non aligned army" => "nonalignedarmy",
            "non-aligned armies" => "nonalignedarmy",
            "non aligned armies" => "nonalignedarmy",
            "non-aligned army" => "nonalignedarmy",
            "japanese secessionist army" => "jsa",
            "o-12" => "o12",
            _ => new string(cleaned.Where(char.IsLetterOrDigit).ToArray())
        };
    }

    private static bool IsLightColor(Color color)
    {
        var luminance = (0.299 * color.Red) + (0.587 * color.Green) + (0.114 * color.Blue);
        return luminance >= 0.6;
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
        var bounds = picture.CullRect;
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        var scale = Math.Min(destination.Width / bounds.Width, destination.Height / bounds.Height);
        var drawnWidth = bounds.Width * scale;
        var drawnHeight = bounds.Height * scale;
        var translateX = destination.Left + ((destination.Width - drawnWidth) / 2f) - (bounds.Left * scale);
        var translateY = destination.Top + ((destination.Height - drawnHeight) / 2f) - (bounds.Top * scale);

        using var restore = new SKAutoCanvasRestore(canvas, true);
        canvas.Translate(translateX, translateY);
        canvas.Scale(scale);
        canvas.DrawPicture(picture);
    }

    private static void DrawSlotPicture(SKPicture? picture, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        if (picture is null)
        {
            return;
        }

        var bounds = picture.CullRect;
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        var width = e.Info.Width;
        var height = e.Info.Height;
        var scale = Math.Min(width / bounds.Width, height / bounds.Height);
        var x = (width - (bounds.Width * scale)) / 2f;
        var y = (height - (bounds.Height * scale)) / 2f;

        using var restore = new SKAutoCanvasRestore(canvas, true);
        canvas.Translate(x, y);
        canvas.Scale(scale);
        canvas.DrawPicture(picture);
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
}

public class ArmyTeamUnitLimitItem : BaseViewModel, IViewerListItem
{
    public string Name { get; init; } = string.Empty;
    public string Min { get; init; } = "0";
    public string Max { get; init; } = "0";
    public string? Slug { get; init; }
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

public sealed class CaptainUnitPopupInfo
{
    public string Name { get; init; } = string.Empty;
    public int Cost { get; init; }
    public string Statline { get; init; } = "-";
    public string RangedWeapons { get; init; } = "-";
    public string CcWeapons { get; init; } = "-";
    public string Skills { get; init; } = "-";
    public string Equipment { get; init; } = "-";
    public string? CachedLogoPath { get; init; }
    public string? PackagedLogoPath { get; init; }
}

public sealed class CaptainUpgradePopupContext
{
    public CaptainUnitPopupInfo Unit { get; init; } = new();
    public int OptionFactionId { get; init; }
    public string OptionFactionName { get; init; } = string.Empty;
    public List<string> WeaponOptions { get; init; } = [];
    public List<string> SkillOptions { get; init; } = [];
    public List<string> EquipmentOptions { get; init; } = [];
}

public sealed class ConfigureCaptainPopupPage : ContentPage
{
    private const double UnifiedPickerWidth = 280;
    private static readonly Color ModifiedStatColor = Color.FromArgb("#22C55E");
    private static readonly Color DefaultStatColor = Colors.White;
    private static readonly IReadOnlyDictionary<string, StatPickerDefinition> StatDefinitions = new Dictionary<string, StatPickerDefinition>(StringComparer.OrdinalIgnoreCase)
    {
        ["CC"] = new StatPickerDefinition([0, 2, 5, 10], [0, 2, 3, 5]),
        ["BS"] = new StatPickerDefinition([0, 1, 2, 3], [0, 2, 3, 5]),
        ["PH"] = new StatPickerDefinition([0, 1, 3], [0, 2, 3], 14),
        ["WIP"] = new StatPickerDefinition([0, 1, 3, 6], [0, 2, 3, 5], 15),
        ["ARM"] = new StatPickerDefinition([0, 1, 3], [0, 5, 5]),
        ["BTS"] = new StatPickerDefinition([0, 3, 6, 9], [0, 2, 3, 5]),
        ["VITA"] = new StatPickerDefinition([0, 1], [0, 10], 2),
        ["STR"] = new StatPickerDefinition([0, 1], [0, 10], 2)
    };
    private const string NoneChoice = "(None)";

    private readonly CaptainUpgradePopupContext _context;
    private readonly TaskCompletionSource<SavedImprovedCaptainStats?> _resultSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly SKCanvasView _logoCanvas;
    private readonly Picker _ccPicker;
    private readonly Picker _bsPicker;
    private readonly Picker _phPicker;
    private readonly Picker _wipPicker;
    private readonly Picker _armPicker;
    private readonly Picker _btsPicker;
    private readonly Picker _vitaPicker;
    private readonly Picker _weapon1Picker;
    private readonly Picker _weapon2Picker;
    private readonly Picker _weapon3Picker;
    private readonly Picker _skill1Picker;
    private readonly Picker _skill2Picker;
    private readonly Picker _skill3Picker;
    private readonly Picker _equipment1Picker;
    private readonly Picker _equipment2Picker;
    private readonly Picker _equipment3Picker;
    private readonly Label _rangedValueLabel;
    private readonly Label _ccValueLabel;
    private readonly Label _skillsValueLabel;
    private readonly Label _equipmentValueLabel;
    private readonly Label _upgradeOptionsHeaderLabel;
    private readonly Label _experienceRemainingLabel;
    private readonly Button _foundCompanyButton;
    private readonly IReadOnlyDictionary<string, int> _baseStats;
    private readonly Grid _statsGrid;
    private readonly List<string> _statGridOrder = [];
    private readonly Dictionary<string, string> _statGridBaseValues = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Label> _statGridValueLabels = new(StringComparer.OrdinalIgnoreCase);
    private readonly Entry _captainNameEntry;
    private readonly Label _captainNameHeadingLabel;
    private readonly SKCanvasView _editCaptainNameCanvas;
    private readonly SKCanvasView _saveCaptainNameCanvas;
    private readonly SKCanvasView _rejectCaptainNameCanvas;
    private string _captainNameCommitted = "Captain";
    private SKPicture? _logoPicture;
    private SKPicture? _editCaptainNamePicture;
    private SKPicture? _saveCaptainNamePicture;
    private SKPicture? _rejectCaptainNamePicture;
    private int _isClosing;

    private ConfigureCaptainPopupPage(CaptainUpgradePopupContext context)
    {
        _context = context;
        _baseStats = ParseBaseStats(context.Unit.Statline);
        var popupHeight = (DeviceDisplay.Current.MainDisplayInfo.Height / DeviceDisplay.Current.MainDisplayInfo.Density) * 0.8;
        BackgroundColor = Color.FromRgba(0, 0, 0, 180);
        Title = "Captain Configuration";

        _logoCanvas = new SKCanvasView
        {
            WidthRequest = 80,
            HeightRequest = 80,
            VerticalOptions = LayoutOptions.Start
        };
        _logoCanvas.PaintSurface += OnLogoCanvasPaintSurface;

        _ccPicker = BuildStatPicker("CC", ReadBaseStat("CC"));
        _bsPicker = BuildStatPicker("BS", ReadBaseStat("BS"));
        _phPicker = BuildStatPicker("PH", ReadBaseStat("PH"));
        _wipPicker = BuildStatPicker("WIP", ReadBaseStat("WIP"));
        _armPicker = BuildStatPicker("ARM", ReadBaseStat("ARM"));
        _btsPicker = BuildStatPicker("BTS", ReadBaseStat("BTS"));
        _vitaPicker = BuildStatPicker("VITA", ReadBaseStat("VITA", "STR", "W"));

        HookSelectionChanged(_ccPicker);
        HookSelectionChanged(_bsPicker);
        HookSelectionChanged(_phPicker);
        HookSelectionChanged(_wipPicker);
        HookSelectionChanged(_armPicker);
        HookSelectionChanged(_btsPicker);
        HookSelectionChanged(_vitaPicker);

        _weapon1Picker = BuildChoicePicker(context.WeaponOptions);
        _weapon2Picker = BuildChoicePicker(context.WeaponOptions);
        _weapon3Picker = BuildChoicePicker(context.WeaponOptions);
        _skill1Picker = BuildChoicePicker(context.SkillOptions);
        _skill2Picker = BuildChoicePicker(context.SkillOptions);
        _skill3Picker = BuildChoicePicker(context.SkillOptions);
        _equipment1Picker = BuildChoicePicker(context.EquipmentOptions);
        _equipment2Picker = BuildChoicePicker(context.EquipmentOptions);
        _equipment3Picker = BuildChoicePicker(context.EquipmentOptions);
        HookSelectionChanged(_weapon1Picker);
        HookSelectionChanged(_weapon2Picker);
        HookSelectionChanged(_weapon3Picker);
        HookSelectionChanged(_skill1Picker);
        HookSelectionChanged(_skill2Picker);
        HookSelectionChanged(_skill3Picker);
        HookSelectionChanged(_equipment1Picker);
        HookSelectionChanged(_equipment2Picker);
        HookSelectionChanged(_equipment3Picker);

        var cancelButton = new Button
        {
            Text = "BACK",
            BackgroundColor = Color.FromArgb("#374151"),
            TextColor = Colors.White,
            Command = new Command(async () => await CloseAsync(false))
        };
        _foundCompanyButton = new Button
        {
            Text = "FOUND COMPANY",
            BackgroundColor = Color.FromArgb("#7C3AED"),
            TextColor = Colors.Black,
            Command = new Command(async () => await CloseAsync(true))
        };

        var actions = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            },
            ColumnSpacing = 10,
            HorizontalOptions = LayoutOptions.Fill,
            Children = { cancelButton, _foundCompanyButton }
        };
        Grid.SetColumn(cancelButton, 0);
        Grid.SetColumn(_foundCompanyButton, 2);

        var rangedBlock = BuildProfileDetailBlock("Ranged", Color.FromArgb("#EF4444"), out _rangedValueLabel);
        var ccBlock = BuildProfileDetailBlock("CC", Color.FromArgb("#22C55E"), out _ccValueLabel);
        var skillsBlock = BuildProfileDetailBlock("Skills", Color.FromArgb("#F59E0B"), out _skillsValueLabel);
        var equipmentBlock = BuildProfileDetailBlock("Equipment", Color.FromArgb("#06B6D4"), out _equipmentValueLabel);

        _captainNameEntry = new Entry
        {
            Text = _captainNameCommitted,
            IsReadOnly = true,
            FontSize = 22,
            HorizontalOptions = LayoutOptions.Fill
        };
        _captainNameHeadingLabel = new Label
        {
            Text = _captainNameCommitted,
            FontAttributes = FontAttributes.Bold,
            FontSize = 22,
            LineBreakMode = LineBreakMode.WordWrap
        };
        _editCaptainNameCanvas = BuildCaptainNameIconCanvas(OnEditCaptainNameTapped);
        _editCaptainNameCanvas.PaintSurface += OnEditCaptainNameCanvasPaintSurface;
        _saveCaptainNameCanvas = BuildCaptainNameIconCanvas(OnSaveCaptainNameTapped);
        _saveCaptainNameCanvas.PaintSurface += OnSaveCaptainNameCanvasPaintSurface;
        _rejectCaptainNameCanvas = BuildCaptainNameIconCanvas(OnRejectCaptainNameTapped);
        _rejectCaptainNameCanvas.PaintSurface += OnRejectCaptainNameCanvasPaintSurface;
        SetCaptainNameEditMode(isEditing: false);

        var captainNameRow = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Auto }
            },
            ColumnSpacing = 8,
            Children =
            {
                _captainNameEntry,
                _editCaptainNameCanvas,
                _saveCaptainNameCanvas,
                _rejectCaptainNameCanvas
            }
        };
        Grid.SetColumn(_captainNameEntry, 0);
        Grid.SetColumn(_editCaptainNameCanvas, 1);
        Grid.SetColumn(_saveCaptainNameCanvas, 2);
        Grid.SetColumn(_rejectCaptainNameCanvas, 3);

        _statsGrid = BuildStatsGrid(_context.Unit.Statline);

        var leftColumn = new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                captainNameRow,
                _logoCanvas,
                _captainNameHeadingLabel,
                _statsGrid,
                rangedBlock,
                ccBlock,
                skillsBlock,
                equipmentBlock
            }
        };

        _upgradeOptionsHeaderLabel = new Label
        {
            FontAttributes = FontAttributes.Bold,
            FontSize = 18,
            TextColor = Colors.White
        };
        _experienceRemainingLabel = new Label
        {
            FontAttributes = FontAttributes.Bold,
            FontSize = 15,
            TextColor = Colors.White
        };

        var rightColumnBody = new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                BuildStatRowPair(("CC", _ccPicker), ("BS", _bsPicker)),
                BuildStatRowPair(("PH", _phPicker), ("WIP", _wipPicker)),
                BuildStatRowPair(("ARM", _armPicker), ("BTS", _btsPicker)),
                BuildStatRowPair(("VITA", _vitaPicker), null),
                BuildCategorySection("Weapons", _weapon1Picker, _weapon2Picker, _weapon3Picker),
                BuildCategorySection("Skills", _skill1Picker, _skill2Picker, _skill3Picker),
                BuildCategorySection("Equipment", _equipment1Picker, _equipment2Picker, _equipment3Picker)
            }
        };

        var rightBodyScroll = new ScrollView { Content = rightColumnBody };
        var leftScroll = new ScrollView { Content = leftColumn };
        var rightColumn = new Grid
        {
            RowDefinitions = new RowDefinitionCollection
            {
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Star }
            },
            RowSpacing = 6,
            Children =
            {
                _upgradeOptionsHeaderLabel,
                _experienceRemainingLabel,
                rightBodyScroll
            }
        };
        Grid.SetRow(_experienceRemainingLabel, 1);
        Grid.SetRow(rightBodyScroll, 2);
        var columnsGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Star }
            },
            ColumnSpacing = 18,
            Children = { leftScroll, rightColumn }
        };
        Grid.SetColumn(rightColumn, 1);

        var cardContent = new Grid
        {
            WidthRequest = 980,
            RowDefinitions = new RowDefinitionCollection
            {
                new RowDefinition { Height = GridLength.Star },
                new RowDefinition { Height = GridLength.Auto }
            },
            RowSpacing = 14,
            Children = { columnsGrid, actions }
        };
        var actionsHost = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Star }
            },
            Children = { actions }
        };
        Grid.SetColumn(actions, 0);
        Grid.SetRow(actions, 1);
        cardContent.Children.Remove(actions);
        cardContent.Children.Add(actionsHost);
        Grid.SetRow(actionsHost, 1);

        var card = new Border
        {
            BackgroundColor = Color.FromArgb("#111827"),
            Stroke = Color.FromArgb("#374151"),
            StrokeThickness = 1,
            Padding = new Thickness(16),
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            HeightRequest = popupHeight,
            Content = cardContent
        };

        Content = new Grid
        {
            Children = { card }
        };

        UpdateProfilePreviewFromSelections();
        _ = LoadLogoAsync();
        _ = LoadCaptainNameActionIconsAsync();
    }

    public static async Task<SavedImprovedCaptainStats?> ShowAsync(INavigation navigation, CaptainUpgradePopupContext context)
    {
        var page = new ConfigureCaptainPopupPage(context);
        await navigation.PushModalAsync(page, false);
        return await page._resultSource.Task;
    }

    protected override bool OnBackButtonPressed()
    {
        _ = CloseAsync(false);
        return true;
    }

    private async Task LoadLogoAsync()
    {
        _logoPicture?.Dispose();
        _logoPicture = null;

        try
        {
            Stream? stream = null;
            if (!string.IsNullOrWhiteSpace(_context.Unit.CachedLogoPath) && File.Exists(_context.Unit.CachedLogoPath))
            {
                stream = File.OpenRead(_context.Unit.CachedLogoPath);
            }
            else if (!string.IsNullOrWhiteSpace(_context.Unit.PackagedLogoPath))
            {
                stream = await FileSystem.Current.OpenAppPackageFileAsync(_context.Unit.PackagedLogoPath);
            }

            if (stream is null)
            {
                _logoCanvas.InvalidateSurface();
                return;
            }

            await using (stream)
            {
                var svg = new SKSvg();
                _logoPicture = svg.Load(stream);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ConfigureCaptainPopupPage LoadLogoAsync failed: {ex.Message}");
            _logoPicture = null;
        }

        _logoCanvas.InvalidateSurface();
    }

    private void OnLogoCanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        if (_logoPicture is null)
        {
            return;
        }

        var bounds = _logoPicture.CullRect;
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        var scale = Math.Min(e.Info.Width / bounds.Width, e.Info.Height / bounds.Height);
        var x = (e.Info.Width - (bounds.Width * scale)) / 2f;
        var y = (e.Info.Height - (bounds.Height * scale)) / 2f;

        canvas.Translate(x, y);
        canvas.Scale(scale);
        canvas.DrawPicture(_logoPicture);
    }

    private async Task CloseAsync(bool confirmed)
    {
        if (Interlocked.Exchange(ref _isClosing, 1) == 1)
        {
            return;
        }

        if (!confirmed)
        {
            _resultSource.TrySetResult(null);
            DisposeCaptainNameActionIcons();
            await DismissModalIfTopAsync();

            return;
        }

        CommitCaptainNameFromEntry();

        var stats = new SavedImprovedCaptainStats
        {
            IsEnabled = true,
            CaptainName = _captainNameCommitted,
            CcTier = ReadStatTier(_ccPicker),
            BsTier = ReadStatTier(_bsPicker),
            PhTier = ReadStatTier(_phPicker),
            WipTier = ReadStatTier(_wipPicker),
            ArmTier = ReadStatTier(_armPicker),
            BtsTier = ReadStatTier(_btsPicker),
            VitalityTier = ReadStatTier(_vitaPicker),
            CcBonus = ReadStatBonus(_ccPicker),
            BsBonus = ReadStatBonus(_bsPicker),
            PhBonus = ReadStatBonus(_phPicker),
            WipBonus = ReadStatBonus(_wipPicker),
            ArmBonus = ReadStatBonus(_armPicker),
            BtsBonus = ReadStatBonus(_btsPicker),
            VitalityBonus = ReadStatBonus(_vitaPicker),
            WeaponChoice1 = ReadChoice(_weapon1Picker),
            WeaponChoice2 = ReadChoice(_weapon2Picker),
            WeaponChoice3 = ReadChoice(_weapon3Picker),
            SkillChoice1 = ReadChoice(_skill1Picker),
            SkillChoice2 = ReadChoice(_skill2Picker),
            SkillChoice3 = ReadChoice(_skill3Picker),
            EquipmentChoice1 = ReadChoice(_equipment1Picker),
            EquipmentChoice2 = ReadChoice(_equipment2Picker),
            EquipmentChoice3 = ReadChoice(_equipment3Picker),
            OptionFactionId = _context.OptionFactionId,
            OptionFactionName = _context.OptionFactionName
        };

        _resultSource.TrySetResult(stats);
        DisposeCaptainNameActionIcons();
        await DismissModalIfTopAsync();
    }

    private async Task DismissModalIfTopAsync()
    {
        try
        {
            var navigation = Navigation;
            var modalStack = navigation?.ModalStack;
            if (modalStack is null || modalStack.Count == 0)
            {
                return;
            }

            if (!ReferenceEquals(modalStack[^1], this))
            {
                return;
            }

            if (navigation is null)
            {
                return;
            }

            await navigation.PopModalAsync(false);
        }
        catch (ArgumentException ex) when (ex.Message.Contains("Ambiguous routes matched", StringComparison.OrdinalIgnoreCase))
        {
            Console.Error.WriteLine($"ConfigureCaptainPopupPage DismissModalIfTopAsync ignored ambiguous route pop: {ex.Message}");
        }
    }

    private static Picker BuildStatPicker(string statName, int baseValue)
    {
        var options = BuildStatOptions(statName, baseValue);
        var picker = new Picker
        {
            HorizontalOptions = LayoutOptions.Fill,
            HorizontalTextAlignment = TextAlignment.Center,
            ItemsSource = options,
            ItemDisplayBinding = new Binding(nameof(StatPickerOption.Label)),
            SelectedIndex = 0
        };

        return picker;
    }

    private static Picker BuildChoicePicker(IEnumerable<string> options)
    {
        var values = new List<string> { NoneChoice };
        values.AddRange(options.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase));

        return new Picker
        {
            WidthRequest = UnifiedPickerWidth,
            HorizontalOptions = LayoutOptions.Start,
            HorizontalTextAlignment = TextAlignment.Center,
            ItemsSource = values,
            SelectedIndex = 0
        };
    }

    private static View BuildStatRow(string label, Picker picker)
    {
        return picker;
    }

    private static View BuildStatRowPair((string Label, Picker Picker) first, (string Label, Picker Picker)? second)
    {
        var grid = new Grid
        {
            ColumnSpacing = 8,
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Star }
            }
        };

        var firstCell = BuildStatRow(first.Label, first.Picker);
        grid.Children.Add(firstCell);
        Grid.SetColumn(firstCell, 0);

        if (second.HasValue)
        {
            var secondCell = BuildStatRow(second.Value.Label, second.Value.Picker);
            grid.Children.Add(secondCell);
            Grid.SetColumn(secondCell, 1);
        }

        return grid;
    }

    private static View BuildCategorySection(string title, Picker first, Picker second, Picker third)
    {
        return new VerticalStackLayout
        {
            Spacing = 4,
            Margin = new Thickness(0, 8, 0, 0),
            Children =
            {
                new Label { Text = title, FontAttributes = FontAttributes.Bold },
                first,
                second,
                third
            }
        };
    }

    private static int ReadStatTier(Picker picker)
    {
        return picker.SelectedItem is StatPickerOption option ? option.Tier : 0;
    }

    private static int ReadStatBonus(Picker picker)
    {
        return picker.SelectedItem is StatPickerOption option ? option.Bonus : 0;
    }

    private static string ReadChoice(Picker picker)
    {
        var value = picker.SelectedItem?.ToString() ?? string.Empty;
        if (string.Equals(value, NoneChoice, StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        var normalized = Regex.Replace(value, @"^\s*\([-+]?\d+\)\s*-\s*", string.Empty).Trim();
        return normalized;
    }

    private static string NormalizeText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "-" : value;
    }

    private static View BuildProfileDetailBlock(string label, Color valueColor, out Label valueLabel)
    {
        valueLabel = new Label
        {
            Text = "-",
            FontSize = 19,
            TextColor = valueColor,
            HorizontalTextAlignment = TextAlignment.End,
            LineBreakMode = LineBreakMode.WordWrap
        };

        return new VerticalStackLayout
        {
            Spacing = 1,
            Children =
            {
                new Label
                {
                    Text = $"{label}:",
                    FontSize = 22,
                    LineBreakMode = LineBreakMode.WordWrap
                },
                valueLabel
            }
        };
    }

    private void HookSelectionChanged(Picker picker)
    {
        picker.SelectedIndexChanged += (_, _) => UpdateProfilePreviewFromSelections();
    }

    private void UpdateProfilePreviewFromSelections()
    {
        UpdateStatlinePreview();
        _rangedValueLabel.Text = BuildUpdatedProfileSection(
            _context.Unit.RangedWeapons,
            GetSelectedChoices(_weapon1Picker, _weapon2Picker, _weapon3Picker),
            prependPlus: true);
        _ccValueLabel.Text = NormalizeText(_context.Unit.CcWeapons);
        _skillsValueLabel.Text = BuildUpdatedProfileSection(
            _context.Unit.Skills,
            GetSelectedChoices(_skill1Picker, _skill2Picker, _skill3Picker),
            prependPlus: true);
        _equipmentValueLabel.Text = BuildUpdatedProfileSection(
            _context.Unit.Equipment,
            GetSelectedChoices(_equipment1Picker, _equipment2Picker, _equipment3Picker),
            prependPlus: true);
        UpdateUpgradeOptionsHeader();
    }

    private void UpdateUpgradeOptionsHeader()
    {
        var baseExperience = Math.Max(0, 28 - _context.Unit.Cost);
        var selectedCost =
            ReadStatPoints(_ccPicker) +
            ReadStatPoints(_bsPicker) +
            ReadStatPoints(_phPicker) +
            ReadStatPoints(_wipPicker) +
            ReadStatPoints(_armPicker) +
            ReadStatPoints(_btsPicker) +
            ReadStatPoints(_vitaPicker) +
            ReadChoicePoints(_weapon1Picker) +
            ReadChoicePoints(_weapon2Picker) +
            ReadChoicePoints(_weapon3Picker) +
            ReadChoicePoints(_skill1Picker) +
            ReadChoicePoints(_skill2Picker) +
            ReadChoicePoints(_skill3Picker) +
            ReadChoicePoints(_equipment1Picker) +
            ReadChoicePoints(_equipment2Picker) +
            ReadChoicePoints(_equipment3Picker);
        var experienceRemaining = baseExperience - selectedCost;

        _upgradeOptionsHeaderLabel.Text = $"Upgrade Options ({_context.OptionFactionName})";
        _experienceRemainingLabel.Text = $"Exp Remaining: {experienceRemaining}";
        _experienceRemainingLabel.TextColor = experienceRemaining < 0 ? Colors.Red : Colors.White;
        _foundCompanyButton.IsEnabled = experienceRemaining >= 0;
        _foundCompanyButton.BackgroundColor = experienceRemaining < 0 ? Color.FromArgb("#6B7280") : Color.FromArgb("#7C3AED");
    }

    private void UpdateStatlinePreview()
    {
        UpdateStatsGridValues();
    }

    private int ReadBaseStat(params string[] statNames)
    {
        foreach (var statName in statNames)
        {
            if (_baseStats.TryGetValue(statName, out var value))
            {
                return value;
            }
        }

        return 0;
    }

    private static List<StatPickerOption> BuildStatOptions(string statName, int baseValue)
    {
        if (!StatDefinitions.TryGetValue(statName, out var definition))
        {
            return [new StatPickerOption(statName, 0, 0, 0)];
        }

        var options = new List<StatPickerOption>
        {
            new(statName, 0, 0, 0)
        };

        var currentValue = baseValue;
        var cumulativeCost = 0;
        for (var tier = 1; tier <= definition.MaxTier; tier++)
        {
            if (definition.HardCap.HasValue && currentValue >= definition.HardCap.Value)
            {
                break;
            }

            var targetValue = baseValue + definition.BonusesByTier[tier];
            if (definition.HardCap.HasValue)
            {
                targetValue = Math.Min(targetValue, definition.HardCap.Value);
            }

            var appliedBonus = Math.Max(0, targetValue - baseValue);
            cumulativeCost += definition.CostsByTier[tier];
            options.Add(new StatPickerOption(statName, tier, appliedBonus, cumulativeCost));
            currentValue = targetValue;
        }

        return options;
    }

    private static int ReadStatPoints(Picker picker)
    {
        return picker.SelectedItem is StatPickerOption option ? option.Cost : 0;
    }

    private static IReadOnlyDictionary<string, int> ParseBaseStats(string? statline)
    {
        var values = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(statline))
        {
            return values;
        }

        var matches = Regex.Matches(statline, @"\b(CC|BS|PH|WIP|ARM|BTS|VITA|STR|W)\s+(\d+)\b", RegexOptions.IgnoreCase);
        foreach (Match match in matches)
        {
            if (!match.Success)
            {
                continue;
            }

            var key = match.Groups[1].Value.ToUpperInvariant();
            if (int.TryParse(match.Groups[2].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            {
                values[key] = parsed;
            }
        }

        return values;
    }

    private Grid BuildStatsGrid(string? statline)
    {
        var entries = ParseStatsGridEntries(statline);
        var grid = new Grid
        {
            ColumnSpacing = 10,
            RowSpacing = 2,
            RowDefinitions = new RowDefinitionCollection
            {
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Auto }
            }
        };

        if (entries.Count == 0)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            var empty = new Label
            {
                Text = "-",
                FontSize = 19,
                LineBreakMode = LineBreakMode.WordWrap
            };
            grid.Children.Add(empty);
            Grid.SetRow(empty, 0);
            return grid;
        }

        for (var i = 0; i < entries.Count; i++)
        {
            var (key, value) = entries[i];
            _statGridOrder.Add(key);
            _statGridBaseValues[key] = value;
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });

            var keyLabel = new Label
            {
                Text = key,
                FontSize = 19,
                FontAttributes = FontAttributes.Bold,
                HorizontalTextAlignment = TextAlignment.Center
            };
            var valueLabel = new Label
            {
                Text = value,
                FontSize = 19,
                HorizontalTextAlignment = TextAlignment.Center,
                TextColor = DefaultStatColor
            };

            _statGridValueLabels[key] = valueLabel;
            grid.Children.Add(keyLabel);
            grid.Children.Add(valueLabel);
            Grid.SetColumn(keyLabel, i);
            Grid.SetRow(keyLabel, 0);
            Grid.SetColumn(valueLabel, i);
            Grid.SetRow(valueLabel, 1);
        }

        return grid;
    }

    private static List<(string Key, string Value)> ParseStatsGridEntries(string? statline)
    {
        var entries = new List<(string Key, string Value)>();
        if (string.IsNullOrWhiteSpace(statline))
        {
            return entries;
        }

        foreach (var segment in statline.Split('|', StringSplitOptions.TrimEntries))
        {
            var match = Regex.Match(segment, @"^\s*([A-Za-z]+)\s+(.+)\s*$", RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                continue;
            }

            entries.Add((match.Groups[1].Value.ToUpperInvariant(), match.Groups[2].Value.Trim()));
        }

        return entries;
    }

    private void UpdateStatsGridValues()
    {
        foreach (var key in _statGridOrder)
        {
            if (!_statGridValueLabels.TryGetValue(key, out var valueLabel) ||
                !_statGridBaseValues.TryGetValue(key, out var rawValue))
            {
                continue;
            }

            if (int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var numericBase))
            {
                var bonus = ReadStatlineBonus(key);
                var modifiedValue = numericBase + bonus;
                valueLabel.Text = modifiedValue.ToString(CultureInfo.InvariantCulture);
                valueLabel.TextColor = modifiedValue == numericBase ? DefaultStatColor : ModifiedStatColor;
            }
            else
            {
                valueLabel.Text = rawValue;
                valueLabel.TextColor = DefaultStatColor;
            }
        }
    }

    private int ReadStatlineBonus(string statKey)
    {
        return statKey switch
        {
            "CC" => ReadStatBonus(_ccPicker),
            "BS" => ReadStatBonus(_bsPicker),
            "PH" => ReadStatBonus(_phPicker),
            "WIP" => ReadStatBonus(_wipPicker),
            "ARM" => ReadStatBonus(_armPicker),
            "BTS" => ReadStatBonus(_btsPicker),
            "VITA" or "STR" or "W" => ReadStatBonus(_vitaPicker),
            _ => 0
        };
    }

    private static List<string> GetSelectedChoices(params Picker[] pickers)
    {
        return pickers
            .Select(ReadChoice)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static SKCanvasView BuildCaptainNameIconCanvas(EventHandler<TappedEventArgs> tappedHandler)
    {
        var canvas = new SKCanvasView
        {
            WidthRequest = 28,
            HeightRequest = 28,
            VerticalOptions = LayoutOptions.Center
        };
        var tap = new TapGestureRecognizer();
        tap.Tapped += tappedHandler;
        canvas.GestureRecognizers.Add(tap);
        return canvas;
    }

    private void SetCaptainNameEditMode(bool isEditing)
    {
        _captainNameEntry.IsEnabled = isEditing;
        _captainNameEntry.IsReadOnly = !isEditing;
        _editCaptainNameCanvas.IsVisible = !isEditing;
        _saveCaptainNameCanvas.IsVisible = isEditing;
        _rejectCaptainNameCanvas.IsVisible = isEditing;
    }

    private void OnEditCaptainNameTapped(object? sender, TappedEventArgs e)
    {
        SetCaptainNameEditMode(isEditing: true);
        _captainNameEntry.Focus();
    }

    private void OnSaveCaptainNameTapped(object? sender, TappedEventArgs e)
    {
        CommitCaptainNameFromEntry();
    }

    private void OnRejectCaptainNameTapped(object? sender, TappedEventArgs e)
    {
        _captainNameEntry.Text = _captainNameCommitted;
        SetCaptainNameEditMode(isEditing: false);
    }

    private void CommitCaptainNameFromEntry()
    {
        var normalized = string.IsNullOrWhiteSpace(_captainNameEntry.Text) ? "Captain" : _captainNameEntry.Text.Trim();
        _captainNameCommitted = normalized;
        _captainNameEntry.Text = _captainNameCommitted;
        _captainNameHeadingLabel.Text = _captainNameCommitted;
        SetCaptainNameEditMode(isEditing: false);
    }

    private async Task LoadCaptainNameActionIconsAsync()
    {
        DisposeCaptainNameActionIcons();

        try
        {
            await using var editStream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/NonCBIcons/noun-edit-333556.svg");
            var svg = new SKSvg();
            _editCaptainNamePicture = svg.Load(editStream);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ConfigureCaptainPopupPage edit icon load failed: {ex.Message}");
            _editCaptainNamePicture = null;
        }

        try
        {
            await using var saveStream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/NonCBIcons/noun-check-3612574.svg");
            var svg = new SKSvg();
            _saveCaptainNamePicture = svg.Load(saveStream);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ConfigureCaptainPopupPage save icon load failed: {ex.Message}");
            _saveCaptainNamePicture = null;
        }

        try
        {
            await using var rejectStream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/NonCBIcons/noun-x-1890844.svg");
            var svg = new SKSvg();
            _rejectCaptainNamePicture = svg.Load(rejectStream);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ConfigureCaptainPopupPage reject icon load failed: {ex.Message}");
            _rejectCaptainNamePicture = null;
        }

        _editCaptainNameCanvas.InvalidateSurface();
        _saveCaptainNameCanvas.InvalidateSurface();
        _rejectCaptainNameCanvas.InvalidateSurface();
    }

    private void DisposeCaptainNameActionIcons()
    {
        _editCaptainNamePicture?.Dispose();
        _editCaptainNamePicture = null;
        _saveCaptainNamePicture?.Dispose();
        _saveCaptainNamePicture = null;
        _rejectCaptainNamePicture?.Dispose();
        _rejectCaptainNamePicture = null;
    }

    private static void DrawActionIcon(SKCanvas canvas, SKImageInfo info, SKPicture? picture)
    {
        canvas.Clear(SKColors.Transparent);
        if (picture is null)
        {
            return;
        }

        var bounds = picture.CullRect;
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        var scale = Math.Min(info.Width / bounds.Width, info.Height / bounds.Height);
        var x = (info.Width - (bounds.Width * scale)) / 2f;
        var y = (info.Height - (bounds.Height * scale)) / 2f;
        canvas.Translate(x, y);
        canvas.Scale(scale);
        canvas.DrawPicture(picture);
    }

    private void OnEditCaptainNameCanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        DrawActionIcon(e.Surface.Canvas, e.Info, _editCaptainNamePicture);
    }

    private void OnSaveCaptainNameCanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        DrawActionIcon(e.Surface.Canvas, e.Info, _saveCaptainNamePicture);
    }

    private void OnRejectCaptainNameCanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        DrawActionIcon(e.Surface.Canvas, e.Info, _rejectCaptainNamePicture);
    }

    private static int ReadChoicePoints(Picker picker)
    {
        var rawValue = picker.SelectedItem?.ToString() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(rawValue) ||
            string.Equals(rawValue, NoneChoice, StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        var match = Regex.Match(rawValue, @"^\s*\(([-+]?\d+)\)");
        if (!match.Success)
        {
            return 0;
        }

        return int.TryParse(match.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : 0;
    }

    private static string BuildUpdatedProfileSection(string? baseText, IReadOnlyList<string> additions, bool prependPlus)
    {
        var lines = SplitProfileText(baseText);
        foreach (var addition in additions)
        {
            lines.Add(prependPlus ? $"+ {addition}" : addition);
        }

        return lines.Count == 0 ? "-" : string.Join(Environment.NewLine, lines);
    }

    private static List<string> SplitProfileText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        return text
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x) && x != "-")
            .ToList();
    }

}

public sealed record StatPickerDefinition(IReadOnlyList<int> BonusesByTier, IReadOnlyList<int> CostsByTier, int? HardCap = null)
{
    public int MaxTier => Math.Min(BonusesByTier.Count, CostsByTier.Count) - 1;
}

public sealed record StatPickerOption(string Stat, int Tier, int Bonus, int Cost)
{
    public string Label => $"{Stat.ToUpperInvariant()} +{Bonus} | {Cost}xp";
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
