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

public partial class InspiringCompanySelectionPage : GeneratedFactionCompanySelectionPageBase
{
    private readonly IArmyDataService _armyDataService;

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
}
