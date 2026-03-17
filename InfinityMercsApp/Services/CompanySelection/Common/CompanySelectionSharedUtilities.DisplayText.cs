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
