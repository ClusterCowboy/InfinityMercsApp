using InfinityMercsApp.Domain.Models.Metadata;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace InfinityMercsApp.Views.Controls;

public partial class WeaponDetailCardView : ContentView
{
    public static readonly BindableProperty WeaponProperty =
        BindableProperty.Create(
            nameof(Weapon),
            typeof(Weapon),
            typeof(WeaponDetailCardView),
            null,
            propertyChanged: (b, _, _) => ((WeaponDetailCardView)b).RebuildContent());

    public static readonly BindableProperty ShowUnitsInInchesProperty =
        BindableProperty.Create(
            nameof(ShowUnitsInInches),
            typeof(bool),
            typeof(WeaponDetailCardView),
            true,
            propertyChanged: (b, _, _) => ((WeaponDetailCardView)b).RebuildContent());

    public static readonly BindableProperty RangeBandHeightRequestProperty =
        BindableProperty.Create(
            nameof(RangeBandHeightRequest),
            typeof(double),
            typeof(WeaponDetailCardView),
            88.0,
            propertyChanged: (b, _, _) => ((WeaponDetailCardView)b).RebuildContent());

    public static readonly BindableProperty XVisorActiveProperty =
        BindableProperty.Create(
            nameof(XVisorActive),
            typeof(bool),
            typeof(WeaponDetailCardView),
            false,
            propertyChanged: (b, _, _) => ((WeaponDetailCardView)b).RebuildContent());

    public static readonly BindableProperty DamageReductionProperty =
        BindableProperty.Create(
            nameof(DamageReduction),
            typeof(int),
            typeof(WeaponDetailCardView),
            0,
            propertyChanged: (b, _, _) => ((WeaponDetailCardView)b).RebuildContent());

    public WeaponDetailCardView()
    {
        InitializeComponent();
    }

    public Weapon? Weapon
    {
        get => (Weapon?)GetValue(WeaponProperty);
        set => SetValue(WeaponProperty, value);
    }

    public bool ShowUnitsInInches
    {
        get => (bool)GetValue(ShowUnitsInInchesProperty);
        set => SetValue(ShowUnitsInInchesProperty, value);
    }

    public double RangeBandHeightRequest
    {
        get => (double)GetValue(RangeBandHeightRequestProperty);
        set => SetValue(RangeBandHeightRequestProperty, value);
    }

    public bool XVisorActive
    {
        get => (bool)GetValue(XVisorActiveProperty);
        set => SetValue(XVisorActiveProperty, value);
    }

    public int DamageReduction
    {
        get => (int)GetValue(DamageReductionProperty);
        set => SetValue(DamageReductionProperty, value);
    }

    private void RebuildContent()
    {
        ContentStack.Children.Clear();
        var weapon = Weapon;
        if (weapon is null) return;

        // Firing mode header — shown when a weapon has multiple modes
        if (!string.IsNullOrWhiteSpace(weapon.Mode))
        {
            ContentStack.Children.Add(new Label
            {
                Text = weapon.Mode,
                TextColor = Colors.White,
                FontAttributes = FontAttributes.Bold,
                Style = (Style)Application.Current!.Resources["LabelBody"],
                Margin = new Thickness(0, 0, 0, 4)
            });
        }

        // Two-column stats: Burst/Damage on the left, Saving/Saving Rolls on the right
        var statsView = BuildStatsView(weapon, DamageReduction);
        if (statsView is not null)
            ContentStack.Children.Add(statsView);

        // Special rules — two-column grid, no bullet points
        var properties = ParseProperties(weapon.PropertiesJson);
        if (properties.Count > 0)
        {
            ContentStack.Children.Add(new Label
            {
                Text = "SPECIAL RULES",
                Style = (Style)Application.Current!.Resources["LabelCaption"],
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#6B7280"),
                Margin = new Thickness(0, 6, 0, 2)
            });

            var numRows = (properties.Count + 1) / 2;
            var propGrid = new Grid { ColumnSpacing = 8, RowSpacing = 4 };
            propGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            propGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            for (int r = 0; r < numRows; r++)
                propGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            for (int i = 0; i < properties.Count; i++)
            {
                var prop = properties[i];
                var url = BuildWikiUrl(prop);
                var label = new Label
                {
                    Text = prop,
                    Style = (Style)Application.Current!.Resources["LabelBody"],
                    TextColor = Color.FromArgb("#60A5FA"),
                    TextDecorations = TextDecorations.Underline,
                    LineBreakMode = LineBreakMode.WordWrap
                };
                var tap = new TapGestureRecognizer();
                tap.Tapped += async (_, _) => await OpenLinkAsync(url);
                label.GestureRecognizers.Add(tap);
                Grid.SetColumn(label, i % 2);
                Grid.SetRow(label, i / 2);
                propGrid.Children.Add(label);
            }
            ContentStack.Children.Add(propGrid);
        }

        // Range band bar
        if (!string.IsNullOrWhiteSpace(weapon.DistanceJson))
        {
            ContentStack.Children.Add(new WeaponRangeBandBarView
            {
                DistanceJson = weapon.DistanceJson,
                ShowUnitsInInches = ShowUnitsInInches,
                BarHeightRequest = RangeBandHeightRequest,
                XVisorActive = XVisorActive,
                Margin = new Thickness(0, 6, 0, 0),
                HorizontalOptions = LayoutOptions.Fill
            });
        }
    }

