using System.Text.RegularExpressions;

namespace InfinityMercsApp.Views.Templates.UICommon;

public static class CompanyProfileTextService
{
    public static bool IsMeleeWeaponName(string name)
    {
        return Regex.IsMatch(
            name,
            @"\bccw\b|\bda ccw\b|\bap ccw\b|\bknife\b|\bsword\b|\bmonofilament\b|\bviral ccw\b|\bpistols?\b|\bclose combat weapon\b|\bcc\s*weapon\b|\bc\.?\s*c\.?\s*weapon\b|\bpara\s*cc\s*weapon\b",
            RegexOptions.IgnoreCase);
    }

    public static string JoinOrDash(IEnumerable<string> values)
    {
        var list = values.Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
        return list.Count == 0 ? "-" : string.Join(Environment.NewLine, list);
    }

    public static List<string> EnsureLieutenantSkill(IEnumerable<string> skills)
    {
        var list = skills
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (!list.Any(x => x.Contains("lieutenant", StringComparison.OrdinalIgnoreCase)))
        {
            list.Add("Lieutenant");
        }

        return list
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static List<string> BuildConfigurationSkillNames(IEnumerable<string> rawSkillNames)
    {
        var result = new List<string>();
        foreach (var rawName in rawSkillNames)
        {
            if (string.IsNullOrWhiteSpace(rawName))
            {
                continue;
            }

            var skillName = rawName.Trim();
            if (!IsCommonSpecOpsSkill(skillName))
            {
                result.Add(skillName);
                continue;
            }

            var lieutenantDetail = ExtractLieutenantSkillDetail(skillName);
            if (!string.IsNullOrWhiteSpace(lieutenantDetail))
            {
                result.Add(lieutenantDetail);
            }
        }

        return result
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static List<string> MergeCommonAndUnique(IEnumerable<string> commonValues, string? uniqueValues)
    {
        var merged = new List<string>();
        foreach (var value in commonValues)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                merged.Add(value.Trim());
            }
        }

        if (!string.IsNullOrWhiteSpace(uniqueValues) && uniqueValues != "-")
        {
            var uniqueParts = uniqueValues
                .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .Where(x => !string.Equals(x, "-", StringComparison.Ordinal))
                .Select(x => x.Trim());
            merged.AddRange(uniqueParts);
        }

        return merged
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static FormattedString BuildNameFormatted(string name)
    {
        var formatted = new FormattedString();
        if (string.IsNullOrWhiteSpace(name))
        {
            formatted.Spans.Add(new Span { Text = "-" });
            return formatted;
        }

        var match = Regex.Match(name, "(lieutenant)", RegexOptions.IgnoreCase);
        if (!match.Success)
        {
            formatted.Spans.Add(new Span { Text = name });
            return formatted;
        }

        if (match.Index > 0)
        {
            formatted.Spans.Add(new Span { Text = name[..match.Index] });
        }

        formatted.Spans.Add(new Span
        {
            Text = name.Substring(match.Index, match.Length),
            TextColor = Color.FromArgb("#C084FC")
        });

        var suffixStart = match.Index + match.Length;
        if (suffixStart < name.Length)
        {
            formatted.Spans.Add(new Span { Text = name[suffixStart..] });
        }

        return formatted;
    }

    public static FormattedString BuildListFormattedString(
        IEnumerable<string> values,
        Color textColor,
        bool highlightLieutenantPurple = false)
    {
        var formatted = new FormattedString();
        var lines = values.Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
        if (lines.Count == 0)
        {
            formatted.Spans.Add(new Span { Text = "-", TextColor = textColor });
            return formatted;
        }

        for (var i = 0; i < lines.Count; i++)
        {
            if (i > 0)
            {
                formatted.Spans.Add(new Span { Text = Environment.NewLine, TextColor = textColor });
            }

            var lineColor = highlightLieutenantPurple && lines[i].Contains("lieutenant", StringComparison.OrdinalIgnoreCase)
                ? Color.FromArgb("#C084FC")
                : textColor;
            formatted.Spans.Add(new Span { Text = lines[i], TextColor = lineColor });
        }

        return formatted;
    }

    public static FormattedString BuildNamedSummaryFormatted(
        string label,
        IEnumerable<string> values,
        Color accentColor,
        bool highlightLieutenantPurple = false)
    {
        var list = values
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var formatted = new FormattedString();
        formatted.Spans.Add(new Span { Text = $"{label}: " });
        if (list.Count == 0)
        {
            formatted.Spans.Add(new Span { Text = "-" });
            return formatted;
        }

        for (var i = 0; i < list.Count; i++)
        {
            if (i > 0)
            {
                formatted.Spans.Add(new Span { Text = ", " });
            }

            formatted.Spans.Add(new Span
            {
                Text = list[i],
                TextColor = highlightLieutenantPurple && list[i].Contains("lieutenant", StringComparison.OrdinalIgnoreCase)
                    ? Color.FromArgb("#C084FC")
                    : accentColor
            });
        }

        return formatted;
    }

    public static FormattedString BuildMercsCompanyLineFormatted(string label, string? value, Color accentColor)
    {
        var normalized = string.IsNullOrWhiteSpace(value)
            ? "-"
            : value
                .Replace("\r\n", ", ", StringComparison.Ordinal)
                .Replace("\n", ", ", StringComparison.Ordinal)
                .Replace("\r", ", ", StringComparison.Ordinal);

        var formatted = new FormattedString();
        formatted.Spans.Add(new Span
        {
            Text = $"{label}: "
        });
        formatted.Spans.Add(new Span
        {
            Text = normalized,
            TextColor = accentColor
        });
        return formatted;
    }

    public static List<string> SplitDisplayLine(string? text)
    {
        if (string.IsNullOrWhiteSpace(text) || text == "-")
        {
            return [];
        }

        return text
            .Replace("\r\n", ",", StringComparison.Ordinal)
            .Replace('\r', ',')
            .Replace('\n', ',')
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => !string.IsNullOrWhiteSpace(x) && x != "-")
            .ToList();
    }

    private static bool IsCommonSpecOpsSkill(string? skillName)
    {
        if (string.IsNullOrWhiteSpace(skillName))
        {
            return false;
        }

        return skillName.Contains("lieutenant", StringComparison.OrdinalIgnoreCase) ||
               skillName.Contains("infinity spec-ops", StringComparison.OrdinalIgnoreCase) ||
               skillName.Contains("infinity spec ops", StringComparison.OrdinalIgnoreCase);
    }

    private static string? ExtractLieutenantSkillDetail(string skillName)
    {
        if (!skillName.Contains("lieutenant", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var detail = Regex.Replace(skillName, "lieutenant", string.Empty, RegexOptions.IgnoreCase).Trim();
        if (string.IsNullOrWhiteSpace(detail))
        {
            return null;
        }

        detail = detail.Trim('(', ')', '[', ']', '-', ':', ',', ';', ' ');
        return string.IsNullOrWhiteSpace(detail) ? null : detail;
    }
}
