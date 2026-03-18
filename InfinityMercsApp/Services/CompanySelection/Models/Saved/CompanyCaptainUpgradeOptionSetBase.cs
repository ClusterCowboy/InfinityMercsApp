namespace InfinityMercsApp.Views.Common;

public abstract class CompanyCaptainUpgradeOptionSetBase
{
    public List<string> Weapons { get; init; } = [];
    public List<string> Skills { get; init; } = [];
    public List<string> Equipment { get; init; } = [];
    public bool IsEmpty => Weapons.Count == 0 && Skills.Count == 0 && Equipment.Count == 0;
}
