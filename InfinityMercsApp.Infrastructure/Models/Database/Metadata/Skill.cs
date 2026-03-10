namespace InfinityMercsApp.Infrastructure.Models.Database.Metadata;

using SQLite;

[Table("skills")]
public class Skill
{
    [PrimaryKey]
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Wiki { get; set; }
}
