namespace InfinityMercsApp.Domain.Models.Stores;

public class Store
{
    public string Name { get; set; } = string.Empty;
    public IReadOnlyList<string> AssociatedFactions { get; set; } = [];
    public string? AssociatedType { get; set; }
    public string Alignment { get; set; } = string.Empty;
    public IReadOnlyList<StoreItem> Items { get; set; } = [];
    public IReadOnlyList<StoreTroopType> TroopTypes { get; set; } = [];
    public IReadOnlyList<StoreAugment> Augments { get; set; } = [];
}
