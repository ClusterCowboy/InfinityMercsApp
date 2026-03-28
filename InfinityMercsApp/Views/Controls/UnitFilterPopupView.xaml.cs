using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using Svg.Skia;

namespace InfinityMercsApp.Views.Controls;

public sealed class UnitFilterPopupOptions
{
    public List<string> Classification { get; init; } = [];
    public List<string> Characteristics { get; init; } = [];
    public List<string> Skills { get; init; } = [];
    public List<string> Equipment { get; init; } = [];
    public List<string> Weapons { get; init; } = [];
    public List<string> Ammo { get; init; } = [];
    public int MinPoints { get; init; }
    public int MaxPoints { get; init; } = 200;
}

public sealed class UnitFilterCriteria
{
    public IReadOnlyList<UnitFilterTerm> Terms { get; init; } = [];
    public int? MinPoints { get; init; }
    public int? MaxPoints { get; init; }
    public bool LieutenantOnlyUnits { get; init; }
    public bool TeamsView { get; init; }

    public static UnitFilterCriteria None { get; } = new();

    public bool IsActive => Terms.Count > 0
        || (MinPoints.HasValue && MinPoints.Value != 0)
        || (MaxPoints.HasValue && MaxPoints.Value != 200)
        || LieutenantOnlyUnits;

    public UnitFilterQuery ToQuery()
    {
        return UnitFilterQuery.FromCriteria(this);
    }
}

public enum UnitFilterField
{
    Classification,
    Characteristics,
    Skills,
    Equipment,
    Weapons,
    Ammo
}

public enum UnitFilterMatchMode
{
    Any,
    All
}

public sealed record UnitFilterTerm(
    UnitFilterField Field,
    IReadOnlyList<string> Values,
    UnitFilterMatchMode MatchMode = UnitFilterMatchMode.Any);

public sealed class UnitFilterQuery
{
    public IReadOnlyList<UnitFilterTerm> Terms { get; init; } = [];
    public int? MinPoints { get; init; }
    public int? MaxPoints { get; init; }
    public bool LieutenantOnlyUnits { get; init; }
    public bool TeamsView { get; init; }

    public static UnitFilterQuery None { get; } = new();

    public static UnitFilterQuery FromCriteria(UnitFilterCriteria? criteria)
    {
        if (criteria is null)
        {
            return None;
        }

        var normalizedTerms = criteria.Terms
            .Where(term => term.Values.Count > 0)
            .Select(term => new UnitFilterTerm(
                term.Field,
                term.Values
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .Select(value => value.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
                    .ToList(),
                term.MatchMode))
            .Where(term => term.Values.Count > 0)
            .OrderBy(term => term.Field)
            .ToList();

        return new UnitFilterQuery
        {
            Terms = normalizedTerms,
            MinPoints = criteria.MinPoints,
            MaxPoints = criteria.MaxPoints,
            LieutenantOnlyUnits = criteria.LieutenantOnlyUnits,
            TeamsView = criteria.TeamsView
        };
    }

    public UnitFilterTerm? GetTerm(UnitFilterField field)
    {
        return Terms.FirstOrDefault(x => x.Field == field);
    }
}

public sealed class SelectableFilterOption : INotifyPropertyChanged
{
    private bool _isSelected;

    public SelectableFilterOption(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value)
            {
                return;
            }

            _isSelected = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}

public sealed class FilterCriterionItem : INotifyPropertyChanged
{
    private string _summary = "Any";

    public FilterCriterionItem(UnitFilterField field, string title)
    {
        Field = field;
        Title = title;
    }

    public UnitFilterField Field { get; }

    public string Title { get; }

    public string Summary
    {
        get => _summary;
        set
        {
            if (string.Equals(_summary, value, StringComparison.Ordinal))
            {
                return;
            }

            _summary = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Summary)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}

public partial class UnitFilterPopupView : ContentView
{
    private const string AnyMode = "Any";
    private const string AllMode = "All";

    private readonly Dictionary<UnitFilterField, List<string>> _availableValues;
    private readonly Dictionary<UnitFilterField, UnitFilterTerm> _committedTerms = [];
    private FilterCriterionItem? _selectedCriterion;
    private string _selectedActiveMatchMode = AnyMode;
    private SKPicture? _applyCriterionIconPicture;
    private SKPicture? _clearCriterionIconPicture;

    public event EventHandler<UnitFilterCriteria>? FilterArmyApplied;
    public event EventHandler? CloseRequested;

