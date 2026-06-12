using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Services.Season;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using Svg.Skia;

namespace InfinityMercsApp.Views.Season;

public partial class ExperiencePage : ContentPage, IQueryAttributable
{
    private string _companyFilePath = string.Empty;
    private string _seasonFilePath = string.Empty;
    private bool _built;

    public ExperiencePage()
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
        await BuildUnitListAsync();
    }

    private async Task BuildUnitListAsync()
    {
        UnitStack.Children.Clear();

        var units = ExperiencePageData.Units;
        if (units.Count == 0) return;

        // Lock confirm until an MVP is selected.
        ConfirmButton.IsEnabled = false;
        ConfirmButton.Opacity = 0.45;

        var mvpCheckboxes = new List<(CheckBox Cb, ExperienceUnitResult Unit)>();

        void UpdateConfirm()
        {
            var hasMvp = mvpCheckboxes.Any(x => x.Unit.IsMvp);
            ConfirmButton.IsEnabled = hasMvp;
            ConfirmButton.Opacity = hasMvp ? 1.0 : 0.45;
        }

        for (var i = 0; i < units.Count; i++)
        {
            if (i > 0)
            {
                UnitStack.Children.Add(new BoxView
                {
                    HeightRequest = 1,
                    Color = Color.FromArgb("#374151")
                });
            }
            UnitStack.Children.Add(await BuildUnitCardAsync(units[i], mvpCheckboxes, UpdateConfirm));
        }
    }

    private async Task<View> BuildUnitCardAsync(
        ExperienceUnitResult unit,
        List<(CheckBox Cb, ExperienceUnitResult Unit)> mvpCheckboxes,
        Action onMvpChanged)
    {
        var logo = await TryLoadLogoAsync(unit.CachedLogoPath, unit.PackagedLogoPath);

        // Create MVP checkbox before wiring its event
        var mvpCb = new CheckBox
        {
            Color = Color.FromArgb("#F59E0B"),
            HorizontalOptions = LayoutOptions.Center
        };

        // Content column
        var content = new VerticalStackLayout
        {
            Padding = new Thickness(12, 12, 12, 12),
            Spacing = 4
        };

        // Logo + name header
        var logoCanvas = new SKCanvasView
        {
            WidthRequest = 44,
            HeightRequest = 44,
            VerticalOptions = LayoutOptions.Center
        };
        logoCanvas.PaintSurface += (_, e) =>
        {
            e.Surface.Canvas.Clear(SKColors.Transparent);
            if (logo is not null) DrawScaled(e.Surface.Canvas, e.Info, logo);
        };

        var nameLabel = new Label
        {
            Text = unit.Name,
            TextColor = Colors.White,
            FontAttributes = FontAttributes.Bold,
            FontSize = 16,
            VerticalTextAlignment = TextAlignment.Center,
            LineBreakMode = LineBreakMode.TailTruncation
        };

        var headerRow = new HorizontalStackLayout { Spacing = 10, Margin = new Thickness(0, 0, 0, 6) };
        headerRow.Children.Add(logoCanvas);
        headerRow.Children.Add(nameLabel);
        content.Children.Add(headerRow);

        content.Children.Add(new BoxView
        {
            HeightRequest = 1,
            Color = Color.FromArgb("#374151"),
            Margin = new Thickness(0, 0, 0, 2)
        });

        Grid MakeXpRow(string label, int xp, Color? labelColor = null)
        {
            var row = new Grid { ColumnSpacing = 8 };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var lbl = new Label
            {
                Text = label,
                TextColor = labelColor ?? Color.FromArgb("#D1D5DB"),
                FontSize = 13,
                VerticalTextAlignment = TextAlignment.Center
            };
            var xpLbl = new Label
            {
                Text = $"+{xp} XP",
                TextColor = Color.FromArgb("#22C55E"),
                FontSize = 13,
                FontAttributes = FontAttributes.Bold,
                HorizontalTextAlignment = TextAlignment.End
            };
            Grid.SetColumn(xpLbl, 1);
            row.Children.Add(lbl);
            row.Children.Add(xpLbl);
            return row;
        }

        // Fixed XP sources
        content.Children.Add(MakeXpRow($"Deployed (Mission {unit.DeploymentXp})", unit.DeploymentXp));

        if (unit.IsEliteDeployment)
            content.Children.Add(MakeXpRow("Elite Deployment", 3));

        if (unit.IsConsciousAtEnd)
            content.Children.Add(MakeXpRow("Survived", 2));

        if (unit.GainedInjury)
            content.Children.Add(MakeXpRow("Injury Sustained", 2));

        // In-game XP from mission actions
        var xd = unit.XpData;
        var assistCount = xd.Assist.Count(b => b);
        if (assistCount > 0)
            content.Children.Add(MakeXpRow(assistCount == 1 ? "Assisted" : $"Assisted ×{assistCount}", assistCount));

        var statesCount = xd.InflictState.Count(b => b);
        if (statesCount > 0)
            content.Children.Add(MakeXpRow(statesCount == 1 ? "Inflicted State" : $"Inflicted State ×{statesCount}", statesCount));

        if (xd.AttemptButton)
            content.Children.Add(MakeXpRow("Attempted Objective", 1));

        if (xd.SucceedButton)
            content.Children.Add(MakeXpRow("Completed Objective", 1));

        if (xd.ScanEnemy)
            content.Children.Add(MakeXpRow("Scan Enemy", 1));

        if (xd.ScanEnemyFo)
            content.Children.Add(MakeXpRow("Scan Enemy (FO)", 1));

        if (xd.TagAndBag)
            content.Children.Add(MakeXpRow("Tag and Bag", 2));

        // MVP XP row — hidden until checkbox checked
        var mvpXpRow = MakeXpRow("MVP", 2, Color.FromArgb("#F59E0B"));
        mvpXpRow.IsVisible = unit.IsMvp;
        content.Children.Add(mvpXpRow);

        // Divider + running total
        content.Children.Add(new BoxView
        {
            HeightRequest = 1,
            Color = Color.FromArgb("#374151"),
            Margin = new Thickness(0, 4, 0, 0)
        });
        var totalLabel = new Label
        {
            Text = $"Total: {unit.TotalXp} XP",
            TextColor = Color.FromArgb("#22C55E"),
            FontSize = 14,
            FontAttributes = FontAttributes.Bold,
            HorizontalTextAlignment = TextAlignment.End
        };
        content.Children.Add(totalLabel);

        // Wire MVP checkbox now that all views exist
        mvpCheckboxes.Add((mvpCb, unit));
        mvpCb.CheckedChanged += (_, e) =>
        {
            if (e.Value)
            {
                // Enforce radio behavior — uncheck every other unit
                foreach (var (cb, u) in mvpCheckboxes)
                {
                    if (cb == mvpCb) continue;
                    u.IsMvp = false;
                    cb.IsChecked = false;
                }
                unit.IsMvp = true;
            }
            else
            {
                unit.IsMvp = false;
            }
            mvpXpRow.IsVisible = unit.IsMvp;
            totalLabel.Text = $"Total: {unit.TotalXp} XP";
            onMvpChanged();
        };

        // Restore persisted MVP state (usually false on first load)
        if (unit.IsMvp) mvpCb.IsChecked = true;

        // MVP column (left sidebar)
        var mvpCol = new VerticalStackLayout
        {
            Spacing = 6,
            Padding = new Thickness(8, 12, 8, 12),
            BackgroundColor = Color.FromArgb("#111827"),
            VerticalOptions = LayoutOptions.Fill
        };
        mvpCol.Children.Add(new Label
        {
            Text = "MVP",
            TextColor = Color.FromArgb("#F59E0B"),
            FontSize = 11,
            FontAttributes = FontAttributes.Bold,
            HorizontalTextAlignment = TextAlignment.Center
        });
        mvpCol.Children.Add(mvpCb);

        // Assemble card
        var card = new Grid
        {
            ColumnSpacing = 0,
            BackgroundColor = Color.FromArgb("#1F2937")
        };
        card.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) });
        card.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
        Grid.SetColumn(mvpCol, 0);
        Grid.SetColumn(content, 1);
        card.Children.Add(mvpCol);
        card.Children.Add(content);

        return card;
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

    private async void OnConfirmExperienceClicked(object sender, EventArgs e)
    {
        await SeasonFileService.UpdateLatestRoundAsync(_seasonFilePath, round =>
        {
            round.MissionResults.UnitResults = ExperiencePageData.Units
                .Select(u => new SeasonMissionUnitResult
                {
                    UnitName = u.Name,
                    Injury = u.GainedInjury ? (u.InjuryName ?? string.Empty) : null,
                    TriedObjective = u.XpData.AttemptButton,
                    CompletedObjective = u.XpData.SucceedButton,
                    AssistCount = u.XpData.Assist.Count(b => b),
                    StatesInflicted = u.XpData.InflictState.Count(b => b),
                    ScannedEnemy = u.XpData.ScanEnemy,
                    ScannedEnemyWithFO = u.XpData.ScanEnemyFo,
                    TagAndBag = u.XpData.TagAndBag,
                    ConsciousAtEnd = u.IsConsciousAtEnd,
                    IsMvp = u.IsMvp
                })
                .ToList();
        });

        var encodedPath = Uri.EscapeDataString(_companyFilePath);
        var encodedSeasonPath = Uri.EscapeDataString(_seasonFilePath);
        await Shell.Current.GoToAsync($"{nameof(DowntimePage)}?companyFilePath={encodedPath}&seasonFilePath={encodedSeasonPath}");
    }
}

