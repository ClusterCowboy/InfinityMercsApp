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

    [Indexed]
    public int FactionId { get; set; }

    [Indexed]
    public int UnitId { get; set; }

    public int? IdArmy { get; set; }

    public int? Canonical { get; set; }

    public string? Isc { get; set; }

    public string? IscAbbr { get; set; }

    [Indexed]
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

    [Indexed]
    public int FactionId { get; set; }

    [Indexed]
    public int UnitId { get; set; }

    public int? IdArmy { get; set; }

    public string? Isc { get; set; }

    [Indexed]
    public string Name { get; set; } = string.Empty;

    public string? Slug { get; set; }

    public string? Logo { get; set; }

    public int? Type { get; set; }

    public int? Category { get; set; }
}
