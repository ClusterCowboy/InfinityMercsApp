using InfinityMercsApp.ViewModels;
using InfinityMercsApp.Views.Controls;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using Svg.Skia;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows.Input;

namespace InfinityMercsApp.Views;

public partial class PerkTesterPage : ContentPage
{
    private const int MaxIconsPerRow = 3;
    private const float IconSize = 24f;
    private const float IconGap = 20f;
    private const float RightPadding = 24f;
    private readonly ViewerViewModel _viewModel;
    private bool _loaded;
    private double _factionDragStartScrollY;
    private double _unitDragStartScrollY;
    private SKPicture? _regularOrderIconPicture;
    private SKPicture? _irregularOrderIconPicture;
    private SKPicture? _impetuousIconPicture;
    private SKPicture? _tacticalAwarenessIconPicture;
    private SKPicture? _cubeIconPicture;
    private SKPicture? _cube2IconPicture;
    private SKPicture? _hackableIconPicture;
    private SKPicture? _filterIconPicture;
    private SKPicture? _selectedUnitPicture;
    private int _selectedUnitLogoLoadVersion;
    private UnitFilterPopupView? _activeUnitFilterPopup;
    private ViewerProfileItem? _selectedProfile;

    public ObservableCollection<QualifiedPerkItem> QualifiedPerks { get; } = [];
    public ICommand SelectProfileCommand { get; }
    
    public PerkTesterPage(ViewerViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
        SelectProfileCommand = new Command<ViewerProfileItem>(OnProfileSelected);
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        _viewModel.Profiles.CollectionChanged += OnProfilesCollectionChanged;
        var topTap = new TapGestureRecognizer();
        topTap.Tapped += OnTopIconRowTapped;
        TopIconRowCanvas.GestureRecognizers.Add(topTap);
        var bottomTap = new TapGestureRecognizer();
        bottomTap.Tapped += OnBottomIconRowTapped;
        BottomIconRowCanvas.GestureRecognizers.Add(bottomTap);
        _ = LoadHeaderIconsAsync();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_viewModel.IsLoading)
        {
            return;
        }

        if (_loaded && _viewModel.Factions.Count > 0)
        {
            return;
        }

