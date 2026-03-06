namespace InfinityMercsApp.Infrastructure.Repositories.Models.Metadata;

using SQLite;

[Table("bootys")]
public class BootyRecord
{
    [PrimaryKey]
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;
}
