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
using InfinityMercsApp.Views.Common.UICommon;
using Microsoft.Maui.Devices;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using Svg.Skia;
using InfinityMercsApp.Views;
using InfinityMercsApp.Views.Common;
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

public partial class CohesiveCompanySelectionPage : CompanySelectionPageBase, ICompanySelectionVisibilityState
{
    private readonly ArmySourceSelectionMode _mode;
    private readonly IArmyDataService _armyDataService;
    private readonly ISpecOpsProvider _specOpsProvider;
    private readonly FactionLogoCacheService? _factionLogoCacheService;
    private readonly FactionSlotSelectionState<ArmyFactionSelectionItem> _factionSelectionState = new();
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
    private readonly CompanySelectionFilterState _filterState = new();
    private readonly Dictionary<int, HashSet<string>> _validCoreFireteamsByFaction = new();
    private string _trackedFireteamName = string.Empty;
    private int _trackedFireteamLevel;
    private SKPicture? _trackedFireteamLevelPicture;
    private bool _isUpdatingTrackedTeamSelection;

    public CohesiveCompanySelectionPage(
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
        Title = "Choose your sectorial:";
        PageHeading = "Choose your sectorial:";

        _armyDataService = armyDataService;
        _specOpsProvider = SpecOpsProvider;
        _factionLogoCacheService = FactionLogoCacheService;

        SelectFactionCommand = CreateSelectFactionCommand<ArmyFactionSelectionItem>(SetSelectedFaction);
        SelectUnitCommand = CreateSelectUnitCommand<ArmyUnitSelectionItem>(
            item => SetSelectedUnit(item),
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
                (resolved, restrictProfiles) => SetSelectedUnit(resolved, restrictProfiles),
                item => IsFtoLabel(item.Name)));
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
        private set => SetAndNotifyUnitHeaderIconFlag(
            () => UnitDisplayConfigurationsView.ShowRegularOrderIcon,
            x => UnitDisplayConfigurationsView.ShowRegularOrderIcon = x,
            value);
    }

    public bool ShowIrregularOrderIcon
    {
        get => UnitDisplayConfigurationsView.ShowIrregularOrderIcon;
        private set => SetAndNotifyUnitHeaderIconFlag(
            () => UnitDisplayConfigurationsView.ShowIrregularOrderIcon,
            x => UnitDisplayConfigurationsView.ShowIrregularOrderIcon = x,
            value);
    }

    public bool ShowImpetuousIcon
    {
        get => UnitDisplayConfigurationsView.ShowImpetuousIcon;
        private set => SetAndNotifyUnitHeaderIconFlag(
            () => UnitDisplayConfigurationsView.ShowImpetuousIcon,
            x => UnitDisplayConfigurationsView.ShowImpetuousIcon = x,
            value);
    }

    public bool ShowTacticalAwarenessIcon
    {
        get => UnitDisplayConfigurationsView.ShowTacticalAwarenessIcon;
        private set => SetAndNotifyUnitHeaderIconFlag(
            () => UnitDisplayConfigurationsView.ShowTacticalAwarenessIcon,
            x => UnitDisplayConfigurationsView.ShowTacticalAwarenessIcon = x,
            value);
    }

    public bool ShowCubeIcon
    {
        get => UnitDisplayConfigurationsView.ShowCubeIcon;
        private set => SetAndNotifyUnitHeaderIconFlag(
            () => UnitDisplayConfigurationsView.ShowCubeIcon,
            x => UnitDisplayConfigurationsView.ShowCubeIcon = x,
            value);
    }

    public bool ShowCube2Icon
    {
        get => UnitDisplayConfigurationsView.ShowCube2Icon;
        private set => SetAndNotifyUnitHeaderIconFlag(
            () => UnitDisplayConfigurationsView.ShowCube2Icon,
            x => UnitDisplayConfigurationsView.ShowCube2Icon = x,
            value);
    }

    public bool ShowHackableIcon
    {
        get => UnitDisplayConfigurationsView.ShowHackableIcon;
        private set => SetAndNotifyUnitHeaderIconFlag(
            () => UnitDisplayConfigurationsView.ShowHackableIcon,
            x => UnitDisplayConfigurationsView.ShowHackableIcon = x,
            value);
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

    UnitFilterCriteria ICompanySelectionVisibilityState.ActiveUnitFilter => _filterState.ActiveUnitFilter;


    private async Task LoadUnitsForActiveSlotAsync(CancellationToken cancellationToken = default)
    {
        var trackedFireteamNameToRestore = _trackedFireteamName;
        _filterState.PreparedUnitFilterPopupOptions = null;
        AreTeamEntriesReady = false;
        Units.Clear();
        TeamEntries.Clear();
        _selectedUnit = null;
        ResetUnitDetails();

        var factions = CompanyUnitDetailsShared.BuildUnitSourceFactions(
            ShowRightSelectionBox,
            _factionSelectionState.LeftSlotFaction,
            _factionSelectionState.RightSlotFaction,
            faction => faction.Id);
        if (factions.Count == 0)
        {
            return;
        }

        try
        {
            var merged = await BuildMergedUnitsAndTeamsAsync(
                factions,
                faction => faction.Id,
                GetResumeByFactionMercsOnlyFromProvider,
                _specOpsProvider.GetSpecopsUnitsByFactionAsync,
                GetFactionSnapshotFromProvider,
                async (factionId, units, ct) =>
                {
                    if (_factionLogoCacheService is not null)
                    {
                        await _factionLogoCacheService.CacheUnitLogosFromRecordsAsync(factionId, units, ct);
                    }
                },
                MergeFireteamEntries,
                IsCharacterCategory,
                BuildUnitSubtitle,
                (factionId, unit, typeLookup, categoryLookup) => new ArmyUnitSelectionItem
                {
                    Id = unit.UnitId,
                    SourceFactionId = factionId,
                    Slug = unit.Slug,
                    Name = unit.Name,
                    Type = unit.Type,
                    IsCharacter = IsCharacterCategory(unit, categoryLookup),
                    Subtitle = BuildUnitSubtitle(unit, typeLookup, categoryLookup),
                    IsSpecOps = false,
                    CachedLogoPath = _factionLogoCacheService?.TryGetCachedUnitLogoPath(factionId, unit.UnitId),
                    PackagedLogoPath = _factionLogoCacheService?.GetPackagedUnitLogoPath(factionId, unit.UnitId)
                        ?? $"SVGCache/units/{factionId}-{unit.UnitId}.svg"
                },
                (factionId, specopsUnit, resumeByUnitId, units, typeLookup, categoryLookup) =>
                {
                    var baseName = string.IsNullOrWhiteSpace(specopsUnit.Name)
                        ? units.FirstOrDefault(x => x.UnitId == specopsUnit.UnitId)?.Name ?? $"Unit {specopsUnit.UnitId}"
                        : specopsUnit.Name.Trim();
                    var key = $"{baseName} - Spec Ops";
                    return new ArmyUnitSelectionItem
                    {
                        Id = specopsUnit.UnitId,
                        SourceFactionId = factionId,
                        Slug = specopsUnit.Slug,
                        Name = key,
                        Type = resumeByUnitId.TryGetValue(specopsUnit.UnitId, out var resumeUnit) ? resumeUnit.Type : null,
                        IsCharacter = resumeByUnitId.TryGetValue(specopsUnit.UnitId, out var characterUnit) &&
                                      IsCharacterCategory(characterUnit, categoryLookup),
                        Subtitle = resumeByUnitId.TryGetValue(specopsUnit.UnitId, out var subtitleUnit)
                            ? BuildUnitSubtitle(subtitleUnit, typeLookup, categoryLookup)
                            : "Spec Ops",
                        IsSpecOps = true,
                        CachedLogoPath = _factionLogoCacheService?.TryGetCachedUnitLogoPath(factionId, specopsUnit.UnitId),
                        PackagedLogoPath = _factionLogoCacheService?.GetPackagedUnitLogoPath(factionId, specopsUnit.UnitId)
                            ?? $"SVGCache/units/{factionId}-{specopsUnit.UnitId}.svg"
                    };
                },
                cancellationToken);

            PopulateUnitsCollection(Units, merged.UnitsByKey.Values);

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

            BuildTeamEntriesFromMerged<ArmyUnitSelectionItem, ArmyTeamUnitLimitItem, ArmyTeamListItem>(
                merged,
                TeamEntries,
                includeTeam: team => validCoreTeamNames.Count == 0 || validCoreTeamNames.Contains(team.Name),
                readTeamCount: team => team.Core,
                buildTeamCountText: team => $"C: {team.Core}",
                buildTeamUnitLimitItem: (name, min, max, slug, sourceUnits) =>
                    CompanyTeamProfilesWorkflow.BuildTeamUnitLimitItem<ArmyUnitSelectionItem, ArmyTeamUnitLimitItem>(
                        name, min, max, slug, sourceUnits),
                createTeam: (name, teamCountsText, isWildcardBucket, isExpanded, allowedProfiles) => new ArmyTeamListItem
                {
                    Name = name,
                    TeamCountsText = teamCountsText,
                    IsWildcardBucket = isWildcardBucket,
                    IsExpanded = isExpanded,
                    AllowedProfiles = allowedProfiles
                });

            await ApplyUnitVisibilityFiltersAsync(cancellationToken);
            await BuildUnitFilterPopupOptionsAsync(cancellationToken);
            RestoreTrackedFireteamSelection(trackedFireteamNameToRestore);
            AreTeamEntriesReady = true;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"CompanySelectionPage LoadUnitsForActiveSlotAsync failed: {ex.Message}");
            RestoreTrackedFireteamSelection(string.Empty);
            AreTeamEntriesReady = false;
        }
    }

    private static bool IsFtoLabel(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return Regex.IsMatch(value, @"\bFTO\b", RegexOptions.IgnoreCase);
    }

}







