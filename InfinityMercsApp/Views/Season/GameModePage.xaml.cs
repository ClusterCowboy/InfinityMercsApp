using System.Text.Json;
using System.Text.RegularExpressions;
using InfinityMercsApp.Domain.Models.Perks;
using InfinityMercsApp.Infrastructure.Providers;
using InfinityMercsApp.Services;
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
    private bool _loadAttempted;
    private bool _showUnitsInInches = true;

    private SKPicture? _ltPicture;
    private SKPicture? _impPicture;
    private SKPicture? _irrPicture;
    private SKPicture? _regPicture;
    private bool _iconsLoaded;

    // Each entry is (expandable content view, arrow label) for exclusive-open behaviour.
    private readonly List<(View ContentArea, Label Arrow)> _accordionRows = [];

    // null = show all (no filter), empty set = none checked, non-empty = filtered list
    private HashSet<int>? _deployedEntryIndices;

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
                    VitalityHeader = InferVitalityHeader(entry.UnitTypeCode),
                    Equipment     = SeasonDisplayUnitFormatter.ConvertExplicitDistances(equipment,     _showUnitsInInches),
                    Skills        = SeasonDisplayUnitFormatter.ConvertExplicitDistances(skills,        _showUnitsInInches),
                    RangedWeapons = SeasonDisplayUnitFormatter.ConvertExplicitDistances(rangedWeapons, _showUnitsInInches),
                    MeleeWeapons  = SeasonDisplayUnitFormatter.ConvertExplicitDistances(meleeWeapons,  _showUnitsInInches)
                };

                AccordionStack.Children.Add(BuildAccordionRow(unit));
            }

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

        var contentArea = BuildUnitDetailContent(unit);
        contentArea.IsVisible = false;

        _accordionRows.Add((contentArea, arrow));

        var headerGrid = new Grid { MinimumHeightRequest = 48, Padding = new Thickness(16, 8) };
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(24) });
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
        headerGrid.Children.Add(arrow);
        Grid.SetColumn(nameLabel, 1);
        headerGrid.Children.Add(nameLabel);

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

    private View BuildUnitDetailContent(DeploymentUnitItem unit)
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

    private async void OnEndGameClicked(object sender, EventArgs e)
    {
        var encodedPath = Uri.EscapeDataString(_companyFilePath);
        await Shell.Current.GoToAsync($"{nameof(ExperiencePage)}?companyFilePath={encodedPath}");
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
