using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Maui.Devices;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using Svg.Skia;

namespace InfinityMercsApp.Views.Templates.Captain;

public sealed class ConfigureCaptainPopupPage : ContentPage
{
    private const double UnifiedPickerWidth = 280;
    private static readonly Color ModifiedStatColor = Color.FromArgb("#22C55E");
    private static readonly Color DefaultStatColor = Colors.White;
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
    private readonly TaskCompletionSource<SavedImprovedCaptainStats?> _resultSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly SKCanvasView _logoCanvas;
    private readonly Picker _ccPicker;
    private readonly Picker _bsPicker;
    private readonly Picker _phPicker;
    private readonly Picker _wipPicker;
    private readonly Picker _armPicker;
    private readonly Picker _btsPicker;
    private readonly Picker _vitaPicker;
    private readonly Picker _weapon1Picker;
    private readonly Picker _weapon2Picker;
    private readonly Picker _weapon3Picker;
    private readonly Picker _skill1Picker;
    private readonly Picker _skill2Picker;
    private readonly Picker _skill3Picker;
    private readonly Picker _equipment1Picker;
    private readonly Picker _equipment2Picker;
    private readonly Picker _equipment3Picker;
    private readonly Label _rangedValueLabel;
    private readonly Label _ccValueLabel;
    private readonly Label _skillsValueLabel;
    private readonly Label _equipmentValueLabel;
    private readonly Label _upgradeOptionsHeaderLabel;
    private readonly Label _experienceRemainingLabel;
    private readonly Button _foundCompanyButton;
    private readonly IReadOnlyDictionary<string, int> _baseStats;
    private readonly Grid _statsGrid;
    private readonly List<string> _statGridOrder = [];
    private readonly Dictionary<string, string> _statGridBaseValues = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Label> _statGridValueLabels = new(StringComparer.OrdinalIgnoreCase);
    private readonly Entry _captainNameEntry;
    private readonly Label _captainNameHeadingLabel;
    private readonly SKCanvasView _editCaptainNameCanvas;
    private readonly SKCanvasView _saveCaptainNameCanvas;
    private readonly SKCanvasView _rejectCaptainNameCanvas;
    private string _captainNameCommitted = "Captain";
    private SKPicture? _logoPicture;
    private SKPicture? _editCaptainNamePicture;
    private SKPicture? _saveCaptainNamePicture;
    private SKPicture? _rejectCaptainNamePicture;
    private int _isClosing;

