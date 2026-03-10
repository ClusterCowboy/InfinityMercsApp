namespace InfinityMercsApp.Domain.Models.Metadata;

public class Faction
{
    public int Id { get; set; }

    public int ParentId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public bool Discontinued { get; set; }

    public string? Logo { get; set; }
}
