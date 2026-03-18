using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Maui.Devices;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using Svg.Skia;

namespace InfinityMercsApp.Views.Common.Captain;

/// <summary>
/// Modal popup page that allows the user to configure captain upgrades before founding a company.
/// Displays stat upgrade pickers, weapon/skill/equipment choices, and a live experience budget tracker.
/// Use <see cref="ShowAsync"/> to push the page modally and await the confirmed result.
/// </summary>
public sealed class ConfigureCaptainPopupPage : ContentPage
{
    // Shared width applied to all choice pickers so they align consistently in the upgrade column.
    private const double UnifiedPickerWidth = 280;

    // Green highlight applied to a stat value when it has been improved above its base.
    private static readonly Color ModifiedStatColor = Color.FromArgb("#22C55E");
    private static readonly Color DefaultStatColor = Colors.White;

    /// <summary>
    /// Defines the available upgrade tiers, bonuses, and costs for each upgradeable stat.
    /// Hard-cap values prevent the stat from exceeding a fixed ceiling regardless of tier.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, StatPickerDefinition> StatDefinitions = new Dictionary<string, StatPickerDefinition>(StringComparer.OrdinalIgnoreCase)
    {
        ["CC"] = new StatPickerDefinition([0, 2, 5, 10], [0, 2, 3, 5]),
        ["BS"] = new StatPickerDefinition([0, 1, 2, 3], [0, 2, 3, 5]),
        ["PH"] = new StatPickerDefinition([0, 1, 3], [0, 2, 3], 14),
        ["WIP"] = new StatPickerDefinition([0, 1, 3, 6], [0, 2, 3, 5], 15),
        ["ARM"] = new StatPickerDefinition([0, 1, 3], [0, 5, 5]),
        ["BTS"] = new StatPickerDefinition([0, 3, 6, 9], [0, 2, 3, 5]),
        ["VITA"] = new StatPickerDefinition([0, 1], [0, 10], 2),
        ["STR"] = new StatPickerDefinition([0, 1], [0, 10], 2)
    };
    private const string NoneChoice = "(None)";

    private readonly CaptainUpgradePopupContext _context;

    // TaskCompletionSource is used so callers can await ShowAsync() and receive the result only when the modal is dismissed.
    private readonly TaskCompletionSource<SavedImprovedCaptainStats?> _resultSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly SKCanvasView _logoCanvas;

    // One picker per upgradeable stat; wired to SelectionChanged to update the live cost preview.
    private readonly Picker _ccPicker;
    private readonly Picker _bsPicker;
    private readonly Picker _phPicker;
    private readonly Picker _wipPicker;
    private readonly Picker _armPicker;
    private readonly Picker _btsPicker;
    private readonly Picker _vitaPicker;

    // Three slots each for weapons, skills, and equipment — any slot can be set to (None).
    private readonly Picker _weapon1Picker;
    private readonly Picker _weapon2Picker;
    private readonly Picker _weapon3Picker;
    private readonly Picker _skill1Picker;
    private readonly Picker _skill2Picker;
    private readonly Picker _skill3Picker;
    private readonly Picker _equipment1Picker;
    private readonly Picker _equipment2Picker;
    private readonly Picker _equipment3Picker;

    // Summary labels on the left column that update in real time as selections change.
    private readonly Label _rangedValueLabel;
    private readonly Label _ccValueLabel;
    private readonly Label _skillsValueLabel;
    private readonly Label _equipmentValueLabel;
    private readonly Label _upgradeOptionsHeaderLabel;
    private readonly Label _experienceRemainingLabel;
    private readonly Button _foundCompanyButton;

    // Parsed numeric base stats, used to compute deltas when a tier is selected.
    private readonly IReadOnlyDictionary<string, int> _baseStats;
    private readonly Grid _statsGrid;

    // Parallel collections that keep the stat grid in sync with picker selections.
    private readonly List<string> _statGridOrder = [];
    private readonly Dictionary<string, string> _statGridBaseValues = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Label> _statGridValueLabels = new(StringComparer.OrdinalIgnoreCase);

    // Captain name editing state — the Entry becomes editable when the pencil icon is tapped.
    private readonly Entry _captainNameEntry;
    private readonly Label _captainNameHeadingLabel;
    private readonly SKCanvasView _editCaptainNameCanvas;
    private readonly SKCanvasView _saveCaptainNameCanvas;
    private readonly SKCanvasView _rejectCaptainNameCanvas;
    private string _captainNameCommitted = "Captain";

    // SVG pictures loaded asynchronously; nullable until loading completes.
    private SKPicture? _logoPicture;
    private SKPicture? _editCaptainNamePicture;
    private SKPicture? _saveCaptainNamePicture;
    private SKPicture? _rejectCaptainNamePicture;

    // Used with Interlocked.Exchange to ensure CloseAsync runs at most once even with rapid taps.
    private int _isClosing;