    private ConfigureCaptainPopupPage(CaptainUpgradePopupContext context)
    {
        _context = context;
        _baseStats = ParseBaseStats(context.Unit.Statline);
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

    public static async Task<SavedImprovedCaptainStats?> ShowAsync(INavigation navigation, CaptainUpgradePopupContext context)
    {
        var page = new ConfigureCaptainPopupPage(context);
        await navigation.PushModalAsync(page, false);
        return await page._resultSource.Task;
    }

    protected override bool OnBackButtonPressed()
    {
        _ = CloseAsync(false);
        return true;
    }

    private async Task LoadLogoAsync()
    {
        _logoPicture?.Dispose();
        _logoPicture = null;

        try
        {
            Stream? stream = null;
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

        var scale = Math.Min(e.Info.Width / bounds.Width, e.Info.Height / bounds.Height);
        var x = (e.Info.Width - (bounds.Width * scale)) / 2f;
        var y = (e.Info.Height - (bounds.Height * scale)) / 2f;

        canvas.Translate(x, y);
        canvas.Scale(scale);
        canvas.DrawPicture(_logoPicture);
    }

    private async Task CloseAsync(bool confirmed)
    {
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
            Console.Error.WriteLine($"ConfigureCaptainPopupPage DismissModalIfTopAsync ignored ambiguous route pop: {ex.Message}");
        }
    }

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

    private static View BuildStatRow(string label, Picker picker)
    {
        return picker;
    }

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

    private static int ReadStatTier(Picker picker)
    {
        return picker.SelectedItem is StatPickerOption option ? option.Tier : 0;
    }

    private static int ReadStatBonus(Picker picker)
    {
        return picker.SelectedItem is StatPickerOption option ? option.Bonus : 0;
    }

    private static string ReadChoice(Picker picker)
    {
        var value = picker.SelectedItem?.ToString() ?? string.Empty;
        if (string.Equals(value, NoneChoice, StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        var normalized = Regex.Replace(value, @"^\s*\([-+]?\d+\)\s*-\s*", string.Empty).Trim();
        return normalized;
    }

    private static string NormalizeText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "-" : value;
    }

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

    private void HookSelectionChanged(Picker picker)
    {
        picker.SelectedIndexChanged += (_, _) => UpdateProfilePreviewFromSelections();
    }

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

    private void UpdateUpgradeOptionsHeader()
    {
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
        _experienceRemainingLabel.TextColor = experienceRemaining < 0 ? Colors.Red : Colors.White;
        _foundCompanyButton.IsEnabled = experienceRemaining >= 0;
        _foundCompanyButton.BackgroundColor = experienceRemaining < 0 ? Color.FromArgb("#6B7280") : Color.FromArgb("#7C3AED");
    }

    private void UpdateStatlinePreview()
    {
        UpdateStatsGridValues();
    }

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

    private static List<StatPickerOption> BuildStatOptions(string statName, int baseValue)
    {
        if (!StatDefinitions.TryGetValue(statName, out var definition))
        {
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
            if (definition.HardCap.HasValue && currentValue >= definition.HardCap.Value)
            {
                break;
            }

            var targetValue = baseValue + definition.BonusesByTier[tier];
            if (definition.HardCap.HasValue)
            {
                targetValue = Math.Min(targetValue, definition.HardCap.Value);
            }

            var appliedBonus = Math.Max(0, targetValue - baseValue);
            cumulativeCost += definition.CostsByTier[tier];
            options.Add(new StatPickerOption(statName, tier, appliedBonus, cumulativeCost));
            currentValue = targetValue;
        }

        return options;
    }

    private static int ReadStatPoints(Picker picker)
    {
        return picker.SelectedItem is StatPickerOption option ? option.Cost : 0;
    }

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
                valueLabel.TextColor = modifiedValue == numericBase ? DefaultStatColor : ModifiedStatColor;
            }
            else
            {
                valueLabel.Text = rawValue;
                valueLabel.TextColor = DefaultStatColor;
            }
        }
    }

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

    private static List<string> GetSelectedChoices(params Picker[] pickers)
    {
        return pickers
            .Select(ReadChoice)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

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

    private void OnRejectCaptainNameTapped(object? sender, TappedEventArgs e)
    {
        _captainNameEntry.Text = _captainNameCommitted;
        SetCaptainNameEditMode(isEditing: false);
    }

    private void CommitCaptainNameFromEntry()
    {
        var normalized = string.IsNullOrWhiteSpace(_captainNameEntry.Text) ? "Captain" : _captainNameEntry.Text.Trim();
        _captainNameCommitted = normalized;
        _captainNameEntry.Text = _captainNameCommitted;
        _captainNameHeadingLabel.Text = _captainNameCommitted;
        SetCaptainNameEditMode(isEditing: false);
    }

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

    private void DisposeCaptainNameActionIcons()
    {
        _editCaptainNamePicture?.Dispose();
        _editCaptainNamePicture = null;
        _saveCaptainNamePicture?.Dispose();
        _saveCaptainNamePicture = null;
        _rejectCaptainNamePicture?.Dispose();
        _rejectCaptainNamePicture = null;
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

    private static int ReadChoicePoints(Picker picker)
    {
        var rawValue = picker.SelectedItem?.ToString() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(rawValue) ||
            string.Equals(rawValue, NoneChoice, StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        var match = Regex.Match(rawValue, @"^\s*\(([-+]?\d+)\)");
        if (!match.Success)
        {
            return 0;
        }

        return int.TryParse(match.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : 0;
    }

    private static string BuildUpdatedProfileSection(string? baseText, IReadOnlyList<string> additions, bool prependPlus)
    {
        var lines = SplitProfileText(baseText);
        foreach (var addition in additions)
        {
            lines.Add(prependPlus ? $"+ {addition}" : addition);
        }

        return lines.Count == 0 ? "-" : string.Join(Environment.NewLine, lines);
    }

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

