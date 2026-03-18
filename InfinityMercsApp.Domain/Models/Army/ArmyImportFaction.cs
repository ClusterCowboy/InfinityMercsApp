namespace InfinityMercsApp.Domain.Models.Army;

public class ArmyImportFaction
{
    public string Version { get; set; } = string.Empty;

    public List<ArmyImportUnit> Units { get; set; } = [];

    public List<ArmyImportResume> Resume { get; set; } = [];

    public string? ReinforcementsJson { get; set; }

    public string? FiltersJson { get; set; }

    public string? FireteamsJson { get; set; }

    public string? RelationsJson { get; set; }

    public string? SpecopsJson { get; set; }

    public string? FireteamChartJson { get; set; }

    public string RawJson { get; set; } = string.Empty;
}
