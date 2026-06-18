using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows.Input;
using InfinityMercsApp.Domain.Models.Stores;
using InfinityMercsApp.Infrastructure.Providers;
using InfinityMercsApp.Services;
using InfinityMercsApp.Views.Controls;

namespace InfinityMercsApp.Views;

public partial class MarketplacesPage : ContentPage
{
    private readonly IStoreProvider _storeProvider;
    private readonly IMetadataProvider? _metadataProvider;
    private readonly IWikiDescriptionService? _wikiDescriptionService;
    private bool _showQuantityControls;
    private string _storeName = string.Empty;
    private string _storeFactionsDisplay = string.Empty;
    private string _storeMetaDisplay = string.Empty;

    public ObservableCollection<string> StoreNames { get; } = [];
    public ObservableCollection<ItemGroupViewModel> ItemGroups { get; } = [];
    public ObservableCollection<TroopTypeRowViewModel> TroopTypeRows { get; } = [];
    public ObservableCollection<AugmentRowViewModel> AugmentRows { get; } = [];

    public ICommand ShowItemCommand { get; }
    public ICommand ShowTroopTypeCommand { get; }
    public ICommand ShowAugmentCommand { get; }

    public bool ShowQuantityControls
    {
        get => _showQuantityControls;
        set
        {
            if (_showQuantityControls == value) return;
            _showQuantityControls = value;
            OnPropertyChanged();
        }
    }

    public string StoreName
    {
        get => _storeName;
        private set
        {
            if (_storeName == value) return;
            _storeName = value;
            OnPropertyChanged();
        }
    }

    public string StoreFactionsDisplay
    {
        get => _storeFactionsDisplay;
        private set
        {
            if (_storeFactionsDisplay == value) return;
            _storeFactionsDisplay = value;
            OnPropertyChanged();
        }
    }

    public string StoreMetaDisplay
    {
        get => _storeMetaDisplay;
        private set
        {
            if (_storeMetaDisplay == value) return;
            _storeMetaDisplay = value;
            OnPropertyChanged();
        }
    }

    public MarketplacesPage(IStoreProvider storeProvider, IMetadataProvider? metadataProvider = null, IWikiDescriptionService? wikiDescriptionService = null)
    {
        InitializeComponent();
        BindingContext = this;
        _storeProvider = storeProvider;
        _metadataProvider = metadataProvider;
        _wikiDescriptionService = wikiDescriptionService;

        ShowItemCommand = new Command<StoreItemRowViewModel>(ShowItemPopup);
        ShowTroopTypeCommand = new Command<TroopTypeRowViewModel>(ShowTroopTypePopup);
        ShowAugmentCommand = new Command<AugmentRowViewModel>(ShowAugmentPopup);

        LoadStoreNames();
    }

    private void LoadStoreNames()
    {
        StoreNames.Clear();
        foreach (var name in _storeProvider.GetAllStoreNames())
            StoreNames.Add(name);
    }

    private async void OnStorePickerSelectedIndexChanged(object sender, EventArgs e)
    {
        if (MarketplacePicker.SelectedIndex < 0 || MarketplacePicker.SelectedIndex >= StoreNames.Count)
        {
            PlaceholderLabel.IsVisible = true;
            StoreContentLayout.IsVisible = false;
            return;
        }

        var name = StoreNames[MarketplacePicker.SelectedIndex];
        var store = await _storeProvider.GetStoreByNameAsync(name);

        if (store is null)
            return;

        PopulateStore(store);
        PlaceholderLabel.IsVisible = false;
        StoreContentLayout.IsVisible = true;
    }