    /// <summary>
    /// Private constructor — callers must use <see cref="ShowAsync"/> so that the modal lifecycle is managed correctly.
    /// </summary>
    private ConfigureCaptainPopupPage(CaptainUpgradePopupContext context)
    {
        _context = context;
        _baseStats = ParseBaseStats(context.Unit.Statline);

        // Cap the popup height to 80 % of the physical screen height to leave room for the OS chrome.
        var popupHeight = (DeviceDisplay.Current.MainDisplayInfo.Height / DeviceDisplay.Current.MainDisplayInfo.Density) * 0.8;
        BackgroundColor = Color.FromRgba(0, 0, 0, 180);
        Title = "Captain Configuration";

        _logoCanvas = new SKCanvasView
        {
            WidthRequest = 80,
            HeightRequest = 80,
            VerticalOptions = LayoutOptions.Start
        };
        _logoCanvas.PaintSurface += OnLogoCanvasPaintSurface;

        _ccPicker = BuildStatPicker("CC", ReadBaseStat("CC"));
        _bsPicker = BuildStatPicker("BS", ReadBaseStat("BS"));
        _phPicker = BuildStatPicker("PH", ReadBaseStat("PH"));
        _wipPicker = BuildStatPicker("WIP", ReadBaseStat("WIP"));
        _armPicker = BuildStatPicker("ARM", ReadBaseStat("ARM"));
        _btsPicker = BuildStatPicker("BTS", ReadBaseStat("BTS"));
        _vitaPicker = BuildStatPicker("VITA", ReadBaseStat("VITA", "STR", "W"));

        HookSelectionChanged(_ccPicker);
        HookSelectionChanged(_bsPicker);
        HookSelectionChanged(_phPicker);
        HookSelectionChanged(_wipPicker);
        HookSelectionChanged(_armPicker);
        HookSelectionChanged(_btsPicker);
        HookSelectionChanged(_vitaPicker);

        _weapon1Picker = BuildChoicePicker(context.WeaponOptions);
        _weapon2Picker = BuildChoicePicker(context.WeaponOptions);
        _weapon3Picker = BuildChoicePicker(context.WeaponOptions);
        _skill1Picker = BuildChoicePicker(context.SkillOptions);
        _skill2Picker = BuildChoicePicker(context.SkillOptions);
        _skill3Picker = BuildChoicePicker(context.SkillOptions);
        _equipment1Picker = BuildChoicePicker(context.EquipmentOptions);
        _equipment2Picker = BuildChoicePicker(context.EquipmentOptions);
        _equipment3Picker = BuildChoicePicker(context.EquipmentOptions);
        HookSelectionChanged(_weapon1Picker);
        HookSelectionChanged(_weapon2Picker);
        HookSelectionChanged(_weapon3Picker);
        HookSelectionChanged(_skill1Picker);
        HookSelectionChanged(_skill2Picker);
        HookSelectionChanged(_skill3Picker);
        HookSelectionChanged(_equipment1Picker);
        HookSelectionChanged(_equipment2Picker);
        HookSelectionChanged(_equipment3Picker);

        var cancelButton = new Button
        {
            Text = "BACK",
            BackgroundColor = Color.FromArgb("#374151"),
            TextColor = Colors.White,
            Command = new Command(async () => await CloseAsync(false))
        };
        _foundCompanyButton = new Button
        {
            Text = "FOUND COMPANY",
            BackgroundColor = Color.FromArgb("#7C3AED"),
            TextColor = Colors.Black,
            Command = new Command(async () => await CloseAsync(true))
        };

        var actions = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            },
            ColumnSpacing = 10,
            HorizontalOptions = LayoutOptions.Fill,
            Children = { cancelButton, _foundCompanyButton }
        };
        Grid.SetColumn(cancelButton, 0);
        Grid.SetColumn(_foundCompanyButton, 2);

        var rangedBlock = BuildProfileDetailBlock("Ranged", Color.FromArgb("#EF4444"), out _rangedValueLabel);
        var ccBlock = BuildProfileDetailBlock("CC", Color.FromArgb("#22C55E"), out _ccValueLabel);
        var skillsBlock = BuildProfileDetailBlock("Skills", Color.FromArgb("#F59E0B"), out _skillsValueLabel);
        var equipmentBlock = BuildProfileDetailBlock("Equipment", Color.FromArgb("#06B6D4"), out _equipmentValueLabel);

        _captainNameEntry = new Entry
        {
            Text = _captainNameCommitted,
            IsReadOnly = true,
            FontSize = 22,
            HorizontalOptions = LayoutOptions.Fill
        };
        _captainNameHeadingLabel = new Label
        {
            Text = _captainNameCommitted,
            FontAttributes = FontAttributes.Bold,
            FontSize = 22,
            LineBreakMode = LineBreakMode.WordWrap
        };
        _editCaptainNameCanvas = BuildCaptainNameIconCanvas(OnEditCaptainNameTapped);
        _editCaptainNameCanvas.PaintSurface += OnEditCaptainNameCanvasPaintSurface;
        _saveCaptainNameCanvas = BuildCaptainNameIconCanvas(OnSaveCaptainNameTapped);
        _saveCaptainNameCanvas.PaintSurface += OnSaveCaptainNameCanvasPaintSurface;
        _rejectCaptainNameCanvas = BuildCaptainNameIconCanvas(OnRejectCaptainNameTapped);
        _rejectCaptainNameCanvas.PaintSurface += OnRejectCaptainNameCanvasPaintSurface;
        SetCaptainNameEditMode(isEditing: false);

        var captainNameRow = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Auto }
            },
            ColumnSpacing = 8,
            Children =
            {
                _captainNameEntry,
                _editCaptainNameCanvas,
                _saveCaptainNameCanvas,
                _rejectCaptainNameCanvas
            }
        };
        Grid.SetColumn(_captainNameEntry, 0);
        Grid.SetColumn(_editCaptainNameCanvas, 1);
        Grid.SetColumn(_saveCaptainNameCanvas, 2);
        Grid.SetColumn(_rejectCaptainNameCanvas, 3);

        _statsGrid = BuildStatsGrid(_context.Unit.Statline);

        var leftColumn = new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                captainNameRow,
                _logoCanvas,
                _captainNameHeadingLabel,
                _statsGrid,
                rangedBlock,
                ccBlock,
                skillsBlock,
                equipmentBlock
            }
        };

        _upgradeOptionsHeaderLabel = new Label
        {
            FontAttributes = FontAttributes.Bold,
            FontSize = 18,
            TextColor = Colors.White
        };
        _experienceRemainingLabel = new Label
        {
            FontAttributes = FontAttributes.Bold,
            FontSize = 15,
            TextColor = Colors.White
        };

        var rightColumnBody = new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                BuildStatRowPair(("CC", _ccPicker), ("BS", _bsPicker)),
                BuildStatRowPair(("PH", _phPicker), ("WIP", _wipPicker)),
                BuildStatRowPair(("ARM", _armPicker), ("BTS", _btsPicker)),
                BuildStatRowPair(("VITA", _vitaPicker), null),
                BuildCategorySection("Weapons", _weapon1Picker, _weapon2Picker, _weapon3Picker),
                BuildCategorySection("Skills", _skill1Picker, _skill2Picker, _skill3Picker),
                BuildCategorySection("Equipment", _equipment1Picker, _equipment2Picker, _equipment3Picker)
            }
        };

        var rightBodyScroll = new ScrollView { Content = rightColumnBody };
        var leftScroll = new ScrollView { Content = leftColumn };
        var rightColumn = new Grid
        {
            RowDefinitions = new RowDefinitionCollection
            {
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Star }
            },
            RowSpacing = 6,
            Children =
            {
                _upgradeOptionsHeaderLabel,
                _experienceRemainingLabel,
                rightBodyScroll
            }
        };
        Grid.SetRow(_experienceRemainingLabel, 1);
        Grid.SetRow(rightBodyScroll, 2);
        var columnsGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Star }
            },
            ColumnSpacing = 18,
            Children = { leftScroll, rightColumn }
        };
        Grid.SetColumn(rightColumn, 1);

        var cardContent = new Grid
        {
            WidthRequest = 980,
            RowDefinitions = new RowDefinitionCollection
            {
                new RowDefinition { Height = GridLength.Star },
                new RowDefinition { Height = GridLength.Auto }
            },
            RowSpacing = 14,
            Children = { columnsGrid, actions }
        };
        var actionsHost = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Star }
            },
            Children = { actions }
        };
        Grid.SetColumn(actions, 0);
        Grid.SetRow(actions, 1);
        cardContent.Children.Remove(actions);
        cardContent.Children.Add(actionsHost);
        Grid.SetRow(actionsHost, 1);

        var card = new Border
        {
            BackgroundColor = Color.FromArgb("#111827"),
            Stroke = Color.FromArgb("#374151"),
            StrokeThickness = 1,
            Padding = new Thickness(16),
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            HeightRequest = popupHeight,
            Content = cardContent
        };

        Content = new Grid
        {
            Children = { card }
        };

        UpdateProfilePreviewFromSelections();
        _ = LoadLogoAsync();
        _ = LoadCaptainNameActionIconsAsync();
    }

    /// <summary>
    /// Pushes the popup modally and waits until the user confirms or cancels.
    /// Returns the configured <see cref="SavedImprovedCaptainStats"/>, or <c>null</c> if cancelled.
    /// </summary>
    public static async Task<SavedImprovedCaptainStats?> ShowAsync(INavigation navigation, CaptainUpgradePopupContext context)
    {
        var page = new ConfigureCaptainPopupPage(context);
        await navigation.PushModalAsync(page, false);
        return await page._resultSource.Task;
    }

    /// <summary>
    /// Intercepts the hardware back button so it behaves identically to tapping BACK
    /// rather than leaving the result source unresolved.
    /// </summary>
    protected override bool OnBackButtonPressed()
    {
        _ = CloseAsync(false);
        return true;
    }

    /// <summary>
    /// Loads the unit's faction logo as an SVG picture for the header canvas.
    /// Prefers the cached on-disk path; falls back to the bundled app-package asset.
    /// Failures are logged and result in no logo being displayed rather than crashing.
    /// </summary>
    private async Task LoadLogoAsync()
    {
        _logoPicture?.Dispose();
        _logoPicture = null;

        try
        {
            Stream? stream = null;

            // Prefer the pre-downloaded cache to avoid reading bundled assets repeatedly.
            if (!string.IsNullOrWhiteSpace(_context.Unit.CachedLogoPath) && File.Exists(_context.Unit.CachedLogoPath))
            {
                stream = File.OpenRead(_context.Unit.CachedLogoPath);
            }
            else if (!string.IsNullOrWhiteSpace(_context.Unit.PackagedLogoPath))
            {
                stream = await FileSystem.Current.OpenAppPackageFileAsync(_context.Unit.PackagedLogoPath);
            }

            if (stream is null)
            {
                _logoCanvas.InvalidateSurface();
                return;
            }

            await using (stream)
            {
                var svg = new SKSvg();
                _logoPicture = svg.Load(stream);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ConfigureCaptainPopupPage LoadLogoAsync failed: {ex.Message}");
            _logoPicture = null;
        }

        _logoCanvas.InvalidateSurface();
    }

    /// <summary>
    /// Renders the SVG logo centred and uniformly scaled within the canvas bounds.
    /// </summary>
    private void OnLogoCanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        if (_logoPicture is null)
        {
            return;
        }

        var bounds = _logoPicture.CullRect;
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        // Scale uniformly so the logo fits entirely within the canvas without distortion.
        var scale = Math.Min(e.Info.Width / bounds.Width, e.Info.Height / bounds.Height);
        var x = (e.Info.Width - (bounds.Width * scale)) / 2f;
        var y = (e.Info.Height - (bounds.Height * scale)) / 2f;

        canvas.Translate(x, y);
        canvas.Scale(scale);
        canvas.DrawPicture(_logoPicture);
    }

    /// <summary>
    /// Closes the popup, resolving the result source with the configured stats on confirmation
    /// or with <c>null</c> on cancellation. Guards against being called more than once via
    /// an atomic flag so that rapid back-button presses cannot double-dismiss the modal.
    /// </summary>
    private async Task CloseAsync(bool confirmed)
    {
        // Atomically swap _isClosing to 1; if it was already 1, another call is in progress.
        if (Interlocked.Exchange(ref _isClosing, 1) == 1)
        {
            return;
        }

        if (!confirmed)
        {
            _resultSource.TrySetResult(null);
            DisposeCaptainNameActionIcons();
            await DismissModalIfTopAsync();

            return;
        }

        // Ensure any in-progress name edit is committed before capturing the result.
        CommitCaptainNameFromEntry();

        var stats = new SavedImprovedCaptainStats
        {
            IsEnabled = true,
            CaptainName = _captainNameCommitted,
            CcTier = ReadStatTier(_ccPicker),
            BsTier = ReadStatTier(_bsPicker),
            PhTier = ReadStatTier(_phPicker),
            WipTier = ReadStatTier(_wipPicker),
            ArmTier = ReadStatTier(_armPicker),
            BtsTier = ReadStatTier(_btsPicker),
            VitalityTier = ReadStatTier(_vitaPicker),
            CcBonus = ReadStatBonus(_ccPicker),
            BsBonus = ReadStatBonus(_bsPicker),
            PhBonus = ReadStatBonus(_phPicker),
            WipBonus = ReadStatBonus(_wipPicker),
            ArmBonus = ReadStatBonus(_armPicker),
            BtsBonus = ReadStatBonus(_btsPicker),
            VitalityBonus = ReadStatBonus(_vitaPicker),
            WeaponChoice1 = ReadChoice(_weapon1Picker),
            WeaponChoice2 = ReadChoice(_weapon2Picker),
            WeaponChoice3 = ReadChoice(_weapon3Picker),
            SkillChoice1 = ReadChoice(_skill1Picker),
            SkillChoice2 = ReadChoice(_skill2Picker),
            SkillChoice3 = ReadChoice(_skill3Picker),
            EquipmentChoice1 = ReadChoice(_equipment1Picker),
            EquipmentChoice2 = ReadChoice(_equipment2Picker),
            EquipmentChoice3 = ReadChoice(_equipment3Picker),
            OptionFactionId = _context.OptionFactionId,
            OptionFactionName = _context.OptionFactionName
        };

        _resultSource.TrySetResult(stats);
        DisposeCaptainNameActionIcons();
        await DismissModalIfTopAsync();
    }

    /// <summary>
    /// Pops this page from the modal stack only if it is still the topmost modal.
    /// Guards against edge cases where navigation state changed between the close request and execution.
    /// </summary>
    private async Task DismissModalIfTopAsync()
    {
        try
        {
            var navigation = Navigation;
            var modalStack = navigation?.ModalStack;
            if (modalStack is null || modalStack.Count == 0)
            {
                return;
            }

            // Do not pop if another modal was pushed on top of this one in the meantime.
            if (!ReferenceEquals(modalStack[^1], this))
            {
                return;
            }

            if (navigation is null)
            {
                return;
            }

            await navigation.PopModalAsync(false);
        }
        catch (ArgumentException ex) when (ex.Message.Contains("Ambiguous routes matched", StringComparison.OrdinalIgnoreCase))
        {
            // Shell routing can throw this transiently when dismissing; safe to ignore.
            Console.Error.WriteLine($"ConfigureCaptainPopupPage DismissModalIfTopAsync ignored ambiguous route pop: {ex.Message}");
        }
    }

    /// <summary>
    /// Creates a stat-upgrade picker pre-populated with the tier options computed from the unit's base value.
    /// Defaults to index 0 (no upgrade).
    /// </summary>
    private static Picker BuildStatPicker(string statName, int baseValue)
    {
        var options = BuildStatOptions(statName, baseValue);
        var picker = new Picker
        {
            HorizontalOptions = LayoutOptions.Fill,
            HorizontalTextAlignment = TextAlignment.Center,
            ItemsSource = options,
            ItemDisplayBinding = new Binding(nameof(StatPickerOption.Label)),
            SelectedIndex = 0
        };

        return picker;
    }

    /// <summary>
    /// Creates a choice picker (weapon / skill / equipment) with a leading "(None)" entry
    /// followed by the deduplicated option list from the context.
    /// </summary>
    private static Picker BuildChoicePicker(IEnumerable<string> options)
    {
        var values = new List<string> { NoneChoice };
        values.AddRange(options.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase));

        return new Picker
        {
            WidthRequest = UnifiedPickerWidth,
            HorizontalOptions = LayoutOptions.Start,
            HorizontalTextAlignment = TextAlignment.Center,
            ItemsSource = values,
            SelectedIndex = 0
        };
    }

    // Returns just the picker; the label parameter is reserved for potential future use.
    private static View BuildStatRow(string label, Picker picker)
    {
        return picker;
    }

    /// <summary>
    /// Lays out two stat pickers side by side in equal columns.
    /// The second slot is optional; if omitted, only the first picker is placed.
    /// </summary>
    private static View BuildStatRowPair((string Label, Picker Picker) first, (string Label, Picker Picker)? second)
    {
        var grid = new Grid
        {
            ColumnSpacing = 8,
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Star }
            }
        };

        var firstCell = BuildStatRow(first.Label, first.Picker);
        grid.Children.Add(firstCell);
        Grid.SetColumn(firstCell, 0);

        if (second.HasValue)
        {
            var secondCell = BuildStatRow(second.Value.Label, second.Value.Picker);
            grid.Children.Add(secondCell);
            Grid.SetColumn(secondCell, 1);
        }

        return grid;
    }

    /// <summary>
    /// Groups three pickers under a bold category heading (Weapons / Skills / Equipment).
    /// </summary>
    private static View BuildCategorySection(string title, Picker first, Picker second, Picker third)
    {
        return new VerticalStackLayout
        {
            Spacing = 4,
            Margin = new Thickness(0, 8, 0, 0),
            Children =
            {
                new Label { Text = title, FontAttributes = FontAttributes.Bold },
                first,
                second,
                third
            }
        };
    }

    /// <summary>
    /// Returns the upgrade tier of the currently selected item, or 0 if nothing is selected.
    /// </summary>
    private static int ReadStatTier(Picker picker)
    {
        return picker.SelectedItem is StatPickerOption option ? option.Tier : 0;
    }

    /// <summary>
    /// Returns the numeric stat bonus of the currently selected tier, or 0 if nothing is selected.
    /// </summary>
    private static int ReadStatBonus(Picker picker)
    {
        return picker.SelectedItem is StatPickerOption option ? option.Bonus : 0;
    }

    /// <summary>
    /// Reads the selected choice label, stripping the leading cost annotation (e.g. "(2) - ")
    /// and returning an empty string when "(None)" is selected.
    /// </summary>
    private static string ReadChoice(Picker picker)
    {
        var value = picker.SelectedItem?.ToString() ?? string.Empty;
        if (string.Equals(value, NoneChoice, StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        // Strip the "(cost) - " prefix that is prepended to choice labels for display purposes.
        var normalized = Regex.Replace(value, @"^\s*\([-+]?\d+\)\s*-\s*", string.Empty).Trim();
        return normalized;
    }

    // Returns a dash placeholder for null/empty values shown in summary labels.
    private static string NormalizeText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "-" : value;
    }

    /// <summary>
    /// Builds a two-row block displaying a category label and its coloured value label.
    /// The value label is returned via an out parameter so callers can update it dynamically.
    /// </summary>
    private static View BuildProfileDetailBlock(string label, Color valueColor, out Label valueLabel)
    {
        valueLabel = new Label
        {
            Text = "-",
            FontSize = 19,
            TextColor = valueColor,
            HorizontalTextAlignment = TextAlignment.End,
            LineBreakMode = LineBreakMode.WordWrap
        };

        return new VerticalStackLayout
        {
            Spacing = 1,
            Children =
            {
                new Label
                {
                    Text = $"{label}:",
                    FontSize = 22,
                    LineBreakMode = LineBreakMode.WordWrap
                },
                valueLabel
            }
        };
    }

    /// <summary>
    /// Subscribes a picker's SelectionChanged event to the shared preview refresh handler.
    /// </summary>
    private void HookSelectionChanged(Picker picker)
    {
        picker.SelectedIndexChanged += (_, _) => UpdateProfilePreviewFromSelections();
    }

    /// <summary>
    /// Rebuilds all live-preview labels (statline, ranged, CC, skills, equipment) and
    /// refreshes the experience budget whenever any picker selection changes.
    /// </summary>
    private void UpdateProfilePreviewFromSelections()
    {
        UpdateStatlinePreview();
        _rangedValueLabel.Text = BuildUpdatedProfileSection(
            _context.Unit.RangedWeapons,
            GetSelectedChoices(_weapon1Picker, _weapon2Picker, _weapon3Picker),
            prependPlus: true);
        _ccValueLabel.Text = NormalizeText(_context.Unit.CcWeapons);
        _skillsValueLabel.Text = BuildUpdatedProfileSection(
            _context.Unit.Skills,
            GetSelectedChoices(_skill1Picker, _skill2Picker, _skill3Picker),
            prependPlus: true);
        _equipmentValueLabel.Text = BuildUpdatedProfileSection(
            _context.Unit.Equipment,
            GetSelectedChoices(_equipment1Picker, _equipment2Picker, _equipment3Picker),
            prependPlus: true);
        UpdateUpgradeOptionsHeader();
    }

    /// <summary>
    /// Recalculates remaining experience and updates the header label and Confirm button state.
    /// Base experience is derived from the unit cost: cheaper units start with more experience to spend.
    /// </summary>
    private void UpdateUpgradeOptionsHeader()
    {
        // Units cost at most 28 pts; the remainder becomes the starting experience budget.
        var baseExperience = Math.Max(0, 28 - _context.Unit.Cost);
        var selectedCost =
            ReadStatPoints(_ccPicker) +
            ReadStatPoints(_bsPicker) +
            ReadStatPoints(_phPicker) +
            ReadStatPoints(_wipPicker) +
            ReadStatPoints(_armPicker) +
            ReadStatPoints(_btsPicker) +
            ReadStatPoints(_vitaPicker) +
            ReadChoicePoints(_weapon1Picker) +
            ReadChoicePoints(_weapon2Picker) +
            ReadChoicePoints(_weapon3Picker) +
            ReadChoicePoints(_skill1Picker) +
            ReadChoicePoints(_skill2Picker) +
            ReadChoicePoints(_skill3Picker) +
            ReadChoicePoints(_equipment1Picker) +
            ReadChoicePoints(_equipment2Picker) +
            ReadChoicePoints(_equipment3Picker);
        var experienceRemaining = baseExperience - selectedCost;

        _upgradeOptionsHeaderLabel.Text = $"Upgrade Options ({_context.OptionFactionName})";
        _experienceRemainingLabel.Text = $"Exp Remaining: {experienceRemaining}";

        // Turn the remaining exp label red and disable the confirm button when overspent.
        _experienceRemainingLabel.TextColor = experienceRemaining < 0 ? Colors.Red : Colors.White;
        _foundCompanyButton.IsEnabled = experienceRemaining >= 0;
        _foundCompanyButton.BackgroundColor = experienceRemaining < 0 ? Color.FromArgb("#6B7280") : Color.FromArgb("#7C3AED");
    }

    // Thin wrapper kept for consistency; may expand to include additional preview logic later.
    private void UpdateStatlinePreview()
    {
        UpdateStatsGridValues();
    }

    /// <summary>
    /// Looks up the first matching stat key from the parsed base stats dictionary.
    /// Accepts multiple candidate keys to handle aliases such as VITA / STR / W.
    /// </summary>
    private int ReadBaseStat(params string[] statNames)
    {
        foreach (var statName in statNames)
        {
            if (_baseStats.TryGetValue(statName, out var value))
            {
                return value;
            }
        }

        return 0;
    }

    /// <summary>
    /// Builds the ordered list of <see cref="StatPickerOption"/> items for a single stat.
    /// Tiers are added incrementally; a hard cap stops further tiers once the stat hits its ceiling.
    /// Costs accumulate across tiers so the picker always shows the total spent.
    /// </summary>
    private static List<StatPickerOption> BuildStatOptions(string statName, int baseValue)
    {
        if (!StatDefinitions.TryGetValue(statName, out var definition))
        {
            // Unknown stat — return a single "no upgrade" option rather than an empty list.
            return [new StatPickerOption(statName, 0, 0, 0)];
        }

        var options = new List<StatPickerOption>
        {
            new(statName, 0, 0, 0)
        };

        var currentValue = baseValue;
        var cumulativeCost = 0;
        for (var tier = 1; tier <= definition.MaxTier; tier++)
        {
            // Skip remaining tiers if the stat has already reached the hard cap.
            if (definition.HardCap.HasValue && currentValue >= definition.HardCap.Value)
            {
                break;
            }

            var targetValue = baseValue + definition.BonusesByTier[tier];
            if (definition.HardCap.HasValue)
            {
                // Clamp the bonus so the stat cannot exceed the hard cap even if a tier would push it over.
                targetValue = Math.Min(targetValue, definition.HardCap.Value);
            }

            var appliedBonus = Math.Max(0, targetValue - baseValue);
            cumulativeCost += definition.CostsByTier[tier];
            options.Add(new StatPickerOption(statName, tier, appliedBonus, cumulativeCost));
            currentValue = targetValue;
        }

        return options;
    }

    /// <summary>
    /// Returns the experience point cost of the currently selected stat tier, or 0 if nothing is selected.
    /// </summary>
    private static int ReadStatPoints(Picker picker)
    {
        return picker.SelectedItem is StatPickerOption option ? option.Cost : 0;
    }

    /// <summary>
    /// Parses a raw statline string (e.g. "CC 14 | BS 12 | ...") into a keyed integer dictionary.
    /// Used to seed the pickers with the correct base values for bonus calculations.
    /// </summary>
    private static IReadOnlyDictionary<string, int> ParseBaseStats(string? statline)
    {
        var values = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(statline))
        {
            return values;
        }

        var matches = Regex.Matches(statline, @"\b(CC|BS|PH|WIP|ARM|BTS|VITA|STR|W)\s+(\d+)\b", RegexOptions.IgnoreCase);
        foreach (Match match in matches)
        {
            if (!match.Success)
            {
                continue;
            }

            var key = match.Groups[1].Value.ToUpperInvariant();
            if (int.TryParse(match.Groups[2].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            {
                values[key] = parsed;
            }
        }

        return values;
    }

    /// <summary>
    /// Constructs the stat grid shown in the left column using the parsed statline.
    /// Registers each stat's value label in <see cref="_statGridValueLabels"/> so it can be
    /// updated live when upgrade pickers change.
    /// </summary>
    private Grid BuildStatsGrid(string? statline)
    {
        var entries = ParseStatsGridEntries(statline);
        var grid = new Grid
        {
            ColumnSpacing = 10,
            RowSpacing = 2,
            RowDefinitions = new RowDefinitionCollection
            {
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Auto }
            }
        };

        if (entries.Count == 0)
        {
            // Show a single dash column when no stats could be parsed.
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            var empty = new Label
            {
                Text = "-",
                FontSize = 19,
                LineBreakMode = LineBreakMode.WordWrap
            };
            grid.Children.Add(empty);
            Grid.SetRow(empty, 0);
            return grid;
        }

        // Row 0 = stat key (bold header), Row 1 = current value (updated as tiers change).
        for (var i = 0; i < entries.Count; i++)
        {
            var (key, value) = entries[i];
            _statGridOrder.Add(key);
            _statGridBaseValues[key] = value;
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });

            var keyLabel = new Label
            {
                Text = key,
                FontSize = 19,
                FontAttributes = FontAttributes.Bold,
                HorizontalTextAlignment = TextAlignment.Center
            };
            var valueLabel = new Label
            {
                Text = value,
                FontSize = 19,
                HorizontalTextAlignment = TextAlignment.Center,
                TextColor = DefaultStatColor
            };

            _statGridValueLabels[key] = valueLabel;
            grid.Children.Add(keyLabel);
            grid.Children.Add(valueLabel);
            Grid.SetColumn(keyLabel, i);
            Grid.SetRow(keyLabel, 0);
            Grid.SetColumn(valueLabel, i);
            Grid.SetRow(valueLabel, 1);
        }

        return grid;
    }

    /// <summary>
    /// Splits a pipe-delimited statline into (key, value) pairs for the stat grid.
    /// Segments that do not match the expected "KEY value" pattern are silently skipped.
    /// </summary>
    private static List<(string Key, string Value)> ParseStatsGridEntries(string? statline)
    {
        var entries = new List<(string Key, string Value)>();
        if (string.IsNullOrWhiteSpace(statline))
        {
            return entries;
        }

        foreach (var segment in statline.Split('|', StringSplitOptions.TrimEntries))
        {
            var match = Regex.Match(segment, @"^\s*([A-Za-z]+)\s+(.+)\s*$", RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                continue;
            }

            entries.Add((match.Groups[1].Value.ToUpperInvariant(), match.Groups[2].Value.Trim()));
        }

        return entries;
    }

    /// <summary>
    /// Re-evaluates every stat label in the grid, applying the current picker bonus
    /// and switching the text colour to green when the value has been modified.
    /// Non-numeric stat values (e.g. "-") are displayed as-is without colour change.
    /// </summary>
    private void UpdateStatsGridValues()
    {
        foreach (var key in _statGridOrder)
        {
            if (!_statGridValueLabels.TryGetValue(key, out var valueLabel) ||
                !_statGridBaseValues.TryGetValue(key, out var rawValue))
            {
                continue;
            }

            if (int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var numericBase))
            {
                var bonus = ReadStatlineBonus(key);
                var modifiedValue = numericBase + bonus;
                valueLabel.Text = modifiedValue.ToString(CultureInfo.InvariantCulture);
                // Highlight in green only when the value differs from the base.
                valueLabel.TextColor = modifiedValue == numericBase ? DefaultStatColor : ModifiedStatColor;
            }
            else
            {
                valueLabel.Text = rawValue;
                valueLabel.TextColor = DefaultStatColor;
            }
        }
    }

    /// <summary>
    /// Maps a stat key to the corresponding upgrade picker and returns the selected bonus.
    /// VITA, STR, and W all share the single vitality picker.
    /// </summary>
    private int ReadStatlineBonus(string statKey)
    {
        return statKey switch
        {
            "CC" => ReadStatBonus(_ccPicker),
            "BS" => ReadStatBonus(_bsPicker),
            "PH" => ReadStatBonus(_phPicker),
            "WIP" => ReadStatBonus(_wipPicker),
            "ARM" => ReadStatBonus(_armPicker),
            "BTS" => ReadStatBonus(_btsPicker),
            "VITA" or "STR" or "W" => ReadStatBonus(_vitaPicker),
            _ => 0
        };
    }

    /// <summary>
    /// Collects the non-empty, deduplicated choice strings from an arbitrary set of pickers.
    /// Used to build the additions list for the profile preview sections.
    /// </summary>
    private static List<string> GetSelectedChoices(params Picker[] pickers)
    {
        return pickers
            .Select(ReadChoice)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// Creates a small 28x28 tappable SkiaSharp canvas for the captain-name action icons
    /// (edit, save, reject).
    /// </summary>
    private static SKCanvasView BuildCaptainNameIconCanvas(EventHandler<TappedEventArgs> tappedHandler)
    {
        var canvas = new SKCanvasView
        {
            WidthRequest = 28,
            HeightRequest = 28,
            VerticalOptions = LayoutOptions.Center
        };
        var tap = new TapGestureRecognizer();
        tap.Tapped += tappedHandler;
        canvas.GestureRecognizers.Add(tap);
        return canvas;
    }

    /// <summary>
    /// Switches the captain-name row between read-only display mode and editable mode,
    /// toggling the visibility of the edit/save/reject icon canvases accordingly.
    /// </summary>
    private void SetCaptainNameEditMode(bool isEditing)
    {
        _captainNameEntry.IsEnabled = isEditing;
        _captainNameEntry.IsReadOnly = !isEditing;
        _editCaptainNameCanvas.IsVisible = !isEditing;
        _saveCaptainNameCanvas.IsVisible = isEditing;
        _rejectCaptainNameCanvas.IsVisible = isEditing;
    }

    private void OnEditCaptainNameTapped(object? sender, TappedEventArgs e)
    {
        SetCaptainNameEditMode(isEditing: true);
        _captainNameEntry.Focus();
    }

    private void OnSaveCaptainNameTapped(object? sender, TappedEventArgs e)
    {
        CommitCaptainNameFromEntry();
    }

    /// <summary>
    /// Discards any unsaved changes to the captain name and reverts the entry to the last committed value.
    /// </summary>
    private void OnRejectCaptainNameTapped(object? sender, TappedEventArgs e)
    {
        _captainNameEntry.Text = _captainNameCommitted;
        SetCaptainNameEditMode(isEditing: false);
    }

    /// <summary>
    /// Validates and persists the captain name from the entry field.
    /// Falls back to "Captain" when the entry is empty or whitespace-only.
    /// </summary>
    private void CommitCaptainNameFromEntry()
    {
        var normalized = string.IsNullOrWhiteSpace(_captainNameEntry.Text) ? "Captain" : _captainNameEntry.Text.Trim();
        _captainNameCommitted = normalized;
        _captainNameEntry.Text = _captainNameCommitted;
        _captainNameHeadingLabel.Text = _captainNameCommitted;
        SetCaptainNameEditMode(isEditing: false);
    }

    /// <summary>
    /// Asynchronously loads the three SVG icons used in the captain-name editing row.
    /// Each icon is loaded independently so a failure in one does not block the others.
    /// </summary>
    private async Task LoadCaptainNameActionIconsAsync()
    {
        DisposeCaptainNameActionIcons();

        try
        {
            await using var editStream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/NonCBIcons/noun-edit-333556.svg");
            var svg = new SKSvg();
            _editCaptainNamePicture = svg.Load(editStream);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ConfigureCaptainPopupPage edit icon load failed: {ex.Message}");
            _editCaptainNamePicture = null;
        }

        try
        {
            await using var saveStream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/NonCBIcons/noun-check-3612574.svg");
            var svg = new SKSvg();
            _saveCaptainNamePicture = svg.Load(saveStream);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ConfigureCaptainPopupPage save icon load failed: {ex.Message}");
            _saveCaptainNamePicture = null;
        }

        try
        {
            await using var rejectStream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/NonCBIcons/noun-x-1890844.svg");
            var svg = new SKSvg();
            _rejectCaptainNamePicture = svg.Load(rejectStream);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ConfigureCaptainPopupPage reject icon load failed: {ex.Message}");
            _rejectCaptainNamePicture = null;
        }

        _editCaptainNameCanvas.InvalidateSurface();
        _saveCaptainNameCanvas.InvalidateSurface();
        _rejectCaptainNameCanvas.InvalidateSurface();
    }

    /// <summary>
    /// Releases the three SVG pictures for the captain-name action icons.
    /// Must be called before the page is dismissed to prevent native resource leaks.
    /// </summary>
    private void DisposeCaptainNameActionIcons()
    {
        _editCaptainNamePicture?.Dispose();
        _editCaptainNamePicture = null;
        _saveCaptainNamePicture?.Dispose();
        _saveCaptainNamePicture = null;
        _rejectCaptainNamePicture?.Dispose();
        _rejectCaptainNamePicture = null;
    }

    /// <summary>
    /// Renders an SVG action icon centred and uniformly scaled on the given canvas.
    /// Shared by the edit, save, and reject icon canvases.
    /// </summary>
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

        var scale = Math.Min(info.Width / bounds.Width, info.Height / bounds.Height);
        var x = (info.Width - (bounds.Width * scale)) / 2f;
        var y = (info.Height - (bounds.Height * scale)) / 2f;
        canvas.Translate(x, y);
        canvas.Scale(scale);
        canvas.DrawPicture(picture);
    }

    private void OnEditCaptainNameCanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        DrawActionIcon(e.Surface.Canvas, e.Info, _editCaptainNamePicture);
    }

    private void OnSaveCaptainNameCanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        DrawActionIcon(e.Surface.Canvas, e.Info, _saveCaptainNamePicture);
    }

    private void OnRejectCaptainNameCanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        DrawActionIcon(e.Surface.Canvas, e.Info, _rejectCaptainNamePicture);
    }

    /// <summary>
    /// Extracts the integer cost prefix from a choice picker item (e.g. "(2) - Sniper Rifle" → 2).
    /// Returns 0 if the item is "(None)" or has no cost annotation.
    /// </summary>
    private static int ReadChoicePoints(Picker picker)
    {
        var rawValue = picker.SelectedItem?.ToString() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(rawValue) ||
            string.Equals(rawValue, NoneChoice, StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        // The cost is encoded as the first token in parentheses, e.g. "(3)".
        var match = Regex.Match(rawValue, @"^\s*\(([-+]?\d+)\)");
        if (!match.Success)
        {
            return 0;
        }

        return int.TryParse(match.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : 0;
    }

    /// <summary>
    /// Builds the display text for a profile section (ranged weapons, skills, etc.) by combining
    /// the unit's base text with the list of chosen additions.
    /// </summary>
    /// <param name="prependPlus">When <c>true</c>, additions are prefixed with "+ " to distinguish them from base entries.</param>
    private static string BuildUpdatedProfileSection(string? baseText, IReadOnlyList<string> additions, bool prependPlus)
    {
        var lines = SplitProfileText(baseText);
        foreach (var addition in additions)
        {
            lines.Add(prependPlus ? $"+ {addition}" : addition);
        }

        return lines.Count == 0 ? "-" : string.Join(Environment.NewLine, lines);
    }

    /// <summary>
    /// Splits multi-line profile text into a trimmed, non-empty list of lines,
    /// discarding bare dash placeholders that indicate an empty field.
    /// </summary>
    private static List<string> SplitProfileText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        return text
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x) && x != "-")
            .ToList();
    }

}


