using SQLite;

namespace InfinityMercsApp.Data.Database;

[Table("factions")]
public class FactionRecord
{
    [PrimaryKey]
    public int Id { get; set; }

    public int ParentId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public bool Discontinued { get; set; }

    public string? Logo { get; set; }
}

[Table("ammunitions")]
public class AmmunitionRecord
{
    [PrimaryKey]
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Wiki { get; set; }
}

[Table("weapons")]
public class WeaponRecord
{
    [PrimaryKey]
    public string WeaponKey { get; set; } = string.Empty;

    public int WeaponId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Type { get; set; }

    public string? Mode { get; set; }

    public string? Wiki { get; set; }

    public int? AmmunitionId { get; set; }

    public string? Burst { get; set; }

    public string? Damage { get; set; }

    public string? Saving { get; set; }

    public string? SavingNum { get; set; }

    public string? Profile { get; set; }

    public string? PropertiesJson { get; set; }

    public string? DistanceJson { get; set; }
}

[Table("skills")]
public class SkillRecord
{
    [PrimaryKey]
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Wiki { get; set; }
}

[Table("equips")]
public class EquipRecord
{
    [PrimaryKey]
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Wiki { get; set; }
}

[Table("hack_programs")]
public class HackProgramRecord
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Opponent { get; set; }

    public string? Special { get; set; }

    public string? Damage { get; set; }

    public string? Attack { get; set; }

    public string? Burst { get; set; }

    public int? Extra { get; set; }

    public string? SkillTypeJson { get; set; }

    public string? DevicesJson { get; set; }

    public string? TargetJson { get; set; }
}

[Table("martial_arts")]
public class MartialArtRecord
{
    [PrimaryKey]
    public string Name { get; set; } = string.Empty;

    public string? Opponent { get; set; }

    public string? Damage { get; set; }

    public string? Attack { get; set; }

    public string? Burst { get; set; }
}

[Table("metachemistry")]
public class MetachemistryRecord
{
    [PrimaryKey]
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;
}

[Table("booty")]
public class BootyRecord
{
    [PrimaryKey]
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;
}