    private void PopulateStore(Store store)
    {
        StoreName = store.Name;

        StoreFactionsDisplay = store.AssociatedFactions.Count > 0
            ? $"Factions: {string.Join(", ", store.AssociatedFactions)}"
            : "Factions: All (Neutral)";

        var meta = new List<string>();
        if (!string.IsNullOrWhiteSpace(store.AssociatedType))
            meta.Add($"Type: {store.AssociatedType}");
        if (!string.IsNullOrWhiteSpace(store.Alignment))
            meta.Add($"Alignment: {store.Alignment}");
        StoreMetaDisplay = string.Join("   |   ", meta);

        ItemGroups.Clear();
        var grouped = store.Items.GroupBy(i => i.Category);
        foreach (var group in grouped)
        {
            var g = new ItemGroupViewModel { Category = group.Key };
            foreach (var item in group)
            {
                g.Items.Add(new StoreItemRowViewModel
                {
                    Name = item.Name,
                    CostCr = item.CostCr,
                    CostSwc = item.CostSwc,
                    CostSwcDisplay = item.CostSwc.HasValue ? item.CostSwc.Value.ToString("0.##") : "—",
                    Category = item.Category,
                    WikiUrl = item.WikiUrl,
                    WikiSection = item.WikiSection
                });
            }
            ItemGroups.Add(g);
        }
        ItemsBorder.IsVisible = ItemGroups.Count > 0;

        TroopTypeRows.Clear();
        foreach (var tt in store.TroopTypes)
        {
            TroopTypeRows.Add(new TroopTypeRowViewModel
            {
                TypeDisplay = tt.Type ?? "—",
                RawType = tt.Type,
                CostCr = tt.CostCr,
                ArmorName = tt.ArmorName,
                ArmDisplay = tt.Arm?.ToString() ?? "—",
                BtsDisplay = tt.Bts?.ToString() ?? "—",
                Abilities = tt.Abilities ?? string.Empty
            });
        }
        TroopTypesBorder.IsVisible = TroopTypeRows.Count > 0;

        AugmentRows.Clear();
        foreach (var aug in store.Augments)
        {
            AugmentRows.Add(new AugmentRowViewModel
            {
                Name = aug.Name,
                RequirementDisplay = aug.Requirement ?? "—",
                RawRequirement = aug.Requirement,
                CostCr = aug.CostCr,
                CostNote = aug.CostNote ?? string.Empty
            });
        }
        AugmentsBorder.IsVisible = AugmentRows.Count > 0;
    }

    // ── Popup handlers ──────────────────────────────────────────────────────

    private async void ShowItemPopup(StoreItemRowViewModel item)
    {
        PopupTitleLabel.Text = item.Name;
        PopupContentArea.Children.Clear();

        AddPopupRow("Cost", item.CostSwc.HasValue
            ? $"{item.CostCr}cr / {item.CostSwc.Value.ToString("0.##")}swc"
            : $"{item.CostCr}cr");
        AddPopupRow("Category", item.Category);

        // Wiki section — placeholder container inserted before weapon cards
        VerticalStackLayout? wikiContainer = null;
        if (_wikiDescriptionService is not null && !string.IsNullOrWhiteSpace(item.WikiUrl))
        {
            wikiContainer = new VerticalStackLayout { Spacing = 4, Margin = new Thickness(0, 6, 0, 0) };
            wikiContainer.Children.Add(new Label
            {
                Text = "Loading…",
                Style = (Style)Application.Current!.Resources["LabelBody"],
                TextColor = Color.FromArgb("#8A97A8"),
                Margin = new Thickness(0, 2, 0, 0)
            });
            PopupContentArea.Children.Add(wikiContainer);
        }

        AppendWeaponDetails(item.Name);

        MarketplacePopupOverlay.IsVisible = true;

        if (wikiContainer is not null && !string.IsNullOrWhiteSpace(item.WikiUrl))
        {
            var blocks = await _wikiDescriptionService!.FetchContentAsync(item.WikiUrl, item.WikiSection);
            wikiContainer.Children.Clear();
            if (blocks.Count == 0)
            {
                wikiContainer.Children.Add(new Label
                {
                    Text = "(No description available)",
                    Style = (Style)Application.Current!.Resources["LabelBody"],
                    TextColor = Color.FromArgb("#8A97A8")
                });
            }
            else
            {
                RenderWikiBlocks(wikiContainer, blocks);
            }
        }
    }

