namespace InfinityMercsApp.Data.Database;

/// <summary>
/// Accessor that builds Mercs-eligible army unit lists for one or more source factions.
/// </summary>
public interface IMercsArmyListAccessor
{
    Task<IReadOnlyList<MercsArmyListEntry>> GetMergedMercsArmyListAsync(
        int factionId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MercsArmyListEntry>> GetMergedMercsArmyListAsync(
        int firstFactionId,
        int secondFactionId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MercsArmyListEntry>> GetMergedMercsArmyListAsync(
        IReadOnlyCollection<int> factionIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ArmyResumeRecord>> GetResumeByFactionMercsOnlyAsync(
        int factionId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ArmyResumeRecord>> GetResumeByFactionMercsOnlyAsync(
        int firstFactionId,
        int secondFactionId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ArmyResumeRecord>> GetResumeByFactionMercsOnlyAsync(
        IReadOnlyCollection<int> factionIds,
        CancellationToken cancellationToken = default);
}

public sealed class MercsArmyListEntry
{
    public required ArmyResumeRecord Resume { get; init; }

    public string? ProfileGroupsJson { get; init; }

    public IReadOnlyList<int> SourceFactionIds { get; init; } = [];
}
