namespace InfinityMercsApp.Infrastructure.Repositories.Models.Army;

using SQLite;

[Table("armies")]
public class Army
{
    [PrimaryKey]
    public int ArmyId { get; set; }

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
