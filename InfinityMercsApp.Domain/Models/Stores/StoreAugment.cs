namespace InfinityMercsApp.Domain.Models.Stores;

public class StoreAugment
{
    public string Name { get; set; } = string.Empty;
    public string? Requirement { get; set; }
    public int CostCr { get; set; }
    public string? CostNote { get; set; }
}
