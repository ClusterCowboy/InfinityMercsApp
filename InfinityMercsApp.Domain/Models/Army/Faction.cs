namespace InfinityMercsApp.Domain.Models.Army;

public class Faction
{
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
