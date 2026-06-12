namespace InfinityMercsApp.Domain.Models.Season;

public class SeasonRound
{
    public int RoundIndex { get; set; }
    public int StartingCr { get; set; }
    public double StartingSwc { get; set; }
    public SeasonMarketplace Marketplace { get; set; } = new();
    public SeasonDowntimeEntry Downtime { get; set; } = new();
    public SeasonMissionResult MissionResults { get; set; } = new();
    public string TemporaryStore { get; set; } = string.Empty;
}
