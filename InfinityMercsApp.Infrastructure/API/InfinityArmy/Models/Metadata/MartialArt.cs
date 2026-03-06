namespace InfinityMercsApp.Infrastructure.API.InfinityArmy.Models.Metadata;

using System.Text.Json.Serialization;

public class MartialArt
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("opponent")]
    public string? Opponent { get; set; }

    [JsonPropertyName("damage")]
    public string? Damage { get; set; }

    [JsonPropertyName("attack")]
    public string? Attack { get; set; }

    [JsonPropertyName("burst")]
    public string? Burst { get; set; }
}