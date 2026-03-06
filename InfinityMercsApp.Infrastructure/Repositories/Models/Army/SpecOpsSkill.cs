namespace InfinityMercsApp.Infrastructure.Repositories.Models.Army;

using SQLite;

[Table("specops_skills")]
public class SpecopsSkill
{
    [PrimaryKey]
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