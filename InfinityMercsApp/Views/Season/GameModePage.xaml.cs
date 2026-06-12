using System.Text.Json;
using System.Text.RegularExpressions;
using InfinityMercsApp.Domain.Models.Perks;
using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Infrastructure.Providers;
using InfinityMercsApp.Services;
using InfinityMercsApp.Services.Season;
using InfinityMercsApp.ViewModels;
using InfinityMercsApp.Views.Common;
using InfinityMercsApp.Views.Controls;
using InfinityMercsApp.Views.StandardCompany;
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
                    UnitCc       = entry.CurrentCc,
                    UnitBs       = entry.CurrentBs,
                    UnitPh       = entry.CurrentPh,
                    UnitWip      = entry.CurrentWip,
                    UnitArm      = entry.CurrentArm,
                    UnitBts      = entry.CurrentBts,
                    UnitVitality = entry.CurrentVitaOrStr,
                    UnitS        = entry.CurrentS,
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

        AddDetailSection(container, unit.Equipment, Color.FromArgb("#06B6D4"));
        AddDetailSection(container, unit.Skills,    Color.FromArgb("#F59E0B"));

        var hasXVisor = ContainsXVisor(unit.Skills) || ContainsXVisor(unit.Equipment);
        var damageReduction = GetBsAttackDamageReduction(unit.Skills);

        AppendWeaponSection(container, unit.RangedWeapons, hasXVisor, damageReduction);
        AppendWeaponSection(container, unit.MeleeWeapons,  hasXVisor, damageReduction);
        AppendStandardActionCards(container, hasXVisor);

        return container;
    }

    private static void AddDetailSection(VerticalStackLayout container, string? text, Color color)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Trim() == "-") return;
        container.Children.Add(new Label
        {
            Text = text,
            TextColor = color,
            FontSize = 12,
            LineBreakMode = LineBreakMode.WordWrap
        });
    }

    private void AppendWeaponSection(VerticalStackLayout container, string? weaponsText, bool hasXVisor = false, int damageReduction = 0)
    {
        var lines = SplitLines(weaponsText);
        foreach (var line in lines)
        {
            var baseName = Regex.Match(line, @"^[^(]+").Value.Trim();
            var matches = _metadataProvider?.SearchWeaponsByName(baseName) ?? [];
            var exact = matches
                .Where(w => string.Equals(w.Name, baseName, StringComparison.OrdinalIgnoreCase))
                .ToList();
            var weapons = exact.Count > 0 ? exact : [.. matches];

            if (weapons.Count > 0)
            {
                // Show the full name as written (may include extras like "(PS=6)")
                container.Children.Add(new Label
                {
                    Text = line,
                    TextColor = Colors.White,
                    FontSize = 15,
                    FontAttributes = FontAttributes.Bold,
                    HorizontalTextAlignment = TextAlignment.Center,
                    Margin = new Thickness(0, 4, 0, 2)
                });
                foreach (var weapon in weapons)
                {
                    container.Children.Add(new WeaponDetailCardView
                    {
                        Weapon = weapon,
                        ShowUnitsInInches = _showUnitsInInches,
                        RangeBandHeightRequest = 44,
                        XVisorActive = hasXVisor,
                        DamageReduction = damageReduction,
                        Margin = new Thickness(0, 0, 0, 4)
                    });
                }
            }
            else
            {
                // No metadata found — fall back to a plain label
                container.Children.Add(new Label
                {
                    Text = line,
                    TextColor = Color.FromArgb("#9CA3AF"),
                    FontSize = 12,
                    LineBreakMode = LineBreakMode.WordWrap
                });
            }
        }
    }

    private void AppendStandardActionCards(VerticalStackLayout container, bool hasXVisor)
    {
        AppendNamedWeaponCard(container, "Discover",             "Discover",                      hasXVisor);
        AppendNamedWeaponCard(container, "Suppressive Fire Mode","Suppressive Fire Mode Weapon",   hasXVisor);
    }

    private void AppendNamedWeaponCard(VerticalStackLayout container, string displayName, string searchName, bool hasXVisor)
    {
        var matches = _metadataProvider?.SearchWeaponsByName(searchName) ?? [];
        var exact = matches
            .Where(w => string.Equals(w.Name, searchName, StringComparison.OrdinalIgnoreCase))
            .ToList();
        var weapons = exact.Count > 0 ? exact : [.. matches];
        if (weapons.Count == 0) return;

        container.Children.Add(new Label
        {
            Text = displayName,
            TextColor = Colors.White,
            FontSize = 15,
            FontAttributes = FontAttributes.Bold,
            HorizontalTextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 4, 0, 2)
        });
        foreach (var weapon in weapons)
        {
            container.Children.Add(new WeaponDetailCardView
            {
                Weapon = weapon,
                ShowUnitsInInches = _showUnitsInInches,
                RangeBandHeightRequest = 44,
                XVisorActive = hasXVisor,
                Margin = new Thickness(0, 0, 0, 4)
            });
        }
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

    private static int GetBsAttackDamageReduction(string? skills)
    {
        if (string.IsNullOrWhiteSpace(skills)) return 0;
        foreach (var line in skills.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            var m = Regex.Match(line, @"BS\s+Attack\s*\(SR-(\d+)\)", RegexOptions.IgnoreCase);
            if (m.Success && int.TryParse(m.Groups[1].Value, out var n))
                return n;
        }
        return 0;
    }
}
