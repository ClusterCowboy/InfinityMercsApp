namespace InfinityMercsApp.Domain.Models.Metadata;

public class MartialArt
{
    public string Name { get; set; } = string.Empty;

    public string? Opponent { get; set; }

    public string? Damage { get; set; }

    public string? Attack { get; set; }

    public string? Burst { get; set; }
}
