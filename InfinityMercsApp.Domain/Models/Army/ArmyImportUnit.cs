namespace InfinityMercsApp.Domain.Models.Army;

public class ArmyImportUnit
{
    public int Id { get; set; }

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
