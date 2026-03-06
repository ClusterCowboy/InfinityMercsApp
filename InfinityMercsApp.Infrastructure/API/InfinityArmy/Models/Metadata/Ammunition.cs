namespace InfinityMercsApp.Infrastructure.API.InfinityArmy.Models.Metadata;

using System.Text.Json.Serialization;

public class Ammunition
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("wiki")]
    public string? Wiki { get; set; }
}