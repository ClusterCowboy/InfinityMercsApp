namespace InfinityMercsApp.Infrastructure.API.InfinityArmy.Models.Army;

using System.Text.Json;
using System.Text.Json.Serialization;

public class Army
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("units")]
    public List<Unit> Units { get; set; } = [];

    [JsonPropertyName("resume")]
    public List<Resume> Resume { get; set; } = [];

    [JsonPropertyName("reinforcements")]
    public JsonElement Reinforcements { get; set; }

    [JsonPropertyName("filters")]
    public JsonElement Filters { get; set; }

    [JsonPropertyName("fireteams")]
    public JsonElement Fireteams { get; set; }

    [JsonPropertyName("relations")]
    public JsonElement Relations { get; set; }

    [JsonPropertyName("specops")]
    public JsonElement Specops { get; set; }

    [JsonPropertyName("fireteamChart")]
    public JsonElement FireteamChart { get; set; }
}