namespace InfinityMercsApp.Infrastructure.API.InfinityArmy.Models.Metadata;

using System.Text.Json.Serialization;

public class MetadataDocument
{
    [JsonPropertyName("factions")]
    public List<Faction> Factions { get; set; } = [];

    [JsonPropertyName("ammunitions")]
    public List<Ammunition> Ammunitions { get; set; } = [];

    [JsonPropertyName("weapons")]
    public List<Weapon> Weapons { get; set; } = [];

    [JsonPropertyName("skills")]
    public List<Skill> Skills { get; set; } = [];

    [JsonPropertyName("equips")]
    public List<Equipment> Equips { get; set; } = [];

    [JsonPropertyName("hack")]
    public List<HackingProgram> Hack { get; set; } = [];

    [JsonPropertyName("martialArts")]
    public List<global::MartialArt> MartialArts { get; set; } = [];

    [JsonPropertyName("metachemistry")]
    public List<Metachemistry> Metachemistry { get; set; } = [];

    [JsonPropertyName("booty")]
    public List<Booty> Booty { get; set; } = [];
}