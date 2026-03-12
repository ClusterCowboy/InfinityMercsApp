using SQLite;

namespace InfinityMercsApp.Data.Database;

[Table("army_factions")]
public class ArmyFactionRecord
{
    [PrimaryKey]
    public int FactionId { get; set; }

    public string Version { get; set; } = string.Empty;

    public long ImportedAtUnixSeconds { get; set; }

    public string? ReinforcementsJson { get; set; }

    public string? FiltersJson { get; set; }

    public string? FireteamsJson { get; set; }

    public string? RelationsJson { get; set; }

    public string? SpecopsJson { get; set; }

    public string? FireteamChartJson { get; set; }

    public string RawJson { get; set; } = string.Empty;
}

[Table("army_units")]
public class ArmyUnitRecord
{
    [PrimaryKey]
    public string UnitKey { get; set; } = string.Empty;

    public int FactionId { get; set; }

    public int UnitId { get; set; }

    public int? IdArmy { get; set; }

    public int? Canonical { get; set; }

    public string? Isc { get; set; }

    public string? IscAbbr { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Slug { get; set; }

    public string? ProfileGroupsJson { get; set; }

    public string? OptionsJson { get; set; }

    public string? FiltersJson { get; set; }

    public string? FactionsJson { get; set; }
}

[Table("army_resume")]
public class ArmyResumeRecord
{
    [PrimaryKey]
    public string ResumeKey { get; set; } = string.Empty;

    public int FactionId { get; set; }

    public int UnitId { get; set; }

    public int? IdArmy { get; set; }

    public string? Isc { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Slug { get; set; }

    public string? Logo { get; set; }

    public int? Type { get; set; }

    public int? Category { get; set; }
}

[Table("army_specops_skills")]
public class ArmySpecopsSkillRecord
{
    [PrimaryKey]
    public string SpecopsSkillKey { get; set; } = string.Empty;

    public int FactionId { get; set; }

    public int EntryOrder { get; set; }

    public int SkillId { get; set; }

    public int Exp { get; set; }

    public string? ExtrasJson { get; set; }

    public string? EquipJson { get; set; }

    public string? WeaponsJson { get; set; }

    public string RawJson { get; set; } = string.Empty;
}

[Table("army_specops_equips")]
public class ArmySpecopsEquipRecord
{
    [PrimaryKey]
    public string SpecopsEquipKey { get; set; } = string.Empty;

    public int FactionId { get; set; }

    public int EntryOrder { get; set; }

    public int EquipId { get; set; }

    public int Exp { get; set; }

    public string? ExtrasJson { get; set; }

    public string? SkillsJson { get; set; }

    public string? WeaponsJson { get; set; }

    public string RawJson { get; set; } = string.Empty;
}

[Table("army_specops_weapons")]
public class ArmySpecopsWeaponRecord
{
    [PrimaryKey]
    public string SpecopsWeaponKey { get; set; } = string.Empty;

    public int FactionId { get; set; }

    public int EntryOrder { get; set; }

    public int WeaponId { get; set; }

    public int Exp { get; set; }

    public string RawJson { get; set; } = string.Empty;
}

[Table("army_specops_units")]
public class ArmySpecopsUnitRecord
{
    [PrimaryKey]
    public string SpecopsUnitKey { get; set; } = string.Empty;

    public int FactionId { get; set; }

    public int EntryOrder { get; set; }

    public int UnitId { get; set; }

    public int? IdArmy { get; set; }

    public int? Canonical { get; set; }

    public string? Isc { get; set; }

    public string? IscAbbr { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Slug { get; set; }

    public string? ProfileGroupsJson { get; set; }

    public string? OptionsJson { get; set; }

    public string? FiltersJson { get; set; }

    public string? FactionsJson { get; set; }

    public string RawJson { get; set; } = string.Empty;
}

[Table("cc_faction_fireteam_validity")]
public class CCFactionFireteamValidityRecord
{
    [PrimaryKey]
    public string CacheKey { get; set; } = string.Empty;

    public int FactionId { get; set; }

    public string FilterKey { get; set; } = string.Empty;

    public bool HasValidCoreFireteams { get; set; }

    public string? ValidCoreFireteamsJson { get; set; }

    public long EvaluatedAtUnixSeconds { get; set; }
}
