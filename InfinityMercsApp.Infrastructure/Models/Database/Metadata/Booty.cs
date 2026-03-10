namespace InfinityMercsApp.Infrastructure.Models.Database.Metadata;

using SQLite;

[Table("bootys")]
public class Booty
{
    [PrimaryKey]
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;
}
