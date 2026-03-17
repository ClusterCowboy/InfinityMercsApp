using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

namespace InfinityMercsApp.Views.Common;

/// <summary>
/// Shared static helpers used by Standard and Cohesive company selection pages.
/// </summary>
internal static class CompanySelectionSharedUtilities
{
    internal static int ParseCostValue(string? cost)
    {
        if (string.IsNullOrWhiteSpace(cost))
        {
            return 0;
        }
    
        if (int.TryParse(cost, out var parsed))
        {
            return parsed;
        }
    
        var match = Regex.Match(cost, "\\d+");
        return match.Success && int.TryParse(match.Value, out var fallback) ? fallback : 0;
    }

    internal static int? ParseAvaLimit(string? ava)
    {
        if (string.IsNullOrWhiteSpace(ava))
        {
            return null;
        }
    
        var trimmed = ava.Trim();
        if (trimmed is "-" or "T")
        {
            return null;
        }
    
        if (!int.TryParse(trimmed, out var parsed))
        {
            return null;
        }
    
        // App convention: 255 means Total (no cap).
        return parsed >= 255 ? null : parsed;
    }

    internal static string ComputeCompanyIdentifier(string fileName)
    {
        var bytes = Encoding.UTF8.GetBytes(fileName);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }

    internal static int GetNextCompanyIndex(string saveDir, string companyName, string safeFileName)
    {
        var maxIndex = 0;
        var files = Directory.EnumerateFiles(saveDir, "*.json");
    
        foreach (var file in files)
        {
            try
            {
                var json = File.ReadAllText(file);
                using var doc = JsonDocument.Parse(json);
                if (!doc.RootElement.TryGetProperty("CompanyName", out var nameElement) ||
                    !string.Equals(nameElement.GetString(), companyName, StringComparison.Ordinal))
                {
                    continue;
                }
    
                if (doc.RootElement.TryGetProperty("CompanyIndex", out var indexElement) &&
                    indexElement.ValueKind == JsonValueKind.Number &&
                    indexElement.TryGetInt32(out var parsedIndex))
                {
                    maxIndex = Math.Max(maxIndex, parsedIndex);
                    continue;
                }
            }
            catch
            {
                // Ignore malformed records and continue.
            }
    
            var baseName = Path.GetFileNameWithoutExtension(file);
            var match = Regex.Match(baseName, $"^{Regex.Escape(safeFileName)}-(\\d+)$", RegexOptions.IgnoreCase);
            if (match.Success && int.TryParse(match.Groups[1].Value, out var fallbackIndex))
            {
                maxIndex = Math.Max(maxIndex, fallbackIndex);
            }
        }
    
        return maxIndex + 1;
    }

    internal static void MergeLookup(Dictionary<int, string> target, IReadOnlyDictionary<int, string> source)
    {
        foreach (var pair in source)
        {
            if (target.ContainsKey(pair.Key) || string.IsNullOrWhiteSpace(pair.Value))
            {
                continue;
            }
    
            target[pair.Key] = pair.Value.Trim();
        }
    }

