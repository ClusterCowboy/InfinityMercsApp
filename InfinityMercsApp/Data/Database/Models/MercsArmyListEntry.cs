namespace InfinityMercsApp.Data.Database;

public sealed class MercsArmyListEntry
{
    public required ArmyResumeRecord Resume { get; init; }

    public string? ProfileGroupsJson { get; init; }

    public IReadOnlyList<int> SourceFactionIds { get; init; } = [];
}
