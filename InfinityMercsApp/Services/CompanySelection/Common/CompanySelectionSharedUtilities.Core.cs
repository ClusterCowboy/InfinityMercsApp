using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace InfinityMercsApp.Views.Common;

/// <summary>
/// Shared static helpers used by Standard and Cohesive company selection pages.
/// </summary>
internal static partial class CompanySelectionSharedUtilities
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
}
