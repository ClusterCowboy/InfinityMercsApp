namespace InfinityMercsApp.Domain.Models.Metadata;

public class HackingProgram
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Opponent { get; set; }

    public string? Special { get; set; }

    public string? Damage { get; set; }

    public string? Attack { get; set; }

    public string? Burst { get; set; }

    public int? Extra { get; set; }

    public string? SkillTypeJson { get; set; }

    public string? DevicesJson { get; set; }

    public string? TargetJson { get; set; }
}
