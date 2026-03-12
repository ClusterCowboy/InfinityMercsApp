namespace InfinityMercsApp.Data.Database;

public interface ICohesiveCompanyFactionQueryAccessor
{
    Task<CohesiveCompanyFactionQueryResult> GetFilterQuerySourceAsync(
        IReadOnlyCollection<int> factionIds,
        CancellationToken cancellationToken = default);
}

public sealed class CohesiveCompanyFactionQueryResult
{
    public IReadOnlyDictionary<int, string> TypeLookup { get; init; } = new Dictionary<int, string>();
    public IReadOnlyDictionary<int, string> CharacteristicsLookup { get; init; } = new Dictionary<int, string>();
    public IReadOnlyDictionary<int, string> SkillsLookup { get; init; } = new Dictionary<int, string>();
    public IReadOnlyDictionary<int, string> EquipmentLookup { get; init; } = new Dictionary<int, string>();
    public IReadOnlyDictionary<int, string> WeaponsLookup { get; init; } = new Dictionary<int, string>();
    public IReadOnlyDictionary<int, string> AmmoLookup { get; init; } = new Dictionary<int, string>();
    public IReadOnlyList<MercsArmyListEntry> MergedMercsListEntries { get; init; } = [];
}
