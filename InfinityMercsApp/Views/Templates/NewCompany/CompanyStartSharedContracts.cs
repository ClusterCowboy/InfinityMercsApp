namespace InfinityMercsApp.Views.Templates.NewCompany;

public interface ICompanySourceFaction
{
    int Id { get; }
    int ParentId { get; }
    string Name { get; }
}

public interface ICompanyMercsEntry
{
    string Name { get; }
    int CostValue { get; }
    bool IsLieutenant { get; }
    string UnitTypeCode { get; }
    string ProfileKey { get; }
    int SourceFactionId { get; }
    int SourceUnitId { get; }
    string SavedEquipment { get; }
    string SavedSkills { get; }
    string SavedRangedWeapons { get; }
    string SavedCcWeapons { get; }
    bool HasPeripheralStatBlock { get; }
    string PeripheralNameHeading { get; }
    string PeripheralMov { get; set; }
    string PeripheralCc { get; }
    string PeripheralBs { get; }
    string PeripheralPh { get; }
    string PeripheralWip { get; }
    string PeripheralArm { get; }
    string PeripheralBts { get; }
    string PeripheralVitalityHeader { get; }
    string PeripheralVitality { get; }
    string PeripheralS { get; }
    string PeripheralAva { get; }
    string SavedPeripheralEquipment { get; }
    string SavedPeripheralSkills { get; }
    int ExperiencePoints { get; }
    int? UnitMoveFirstCm { get; }
    int? UnitMoveSecondCm { get; }
    string UnitMoveDisplay { get; set; }
    string? Subtitle { get; set; }
    int? PeripheralMoveFirstCm { get; }
    int? PeripheralMoveSecondCm { get; }
    string? CachedLogoPath { get; }
    string? PackagedLogoPath { get; }
}
