namespace InfinityMercsApp.Domain.Models.Army;

public class CohesiveCompanyFactionQueryResult
{
    public IReadOnlyDictionary<int, string> TypeLookup { get; init; } = new Dictionary<int, string>();

    public IReadOnlyDictionary<int, string> CharacteristicsLookup { get; init; } = new Dictionary<int, string>();

    public IReadOnlyDictionary<int, string> SkillsLookup { get; init; } = new Dictionary<int, string>();

    public IReadOnlyDictionary<int, string> EquipmentLookup { get; init; } = new Dictionary<int, string>();

    public IReadOnlyDictionary<int, string> WeaponsLookup { get; init; } = new Dictionary<int, string>();

    public IReadOnlyDictionary<int, string> AmmoLookup { get; init; } = new Dictionary<int, string>();

    public IReadOnlyList<MercsArmyListEntry> MergedMercsListEntries { get; init; } = [];
}
