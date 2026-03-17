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

public abstract class CompanySavedCompanyFactionBase
{
    public int FactionId { get; init; }
    public string FactionName { get; init; } = string.Empty;
}

public abstract class CompanySavedCompanyEntryBase
{
    public int EntryIndex { get; init; }
    public string Name { get; init; } = string.Empty;
    public string BaseUnitName { get; set; } = string.Empty;
    public string CustomName { get; set; } = string.Empty;
    public string UnitTypeCode { get; set; } = string.Empty;
    public string ProfileKey { get; init; } = string.Empty;
    public int SourceFactionId { get; init; }
    public int SourceUnitId { get; init; }
    public int Cost { get; init; }
    public bool IsLieutenant { get; init; }
    public string SavedEquipment { get; init; } = "-";
    public string SavedSkills { get; init; } = "-";
    public string SavedRangedWeapons { get; init; } = "-";
    public string SavedCcWeapons { get; init; } = "-";
    public bool HasPeripheralStatBlock { get; init; }
    public string PeripheralNameHeading { get; init; } = string.Empty;
    public string PeripheralMov { get; init; } = "-";
    public string PeripheralCc { get; init; } = "-";
    public string PeripheralBs { get; init; } = "-";
    public string PeripheralPh { get; init; } = "-";
    public string PeripheralWip { get; init; } = "-";
    public string PeripheralArm { get; init; } = "-";
    public string PeripheralBts { get; init; } = "-";
    public string PeripheralVitalityHeader { get; init; } = "VITA";
    public string PeripheralVitality { get; init; } = "-";
    public string PeripheralS { get; init; } = "-";
    public string PeripheralAva { get; init; } = "-";
    public string SavedPeripheralEquipment { get; init; } = "-";
    public string SavedPeripheralSkills { get; init; } = "-";
    public int ExperiencePoints { get; init; }
    public string ExperienceRankName => CompanyUnitExperienceRanks.GetRankName(ExperiencePoints);
}

public abstract class CompanySavedCompanyFileBase<TCaptainStats, TFaction, TEntry>
    where TCaptainStats : CompanySavedImprovedCaptainStatsBase, new()
    where TFaction : CompanySavedCompanyFactionBase, new()
    where TEntry : CompanySavedCompanyEntryBase, new()
{
    public string CompanyName { get; init; } = string.Empty;
    public string CompanyType { get; init; } = string.Empty;
    public string CompanyIdentifier { get; init; } = string.Empty;
    public int CompanyIndex { get; init; }
    public string CreatedUtc { get; init; } = string.Empty;
    public int PointsLimit { get; init; }
    public int CurrentPoints { get; init; }
    public TCaptainStats ImprovedCaptainStats { get; init; } = new();
    public List<TFaction> SourceFactions { get; init; } = [];
    public List<TEntry> Entries { get; init; } = [];
}

public abstract class CompanyPeripheralMercsCompanyStatsBase
{
    public string NameHeading { get; init; } = string.Empty;
    public int? MoveFirstCm { get; init; }
    public int? MoveSecondCm { get; init; }
    public string Mov { get; init; } = "-";
    public string Cc { get; init; } = "-";
    public string Bs { get; init; } = "-";
    public string Ph { get; init; } = "-";
    public string Wip { get; init; } = "-";
    public string Arm { get; init; } = "-";
    public string Bts { get; init; } = "-";
    public string VitalityHeader { get; init; } = "VITA";
    public string Vitality { get; init; } = "-";
    public string S { get; init; } = "-";
    public string Ava { get; init; } = "-";
    public string Equipment { get; init; } = "-";
    public string Skills { get; init; } = "-";
}

public abstract class CompanyCaptainUpgradeOptionSetBase
{
    public List<string> Weapons { get; init; } = [];
    public List<string> Skills { get; init; } = [];
    public List<string> Equipment { get; init; } = [];
    public bool IsEmpty => Weapons.Count == 0 && Skills.Count == 0 && Equipment.Count == 0;
}


