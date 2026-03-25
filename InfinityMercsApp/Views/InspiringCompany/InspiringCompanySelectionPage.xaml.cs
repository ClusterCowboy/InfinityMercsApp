using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using InfinityMercsApp.Infrastructure.Providers;
using InfinityMercsApp.Services;
using InspiringGen = InfinityMercsApp.Infrastructure.Providers.InspiringCompanyFactionGenerator;
using InfinityMercsApp.ViewModels;
using InfinityMercsApp.Views.Controls;
using InfinityMercsApp.Views.Common;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using ArmyFactionRecord = InfinityMercsApp.Domain.Models.Army.Faction;
using ArmyResumeRecord = InfinityMercsApp.Domain.Models.Army.Resume;
using ArmyUnitRecord = InfinityMercsApp.Domain.Models.Army.Unit;
using FactionRecord = InfinityMercsApp.Domain.Models.Metadata.Faction;
using MercsArmyListEntry = InfinityMercsApp.Domain.Models.Army.MercsArmyListEntry;
using InfinityMercsApp.Views.Common.UICommon;

namespace InfinityMercsApp.Views.InspiringCompany;

public partial class InspiringCompanySelectionPage : CompanySelectionPageBase, ICompanySelectionVisibilityState
{
    private readonly IArmyDataService _armyDataService;
    private readonly ISpecOpsProvider _specOpsProvider;
    private readonly FactionLogoCacheService? _factionLogoCacheService;
    private readonly CompanyProfileCoordinator _profileCoordinator;
    private readonly FactionSlotSelectionState<ArmyFactionSelectionItem> _factionSelectionState = new();
    private string _companyName = "Company Name";
    private readonly Command _startCompanyCommand;
    private bool _showCompanyNameValidationError;
    private Color _companyNameBorderColor = Color.FromArgb("#6B7280");
    private bool _loaded;
    private ArmyUnitSelectionItem? _selectedUnit;
    private bool _summaryHighlightLieutenant;
    private int _activeSlotIndex;
    private readonly CompanySelectionFilterState _filterState = new();

    public InspiringCompanySelectionPage(
        IMetadataProvider? metadataProvider,
        IFactionProvider? factionProvider,
        ISpecOpsProvider specOpsProvider,
        ICohesiveCompanyFactionQueryProvider cohesiveCompanyFactionQueryProvider,
        FactionLogoCacheService? factionLogoCacheService,
        IAppSettingsProvider? appSettingsProvider,
        IArmyDataService armyDataService)
        : base(ArmySourceSelectionMode.Sectorials, metadataProvider, factionProvider, specOpsProvider, cohesiveCompanyFactionQueryProvider, factionLogoCacheService, appSettingsProvider)
    {
        InitializeComponent();
        SeasonStartPointsView.SelectedStartSeasonPointsChanged += OnSelectedStartSeasonPointsChanged;
        SetIsUnitFilterActive(true);
        _filterState.ActiveUnitFilter = new UnitFilterCriteria { LieutenantOnlyUnits = true };
        WireFactionSlotTapHandlers(SetActiveSlot, () => true);
        Title = "Choose your sectorial";

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

        LieutenantOnlyUnits = true;
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
        }
    }

    private void OnSelectedStartSeasonPointsChanged(object? sender, EventArgs e)
    {
        UpdateSeasonValidationState();
        _ = RefreshSeasonPointsDependentUnitStateAsync();
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
        DiagnoseInspiringCompanyFaction();
        PopulateInspiringCompanySlot();
        await LoadFactionsAsync();
    }

    private void DiagnoseInspiringCompanyFaction()
    {
        var factionId = InspiringGen.InspiringCompanyFactionId;
        var snapshot = _armyDataService.GetFactionSnapshot(factionId);
        if (snapshot is null)
        {
            Console.WriteLine($"[InspiringCompanyDiag] Faction {factionId} NOT FOUND in database. Has data been imported since the Inspiring generator was added?");
            return;
        }

        Console.WriteLine($"[InspiringCompanyDiag] Faction {factionId} found. Version={snapshot.Version}");

        var resumes = _armyDataService.GetResumeByFactionMercsOnly(factionId);
        Console.WriteLine($"[InspiringCompanyDiag] Resumes (mercs-only) for faction {factionId}: {resumes.Count}");
        foreach (var r in resumes.Take(5))
        {
            Console.WriteLine($"[InspiringCompanyDiag]   - UnitId={r.UnitId} Name={r.Name} Slug={r.Slug} Type={r.Type} Category={r.Category}");
        }

        if (resumes.Count > 5)
        {
            Console.WriteLine($"[InspiringCompanyDiag]   ... and {resumes.Count - 5} more");
        }
    }

    private void PopulateInspiringCompanySlot()
    {
        var inspiringItem = new ArmyFactionSelectionItem
        {
            Id = InspiringGen.InspiringCompanyFactionId,
            ParentId = InspiringGen.InspiringCompanyFactionId,
            Name = "Inspiring Company",
            CachedLogoPath = null,
            PackagedLogoPath = InspiringCompanyLogoPath
        };

        _factionSelectionState.RightSlotFaction = inspiringItem;
        _ = LoadSlotIconAsync(1, null, InspiringCompanyLogoPath);
    }

    private const string InspiringCompanyLogoPath = "SVGCache/MercsIcons/noun-leadership-7195245.svg";

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

    private void SwitchToLeftSlot()
    {
        _activeSlotIndex = 0;
        FactionSlotSelectorView.ApplyActiveSlotBorders(0);
        _filterState.ActiveUnitFilter = new UnitFilterCriteria { LieutenantOnlyUnits = true };
        LieutenantOnlyUnits = true;
        SetIsUnitFilterActive(_filterState.ActiveUnitFilter.IsActive);
    }

    private void SetActiveSlot(int index)
    {
        var previousSlot = _activeSlotIndex;
        _activeSlotIndex = ResolveActiveSlotIndexCore(index, true);
        FactionSlotSelectorView.ApplyActiveSlotBorders(_activeSlotIndex);

        var isLeftSlot = _activeSlotIndex == 0;
        _filterState.ActiveUnitFilter = new UnitFilterCriteria { LieutenantOnlyUnits = isLeftSlot };
        LieutenantOnlyUnits = isLeftSlot;
        SetIsUnitFilterActive(_filterState.ActiveUnitFilter.IsActive);

        if (previousSlot != _activeSlotIndex && _loaded)
        {
            _ = LoadUnitsForActiveSlotAsync();
        }
    }
}
