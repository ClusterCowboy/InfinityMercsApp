namespace InfinityMercsApp.Infrastructure.Repositories.Models.Metadata;

using SQLite;

[Table("weapons")]
public class Weapon
{
    [PrimaryKey]
    public string WeaponKey { get; set; } = string.Empty;

    public int WeaponId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Type { get; set; }

    public string? Mode { get; set; }

    public string? Wiki { get; set; }

    public int? AmmunitionId { get; set; }

    public string? Burst { get; set; }

    public string? Damage { get; set; }

    public string? Saving { get; set; }

    public string? SavingNum { get; set; }

    public string? Profile { get; set; }

    public string? PropertiesJson { get; set; }

    public string? DistanceJson { get; set; }
}
