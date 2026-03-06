namespace InfinityMercsApp.Infrastructure.Repositories.Models.Metadata;

using SQLite;

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
}
