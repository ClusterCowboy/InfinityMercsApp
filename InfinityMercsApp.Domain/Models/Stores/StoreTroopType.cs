namespace InfinityMercsApp.Domain.Models.Stores;

public class StoreTroopType
{
    public string? Type { get; set; }
    public int CostCr { get; set; }
    public string ArmorName { get; set; } = string.Empty;
    public int? Arm { get; set; }
    public int? Bts { get; set; }
    public string? Abilities { get; set; }
}
