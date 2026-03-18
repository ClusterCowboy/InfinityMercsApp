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
    private string _pageHeading = string.Empty;
    private ArmyUnitSelectionItem? _selectedUnit;
    private bool _summaryHighlightLieutenant;
    private readonly CompanySelectionFilterState _filterState = new();

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
        WireFactionSlotTapHandlers(SetActiveSlot, () => ShowRightSelectionBox);
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

        SelectFactionCommand = CreateSelectFactionCommand<ArmyFactionSelectionItem>(SetSelectedFaction);
        SelectUnitCommand = CreateSelectUnitCommand<ArmyUnitSelectionItem>(
            SetSelectedUnit,
            item => item.Id,
            item => item.SourceFactionId,
            item => item.Name);
        AddProfileToMercsCompanyCommand = new Command<ViewerProfileItem>(AddProfileToMercsCompany);
        RemoveMercsCompanyEntryCommand = new Command<MercsCompanyEntry>(RemoveMercsCompanyEntry);
        SelectMercsCompanyEntryCommand = new Command<MercsCompanyEntry>(entry => _ = SelectMercsCompanyEntryAsync(entry));
        SelectTeamAllowedProfileCommand = new Command<ArmyTeamUnitLimitItem>(teamItem =>
            HandleTeamAllowedProfileSelected(
                teamItem,
                Units,
                (resolved, _) => SetSelectedUnit(resolved)));
        _startCompanyCommand = CreateStartCompanyCommand(StartCompanyAsync, () => IsCompanyValid);
        StartCompanyCommand = _startCompanyCommand;

        FinalizePageInitialization(() => SetActiveSlot(0));
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
            _ = RefreshSeasonPointsDependentUnitStateAsync();
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

    protected override void ApplyLieutenantVisualStatesFromBase()
    {
        ApplyLieutenantVisualStates();
    }

    protected override Task ApplyUnitVisibilityFiltersFromBaseAsync()
    {
        return ApplyUnitVisibilityFiltersAsync();
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

    protected override Task LoadFactionsAsync()
    {
        return LoadFactionsAsync(CancellationToken.None);
    }

    protected override Task LoadUnitsForActiveSlotAsync()
    {
        return LoadUnitsForActiveSlotAsync(CancellationToken.None);
    }

    private async Task RefreshSeasonPointsDependentUnitStateAsync(CancellationToken cancellationToken = default)
    {
        _filterState.PreparedUnitFilterPopupOptions = null;
        await ApplyUnitVisibilityFiltersAsync(cancellationToken);
        await BuildUnitFilterPopupOptionsAsync(cancellationToken);
    }

    protected override Task LoadSelectedUnitDetailsAsync()
    {
        return LoadSelectedUnitDetailsAsync(CancellationToken.None);
    }

    UnitFilterCriteria ICompanySelectionVisibilityState.ActiveUnitFilter => _filterState.ActiveUnitFilter;

}




