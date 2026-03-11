using System.Text.Json;
using System.Text.Json.Serialization;

namespace InfinityMercsApp.Data.Database;

public class ArmyDocument
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("units")]
    public List<ArmyUnitDto> Units { get; set; } = [];

    [JsonPropertyName("resume")]
    public List<ArmyResumeDto> Resume { get; set; } = [];

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

public class ArmyUnitDto
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

public class ArmyResumeDto
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
