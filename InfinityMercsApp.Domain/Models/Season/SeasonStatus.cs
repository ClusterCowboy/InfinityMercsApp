namespace InfinityMercsApp.Domain.Models.Season;

public class SeasonStatus
{
    public int CrEarned { get; set; }
    public int CrSpent { get; set; }
    public double SwcEarned { get; set; }
    public double SwcBought { get; set; }
    public double SwcSpent { get; set; }
    public List<SeasonUnitStatus> Units { get; set; } = [];
}
