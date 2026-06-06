namespace InfinityMercsApp.Domain.Models.Season;

public class SeasonMissionResult
{
    public int UnitsDeployed { get; set; }
    public bool EliteDeploymentMet { get; set; }
    public int OpponentRp { get; set; }
    public bool Won { get; set; }
    public int OpScored { get; set; }
    public int MissionRound { get; set; }
    public List<SeasonMissionUnitResult> UnitResults { get; set; } = [];
}
