using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace InfinityMercsApp.Services;

public sealed class WikiDescriptionService : IWikiDescriptionService
{
    // Priority order for auto-detecting which CSS box to extract.
    // "cssbox" (bare word) matches skills like "cssbox cssbox-greenyellow" but NOT
    // "cssbox-black", "cssbox-title", or "cssbox-type" — those don't have a bare cssbox token.
    // errata_border is intentionally excluded: pages that use it (e.g. BS Attack Guided) also
    // have an introductory paragraph outside the box that would be missed if we only grab the box.
    // The section fallback (StripCollapsedElements + full parse) handles those pages correctly.
    private static readonly string[] AutoDetectClasses = ["cssbox-black", "cssbox"];

    private readonly HttpClient _http;
    private readonly Dictionary<string, IReadOnlyList<WikiContentBlock>> _cache =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly SemaphoreSlim _lock = new(1, 1);

    public WikiDescriptionService(HttpClient http) => _http = http;

    public async Task<IReadOnlyList<WikiContentBlock>> FetchContentAsync(
        string url,
        string? section = null,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{url}\0{section ?? ""}";

        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_cache.TryGetValue(cacheKey, out var cached))
                return cached;

            var html = await _http.GetStringAsync(url, cancellationToken).ConfigureAwait(false);

            if (!string.IsNullOrEmpty(section))
                html = IsolateSectionByText(html, section);

