using System.Windows.Input;
using InfinityMercsApp.Infrastructure.Providers;
using InfinityMercsApp.Services;
using AirborneGen = InfinityMercsApp.Infrastructure.Providers.AirborneCompanyFactionGenerator;
using InfinityMercsApp.ViewModels;
using InfinityMercsApp.Views.Controls;
using InfinityMercsApp.Views.Common;

namespace InfinityMercsApp.Views.AirborneCompany;

public partial class AirborneCompanySelectionPage : GeneratedFactionCompanySelectionPageBase
{
    private readonly IArmyDataService _armyDataService;

    public AirborneCompanySelectionPage(
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

    public ICommand SelectFactionCommand { get; }
    public ICommand SelectUnitCommand { get; }
    public ICommand AddProfileToMercsCompanyCommand { get; }
    public ICommand RemoveMercsCompanyEntryCommand { get; }
    public ICommand SelectMercsCompanyEntryCommand { get; }
    public ICommand SelectTeamAllowedProfileCommand { get; }
    public ICommand StartCompanyCommand { get; }

    protected override Grid AdaptiveMainContentGrid => MainContentGrid;
    protected override View AdaptiveUnitsPane => UnitsPane;
    protected override View AdaptiveRightPane => RightPane;
    protected override View AdaptiveCompactTabBar => CompactTabBar;
    protected override Button AdaptiveUnitsTabButton => UnitsTabButton;
    protected override Button AdaptiveCompanyTabButton => CompanyTabButton;

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_loaded)
        {
            return;
        }

        _loaded = true;
        DiagnoseAirborneCompanyFaction();
        PopulateAirborneCompanySlot();
        await LoadFactionsAsync();
    }

    private void DiagnoseAirborneCompanyFaction()
    {
        var factionId = AirborneGen.AirborneCompanyFactionId;
        var snapshot = _armyDataService.GetFactionSnapshot(factionId);
        if (snapshot is null)
        {
            Console.WriteLine($"[AirborneCompanyDiag] Faction {factionId} NOT FOUND in database. Has data been imported since the Airborne generator was added?");
            return;
        }

        Console.WriteLine($"[AirborneCompanyDiag] Faction {factionId} found. Version={snapshot.Version}");

        var resumes = _armyDataService.GetResumeByFactionMercsOnly(factionId);
        Console.WriteLine($"[AirborneCompanyDiag] Resumes (mercs-only) for faction {factionId}: {resumes.Count}");
        foreach (var r in resumes.Take(5))
        {
            Console.WriteLine($"[AirborneCompanyDiag]   - UnitId={r.UnitId} Name={r.Name} Slug={r.Slug} Type={r.Type} Category={r.Category}");
        }

        if (resumes.Count > 5)
        {
            Console.WriteLine($"[AirborneCompanyDiag]   ... and {resumes.Count - 5} more");
        }
    }

    private void PopulateAirborneCompanySlot()
    {
        var airborneItem = new ArmyFactionSelectionItem
        {
            Id = AirborneGen.AirborneCompanyFactionId,
            ParentId = AirborneGen.AirborneCompanyFactionId,
            Name = "Airborne Company",
            CachedLogoPath = null,
            PackagedLogoPath = AirborneCompanyLogoPath
        };

        _factionSelectionState.RightSlotFaction = airborneItem;
        _ = LoadSlotIconAsync(1, null, AirborneCompanyLogoPath);
    }

    private const string AirborneCompanyLogoPath = "SVGCache/MercsIcons/noun-airborne-8005870.svg";
}
