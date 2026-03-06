using System.Text.Json.Serialization;

namespace InfinityMercsApp.Infrastructure.API.InfinityArmy.Models.Army;

public class Resume
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("isc")]
    public string? Isc { get; set; }

    [JsonPropertyName("idArmy")]
    public int? IdArmy { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("slug")]
    public string? Slug { get; set; }

    [JsonPropertyName("logo")]
    public string? Logo { get; set; }

    [JsonPropertyName("type")]
    public int? Type { get; set; }

    [JsonPropertyName("category")]
    public int? Category { get; set; }
}