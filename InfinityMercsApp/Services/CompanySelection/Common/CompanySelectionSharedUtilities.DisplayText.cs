using System.Text.RegularExpressions;

namespace InfinityMercsApp.Views.Common;

internal static partial class CompanySelectionSharedUtilities
{
    internal static string ConvertDistanceText(string distanceText, bool showUnitsInInches)
    {
        if (string.IsNullOrWhiteSpace(distanceText))
        {
            return distanceText;
        }

        var match = Regex.Match(
            distanceText,
            @"([+-]?)(\d+(?:\.\d+)?)(?:\s*(cm|""|in|inch|inches))?",
            RegexOptions.IgnoreCase);
        if (!match.Success)
        {
            return distanceText;
        }

        var sign = match.Groups[1].Value;
        var numberText = match.Groups[2].Value;
        if (!double.TryParse(
                numberText,
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out var parsedValue))
        {
            return distanceText;
        }

        var unitToken = match.Groups[3].Success
            ? match.Groups[3].Value.Trim().ToLowerInvariant()
            : string.Empty;
        var valueInCm = unitToken is "\"" or "in" or "inch" or "inches"
            ? parsedValue * 2.5
            : parsedValue;

        string replacement;
        if (showUnitsInInches)
        {
            var inches = (int)Math.Round(valueInCm / 2.5, MidpointRounding.AwayFromZero);
            replacement = $"{sign}{inches}\"";
        }
        else
        {
            var roundedCm = Math.Round(valueInCm, 2, MidpointRounding.AwayFromZero);
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

        return NormalizePeripheralBaseName(firstEntry);
    }

    internal static string NormalizePeripheralNameForDedupe(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value == "-")
        {
            return string.Empty;
        }

        return NormalizePeripheralBaseName(value);
    }

    private static string NormalizePeripheralBaseName(string value)
    {
        var result = value.Trim();
        const string prefix = "Peripheral:";
        if (result.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            result = result[prefix.Length..].Trim();
        }
        while (true)
        {
            var updated = Regex.Replace(result, @"\s*\([^)]*\)\s*$", string.Empty).Trim();
            if (updated == result)
            {
                break;
            }

            result = updated;
        }

        return result;
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
