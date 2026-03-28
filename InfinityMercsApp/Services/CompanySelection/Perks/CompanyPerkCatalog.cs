namespace InfinityMercsApp.Views.Common;

/// <summary>
/// Baseline backend catalog for trooper perks.
/// UI and game rules can evolve this list over time without changing save shape.
/// </summary>
public static class CompanyPerkCatalog
{
    // Initial baseline set; values can be tuned as design solidifies.
    private static readonly IReadOnlyList<CompanyTrooperPerk> AllPerksInternal =
    [
        new CompanyTrooperPerk
        {
            Id = "hardened",
            Name = "Hardened",
            Description = "This trooper is harder to put down and shrugs off punishment.",
            MaxRank = 2
        },
        new CompanyTrooperPerk
        {
            Id = "gunslinger",
            Name = "Gunslinger",
            Description = "This trooper has improved handling with ranged weapons.",
            MaxRank = 2
        },
        new CompanyTrooperPerk
        {
            Id = "combat-instinct",
            Name = "Combat Instinct",
            Description = "This trooper reacts faster and more decisively in close engagements.",
            MaxRank = 1
        }
    ];

    public static IReadOnlyList<CompanyTrooperPerk> AllPerks => AllPerksInternal;

    public static CompanyTrooperPerk? FindById(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        return AllPerksInternal.FirstOrDefault(x =>
            string.Equals(x.Id, id.Trim(), StringComparison.OrdinalIgnoreCase));
    }
}
