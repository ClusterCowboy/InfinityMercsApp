using System.Collections.ObjectModel;

namespace InfinityMercsApp.Views.StandardCompany;

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
    public string? Classification { get; init; }
    public string? Characteristics { get; init; }
    public string? Skills { get; init; }
    public string? Equipment { get; init; }
    public string? Weapons { get; init; }
    public string? Ammo { get; init; }
    public int? MinPoints { get; init; }
    public int? MaxPoints { get; init; }
    public bool LieutenantOnlyUnits { get; init; }
    public bool TeamsView { get; init; }

    public static UnitFilterCriteria None { get; } = new();
}

public partial class UnitFilterPopupPage : ContentView
{
    public event EventHandler<UnitFilterCriteria>? FilterArmyApplied;
    public event EventHandler? CloseRequested;

    public ObservableCollection<string> ClassificationOptions { get; }
    public ObservableCollection<string> CharacteristicsOptions { get; }
    public ObservableCollection<string> SkillsOptions { get; }
    public ObservableCollection<string> EquipmentOptions { get; }
    public ObservableCollection<string> WeaponsOptions { get; }
    public ObservableCollection<string> AmmoOptions { get; }
    public ObservableCollection<string> PointsOptions { get; }

    public string? SelectedClassification { get; set; }
    public string? SelectedCharacteristics { get; set; }
    public string? SelectedSkills { get; set; }
    public string? SelectedEquipment { get; set; }
    public string? SelectedWeapons { get; set; }
    public string? SelectedAmmo { get; set; }
    public string? SelectedMinPoints { get; set; }
    public string? SelectedMaxPoints { get; set; }
    public bool SelectedLieutenantOnlyUnits { get; set; }
    public bool SelectedTeamsView { get; set; }

    public UnitFilterPopupPage()
        : this(new UnitFilterPopupOptions(), UnitFilterCriteria.None, lieutenantOnlyUnits: false, teamsView: false)
    {
    }

    public UnitFilterPopupPage(
        UnitFilterPopupOptions options,
        UnitFilterCriteria? existingCriteria = null,
        bool lieutenantOnlyUnits = false,
        bool teamsView = false)
    {
        InitializeComponent();
        ClassificationOptions = BuildOptionCollection(options.Classification);
        CharacteristicsOptions = BuildOptionCollection(options.Characteristics);
        SkillsOptions = BuildOptionCollection(options.Skills);
        EquipmentOptions = BuildOptionCollection(options.Equipment);
        WeaponsOptions = BuildOptionCollection(options.Weapons);
        AmmoOptions = BuildOptionCollection(options.Ammo);
        var minPoints = Math.Max(0, options.MinPoints);
        var maxPoints = Math.Max(minPoints, options.MaxPoints);
        PointsOptions = BuildPointsOptions(minPoints, maxPoints);
        BindingContext = this;
        ApplySelections(existingCriteria ?? UnitFilterCriteria.None, minPoints, maxPoints, lieutenantOnlyUnits, teamsView);
    }

