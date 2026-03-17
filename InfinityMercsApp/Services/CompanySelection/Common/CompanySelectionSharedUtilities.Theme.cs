namespace InfinityMercsApp.Views.Common;

internal static partial class CompanySelectionSharedUtilities
{
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
}
