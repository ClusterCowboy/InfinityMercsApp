using InfinityMercsApp.Infrastructure.Models.Database.Army;

namespace InfinityMercsApp.Infrastructure.Providers;

/// <summary>
/// Accessor that builds Mercs-eligible army unit lists for one or more source factions.
/// </summary>
public interface IMercsArmyListProvider
{
    /// <summary>
    /// Gets merged lists for a single faction.
    /// </summary>
    /// <param name="factionId"></param>
    /// <returns></returns>
    IReadOnlyList<MercsArmyListEntry> GetMergedMercsArmyList(int factionId);

    /// <summary>
    /// Gets merged lists for two factions.
    /// Do we need this? This seems like it could be fully replaced by the method below.
    /// </summary>
    /// <param name="firstFactionId"></param>
    /// <param name="secondFactionId"></param>
    /// <returns></returns>
    IReadOnlyList<MercsArmyListEntry> GetMergedMercsArmyList(
        int firstFactionId,
        int secondFactionId);

    /// <summary>
    /// Gets merged lists for an arbitrary number of factions.
    /// </summary>
    /// <param name="factionIds"></param>
    /// <returns></returns>
    IReadOnlyList<MercsArmyListEntry> GetMergedMercsArmyList(IReadOnlyCollection<int> factionIds);

    /// <summary>
    /// Gets resumes for a single faction.
    /// </summary>
    /// <param name="factionId"></param>
    /// <returns></returns>
    IReadOnlyList<Resume> GetResumeByFactionMercsOnly(int factionId);

    /// <summary>
    /// Gets resumes for two factions only.
    /// Do we need this? This seems like it could be fully replaced by the method below.
    /// </summary>
    /// <param name="firstFactionId"></param>
    /// <param name="secondFactionId"></param>
    /// <returns></returns>
    IReadOnlyList<Resume> GetResumeByFactionMercsOnly(
        int firstFactionId,
        int secondFactionId);

    /// <summary>
    /// Gets resumes for an arbitrary number of factions.
    /// </summary>
    /// <param name="factionIds"></param>
    /// <returns></returns>
    IReadOnlyList<Resume> GetResumeByFactionMercsOnly(IReadOnlyCollection<int> factionIds);
}

// TODO: Punt this to Domain.Models so we're not returning DB objects
// Can't do this for now because we don't want SQLite attributes in Domain.Models
// And Domain.Models isn't fully configured.
public sealed class MercsArmyListEntry
{
    public required Resume Resume { get; init; }

    public string? ProfileGroupsJson { get; init; }

    public IReadOnlyList<int> SourceFactionIds { get; init; } = [];
}
