namespace InfinityMercsApp.Infrastructure.Repositories.Models.Army;

using SQLite;

[Table("resumes")]
public class Resume
{
    [PrimaryKey]
    public string ResumeKey { get; set; } = string.Empty;

    public int FactionId { get; set; }

    public int UnitId { get; set; }

    public int? IdArmy { get; set; }

    public string? Isc { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Slug { get; set; }

    public string? Logo { get; set; }

    public int? Type { get; set; }

    public int? Category { get; set; }
}