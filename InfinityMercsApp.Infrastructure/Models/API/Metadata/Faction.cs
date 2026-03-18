namespace InfinityMercsApp.Infrastructure.Models.API.Metadata;

using System.Text.Json.Serialization;

public class Faction
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("parent")]
    public int Parent { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("slug")]
    public string Slug { get; set; } = string.Empty;

    [JsonPropertyName("discontinued")]
    public bool Discontinued { get; set; }

    [JsonPropertyName("logo")]
    public string? Logo { get; set; }
}