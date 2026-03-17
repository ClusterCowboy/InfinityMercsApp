using System.Collections.ObjectModel;
using InfinityMercsApp.ViewModels;
using InfinityMercsApp.Views.Templates.NewCompany;

namespace InfinityMercsApp.Views.StandardCompany;

public class ArmyFactionSelectionItem : BaseViewModel, IViewerListItem, ICompanySourceFaction
{
    public int Id { get; init; }

    public int ParentId { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? CachedLogoPath { get; init; }

    public string? PackagedLogoPath { get; init; }

    public string? Subtitle => null;

    public bool HasSubtitle => false;

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value)
            {
                return;
            }

            _isSelected = value;
            OnPropertyChanged();
        }
    }
}

public class ArmyUnitSelectionItem : BaseViewModel, IViewerListItem
{
    public int Id { get; init; }
    public int SourceFactionId { get; init; }
    public string? Slug { get; init; }

    public int? Type { get; init; }
    public bool IsCharacter { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? CachedLogoPath { get; init; }

    public string? PackagedLogoPath { get; init; }

    public string? Subtitle { get; init; }
    public bool IsSpecOps { get; init; }

    public bool HasSubtitle => !string.IsNullOrWhiteSpace(Subtitle);

    private bool _isVisible = true;
    public bool IsVisible
    {
        get => _isVisible;
        set
        {
            if (_isVisible == value)
            {
                return;
            }

            _isVisible = value;
            OnPropertyChanged();
        }
    }

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value)
            {
                return;
            }

            _isSelected = value;
            OnPropertyChanged();
        }
    }
}

public class ArmyTeamListItem : BaseViewModel
{
    public string Name { get; init; } = string.Empty;
    public string TeamCountsText { get; init; } = string.Empty;
    public bool HasTeamCounts => !string.IsNullOrWhiteSpace(TeamCountsText);
    public bool IsWildcardBucket { get; init; }
    public ObservableCollection<ArmyTeamUnitLimitItem> AllowedProfiles { get; init; } = [];

    private bool _isVisible = true;
    public bool IsVisible
    {
        get => _isVisible;
        set
        {
            if (_isVisible == value)
            {
                return;
            }

            _isVisible = value;
            OnPropertyChanged();
        }
    }

    private bool _isExpanded;
    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (_isExpanded == value)
            {
                return;
            }

            _isExpanded = value;
            OnPropertyChanged();
        }
    }
}

public class ArmyTeamUnitLimitItem : BaseViewModel, IViewerListItem
{
    public string Name { get; init; } = string.Empty;
    public string Min { get; init; } = "0";
    public string Max { get; init; } = "0";
    public string? Slug { get; init; }
    public bool IsCharacter { get; init; }
    public int? ResolvedUnitId { get; init; }
    public int? ResolvedSourceFactionId { get; init; }
    public string? CachedLogoPath { get; init; }
    public string? PackagedLogoPath { get; init; }
    public string? Subtitle { get; init; }
    public bool HasSubtitle => !string.IsNullOrWhiteSpace(Subtitle);

    private bool _isVisible = true;
    public bool IsVisible
    {
        get => _isVisible;
        set
        {
            if (_isVisible == value)
            {
                return;
            }

            _isVisible = value;
            OnPropertyChanged();
        }
    }

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value)
            {
                return;
            }

            _isSelected = value;
            OnPropertyChanged();
        }
    }
}

public class MercsCompanyEntry : BaseViewModel, IViewerListItem, ICompanyMercsEntry
{
    public string Name { get; init; } = string.Empty;
    public FormattedString? NameFormatted { get; init; }
    public string CostDisplay { get; init; } = string.Empty;
    public int CostValue { get; init; }
    public string ProfileKey { get; init; } = string.Empty;
    public bool IsLieutenant { get; init; }
    public int SourceUnitId { get; init; }
    public int SourceFactionId { get; init; }

    public string? CachedLogoPath { get; init; }

    public string? PackagedLogoPath { get; init; }

