using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using Svg.Skia;

namespace InfinityMercsApp.Views.Season;

public partial class InjuriesPage : ContentPage, IQueryAttributable
{
    private string _companyFilePath = string.Empty;
    private string _seasonFilePath = string.Empty;
    private bool _built;

    public InjuriesPage()
    {
        InitializeComponent();
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("companyFilePath", out var raw))
            _companyFilePath = Uri.UnescapeDataString(raw?.ToString() ?? string.Empty);
        if (query.TryGetValue("seasonFilePath", out var seasonRaw))
            _seasonFilePath = Uri.UnescapeDataString(seasonRaw?.ToString() ?? string.Empty);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_built) return;
        _built = true;
        await BuildInjuryListAsync();
    }

    private async Task BuildInjuryListAsync()
    {
        InjuryStack.Children.Clear();

        var count = InjuryPageData.PendingInjuries.Count;

        if (count == 0)
        {
            ContinueButton.IsEnabled = true;
            ContinueButton.Opacity = 1.0;
            return;
        }

        var resolved = new bool[count];

        void CheckAllResolved()
        {
            var allDone = resolved.All(r => r);
            ContinueButton.IsEnabled = allDone;
            ContinueButton.Opacity = allDone ? 1.0 : 0.45;
        }

        for (var i = 0; i < count; i++)
        {
            var idx = i;
            var injury = InjuryPageData.PendingInjuries[i];
            void OnResolved() { resolved[idx] = true; CheckAllResolved(); }
            void OnInjured(string injuryLabel)
            {
                var match = ExperiencePageData.Units.FirstOrDefault(u => u.EntryIndex == injury.EntryIndex);
                if (match is null) return;
                match.GainedInjury = true;
                match.InjuryName = injuryLabel;
            }

            InjuryStack.Children.Add(await BuildInjuryRowAsync(injury, OnResolved, OnInjured));
            InjuryStack.Children.Add(new BoxView
            {
                HeightRequest = 1,
                Color = Color.FromArgb("#374151"),
                Margin = new Thickness(0)
            });
        }
    }

    private static readonly (int Min, int Max, string Label)[] InjuryTable =
    [
        (1,  6,  "Battle Fury"),
        (7,  8,  "Punctured Lung"),
        (9,  10, "Arms"),
        (11, 12, "Brain Injury"),
        (13, 14, "Legs Injury"),
        (15, 16, "Body Injury"),
        (17, 18, "Eyes Injury"),
        (19, 20, "Shell Shocked"),
    ];

    private static int InjuryIndexForRoll(int roll) =>
        Array.FindIndex(InjuryTable, e => roll >= e.Min && roll <= e.Max);

    private static void ApplyInjuryFail(Label avoidLabel, Grid passFailRow, Picker injuryPicker,
                                        Button rollBtn, Label rollVirtuallyLabel)
    {
        avoidLabel.Text = "Injury Incurred";
        avoidLabel.TextColor = Color.FromArgb("#DC2626");
        passFailRow.IsVisible = false;
        injuryPicker.IsVisible = true;
        rollBtn.IsEnabled = false;
        rollBtn.Opacity = 0.38;
        rollVirtuallyLabel.TextColor = Color.FromArgb("#6B7280");
    }

    private static async Task<View> BuildInjuryRowAsync(InjuryItem item, Action onResolved, Action<string>? onInjured = null)
    {
        var container = new VerticalStackLayout
        {
            Padding = new Thickness(16, 16, 16, 16),
            Spacing = 10,
            BackgroundColor = Color.FromArgb("#1A2332")
        };

        // Unit header: [icon] [name (star)] [Avoid Injury (right)]
        var logo = await TryLoadLogoAsync(item.CachedLogoPath, item.PackagedLogoPath);

        var logoCanvas = new SKCanvasView
        {
            WidthRequest = 48,
            HeightRequest = 48,
            VerticalOptions = LayoutOptions.Center
        };
        logoCanvas.PaintSurface += (_, e) =>
        {
            e.Surface.Canvas.Clear(SKColors.Transparent);
            if (logo is not null) DrawScaled(e.Surface.Canvas, e.Info, logo);
        };

        var nameLabel = new Label
        {
            Text = item.Name,
            TextColor = Colors.White,
            FontAttributes = FontAttributes.Bold,
            FontSize = 16,
            VerticalTextAlignment = TextAlignment.Center,
            LineBreakMode = LineBreakMode.TailTruncation
        };

        var avoidLabel = new Label
        {
            Text = $"Avoid Injury: {ComputePhMinusThree(item.PhValue)}",
            TextColor = Color.FromArgb("#9CA3AF"),
            FontSize = 14,
            FontAttributes = FontAttributes.Bold,
            VerticalTextAlignment = TextAlignment.Center,
            HorizontalTextAlignment = TextAlignment.End
        };

        var headerGrid = new Grid { ColumnSpacing = 12 };
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(48) });
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(logoCanvas, 0);
        Grid.SetColumn(nameLabel, 1);
        Grid.SetColumn(avoidLabel, 2);
        headerGrid.Children.Add(logoCanvas);
        headerGrid.Children.Add(nameLabel);
        headerGrid.Children.Add(avoidLabel);
        container.Children.Add(headerGrid);

        // 2×2 grid: left column auto-width, right column fills remaining space
        var grid = new Grid
        {
            ColumnSpacing = 16,
            RowSpacing = 8
        };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        // Row 0, Col 0 — "Roll Virtually"
        var rollVirtuallyLabel = new Label
        {
            Text = "Roll Virtually",
            TextColor = Color.FromArgb("#D1D5DB"),
            FontSize = 14,
            VerticalTextAlignment = TextAlignment.Center
        };
        Grid.SetRow(rollVirtuallyLabel, 0);
        Grid.SetColumn(rollVirtuallyLabel, 0);
        grid.Children.Add(rollVirtuallyLabel);

        // Row 0, Col 1 — Roll button + result label stacked vertically
        var resultLabel = new Label
        {
            TextColor = Colors.White,
            FontSize = 13,
            FontAttributes = FontAttributes.Bold,
            HorizontalTextAlignment = TextAlignment.Center,
            IsVisible = false
        };

        var rollBtn = new Button
        {
            Text = "Roll",
            BackgroundColor = Color.FromArgb("#374151"),
            TextColor = Colors.White,
            CornerRadius = 6,
            FontSize = 14,
            HeightRequest = 36,
            Padding = new Thickness(0),
            HorizontalOptions = LayoutOptions.Fill
        };

        var phNum = int.TryParse(item.PhValue, out var ph) ? ph : -1;
        var targetNum = phNum >= 0 ? phNum - 3 : -1;

        // Injury picker — hidden until a FAIL occurs
        var injuryPicker = new Picker
        {
            Title = "Select Injury…",
            TextColor = Colors.White,
            BackgroundColor = Color.FromArgb("#374151"),
            HorizontalOptions = LayoutOptions.Fill,
            IsVisible = false
        };
        injuryPicker.ItemsSource = InjuryTable.Select(e => $"{e.Min}-{e.Max} | {e.Label}").ToList();

        // Row 1, Col 1 — PASS / FAIL buttons, overlaid with the injury picker in the same cell
        var passBtn = new Button
        {
            Text = "PASS",
            BackgroundColor = Color.FromArgb("#374151"),
            TextColor = Color.FromArgb("#22C55E"),
            CornerRadius = 6,
            FontSize = 14,
            FontAttributes = FontAttributes.Bold,
            HeightRequest = 36,
            Padding = new Thickness(0),
            HorizontalOptions = LayoutOptions.Fill
        };

        var failBtn = new Button
        {
            Text = "FAIL",
            BackgroundColor = Color.FromArgb("#374151"),
            TextColor = Color.FromArgb("#DC2626"),
            CornerRadius = 6,
            FontSize = 14,
            FontAttributes = FontAttributes.Bold,
            HeightRequest = 36,
            Padding = new Thickness(0),
            HorizontalOptions = LayoutOptions.Fill
        };

        var passFailRow = new Grid { ColumnSpacing = 8 };
        passFailRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
        passFailRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
        Grid.SetColumn(passBtn, 0);
        Grid.SetColumn(failBtn, 1);
        passFailRow.Children.Add(passBtn);
        passFailRow.Children.Add(failBtn);

        // Container in Row 1 Col 1 holds both the PASS/FAIL row and the picker (toggled by visibility)
        var realityContainer = new VerticalStackLayout { Spacing = 0 };
        realityContainer.Children.Add(passFailRow);
        realityContainer.Children.Add(injuryPicker);
        Grid.SetRow(realityContainer, 1);
        Grid.SetColumn(realityContainer, 1);
        grid.Children.Add(realityContainer);

        // ── Virtual Roll ─────────────────────────────────────────────────────
        rollBtn.Clicked += (_, _) =>
        {
            var roll = Random.Shared.Next(1, 21);
            var passed = targetNum >= 0 && roll <= targetNum;

            resultLabel.Text = targetNum >= 0
                ? $"Rolled: {roll} — {(passed ? "PASS" : "FAIL")}"
                : $"Rolled: {roll}";
            resultLabel.TextColor = passed ? Color.FromArgb("#22C55E") : Color.FromArgb("#DC2626");
            resultLabel.IsVisible = true;

            rollBtn.IsEnabled = false;
            passBtn.IsEnabled = false;
            failBtn.IsEnabled = false;

            if (!passed && targetNum >= 0)
            {
                ApplyInjuryFail(avoidLabel, passFailRow, injuryPicker, rollBtn, rollVirtuallyLabel);
                var injIdx = InjuryIndexForRoll(roll);
                if (injIdx >= 0) injuryPicker.SelectedIndex = injIdx;
                // Auto-selected by the roll, so resolve immediately
                onResolved();
            }
            else
            {
                onResolved();
            }
        };

        // ── Roll In Reality ──────────────────────────────────────────────────
        var realityResolved = false;

        passBtn.Clicked += (_, _) =>
        {
            passBtn.BackgroundColor = Color.FromArgb("#166534");
            failBtn.IsEnabled = false;
            rollBtn.IsEnabled = false;
            rollBtn.Opacity = 0.38;
            rollVirtuallyLabel.TextColor = Color.FromArgb("#6B7280");
            if (!realityResolved) { realityResolved = true; onResolved(); }
        };

        failBtn.Clicked += (_, _) =>
        {
            ApplyInjuryFail(avoidLabel, passFailRow, injuryPicker, rollBtn, rollVirtuallyLabel);
            // onResolved deferred until the user picks an injury from the dropdown
        };

        injuryPicker.SelectedIndexChanged += (_, _) =>
        {
            if (injuryPicker.SelectedIndex >= 0 && !realityResolved)
            {
                realityResolved = true;
                var label = InjuryTable[injuryPicker.SelectedIndex].Label;
                onInjured?.Invoke(label);
                onResolved();
            }
        };

        var rollCol = new VerticalStackLayout { Spacing = 4 };
        rollCol.Children.Add(rollBtn);
        rollCol.Children.Add(resultLabel);
        Grid.SetRow(rollCol, 0);
        Grid.SetColumn(rollCol, 1);
        grid.Children.Add(rollCol);

        // Row 1, Col 0 — "Roll In Reality"
        var rollRealityLabel = new Label
        {
            Text = "Roll In Reality",
            TextColor = Color.FromArgb("#D1D5DB"),
            FontSize = 14,
            VerticalTextAlignment = TextAlignment.Center
        };
        Grid.SetRow(rollRealityLabel, 1);
        Grid.SetColumn(rollRealityLabel, 0);
        grid.Children.Add(rollRealityLabel);

        container.Children.Add(grid);
        return container;
    }

    private static string ComputePhMinusThree(string phValue)
    {
        if (int.TryParse(phValue, out var ph))
            return (ph - 3).ToString();
        return "-";
    }

    private static void DrawScaled(SKCanvas canvas, SKImageInfo info, SKPicture picture)
    {
        var bounds = picture.CullRect;
        if (bounds.Width <= 0 || bounds.Height <= 0) return;
        var scale = Math.Min(info.Width / bounds.Width, info.Height / bounds.Height);
        var x = (info.Width - bounds.Width * scale) / 2f;
        var y = (info.Height - bounds.Height * scale) / 2f;
        canvas.Save();
        canvas.Translate(x, y);
        canvas.Scale(scale);
        canvas.DrawPicture(picture);
        canvas.Restore();
    }

    private static async Task<SKPicture?> TryLoadLogoAsync(string? cachedPath, string? packagedPath)
    {
        if (!string.IsNullOrWhiteSpace(cachedPath) && File.Exists(cachedPath))
        {
            try
            {
                await using var stream = File.OpenRead(cachedPath);
                var svg = new SKSvg();
                return svg.Load(stream);
            }
            catch { }
        }

        foreach (var candidate in new[] { packagedPath, packagedPath?.ToLowerInvariant() })
        {
            if (string.IsNullOrWhiteSpace(candidate)) continue;
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

    private async void OnContinueClicked(object sender, EventArgs e)
    {
        InjuryPageData.PendingInjuries.Clear();
        var encodedPath = Uri.EscapeDataString(_companyFilePath);
        var encodedSeasonPath = Uri.EscapeDataString(_seasonFilePath);
        await Shell.Current.GoToAsync($"{nameof(MissionOutcomePage)}?companyFilePath={encodedPath}&seasonFilePath={encodedSeasonPath}");
    }
}

public class InjuryItem
{
    public int EntryIndex { get; set; }
    public string Name { get; set; } = string.Empty;
    public string PhValue { get; set; } = "-";
    public string? CachedLogoPath { get; set; }
    public string? PackagedLogoPath { get; set; }
}

public static class InjuryPageData
{
    public static List<InjuryItem> PendingInjuries { get; set; } = [];
}
