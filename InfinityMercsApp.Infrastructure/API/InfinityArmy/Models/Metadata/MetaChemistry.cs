namespace InfinityMercsApp.Infrastructure.API.InfinityArmy.Models.Metadata;

using System.Text.Json.Serialization;

public class MetaChemistry
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;
}