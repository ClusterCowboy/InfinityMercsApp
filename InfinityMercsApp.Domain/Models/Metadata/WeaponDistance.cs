using System.Text.Json.Serialization;

namespace InfinityMercsApp.Domain.Models.Metadata;

public class WeaponDistanceBand
{
    [JsonPropertyName("max")]
    public int Max { get; set; }

    [JsonPropertyName("mod")]
    public string Mod { get; set; } = "0";
}

public class WeaponDistance
{
    [JsonPropertyName("short")]
    public WeaponDistanceBand? Short { get; set; }

    [JsonPropertyName("med")]
    public WeaponDistanceBand? Med { get; set; }

    [JsonPropertyName("long")]
    public WeaponDistanceBand? Long { get; set; }

    [JsonPropertyName("max")]
    public WeaponDistanceBand? Max { get; set; }

    /// <summary>
    /// Returns bands in range order with computed start/end values.
    /// Each tuple: (label, rangeStart, rangeEnd, mod)
    /// </summary>
    public IReadOnlyList<(string Label, int RangeStart, int RangeEnd, string Mod)> GetOrderedBands()
    {
        var result = new List<(string, int, int, string)>();
        int previous = 0;

        void AddBand(string label, WeaponDistanceBand? band)
        {
            if (band is null) return;
            result.Add((label, previous, band.Max, band.Mod));
            previous = band.Max;
        }

        AddBand("short", Short);
        AddBand("med", Med);
        AddBand("long", Long);
        AddBand("max", Max);

        return result;
    }
}
