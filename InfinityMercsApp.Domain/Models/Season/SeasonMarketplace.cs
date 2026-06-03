namespace InfinityMercsApp.Domain.Models.Season;

public class SeasonMarketplace
{
    public List<string> Stores { get; set; } = [];
    public List<SeasonTransaction> Transactions { get; set; } = [];
}
