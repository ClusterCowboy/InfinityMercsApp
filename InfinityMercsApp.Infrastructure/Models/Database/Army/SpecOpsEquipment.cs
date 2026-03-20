namespace InfinityMercsApp.Infrastructure.Models.Database.Army;

using SQLite;

[Table("specops_equipment")]
public class SpecopsEquipment
{
    [PrimaryKey]
    public string SpecopsEquipmentKey { get; set; } = string.Empty;

    public int FactionId { get; set; }

    public int EntryOrder { get; set; }

    public int EquipmentId { get; set; }

    public int Exp { get; set; }

    public string? ExtrasJson { get; set; }

    public string? SkillsJson { get; set; }

    public string? WeaponsJson { get; set; }

    public string RawJson { get; set; } = string.Empty;
}