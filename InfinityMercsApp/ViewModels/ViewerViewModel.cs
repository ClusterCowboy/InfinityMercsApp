using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Text;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using InfinityMercsApp.Domain.Utilities;
using InfinityMercsApp.Infrastructure.Providers;
using InfinityMercsApp.Services;
using InfinityMercsApp.Views.Controls;
using FactionRecord = InfinityMercsApp.Domain.Models.Metadata.Faction;
using Resume = InfinityMercsApp.Domain.Models.Army.Resume;

namespace InfinityMercsApp.ViewModels;

public partial class ViewerViewModel : BaseViewModel
{
    private readonly record struct ExtraDefinition(string Name, string Type, string? Url);

    private enum FactionFilterMode
    {
        All,
        Factions,
        Sectorials
    }

    private readonly IArmyDataService? _armyDataService;
    private readonly FactionLogoCacheService? _factionLogoCacheService;
    private readonly IAppSettingsProvider? _appSettingsProvider;
    private bool _isLoading;
    private string _status = "Loading factions...";
    private string _unitsStatus = "Select a faction.";
    private string _profilesStatus = "Select a unit.";
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
    private string _equipmentSummary = "Equipment: -";
    private string _specialSkillsSummary = "Special Skills: -";
    private FormattedString _equipmentSummaryFormatted = new();
    private FormattedString _specialSkillsSummaryFormatted = new();
    private bool _showRegularOrderIcon;
    private bool _showIrregularOrderIcon;
    private bool _showImpetuousIcon;
    private bool _showTacticalAwarenessIcon;
    private bool _showCubeIcon;
    private bool _showCube2Icon;
    private bool _showHackableIcon;
    private string? _impetuousIconUrl;
    private string? _tacticalAwarenessIconUrl;
    private string? _cubeIconUrl;
    private string? _cube2IconUrl;
    private string? _hackableIconUrl;
    private bool _showUnitsInInches = true;
    private IReadOnlyDictionary<int, string> _currentEquipmentLookup = new Dictionary<int, string>();
    private IReadOnlyDictionary<int, string> _currentEquipmentLinks = new Dictionary<int, string>();
    private IReadOnlyDictionary<int, string> _currentSkillsLookup = new Dictionary<int, string>();
    private IReadOnlyDictionary<int, string> _currentSkillsLinks = new Dictionary<int, string>();
    private string _fireteamDuoCount = "-";
    private string _fireteamHarisCount = "-";
    private string _fireteamCoreCount = "-";
    private string _fireteamsStatus = "Select a faction.";
    private int? _unitMoveFirstCm;
    private int? _unitMoveSecondCm;
    private ViewerFactionItem? _selectedFaction;
    private ViewerUnitItem? _selectedUnit;
    private bool _showUnitsTab = true;
    private bool _mercsOnlyUnits;
    private bool _lieutenantOnlyUnits;
    private UnitFilterCriteria _activeUnitFilter = UnitFilterCriteria.None;
    private FactionFilterMode _factionFilterMode = FactionFilterMode.All;
    private List<ViewerFactionItem> _allFactions = [];
    public ViewerViewModel(
        IMetadataProvider? metadataProvider = null,
        IFactionProvider? factionProvider = null,
        IArmyDataService? armyDataService = null,
        FactionLogoCacheService? factionLogoCacheService = null,
        IAppSettingsProvider? appSettingsProvider = null)
    {
        _armyDataService = armyDataService;
        _factionLogoCacheService = factionLogoCacheService;
        _appSettingsProvider = appSettingsProvider;

        SelectFactionCommand = new Command<ViewerFactionItem>(async item =>
        {
            if (item is null)
            {
                return;
            }

            SelectedFaction = item;
            await LoadUnitsForSelectedFactionAsync();
        });

        SelectUnitCommand = new Command<ViewerUnitItem>(async item =>
        {
            if (item is null)
            {
                return;
            }

            SelectedUnit = item;
            await LoadProfilesForSelectedUnitAsync();
        });

        ShowUnitsTabCommand = new Command(() => ShowUnitsTab = true);
        ShowFireteamsTabCommand = new Command(() => ShowUnitsTab = false);
    }

    public ObservableCollection<ViewerFactionItem> Factions { get; } = [];

    public ObservableCollection<ViewerUnitItem> Units { get; } = [];
    public ObservableCollection<ViewerProfileItem> Profiles { get; } = [];
    public ObservableCollection<FireteamTeamItem> Fireteams { get; } = [];