    private void ShowTroopTypePopup(TroopTypeRowViewModel troop)
    {
        var displayName = string.IsNullOrWhiteSpace(troop.RawType)
            ? troop.ArmorName
            : $"{troop.ArmorName} ({troop.RawType})";
        PopupTitleLabel.Text = displayName;
        PopupContentArea.Children.Clear();

        AddPopupRow("Cost", $"{troop.CostCr}cr");
        AddPopupRow("ARM", troop.ArmDisplay);
        AddPopupRow("BTS", troop.BtsDisplay);

        if (!string.IsNullOrWhiteSpace(troop.Abilities))
        {
            PopupContentArea.Children.Add(new Label
            {
                Text = "ABILITIES & EQUIPMENT",
                Style = (Style)Application.Current!.Resources["LabelCaption"],
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#8A97A8"),
                Margin = new Thickness(0, 8, 0, 4)
            });

            var skillsLookup = BuildSkillsWikiLookup();
            var abilities = troop.Abilities
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var ability in abilities)
            {
                var baseName = ExtractSkillBaseName(ability);
                var isSkill = skillsLookup.TryGetValue(baseName, out var wikiUrl);

                var label = new Label
                {
                    Text = $"• {ability}",
                    Style = (Style)Application.Current!.Resources["LabelBody"],
                    TextColor = isSkill ? Color.FromArgb("#B5C0CE") : Color.FromArgb("#E6EBF2"),
                    TextDecorations = isSkill ? TextDecorations.Underline : TextDecorations.None,
                    LineBreakMode = LineBreakMode.WordWrap
                };

                if (isSkill && !string.IsNullOrWhiteSpace(wikiUrl))
                {
                    var url = wikiUrl;
                    var tap = new TapGestureRecognizer();
                    tap.Tapped += async (_, _) => await OpenLinkAsync(url);
                    label.GestureRecognizers.Add(tap);
                }

                PopupContentArea.Children.Add(label);
            }
        }