    private string? _subtitle;
    public string? Subtitle
    {
        get => _subtitle;
        set
        {
            if (_subtitle == value)
            {
                return;
            }

            _subtitle = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasSubtitle));
        }
    }
    public string UnitTypeCode { get; init; } = string.Empty;
    public string SavedEquipment { get; init; } = "-";
    public string SavedSkills { get; init; } = "-";
    public string SavedRangedWeapons { get; init; } = "-";
    public string SavedCcWeapons { get; init; } = "-";
    public int? UnitMoveFirstCm { get; init; }
    public int? UnitMoveSecondCm { get; init; }
    public string UnitMoveDisplay { get; set; } = "-";
    public bool HasPeripheralStatBlock { get; init; }
    public string PeripheralNameHeading { get; init; } = string.Empty;
    private string _peripheralMov = "-";
    public string PeripheralMov
    {
        get => _peripheralMov;
        set
        {
            if (_peripheralMov == value)
            {
                return;
            }

            _peripheralMov = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(PeripheralSubtitle));
        }
    }
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
    public int? PeripheralMoveFirstCm { get; init; }
    public int? PeripheralMoveSecondCm { get; init; }
    public string SavedPeripheralEquipment { get; init; } = "-";
    public string SavedPeripheralSkills { get; init; } = "-";
    public string PeripheralSubtitle => $"MOV {PeripheralMov} | CC {PeripheralCc} | BS {PeripheralBs} | PH {PeripheralPh} | WIP {PeripheralWip} | ARM {PeripheralArm} | BTS {PeripheralBts} | {PeripheralVitalityHeader} {PeripheralVitality} | S {PeripheralS} | AVA {PeripheralAva}";

    public bool HasSubtitle => !string.IsNullOrWhiteSpace(Subtitle);

    public FormattedString EquipmentLineFormatted { get; init; } = new();
    public bool HasEquipmentLine { get; init; }
    public FormattedString SkillsLineFormatted { get; init; } = new();
    public bool HasSkillsLine { get; init; }
    public FormattedString RangedLineFormatted { get; init; } = new();
    public FormattedString CcLineFormatted { get; init; } = new();
    public FormattedString PeripheralEquipmentLineFormatted { get; init; } = new();
    public bool HasPeripheralEquipmentLine { get; init; }
    public FormattedString PeripheralSkillsLineFormatted { get; init; } = new();
    public bool HasPeripheralSkillsLine { get; init; }
    private int _experiencePoints;
    public int ExperiencePoints
    {
        get => _experiencePoints;
        set
        {
            var normalized = Math.Max(0, value);
            if (_experiencePoints == normalized)
            {
                return;
            }

            _experiencePoints = normalized;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ExperienceRankName));
        }
    }

    public string ExperienceRankName => UnitExperienceRanks.GetRankName(ExperiencePoints);

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value)
            {
                return;
            }

            _isSelected = value;
            OnPropertyChanged();
        }
    }
}

public sealed class SavedCompanyFile
{
    public string CompanyName { get; init; } = string.Empty;
    public string CompanyType { get; init; } = string.Empty;
    public string CompanyIdentifier { get; init; } = string.Empty;
    public int CompanyIndex { get; init; }
    public string CreatedUtc { get; init; } = string.Empty;
    public int PointsLimit { get; init; }
    public int CurrentPoints { get; init; }
    public SavedImprovedCaptainStats ImprovedCaptainStats { get; init; } = new();
    public List<SavedCompanyFaction> SourceFactions { get; init; } = [];
    public List<SavedCompanyEntry> Entries { get; init; } = [];
}

public sealed class SavedImprovedCaptainStats
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

public sealed class SavedCompanyFaction
{
    public int FactionId { get; init; }
    public string FactionName { get; init; } = string.Empty;
}

public sealed class SavedCompanyEntry
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
    public string ExperienceRankName => UnitExperienceRanks.GetRankName(ExperiencePoints);
}

sealed class PeripheralMercsCompanyStats
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

public sealed class CaptainUpgradeOptionSet
{
    public static CaptainUpgradeOptionSet Empty { get; } = new();
    public List<string> Weapons { get; init; } = [];
    public List<string> Skills { get; init; } = [];
    public List<string> Equipment { get; init; } = [];
    public bool IsEmpty => Weapons.Count == 0 && Skills.Count == 0 && Equipment.Count == 0;
}

public static class UnitExperienceRanks
{
    private static readonly (int MinXp, string RankName)[] OrderedRanks =
    [
        (5, "Newbie"),
        (10, "Hired Gun"),
        (30, "Hitman"),
        (50, "Operative"),
        (75, "Veteran"),
        (105, "Officer"),
        (140, "Legend")
    ];

    public static string GetRankName(int experiencePoints)
    {
        var normalized = Math.Max(0, experiencePoints);
        for (var i = OrderedRanks.Length - 1; i >= 0; i--)
        {
            if (normalized >= OrderedRanks[i].MinXp)
            {
                return OrderedRanks[i].RankName;
            }
        }

        return "Unranked";
    }

    public static int GetRankLevel(int experiencePoints)
    {
        var normalized = Math.Max(0, experiencePoints);
        for (var i = OrderedRanks.Length - 1; i >= 0; i--)
        {
            if (normalized >= OrderedRanks[i].MinXp)
            {
                return i + 1;
            }
        }

        return 0;
    }
}
