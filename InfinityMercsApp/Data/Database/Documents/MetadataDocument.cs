using System.Text.Json.Serialization;

namespace InfinityMercsApp.Data.Database;

public class MetadataDocument
{
    [JsonPropertyName("factions")]
    public List<FactionDto> Factions { get; set; } = [];

    [JsonPropertyName("ammunitions")]
    public List<AmmunitionDto> Ammunitions { get; set; } = [];

    [JsonPropertyName("weapons")]
    public List<WeaponDto> Weapons { get; set; } = [];

    [JsonPropertyName("skills")]
    public List<SkillDto> Skills { get; set; } = [];

    [JsonPropertyName("equips")]
    public List<EquipDto> Equips { get; set; } = [];

    [JsonPropertyName("hack")]
    public List<HackProgramDto> Hack { get; set; } = [];

    [JsonPropertyName("martialArts")]
    public List<MartialArtDto> MartialArts { get; set; } = [];

    [JsonPropertyName("metachemistry")]
    public List<MetachemistryDto> Metachemistry { get; set; } = [];

    [JsonPropertyName("booty")]
    public List<BootyDto> Booty { get; set; } = [];
}

public class FactionDto
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

public class AmmunitionDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("wiki")]
    public string? Wiki { get; set; }
}

public class WeaponDto
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

public class SkillDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("wiki")]
    public string? Wiki { get; set; }
}

public class EquipDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("wiki")]
    public string? Wiki { get; set; }
}

public class HackProgramDto
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

public class MartialArtDto
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

public class MetachemistryDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;
}

public class BootyDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;
}