    public ObservableCollection<FilterCriterionItem> CriteriaItems { get; }
    public ObservableCollection<SelectableFilterOption> ActiveCriterionOptions { get; } = [];
    public ObservableCollection<string> PointsOptions { get; }
    public ObservableCollection<string> MatchModeOptions { get; } = [AnyMode, AllMode];

    public FilterCriterionItem? SelectedCriterion
    {
        get => _selectedCriterion;
        set
        {
            if (_selectedCriterion == value)
            {
                return;
            }

            // Persist current editor state when switching criteria.
            if (_selectedCriterion is not null)
            {
                CommitCriterionFromEditor(_selectedCriterion);
            }

            _selectedCriterion = value;
            OnPropertyChanged(nameof(SelectedCriterion));
            OnPropertyChanged(nameof(SelectedCriterionTitle));
            LoadSelectedCriterionEditor();
        }
    }

    public string SelectedCriterionTitle => SelectedCriterion?.Title ?? "Select a criterion";

    public string SelectedActiveMatchMode
    {
        get => _selectedActiveMatchMode;
        set
        {
            if (string.Equals(_selectedActiveMatchMode, value, StringComparison.Ordinal))
            {
                return;
            }

            _selectedActiveMatchMode = value;
            OnPropertyChanged(nameof(SelectedActiveMatchMode));
        }
    }

    public string? SelectedMinPoints { get; set; }
    public string? SelectedMaxPoints { get; set; }
    public bool SelectedLieutenantOnlyUnits { get; set; }
    public bool LieutenantOnlyUnitsEnabled { get; set; } = true;
    public bool SelectedTeamsView { get; set; }
    public bool TeamsViewEnabled { get; set; }

    public UnitFilterPopupView()
        : this(new UnitFilterPopupOptions(), UnitFilterCriteria.None, lieutenantOnlyUnits: false, teamsView: false)
    {
    }

    public UnitFilterPopupView(
        UnitFilterPopupOptions options,
        UnitFilterCriteria? existingCriteria = null,
        bool lieutenantOnlyUnits = false,
        bool teamsView = false,
        bool teamsViewEnabled = true,
        bool lieutenantOnlyUnitsEnabled = true)
    {
        InitializeComponent();

        _availableValues = new Dictionary<UnitFilterField, List<string>>
        {
            [UnitFilterField.Classification] = NormalizeValues(options.Classification),
            [UnitFilterField.Characteristics] = NormalizeValues(options.Characteristics),
            [UnitFilterField.Skills] = NormalizeValues(options.Skills),
            [UnitFilterField.Equipment] = NormalizeValues(options.Equipment),
            [UnitFilterField.Weapons] = NormalizeValues(options.Weapons),
            [UnitFilterField.Ammo] = NormalizeValues(options.Ammo)
        };

        CriteriaItems = new ObservableCollection<FilterCriterionItem>
        {
            new(UnitFilterField.Classification, "Classification"),
            new(UnitFilterField.Characteristics, "Characteristics"),
            new(UnitFilterField.Skills, "Skills"),
            new(UnitFilterField.Equipment, "Equipment"),
            new(UnitFilterField.Weapons, "Weapons"),
            new(UnitFilterField.Ammo, "Ammo")
        };

        var minPoints = Math.Max(0, options.MinPoints);
        var maxPoints = Math.Max(minPoints, options.MaxPoints);
        PointsOptions = BuildPointsOptions(minPoints, maxPoints);
        LieutenantOnlyUnitsEnabled = lieutenantOnlyUnitsEnabled;
        TeamsViewEnabled = teamsViewEnabled;
        BindingContext = this;
        ApplySelections(existingCriteria ?? UnitFilterCriteria.None, minPoints, maxPoints, lieutenantOnlyUnits, teamsView);
        _ = LoadActionIconsAsync();
    }

