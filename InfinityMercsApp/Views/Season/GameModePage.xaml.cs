using System.Text.Json;
using System.Text.RegularExpressions;
using InfinityMercsApp.Domain.Models.Metadata;
using InfinityMercsApp.Domain.Models.Perks;
using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Infrastructure.Providers;
using InfinityMercsApp.Services;
using InfinityMercsApp.Services.Season;
using InfinityMercsApp.ViewModels;
using InfinityMercsApp.Views.Common;
using InfinityMercsApp.Views.Controls;
using Microsoft.Maui.Controls.Shapes;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using Svg.Skia;

namespace InfinityMercsApp.Views.Season;

public partial class GameModePage : ContentPage, IQueryAttributable
{
    private const int TagCompanyFactionId = 2003;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly SKColorFilter GreyFilter = SKColorFilter.CreateColorMatrix(
    [
        0.21f, 0.72f, 0.07f, 0, 0,
        0.21f, 0.72f, 0.07f, 0, 0,
        0.21f, 0.72f, 0.07f, 0, 0,
        0,     0,     0,     1, 0
    ]);

    private readonly IArmyDataService? _armyDataService;
    private readonly FactionLogoCacheService? _factionLogoCacheService;
    private readonly IAppSettingsProvider? _appSettingsProvider;
    private readonly IMetadataProvider? _metadataProvider;
    private string _companyFilePath = string.Empty;
    private string _seasonFilePath = string.Empty;
    private bool _loadAttempted;
    private bool _showUnitsInInches = true;

    // Lazily-built map of ammunition id → display name, used to label weapon cards with their damage type.
    private Dictionary<int, string>? _ammoNamesById;

    private SKPicture? _ltPicture;
    private SKPicture? _impPicture;
    private SKPicture? _irrPicture;
    private SKPicture? _regPicture;
    private bool _iconsLoaded;

    private SKPicture? _woundHealthyPicture;
    private SKPicture? _woundWoundedPicture;
    private SKPicture? _woundMadPicture;
    private SKPicture? _woundKoPicture;
    private SKPicture? _woundDeadPicture;
    private SKPicture? _tacticalPicture;
    private readonly List<(DeploymentUnitItem Unit, SKCanvasView Canvas, bool[] IsGrey)> _eliteTacticalIcons = [];

    // Each entry is (expandable content view, arrow label) for exclusive-open behaviour.
    private readonly List<(View ContentArea, Label Arrow)> _accordionRows = [];

    private readonly List<(DeploymentUnitItem Unit, bool IsLt, bool IsImp, bool IsIrr)> _unitTracker = [];
    private int _currentRound = 1;

    // null = show all (no filter), empty set = none checked, non-empty = filtered list
    private HashSet<int>? _deployedEntryIndices;
    private bool _isEliteDeployment;

    public GameModePage(
        IArmyDataService? armyDataService = null,
        FactionLogoCacheService? factionLogoCacheService = null,
        IAppSettingsProvider? appSettingsProvider = null,
        IMetadataProvider? metadataProvider = null)
    {
        InitializeComponent();
        _armyDataService = armyDataService;
        _factionLogoCacheService = factionLogoCacheService;
        _appSettingsProvider = appSettingsProvider;
        _metadataProvider = metadataProvider;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("companyFilePath", out var raw))
        {
            _companyFilePath = Uri.UnescapeDataString(raw?.ToString() ?? string.Empty);
            _loadAttempted = false;
        }

        if (query.TryGetValue("seasonFilePath", out var seasonRaw))
            _seasonFilePath = Uri.UnescapeDataString(seasonRaw?.ToString() ?? string.Empty);