        _loaded = true;
        await _viewModel.LoadFactionsAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        CloseUnitFilterPopup(null);
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
            Console.Error.WriteLine($"PerkTesterPage regular order icon load failed: {ex.Message}");
        }

        try
        {
            await using var irregularStream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/CBIcons/irregular.svg");
            var irregularSvg = new SKSvg();
            _irregularOrderIconPicture = irregularSvg.Load(irregularStream);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"PerkTesterPage irregular order icon load failed: {ex.Message}");
        }

        try
        {
            await using var impetuousStream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/CBIcons/impetuous.svg");
            var impetuousSvg = new SKSvg();
            _impetuousIconPicture = impetuousSvg.Load(impetuousStream);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"PerkTesterPage impetuous icon load failed: {ex.Message}");
        }

        try
        {
            await using var tacticalStream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/CBIcons/tactical.svg");
            var tacticalSvg = new SKSvg();
            _tacticalAwarenessIconPicture = tacticalSvg.Load(tacticalStream);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"PerkTesterPage tactical awareness icon load failed: {ex.Message}");
        }

        try
        {
            await using var cubeStream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/CBIcons/cube.svg");
            var cubeSvg = new SKSvg();
            _cubeIconPicture = cubeSvg.Load(cubeStream);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"PerkTesterPage cube icon load failed: {ex.Message}");
        }

        try
        {
            await using var cube2Stream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/CBIcons/cube2.svg");
            var cube2Svg = new SKSvg();
            _cube2IconPicture = cube2Svg.Load(cube2Stream);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"PerkTesterPage cube 2.0 icon load failed: {ex.Message}");
        }

        try
        {
            await using var hackableStream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/CBIcons/hackable.svg");
            var hackableSvg = new SKSvg();
            _hackableIconPicture = hackableSvg.Load(hackableStream);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"PerkTesterPage hackable icon load failed: {ex.Message}");
        }

        try
        {
            await using var filterStream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/NonCBIcons/noun-filter.svg");
            var filterSvg = new SKSvg();
            _filterIconPicture = filterSvg.Load(filterStream);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"PerkTesterPage filter icon load failed: {ex.Message}");
        }

        TopIconRowCanvas.InvalidateSurface();
        BottomIconRowCanvas.InvalidateSurface();
        UnitSelectionFilterCanvas.InvalidateSurface();
        UnitDisplayConfigurationsView.RegularOrderIconPicture = _regularOrderIconPicture;
        UnitDisplayConfigurationsView.IrregularOrderIconPicture = _irregularOrderIconPicture;
        UnitDisplayConfigurationsView.ImpetuousIconPicture = _impetuousIconPicture;
        UnitDisplayConfigurationsView.TacticalAwarenessIconPicture = _tacticalAwarenessIconPicture;
        UnitDisplayConfigurationsView.CubeIconPicture = _cubeIconPicture;
        UnitDisplayConfigurationsView.Cube2IconPicture = _cube2IconPicture;
        UnitDisplayConfigurationsView.HackableIconPicture = _hackableIconPicture;
        UnitDisplayConfigurationsView.InvalidateHeaderIconsCanvas();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ViewerViewModel.ShowRegularOrderIcon)
            or nameof(ViewerViewModel.ShowIrregularOrderIcon)
            or nameof(ViewerViewModel.ShowImpetuousIcon)
            or nameof(ViewerViewModel.ShowTacticalAwarenessIcon))
        {
            TopIconRowCanvas.InvalidateSurface();
        }

        if (e.PropertyName is nameof(ViewerViewModel.ShowCubeIcon)
            or nameof(ViewerViewModel.ShowCube2Icon)
            or nameof(ViewerViewModel.ShowHackableIcon))
        {
            BottomIconRowCanvas.InvalidateSurface();
        }

        if (e.PropertyName is nameof(ViewerViewModel.ShowRegularOrderIcon)
            or nameof(ViewerViewModel.ShowIrregularOrderIcon)
            or nameof(ViewerViewModel.ShowImpetuousIcon)
            or nameof(ViewerViewModel.ShowTacticalAwarenessIcon)
            or nameof(ViewerViewModel.ShowCubeIcon)
            or nameof(ViewerViewModel.ShowCube2Icon)
            or nameof(ViewerViewModel.ShowHackableIcon))
        {
            UnitDisplayConfigurationsView.InvalidateHeaderIconsCanvas();
        }

        if (e.PropertyName == nameof(ViewerViewModel.SelectedUnit))
        {
            _ = LoadSelectedUnitLogoAsync(_viewModel.SelectedUnit);
            ClearProfileSelectionAndPerks();
        }
    }

    private void OnProfilesCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (_viewModel.Profiles.Count == 0)
        {
            ClearProfileSelectionAndPerks();
            return;
        }

        if (_selectedProfile is not null && _viewModel.Profiles.Contains(_selectedProfile))
        {
            return;
        }

        OnProfileSelected(_viewModel.Profiles.FirstOrDefault());
    }

    private void OnProfileSelected(ViewerProfileItem? profile)
    {
        if (profile is null)
        {
            return;
        }

        foreach (var entry in _viewModel.Profiles)
        {
            entry.IsSelected = ReferenceEquals(entry, profile);
        }

        _selectedProfile = profile;
        BuildQualifiedPerks(profile);
    }

    private void ClearProfileSelectionAndPerks()
    {
        _selectedProfile = null;
        foreach (var profile in _viewModel.Profiles)
        {
            profile.IsSelected = false;
        }

        QualifiedPerks.Clear();
        OnPropertyChanged(nameof(QualifiedPerks));
    }

    private void BuildQualifiedPerks(ViewerProfileItem profile)
    {
        var skills = new List<string>();
        var equipment = new List<string>();
        var weapons = new List<string>();
        var characteristics = new List<string>();

        skills.AddRange(ParseSummaryValues(_viewModel.SpecialSkillsSummary));
        skills.AddRange(ParseList(profile.UniqueSkills));

        equipment.AddRange(ParseSummaryValues(_viewModel.EquipmentSummary));
        equipment.AddRange(ParseList(profile.UniqueEquipment));

        weapons.AddRange(ParseList(profile.RangedWeapons));
        weapons.AddRange(ParseList(profile.MeleeWeapons));

        characteristics.AddRange(ParseList(profile.Characteristics));

        var includeMechaTrack = _viewModel.SelectedUnit?.Subtitle?.Contains("tag", StringComparison.OrdinalIgnoreCase) == true;
        var ownedIds = CompanyPerkOwnershipResolver.ResolveOwnedPerkNodeIds(skills, equipment, weapons, characteristics, includeMechaTrack);

        QualifiedPerks.Clear();
        foreach (var id in ownedIds)
        {
            var perk = CompanyPerkCatalog.FindById(id);
            if (perk is null)
            {
                continue;
            }

            var listId = ParseListId(id);
            var listName = CompanyPerkCatalog.FindPerkListCatalogEntry(listId)?.Name ?? listId;
            ParseTrackTier(id, out var track, out var tier);
            var node = FindPerkNodeById(id);
            QualifiedPerks.Add(new QualifiedPerkItem
            {
                PerkText = string.IsNullOrWhiteSpace(perk.Description) ? perk.Name : perk.Description,
                Meta = $"{listName} | Track {track} Tier {tier} | {id}",
                Granted = node is null ? "Gives: -" : BuildGrantedText(node)
            });
        }

        OnPropertyChanged(nameof(QualifiedPerks));
    }

    private static IEnumerable<string> ParseSummaryValues(string summary)
    {
        if (string.IsNullOrWhiteSpace(summary))
        {
            return [];
        }

        var separatorIndex = summary.IndexOf(':', StringComparison.Ordinal);
        var values = separatorIndex >= 0 ? summary[(separatorIndex + 1)..] : summary;
        return ParseList(values);
    }

    private static IEnumerable<string> ParseList(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        return text
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Where(value => !string.Equals(value, "-", StringComparison.OrdinalIgnoreCase));
    }

    private static string ParseListId(string nodeId)
    {
        var marker = "-track-";
        var index = nodeId.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        return index > 0 ? nodeId[..index] : "unknown";
    }

    private static void ParseTrackTier(string nodeId, out int track, out int tier)
    {
        track = 0;
        tier = 0;
        var match = Regex.Match(nodeId, @"-track-(?<track>\d+)-tier-(?<tier>\d+)", RegexOptions.IgnoreCase);
        if (!match.Success)
        {
            return;
        }

        _ = int.TryParse(match.Groups["track"].Value, out track);
        _ = int.TryParse(match.Groups["tier"].Value, out tier);
    }

    private static string BuildGrantedText(PerkNode node)
    {
        if (node.SkillsEquipmentGained is null || node.SkillsEquipmentGained.Count == 0)
        {
            return "Gives: -";
        }

        var parts = node.SkillsEquipmentGained
            .Select(tuple =>
            {
                var name = tuple.Item1?.Trim();
                var extra = tuple.Item2?.Trim();
                if (string.IsNullOrWhiteSpace(name))
                {
                    return string.Empty;
                }

                return string.IsNullOrWhiteSpace(extra)
                    ? name
                    : $"{name} ({extra})";
            })
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return parts.Count == 0
            ? "Gives: -"
            : $"Gives: {string.Join(", ", parts)}";
    }

    private static PerkNode? FindPerkNodeById(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        foreach (var list in CompanyPerkCatalog.GetPerkNodeLists())
        {
            foreach (var root in list.Roots)
            {
                var match = root.FindById(id);
                if (match is not null)
                {
                    return match;
                }
            }
        }

        return null;
    }

    private async Task LoadSelectedUnitLogoAsync(ViewerUnitItem? unit)
    {
        var loadVersion = ++_selectedUnitLogoLoadVersion;
        SKPicture? loadedPicture = null;

        try
        {
            if (unit is not null)
            {
                Stream? stream = null;
                try
                {
                    if (!string.IsNullOrWhiteSpace(unit.CachedLogoPath) && File.Exists(unit.CachedLogoPath))
                    {
                        stream = File.OpenRead(unit.CachedLogoPath);
                    }
                    else if (!string.IsNullOrWhiteSpace(unit.PackagedLogoPath))
                    {
                        stream = await FileSystem.Current.OpenAppPackageFileAsync(unit.PackagedLogoPath);
                    }

                    if (stream is not null)
                    {
                        await using (stream)
                        {
                            var svg = new SKSvg();
                            loadedPicture = svg.Load(stream);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"PerkTesterPage selected unit logo load failed: {ex.Message}");
                }
            }

            if (loadVersion != _selectedUnitLogoLoadVersion)
            {
                loadedPicture?.Dispose();
                return;
            }

            _selectedUnitPicture?.Dispose();
            _selectedUnitPicture = loadedPicture;
            UnitDisplayConfigurationsView.SelectedUnitPicture = _selectedUnitPicture;
            UnitDisplayConfigurationsView.InvalidateSelectedUnitCanvas();
        }
        catch
        {
            loadedPicture?.Dispose();
            throw;
        }
    }

    private async void OnFactionListPanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        switch (e.StatusType)
        {
            case GestureStatus.Started:
                _factionDragStartScrollY = FactionScrollView.ScrollY;
                break;
            case GestureStatus.Running:
                var targetY = Math.Max(0, _factionDragStartScrollY - e.TotalY);
                await FactionScrollView.ScrollToAsync(0, targetY, false);
                break;
        }
    }

    private async void OnUnitListPanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        switch (e.StatusType)
        {
            case GestureStatus.Started:
                _unitDragStartScrollY = UnitScrollView.ScrollY;
                break;
            case GestureStatus.Running:
                var targetY = Math.Max(0, _unitDragStartScrollY - e.TotalY);
                await UnitScrollView.ScrollToAsync(0, targetY, false);
                break;
        }
    }

    private async void OnUnitSelectionFilterButtonTapped(object? sender, TappedEventArgs e)
    {
        try
        {
            var options = await _viewModel.BuildUnitFilterPopupOptionsAsync();
            var popup = new UnitFilterPopupView(
                options,
                _viewModel.ActiveUnitFilter,
                lieutenantOnlyUnits: _viewModel.LieutenantOnlyUnits,
                teamsView: false);
            var popupHeight = ResolveUnitFilterPopupHeight();
            popup.HeightRequest = popupHeight;
            popup.FilterArmyApplied += OnFilterArmyApplied;
            popup.CloseRequested += OnUnitFilterPopupCloseRequested;
            _activeUnitFilterPopup = popup;
            UnitFilterPopupHost.HeightRequest = popupHeight;
            UnitFilterPopupHost.Content = popup;
            UnitFilterOverlay.IsVisible = true;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"PerkTesterPage filter popup open failed: {ex.Message}");
        }
    }

    private async void OnFilterArmyApplied(object? sender, UnitFilterCriteria criteria)
    {
        CloseUnitFilterPopup(sender as UnitFilterPopupView);
        await _viewModel.ApplyActiveUnitFilterAsync(criteria);
    }

    private void OnUnitFilterPopupCloseRequested(object? sender, EventArgs e)
    {
        CloseUnitFilterPopup(sender as UnitFilterPopupView);
    }

    private void CloseUnitFilterPopup(UnitFilterPopupView? popup)
    {
        var target = popup ?? _activeUnitFilterPopup;
        if (target is not null)
        {
            target.FilterArmyApplied -= OnFilterArmyApplied;
            target.CloseRequested -= OnUnitFilterPopupCloseRequested;
        }

        _activeUnitFilterPopup = null;
        UnitFilterPopupHost.Content = null;
        UnitFilterPopupHost.HeightRequest = -1;
        UnitFilterOverlay.IsVisible = false;
    }

    private double ResolveUnitFilterPopupHeight()
    {
        var pageHeight = Height > 0 ? Height : Window?.Height ?? Application.Current?.Windows.FirstOrDefault()?.Page?.Height ?? 0;
        if (pageHeight <= 0)
        {
            return 800;
        }

        return pageHeight * 0.9;
    }

    private void OnTopIconRowCanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        var entries = BuildTopIconEntries();
        DrawIconRow(canvas, e.Info, entries.Select(x => x.Picture).ToList());
    }

    private void OnBottomIconRowCanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        var entries = BuildBottomIconEntries();
        DrawIconRow(canvas, e.Info, entries.Select(x => x.Picture).ToList());
    }

    private void OnUnitSelectionFilterCanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Transparent);
        if (_filterIconPicture is null)
        {
            return;
        }

        DrawPictureInRect(canvas, _filterIconPicture, new SKRect(0, 0, e.Info.Width, e.Info.Height));
    }

    private void OnProfileTacticalIconCanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Transparent);
        if (_tacticalAwarenessIconPicture is null)
        {
            return;
        }

        DrawPictureInRect(canvas, _tacticalAwarenessIconPicture, new SKRect(0, 0, e.Info.Width, e.Info.Height));
    }

    private List<(SKPicture Picture, string? Url)> BuildTopIconEntries()
    {
        var entries = new List<(SKPicture Picture, string? Url)>(MaxIconsPerRow);
        var orderTypePicture = _viewModel.ShowIrregularOrderIcon ? _irregularOrderIconPicture : _regularOrderIconPicture;
        if (_viewModel.HasOrderTypeIcon && orderTypePicture is not null)
        {
            entries.Add((orderTypePicture, null));
        }

        if (_viewModel.ShowImpetuousIcon && _impetuousIconPicture is not null)
        {
            entries.Add((_impetuousIconPicture, _viewModel.ImpetuousIconUrl));
        }

        if (_viewModel.ShowTacticalAwarenessIcon && _tacticalAwarenessIconPicture is not null)
        {
            entries.Add((_tacticalAwarenessIconPicture, _viewModel.TacticalAwarenessIconUrl));
        }

        return entries;
    }

    private List<(SKPicture Picture, string? Url)> BuildBottomIconEntries()
    {
        var entries = new List<(SKPicture Picture, string? Url)>(MaxIconsPerRow);
        if (_viewModel.ShowCubeIcon && _cubeIconPicture is not null)
        {
            entries.Add((_cubeIconPicture, _viewModel.CubeIconUrl));
        }

        if (_viewModel.ShowCube2Icon && _cube2IconPicture is not null)
        {
            entries.Add((_cube2IconPicture, _viewModel.Cube2IconUrl));
        }

        if (_viewModel.ShowHackableIcon && _hackableIconPicture is not null)
        {
            entries.Add((_hackableIconPicture, _viewModel.HackableIconUrl));
        }

        return entries;
    }

    private async void OnTopIconRowTapped(object? sender, TappedEventArgs args)
    {
        await HandleIconRowTapAsync(TopIconRowCanvas, args, BuildTopIconEntries());
    }

    private async void OnBottomIconRowTapped(object? sender, TappedEventArgs args)
    {
        await HandleIconRowTapAsync(BottomIconRowCanvas, args, BuildBottomIconEntries());
    }

    private static async Task HandleIconRowTapAsync(
        SKCanvasView canvasView,
        TappedEventArgs args,
        IReadOnlyList<(SKPicture Picture, string? Url)> entries)
    {
        if (entries.Count == 0)
        {
            return;
        }

        var point = args.GetPosition(canvasView);
        if (point is null)
        {
            return;
        }

        var slot = GetTappedIconSlot(point.Value.X, canvasView.Width);
        if (!slot.HasValue || slot.Value < 0 || slot.Value >= entries.Count)
        {
            return;
        }

        var url = entries[slot.Value].Url;
        if (string.IsNullOrWhiteSpace(url))
        {
            return;
        }

        try
        {
            await Launcher.Default.OpenAsync(url);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"PerkTesterPage icon link open failed: {ex.Message}");
        }
    }

    private static int? GetTappedIconSlot(double tapX, double controlWidth)
    {
        var rowWidth = (MaxIconsPerRow * IconSize) + ((MaxIconsPerRow - 1) * IconGap);
        var startX = controlWidth - RightPadding - rowWidth;
        if (startX < 0)
        {
            startX = 0;
        }

        for (var i = 0; i < MaxIconsPerRow; i++)
        {
            var iconLeft = startX + (i * (IconSize + IconGap));
            var iconRight = iconLeft + IconSize;
            if (tapX >= iconLeft && tapX <= iconRight)
            {
                return i;
            }
        }

        return null;
    }

    private static void DrawIconRow(SKCanvas canvas, SKImageInfo info, IReadOnlyList<SKPicture> pictures)
    {
        if (pictures.Count == 0)
        {
            return;
        }

        var drawCount = Math.Min(MaxIconsPerRow, pictures.Count);
        var rowWidth = (MaxIconsPerRow * IconSize) + ((MaxIconsPerRow - 1) * IconGap);
        var startX = info.Width - RightPadding - rowWidth;
        if (startX < 0)
        {
            startX = 0;
        }

        for (var i = 0; i < drawCount; i++)
        {
            var x = startX + (i * (IconSize + IconGap));
            var y = (info.Height - IconSize) / 2f;
            var destination = new SKRect(x, y, x + IconSize, y + IconSize);
            DrawPictureInRect(canvas, pictures[i], destination);
        }
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

    public sealed class QualifiedPerkItem
    {
        public string PerkText { get; init; } = string.Empty;
        public string Meta { get; init; } = string.Empty;
        public string Granted { get; init; } = "Gives: -";
    }
}