    internal static Dictionary<int, string> BuildIdNameLookup(string? filtersJson, string sectionName)
    {
        var map = new Dictionary<int, string>();
        if (string.IsNullOrWhiteSpace(filtersJson))
        {
            return map;
        }
    
        try
        {
            using var doc = JsonDocument.Parse(filtersJson);
            if (!doc.RootElement.TryGetProperty(sectionName, out var section) || section.ValueKind != JsonValueKind.Array)
            {
                return map;
            }
    
            foreach (var entry in section.EnumerateArray())
            {
                if (!entry.TryGetProperty("id", out var idElement))
                {
                    continue;
                }
    
                int id;
                if (idElement.ValueKind == JsonValueKind.Number && idElement.TryGetInt32(out var intId))
                {
                    id = intId;
                }
                else if (idElement.ValueKind == JsonValueKind.String && int.TryParse(idElement.GetString(), out var stringId))
                {
                    id = stringId;
                }
                else
                {
                    continue;
                }
    
                if (!entry.TryGetProperty("name", out var nameElement) || nameElement.ValueKind != JsonValueKind.String)
                {
                    continue;
                }
    
                var name = nameElement.GetString();
                if (!string.IsNullOrWhiteSpace(name))
                {
                    map[id] = name;
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"CompanySelectionPage BuildIdNameLookup failed for '{sectionName}': {ex.Message}");
        }
    
        return map;
    }

    internal static bool TryGetPropertyFlexible(JsonElement element, string propertyName, out JsonElement value)
    {
        var variants = new[]
        {
            propertyName,
            propertyName.ToLowerInvariant(),
            propertyName.ToUpperInvariant(),
            char.ToUpperInvariant(propertyName[0]) + propertyName.Substring(1).ToLowerInvariant()
        };
    
        foreach (var variant in variants.Distinct(StringComparer.Ordinal))
        {
            if (element.TryGetProperty(variant, out value))
            {
                return true;
            }
        }
    
        foreach (var containerName in new[] { "stats", "Stats", "attributes", "Attributes", "attrs", "Attrs" })
        {
            if (!element.TryGetProperty(containerName, out var container) || container.ValueKind != JsonValueKind.Object)
            {
                continue;
            }
    
            foreach (var variant in variants.Distinct(StringComparer.Ordinal))
            {
                if (container.TryGetProperty(variant, out value))
                {
                    return true;
                }
            }
        }
    
        value = default;
        return false;
    }

    internal static bool TryParseId(JsonElement element, out int id)
    {
        if (element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out var numberId))
        {
            id = numberId;
            return true;
        }
    
        if (element.ValueKind == JsonValueKind.String &&
            int.TryParse(element.GetString(), out var stringId))
        {
            id = stringId;
            return true;
        }
    
        if (element.ValueKind == JsonValueKind.Object &&
            element.TryGetProperty("id", out var idElement))
        {
            return TryParseId(idElement, out id);
        }
    
        id = 0;
        return false;
    }

    internal static string ReadString(JsonElement element, string propertyName, string fallback)
    {
        if (!element.TryGetProperty(propertyName, out var value) || value.ValueKind != JsonValueKind.String)
        {
            return fallback;
        }
    
        var text = value.GetString();
        return string.IsNullOrWhiteSpace(text) ? fallback : text;
    }

