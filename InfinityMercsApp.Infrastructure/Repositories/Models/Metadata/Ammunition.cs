namespace InfinityMercsApp.Infrastructure.Repositories.Models.Metadata;

using SQLite;

[Table("ammunitions")]
public class Ammunition
{
    [PrimaryKey]
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Wiki { get; set; }
}