    private static List<string> NormalizeValues(IEnumerable<string> values)
    {
        return values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static ObservableCollection<string> BuildPointsOptions(int minValue, int maxValue)
    {
        var values = new ObservableCollection<string>();
        for (var i = minValue; i <= maxValue; i++)
        {
            values.Add(i.ToString());
        }

        return values;
    }

    private void ResetSelections(int minPoints, int maxPoints)
    {
        _committedTerms.Clear();
        UpdateAllCriterionSummaries();

        SelectedActiveMatchMode = AnyMode;
        SelectedMinPoints = minPoints.ToString();
        SelectedMaxPoints = maxPoints.ToString();
        SelectedLieutenantOnlyUnits = false;
        SelectedTeamsView = false;
        OnPropertyChanged(nameof(SelectedMinPoints));
        OnPropertyChanged(nameof(SelectedMaxPoints));
        OnPropertyChanged(nameof(SelectedLieutenantOnlyUnits));
        OnPropertyChanged(nameof(SelectedTeamsView));

        if (CriteriaItems.Count == 0)
        {
            ActiveCriterionOptions.Clear();
            return;
        }

        if (SelectedCriterion is null)
        {
            SelectedCriterion = CriteriaItems[0];
        }
        else
        {
            LoadSelectedCriterionEditor();
        }
    }

    private void ApplySelections(
        UnitFilterCriteria criteria,
        int minPoints,
        int maxPoints,
        bool lieutenantOnlyUnits,
        bool teamsView)
    {
        ResetSelections(minPoints, maxPoints);

        var query = criteria.ToQuery();
        foreach (var term in query.Terms)
        {
            _committedTerms[term.Field] = term;
        }

        UpdateAllCriterionSummaries();
        SelectedMinPoints = ResolvePointsSelection(minPoints, maxPoints, criteria.MinPoints, minPoints);
        SelectedMaxPoints = ResolvePointsSelection(minPoints, maxPoints, criteria.MaxPoints, maxPoints);
        SelectedLieutenantOnlyUnits = lieutenantOnlyUnits;
        SelectedTeamsView = teamsView;
        OnPropertyChanged(nameof(SelectedMinPoints));
        OnPropertyChanged(nameof(SelectedMaxPoints));
        OnPropertyChanged(nameof(SelectedLieutenantOnlyUnits));
        OnPropertyChanged(nameof(SelectedTeamsView));

        if (SelectedCriterion is null && CriteriaItems.Count > 0)
        {
            SelectedCriterion = CriteriaItems[0];
            return;
        }

        LoadSelectedCriterionEditor();
    }

    private void LoadSelectedCriterionEditor()
    {
        ActiveCriterionOptions.Clear();
        if (SelectedCriterion is null)
        {
            SelectedActiveMatchMode = AnyMode;
            return;
        }

        var field = SelectedCriterion.Field;
        _committedTerms.TryGetValue(field, out var term);
        var selectedValues = term?.Values.ToHashSet(StringComparer.OrdinalIgnoreCase)
                             ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var value in _availableValues[field])
        {
            ActiveCriterionOptions.Add(new SelectableFilterOption(value)
            {
                IsSelected = selectedValues.Contains(value)
            });
        }

        SelectedActiveMatchMode = term?.MatchMode == UnitFilterMatchMode.All ? AllMode : AnyMode;
    }

    private void UpdateAllCriterionSummaries()
    {
        foreach (var item in CriteriaItems)
        {
            item.Summary = BuildSummary(item.Field);
        }
    }

    private string BuildSummary(UnitFilterField field)
    {
        if (!_committedTerms.TryGetValue(field, out var term) || term.Values.Count == 0)
        {
            return "Any";
        }

        var prefix = term.MatchMode == UnitFilterMatchMode.All ? "All" : "Any";
        var values = string.Join(", ", term.Values.OrderBy(value => value, StringComparer.OrdinalIgnoreCase));
        return $"{prefix}: {values}";
    }

    private static string ResolvePointsSelection(int minPoints, int maxPoints, int? value, int fallback)
    {
        var normalized = value ?? fallback;
        normalized = Math.Min(Math.Max(normalized, minPoints), maxPoints);
        return normalized.ToString();
    }

    private static UnitFilterMatchMode ParseMatchMode(string? mode)
    {
        return string.Equals(mode, AllMode, StringComparison.OrdinalIgnoreCase)
            ? UnitFilterMatchMode.All
            : UnitFilterMatchMode.Any;
    }

    private async Task LoadActionIconsAsync()
    {
        try
        {
            await using var checkStream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/NonCBIcons/noun-check-3612574.svg");
            var checkSvg = new SKSvg();
            _applyCriterionIconPicture = checkSvg.Load(checkStream);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"UnitFilterPopupView check icon load failed: {ex.Message}");
        }

        try
        {
            await using var xStream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/NonCBIcons/noun-x-1890844.svg");
            var xSvg = new SKSvg();
            _clearCriterionIconPicture = xSvg.Load(xStream);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"UnitFilterPopupView x icon load failed: {ex.Message}");
        }