    private static ObservableCollection<string> BuildOptionCollection(IEnumerable<string> values)
    {
        var output = new ObservableCollection<string> { "Any" };
        foreach (var value in values
                     .Where(x => !string.IsNullOrWhiteSpace(x))
                     .Distinct(StringComparer.OrdinalIgnoreCase)
                     .OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
        {
            output.Add(value.Trim());
        }

        return output;
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
        SelectedClassification = ClassificationOptions[0];
        SelectedCharacteristics = CharacteristicsOptions[0];
        SelectedSkills = SkillsOptions[0];
        SelectedEquipment = EquipmentOptions[0];
        SelectedWeapons = WeaponsOptions[0];
        SelectedAmmo = AmmoOptions[0];
        SelectedMinPoints = minPoints.ToString();
        SelectedMaxPoints = maxPoints.ToString();
        SelectedLieutenantOnlyUnits = false;
        SelectedTeamsView = false;
        OnPropertyChanged(nameof(SelectedClassification));
        OnPropertyChanged(nameof(SelectedCharacteristics));
        OnPropertyChanged(nameof(SelectedSkills));
        OnPropertyChanged(nameof(SelectedEquipment));
        OnPropertyChanged(nameof(SelectedWeapons));
        OnPropertyChanged(nameof(SelectedAmmo));
        OnPropertyChanged(nameof(SelectedMinPoints));
        OnPropertyChanged(nameof(SelectedMaxPoints));
        OnPropertyChanged(nameof(SelectedLieutenantOnlyUnits));
        OnPropertyChanged(nameof(SelectedTeamsView));
    }

    private void ApplySelections(
        UnitFilterCriteria criteria,
        int minPoints,
        int maxPoints,
        bool lieutenantOnlyUnits,
        bool teamsView)
    {
        if (criteria is null)
        {
            ResetSelections(minPoints, maxPoints);
            return;
        }

        SelectedClassification = ResolveSelection(ClassificationOptions, criteria.Classification);
        SelectedCharacteristics = ResolveSelection(CharacteristicsOptions, criteria.Characteristics);
        SelectedSkills = ResolveSelection(SkillsOptions, criteria.Skills);
        SelectedEquipment = ResolveSelection(EquipmentOptions, criteria.Equipment);
        SelectedWeapons = ResolveSelection(WeaponsOptions, criteria.Weapons);
        SelectedAmmo = ResolveSelection(AmmoOptions, criteria.Ammo);
        SelectedMinPoints = ResolvePointsSelection(minPoints, maxPoints, criteria.MinPoints, minPoints);
        SelectedMaxPoints = ResolvePointsSelection(minPoints, maxPoints, criteria.MaxPoints, maxPoints);
        SelectedLieutenantOnlyUnits = lieutenantOnlyUnits;
        SelectedTeamsView = teamsView;
        OnPropertyChanged(nameof(SelectedClassification));
        OnPropertyChanged(nameof(SelectedCharacteristics));
        OnPropertyChanged(nameof(SelectedSkills));
        OnPropertyChanged(nameof(SelectedEquipment));
        OnPropertyChanged(nameof(SelectedWeapons));
        OnPropertyChanged(nameof(SelectedAmmo));
        OnPropertyChanged(nameof(SelectedMinPoints));
        OnPropertyChanged(nameof(SelectedMaxPoints));
        OnPropertyChanged(nameof(SelectedLieutenantOnlyUnits));
        OnPropertyChanged(nameof(SelectedTeamsView));
    }

    private static string ResolveSelection(ObservableCollection<string> options, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return options[0];
        }

        return options.FirstOrDefault(x => string.Equals(x, value, StringComparison.OrdinalIgnoreCase))
               ?? options[0];
    }

    private static string ResolvePointsSelection(int minPoints, int maxPoints, int? value, int fallback)
    {
        var normalized = value ?? fallback;
        normalized = Math.Min(Math.Max(normalized, minPoints), maxPoints);
        return normalized.ToString();
    }

    private static string? NormalizeChoice(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) ||
            string.Equals(value, "Any", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return value.Trim();
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
        var minPoints = int.TryParse(SelectedMinPoints, out var parsedMin) ? parsedMin : 0;
        var maxPoints = int.TryParse(SelectedMaxPoints, out var parsedMax) ? parsedMax : minPoints;
        if (minPoints > maxPoints)
        {
            (minPoints, maxPoints) = (maxPoints, minPoints);
        }

        var criteria = new UnitFilterCriteria
        {
            Classification = NormalizeChoice(SelectedClassification),
            Characteristics = NormalizeChoice(SelectedCharacteristics),
            Skills = NormalizeChoice(SelectedSkills),
            Equipment = NormalizeChoice(SelectedEquipment),
            Weapons = NormalizeChoice(SelectedWeapons),
            Ammo = NormalizeChoice(SelectedAmmo),
            MinPoints = minPoints,
            MaxPoints = maxPoints,
            LieutenantOnlyUnits = SelectedLieutenantOnlyUnits,
            TeamsView = SelectedTeamsView
        };
        FilterArmyApplied?.Invoke(this, criteria);
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }
}
