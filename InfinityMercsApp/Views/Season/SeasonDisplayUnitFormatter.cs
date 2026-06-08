using System.Globalization;
using System.Text.RegularExpressions;
using InfinityMercsApp.Infrastructure.Providers;
using InfinityMercsApp.Views.Controls;

namespace InfinityMercsApp.Views.Season;

internal static class SeasonDisplayUnitFormatter
{
    private const decimal CmPerGameInch = 2.5m;

    private static readonly Regex ExplicitDistancePattern = new(
        "(?<sign>[+-]?)(?<value>\\d+(?:\\.\\d+)?)\\s*(?<unit>cm|inches|inch|in|\")",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static bool GetShowUnitsInInches(IAppSettingsProvider? appSettingsProvider)
    {
        if (appSettingsProvider is null)
        {
            return true;
        }

        try
        {
            return appSettingsProvider.GetShowUnitsInInches();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Season display unit preference load failed: {ex.Message}");
            return true;
        }
    }

    public static string FormatMoveValue(
        string? savedMove,
        int? firstCm,
        int? secondCm,
        bool showUnitsInInches)
    {
        if (firstCm is > 0 && secondCm is > 0)
        {
            return UnitDisplayConfigurationsView.FormatMoveValue(firstCm, secondCm, showUnitsInInches);
        }

        if (TryParseMoveToCentimeters(savedMove, out var parsedFirstCm, out var parsedSecondCm))
        {
            return UnitDisplayConfigurationsView.FormatMoveValue(parsedFirstCm, parsedSecondCm, showUnitsInInches);
        }

        return string.IsNullOrWhiteSpace(savedMove) ? "-" : savedMove.Trim();
    }

    public static string ConvertExplicitDistances(string? text, bool showUnitsInInches)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.IsNullOrWhiteSpace(text) ? string.Empty : text;
        }

        return ExplicitDistancePattern.Replace(text, match =>
        {
            var signToken = match.Groups["sign"].Value;
            var valueToken = match.Groups["value"].Value;
            var unitToken = match.Groups["unit"].Value;

            if (!decimal.TryParse(valueToken, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
            {
                return match.Value;
            }

            if (signToken == "-")
            {
                value = -value;
            }

            var isInches = unitToken is "\"" ||
                           unitToken.Equals("in", StringComparison.OrdinalIgnoreCase) ||
                           unitToken.Equals("inch", StringComparison.OrdinalIgnoreCase) ||
                           unitToken.Equals("inches", StringComparison.OrdinalIgnoreCase);

            var converted = showUnitsInInches
                ? isInches ? value : Math.Round(value / CmPerGameInch, 0, MidpointRounding.AwayFromZero)
                : isInches ? value * CmPerGameInch : value;

            return $"{FormatSignedValue(converted, signToken)}{(showUnitsInInches ? "\"" : "cm")}";
        });
    }

    private static bool TryParseMoveToCentimeters(string? moveText, out int firstCm, out int secondCm)
    {
        firstCm = 0;
        secondCm = 0;

        if (string.IsNullOrWhiteSpace(moveText))
        {
            return false;
        }

        var match = Regex.Match(
            moveText.Trim(),
            "^(?<a>\\d+)\\s*(?<au>cm|\"|in|inch|inches)?\\s*[-/]\\s*(?<b>\\d+)\\s*(?<bu>cm|\"|in|inch|inches)?$",
            RegexOptions.IgnoreCase);
        if (!match.Success ||
            !int.TryParse(match.Groups["a"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var a) ||
            !int.TryParse(match.Groups["b"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var b))
        {
            return false;
        }

        var aUnit = match.Groups["au"].Success ? match.Groups["au"].Value.Trim().ToLowerInvariant() : string.Empty;
        var bUnit = match.Groups["bu"].Success ? match.Groups["bu"].Value.Trim().ToLowerInvariant() : string.Empty;
        var hasExplicitUnits = !string.IsNullOrWhiteSpace(aUnit) || !string.IsNullOrWhiteSpace(bUnit);
        var useInches = hasExplicitUnits
            ? IsInchUnit(aUnit) || IsInchUnit(bUnit)
            : a <= 8 && b <= 8;

        firstCm = useInches ? (int)Math.Round(a * CmPerGameInch, MidpointRounding.AwayFromZero) : a;
        secondCm = useInches ? (int)Math.Round(b * CmPerGameInch, MidpointRounding.AwayFromZero) : b;
        return true;
    }

    private static bool IsInchUnit(string unit)
    {
        return unit is "\"" or "in" or "inch" or "inches";
    }

    private static string FormatSignedValue(decimal value, string originalSign)
    {
        var formatted = FormatNumber(Math.Abs(value));
        if (value < 0)
        {
            return $"-{formatted}";
        }

        return originalSign == "+" ? $"+{formatted}" : formatted;
    }

    private static string FormatNumber(decimal value)
    {
        return decimal.Truncate(value) == value
            ? value.ToString("0", CultureInfo.InvariantCulture)
            : value.ToString("0.##", CultureInfo.InvariantCulture);
    }
}
