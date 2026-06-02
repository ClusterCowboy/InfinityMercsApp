namespace InfinityMercsApp.Domain.Models.Stores;

public class StoreItem
{
    public string Name { get; set; } = string.Empty;
    public int CostCr { get; set; }
    public decimal? CostSwc { get; set; }
    public string Category { get; set; } = string.Empty;
}
