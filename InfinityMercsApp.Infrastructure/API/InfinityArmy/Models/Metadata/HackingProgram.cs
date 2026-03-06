namespace InfinityMercsApp.Infrastructure.API.InfinityArmy.Models.Metadata;

using System.Text.Json.Serialization;

public class HackingProgram
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("opponent")]
    public string? Opponent { get; set; }

    [JsonPropertyName("special")]
    public string? Special { get; set; }

    [JsonPropertyName("damage")]
    public string? Damage { get; set; }

    [JsonPropertyName("attack")]
    public string? Attack { get; set; }

    [JsonPropertyName("burst")]
    public string? Burst { get; set; }

    [JsonPropertyName("extra")]
    public int? Extra { get; set; }

    [JsonPropertyName("skillType")]
    public List<string>? SkillType { get; set; }

    [JsonPropertyName("devices")]
    public List<int>? Devices { get; set; }

    [JsonPropertyName("target")]
    public List<string>? Target { get; set; }
}