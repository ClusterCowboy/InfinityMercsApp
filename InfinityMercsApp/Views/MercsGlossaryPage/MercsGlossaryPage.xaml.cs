using System.Collections.ObjectModel;
using System.Net;
using System.Text.Json;
using Markdig;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using Svg.Skia;
using InfinityMercsApp.Views.Adaptive;

namespace InfinityMercsApp.Views;

public partial class MercsGlossaryPage : AdaptiveContentPage
{
    private bool _showDetail;

    /// <summary>
    /// Rules-index list text size, scaled to the available width so long titles stay readable and
    /// fit inside the index box. Compact has the full screen width; the medium+ rail is narrow.
    /// </summary>
    public double ListFontSize => LayoutMode switch
    {
        AdaptiveLayoutMode.Compact => 17d,
        AdaptiveLayoutMode.Medium => 14d,
        AdaptiveLayoutMode.Expanded => 15d,
        _ => 16d
    };

    private SKPicture? _deltaIconPicture;
    private readonly ObservableCollection<GlossaryEntryItem> _glossaryItems = [];
    private GlossaryEntryItem? _selectedItem;
    private string _selectedTitle = "Select a rule";
    private string _selectedContentHtml = BuildHtmlFromMarkdown("Choose an entry from the Rules Index to view its details.");
    private readonly MarkdownPipeline _markdownPipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
    private int _selectionVersion;

    public ObservableCollection<GlossaryEntryItem> GlossaryItems => _glossaryItems;

    public GlossaryEntryItem? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (value?.IsHeader == true)
            {
                OnPropertyChanged();
                return;
            }

            if (_selectedItem == value)
            {
                return;
            }

            _selectedItem = value;
            OnPropertyChanged();

            // In compact mode, choosing a rule reveals the detail pane over the index.
            _showDetail = _selectedItem is not null;
            ApplyLayout();

            if (_selectedItem is null)
            {
                SelectedTitle = "Select a rule";
                SelectedContentHtml = BuildHtmlFromMarkdown("Choose an entry from the Rules Index to view its details.");
                return;
            }

