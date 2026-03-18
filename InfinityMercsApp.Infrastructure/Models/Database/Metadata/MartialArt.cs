namespace InfinityMercsApp.Infrastructure.Models.Database.Metadata;

using SQLite;

[Table("martial_arts")]
public class MartialArt
{
    [PrimaryKey]
    public string Name { get; set; } = string.Empty;

    public string? Opponent { get; set; }

    public string? Damage { get; set; }

    public string? Attack { get; set; }

    public string? Burst { get; set; }
}
