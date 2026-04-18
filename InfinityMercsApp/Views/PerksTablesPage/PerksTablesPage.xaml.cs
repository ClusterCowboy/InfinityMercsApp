using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

namespace InfinityMercsApp.Views;

public partial class PerksTablesPage : ContentPage
{
    private readonly Dictionary<(int Track, int Tier), Border> _perkCellByTrackTier = [];
    private readonly List<DependencyLink> _dependencyLinks = [];
    private readonly DependencyLinesDrawable _dependencyLinesDrawable = new();
    private PerkTableOption? _selectedPerkTableOption;

    public ObservableCollection<PerkTableOption> PerkTableOptions { get; } = [];

    public PerkTableOption? SelectedPerkTableOption
    {
        get => _selectedPerkTableOption;
        set
        {
            if (ReferenceEquals(_selectedPerkTableOption, value))
            {
                return;
            }

            _selectedPerkTableOption = value;
            OnPropertyChanged();
            BuildPerksTableGrid();
            ScheduleOverlayRefresh();
        }
    }

    public PerksTablesPage()
    {
        InitializeComponent();
        BindingContext = this;
        DependencyLinesView.Drawable = _dependencyLinesDrawable;
        PerksTableGrid.SizeChanged += OnPerksTableGridSizeChanged;
        DependencyLinesView.SizeChanged += OnDependencyLinesViewSizeChanged;
        PerksGridHost.SizeChanged += OnPerksGridHostSizeChanged;

        var discoveredNames = CompanyPerkCatalog
            .GetPerkNodeLists()
            .Select(list => list.ListName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var preferredOrder = new[]
        {
            "Initiative",
            "Cool",
            "Body",
            "Reflex",
            "Intelligence",
            "Empathy",
            "Mecha"
        };

        var orderedNames = preferredOrder
            .Where(name => discoveredNames.Contains(name, StringComparer.OrdinalIgnoreCase))
            .Concat(discoveredNames.Where(name => !preferredOrder.Contains(name, StringComparer.OrdinalIgnoreCase)))
            .ToList();

        var listDefinitions = CompanyPerkCatalog
            .GetPerkListCatalogEntries()
            .ToDictionary(list => list.Name, StringComparer.OrdinalIgnoreCase);

        foreach (var name in orderedNames)
        {
            listDefinitions.TryGetValue(name, out var definition);
            var rangeText = definition is null || definition.ListRollRanges.Count == 0
                ? "No Roll"
                : BuildRollRangesLabel(definition.ListRollRanges);

            PerkTableOptions.Add(new PerkTableOption
            {
                ListName = name,
                DisplayName = $"{name} ({rangeText})"
            });
        }

        SelectedPerkTableOption = PerkTableOptions.FirstOrDefault();
    }

    private void BuildPerksTableGrid()
    {
        _perkCellByTrackTier.Clear();
        _dependencyLinks.Clear();
        PerksTableGrid.Children.Clear();
        PerksTableGrid.RowDefinitions.Clear();
        PerksTableGrid.ColumnDefinitions.Clear();

        if (_selectedPerkTableOption is null)
        {
            UpdateLineOverlay();
            return;
        }

        var nodeList = CompanyPerkCatalog
            .GetPerkNodeLists()
            .FirstOrDefault(list => string.Equals(
                list.ListName,
                _selectedPerkTableOption.ListName,
                StringComparison.OrdinalIgnoreCase));
        if (nodeList is null)
        {
            UpdateLineOverlay();
            return;
        }

        var allNodes = FlattenNodes(nodeList.Roots)
            .GroupBy(node => node.Id, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();

        var rowsByTrack = new Dictionary<int, List<PerkNode>>();
        foreach (var node in allNodes)
        {
            if (!TryParseTrackTier(node.Id, out var track, out _))
            {
                continue;
            }

            if (!rowsByTrack.TryGetValue(track, out var rowNodes))
            {
                rowNodes = [];
                rowsByTrack[track] = rowNodes;
            }

            rowNodes.Add(node);
        }

        var orderedTrackNumbers = rowsByTrack.Keys.OrderBy(x => x).ToList();
        if (orderedTrackNumbers.Count == 0)
        {
            UpdateLineOverlay();
            return;
        }

        var maxTier = allNodes.Count == 0 ? 1 : allNodes.Max(node => node.Tier);

        const double headerRowHeight = 56;
        const double rollColumnWidth = 110;
        var displayedTierCount = Math.Max(5, maxTier);
        var listDefinition = CompanyPerkCatalog.FindPerkListCatalogEntry(nodeList.ListId) ??
                             CompanyPerkCatalog.FindPerkListCatalogEntry(nodeList.ListName);
        var rollLabelByTrack = listDefinition?.Tracks.ToDictionary(
            track => track.TrackNumber,
            track => BuildRollRangesLabel(track.RollRanges));

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
        foreach (var _ in orderedTrackNumbers)
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

        for (var i = 0; i < orderedTrackNumbers.Count; i++)
        {
            var trackNumber = orderedTrackNumbers[i];
            var rollLabel = rollLabelByTrack is not null &&
                            rollLabelByTrack.TryGetValue(trackNumber, out var trackRollLabel)
                ? trackRollLabel
                : trackNumber.ToString();
            var rollCell = BuildTierCell(rollLabel);
            Grid.SetRow(rollCell, i + 1);
            Grid.SetColumn(rollCell, 0);
            PerksTableGrid.Children.Add(rollCell);
        }

        for (var i = 0; i < orderedTrackNumbers.Count; i++)
        {
            var trackNumber = orderedTrackNumbers[i];
            var nodesByTier = rowsByTrack[trackNumber]
                .GroupBy(node => node.Tier)
                .ToDictionary(
                    group => group.Key,
                    group => string.Join(
                        Environment.NewLine,
                        group.Select(node => FormatPerkDisplayText(node.Name))));

            for (var tier = 1; tier <= displayedTierCount; tier++)
            {
                nodesByTier.TryGetValue(tier, out var perksText);
                var hasText = !string.IsNullOrWhiteSpace(perksText);
                var cell = BuildPerkCell(hasText ? perksText! : string.Empty, hasText);
                Grid.SetRow(cell, i + 1);
                Grid.SetColumn(cell, tier);
                PerksTableGrid.Children.Add(cell);
                if (tier <= maxTier)
                {
                    _perkCellByTrackTier[(trackNumber, tier)] = cell;
                }
            }
        }

        foreach (var node in allNodes)
        {
            if (!TryParseTrackTier(node.Id, out var childTrack, out var childTier))
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(node.ParentId) &&
                TryParseTrackTier(node.ParentId, out var parentTrack, out var parentTier) &&
                parentTrack == childTrack)
            {
                _dependencyLinks.Add(new DependencyLink
                {
                    Track = childTrack,
                    FromTier = parentTier,
                    ToTier = childTier
                });
            }

            foreach (var child in node.Children)
            {
                if (!TryParseTrackTier(child.Id, out var childNodeTrack, out var childNodeTier))
                {
                    continue;
                }

                if (childNodeTrack != childTrack)
                {
                    continue;
                }

                _dependencyLinks.Add(new DependencyLink
                {
                    Track = childTrack,
                    FromTier = childTier,
                    ToTier = childNodeTier
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

    private static IEnumerable<PerkNode> FlattenNodes(IEnumerable<PerkNode> roots)
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

    private static bool TryParseTrackTier(string? nodeId, out int trackNumber, out int tierNumber)
    {
        trackNumber = 0;
        tierNumber = 0;
        if (string.IsNullOrWhiteSpace(nodeId))
        {
            return false;
        }

        var match = Regex.Match(
            nodeId,
            @"-track-(?<track>\d+)-tier-(?<tier>\d+)",
            RegexOptions.IgnoreCase);
        if (!match.Success)
        {
            return false;
        }

        return int.TryParse(match.Groups["track"].Value, out trackNumber) &&
               int.TryParse(match.Groups["tier"].Value, out tierNumber);
    }

    public sealed class PerkTableOption
    {
        public string ListName { get; init; } = string.Empty;
        public string DisplayName { get; init; } = string.Empty;
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

    private static Border BuildPerkCell(string text, bool hasText)
    {
        return new Border
        {
            StrokeThickness = 1,
            BackgroundColor = hasText
                ? Color.FromArgb("#3322C55E")
                : Color.FromArgb("#0B1220"),
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
            canvas.StrokeColor = Color.FromArgb("#22C55E");
            canvas.StrokeSize = 2f;

            foreach (var line in _lines)
            {
                canvas.DrawLine((float)line.X1, (float)line.Y1, (float)line.X2, (float)line.Y2);
            }
        }
    }
}
