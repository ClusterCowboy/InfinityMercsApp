using InfinityMercsApp.Domain.Models.Metadata;
using Microsoft.Maui.Controls.Shapes;
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

    public static readonly BindableProperty BurstBonusProperty =
        BindableProperty.Create(
            nameof(BurstBonus),
            typeof(int),
            typeof(WeaponDetailCardView),
            0,
            propertyChanged: (b, _, _) => ((WeaponDetailCardView)b).RebuildContent());

    public static readonly BindableProperty AmmoTypeProperty =
        BindableProperty.Create(
            nameof(AmmoType),
            typeof(string),
            typeof(WeaponDetailCardView),
            null,
            propertyChanged: (b, _, _) => ((WeaponDetailCardView)b).RebuildContent());

    public static readonly BindableProperty CompactProperty =
        BindableProperty.Create(
            nameof(Compact),
            typeof(bool),
            typeof(WeaponDetailCardView),
            false,
            propertyChanged: (b, _, _) => ((WeaponDetailCardView)b).RebuildContent());

    // The label shown in the top-left of the compact card — typically the weapon name or mode name,
    // set by the host so the card can be self-contained (no external header label needed).
    public static readonly BindableProperty DisplayNameProperty =
        BindableProperty.Create(
            nameof(DisplayName),
            typeof(string),
            typeof(WeaponDetailCardView),
            string.Empty,
            propertyChanged: (b, _, _) => ((WeaponDetailCardView)b).RebuildContent());

    /// <summary>
    /// Raised when the card is tapped while in <see cref="Compact"/> mode, signalling that the
    /// host should reveal the full weapon detail (e.g. in a popup).
    /// </summary>
    public event EventHandler? DetailRequested;

    public WeaponDetailCardView()
    {
        InitializeComponent();

        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) =>
        {
            if (Compact) DetailRequested?.Invoke(this, EventArgs.Empty);
        };
        CardBorder.GestureRecognizers.Add(tap);
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

    public int BurstBonus
    {
        get => (int)GetValue(BurstBonusProperty);
        set => SetValue(BurstBonusProperty, value);
    }

    public string? AmmoType
    {
        get => (string?)GetValue(AmmoTypeProperty);
        set => SetValue(AmmoTypeProperty, value);
    }

    public bool Compact
    {
        get => (bool)GetValue(CompactProperty);
        set => SetValue(CompactProperty, value);
    }

    public string DisplayName
    {
        get => (string)GetValue(DisplayNameProperty);
        set => SetValue(DisplayNameProperty, value);
    }

    private void RebuildContent()
    {
        ContentStack.Children.Clear();
        var weapon = Weapon;
        if (weapon is null) return;

        if (Compact)
            BuildCompact(weapon);
        else
            BuildFull(weapon);
    }

    private void BuildFull(Weapon weapon)
    {
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

        // Two-column stats: Burst/Damage/Ammo on the left, Saving/Saving Rolls on the right
        var statsView = BuildStatsView(weapon, DamageReduction, AmmoType, BurstBonus);
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

        AddRangeBar(weapon);
    }

    // Compact glance view: a single tappable row (Burst / Damage / ammo pill / chevron) plus the
    // range bar. Saving, Saving Rolls and Special Rules are deferred to the detail popup.
    // Compact single-row card:
    //   [DisplayName — left/Star]  [B chip] [DAM chip] [ammo pill] [›]  — all at 3 px spacing
    //   range bar below (when present)
    private void BuildCompact(Weapon weapon)
    {
        var row = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star }, // weapon / mode label
                new ColumnDefinition { Width = GridLength.Auto }  // stats + ammo + chevron
            },
            ColumnSpacing = 8,
            VerticalOptions = LayoutOptions.Center
        };

        var nameLabel = new Label
        {
            Text = DisplayName,
            TextColor = Colors.White,
            FontAttributes = FontAttributes.Bold,
            Style = (Style)Application.Current!.Resources["LabelCaption"],
            VerticalTextAlignment = TextAlignment.Center,
            LineBreakMode = LineBreakMode.TailTruncation
        };
        Grid.SetColumn(nameLabel, 0);
        row.Children.Add(nameLabel);

        var rightStack = new HorizontalStackLayout { Spacing = 3, VerticalOptions = LayoutOptions.Center };

        if (!IsDash(weapon.Burst))
        {
            var burst = weapon.Burst!;
            if (BurstBonus > 0 && int.TryParse(burst.Trim(), out var bv))
                burst = (bv + BurstBonus).ToString();
            rightStack.Children.Add(BuildStatChip("B", burst));
        }

        var damageDisplay = ApplyDamageReduction(weapon.Damage, DamageReduction);
        if (!IsDash(damageDisplay))
            rightStack.Children.Add(BuildStatChip("DAM", damageDisplay!));

        if (!string.IsNullOrWhiteSpace(AmmoType))
            rightStack.Children.Add(BuildAmmoPill(AmmoType!));

        rightStack.Children.Add(new Label
        {
            Text = "›",
            TextColor = Color.FromArgb("#6B7280"),
            FontSize = 18,
            VerticalTextAlignment = TextAlignment.Center
        });

        Grid.SetColumn(rightStack, 1);
        row.Children.Add(rightStack);

        ContentStack.Children.Add(row);
        AddRangeBar(weapon);
    }

    private void AddRangeBar(Weapon weapon)
    {
        if (string.IsNullOrWhiteSpace(weapon.DistanceJson)) return;

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

    // Compact pill-button chip: dimmed label prefix + bold white value, dark bordered background.
    private static View BuildStatChip(string label, string value)
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

    // Coloured pill conveying the ammunition / damage type, e.g. "AP+Exp".
    private static View BuildAmmoPill(string ammo)
    {
        var border = new Border
        {
            BackgroundColor = Color.FromArgb("#1F2937"),
            Stroke = Color.FromArgb("#F59E0B"),
            StrokeThickness = 1,
            Padding = new Thickness(6, 1),
            HorizontalOptions = LayoutOptions.Start,
            VerticalOptions = LayoutOptions.Center,
            StrokeShape = new RoundRectangle { CornerRadius = 8 }
        };
        border.Content = new Label
        {
            Text = ammo,
            Style = (Style)Application.Current!.Resources["LabelCaption"],
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#F59E0B"),
            VerticalTextAlignment = TextAlignment.Center
        };
        return border;
    }

    private static View? BuildStatsView(Weapon weapon, int damageReduction = 0, string? ammoType = null, int burstBonus = 0)
    {
        var leftStats = new List<(string Label, string Value)>();
        var rightStats = new List<(string Label, string Value)>();

        if (!IsDash(weapon.Burst))
        {
            var burst = weapon.Burst!;
            if (burstBonus > 0 && int.TryParse(burst.Trim(), out var bv))
                burst = (bv + burstBonus).ToString();
            leftStats.Add(("Burst", burst));
        }
        var damageDisplay = ApplyDamageReduction(weapon.Damage, damageReduction);
        if (!IsDash(damageDisplay)) leftStats.Add(("Damage", damageDisplay!));
        if (!string.IsNullOrWhiteSpace(ammoType)) leftStats.Add(("Ammo", ammoType!));
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
