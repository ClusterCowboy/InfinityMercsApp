namespace InfinityMercsApp.Infrastructure.Models.Database.Army;

using SQLite;

[Table("cc_faction_fireteam_validity")]
public class CCFactionFireteamValidity
{
    [PrimaryKey]
    public string CacheKey { get; set; } = string.Empty;

    public int FactionId { get; set; }

    public string FilterKey { get; set; } = string.Empty;

    public bool HasValidCoreFireteams { get; set; }

    public string? ValidCoreFireteamsJson { get; set; }

    public long EvaluatedAtUnixSeconds { get; set; }
}