    public bool ShowUnitsTab
    {
        get => _showUnitsTab;
        set
        {
            if (_showUnitsTab == value)
            {
                return;
            }

            _showUnitsTab = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ShowFireteamsTab));
        }
    }

    public bool ShowFireteamsTab => !_showUnitsTab;

    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            if (_isLoading == value)
            {
                return;
            }

            _isLoading = value;
            OnPropertyChanged();
        }
    }

    public string Status
    {
        get => _status;
        private set
        {
            if (_status == value)
            {
                return;
            }

            _status = value;
            OnPropertyChanged();
        }
    }

    public string UnitsStatus
    {
        get => _unitsStatus;
        private set
        {
            if (_unitsStatus == value)
            {
                return;
            }

            _unitsStatus = value;
            OnPropertyChanged();
        }
    }

    public string ProfilesStatus
    {
        get => _profilesStatus;
        private set
        {
            if (_profilesStatus == value)
            {
                return;
            }

            _profilesStatus = value;
            OnPropertyChanged();
        }
    }

    public string EquipmentSummary
    {
        get => _equipmentSummary;
        private set
        {
            if (_equipmentSummary == value)
            {
                return;
            }

            _equipmentSummary = value;
            OnPropertyChanged();
        }
    }

    public FormattedString EquipmentSummaryFormatted
    {
        get => _equipmentSummaryFormatted;
        private set
        {
            _equipmentSummaryFormatted = value;
            OnPropertyChanged();
        }
    }

    public string SpecialSkillsSummary
    {
        get => _specialSkillsSummary;
        private set
        {
            if (_specialSkillsSummary == value)
            {
                return;
            }

            _specialSkillsSummary = value;
            OnPropertyChanged();
        }
    }

    public FormattedString SpecialSkillsSummaryFormatted
    {
        get => _specialSkillsSummaryFormatted;
        private set
        {
            _specialSkillsSummaryFormatted = value;
            OnPropertyChanged();
        }
    }

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
            OnPropertyChanged(nameof(HasOrderTypeIcon));
            OnPropertyChanged(nameof(HasAnyTopHeaderIcons));
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
            OnPropertyChanged(nameof(HasOrderTypeIcon));
            OnPropertyChanged(nameof(HasAnyTopHeaderIcons));
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
        }
    }

    public bool HasOrderTypeIcon => ShowRegularOrderIcon || ShowIrregularOrderIcon;
    public bool HasAnyTopHeaderIcons => HasOrderTypeIcon || ShowImpetuousIcon || ShowTacticalAwarenessIcon;
    public bool HasAnyBottomHeaderIcons => ShowCubeIcon || ShowCube2Icon || ShowHackableIcon;

    public string? ImpetuousIconUrl
    {
        get => _impetuousIconUrl;
        private set
        {
            if (_impetuousIconUrl == value)
            {
                return;
            }

            _impetuousIconUrl = value;
            OnPropertyChanged();
        }
    }

    public string? TacticalAwarenessIconUrl
    {
        get => _tacticalAwarenessIconUrl;
        private set
        {
            if (_tacticalAwarenessIconUrl == value)
            {
                return;
            }

            _tacticalAwarenessIconUrl = value;
            OnPropertyChanged();
        }
    }

    public string? CubeIconUrl
    {
        get => _cubeIconUrl;
        private set
        {
            if (_cubeIconUrl == value)
            {
                return;
            }

            _cubeIconUrl = value;
            OnPropertyChanged();
        }
    }

    public string? Cube2IconUrl
    {
        get => _cube2IconUrl;
        private set
        {
            if (_cube2IconUrl == value)
            {
                return;
            }

            _cube2IconUrl = value;
            OnPropertyChanged();
        }
    }

    public string? HackableIconUrl
    {
        get => _hackableIconUrl;
        private set
        {
            if (_hackableIconUrl == value)
            {
                return;
            }

            _hackableIconUrl = value;
            OnPropertyChanged();
        }
    }

    public bool ShowUnitsInInches
    {
        get => _showUnitsInInches;
        set
        {
            if (_showUnitsInInches == value)
            {
                return;
            }

            _showUnitsInInches = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ShowUnitsInCentimeters));
            UpdateUnitMoveDisplay();
        }
    }

    public bool ShowUnitsInCentimeters
    {
        get => !_showUnitsInInches;
        set
        {
            var targetInches = !value;
            if (_showUnitsInInches == targetInches)
            {
                return;
            }

            _showUnitsInInches = targetInches;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ShowUnitsInInches));
            UpdateUnitMoveDisplay();
        }
    }

    public string FireteamDuoCount
    {
        get => _fireteamDuoCount;
        private set
        {
            if (_fireteamDuoCount == value)
            {
                return;
            }

            _fireteamDuoCount = value;
            OnPropertyChanged();
        }
    }

    public string FireteamHarisCount
    {
        get => _fireteamHarisCount;
        private set
        {
            if (_fireteamHarisCount == value)
            {
                return;
            }

            _fireteamHarisCount = value;
            OnPropertyChanged();
        }
    }

    public string FireteamCoreCount
    {
        get => _fireteamCoreCount;
        private set
        {
            if (_fireteamCoreCount == value)
            {
                return;
            }

            _fireteamCoreCount = value;
            OnPropertyChanged();
        }
    }

    public string FireteamsStatus
    {
        get => _fireteamsStatus;
        private set
        {
            if (_fireteamsStatus == value)
            {
                return;
            }

            _fireteamsStatus = value;
            OnPropertyChanged();
        }
    }

    public ViewerFactionItem? SelectedFaction
    {
        get => _selectedFaction;
        set
        {
            if (_selectedFaction == value)
            {
                return;
            }

            if (_selectedFaction is not null)
            {
                _selectedFaction.IsSelected = false;
            }

            _selectedFaction = value;

            if (_selectedFaction is not null)
            {
                _selectedFaction.IsSelected = true;
            }

            SelectedUnit = null;
            ResetUnitDetails();
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedFactionLogoUrl));
        }
    }

    public ViewerUnitItem? SelectedUnit
    {
        get => _selectedUnit;
        set
        {
            if (_selectedUnit == value)
            {
                return;
            }

            if (_selectedUnit is not null)
            {
                _selectedUnit.IsSelected = false;
            }

            _selectedUnit = value;

            if (_selectedUnit is not null)
            {
                _selectedUnit.IsSelected = true;
            }

            OnPropertyChanged();
        }
    }

    public string SelectedFactionLogoUrl => SelectedFaction?.Logo ?? string.Empty;

    public ICommand SelectFactionCommand { get; }

    public ICommand SelectUnitCommand { get; }
    public ICommand ShowUnitsTabCommand { get; }
    public ICommand ShowFireteamsTabCommand { get; }
    public ICommand? AddProfileToMercsCompanyCommand { get; } = null;

}
