using System.Text.Json.Serialization;

namespace InfinityMercsApp.Views.Common;

public sealed class CompanyTrooperPerk
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; init; } = string.Empty;

    [JsonPropertyName("maxRank")]
    public int MaxRank { get; init; } = 1;
}

public sealed class CompanyTrooperPerkState
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("rank")]
    public int Rank { get; init; } = 1;
}
