using System.Collections.ObjectModel;

namespace InfinityMercsApp.Views;

public partial class PerksTablesPage : ContentPage
{
    private readonly Dictionary<(int Track, int Tier), Border> _perkCellByTrackTier = [];
    private readonly List<DependencyLink> _dependencyLinks = [];
    private readonly DependencyLinesDrawable _dependencyLinesDrawable = new();
    private string? _selectedPerkTableName;

    public ObservableCollection<string> PerkTableNames { get; } = [];

    public string? SelectedPerkTableName
    {
        get => _selectedPerkTableName;
        set
        {
            if (string.Equals(_selectedPerkTableName, value, StringComparison.Ordinal))
            {
                return;
            }

            _selectedPerkTableName = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedPerkTableLabel));
            BuildPerksTableGrid();
            ScheduleOverlayRefresh();
        }
    }

    public string SelectedPerkTableLabel =>
        string.IsNullOrWhiteSpace(SelectedPerkTableName)
            ? "Choose a table to view."
            : $"Selected table: {SelectedPerkTableName}";

    public PerksTablesPage()
    {
        InitializeComponent();
        BindingContext = this;
        DependencyLinesView.Drawable = _dependencyLinesDrawable;
        PerksTableGrid.SizeChanged += OnPerksTableGridSizeChanged;
        DependencyLinesView.SizeChanged += OnDependencyLinesViewSizeChanged;
        PerksGridHost.SizeChanged += OnPerksGridHostSizeChanged;

        var names = CompanyPerkCatalog
            .GetPerkNodeLists()
            .Select(list => list.ListName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();
        foreach (var name in names)
        {
            PerkTableNames.Add(name);
        }

        SelectedPerkTableName = PerkTableNames.FirstOrDefault();
    }

    private void BuildPerksTableGrid()
    {
        _perkCellByTrackTier.Clear();
        _dependencyLinks.Clear();
        PerksTableGrid.Children.Clear();
        PerksTableGrid.RowDefinitions.Clear();
        PerksTableGrid.ColumnDefinitions.Clear();

        if (string.IsNullOrWhiteSpace(SelectedPerkTableName))
        {
            UpdateLineOverlay();
            return;
        }

        var trees = CompanyPerkCatalog.GetPerkTrees(SelectedPerkTableName)
            .OrderBy(x => x.TrackNumber)
            .ToList();
        if (trees.Count == 0)
        {
            UpdateLineOverlay();
            return;
        }

        var maxTier = trees
            .SelectMany(tree => FlattenNodes(tree.Roots))
            .Select(node => node.Tier)
            .DefaultIfEmpty(1)
            .Max();

        const double headerRowHeight = 56;
        const double rollColumnWidth = 110;
        var displayedTierCount = Math.Max(5, maxTier);

        PerksTableGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = rollColumnWidth });
        for (var tier = 1; tier <= maxTier; tier++)
        {
            PerksTableGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
        }
        for (var tier = maxTier + 1; tier <= displayedTierCount; tier++)
        {
            PerksTableGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
        }

        PerksTableGrid.RowDefinitions.Add(new RowDefinition { Height = headerRowHeight });
        foreach (var _ in trees)
        {
            PerksTableGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });
        }

        var topLeft = BuildHeaderCell("Roll");
        Grid.SetRow(topLeft, 0);
        Grid.SetColumn(topLeft, 0);
        PerksTableGrid.Children.Add(topLeft);

        for (var tier = 1; tier <= displayedTierCount; tier++)
        {
            var tierHeader = BuildHeaderCell($"Tier {tier}");
            Grid.SetRow(tierHeader, 0);
            Grid.SetColumn(tierHeader, tier);
            PerksTableGrid.Children.Add(tierHeader);
        }

        for (var i = 0; i < trees.Count; i++)
        {
            var track = trees[i];
            var rollLabel = BuildRollRangesLabel(track.RollRanges);
            var rollCell = BuildTierCell(rollLabel);
            Grid.SetRow(rollCell, i + 1);
            Grid.SetColumn(rollCell, 0);
            PerksTableGrid.Children.Add(rollCell);
        }

        for (var i = 0; i < trees.Count; i++)
        {
            var track = trees[i];
            var nodesByTier = FlattenNodes(track.Roots)
                .GroupBy(node => node.Tier)
                .ToDictionary(
                    group => group.Key,
                    group => string.Join(
                        Environment.NewLine,
                        group.Select(node => FormatPerkDisplayText(node.PerkText))));

            for (var tier = 1; tier <= displayedTierCount; tier++)
            {
                nodesByTier.TryGetValue(tier, out var perksText);
                var cell = BuildPerkCell(string.IsNullOrWhiteSpace(perksText) ? string.Empty : perksText);
                Grid.SetRow(cell, i + 1);
                Grid.SetColumn(cell, tier);
                PerksTableGrid.Children.Add(cell);
                if (tier <= maxTier)
                {
                    _perkCellByTrackTier[(track.TrackNumber, tier)] = cell;
                }
            }

            foreach (var node in FlattenNodes(track.Roots))
            {
                if (!node.RequiredTier.HasValue)
                {
                    continue;
                }

                _dependencyLinks.Add(new DependencyLink
                {
                    Track = track.TrackNumber,
                    FromTier = node.RequiredTier.Value,
                    ToTier = node.Tier
                });
            }
        }

        ApplyResponsiveGridSize();
        UpdateLineOverlay();
        ScheduleOverlayRefresh();
    }

    private static string BuildRollRangesLabel(IReadOnlyList<CompanyPerkRollRange> ranges)
    {
        if (ranges.Count == 0)
        {
            return "Always";
        }

        return string.Join("/", ranges.Select(range => range.ToString()));
    }

    private static string FormatPerkDisplayText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var parts = text
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return parts.Length <= 1
            ? text.Replace(",", string.Empty).Trim()
            : string.Join(Environment.NewLine, parts);
    }

    private static IEnumerable<CompanyPerkTreeNode> FlattenNodes(IEnumerable<CompanyPerkTreeNode> roots)
    {
        foreach (var root in roots)
        {
            yield return root;

            foreach (var child in FlattenNodes(root.Children))
            {
                yield return child;
            }
        }
    }

    private static Border BuildHeaderCell(string text)
    {
        return new Border
        {
            StrokeThickness = 1,
            BackgroundColor = Color.FromArgb("#1F2937"),
            Padding = 8,
            Content = new Label
            {
                Text = text,
                FontAttributes = FontAttributes.Bold,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center,
                TextColor = Colors.White
            }
        };
    }

    private static Border BuildTierCell(string text)
    {
        return new Border
        {
            StrokeThickness = 1,
            BackgroundColor = Color.FromArgb("#111827"),
            Padding = new Thickness(8, 10),
            MinimumWidthRequest = 70,
            Content = new Label
            {
                Text = text,
                FontAttributes = FontAttributes.Bold,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center,
                TextColor = Colors.White
            }
        };
    }

    private static Border BuildPerkCell(string text)
    {
        return new Border
        {
            StrokeThickness = 1,
            BackgroundColor = Color.FromArgb("#0B1220"),
            Padding = new Thickness(10, 8),
            HorizontalOptions = LayoutOptions.Fill,
            Content = new Label
            {
                Text = text,
                FontSize = 14,
                LineBreakMode = LineBreakMode.WordWrap,
                VerticalTextAlignment = TextAlignment.Center,
                HorizontalTextAlignment = TextAlignment.Center,
                TextColor = Colors.White
            }
        };
    }

    private void OnPerksTableGridSizeChanged(object? sender, EventArgs e)
    {
        UpdateLineOverlay();
        ScheduleOverlayRefresh();
    }

    private void OnDependencyLinesViewSizeChanged(object? sender, EventArgs e)
    {
        UpdateLineOverlay();
        ScheduleOverlayRefresh();
    }

    private void OnPerksGridHostSizeChanged(object? sender, EventArgs e)
    {
        ApplyResponsiveGridSize();
        UpdateLineOverlay();
        ScheduleOverlayRefresh();
    }

    private void ApplyResponsiveGridSize()
    {
        if (PerksGridHost.Width <= 0 || PerksGridHost.Height <= 0)
        {
            return;
        }

        // Force the table to match viewport size so star rows/columns reflow.
        PerksTableGrid.WidthRequest = PerksGridHost.Width;
        PerksTableGrid.HeightRequest = PerksGridHost.Height;
        DependencyLinesView.WidthRequest = PerksGridHost.Width;
        DependencyLinesView.HeightRequest = PerksGridHost.Height;
    }

    private void ScheduleOverlayRefresh()
    {
        if (Dispatcher is null)
        {
            return;
        }

        Dispatcher.Dispatch(UpdateLineOverlay);
        Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(50), UpdateLineOverlay);
        Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(150), UpdateLineOverlay);
    }

    private void UpdateLineOverlay()
    {
        if (PerksTableGrid.Width <= 0 || PerksTableGrid.Height <= 0)
        {
            return;
        }

        ApplyResponsiveGridSize();

        var lines = new List<LineSegment>();
        foreach (var link in _dependencyLinks)
        {
            if (!_perkCellByTrackTier.TryGetValue((link.Track, link.FromTier), out var fromCell))
            {
                continue;
            }

            if (!_perkCellByTrackTier.TryGetValue((link.Track, link.ToTier), out var toCell))
            {
                continue;
            }

            if (fromCell.Width <= 0 || fromCell.Height <= 0 || toCell.Width <= 0 || toCell.Height <= 0)
            {
                continue;
            }

            var parentLeft = fromCell.X;
            var parentRight = fromCell.X + fromCell.Width;
            var childLeft = toCell.X;
            var childRight = toCell.X + toCell.Width;
            var startY = fromCell.Y + (fromCell.Height / 2d);
            var endY = toCell.Y + (toCell.Height / 2d);

            const double inset = 20d;
            double startX;
            double endX;
            if (childLeft >= parentRight)
            {
                // Parent is left of child: draw near each edge with 20px inset.
                startX = parentRight - inset;
                endX = childLeft + inset;
            }
            else if (parentLeft >= childRight)
            {
                // Parent is right of child: mirrored case.
                startX = parentLeft + inset;
                endX = childRight - inset;
            }
            else
            {
                // Overlap fallback.
                startX = fromCell.X + (fromCell.Width / 2d);
                endX = toCell.X + (toCell.Width / 2d);
            }

            lines.Add(new LineSegment(startX, startY, endX, endY));
        }

        _dependencyLinesDrawable.SetLines(lines);
        DependencyLinesView.Invalidate();
    }

    private sealed class DependencyLink
    {
        public int Track { get; init; }
        public int FromTier { get; init; }
        public int ToTier { get; init; }
    }

    private readonly record struct LineSegment(double X1, double Y1, double X2, double Y2);

    private sealed class DependencyLinesDrawable : IDrawable
    {
        private IReadOnlyList<LineSegment> _lines = [];

        public void SetLines(IReadOnlyList<LineSegment> lines)
        {
            _lines = lines;
        }

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            canvas.StrokeColor = Color.FromArgb("#22D3EE");
            canvas.StrokeSize = 2f;

            foreach (var line in _lines)
            {
                canvas.DrawLine((float)line.X1, (float)line.Y1, (float)line.X2, (float)line.Y2);
            }
        }
    }
}