    private static View? BuildStatsView(Weapon weapon, int damageReduction = 0)
    {
        var leftStats = new List<(string Label, string Value)>();
        var rightStats = new List<(string Label, string Value)>();

        if (!IsDash(weapon.Burst)) leftStats.Add(("Burst", weapon.Burst!));
        var damageDisplay = ApplyDamageReduction(weapon.Damage, damageReduction);
        if (!IsDash(damageDisplay)) leftStats.Add(("Damage", damageDisplay!));
        if (!IsDash(weapon.Saving)) rightStats.Add(("Saving", weapon.Saving!));
        if (!IsDash(weapon.SavingNum)) rightStats.Add(("Saving Rolls", weapon.SavingNum!));

        if (leftStats.Count == 0 && rightStats.Count == 0)
            return null;

        var outerGrid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Star }
            },
            ColumnSpacing = 12
        };

        var leftStack = new VerticalStackLayout { Spacing = 2 };
        foreach (var (lbl, val) in leftStats)
            leftStack.Children.Add(BuildStatRow(lbl, val));

        var rightStack = new VerticalStackLayout { Spacing = 2 };
        foreach (var (lbl, val) in rightStats)
            rightStack.Children.Add(BuildStatRow(lbl, val));

        outerGrid.Children.Add(leftStack);
        Grid.SetColumn(rightStack, 1);
        outerGrid.Children.Add(rightStack);

        return outerGrid;
    }

    private static View BuildStatRow(string label, string value)
    {
        var row = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Star }
            }
        };

        row.Children.Add(new Label
        {
            Text = label,
            Style = (Style)Application.Current!.Resources["LabelCaption"],
            TextColor = Color.FromArgb("#9CA3AF"),
            VerticalTextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 6, 0)
        });

        var valueLabel = new Label
        {
            Text = value,
            Style = (Style)Application.Current!.Resources["LabelCaption"],
            FontAttributes = FontAttributes.Bold,
            TextColor = Colors.White,
            VerticalTextAlignment = TextAlignment.Center,
            HorizontalTextAlignment = TextAlignment.End,
            HorizontalOptions = LayoutOptions.Fill
        };

        Grid.SetColumn(valueLabel, 1);
        row.Children.Add(valueLabel);
        return row;
    }

    private static readonly Regex AsteriskBracketPattern = new(@"^\[\*+\]$", RegexOptions.Compiled);

    private static IReadOnlyList<string> ParseProperties(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try
        {
            var all = JsonSerializer.Deserialize<List<string>>(json) ?? [];
            return all.Where(p => !AsteriskBracketPattern.IsMatch(p.Trim())).ToList();
        }
        catch { return []; }
    }

    private static string BuildWikiUrl(string name)
    {
        var baseName = Regex.Match(name, @"^[^(]+").Value.Trim();
        return $"https://infinitythewiki.com/{baseName.Replace(' ', '_')}?version=n4";
    }

    private static string? ApplyDamageReduction(string? damage, int reduction)
    {
        if (reduction <= 0 || IsDash(damage)) return damage;
        if (int.TryParse(damage!.Trim(), out var val))
            return Math.Max(1, val - reduction).ToString();
        return damage;
    }

    private static bool IsDash(string? v) =>
        string.IsNullOrWhiteSpace(v) || v.Trim() == "-";

    private static async Task OpenLinkAsync(string url)
    {
        try { await Launcher.Default.OpenAsync(url); }
        catch (Exception ex) { Console.Error.WriteLine($"Failed to open '{url}': {ex.Message}"); }
    }
}
