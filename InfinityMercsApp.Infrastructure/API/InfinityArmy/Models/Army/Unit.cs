namespace InfinityMercsApp.Infrastructure.API.InfinityArmy.Models.Army;

using System.Text.Json;
using System.Text.Json.Serialization;

public class Unit
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("idArmy")]
    public int? IdArmy { get; set; }

    [JsonPropertyName("canonical")]
    public int? Canonical { get; set; }

    [JsonPropertyName("isc")]
    public string? Isc { get; set; }

    [JsonPropertyName("iscAbbr")]
    public string? IscAbbr { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("slug")]
    public string? Slug { get; set; }

    [JsonPropertyName("profileGroups")]
    public JsonElement ProfileGroups { get; set; }

    [JsonPropertyName("options")]
    public JsonElement Options { get; set; }

    [JsonPropertyName("filters")]
    public JsonElement Filters { get; set; }

    [JsonPropertyName("factions")]
    public JsonElement Factions { get; set; }
}