namespace InfinityMercsApp.Domain.Models.Season;

public class SeasonFile
{
    public string CompanyName { get; set; } = string.Empty;
    public string CompanyIdentifier { get; set; } = string.Empty;
    public string CompanyFilePath { get; set; } = string.Empty;
    public string CreatedDate { get; set; } = string.Empty;
    public SeasonMarketplace InitialPurchases { get; set; } = new();
    public List<SeasonRound> Rounds { get; set; } = [];
    public SeasonStatus CurrentStatus { get; set; } = new();
}
