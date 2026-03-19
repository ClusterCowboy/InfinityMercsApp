using System;
using System.Collections.Generic;

namespace InfinityMercsApp.Models;

public static class UnitStatline
{
    public static Dictionary<string, string> ParseSegments(string? statline)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(statline))
        {
            return result;
        }

        foreach (var segment in statline.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = segment.Split(' ', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
            {
                continue;
            }

            var key = parts[0].Trim().ToUpperInvariant();
            var value = parts[1].Trim();
            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            result[key] = value;
        }

        return result;
    }

    public static string ReadValue(string? statline, string key, string fallback)
    {
        return ReadValue(ParseSegments(statline), key, fallback);
    }

    public static string ReadValue(IReadOnlyDictionary<string, string> segments, string key, string fallback)
    {
        return segments.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : fallback;
    }

    public static string ResolveVitalityHeader(IReadOnlyDictionary<string, string> segments, string fallback = "")
    {
        if (segments.ContainsKey("STR"))
        {
            return "STR";
        }

        if (segments.ContainsKey("W"))
        {
            return "W";
        }

        foreach (var key in segments.Keys)
        {
            if (key is "MOV" or "CC" or "BS" or "PH" or "WIP" or "ARM" or "BTS" or "S" or "AVA")
            {
                continue;
            }

            return key;
        }

        return fallback;
    }
}