    internal static int ReadInt(JsonElement element, string propertyName, int fallback)
    {
        if (!element.TryGetProperty(propertyName, out var value))
        {
            return fallback;
        }
    
        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number))
        {
            return number;
        }
    
        if (value.ValueKind == JsonValueKind.String && int.TryParse(value.GetString(), out var parsed))
        {
            return parsed;
        }
    
        return fallback;
    }

    internal static bool ReadBool(JsonElement element, string propertyName, bool fallback)
    {
        if (!element.TryGetProperty(propertyName, out var value))
        {
            return fallback;
        }
    
        if (value.ValueKind == JsonValueKind.True)
        {
            return true;
        }
    
        if (value.ValueKind == JsonValueKind.False)
        {
            return false;
        }
    
        if (value.ValueKind == JsonValueKind.String && bool.TryParse(value.GetString(), out var parsed))
        {
            return parsed;
        }
    
        return fallback;
    }

    internal static string ReadIntAsString(JsonElement option, string propertyName)
    {
        if (!TryGetPropertyFlexible(option, propertyName, out var element))
        {
            return "-";
        }
    
        if (element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out var value))
        {
            return value.ToString();
        }
    
        if (element.ValueKind == JsonValueKind.String && int.TryParse(element.GetString(), out var parsed))
        {
            return parsed.ToString();
        }
    
        return "-";
    }

    internal static string ReadNumericString(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out var numericValue))
        {
            return numericValue.ToString();
        }
    
        if (element.ValueKind == JsonValueKind.String && int.TryParse(element.GetString(), out var parsedValue))
        {
            return parsedValue.ToString();
        }
    
        return "-";
    }

    internal static string ReadMove(JsonElement option)
    {
        if (!TryGetPropertyFlexible(option, "mov", out var movElement))
        {
            return "-";
        }
    
        if (movElement.ValueKind == JsonValueKind.Array)
        {
            var parts = movElement.EnumerateArray()
                .Select(x => x.ValueKind == JsonValueKind.Number && x.TryGetInt32(out var n) ? n.ToString() : x.GetString())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();
            return parts.Count == 0 ? "-" : string.Join("-", parts);
        }
    
        if (movElement.ValueKind == JsonValueKind.String)
        {
            return movElement.GetString() ?? "-";
        }
    
        return "-";
    }

    internal static string ReadMoveFromProfile(JsonElement profile)
    {
        if (TryGetPropertyFlexible(profile, "move", out var moveElement) && moveElement.ValueKind == JsonValueKind.Array)
        {
            var moveParts = moveElement.EnumerateArray()
                .Select(ReadNumericString)
                .Where(x => !string.IsNullOrWhiteSpace(x) && x != "-")
                .ToList();
    
            if (moveParts.Count > 0)
            {
                return string.Join("-", moveParts);
            }
        }
    
        return ReadMove(profile);
    }

    internal static bool HasStatFields(JsonElement element)
    {
        return element.ValueKind == JsonValueKind.Object &&
               (element.TryGetProperty("cc", out _) ||
                element.TryGetProperty("bs", out _) ||
                element.TryGetProperty("ph", out _) ||
                element.TryGetProperty("wip", out _) ||
                element.TryGetProperty("arm", out _) ||
                element.TryGetProperty("bts", out _));
    }

    internal static bool HasAsteriskMin(JsonElement element)
    {
        if (!element.TryGetProperty("min", out var value) || value.ValueKind != JsonValueKind.String)
        {
            return false;
        }
    
        var text = value.GetString();
        return string.Equals(text?.Trim(), "*", StringComparison.Ordinal);
    }

    internal static bool HasLieutenantOrder(JsonElement option)
    {
        if (!option.TryGetProperty("orders", out var ordersElement) || ordersElement.ValueKind != JsonValueKind.Array)
        {
            return false;
        }
    
        foreach (var order in ordersElement.EnumerateArray())
        {
            if (!order.TryGetProperty("type", out var typeElement) || typeElement.ValueKind != JsonValueKind.String)
            {
                continue;
            }
    
            if (string.Equals(typeElement.GetString(), "LIEUTENANT", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
    
        return false;
    }

    internal static bool IsPositiveSwc(string swc)
    {
        if (string.IsNullOrWhiteSpace(swc) || swc == "-")
        {
            return false;
        }
    
        return decimal.TryParse(
                   swc,
                   System.Globalization.NumberStyles.Number,
                   System.Globalization.CultureInfo.InvariantCulture,
                   out var value)
               && value > 0m;
    }

    internal static bool IsLieutenantOption(JsonElement option, IReadOnlyDictionary<int, string> skillsLookup)
    {
        if (HasLieutenantOrder(option))
        {
            return true;
        }
    
        if (option.TryGetProperty("name", out var nameElement) &&
            nameElement.ValueKind == JsonValueKind.String &&
            nameElement.GetString()?.Contains("lieutenant", StringComparison.OrdinalIgnoreCase) == true)
        {
            return true;
        }
    
        if (!option.TryGetProperty("skills", out var skillsElement) || skillsElement.ValueKind != JsonValueKind.Array)
        {
            return false;
        }
    
        foreach (var entry in skillsElement.EnumerateArray())
        {
            if (!TryParseId(entry, out var id))
            {
                continue;
            }
    
            if (skillsLookup.TryGetValue(id, out var name) &&
                name.Contains("lieutenant", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
    
        return false;
    }

    internal static bool UnitHasLieutenantOption(string? profileGroupsJson, IReadOnlyDictionary<int, string> skillsLookup)
    {
        if (string.IsNullOrWhiteSpace(profileGroupsJson))
        {
            return false;
        }
    
        try
        {
            using var doc = JsonDocument.Parse(profileGroupsJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
            {
                return false;
            }
    
            foreach (var group in doc.RootElement.EnumerateArray())
            {
                if (!group.TryGetProperty("options", out var optionsElement) || optionsElement.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }
    
                foreach (var option in optionsElement.EnumerateArray())
                {
                    if (IsLieutenantOption(option, skillsLookup))
                    {
                        return true;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"CompanySelectionPage UnitHasLieutenantOption failed: {ex.Message}");
        }
    
        return false;
    }

    internal static IEnumerable<JsonElement> EnumerateOptions(JsonElement profileGroupsRoot)
    {
        if (profileGroupsRoot.ValueKind != JsonValueKind.Array)
        {
            yield break;
        }
    
        foreach (var group in profileGroupsRoot.EnumerateArray())
        {
            if (!group.TryGetProperty("options", out var options) || options.ValueKind != JsonValueKind.Array)
            {
                continue;
            }
    
            foreach (var option in options.EnumerateArray())
            {
                yield return option.Clone();
            }
        }
    }

    internal static IEnumerable<JsonElement> GetContainerEntries(JsonElement container, string propertyName)
    {
        if (!TryGetPropertyFlexible(container, propertyName, out var entriesElement) ||
            entriesElement.ValueKind != JsonValueKind.Array)
        {
            yield break;
        }
    
        foreach (var entry in entriesElement.EnumerateArray())
        {
            yield return entry.Clone();
        }
    }

    internal static IReadOnlyList<int> ParseFactionIds(string? factionsJson)
    {
        if (string.IsNullOrWhiteSpace(factionsJson))
        {
            return Array.Empty<int>();
        }
    
        try
        {
            using var doc = JsonDocument.Parse(factionsJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
            {
                return Array.Empty<int>();
            }
    
            var ids = new List<int>();
            foreach (var element in doc.RootElement.EnumerateArray())
            {
                if (element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out var numericId))
                {
                    ids.Add(numericId);
                    continue;
                }
    
                if (element.ValueKind == JsonValueKind.String &&
                    int.TryParse(element.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var stringId))
                {
                    ids.Add(stringId);
                }
            }
    
            return ids;
        }
        catch
        {
            return Array.Empty<int>();
        }
    }

    internal static string NormalizeFactionName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }
    
        var cleaned = value.Trim().ToLowerInvariant();
        cleaned = cleaned.Replace("reinforcements", string.Empty, StringComparison.Ordinal)
                         .Replace("reinforcement", string.Empty, StringComparison.Ordinal)
                         .Trim();
        return cleaned switch
        {
            "yu jing" => "yujing",
            "combined army" => "combinedarmy",
            "non aligned army" => "nonalignedarmy",
            "non-aligned armies" => "nonalignedarmy",
            "non aligned armies" => "nonalignedarmy",
            "non-aligned army" => "nonalignedarmy",
            "japanese secessionist army" => "jsa",
            "o-12" => "o12",
            _ => new string(cleaned.Where(char.IsLetterOrDigit).ToArray())
        };
    }

    internal static bool IsThemeFactionName(string? factionName)
    {
        var key = NormalizeFactionName(factionName);
        return key is
            "panoceania" or
            "yujing" or
            "ariadna" or
            "haqqislam" or
            "nomads" or
            "combinedarmy" or
            "aleph" or
            "tohaa" or
            "nonalignedarmy" or
            "o12" or
            "jsa";
    }

    internal static bool IsLightColor(Color color)
    {
        var luminance = (0.299 * color.Red) + (0.587 * color.Green) + (0.114 * color.Blue);
        return luminance >= 0.6;
    }

    internal static void DrawPictureInRect(SKCanvas canvas, SKPicture picture, SKRect destination)
    {
        var bounds = picture.CullRect;
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }
    
        var scale = Math.Min(destination.Width / bounds.Width, destination.Height / bounds.Height);
        var drawnWidth = bounds.Width * scale;
        var drawnHeight = bounds.Height * scale;
        var translateX = destination.Left + ((destination.Width - drawnWidth) / 2f) - (bounds.Left * scale);
        var translateY = destination.Top + ((destination.Height - drawnHeight) / 2f) - (bounds.Top * scale);
    
        using var restore = new SKAutoCanvasRestore(canvas, true);
        canvas.Translate(translateX, translateY);
        canvas.Scale(scale);
        canvas.DrawPicture(picture);
    }

    internal static void DrawSlotPicture(SKPicture? picture, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
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
    
        var width = e.Info.Width;
        var height = e.Info.Height;
        var scale = Math.Min(width / bounds.Width, height / bounds.Height);
        var x = (width - (bounds.Width * scale)) / 2f;
        var y = (height - (bounds.Height * scale)) / 2f;
    
        using var restore = new SKAutoCanvasRestore(canvas, true);
        canvas.Translate(x, y);
        canvas.Scale(scale);
        canvas.DrawPicture(picture);
    }

    internal static void DrawSlotBorder(SKPaintSurfaceEventArgs e, SKColor borderColor)
    {
        var canvas = e.Surface.Canvas;
        using var borderPaint = new SKPaint
        {
            Color = borderColor,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2f,
            IsAntialias = true
        };
    
        const float inset = 1f;
        canvas.DrawRect(inset, inset, e.Info.Width - (inset * 2f), e.Info.Height - (inset * 2f), borderPaint);
    }

    internal static void ApplyFilterButtonSize(Border? buttonBorder, SKCanvasView? iconCanvas, double iconButtonSize)
    {
        if (buttonBorder is null || iconCanvas is null || iconButtonSize <= 0)
        {
            return;
        }
    
        buttonBorder.WidthRequest = iconButtonSize;
        buttonBorder.HeightRequest = iconButtonSize;
        iconCanvas.WidthRequest = iconButtonSize;
        iconCanvas.HeightRequest = iconButtonSize;
    }

    internal static string ConvertDistanceText(string distanceText, bool showUnitsInInches)
    {
        if (string.IsNullOrWhiteSpace(distanceText))
        {
            return distanceText;
        }
    
        var match = Regex.Match(distanceText, @"([+-]?)(\d+(?:\.\d+)?)(?:\s*(?:cm|""|in|inch|inches))?", RegexOptions.IgnoreCase);
        if (!match.Success)
        {
            return distanceText;
        }
    
        var sign = match.Groups[1].Value;
        var numberText = match.Groups[2].Value;
        if (!double.TryParse(numberText, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var cm))
        {
            return distanceText;
        }
    
        string replacement;
        if (showUnitsInInches)
        {
            var inches = (int)Math.Round(cm / 2.5, MidpointRounding.AwayFromZero);
            replacement = $"{sign}{inches}\"";
        }
        else
        {
            var roundedCm = Math.Round(cm, 2, MidpointRounding.AwayFromZero);
            var cmText = Math.Abs(roundedCm - Math.Round(roundedCm)) < 0.001
                ? ((int)Math.Round(roundedCm)).ToString(System.Globalization.CultureInfo.InvariantCulture)
                : roundedCm.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
            replacement = $"{sign}{cmText}cm";
        }
    
        return distanceText.Remove(match.Index, match.Length).Insert(match.Index, replacement);
    }

    internal static string ReplaceSubtitleMoveDisplay(string? subtitle, string moveDisplay)
    {
        if (string.IsNullOrWhiteSpace(subtitle))
        {
            return $"MOV {moveDisplay}";
        }
    
        return Regex.Replace(
            subtitle,
            @"(?<=\bMOV\s)[^|]+",
            moveDisplay,
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    internal static string ExtractFirstPeripheralName(string? peripheralsText)
    {
        if (string.IsNullOrWhiteSpace(peripheralsText) || peripheralsText == "-")
        {
            return string.Empty;
        }
    
        var firstEntry = peripheralsText
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault();
        if (string.IsNullOrWhiteSpace(firstEntry))
        {
            return string.Empty;
        }
    
        return Regex.Replace(firstEntry, @"\s*\(\d+\)\s*$", string.Empty).Trim();
    }

    internal static string NormalizePeripheralNameForDedupe(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value == "-")
        {
            return string.Empty;
        }
    
        return Regex.Replace(value, @"\s*\(\d+\)\s*$", string.Empty).Trim();
    }

    internal static int GetPeripheralTotalCount(IEnumerable<string> peripheralNames)
    {
        var total = 0;
        foreach (var name in peripheralNames)
        {
            if (string.IsNullOrWhiteSpace(name) || name == "-")
            {
                continue;
            }
    
            var match = Regex.Match(name, @"\((\d+)\)\s*$");
            if (match.Success && int.TryParse(match.Groups[1].Value, out var parsedCount))
            {
                total += Math.Max(0, parsedCount);
            }
            else
            {
                total += 1;
            }
        }
    
        return total;
    }

}



