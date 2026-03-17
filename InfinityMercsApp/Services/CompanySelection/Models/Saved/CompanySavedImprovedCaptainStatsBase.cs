namespace InfinityMercsApp.Views.Common;

public abstract class CompanySavedImprovedCaptainStatsBase
{
    public bool IsEnabled { get; init; }
    public string CaptainName { get; init; } = string.Empty;
    public int CcTier { get; init; }
    public int BsTier { get; init; }
    public int PhTier { get; init; }
    public int WipTier { get; init; }
    public int ArmTier { get; init; }
    public int BtsTier { get; init; }
    public int VitalityTier { get; init; }
    public int CcBonus { get; init; }
    public int BsBonus { get; init; }
    public int PhBonus { get; init; }
    public int WipBonus { get; init; }
    public int ArmBonus { get; init; }
    public int BtsBonus { get; init; }
    public int VitalityBonus { get; init; }
    public string WeaponChoice1 { get; init; } = string.Empty;
    public string WeaponChoice2 { get; init; } = string.Empty;
    public string WeaponChoice3 { get; init; } = string.Empty;
    public string SkillChoice1 { get; init; } = string.Empty;
    public string SkillChoice2 { get; init; } = string.Empty;
    public string SkillChoice3 { get; init; } = string.Empty;
    public string EquipmentChoice1 { get; init; } = string.Empty;
    public string EquipmentChoice2 { get; init; } = string.Empty;
    public string EquipmentChoice3 { get; init; } = string.Empty;
    public int OptionFactionId { get; init; }
    public string OptionFactionName { get; init; } = string.Empty;
}
