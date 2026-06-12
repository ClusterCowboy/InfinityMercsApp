namespace InfinityMercsApp.Domain.Models.Season;

public class SeasonTransaction
{
    public string OriginStore { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public int CostCr { get; set; }
    public decimal? CostSwc { get; set; }
    public bool IsSale { get; set; }
}