        ApplyCriterionIconCanvas.InvalidateSurface();
        ClearCriterionIconCanvas.InvalidateSurface();
    }

    private static void DrawActionIcon(SKCanvas canvas, SKImageInfo info, SKPicture? picture)
    {
        canvas.Clear(SKColors.Transparent);
        if (picture is null)
        {
            return;
        }

        var bounds = picture.CullRect;
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        var destination = new SKRect(0, 0, info.Width, info.Height);
        var scaleX = destination.Width / bounds.Width;
        var scaleY = destination.Height / bounds.Height;

        canvas.Save();
        canvas.Translate(destination.Left, destination.Top);
        canvas.Scale(scaleX, scaleY);
        canvas.Translate(-bounds.Left, -bounds.Top);
        canvas.DrawPicture(picture);
        canvas.Restore();
    }

    private void OnApplyCriterionIconCanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        DrawActionIcon(e.Surface.Canvas, e.Info, _applyCriterionIconPicture);
    }

    private void OnClearCriterionIconCanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        DrawActionIcon(e.Surface.Canvas, e.Info, _clearCriterionIconPicture);
    }

    private void OnApplyCriterionTapped(object? sender, TappedEventArgs e)
    {
        OnApplyCriterionClicked(sender, EventArgs.Empty);
    }

    private void OnClearCriterionTapped(object? sender, TappedEventArgs e)
    {
        OnClearCriterionClicked(sender, EventArgs.Empty);
    }

    private void OnApplyCriterionClicked(object? sender, EventArgs e)
    {
        if (SelectedCriterion is null)
        {
            return;
        }

        CommitCriterionFromEditor(SelectedCriterion);
    }

    private void OnClearSelectionsClicked(object? sender, EventArgs e)
    {
        _committedTerms.Clear();
        UpdateAllCriterionSummaries();
        LoadSelectedCriterionEditor();
    }

    private void OnClearCriterionClicked(object? sender, EventArgs e)
    {
        if (SelectedCriterion is null)
        {
            return;
        }

        // Discard in-editor changes and restore previously committed values.
        LoadSelectedCriterionEditor();
    }

    private void CommitCriterionFromEditor(FilterCriterionItem criterion)
    {
        var field = criterion.Field;
        var values = ActiveCriterionOptions
            .Where(option => option.IsSelected)
            .Select(option => option.Value)
            .ToList();

        if (values.Count == 0)
        {
            _committedTerms.Remove(field);
        }
        else
        {
            _committedTerms[field] = new UnitFilterTerm(
                field,
                values,
                ParseMatchMode(SelectedActiveMatchMode));
        }

        criterion.Summary = BuildSummary(field);
    }

    private void OnBackClicked(object? sender, EventArgs e)
    {
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OnResetClicked(object? sender, EventArgs e)
    {
        var parsedMin = int.TryParse(PointsOptions.FirstOrDefault(), out var minValue) ? minValue : 0;
        var parsedMax = int.TryParse(PointsOptions.LastOrDefault(), out var maxValue) ? maxValue : parsedMin;
        ResetSelections(parsedMin, parsedMax);
    }

    private void OnFilterArmyClicked(object? sender, EventArgs e)
    {
        if (SelectedCriterion is not null)
        {
            CommitCriterionFromEditor(SelectedCriterion);
        }

        var minPoints = int.TryParse(SelectedMinPoints, out var parsedMin) ? parsedMin : 0;
        var maxPoints = int.TryParse(SelectedMaxPoints, out var parsedMax) ? parsedMax : minPoints;
        if (minPoints > maxPoints)
        {
            (minPoints, maxPoints) = (maxPoints, minPoints);
        }

        var terms = _committedTerms.Values
            .OrderBy(term => term.Field)
            .ToList();

        var criteria = new UnitFilterCriteria
        {
            Terms = terms,
            MinPoints = minPoints,
            MaxPoints = maxPoints,
            LieutenantOnlyUnits = SelectedLieutenantOnlyUnits,
            TeamsView = SelectedTeamsView
        };

        FilterArmyApplied?.Invoke(this, criteria);
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OnPopupSizeChanged(object? sender, EventArgs e)
    {
        var hostHeight = Height > 0
            ? Height
            : Window?.Height ?? Application.Current?.Windows.FirstOrDefault()?.Page?.Height ?? 0;
        if (hostHeight <= 0)
        {
            return;
        }

        PopupContainer.HeightRequest = hostHeight * 0.9;
    }
}
