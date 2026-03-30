namespace InfinityMercsApp.Domain.Models.Perks;

public sealed class CompanyPerkRollRange
{
    public int Min { get; init; }
    public int Max { get; init; }

    public bool Contains(int roll)
    {
        return roll >= Min && roll <= Max;
    }

    public override string ToString()
    {
        return Min == Max ? Min.ToString() : $"{Min}-{Max}";
    }
}

public sealed class CompanyPerkTierDefinition
{
    public string Id { get; init; } = string.Empty;
    public int Tier { get; init; }
    public string PerkText { get; init; } = string.Empty;
    public bool RequiresPreviousTier { get; init; }
    public bool IsEmpty => string.IsNullOrWhiteSpace(PerkText);
}

public sealed class CompanyPerkTrackDefinition
{
    public int TrackNumber { get; init; }
    public List<CompanyPerkRollRange> RollRanges { get; init; } = [];
    public List<CompanyPerkTierDefinition> Tiers { get; init; } = [];
}

public sealed class CompanyPerkListDefinition
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public bool IsRandomlyGenerated { get; init; }
    public List<CompanyPerkRollRange> ListRollRanges { get; init; } = [];
    public List<CompanyPerkTrackDefinition> Tracks { get; init; } = [];
}

public sealed class CompanyPerkRollOption
{
    public string ListId { get; init; } = string.Empty;
    public string ListName { get; init; } = string.Empty;
    public int TrackNumber { get; init; }
    public int Tier { get; init; }
    public string PerkText { get; init; } = string.Empty;
    public int? RequiredTier { get; init; }
}
