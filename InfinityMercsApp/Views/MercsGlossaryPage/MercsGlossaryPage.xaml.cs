using System.Collections.ObjectModel;
using System.Net;
using System.Text.Json;
using Markdig;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using Svg.Skia;

namespace InfinityMercsApp.Views;

public partial class MercsGlossaryPage : ContentPage
{
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
            if (_selectedItem == value)
            {
                return;
            }

            _selectedItem = value;
            OnPropertyChanged();

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
        _ = LoadGlossaryAsync();
        _ = LoadDeltaIconAsync();
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
            foreach (var entry in payload?.Entries ?? [])
            {
                if (string.IsNullOrWhiteSpace(entry.Title))
                {
                    continue;
                }

                _glossaryItems.Add(new GlossaryEntryItem
                {
                    Title = entry.Title.Trim(),
                    MarkdownFile = entry.MarkdownFile?.Trim() ?? string.Empty,
                    HasDelta = entry.HasDelta
                });
            }

            SelectedItem = _glossaryItems.FirstOrDefault();
        }
        catch (Exception ex)
        {
            SelectedTitle = "Load Error";
            SelectedContentHtml = BuildHtmlFromMarkdown($"Failed to load glossary data: {ex.Message}");
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
    }

    private sealed class GlossaryPayloadEntry
    {
        public string Title { get; init; } = string.Empty;
        public string MarkdownFile { get; init; } = string.Empty;
        public bool HasDelta { get; init; }
    }

    public sealed class GlossaryEntryItem
    {
        public string Title { get; init; } = string.Empty;
        public string MarkdownFile { get; init; } = string.Empty;
        public bool HasDelta { get; init; }
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
            background: #1e1e1e;
            color: #f3f4f6;
            font-family: Segoe UI, Arial, sans-serif;
            font-size: 18px;
            line-height: 1.4;
        }
        body {
            padding: 6px;
        }
        h1, h2, h3, h4, h5, h6 {
            color: #f9fafb;
            margin-top: 0.6em;
            margin-bottom: 0.3em;
        }
        p, ul, ol, pre, blockquote {
            margin-top: 0.25em;
            margin-bottom: 0.7em;
        }
        code, pre {
            font-family: Consolas, "Courier New", monospace;
            background: #111827;
            color: #e5e7eb;
        }
        pre {
            padding: 0.4em;
            border-radius: 4px;
            overflow-x: auto;
        }
        a {
            color: #60a5fa;
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
