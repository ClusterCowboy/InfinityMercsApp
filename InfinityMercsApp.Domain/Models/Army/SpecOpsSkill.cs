namespace InfinityMercsApp.Domain.Models.Army;

public class SpecopsSkill
{
    public string SpecopsSkillKey { get; set; } = string.Empty;

    public int FactionId { get; set; }

    public int EntryOrder { get; set; }

    public int SkillId { get; set; }

    public int Exp { get; set; }

    public string? ExtrasJson { get; set; }

    public string? EquipJson { get; set; }

    public string? WeaponsJson { get; set; }

    public string RawJson { get; set; } = string.Empty;
}