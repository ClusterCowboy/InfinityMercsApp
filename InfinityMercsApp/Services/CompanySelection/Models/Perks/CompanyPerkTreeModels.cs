namespace InfinityMercsApp.Views.Common;

public sealed class CompanyPerkTreeNode
{
    public string Id { get; init; } = string.Empty;
    public int Tier { get; init; }
    public string PerkText { get; init; } = string.Empty;
    public int? RequiredTier { get; init; }
    public List<CompanyPerkTreeNode> Children { get; init; } = [];
}

public sealed class CompanyPerkTrackTree
{
    public string ListId { get; init; } = string.Empty;
    public string ListName { get; init; } = string.Empty;
    public int TrackNumber { get; init; }
    public List<CompanyPerkRollRange> RollRanges { get; init; } = [];
    public List<CompanyPerkTreeNode> Roots { get; init; } = [];
}
