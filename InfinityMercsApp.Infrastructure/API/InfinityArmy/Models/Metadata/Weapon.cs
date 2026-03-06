namespace InfinityMercsApp.Infrastructure.API.InfinityArmy.Models.Metadata;

using System.Text.Json.Serialization;

public class Weapon
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("mode")]
    public string? Mode { get; set; }

    [JsonPropertyName("wiki")]
    public string? Wiki { get; set; }

    [JsonPropertyName("ammunition")]
    public int? Ammunition { get; set; }

    [JsonPropertyName("burst")]
    public string? Burst { get; set; }

    [JsonPropertyName("damage")]
    public string? Damage { get; set; }

    [JsonPropertyName("saving")]
    public string? Saving { get; set; }

    [JsonPropertyName("savingNum")]
    public string? SavingNum { get; set; }

    [JsonPropertyName("profile")]
    public string? Profile { get; set; }

    [JsonPropertyName("properties")]
    public List<string>? Properties { get; set; }

    [JsonPropertyName("distance")]
    public object? Distance { get; set; }
}
