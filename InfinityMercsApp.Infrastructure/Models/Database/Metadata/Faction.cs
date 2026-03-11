namespace InfinityMercsApp.Infrastructure.Models.Database.Metadata;

using SQLite;
using System.Text.RegularExpressions;

[Table("factions")]
public class Faction
{
    [PrimaryKey]
    public int Id { get; set; }

    public int ParentId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public bool Discontinued { get; set; }

    public string? Logo { get; set; }

    public bool IsNonAlignedArmyName => DetermineIsNonAlignedArmyName(Name);

    public bool IsContractedBackUpName => DetermineIsContractedBackUpName(Name);

    // TODO: Shift these to Domain.Models when classes for the view change.
    private static bool DetermineIsNonAlignedArmyName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        return name.Trim().Equals("Non-Aligned Armies", StringComparison.OrdinalIgnoreCase);
    }

    private static bool DetermineIsContractedBackUpName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        var normalized = Regex.Replace(name.Trim(), @"[\s\-]+", " ").ToLowerInvariant();
        return normalized == "contracted back up";
    }
}