        if (query.TryGetValue("deployedIndices", out var indicesRaw))
        {
            var decoded = Uri.UnescapeDataString(indicesRaw?.ToString() ?? string.Empty);
            _deployedEntryIndices = decoded
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => int.TryParse(s.Trim(), out var n) ? n : (int?)null)
                .Where(n => n.HasValue)
                .Select(n => n!.Value)
                .ToHashSet();
        }
        else
        {
            _deployedEntryIndices = null;
        }

        _isEliteDeployment = query.TryGetValue("eliteDeployment", out var eliteRaw) &&
                             eliteRaw?.ToString() == "1";
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _showUnitsInInches = SeasonDisplayUnitFormatter.GetShowUnitsInInches(_appSettingsProvider);
        if (!_iconsLoaded)
            await LoadOrderIconsAsync();
        if (!_loadAttempted && !string.IsNullOrWhiteSpace(_companyFilePath))
            await LoadCompanyFromFileAsync(_companyFilePath);
    }

    // ── Icon loading ─────────────────────────────────────────────────────────

    private async Task LoadOrderIconsAsync()
    {
        _iconsLoaded = true;
        _ltPicture  = await TryLoadSvgAsync("SVGCache/CBIcons/lieutenant.svg");
        _impPicture = await TryLoadSvgAsync("SVGCache/CBIcons/impetuous.svg");
        _irrPicture = await TryLoadSvgAsync("SVGCache/CBIcons/irregular.svg");
        _regPicture = await TryLoadSvgAsync("SVGCache/CBIcons/regular.svg");

        _woundHealthyPicture = await TryLoadSvgAsync("SVGCache/NonCBIcons/noun-smiley-face.svg");
        _woundWoundedPicture = await TryLoadSvgAsync("SVGCache/NonCBIcons/noun-sad-face.svg");
        _woundMadPicture     = await TryLoadSvgAsync("SVGCache/NonCBIcons/noun-mad-face.svg");
        _woundKoPicture      = await TryLoadSvgAsync("SVGCache/NonCBIcons/noun-knocked-out-face.svg");
        _woundDeadPicture    = await TryLoadSvgAsync("SVGCache/NonCBIcons/noun-gravestone.svg");
        _tacticalPicture     = await TryLoadSvgAsync("SVGCache/CBIcons/tactical.svg");

        foreach (var canvas in new SKCanvasView[] { LtColorCanvas, ImpColorCanvas, IrrColorCanvas, RegColorCanvas,
                                                    LtGreyCanvas,  ImpGreyCanvas,  IrrGreyCanvas,  RegGreyCanvas })
            canvas.InvalidateSurface();
    }

    private static async Task<SKPicture?> TryLoadSvgAsync(string path)
    {
        foreach (var candidate in new[] { path, path.ToLowerInvariant() })
        {
            try
            {
                await using var stream = await FileSystem.Current.OpenAppPackageFileAsync(candidate);
                var svg = new SKSvg();
                return svg.Load(stream);
            }
            catch { }
        }
        return null;
    }

    // ── Paint handlers ───────────────────────────────────────────────────────

    private void OnLtColorPaint (object? s, SKPaintSurfaceEventArgs e) => DrawColor(e, _ltPicture);
    private void OnImpColorPaint(object? s, SKPaintSurfaceEventArgs e) => DrawColor(e, _impPicture);
    private void OnIrrColorPaint(object? s, SKPaintSurfaceEventArgs e) => DrawColor(e, _irrPicture);
    private void OnRegColorPaint(object? s, SKPaintSurfaceEventArgs e) => DrawColor(e, _regPicture);

    private void OnLtGreyPaint (object? s, SKPaintSurfaceEventArgs e) => DrawGrey(e, _ltPicture);
    private void OnImpGreyPaint(object? s, SKPaintSurfaceEventArgs e) => DrawGrey(e, _impPicture);
    private void OnIrrGreyPaint(object? s, SKPaintSurfaceEventArgs e) => DrawGrey(e, _irrPicture);
    private void OnRegGreyPaint(object? s, SKPaintSurfaceEventArgs e) => DrawGrey(e, _regPicture);

    private static void DrawColor(SKPaintSurfaceEventArgs e, SKPicture? picture)
    {
        e.Surface.Canvas.Clear(SKColors.Transparent);
        if (picture is null) return;
        DrawScaled(e.Surface.Canvas, e.Info, picture, null);
    }

    private static void DrawGrey(SKPaintSurfaceEventArgs e, SKPicture? picture)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Transparent);
        if (picture is null) return;

        // Render picture into an offscreen bitmap, then composite with greyscale filter.
        using var bmp = new SKBitmap(e.Info.Width, e.Info.Height);
        using var offCanvas = new SKCanvas(bmp);
        offCanvas.Clear(SKColors.Transparent);
        DrawScaled(offCanvas, e.Info, picture, null);
        offCanvas.Flush();

        using var paint = new SKPaint { ColorFilter = GreyFilter };
        canvas.DrawBitmap(bmp, 0, 0, paint);
    }

    // ── Order tap handlers ───────────────────────────────────────────────────

    private void OnLtColorTapped (object? s, TappedEventArgs e) => TransferOrder(LtCountLabel,  LtGreyCountLabel);
    private void OnImpColorTapped(object? s, TappedEventArgs e) => TransferOrder(ImpCountLabel, ImpGreyCountLabel);
    private void OnIrrColorTapped(object? s, TappedEventArgs e) => TransferOrder(IrrCountLabel, IrrGreyCountLabel);
    private void OnRegColorTapped(object? s, TappedEventArgs e) => TransferOrder(RegCountLabel, RegGreyCountLabel);

    private void OnLtGreyTapped (object? s, TappedEventArgs e) => TransferOrder(LtGreyCountLabel,  LtCountLabel);
    private void OnImpGreyTapped(object? s, TappedEventArgs e) => TransferOrder(ImpGreyCountLabel, ImpCountLabel);
    private void OnIrrGreyTapped(object? s, TappedEventArgs e) => TransferOrder(IrrGreyCountLabel, IrrCountLabel);
    private void OnRegGreyTapped(object? s, TappedEventArgs e) => TransferOrder(RegGreyCountLabel, RegCountLabel);

    private static void TransferOrder(Label from, Label to)
    {
        if (!int.TryParse(from.Text, out var count) || count <= 0) return;
        from.Text = (count - 1).ToString();
        to.Text = (int.TryParse(to.Text, out var dest) ? dest + 1 : 1).ToString();
    }

    private static void DrawScaled(SKCanvas canvas, SKImageInfo info, SKPicture picture, SKPaint? paint)
    {
        var bounds = picture.CullRect;
        if (bounds.Width <= 0 || bounds.Height <= 0) return;
        var scale = Math.Min(info.Width / bounds.Width, info.Height / bounds.Height);
        var x = (info.Width  - bounds.Width  * scale) / 2f;
        var y = (info.Height - bounds.Height * scale) / 2f;
        canvas.Save();
        canvas.Translate(x, y);
        canvas.Scale(scale);
        canvas.DrawPicture(picture, paint);
        canvas.Restore();
    }

    // ── Unit loading ─────────────────────────────────────────────────────────

    private async Task LoadCompanyFromFileAsync(string filePath)
    {
        _loadAttempted = true;
        AccordionStack.Children.Clear();
        _accordionRows.Clear();
        _unitTracker.Clear();
        _eliteTacticalIcons.Clear();
        _currentRound = 1;
        RoundLabel.Text = "Round 1";
        EndRoundButton.IsVisible = false;

        if (!File.Exists(filePath)) return;

        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            var payload = JsonSerializer.Deserialize<SavedCompanyFile>(json, JsonOptions);
            if (payload?.Entries is null) return;

            var captainStats = payload.ImprovedCaptainStats ?? new SavedImprovedCaptainStats();
            var captainWeaponChoices = captainStats.IsEnabled
                ? CollectCaptainChoices(captainStats.WeaponChoice1, captainStats.WeaponChoice2, captainStats.WeaponChoice3)
                : [];
            var captainSkillChoices = captainStats.IsEnabled
                ? CollectCaptainChoices(captainStats.SkillChoice1, captainStats.SkillChoice2, captainStats.SkillChoice3)
                : [];
            var captainEquipmentChoices = captainStats.IsEnabled
                ? CollectCaptainChoices(captainStats.EquipmentChoice1, captainStats.EquipmentChoice2, captainStats.EquipmentChoice3)
                : [];

            var entries = payload.Entries
                .Where(e => !e.IsPeripheralUnit)
                .Where(e => _deployedEntryIndices is null || _deployedEntryIndices.Contains(e.EntryIndex))
                .OrderByDescending(x => x.IsLieutenant)
                .ThenBy(x => x.EntryIndex)
                .ToList();

            var seasonGear = await LoadSeasonGearAsync();

            int ltCount = 0, regCount = 0, irrCount = 0, impCount = 0;

            foreach (var entry in entries)
            {
                var effectiveSourceFactionId = ResolveEffectiveSourceFactionId(entry);
                var baseUnitName = string.IsNullOrWhiteSpace(entry.BaseUnitName) ? entry.Name : entry.BaseUnitName;
                var displayName = string.IsNullOrWhiteSpace(entry.CustomName)
                    ? baseUnitName
                    : entry.CustomName.Trim();

                var skills = ResolveSavedSkills(effectiveSourceFactionId, entry);
                var equipment = ResolveSavedEquipment(effectiveSourceFactionId, entry);
                var (rangedWeapons, meleeWeapons) = ResolveSavedWeapons(effectiveSourceFactionId, entry);
                var characteristics = ResolveSavedCharacteristics(effectiveSourceFactionId, entry);
                var isIrregular = HasOrderKeyword(characteristics, "Irregular");
                var isImpetuous = HasOrderKeyword(characteristics, "Impetuous");
                var hasNwi = HasSkill(skills, "No Wound Incapacitation");
                var hasRemotePresence = HasSkill(skills, "Remote Presence");
                var startingVitality = Math.Max(1, int.TryParse(entry.CurrentVitaOrStr, out var svParsed) ? svParsed : 1);

                if (entry.IsLieutenant) ltCount++;
                if (isImpetuous) impCount++;
                if (isIrregular) irrCount++;
                if (!isIrregular) regCount++;

                if (entry.IsLieutenant && captainStats.IsEnabled)
                {
                    rangedWeapons = AppendChoices(rangedWeapons,
                        captainWeaponChoices.Where(w => !CompanyProfileTextService.IsMeleeWeaponName(w)).ToList());
                    meleeWeapons = AppendChoices(meleeWeapons,
                        captainWeaponChoices.Where(CompanyProfileTextService.IsMeleeWeaponName).ToList());
                    skills = AppendChoices(skills, captainSkillChoices);
                    equipment = AppendChoices(equipment, captainEquipmentChoices);
                }

                var logoFactionId = entry.LogoSourceFactionId > 0 ? entry.LogoSourceFactionId : entry.SourceFactionId;
                var logoUnitId    = entry.LogoSourceUnitId   > 0 ? entry.LogoSourceUnitId   : entry.SourceUnitId;

                var unitCc       = entry.CurrentCc;
                var unitBs       = entry.CurrentBs;
                var unitPh       = entry.CurrentPh;
                var unitWip      = entry.CurrentWip;
                var unitArm      = entry.CurrentArm;
                var unitBts      = entry.CurrentBts;
                var unitS        = entry.CurrentS;
                var unitVitality = entry.CurrentVitaOrStr;

                ApplySeasonGearToUnit(entry.EntryIndex, seasonGear,
                    ref rangedWeapons, ref meleeWeapons, ref skills,
                    ref unitCc, ref unitBs, ref unitPh, ref unitWip,
                    ref unitArm, ref unitBts, ref unitS, ref unitVitality);

                var unit = new DeploymentUnitItem
                {
                    EntryIndex = entry.EntryIndex,
                    Name = displayName,
                    BaseUnitDisplayName = BuildUnitBaseDisplayName(baseUnitName),
                    IsLieutenant = entry.IsLieutenant,
                    IsIrregular = isIrregular,
                    IsImpetuous = isImpetuous,
                    CaptainIconPackagedPath = entry.IsLieutenant
                        ? "SVGCache/NonCBIcons/noun-captain-8115950.svg"
                        : string.Empty,
                    ExperienceIconPackagedPath = string.Empty,
                    CachedLogoPath   = _factionLogoCacheService?.TryGetCachedUnitLogoPath(logoFactionId, logoUnitId),
                    PackagedLogoPath = _factionLogoCacheService?.GetPackagedUnitLogoPath(logoFactionId, logoUnitId)
                        ?? $"SVGCache/units/{logoFactionId}-{logoUnitId}.svg",
                    UnitMov = SeasonDisplayUnitFormatter.FormatMoveValue(
                        entry.CurrentMov, entry.CurrentMoveFirstCm, entry.CurrentMoveSecondCm, _showUnitsInInches),
                    UnitCc       = unitCc,
                    UnitBs       = unitBs,
                    UnitPh       = unitPh,
                    UnitWip      = unitWip,
                    UnitArm      = unitArm,
                    UnitBts      = unitBts,
                    UnitVitality = unitVitality,
                    UnitS        = unitS,
                    Renown       = entry.Cost + entry.ExperiencePoints,
                    VitalityHeader    = InferVitalityHeader(entry.UnitTypeCode),
                    StartingVitality  = startingVitality,
                    HasNwi            = hasNwi,
                    HasRemotePresence = hasRemotePresence,
                    Equipment     = SeasonDisplayUnitFormatter.ConvertExplicitDistances(equipment,     _showUnitsInInches),
                    Skills        = SeasonDisplayUnitFormatter.ConvertExplicitDistances(skills,        _showUnitsInInches),
                    RangedWeapons = SeasonDisplayUnitFormatter.ConvertExplicitDistances(rangedWeapons, _showUnitsInInches),
                    MeleeWeapons  = SeasonDisplayUnitFormatter.ConvertExplicitDistances(meleeWeapons,  _showUnitsInInches)
                };

                _unitTracker.Add((unit, entry.IsLieutenant, isImpetuous, isIrregular));
                AccordionStack.Children.Add(BuildAccordionRow(unit));
            }

            regCount++;

            LtCountLabel.Text  = ltCount.ToString();
            ImpCountLabel.Text = impCount.ToString();
            IrrCountLabel.Text = irrCount.ToString();
            RegCountLabel.Text = regCount.ToString();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"GameModePage failed to load: {ex.Message}");
        }
    }

    // ── Accordion ────────────────────────────────────────────────────────────

    private View BuildAccordionRow(DeploymentUnitItem unit)
    {
        var arrow = new Label
        {
            Text = "▶",
            TextColor = Color.FromArgb("#9CA3AF"),
            FontSize = 12,
            WidthRequest = 20,
            VerticalTextAlignment = TextAlignment.Center,
            HorizontalTextAlignment = TextAlignment.Center
        };

        var nameLabel = new Label
        {
            Text = unit.Name,
            TextColor = Colors.White,
            FontAttributes = FontAttributes.Bold,
            VerticalTextAlignment = TextAlignment.Center,
            LineBreakMode = LineBreakMode.TailTruncation
        };

        VerticalStackLayout contentArea = BuildUnitDetailContent(unit);
        contentArea.IsVisible = false;

        _accordionRows.Add((contentArea, arrow));

        // ── Wound icon ──────────────────────────────────────────────────────
        var woundCanvas = new SKCanvasView
        {
            WidthRequest = 32,
            HeightRequest = 32,
            VerticalOptions = LayoutOptions.Center
        };
        woundCanvas.PaintSurface += (_, e) =>
        {
            e.Surface.Canvas.Clear(SKColors.Transparent);
            var pic = GetWoundPicture(unit.WoundStateKey);
            if (pic is not null) DrawScaled(e.Surface.Canvas, e.Info, pic, null);
        };

        var woundBadge = new Label
        {
            Text = "2",
            TextColor = Colors.White,
            FontSize = 9,
            FontAttributes = FontAttributes.Bold,
            HorizontalTextAlignment = TextAlignment.End,
            VerticalTextAlignment = TextAlignment.End,
            IsVisible = unit.WoundStateKey == "KnockedOutBody2",
            Padding = 0
        };

        var woundIconGrid = new Grid
        {
            WidthRequest = 32,
            HeightRequest = 32,
            VerticalOptions = LayoutOptions.Center
        };
        woundIconGrid.Children.Add(woundCanvas);
        woundIconGrid.Children.Add(woundBadge);

        void RefreshWoundDisplay()
        {
            woundCanvas.InvalidateSurface();
            woundBadge.IsVisible = unit.WoundStateKey == "KnockedOutBody2";
        }

        // ── Wound buttons (live inside the expanded content area) ────────────
        var addWoundBtn = new Button
        {
            Text = "Take Wound",
            FontSize = 13,
            HeightRequest = 36,
            Padding = new Thickness(0),
            BackgroundColor = Color.FromArgb("#374151"),
            TextColor = Colors.White,
            CornerRadius = 4
        };
        addWoundBtn.Clicked += (_, _) =>
        {
            unit.WoundsReceived++;
            RefreshWoundDisplay();
        };

        var removeWoundBtn = new Button
        {
            Text = "Heal Wound",
            FontSize = 13,
            HeightRequest = 36,
            Padding = new Thickness(0),
            BackgroundColor = Color.FromArgb("#374151"),
            TextColor = Colors.White,
            CornerRadius = 4
        };
        removeWoundBtn.Clicked += (_, _) =>
        {
            unit.WoundsReceived--;
            RefreshWoundDisplay();
        };

        var xpBtn = new Button
        {
            Text = "XP",
            FontSize = 13,
            HeightRequest = 36,
            Padding = new Thickness(0),
            BackgroundColor = Color.FromArgb("#374151"),
            TextColor = Color.FromArgb("#22C55E"),
            CornerRadius = 4
        };
        xpBtn.Clicked += async (_, _) =>
        {
            await ShowXpPopupAsync(unit);
            var total = unit.XpData.TotalXp;
            xpBtn.Text = total > 0 ? $"XP ({total})" : "XP";
        };

        var woundButtonRow = new Grid
        {
            ColumnSpacing = 8,
            Margin = new Thickness(0, 0, 0, 4)
        };
        woundButtonRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
        woundButtonRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
        woundButtonRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
        Grid.SetColumn(addWoundBtn, 0);
        Grid.SetColumn(removeWoundBtn, 1);
        Grid.SetColumn(xpBtn, 2);
        woundButtonRow.Children.Add(addWoundBtn);
        woundButtonRow.Children.Add(removeWoundBtn);
        woundButtonRow.Children.Add(xpBtn);

        contentArea.Children.Insert(0, woundButtonRow);

        // Tactical icon: shown to the left of the wound icon when this is an elite deployment.
        SKCanvasView? tacticalCanvas = null;
        if (_isEliteDeployment && (!unit.IsLieutenant || HasSkill(unit.Skills, "Tactical Awareness")))
        {
            var isGrey = new bool[] { false };
            var tc = new SKCanvasView
            {
                WidthRequest = 32,
                HeightRequest = 32,
                VerticalOptions = LayoutOptions.Center
            };
            tc.PaintSurface += (_, e) =>
            {
                if (isGrey[0])
                    DrawGrey(e, _tacticalPicture);
                else
                    DrawColor(e, _tacticalPicture);
            };
            tc.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = new Command(() =>
                {
                    isGrey[0] = !isGrey[0];
                    tc.InvalidateSurface();
                })
            });
            _eliteTacticalIcons.Add((unit, tc, isGrey));
            tacticalCanvas = tc;
        }

        var headerGrid = new Grid
        {
            MinimumHeightRequest = 48,
            Padding = new Thickness(16, 8),
            ColumnSpacing = 4
        };
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(24) });
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
        if (tacticalCanvas is not null)
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(32) });
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(32) });

        headerGrid.Children.Add(arrow);
        Grid.SetColumn(nameLabel, 1);
        headerGrid.Children.Add(nameLabel);
        if (_isEliteDeployment && tacticalCanvas is not null)
        {
            Grid.SetColumn(tacticalCanvas, 2);
            headerGrid.Children.Add(tacticalCanvas);
            Grid.SetColumn(woundIconGrid, 3);
        }
        else
        {
            Grid.SetColumn(woundIconGrid, 2);
        }
        headerGrid.Children.Add(woundIconGrid);

        headerGrid.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(() =>
            {
                var willExpand = !contentArea.IsVisible;
                foreach (var (ca, ar) in _accordionRows)
                {
                    ca.IsVisible = false;
                    ar.Text = "▶";
                }
                if (willExpand)
                {
                    contentArea.IsVisible = true;
                    arrow.Text = "▼";
                }
            })
        });

        var row = new VerticalStackLayout { Spacing = 0 };
        row.Children.Add(headerGrid);
        row.Children.Add(contentArea);
        row.Children.Add(new BoxView { HeightRequest = 1, Color = Color.FromArgb("#374151") });
        return row;
    }

    private VerticalStackLayout BuildUnitDetailContent(DeploymentUnitItem unit)
    {
        var container = new VerticalStackLayout
        {
            Padding = new Thickness(16, 4, 16, 12),
            Spacing = 8,
            BackgroundColor = Color.FromArgb("#1A2332")
        };

        // Stats table
        var statHeaders = new[] { "MOV", "CC", "BS", "PH", "WIP", "ARM", "BTS", unit.VitalityHeader, "S" };
        var statValues  = new[] { unit.UnitMov, unit.UnitCc, unit.UnitBs, unit.UnitPh, unit.UnitWip,
                                  unit.UnitArm, unit.UnitBts, unit.UnitVitality, unit.UnitS };

        var statsGrid = new Grid { ColumnSpacing = 2, RowSpacing = 2 };
        for (var i = 0; i < statHeaders.Length; i++)
            statsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
        statsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        statsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        for (var i = 0; i < statHeaders.Length; i++)
        {
            var h = new Label
            {
                Text = statHeaders[i],
                FontSize = 10,
                TextColor = Color.FromArgb("#9CA3AF"),
                HorizontalTextAlignment = TextAlignment.Center,
                FontAttributes = FontAttributes.Bold
            };
            var v = new Label
            {
                Text = statValues[i],
                FontSize = 13,
                TextColor = Colors.White,
                HorizontalTextAlignment = TextAlignment.Center
            };
            Grid.SetColumn(h, i); Grid.SetRow(h, 0);
            Grid.SetColumn(v, i); Grid.SetRow(v, 1);
            statsGrid.Children.Add(h);
            statsGrid.Children.Add(v);
        }
        container.Children.Add(statsGrid);

        var normalizedSkills = NormalizeBsAttackText(unit.Skills);

        AddTwoColumnDetailSection(container, unit.Equipment, Color.FromArgb("#06B6D4"));
        AddTwoColumnDetailSection(container, normalizedSkills, Color.FromArgb("#F59E0B"));

        var hasXVisor = ContainsXVisor(normalizedSkills) || ContainsXVisor(unit.Equipment);
        var damageReduction = GetBsAttackDamageReduction(normalizedSkills);
        var ccSr = GetCcAttackDamageReduction(normalizedSkills);
        var ammoModifier = GetBsAttackAmmoModifier(normalizedSkills);
        var burstBonus = GetBsAttackBurstBonus(normalizedSkills);

        AppendRangedWeaponSection(container, unit.RangedWeapons, hasXVisor, damageReduction, ammoModifier, burstBonus);
        AppendCcWeaponSection(container, unit.MeleeWeapons, hasXVisor, ccSr);
        AppendStandardActionCards(container, hasXVisor, damageReduction);

        return container;
    }

    private static void AddTwoColumnDetailSection(VerticalStackLayout container, string? text, Color color)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Trim() == "-") return;

        var items = text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x) && x != "-")
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (items.Count == 0) return;

        var numRows = (items.Count + 1) / 2;
        var grid = new Grid { ColumnSpacing = 8, RowSpacing = 2 };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
        for (var r = 0; r < numRows; r++)
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        for (var i = 0; i < items.Count; i++)
        {
            var label = new Label
            {
                Text = items[i],
                TextColor = color,
                FontSize = 12,
                LineBreakMode = LineBreakMode.WordWrap
            };
            Grid.SetColumn(label, i % 2);
            Grid.SetRow(label, i / 2);
            grid.Children.Add(label);
        }

        container.Children.Add(grid);
    }

    // A resolved weapon line: the text as written, its lookup base name, the matching metadata
    // weapon(s) (multiple = firing modes), and whether any of them define a range band.
    private readonly record struct WeaponLineEntry(
        string Line, string BaseName, IReadOnlyList<Weapon> Weapons, bool HasRangeBand);

    private List<WeaponLineEntry> ResolveWeaponLines(string? weaponsText)
    {
        var entries = new List<WeaponLineEntry>();
        foreach (var line in SplitLines(weaponsText))
        {
            var baseName = Regex.Match(line, @"^[^(]+").Value.Trim();
            var searchName = NormalizeWeaponBaseName(baseName);
            var matches = _metadataProvider?.SearchWeaponsByName(searchName) ?? [];
            var exact = matches
                .Where(w => string.Equals(w.Name, searchName, StringComparison.OrdinalIgnoreCase))
                .ToList();
            var weapons = exact.Count > 0 ? exact : [.. matches];
            var hasRangeBand = weapons.Any(w => !string.IsNullOrWhiteSpace(w.DistanceJson));
            entries.Add(new WeaponLineEntry(line, baseName, weapons, hasRangeBand));
        }
        return entries;
    }

    // Maps army-builder weapon abbreviations to their canonical metadata names.
    private static string NormalizeWeaponBaseName(string baseName) => baseName switch
    {
        "T2 CCW" => "T2 CC Weapon",
        _ => baseName
    };

    // Ranged (non-CC) weapons: those with a range band first, then those without, each alphabetical.
    // Lines with no metadata match fall to the end as plain labels.
    private void AppendRangedWeaponSection(VerticalStackLayout container, string? weaponsText, bool hasXVisor, int damageReduction, string? ammoModifier = null, int burstBonus = 0)
    {
        var entries = ResolveWeaponLines(weaponsText);

        var banded = entries.Where(e => e.Weapons.Count > 0 && e.HasRangeBand)
            .OrderBy(e => e.BaseName, StringComparer.OrdinalIgnoreCase);
        var nonBanded = entries.Where(e => e.Weapons.Count > 0 && !e.HasRangeBand)
            .OrderBy(e => e.BaseName, StringComparer.OrdinalIgnoreCase);
        var unmatched = entries.Where(e => e.Weapons.Count == 0)
            .OrderBy(e => e.BaseName, StringComparer.OrdinalIgnoreCase);

        foreach (var e in banded.Concat(nonBanded).Concat(unmatched))
            RenderWeaponBlock(container, e, hasXVisor, damageReduction, ammoModifier, burstBonus);
    }

    // Renders weapon card(s) for a single line from the army list.
    // All weapons (single or multi-mode) use the same card style: name at top, stats on the right, range bar at bottom.
    private void RenderWeaponBlock(VerticalStackLayout container, WeaponLineEntry entry, bool hasXVisor, int damageReduction, string? ammoModifier = null, int burstBonus = 0)
    {
        if (entry.Weapons.Count == 0)
        {
            container.Children.Add(BuildUnmatchedWeaponCard(entry.Line));
            return;
        }

        container.Children.Add(BuildMultiModeWeaponCard(entry.Weapons, hasXVisor, damageReduction, entry.Line, ammoModifier, burstBonus));
    }

    // All CC weapons grouped into a single "CC Weapon" card, each weapon as a mode row, ordered alphabetically.
    private void AppendCcWeaponSection(VerticalStackLayout container, string? weaponsText, bool hasXVisor, int ccSr)
    {
        var entries = ResolveWeaponLines(weaponsText);

        var allWeapons = entries
            .Where(e => e.Weapons.Count > 0)
            .SelectMany(e => e.Weapons)
            .OrderBy(w => string.IsNullOrWhiteSpace(w.Mode) ? w.Name : w.Mode, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var unmatchedLines = entries.Where(e => e.Weapons.Count == 0).Select(e => e.Line).ToList();

        if (allWeapons.Count == 0 && unmatchedLines.Count == 0) return;

        if (allWeapons.Count > 0)
            container.Children.Add(BuildMultiModeWeaponCard(allWeapons, hasXVisor, ccSr, "CC Weapon"));

        foreach (var line in unmatchedLines)
            container.Children.Add(BuildUnmatchedWeaponCard(line));
    }

    // Minimal card for weapons present in the army list but absent from the metadata DB.
    // Keeps the visual language consistent — no grey plain-text fallbacks.
    private static Border BuildUnmatchedWeaponCard(string displayName)
    {
        return new Border
        {
            BackgroundColor = Color.FromArgb("#111827"),
            Stroke = Color.FromArgb("#4B5563"),
            StrokeThickness = 1,
            Padding = new Thickness(10, 8),
            StrokeShape = new RoundRectangle { CornerRadius = 6 },
            Margin = new Thickness(0, 0, 0, 4),
            Content = new Label
            {
                Text = displayName,
                TextColor = Colors.White,
                FontAttributes = FontAttributes.Bold,
                FontSize = 13,
                VerticalTextAlignment = TextAlignment.Center
            }
        };
    }

    // One combined tappable card for a weapon with multiple firing modes.
    // Layout: weapon name (bold centered) → mode rows → single range bar at bottom (first mode's).
    // Tapping opens a popup showing every mode in full detail.
    private Border BuildMultiModeWeaponCard(IReadOnlyList<Weapon> weapons, bool hasXVisor, int damageReduction, string displayName, string? ammoModifier = null, int burstBonus = 0)
    {
        var isSuppressiveFire = displayName.Equals("Suppressive Fire Mode", StringComparison.OrdinalIgnoreCase);
        // True when the weapon has a Deployable mode among its modes (e.g. Drop Bears).
        // Non-deployable modes of such weapons receive burst bonuses only — no ammo or damage modifiers.
        var hasDeployableMode = weapons.Any(w => w.Mode?.Contains("Deployable", StringComparison.OrdinalIgnoreCase) ?? false);

        var stack = new VerticalStackLayout { Spacing = 0 };

        // Weapon name centred at the top of the card.
        stack.Children.Add(new Label
        {
            Text = displayName,
            TextColor = Colors.White,
            FontSize = 15,
            FontAttributes = FontAttributes.Bold,
            HorizontalTextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 6)
        });

        for (var i = 0; i < weapons.Count; i++)
        {
            var weapon = weapons[i];

            if (i > 0)
                stack.Children.Add(new BoxView
                {
                    HeightRequest = 1,
                    Color = Color.FromArgb("#374151"),
                    Margin = new Thickness(0, 5, 0, 5)
                });

            var row = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Auto }
                },
                ColumnSpacing = 8,
                VerticalOptions = LayoutOptions.Center
            };

            var modeLabel = new Label
            {
                Text = string.IsNullOrWhiteSpace(weapon.Mode) ? weapon.Name : weapon.Mode,
                TextColor = Colors.White,
                FontAttributes = FontAttributes.Bold,
                Style = (Style)Application.Current!.Resources["LabelCaption"],
                VerticalTextAlignment = TextAlignment.Center,
                LineBreakMode = LineBreakMode.TailTruncation
            };
            Grid.SetColumn(modeLabel, 0);
            row.Children.Add(modeLabel);

            var rightStack = new HorizontalStackLayout { Spacing = 3, VerticalOptions = LayoutOptions.Center };

            // Deployable mode or BS Weapon (WIP) → no BS modifiers at all.
            // Non-deployable mode of a deployable weapon (e.g. Drop Bears BS Mode) → burst bonus only.
            // Suppressive Fire Mode → burst bonus and ammo modifier suppressed, damage reduction kept.
            var isDeployable = (weapon.Mode?.Contains("Deployable", StringComparison.OrdinalIgnoreCase) ?? false)
                               || IsBsWipWeapon(weapon);
            int effectiveDmgReduction;
            string? effectiveAmmoMod;
            int effectiveBurstBonus;
            if (isDeployable)
            {
                effectiveDmgReduction = 0;
                effectiveAmmoMod      = null;
                effectiveBurstBonus   = 0;
            }
            else if (hasDeployableMode)
            {
                effectiveDmgReduction = 0;
                effectiveAmmoMod      = null;
                effectiveBurstBonus   = isSuppressiveFire ? 0 : burstBonus;
            }
            else
            {
                effectiveDmgReduction = damageReduction;
                effectiveAmmoMod      = isSuppressiveFire ? null : ammoModifier;
                effectiveBurstBonus   = isSuppressiveFire ? 0 : burstBonus;
            }

            var burst = weapon.Burst;
            if (effectiveBurstBonus > 0 && !IsWeaponStatDash(burst) && int.TryParse(burst!.Trim(), out var bv))
                burst = (bv + effectiveBurstBonus).ToString();
            if (!IsWeaponStatDash(burst))
                rightStack.Children.Add(BuildModeStatChip("B", burst!));

            var dmg = ReduceWeaponDamage(weapon.Damage, effectiveDmgReduction);
            if (!IsWeaponStatDash(dmg))
                rightStack.Children.Add(BuildModeStatChip("DAM", dmg!));

            var ammo = CombineAmmo(ResolveAmmoName(weapon), effectiveAmmoMod);
            if (!string.IsNullOrWhiteSpace(ammo))
                rightStack.Children.Add(BuildModeAmmoPill(ammo!));

            rightStack.Children.Add(new Label
            {
                Text = "›",
                TextColor = Color.FromArgb("#6B7280"),
                FontSize = 18,
                VerticalTextAlignment = TextAlignment.Center
            });

            Grid.SetColumn(rightStack, 1);
            row.Children.Add(rightStack);
            stack.Children.Add(row);
        }

        // Single range bar at the bottom — from the first mode that has one.
        var firstWithRange = weapons.FirstOrDefault(w => !string.IsNullOrWhiteSpace(w.DistanceJson));
        if (firstWithRange is not null)
            stack.Children.Add(new WeaponRangeBandBarView
            {
                DistanceJson = firstWithRange.DistanceJson,
                ShowUnitsInInches = _showUnitsInInches,
                BarHeightRequest = 44,
                XVisorActive = hasXVisor,
                Margin = new Thickness(0, 6, 0, 0),
                HorizontalOptions = LayoutOptions.Fill
            });

        var border = new Border
        {
            BackgroundColor = Color.FromArgb("#111827"),
            Stroke = Color.FromArgb("#4B5563"),
            StrokeThickness = 1,
            Padding = new Thickness(10, 8),
            StrokeShape = new RoundRectangle { CornerRadius = 6 },
            Margin = new Thickness(0, 0, 0, 4),
            Content = stack
        };

        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) => _ = ShowMultiModeWeaponDetailPopupAsync(weapons, hasXVisor, damageReduction, displayName, ammoModifier, burstBonus);
        border.GestureRecognizers.Add(tap);

        return border;
    }

    // Popup showing every firing mode in full detail for a multi-mode weapon.
    private async Task ShowMultiModeWeaponDetailPopupAsync(IReadOnlyList<Weapon> weapons, bool hasXVisor, int damageReduction, string? title = null, string? ammoModifier = null, int burstBonus = 0)
    {
        var originalContent = Content;
        var tcs = new TaskCompletionSource();

        var isSuppressiveFire = title?.Equals("Suppressive Fire Mode", StringComparison.OrdinalIgnoreCase) ?? false;
        var hasDeployableMode = weapons.Any(w => w.Mode?.Contains("Deployable", StringComparison.OrdinalIgnoreCase) ?? false);

        var stack = new VerticalStackLayout
        {
            Padding = new Thickness(12, 8, 12, 8),
            Spacing = 8,
            BackgroundColor = Color.FromArgb("#0F1923")
        };

        stack.Children.Add(new Label
        {
            Text = title ?? weapons[0].Name,
            TextColor = Colors.White,
            FontAttributes = FontAttributes.Bold,
            FontSize = 16,
            HorizontalTextAlignment = TextAlignment.Center
        });

        foreach (var weapon in weapons)
        {
            var isDeployable = (weapon.Mode?.Contains("Deployable", StringComparison.OrdinalIgnoreCase) ?? false)
                               || IsBsWipWeapon(weapon);
            int effectiveDmgReduction;
            string? effectiveAmmoMod;
            int effectiveBurstBonus;
            if (isDeployable)
            {
                effectiveDmgReduction = 0;
                effectiveAmmoMod      = null;
                effectiveBurstBonus   = 0;
            }
            else if (hasDeployableMode)
            {
                effectiveDmgReduction = 0;
                effectiveAmmoMod      = null;
                effectiveBurstBonus   = isSuppressiveFire ? 0 : burstBonus;
            }
            else
            {
                effectiveDmgReduction = damageReduction;
                effectiveAmmoMod      = isSuppressiveFire ? null : ammoModifier;
                effectiveBurstBonus   = isSuppressiveFire ? 0 : burstBonus;
            }

            stack.Children.Add(new WeaponDetailCardView
            {
                Weapon = weapon,
                AmmoType = CombineAmmo(ResolveAmmoName(weapon), effectiveAmmoMod),
                Compact = false,
                ShowUnitsInInches = _showUnitsInInches,
                RangeBandHeightRequest = 88,
                XVisorActive = hasXVisor,
                DamageReduction = effectiveDmgReduction,
                BurstBonus = effectiveBurstBonus
            });
        }

        var doneBtn = new Button
        {
            Text = "Done",
            BackgroundColor = Color.FromArgb("#22C55E"),
            TextColor = Colors.White,
            FontAttributes = FontAttributes.Bold,
            CornerRadius = 8,
            HeightRequest = 36,
            Margin = new Thickness(0, 4, 0, 0)
        };
        doneBtn.Clicked += (_, _) =>
        {
            Content = originalContent;
            tcs.TrySetResult();
        };
        stack.Children.Add(doneBtn);

        Content = new ScrollView
        {
            BackgroundColor = Color.FromArgb("#0F1923"),
            Content = stack
        };

        await tcs.Task;
    }

    // ── Helpers used by BuildMultiModeWeaponCard (mirrors WeaponDetailCardView internals) ─────

    private static bool IsWeaponStatDash(string? v) =>
        string.IsNullOrWhiteSpace(v) || v.Trim() == "-";

    private static string? ReduceWeaponDamage(string? damage, int reduction)
    {
        if (reduction <= 0 || IsWeaponStatDash(damage)) return damage;
        if (int.TryParse(damage!.Trim(), out var val))
            return Math.Max(1, val - reduction).ToString();
        return damage;
    }

    private static Border BuildModeStatChip(string label, string value)
    {
        var inner = new HorizontalStackLayout { Spacing = 3, VerticalOptions = LayoutOptions.Center };
        inner.Children.Add(new Label
        {
            Text = label,
            Style = (Style)Application.Current!.Resources["LabelCaption"],
            TextColor = Color.FromArgb("#9CA3AF"),
            VerticalTextAlignment = TextAlignment.Center
        });
        inner.Children.Add(new Label
        {
            Text = value,
            Style = (Style)Application.Current!.Resources["LabelCaption"],
            FontAttributes = FontAttributes.Bold,
            TextColor = Colors.White,
            VerticalTextAlignment = TextAlignment.Center
        });
        return new Border
        {
            BackgroundColor = Color.FromArgb("#1F2937"),
            Stroke = Color.FromArgb("#374151"),
            StrokeThickness = 1,
            Padding = new Thickness(6, 2),
            VerticalOptions = LayoutOptions.Center,
            StrokeShape = new RoundRectangle { CornerRadius = 6 },
            Content = inner
        };
    }

    private static Border BuildModeAmmoPill(string ammo)
    {
        return new Border
        {
            BackgroundColor = Color.FromArgb("#1F2937"),
            Stroke = Color.FromArgb("#F59E0B"),
            StrokeThickness = 1,
            Padding = new Thickness(6, 1),
            VerticalOptions = LayoutOptions.Center,
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            Content = new Label
            {
                Text = ammo,
                Style = (Style)Application.Current!.Resources["LabelCaption"],
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#F59E0B"),
                VerticalTextAlignment = TextAlignment.Center
            }
        };
    }

    // Small pill conveying the CC Attack saving-roll reduction applied to a CC weapon block.

    private void AppendStandardActionCards(VerticalStackLayout container, bool hasXVisor, int damageReduction = 0)
    {
        // Discover is a WIP check — no damage reduction, no BS modifiers.
        AppendNamedWeaponCard(container, "Discover", "Discover", hasXVisor);
        // Suppressive Fire Mode: damage reduction applies but ammo modifier and burst bonus do not.
        AppendNamedWeaponCard(container, "Suppressive Fire Mode", "Suppressive Fire Mode Weapon", hasXVisor, damageReduction);
    }

    private void AppendNamedWeaponCard(VerticalStackLayout container, string displayName, string searchName, bool hasXVisor, int damageReduction = 0, string? ammoModifier = null, int burstBonus = 0)
    {
        var matches = _metadataProvider?.SearchWeaponsByName(searchName) ?? [];
        var exact = matches
            .Where(w => string.Equals(w.Name, searchName, StringComparison.OrdinalIgnoreCase))
            .ToList();
        var weapons = exact.Count > 0 ? exact : [.. matches];
        if (weapons.Count == 0) return;

        container.Children.Add(BuildMultiModeWeaponCard(weapons, hasXVisor, damageReduction, displayName, ammoModifier, burstBonus));
    }

    // Resolves a weapon's ammunition id to its display name (e.g. "AP+Exp"), or null when unknown.
    private string? ResolveAmmoName(Weapon weapon)
    {
        if (weapon.AmmunitionId is not int id) return null;
        _ammoNamesById ??= (_metadataProvider?.GetAmmunitions() ?? [])
            .GroupBy(a => a.Id)
            .ToDictionary(g => g.Key, g => g.First().Name);
        return _ammoNamesById.TryGetValue(id, out var name) ? name : null;
    }

    private static IReadOnlyList<string> SplitLines(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return [];
        return text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
                   .Select(x => x.Trim())
                   .Where(x => !string.IsNullOrWhiteSpace(x) && x != "-")
                   .ToList();
    }

    // ── Navigation ───────────────────────────────────────────────────────────

    private async void OnEndGameClicked(object sender, EventArgs e) => await ConfirmAndEndGame();

    private async void OnEndRoundButtonClicked(object sender, EventArgs e) => await ConfirmAndEndGame();

    private async Task ConfirmAndEndGame()
    {
        var confirmed = await DisplayAlert("End Game", "Are you sure you want to end the game?", "Yes", "No");
        if (!confirmed) return;

        // Determine which campaign mission this is (completed rounds + 1)
        var seasonFile = await SeasonFileService.LoadSeasonFileAsync(_seasonFilePath);
        var deploymentXp = SeasonFileService.ResolveCurrentRound(seasonFile) + 1;

        if (seasonFile is not null)
        {
            seasonFile.Rounds.Add(new SeasonRound
            {
                RoundIndex = deploymentXp,
                MissionResults = new SeasonMissionResult
                {
                    UnitsDeployed = _unitTracker.Count,
                    EliteDeploymentMet = _isEliteDeployment,
                    MissionRound = _currentRound
                }
            });
            await SeasonFileService.SaveSeasonFileAsync(_seasonFilePath, seasonFile);
        }

        ExperiencePageData.Units = _unitTracker
            .Select(t => new ExperienceUnitResult
            {
                EntryIndex = t.Unit.EntryIndex,
                Name = t.Unit.Name,
                CachedLogoPath = t.Unit.CachedLogoPath,
                PackagedLogoPath = t.Unit.PackagedLogoPath,
                DeploymentXp = deploymentXp,
                IsEliteDeployment = _isEliteDeployment,
                IsConsciousAtEnd = t.Unit.WoundStateKey is "Healthy" or "Wounded" or "NwiDown",
                GainedInjury = false,
                XpData = t.Unit.XpData,
                IsCaptain = t.Unit.IsLieutenant,
                IsLieutenant = t.Unit.IsLieutenant,
                UnitPh = t.Unit.UnitPh,
                UnitBs = t.Unit.UnitBs,
                UnitCc = t.Unit.UnitCc,
                UnitWip = t.Unit.UnitWip,
                UnitArm = t.Unit.UnitArm,
                Renown = t.Unit.Renown,
                Skills = t.Unit.Skills,
                Equipment = t.Unit.Equipment
            })
            .ToList();

        var deadUnits = _unitTracker
            .Where(t => t.Unit.WoundStateKey == "Dead")
            .Select(t => new InjuryItem
            {
                EntryIndex = t.Unit.EntryIndex,
                Name = t.Unit.Name,
                PhValue = t.Unit.UnitPh,
                CachedLogoPath = t.Unit.CachedLogoPath,
                PackagedLogoPath = t.Unit.PackagedLogoPath
            })
            .ToList();

        MissionOutcomePageData.CurrentRound = deploymentXp;
        MissionOutcomePageData.Victory = null;
        MissionOutcomePageData.PointsScored = null;

        var encodedPath = Uri.EscapeDataString(_companyFilePath);
        var encodedSeasonPath = Uri.EscapeDataString(_seasonFilePath);

        if (deadUnits.Count > 0)
        {
            InjuryPageData.PendingInjuries = deadUnits;
            await Shell.Current.GoToAsync($"{nameof(InjuriesPage)}?companyFilePath={encodedPath}&seasonFilePath={encodedSeasonPath}");
        }
        else
        {
            await Shell.Current.GoToAsync($"{nameof(MissionOutcomePage)}?companyFilePath={encodedPath}&seasonFilePath={encodedSeasonPath}");
        }
    }

    private void OnNextRoundClicked(object sender, EventArgs e)
    {
        _currentRound++;
        RoundLabel.Text = $"Round {_currentRound}";
        EndRoundButton.IsVisible = _currentRound >= 3;
        NextRoundButton.IsVisible = _currentRound < 4;
        RecalculateOrders();
    }

    private async Task ShowXpPopupAsync(DeploymentUnitItem unit)
    {
        var xp = unit.XpData;
        var originalContent = Content;
        var tcs = new TaskCompletionSource();

        var totalLabel = new Label
        {
            TextColor = Color.FromArgb("#22C55E"),
            FontSize = 12,
            FontAttributes = FontAttributes.Bold,
            HorizontalTextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 2, 0, 0)
        };
        void UpdateTotal() => totalLabel.Text = $"Total XP Earned: {xp.TotalXp}";
        UpdateTotal();

        var stack = new VerticalStackLayout
        {
            Padding = new Thickness(12, 6, 12, 6),
            Spacing = 0,
            BackgroundColor = Color.FromArgb("#0F1923")
        };
        stack.Children.Add(new Label
        {
            Text = unit.Name,
            TextColor = Colors.White,
            FontAttributes = FontAttributes.Bold,
            FontSize = 15,
            HorizontalTextAlignment = TextAlignment.Center
        });
        stack.Children.Add(totalLabel);

        void AddSection(string description)
        {
            stack.Children.Add(new BoxView
            {
                HeightRequest = 1,
                Color = Color.FromArgb("#374151"),
                Margin = new Thickness(0, 7, 0, 4)
            });
            stack.Children.Add(new Label
            {
                Text = description,
                TextColor = Color.FromArgb("#D1D5DB"),
                FontSize = 12,
                LineBreakMode = LineBreakMode.WordWrap
            });
        }

        // Suppress the CheckedChanged that fires when IsChecked is set during initialisation.
        CheckBox MakeCheckBox(bool current, Action<bool> onChanged)
        {
            var cb = new CheckBox { Color = Color.FromArgb("#22C55E") };
            var suppress = true;
            cb.CheckedChanged += (_, e) =>
            {
                if (suppress) return;
                onChanged(e.Value);
                UpdateTotal();
            };
            cb.IsChecked = current;
            suppress = false;
            return cb;
        }

        void AddRow(CheckBox cb, string label)
        {
            var lbl = new Label
            {
                Text = label,
                TextColor = Color.FromArgb("#9CA3AF"),
                FontSize = 12,
                VerticalTextAlignment = TextAlignment.Center
            };
            var row = new HorizontalStackLayout { Spacing = 4, Margin = new Thickness(0, 2, 0, 0) };
            row.Children.Add(cb);
            row.Children.Add(lbl);
            stack.Children.Add(row);
        }

        // Sequential multi-check with equal-width columns.
        // Only the highest-checked and lowest-unchecked checkboxes are enabled.
        void AddMultiChecks(bool[] arr)
        {
            var checkboxes = new CheckBox[arr.Length];

            void RefreshStates()
            {
                var highestChecked = -1;
                for (var i = arr.Length - 1; i >= 0; i--)
                    if (arr[i]) { highestChecked = i; break; }

                var lowestUnchecked = -1;
                for (var i = 0; i < arr.Length; i++)
                    if (!arr[i]) { lowestUnchecked = i; break; }

                for (var i = 0; i < checkboxes.Length; i++)
                    checkboxes[i].IsEnabled = i == highestChecked || i == lowestUnchecked;
            }

            var grid = new Grid { ColumnSpacing = 4, Margin = new Thickness(0, 2, 0, 0) };
            for (var i = 0; i < arr.Length; i++)
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });

            for (var i = 0; i < arr.Length; i++)
            {
                var idx = i;
                var cb = MakeCheckBox(arr[idx], v =>
                {
                    arr[idx] = v;
                    RefreshStates();
                });
                checkboxes[idx] = cb;

                var numLbl = new Label
                {
                    Text = $"#{idx + 1}",
                    TextColor = Color.FromArgb("#9CA3AF"),
                    FontSize = 12,
                    VerticalTextAlignment = TextAlignment.Center
                };
                var item = new HorizontalStackLayout { Spacing = 2, HorizontalOptions = LayoutOptions.Center };
                item.Children.Add(cb);
                item.Children.Add(numLbl);
                Grid.SetColumn(item, idx);
                grid.Children.Add(item);
            }
            stack.Children.Add(grid);
            RefreshStates();
        }

        // ── XP Categories ──────────────────────────────────────────────────────

        AddSection("Assisting an allied unit\n(engineering / doctoring / medikit / gizmokit / casevac)");
        AddMultiChecks(xp.Assist);

        AddSection("Inflicting 1+ state(s) (stunned / spotlit / immobilized / isolated) in one order");
        AddMultiChecks(xp.InflictState);

        CheckBox? attemptButtonCb = null;
        CheckBox? succeedButtonCb = null;

        AddSection("Making a roll to accomplish an objective that is NOT attacking the opponent");
        {
            var cbAttempt = MakeCheckBox(xp.AttemptButton, v =>
            {
                xp.AttemptButton = v;
                if (!v && xp.SucceedButton)
                {
                    xp.SucceedButton = false;
                    if (succeedButtonCb is not null) succeedButtonCb.IsChecked = false;
                }
            });
            var cbSucceed = MakeCheckBox(xp.SucceedButton, v =>
            {
                xp.SucceedButton = v;
                if (v && !xp.AttemptButton)
                {
                    xp.AttemptButton = true;
                    if (attemptButtonCb is not null) attemptButtonCb.IsChecked = true;
                }
            });
            attemptButtonCb = cbAttempt;
            succeedButtonCb = cbSucceed;

            var pairGrid = new Grid { ColumnSpacing = 4, Margin = new Thickness(0, 2, 0, 0) };
            pairGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            pairGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            var leftItem = new HorizontalStackLayout { Spacing = 4 };
            leftItem.Children.Add(cbAttempt);
            leftItem.Children.Add(new Label { Text = "Attempted", TextColor = Color.FromArgb("#9CA3AF"), FontSize = 12, VerticalTextAlignment = TextAlignment.Center });
            var rightItem = new HorizontalStackLayout { Spacing = 4, HorizontalOptions = LayoutOptions.End };
            rightItem.Children.Add(cbSucceed);
            rightItem.Children.Add(new Label { Text = "Succeeded", TextColor = Color.FromArgb("#9CA3AF"), FontSize = 12, VerticalTextAlignment = TextAlignment.Center });
            Grid.SetColumn(leftItem, 0);
            Grid.SetColumn(rightItem, 1);
            pairGrid.Children.Add(leftItem);
            pairGrid.Children.Add(rightItem);
            stack.Children.Add(pairGrid);
        }

        CheckBox? scanEnemyCb = null;
        CheckBox? scanEnemyFoCb = null;

        AddSection("Scan Enemy - LoF to target unconscious, immobilized, or shasvastii state. That unit gets the Targeted state.");
        {
            var cbScan = MakeCheckBox(xp.ScanEnemy, v =>
            {
                xp.ScanEnemy = v;
                if (!v && xp.ScanEnemyFo)
                {
                    xp.ScanEnemyFo = false;
                    if (scanEnemyFoCb is not null) scanEnemyFoCb.IsChecked = false;
                }
            });
            var cbScanFo = MakeCheckBox(xp.ScanEnemyFo, v =>
            {
                xp.ScanEnemyFo = v;
                if (v && !xp.ScanEnemy)
                {
                    xp.ScanEnemy = true;
                    if (scanEnemyCb is not null) scanEnemyCb.IsChecked = true;
                }
            });
            scanEnemyCb = cbScan;
            scanEnemyFoCb = cbScanFo;

            var pairGrid = new Grid { ColumnSpacing = 4, Margin = new Thickness(0, 2, 0, 0) };
            pairGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            pairGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            var leftItem = new HorizontalStackLayout { Spacing = 4 };
            leftItem.Children.Add(cbScan);
            leftItem.Children.Add(new Label { Text = "Scanned", TextColor = Color.FromArgb("#9CA3AF"), FontSize = 12, VerticalTextAlignment = TextAlignment.Center });
            var rightItem = new HorizontalStackLayout { Spacing = 4, HorizontalOptions = LayoutOptions.End };
            rightItem.Children.Add(cbScanFo);
            rightItem.Children.Add(new Label { Text = "Scanned with FO", TextColor = Color.FromArgb("#9CA3AF"), FontSize = 12, VerticalTextAlignment = TextAlignment.Center });
            Grid.SetColumn(leftItem, 0);
            Grid.SetColumn(rightItem, 1);
            pairGrid.Children.Add(leftItem);
            pairGrid.Children.Add(rightItem);
            stack.Children.Add(pairGrid);
        }

        AddSection("Tag and Bag [Mercs Skill]");
        AddRow(MakeCheckBox(xp.TagAndBag, v => xp.TagAndBag = v), "Tagged and Bagged");

        stack.Children.Add(new BoxView
        {
            HeightRequest = 1,
            Color = Color.FromArgb("#374151"),
            Margin = new Thickness(0, 8, 0, 4)
        });
        var doneBtn = new Button
        {
            Text = "Done",
            BackgroundColor = Color.FromArgb("#22C55E"),
            TextColor = Colors.White,
            FontAttributes = FontAttributes.Bold,
            CornerRadius = 8,
            HeightRequest = 36
        };
        doneBtn.Clicked += (_, _) =>
        {
            Content = originalContent;
            tcs.TrySetResult();
        };
        stack.Children.Add(doneBtn);

        Content = new ScrollView
        {
            BackgroundColor = Color.FromArgb("#0F1923"),
            Content = stack
        };

        await tcs.Task;
    }

    private void RecalculateOrders()
    {
        int ltCount = 0, impCount = 0, irrCount = 0, regCount = 0;
        foreach (var (unit, isLt, isImp, isIrr) in _unitTracker)
        {
            if (unit.WoundStateKey is not ("Healthy" or "Wounded" or "NwiDown")) continue;
            if (isLt) ltCount++;
            if (isImp) impCount++;
            if (isIrr) irrCount++;
            if (!isIrr) regCount++;
        }
        // Mercs always generates one bonus regular order
        regCount++;

        LtCountLabel.Text  = ltCount.ToString();
        ImpCountLabel.Text = impCount.ToString();
        IrrCountLabel.Text = irrCount.ToString();
        RegCountLabel.Text = regCount.ToString();
        LtGreyCountLabel.Text  = "0";
        ImpGreyCountLabel.Text = "0";
        IrrGreyCountLabel.Text = "0";
        RegGreyCountLabel.Text = "0";

        foreach (var (u, canvas, isGrey) in _eliteTacticalIcons)
        {
            isGrey[0] = u.WoundStateKey is not ("Healthy" or "Wounded" or "NwiDown");
            canvas.InvalidateSurface();
        }
    }

    // ── Resolution helpers ────────────────────────────────────────────────────

    private string ResolveSavedCharacteristics(int sourceFactionId, SavedCompanyEntry entry)
    {
        var names = ResolveCodeNames(sourceFactionId, entry.CurrentCharacteristicCodes, "chars");
        names.AddRange(entry.CustomCharacteristics ?? []);
        return JoinOrDash(names);
    }

    private static bool HasOrderKeyword(string characteristics, string keyword) =>
        characteristics
            .Split(['\r', '\n', ','], StringSplitOptions.RemoveEmptyEntries)
            .Any(s => s.Trim().Equals(keyword, StringComparison.OrdinalIgnoreCase));

    private static bool HasSkill(string skills, string skillName) =>
        skills.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
              .Any(s => s.Trim().Equals(skillName, StringComparison.OrdinalIgnoreCase));

    private SKPicture? GetWoundPicture(string stateKey) => stateKey switch
    {
        "Healthy"                              => _woundHealthyPicture,
        "Wounded"                              => _woundWoundedPicture,
        "NwiDown"                              => _woundMadPicture,
        "KnockedOut" or "KnockedOutBody2"      => _woundKoPicture,
        _                                      => _woundDeadPicture
    };

    private string ResolveSavedSkills(int sourceFactionId, SavedCompanyEntry entry)
    {
        var names = ResolveCodeNames(sourceFactionId, entry.CurrentSkillCodes, "skills");
        names.AddRange(entry.CustomSkills ?? []);
        return JoinOrDash(names);
    }

    private string ResolveSavedEquipment(int sourceFactionId, SavedCompanyEntry entry)
    {
        var names = ResolveCodeNames(sourceFactionId, entry.CurrentEquipmentCodes, "equip");
        names.AddRange(entry.CustomEquipment ?? []);
        return JoinOrDash(names);
    }

    private (string Ranged, string Melee) ResolveSavedWeapons(int sourceFactionId, SavedCompanyEntry entry)
    {
        var weapons = ResolveCodeNames(sourceFactionId, entry.CurrentWeaponCodes, "weapons")
            .Where(x => !string.IsNullOrWhiteSpace(x) && x.Trim() != "-")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        weapons.AddRange((entry.CustomWeapons ?? [])
            .Where(x => !string.IsNullOrWhiteSpace(x) && x.Trim() != "-"));
        weapons = weapons.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        if (weapons.Count == 0) return ("-", "-");
        var ranged = weapons.Where(x => !CompanyProfileTextService.IsMeleeWeaponName(x)).ToList();
        var melee  = weapons.Where(CompanyProfileTextService.IsMeleeWeaponName).ToList();
        return (JoinOrDash(ranged), JoinOrDash(melee));
    }

    private List<string> ResolveCodeNames(int sourceFactionId, IEnumerable<CompanySavedCodeRef> codes, string section)
    {
        var list = (codes ?? []).Where(x => x is not null && x.Id > 0).ToList();
        if (list.Count == 0) return [];

        var snapshot = _armyDataService?.GetFactionSnapshot(sourceFactionId);
        var lookup   = CompanyUnitDetailsShared.BuildIdNameLookup(snapshot?.FiltersJson, section);
        var extras   = CompanyUnitDetailsShared.BuildIdNameLookup(snapshot?.FiltersJson, "extras");
        var resolved = new List<string>();

        foreach (var code in list)
        {
            if (!lookup.TryGetValue(code.Id, out var name) || string.IsNullOrWhiteSpace(name)) continue;
            var display = name.Trim();
            var extraParts = (code.Extra ?? [])
                .Distinct()
                .Select(id => extras.TryGetValue(id, out var n) ? n?.Trim() : null)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Cast<string>()
                .ToList();
            if (extraParts.Count > 0)
                display = $"{display} ({string.Join(", ", extraParts)})";
            resolved.Add(display);
        }
        return resolved;
    }

    private static string JoinOrDash(IEnumerable<string> values)
    {
        var lines = values
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Where(x => x != "-")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        return lines.Count == 0 ? "-" : string.Join(Environment.NewLine, lines);
    }

    private static string AppendChoices(string? baseText, IReadOnlyList<string> additions)
    {
        if (additions.Count == 0) return baseText ?? "-";
        var lines = (baseText ?? string.Empty)
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim()).Where(x => !string.IsNullOrWhiteSpace(x) && x != "-")
            .ToList();
        var seen = new HashSet<string>(lines, StringComparer.OrdinalIgnoreCase);
        foreach (var a in additions)
            if (seen.Add(a)) lines.Add(a);
        return lines.Count == 0 ? "-" : string.Join(Environment.NewLine, lines);
    }

    private static IReadOnlyList<string> CollectCaptainChoices(params string[] choices) =>
        choices.Select(NormalizeCaptainChoice)
               .Where(x => !string.IsNullOrWhiteSpace(x))
               .Distinct(StringComparer.OrdinalIgnoreCase)
               .ToList();

    private static string NormalizeCaptainChoice(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim() == "-") return string.Empty;
        return System.Text.RegularExpressions.Regex
            .Replace(value.Trim(), @"^\s*\([-+]?\d+\)\s*-\s*", string.Empty).Trim();
    }

    private static int ResolveEffectiveSourceFactionId(SavedCompanyEntry entry)
    {
        if (entry.SourceFactionId == TagCompanyFactionId || entry.LogoSourceFactionId == TagCompanyFactionId)
            return TagCompanyFactionId;
        var base_ = entry.BaseUnitName ?? string.Empty;
        if (base_.Contains("Repurposed Mining Equipment", StringComparison.OrdinalIgnoreCase) ||
            base_.Contains("Turtlemek", StringComparison.OrdinalIgnoreCase))
            return TagCompanyFactionId;
        return entry.SourceFactionId;
    }

    private static string InferVitalityHeader(string? unitTypeCode)
    {
        var n = unitTypeCode?.Trim().ToUpperInvariant();
        return n is "TAG" or "REM" or "PERIPHERAL" ? "STR" : "VITA";
    }

    private static string BuildUnitBaseDisplayName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "Unit";
        var s = System.Text.RegularExpressions.Regex.Replace(name, @"\s*\([^)]*\)\s*", " ").Trim();
        s = System.Text.RegularExpressions.Regex.Replace(s, @"\s{2,}", " ").Trim();
        return string.IsNullOrWhiteSpace(s) ? "Unit" : s;
    }

    private static bool ContainsXVisor(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        return text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Any(line => line.Contains("X Visor", StringComparison.OrdinalIgnoreCase) ||
                         line.Contains("X-Visor", StringComparison.OrdinalIgnoreCase));
    }

    // Converts army-builder notation "BS Attack (-N SR)" → canonical "BS Attack (SR-N)" for display and parsing.
    private static string NormalizeBsAttackText(string? skills)
    {
        if (string.IsNullOrWhiteSpace(skills)) return string.Empty;
        return Regex.Replace(
            skills,
            @"(BS\s+Attack\s*\()-\s*(\d+)\s*SR\s*\)",
            "$1SR-$2)",
            RegexOptions.IgnoreCase);
    }

    private static int GetBsAttackDamageReduction(string? skills)
    {
        if (string.IsNullOrWhiteSpace(skills)) return 0;
        foreach (var line in skills.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            // Matches both "BS Attack (SR-N)" (canonical) and "BS Attack (-N SR)" (legacy) formats.
            var m = Regex.Match(line, @"BS\s+Attack\s*\(\s*(?:SR-|-)\s*(\d+)(?:\s*SR)?\s*\)", RegexOptions.IgnoreCase);
            if (m.Success && int.TryParse(m.Groups[1].Value, out var n))
                return n;
        }
        return 0;
    }

    private static string? GetBsAttackAmmoModifier(string? skills)
    {
        if (string.IsNullOrWhiteSpace(skills)) return null;
        foreach (var line in skills.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            var m = Regex.Match(line, @"\bBS\s+Attack\s*\(([^)]+)\)", RegexOptions.IgnoreCase);
            if (!m.Success) continue;
            var content = m.Groups[1].Value.Trim();
            // Exclude damage-reduction (SR-N, -N SR) and burst-bonus (+N B) — those are handled elsewhere.
            if (Regex.IsMatch(content, @"^SR[-\s]*\d|^-\s*\d.*SR|\+\s*\d+\s*B$", RegexOptions.IgnoreCase)) continue;
            return content;
        }
        return null;
    }

    private static int GetBsAttackBurstBonus(string? skills)
    {
        if (string.IsNullOrWhiteSpace(skills)) return 0;
        foreach (var line in skills.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            var m = Regex.Match(line, @"\bBS\s+Attack\s*\(\s*\+(\d+)\s*B\s*\)", RegexOptions.IgnoreCase);
            if (m.Success && int.TryParse(m.Groups[1].Value, out var n))
                return n;
        }
        return 0;
    }

    // Returns true when the weapon's special rules include "BS Weapon (WIP)", meaning it rolls WIP
    // rather than BS and therefore is unaffected by BS Attack (*) modifiers.
    private static bool IsBsWipWeapon(Weapon weapon)
    {
        if (string.IsNullOrWhiteSpace(weapon.PropertiesJson)) return false;
        if (!weapon.PropertiesJson.Contains("WIP", StringComparison.OrdinalIgnoreCase)) return false;
        try
        {
            var props = JsonSerializer.Deserialize<List<string>>(weapon.PropertiesJson) ?? [];
            return props.Any(p => Regex.IsMatch(p, @"\bBS\s+Weapon\s*\(\s*WIP\s*\)", RegexOptions.IgnoreCase));
        }
        catch { return false; }
    }

    // Combines a weapon's native ammo string with a BS Attack ammo modifier.
    // T2 uses AP+T2 notation (modifier first); all others append modifier after.
    private static string? CombineAmmo(string? baseAmmo, string? modifier)
    {
        if (string.IsNullOrWhiteSpace(modifier)) return baseAmmo;
        if (string.IsNullOrWhiteSpace(baseAmmo)) return modifier;
        if (baseAmmo.Equals("N", StringComparison.OrdinalIgnoreCase)) return modifier;
        if (baseAmmo.Contains(modifier, StringComparison.OrdinalIgnoreCase)) return baseAmmo;
        return $"{baseAmmo}+{modifier}";
    }

    // Parses the unit-wide "CC Attack (SR-N)" skill modifier; N reduces CC weapon damage.
    private static int GetCcAttackDamageReduction(string? skills)
    {
        if (string.IsNullOrWhiteSpace(skills)) return 0;
        foreach (var line in skills.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            var m = Regex.Match(line, @"CC\s+Attack\s*\(SR-(\d+)\)", RegexOptions.IgnoreCase);
            if (m.Success && int.TryParse(m.Groups[1].Value, out var n))
                return n;
        }
        return 0;
    }

    private async Task<Dictionary<int, List<InfinityMercsApp.Domain.Models.Season.SeasonUnitGear>>> LoadSeasonGearAsync()
    {
        var seasonFile = await InfinityMercsApp.Services.Season.SeasonFileService.LoadSeasonFileAsync(_seasonFilePath);
        return seasonFile?.UnitGear ?? [];
    }

    private static void ApplySeasonGearToUnit(
        int entryIndex,
        Dictionary<int, List<InfinityMercsApp.Domain.Models.Season.SeasonUnitGear>> seasonGear,
        ref string rangedWeapons, ref string meleeWeapons, ref string skills,
        ref string unitCc, ref string unitBs, ref string unitPh, ref string unitWip,
        ref string unitArm, ref string unitBts, ref string unitS, ref string unitVitality)
    {
        if (!seasonGear.TryGetValue(entryIndex, out var gearList)) return;

        foreach (var gear in gearList)
        {
            if (string.IsNullOrWhiteSpace(gear.ItemName)) continue;
            switch (gear.Slot)
            {
                case "Primary":
                case "Secondary":
                case "Sidearm":
                case "Accessories":
                    if (CompanyProfileTextService.IsMeleeWeaponName(gear.ItemName))
                        meleeWeapons = AppendChoices(meleeWeapons, [gear.ItemName]);
                    else
                        rangedWeapons = AppendChoices(rangedWeapons, [gear.ItemName]);
                    break;

                case "Roles":
                    skills = AppendChoices(skills, [gear.ItemName]);
                    break;

                case "Augments":
                    var m = System.Text.RegularExpressions.Regex.Match(
                        gear.ItemName.Trim(), @"^([A-Za-z]+)\s*=\s*(\d+)$");
                    if (m.Success)
                    {
                        var val = m.Groups[2].Value;
                        switch (m.Groups[1].Value.ToUpperInvariant())
                        {
                            case "CC":   unitCc       = val; break;
                            case "BS":   unitBs       = val; break;
                            case "PH":   unitPh       = val; break;
                            case "WIP":  unitWip      = val; break;
                            case "ARM":  unitArm      = val; break;
                            case "BTS":  unitBts      = val; break;
                            case "S":    unitS        = val; break;
                            case "VITA": unitVitality = val; break;
                            case "STR":  unitVitality = val; break;
                        }
                    }
                    else
                    {
                        skills = AppendChoices(skills, [gear.ItemName]);
                    }
                    break;
            }
        }
    }
}
