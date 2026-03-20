namespace InfinityMercsApp.Infrastructure.Models.Database.Army;

using SQLite;

[Table("specops_units")]
public class SpecopsUnit
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