            SelectedTitle = _selectedItem.Title;
            var currentVersion = ++_selectionVersion;
            _ = LoadSelectedContentAsync(_selectedItem, currentVersion);
        }
    }

    public string SelectedTitle
    {
        get => _selectedTitle;
        private set
        {
            if (_selectedTitle == value)
            {
                return;
            }

            _selectedTitle = value;
            OnPropertyChanged();
        }
    }

    public string SelectedContentHtml
    {
        get => _selectedContentHtml;
        private set
        {
            if (_selectedContentHtml == value)
            {
                return;
            }

            _selectedContentHtml = value;
            OnPropertyChanged();
            UpdateWebViewSource();
        }
    }

    public MercsGlossaryPage()
    {
        InitializeComponent();
        BindingContext = this;
        UpdateWebViewSource();
        ApplyLayout();
        _ = LoadGlossaryAsync();
        _ = LoadDeltaIconAsync();
    }

    protected override void OnLayoutModeChanged(AdaptiveLayoutMode mode) => ApplyLayout();

    private void ApplyLayout()
    {
        OnPropertyChanged(nameof(ListFontSize));

        if (IsCompact)
        {
            RootGrid.ColumnDefinitions = [new ColumnDefinition(GridLength.Star)];
            RootGrid.ColumnSpacing = 0;
            Grid.SetColumn(IndexPane, 0);
            Grid.SetColumn(DetailPane, 0);
            IndexPane.IsVisible = !_showDetail;
            DetailPane.IsVisible = _showDetail;
            DetailBackButton.IsVisible = true;
        }
        else
        {
            var indexWidth = LayoutMode switch
            {
                AdaptiveLayoutMode.Medium => 300d,
                AdaptiveLayoutMode.Expanded => 320d,
                _ => 340d
            };

            RootGrid.ColumnDefinitions =
            [
                new ColumnDefinition(new GridLength(indexWidth)),
                new ColumnDefinition(GridLength.Star)
            ];
            RootGrid.ColumnSpacing = 16;
            Grid.SetColumn(IndexPane, 0);
            Grid.SetColumn(DetailPane, 1);
            IndexPane.IsVisible = true;
            DetailPane.IsVisible = true;
            DetailBackButton.IsVisible = false;
        }

        // Keep rules text at a comfortable reading width on the largest screens. The WebView must
        // stay Fill — it has no intrinsic content width, so centering collapses it to zero and the
        // text disappears. Cap the width via MaximumWidthRequest instead.
        GlossaryWebView.HorizontalOptions = LayoutOptions.Fill;
        GlossaryWebView.MaximumWidthRequest = IsWide ? 860d : double.PositiveInfinity;
    }

    private void OnDetailBackClicked(object? sender, EventArgs e)
    {
        _showDetail = false;
        ApplyLayout();
    }

    private async Task LoadGlossaryAsync()
    {
        try
        {
            await using var stream = await FileSystem.Current.OpenAppPackageFileAsync("mercs_glossary.json");
            var payload = await JsonSerializer.DeserializeAsync<GlossaryPayload>(stream, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            _glossaryItems.Clear();
            AddGlossaryItems(payload);

            // Preselect the first rule so the detail pane has content, but keep compact mode on the
            // index list — the user opens a rule explicitly.
            SelectedItem = _glossaryItems.FirstOrDefault(item => !item.IsHeader);
            _showDetail = false;
            ApplyLayout();
        }
        catch (Exception ex)
        {
            SelectedTitle = "Load Error";
            SelectedContentHtml = BuildHtmlFromMarkdown($"Failed to load glossary data: {ex.Message}");
        }
    }

    private void AddGlossaryItems(GlossaryPayload? payload)
    {
        if (payload?.Sections.Count > 0)
        {
            foreach (var section in payload.Sections)
            {
                AddHeader(section.HeaderOrTitle);
                AddEntries(section.Entries);
            }

            return;
        }

        string? lastSectionHeader = null;
        foreach (var entry in payload?.Entries ?? [])
        {
            var sectionHeader = entry.SectionHeaderOrHeader;
            if (!string.IsNullOrWhiteSpace(sectionHeader) &&
                !string.Equals(sectionHeader, lastSectionHeader, StringComparison.Ordinal))
            {
                AddHeader(sectionHeader);
                lastSectionHeader = sectionHeader;
            }

            AddEntry(entry);
        }

        void AddHeader(string? header)
        {
            if (string.IsNullOrWhiteSpace(header))
            {
                return;
            }

            _glossaryItems.Add(new GlossaryEntryItem
            {
                Title = header.Trim(),
                IsHeader = true
            });
        }

        void AddEntries(IEnumerable<GlossaryPayloadEntry> entries)
        {
            foreach (var entry in entries)
            {
                AddEntry(entry);
            }
        }

        void AddEntry(GlossaryPayloadEntry entry)
        {
            if (string.IsNullOrWhiteSpace(entry.Title))
            {
                return;
            }

            _glossaryItems.Add(new GlossaryEntryItem
            {
                Title = entry.Title.Trim(),
                MarkdownFile = entry.MarkdownFile?.Trim() ?? string.Empty,
                HasDelta = entry.HasDelta
            });
        }
    }

    private async Task LoadSelectedContentAsync(GlossaryEntryItem entry, int version)
    {
        var fallback = "No details available for this rule yet.";
        if (string.IsNullOrWhiteSpace(entry.MarkdownFile))
        {
            if (version == _selectionVersion)
            {
                SelectedContentHtml = BuildHtmlFromMarkdown(fallback);
            }

            return;
        }

        var relativePath = $"glossary/{entry.MarkdownFile}";

        try
        {
            await using var stream = await FileSystem.Current.OpenAppPackageFileAsync(relativePath);
            using var reader = new StreamReader(stream);
            var markdown = await reader.ReadToEndAsync();
            var safeMarkdown = string.IsNullOrWhiteSpace(markdown) ? fallback : markdown;
            var htmlBody = Markdown.ToHtml(safeMarkdown, _markdownPipeline);
            if (version == _selectionVersion)
            {
                SelectedContentHtml = WrapHtmlDocument(htmlBody);
            }
        }
        catch (Exception ex)
        {
            var errorMessage = $"Failed to load glossary markdown file '{entry.MarkdownFile}': {ex.Message}";
            if (version == _selectionVersion)
            {
                SelectedContentHtml = BuildHtmlFromMarkdown(errorMessage);
            }
        }
    }

    private async Task LoadDeltaIconAsync()
    {
        _deltaIconPicture?.Dispose();
        _deltaIconPicture = null;

        try
        {
            await using var stream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/NonCBIcons/noun-delta.svg");
            var svg = new SKSvg();
            _deltaIconPicture = svg.Load(stream);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"MercsGlossaryPage delta icon load failed: {ex.Message}");
        }

        LegendDeltaCanvas.InvalidateSurface();
    }

    private void UpdateWebViewSource()
    {
        if (GlossaryWebView is null)
        {
            return;
        }

        MainThread.BeginInvokeOnMainThread(() =>
        {
            GlossaryWebView.Source = new HtmlWebViewSource
            {
                Html = _selectedContentHtml
            };
        });
    }

    private void OnLegendDeltaCanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        DrawDeltaIcon(e);
    }

    private void OnListDeltaCanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        DrawDeltaIcon(e);
    }

    private void DrawDeltaIcon(SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        if (_deltaIconPicture is null)
        {
            return;
        }

        var bounds = _deltaIconPicture.CullRect;
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        var destination = new SKRect(0, 0, e.Info.Width, e.Info.Height);
        var scale = Math.Min(destination.Width / bounds.Width, destination.Height / bounds.Height);
        var drawnWidth = bounds.Width * scale;
        var drawnHeight = bounds.Height * scale;
        var translateX = destination.Left + ((destination.Width - drawnWidth) / 2f) - (bounds.Left * scale);
        var translateY = destination.Top + ((destination.Height - drawnHeight) / 2f) - (bounds.Top * scale);

        using var restore = new SKAutoCanvasRestore(canvas, true);
        canvas.Translate(translateX, translateY);
        canvas.Scale(scale);
        canvas.DrawPicture(_deltaIconPicture);
    }

    private sealed class GlossaryPayload
    {
        public List<GlossaryPayloadEntry> Entries { get; init; } = [];
        public List<GlossaryPayloadSection> Sections { get; init; } = [];
    }

    private sealed class GlossaryPayloadSection
    {
        public string Header { get; init; } = string.Empty;
        public string Title { get; init; } = string.Empty;
        public List<GlossaryPayloadEntry> Entries { get; init; } = [];

        public string HeaderOrTitle => string.IsNullOrWhiteSpace(Header) ? Title : Header;
    }

    private sealed class GlossaryPayloadEntry
    {
        public string Title { get; init; } = string.Empty;
        public string MarkdownFile { get; init; } = string.Empty;
        public bool HasDelta { get; init; }
        public string SectionHeader { get; init; } = string.Empty;
        public string Header { get; init; } = string.Empty;

        public string SectionHeaderOrHeader => string.IsNullOrWhiteSpace(SectionHeader) ? Header : SectionHeader;
    }

    public sealed class GlossaryEntryItem
    {
        public string Title { get; init; } = string.Empty;
        public string MarkdownFile { get; init; } = string.Empty;
        public bool HasDelta { get; init; }
        public bool IsHeader { get; init; }
        public bool ShowDeltaIcon => !IsHeader && HasDelta;
    }

    private static string BuildHtmlFromMarkdown(string markdown)
    {
        var encoded = WebUtility.HtmlEncode(markdown ?? string.Empty);
        var htmlBody = $"<p>{encoded.Replace("\n", "<br />")}</p>";
        return WrapHtmlDocument(htmlBody);
    }

    private static string WrapHtmlDocument(string htmlBody)
    {
        return $$"""
<!DOCTYPE html>
<html>
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <style>
        html, body {
            margin: 0;
            padding: 0;
            background: #0E1116;
            color: #E6EBF2;
            font-family: Segoe UI, Arial, sans-serif;
            font-size: 18px;
            line-height: 1.4;
        }
        body {
            padding: 6px;
        }
        h1, h2, h3, h4, h5, h6 {
            color: #E6EBF2;
            margin-top: 0.6em;
            margin-bottom: 0.3em;
        }
        p, ul, ol, pre, blockquote {
            margin-top: 0.25em;
            margin-bottom: 0.7em;
        }
        code, pre {
            font-family: Consolas, "Courier New", monospace;
            background: #161B22;
            color: #B5C0CE;
        }
        pre {
            padding: 0.4em;
            border-radius: 4px;
            overflow-x: auto;
        }
        a {
            color: #34D399;
        }
    </style>
</head>
<body>
{{htmlBody}}
</body>
</html>
""";
    }
}