            var blocks = ParsePage(html, AutoDetectClasses);
            _cache[cacheKey] = blocks;
            return blocks;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Wiki fetch failed for '{url}': {ex.Message}");
            return [];
        }
        finally
        {
            _lock.Release();
        }
    }

    // ── Section isolation ─────────────────────────────────────────────────────

    // Finds the first heading whose stripped text contains sectionText (case-insensitive)
    // and returns only the HTML from that heading to the next heading of equal or higher level.
    private static string IsolateSectionByText(string html, string sectionText)
    {
        var headingPattern = new Regex(@"<(h[1-6])\b", RegexOptions.IgnoreCase);

        foreach (Match hm in headingPattern.Matches(html))
        {
            int headingStart = hm.Index;
            int level = int.Parse(hm.Groups[1].Value[1..]);
            var closeTag = $"</h{level}>";

            int closeIdx = html.IndexOf(closeTag, headingStart, StringComparison.OrdinalIgnoreCase);
            if (closeIdx < 0) continue;

            int closeEnd = closeIdx + closeTag.Length;
            var headingText = StripTagsToText(html[headingStart..closeEnd]);

            if (!headingText.Contains(sectionText, StringComparison.OrdinalIgnoreCase))
                continue;

            // Slice to the next heading of same or higher level
            var nextPattern = new Regex($@"<h[1-{level}]\b", RegexOptions.IgnoreCase);
            var next = nextPattern.Match(html, closeEnd);
            int sectionEnd = next.Success ? next.Index : html.Length;

            return html[headingStart..sectionEnd];
        }

        return html; // section not found — fall back to full page
    }

    // ── Block extraction ──────────────────────────────────────────────────────

    // For each CSS class in order, extract the FIRST matching element and parse it.
    // If no box class matches at all, falls back to parsing the section HTML directly —
    // wrapper divs (e.g. rendered {{update}} templates) are transparent to ParseToBlocks
    // since it only handles semantic elements (h2/h3, p, ul/li, b, etc.).
    private static IReadOnlyList<WikiContentBlock> ParsePage(
        string html, IReadOnlyList<string> boxClasses)
    {
        var all = new List<WikiContentBlock>();
        foreach (var className in boxClasses)
        {
            var inner = ExtractFirstBoxByClass(html, className);
            if (inner is not null)
                all.AddRange(ParseToBlocks(inner));
        }

        if (all.Count == 0)
        {
            // Strip mw-collapsed elements (hidden old/legacy rule versions) before
            // parsing so collapsed content doesn't appear in the output.
            var cleaned = StripCollapsedElements(html);
            // Skip the section heading itself — it's already shown as the popup title
            all.AddRange(ParseToBlocks(cleaned)
                .SkipWhile(b => b.Type == WikiBlockType.SectionHeader));
        }

        return all;
    }

    // Returns the inner HTML of the first element on the page that carries the given CSS class.
    private static string? ExtractFirstBoxByClass(string html, string className)
    {
        var classPattern = new Regex(
            $@"class\s*=\s*(?:""[^""]*\b{Regex.Escape(className)}\b[^""]*""|'[^']*\b{Regex.Escape(className)}\b[^']*')",
            RegexOptions.IgnoreCase);

        var classMatch = classPattern.Match(html);
        if (!classMatch.Success) return null;

        int tagStart = html.LastIndexOf('<', classMatch.Index);
        if (tagStart < 0) return null;

        var tagNameMatch = Regex.Match(html[(tagStart + 1)..], @"^\w+");
        if (!tagNameMatch.Success) return null;
        var tagName = tagNameMatch.Value;

        int openEnd = html.IndexOf('>', classMatch.Index);
        if (openEnd < 0 || html[openEnd - 1] == '/') return null; // self-closing

        int depth = 1, pos = openEnd + 1, innerEnd = -1;
        while (depth > 0 && pos < html.Length)
        {
            int nextOpen = FindTag(html, tagName, pos, opening: true);
            int nextClose = FindTag(html, tagName, pos, opening: false);

            if (nextClose < 0) break;
            if (nextOpen >= 0 && nextOpen < nextClose)
            {
                depth++;
                pos = nextOpen + 2 + tagName.Length;
            }
            else
            {
                depth--;
                if (depth == 0) innerEnd = nextClose;
                pos = nextClose + 3 + tagName.Length;
            }
        }

        return innerEnd >= 0 ? html[(openEnd + 1)..innerEnd] : null;
    }

    private static int FindTag(string html, string tagName, int from, bool opening)
    {
        var needle = opening ? $"<{tagName}" : $"</{tagName}>";
        int pos = from;
        while (pos < html.Length)
        {
            int idx = html.IndexOf(needle, pos, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return -1;
            if (opening)
            {
                int after = idx + 1 + tagName.Length;
                if (after < html.Length)
                {
                    char c = html[after];
                    if (c is '>' or ' ' or '\t' or '\r' or '\n' or '/')
                        return idx;
                }
                pos = idx + 1;
            }
            else return idx;
        }
        return -1;
    }

    // ── Block parser ──────────────────────────────────────────────────────────
    // Linear scan: tracks context flags and a text buffer; emits blocks on
    // element boundaries. Handles h2/h3, p, ul/ol/li, dt/dd, b/strong.

    private static IReadOnlyList<WikiContentBlock> ParseToBlocks(string innerHtml)
    {
        var blocks = new List<WikiContentBlock>();
        var buf = new StringBuilder();

        bool inHeader = false, inParagraph = false, inListItem = false;
        bool listItemHasBold = false;
        int listItemIndent = 0, ulDepth = 0;

        int pos = 0;
        while (pos < innerHtml.Length)
        {
            int tagStart = innerHtml.IndexOf('<', pos);
            int textEnd = tagStart >= 0 ? tagStart : innerHtml.Length;

            if (textEnd > pos && (inHeader || inParagraph || inListItem))
                buf.Append(WebUtility.HtmlDecode(innerHtml[pos..textEnd]));

            if (tagStart < 0) break;

            int tagEnd = innerHtml.IndexOf('>', tagStart);
            if (tagEnd < 0) break;

            var tag = innerHtml[(tagStart + 1)..tagEnd];
            bool closing = tag.StartsWith('/');
            var nmatch = Regex.Match(closing ? tag[1..] : tag, @"^\w+");
            if (!nmatch.Success) { pos = tagEnd + 1; continue; }
            var name = nmatch.Value.ToLowerInvariant();

            switch (name)
            {
                case "h2": case "h3": case "h4":
                    if (!closing)
                    {
                        Flush(blocks, buf, inHeader, inParagraph, inListItem, listItemIndent, listItemHasBold);
                        buf.Clear();
                        (inHeader, inParagraph, inListItem) = (true, false, false);
                    }
                    else if (inHeader)
                    {
                        AddBlock(blocks, WikiBlockType.SectionHeader, buf);
                        buf.Clear(); inHeader = false;
                    }
                    break;

                case "p":
                    if (!closing)
                    {
                        Flush(blocks, buf, inHeader, inParagraph, inListItem, listItemIndent, listItemHasBold);
                        buf.Clear();
                        (inHeader, inParagraph, inListItem) = (false, true, false);
                    }
                    else if (inParagraph)
                    {
                        AddBlock(blocks, WikiBlockType.Paragraph, buf);
                        buf.Clear(); inParagraph = false;
                    }
                    break;

                case "ul": case "ol":
                    if (!closing)
                    {
                        if (inListItem && buf.Length > 0)
                        {
                            EmitBullet(blocks, buf, listItemHasBold, listItemIndent);
                            buf.Clear(); inListItem = false;
                        }
                        ulDepth++;
                    }
                    else ulDepth = Math.Max(0, ulDepth - 1);
                    break;

                case "li":
                    if (!closing)
                    {
                        if (inListItem) { EmitBullet(blocks, buf, listItemHasBold, listItemIndent); buf.Clear(); }
                        inListItem = true;
                        listItemIndent = Math.Max(0, ulDepth - 1);
                        listItemHasBold = false;
                    }
                    else if (inListItem)
                    {
                        EmitBullet(blocks, buf, listItemHasBold, listItemIndent);
                        buf.Clear(); inListItem = false;
                    }
                    break;

                case "dt":
                    if (!closing)
                    {
                        Flush(blocks, buf, inHeader, inParagraph, inListItem, listItemIndent, listItemHasBold);
                        buf.Clear();
                        (inListItem, listItemIndent, listItemHasBold) = (true, 0, true);
                    }
                    else if (inListItem) { EmitBullet(blocks, buf, true, 0); buf.Clear(); inListItem = false; }
                    break;

                case "dd":
                    if (!closing)
                    {
                        Flush(blocks, buf, inHeader, inParagraph, inListItem, listItemIndent, listItemHasBold);
                        buf.Clear();
                        (inListItem, listItemIndent, listItemHasBold) = (true, 1, false);
                    }
                    else if (inListItem) { EmitBullet(blocks, buf, false, 1); buf.Clear(); inListItem = false; }
                    break;

                case "b": case "strong":
                    if (!closing && inListItem) listItemHasBold = true;
                    break;

                case "br":
                    if (inHeader || inParagraph || inListItem) buf.Append(' ');
                    break;
            }

            pos = tagEnd + 1;
        }

        Flush(blocks, buf, inHeader, inParagraph, inListItem, listItemIndent, listItemHasBold);
        return blocks;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void Flush(List<WikiContentBlock> blocks, StringBuilder buf,
        bool inHeader, bool inParagraph, bool inListItem, int indent, bool bold)
    {
        var t = Normalize(buf);
        if (t.Length == 0) return;
        if (inHeader) blocks.Add(new WikiContentBlock { Type = WikiBlockType.SectionHeader, Text = t });
        else if (inParagraph) blocks.Add(new WikiContentBlock { Type = WikiBlockType.Paragraph, Text = t });
        else if (inListItem) blocks.Add(new WikiContentBlock { Type = WikiBlockType.BulletItem, Text = t, Bold = bold, IndentLevel = indent });
    }

    private static void AddBlock(List<WikiContentBlock> blocks, WikiBlockType type, StringBuilder buf)
    {
        var t = Normalize(buf);
        if (t.Length > 0) blocks.Add(new WikiContentBlock { Type = type, Text = t });
    }

    private static void EmitBullet(List<WikiContentBlock> blocks, StringBuilder buf, bool bold, int indent)
    {
        var t = Normalize(buf);
        if (t.Length > 0)
            blocks.Add(new WikiContentBlock { Type = WikiBlockType.BulletItem, Text = t, Bold = bold, IndentLevel = indent });
    }

    // Removes any element whose class attribute contains mw-collapsed (hidden legacy
    // content) from the HTML before parsing, so old rule versions don't appear.
    private static string StripCollapsedElements(string html)
    {
        var classPattern = new Regex(
            @"class\s*=\s*(?:""[^""]*\bmw-collapsed\b[^""]*""|'[^']*\bmw-collapsed\b[^']*')",
            RegexOptions.IgnoreCase);

        var toRemove = new List<(int Start, int End)>();

        foreach (Match classMatch in classPattern.Matches(html))
        {
            int tagStart = html.LastIndexOf('<', classMatch.Index);
            if (tagStart < 0) continue;

            var tagNameMatch = Regex.Match(html[(tagStart + 1)..], @"^\w+");
            if (!tagNameMatch.Success) continue;
            var tagName = tagNameMatch.Value;

            int openEnd = html.IndexOf('>', classMatch.Index);
            if (openEnd < 0 || html[openEnd - 1] == '/') continue;

            int depth = 1, pos = openEnd + 1, elementEnd = -1;
            while (depth > 0 && pos < html.Length)
            {
                int nextOpen = FindTag(html, tagName, pos, opening: true);
                int nextClose = FindTag(html, tagName, pos, opening: false);

                if (nextClose < 0) break;
                if (nextOpen >= 0 && nextOpen < nextClose)
                {
                    depth++;
                    pos = nextOpen + 2 + tagName.Length;
                }
                else
                {
                    depth--;
                    if (depth == 0) elementEnd = nextClose + tagName.Length + 3;
                    pos = nextClose + 3 + tagName.Length;
                }
            }

            if (elementEnd >= 0)
                toRemove.Add((tagStart, elementEnd));
        }

        if (toRemove.Count == 0) return html;

        // Remove in reverse order so earlier indices stay valid
        toRemove.Sort((a, b) => b.Start.CompareTo(a.Start));
        var sb = new StringBuilder(html);
        foreach (var (start, end) in toRemove)
            sb.Remove(start, end - start);

        return sb.ToString();
    }

    private static string StripTagsToText(string html)
    {
        var text = Regex.Replace(html, @"<[^>]+>", "");
        return WebUtility.HtmlDecode(text).Trim();
    }

    private static string Normalize(StringBuilder sb)
        => Regex.Replace(sb.ToString().Trim(), @"\s+", " ");
}