        MarketplacePopupOverlay.IsVisible = true;
    }

    private void ShowAugmentPopup(AugmentRowViewModel augment)
    {
        PopupTitleLabel.Text = augment.Name;
        PopupContentArea.Children.Clear();

        AddPopupRow("Cost", $"{augment.CostCr}cr");
        if (!string.IsNullOrWhiteSpace(augment.RawRequirement))
            AddPopupRow("Requires", augment.RawRequirement);
        if (!string.IsNullOrWhiteSpace(augment.CostNote))
            AddPopupRow("Note", augment.CostNote);

        MarketplacePopupOverlay.IsVisible = true;
    }

    private void OnPopupBackClicked(object sender, EventArgs e)
    {
        MarketplacePopupOverlay.IsVisible = false;
    }

    // ── Popup helpers ────────────────────────────────────────────────────────

    private static void RenderWikiBlocks(Layout container, IReadOnlyList<Services.WikiContentBlock> blocks)
    {
        foreach (var block in blocks)
        {
            switch (block.Type)
            {
                case Services.WikiBlockType.SectionHeader:
                    container.Children.Add(new Border
                    {
                        BackgroundColor = Color.FromArgb("#3A4554"),
                        StrokeThickness = 0,
                        Padding = new Thickness(10, 6),
                        Margin = new Thickness(0, 8, 0, 2),
                        Content = new Label
                        {
                            Text = block.Text,
                            Style = (Style)Application.Current!.Resources["LabelBody"],
                            FontAttributes = FontAttributes.Bold,
                            TextColor = Color.FromArgb("#E6EBF2")
                        }
                    });
                    break;

                case Services.WikiBlockType.Paragraph:
                    container.Children.Add(new Label
                    {
                        Text = block.Text,
                        Style = (Style)Application.Current!.Resources["LabelBody"],
                        TextColor = Color.FromArgb("#B5C0CE"),
                        LineBreakMode = LineBreakMode.WordWrap,
                        Margin = new Thickness(0, 4, 0, 0)
                    });
                    break;

                case Services.WikiBlockType.BulletItem:
                {
                    double leftMargin = block.IndentLevel == 0 ? 4 : 20;
                    var row = new Grid
                    {
                        ColumnSpacing = 4,
                        Margin = new Thickness(leftMargin, 3, 0, 0)
                    };
                    row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                    row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });

                    row.Children.Add(new Label
                    {
                        Text = "▶",
                        Style = (Style)Application.Current!.Resources["LabelMicro"],
                        TextColor = Color.FromArgb("#8A97A8"),
                        VerticalTextAlignment = TextAlignment.Start,
                        Margin = new Thickness(0, 3, 0, 0)
                    });

                    var textLabel = new Label
                    {
                        Text = block.Text,
                        Style = (Style)Application.Current!.Resources["LabelBody"],
                        FontAttributes = block.Bold ? FontAttributes.Bold : FontAttributes.None,
                        TextColor = block.Bold ? Color.FromArgb("#E6EBF2") : Color.FromArgb("#B5C0CE"),
                        LineBreakMode = LineBreakMode.WordWrap
                    };
                    Grid.SetColumn(textLabel, 1);
                    row.Children.Add(textLabel);

                    container.Children.Add(row);
                    break;
                }
            }
        }
    }

    private void AddPopupRow(string labelText, string value)
    {
        var row = new Grid();
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
        row.Margin = new Thickness(0, 2);

        var labelView = new Label
        {
            Text = labelText,
            Style = (Style)Application.Current!.Resources["LabelBody"],
            TextColor = Color.FromArgb("#8A97A8"),
            VerticalTextAlignment = TextAlignment.Center
        };
        var valueView = new Label
        {
            Text = value,
            Style = (Style)Application.Current!.Resources["LabelBody"],
            TextColor = Color.FromArgb("#E6EBF2"),
            VerticalTextAlignment = TextAlignment.Center,
            LineBreakMode = LineBreakMode.WordWrap
        };
        Grid.SetColumn(valueView, 1);
        row.Children.Add(labelView);
        row.Children.Add(valueView);
        PopupContentArea.Children.Add(row);
    }

    private void AppendWeaponDetails(string weaponName)
    {
        var baseName = Regex.Match(weaponName, @"^[^(]+").Value.Trim();
        var weapons = FindAllWeaponsByName(baseName);
        foreach (var weapon in weapons)
        {
            PopupContentArea.Children.Add(new WeaponDetailCardView
            {
                Weapon = weapon,
                Margin = new Thickness(0, 0, 0, 8)
            });
        }
    }

    private IReadOnlyList<Domain.Models.Metadata.Weapon> FindAllWeaponsByName(string name)
    {
        var matches = _metadataProvider?.SearchWeaponsByName(name) ?? [];
        var exact = matches.Where(w => string.Equals(w.Name, name, StringComparison.OrdinalIgnoreCase)).ToList();
        return exact.Count > 0 ? exact : [.. matches];
    }

    private Dictionary<string, string?> BuildSkillsWikiLookup()
    {
        var skills = _metadataProvider?.GetSkills() ?? [];
        var lookup = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var s in skills)
        {
            var url = string.IsNullOrWhiteSpace(s.Wiki) ? BuildPropertyWikiUrl(s.Name) : s.Wiki;
            lookup.TryAdd(s.Name.Trim(), url);
        }
        return lookup;
    }

    private static string BuildPropertyWikiUrl(string propertyName)
    {
        var baseName = Regex.Match(propertyName, @"^[^(]+").Value.Trim();
        return $"https://infinitythewiki.com/{baseName.Replace(' ', '_')}?version=n4";
    }

    private static string ExtractSkillBaseName(string ability)
    {
        var beforeParen = Regex.Match(ability, @"^[^(]+").Value.Trim();
        return Regex.Replace(beforeParen, @"\W+$", string.Empty).Trim();
    }

    private static async Task OpenLinkAsync(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return;
        try
        {
            await Launcher.Default.OpenAsync(url);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to open link '{url}': {ex.Message}");
        }
    }

    // ── Quantity controls ────────────────────────────────────────────────────

    private void OnItemIncrement(object sender, EventArgs e)
    {
        if (sender is Button { CommandParameter: StoreItemRowViewModel row })
            row.Quantity++;
    }

    private void OnItemDecrement(object sender, EventArgs e)
    {
        if (sender is Button { CommandParameter: StoreItemRowViewModel row })
            row.Quantity--;
    }

    private void OnTroopIncrement(object sender, EventArgs e)
    {
        if (sender is Button { CommandParameter: TroopTypeRowViewModel row })
            row.Quantity++;
    }

    private void OnTroopDecrement(object sender, EventArgs e)
    {
        if (sender is Button { CommandParameter: TroopTypeRowViewModel row })
            row.Quantity--;
    }

    private void OnAugmentIncrement(object sender, EventArgs e)
    {
        if (sender is Button { CommandParameter: AugmentRowViewModel row })
            row.Quantity++;
    }

    private void OnAugmentDecrement(object sender, EventArgs e)
    {
        if (sender is Button { CommandParameter: AugmentRowViewModel row })
            row.Quantity--;
    }

    // ── View models ──────────────────────────────────────────────────────────

    public sealed class ItemGroupViewModel
    {
        public string Category { get; init; } = string.Empty;
        public ObservableCollection<StoreItemRowViewModel> Items { get; } = [];
    }

    public sealed class StoreItemRowViewModel : INotifyPropertyChanged
    {
        private int _quantity;

        public string Name { get; init; } = string.Empty;
        public int CostCr { get; init; }
        public decimal? CostSwc { get; init; }
        public string CostSwcDisplay { get; init; } = string.Empty;
        public string Category { get; init; } = string.Empty;
        public string? WikiUrl { get; init; }
        public string? WikiSection { get; init; }

        public string CostDisplay => CostSwc.HasValue
            ? $"{CostCr}cr / {CostSwc.Value.ToString("0.##")}swc"
            : $"{CostCr}cr";

        public int Quantity
        {
            get => _quantity;
            set
            {
                _quantity = Math.Max(0, value);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Quantity)));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }

    public sealed class TroopTypeRowViewModel : INotifyPropertyChanged
    {
        private int _quantity;

        public string TypeDisplay { get; init; } = string.Empty;
        public string? RawType { get; init; }
        public int CostCr { get; init; }
        public string ArmorName { get; init; } = string.Empty;
        public string ArmDisplay { get; init; } = string.Empty;
        public string BtsDisplay { get; init; } = string.Empty;
        public string Abilities { get; init; } = string.Empty;

        public string DisplayName => string.IsNullOrWhiteSpace(RawType)
            ? ArmorName
            : $"{ArmorName} ({RawType})";

        public string StatDisplay => $"ARM {ArmDisplay}  |  BTS {BtsDisplay}";

        public string CostDisplay => $"{CostCr}cr";

        public bool HasAbilities => !string.IsNullOrWhiteSpace(Abilities);

        public int Quantity
        {
            get => _quantity;
            set
            {
                _quantity = Math.Max(0, value);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Quantity)));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }

    public sealed class AugmentRowViewModel : INotifyPropertyChanged
    {
        private int _quantity;

        public string Name { get; init; } = string.Empty;
        public string RequirementDisplay { get; init; } = string.Empty;
        public string? RawRequirement { get; init; }
        public int CostCr { get; init; }
        public string CostNote { get; init; } = string.Empty;

        public string CostDisplay => $"{CostCr}cr";

        public bool HasRequirement => !string.IsNullOrWhiteSpace(RawRequirement);

        public int Quantity
        {
            get => _quantity;
            set
            {
                _quantity = Math.Max(0, value);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Quantity)));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
