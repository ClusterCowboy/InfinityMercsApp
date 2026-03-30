namespace InfinityMercsApp.Domain.Models.Perks;

/// <summary>
/// Generic perk tree node that can be used by UI/state workflows without
/// coupling directly to persistence-specific perk models.
/// </summary>
public sealed class PerkNode
{
    /// <summary>
    /// Stable identifier for this node (for example: "cool-track-4-tier-2").
    /// </summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// Human-readable node name.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Tier level in a perk track (1..N).
    /// </summary>
    public int Tier { get; init; }

    /// <summary>
    /// Parent node id when this node has a dependency on another node.
    /// </summary>
    public string? ParentId { get; init; }

    /// <summary>
    /// Child perk nodes that can be reached from this node.
    /// </summary>
    public List<PerkNode> Children { get; } = [];

    /// <summary>
    /// Runtime-owned state (not persisted by this type).
    /// </summary>
    public bool IsOwned { get; set; }

    public string? MOV { get; set; } = string.Empty;
    public int? CC { get; set; } = 0;
    public int? BS { get; set; } = 0;
    public int? WIP { get; set; } = 0;
    public int? ARM { get; set; } = 0;
    public int? BTS { get; set; } = 0;
    public int? S { get; set; } = 2;
    public List<Tuple<string, string>> SkillsEquipmentGained { get; set; } = [];

    public bool HasChildren => Children.Count > 0;

    public void AddChild(PerkNode child)
    {
        ArgumentNullException.ThrowIfNull(child);
        Children.Add(child);
    }

    public IEnumerable<PerkNode> TraverseDepthFirst()
    {
        yield return this;
        foreach (var child in Children)
        {
            foreach (var descendant in child.TraverseDepthFirst())
            {
                yield return descendant;
            }
        }
    }

    public PerkNode? FindById(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        return TraverseDepthFirst()
            .FirstOrDefault(node => string.Equals(node.Id, id, StringComparison.OrdinalIgnoreCase));
    }
}

public sealed class PerkNodeList
{
    public string ListId { get; init; } = string.Empty;
    public string ListName { get; init; } = string.Empty;
    public bool IsRandomlyGenerated { get; init; }
    public IReadOnlyList<CompanyPerkRollRange> ListRollRanges { get; init; } = [];
    public IReadOnlyList<PerkNode> Roots { get; init; } = [];
}