public sealed class ExperienceUnitResult
{
    public int EntryIndex { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? CachedLogoPath { get; set; }
    public string? PackagedLogoPath { get; set; }
    public int DeploymentXp { get; set; }
    public bool IsEliteDeployment { get; set; }
    public bool IsConsciousAtEnd { get; set; }
    public bool GainedInjury { get; set; }
    public string? InjuryName { get; set; }
    public UnitXpData XpData { get; init; } = new();
    public bool IsMvp { get; set; }
    public bool IsCaptain { get; set; }
    public string UnitPh { get; set; } = "-";
    public string UnitBs { get; set; } = "-";
    public string UnitCc { get; set; } = "-";
    public string UnitWip { get; set; } = "-";
    public string UnitArm { get; set; } = "-";

    // Cost paid at purchase (includes the loadout) + accumulated XP.
    public int Renown { get; set; }

    // Free-form text — used by downtime to match requirements like "Hacker", "Trinity Program".
    public string Skills { get; set; } = string.Empty;
    public string Equipment { get; set; } = string.Empty;

    public int TotalXp =>
        DeploymentXp +
        (IsEliteDeployment ? 3 : 0) +
        (IsConsciousAtEnd ? 2 : 0) +
        (GainedInjury ? 2 : 0) +
        XpData.TotalXp +
        (IsMvp ? 2 : 0);
}

public static class ExperiencePageData
{
    public static List<ExperienceUnitResult> Units { get; set; } = [];
}
