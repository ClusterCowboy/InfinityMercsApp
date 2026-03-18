namespace InfinityMercsApp.Domain.Models.Army;

public class MercsArmyListEntry
{
    public required Resume Resume { get; init; }

    public string? ProfileGroupsJson { get; init; }

    public IReadOnlyList<int> SourceFactionIds { get; init; } = [];
}
