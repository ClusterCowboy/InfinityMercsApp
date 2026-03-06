namespace InfinityMercsApp.Infrastructure.Repositories.Models.Metadata;

using SQLite;

[Table("equipment")]
public class Equipments
{
    [PrimaryKey]
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Wiki { get; set; }
}
