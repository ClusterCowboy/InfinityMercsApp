using System.Text.Json.Serialization;

namespace InfinityMercsApp.Views.Common;

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
    public int LogoSourceFactionId { get; init; }
    public int LogoSourceUnitId { get; init; }
    public bool IsPeripheralUnit { get; init; }
    public int? ParentEntryIndex { get; init; }
    public int Cost { get; init; }
    public bool IsLieutenant { get; init; }

    [JsonIgnore]
    public string SavedEquipment { get; init; } = "-";
    [JsonIgnore]
    public string SavedSkills { get; init; } = "-";
    [JsonIgnore]
    public string SavedRangedWeapons { get; init; } = "-";
    [JsonIgnore]
    public string SavedCcWeapons { get; init; } = "-";

    [JsonPropertyName("CurrentSkillCodes")]
    public List<CompanySavedCodeRef> CurrentSkillCodes { get; init; } = [];
    [JsonPropertyName("CurrentCharacteristicCodes")]
    public List<CompanySavedCodeRef> CurrentCharacteristicCodes { get; init; } = [];
    [JsonPropertyName("CurrentEquipmentCodes")]
    public List<CompanySavedCodeRef> CurrentEquipmentCodes { get; init; } = [];
    [JsonPropertyName("CurrentWeaponCodes")]
    public List<CompanySavedCodeRef> CurrentWeaponCodes { get; init; } = [];
    [JsonPropertyName("Custom Skills")]
    public List<string> CustomSkills { get; init; } = [];
    [JsonPropertyName("Custom Characteristics")]
    public List<string> CustomCharacteristics { get; init; } = [];
    [JsonPropertyName("Custom Equipment")]
    public List<string> CustomEquipment { get; init; } = [];
    [JsonPropertyName("Custom Weapons")]
    public List<string> CustomWeapons { get; init; } = [];

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

public sealed class CompanySavedCodeRef
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("extra")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<int>? Extra { get; init; }
}
