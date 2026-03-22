using InfinityMercsApp.ViewModels;

namespace InfinityMercsApp.Views.Common;

public abstract class CompanyMercsCompanyEntryBase : BaseViewModel, IViewerListItem, ICompanyMercsEntry
{
    public string Name { get; init; } = string.Empty;
    public string BaseUnitName { get; init; } = string.Empty;
    public FormattedString? NameFormatted { get; init; }
    public string CostDisplay { get; init; } = string.Empty;
    public int CostValue { get; init; }
    public string ProfileKey { get; init; } = string.Empty;
    public bool IsLieutenant { get; init; }
    public int SourceUnitId { get; init; }
    public int SourceFactionId { get; init; }
    public int? LogoSourceFactionId { get; init; }
    public int? LogoSourceUnitId { get; init; }

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

    public string ExperienceRankName => CompanyUnitExperienceRanks.GetRankName(ExperiencePoints);

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

    private bool _isIrregular;
    public bool IsIrregular
    {
        get => _isIrregular;
        set
        {
            if (_isIrregular == value)
            {
                return;
            }

            _isIrregular = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ShowRegularModifier));
        }
    }

    private bool _normallyIrregular;
    public bool NormallyIrregular
    {
        get => _normallyIrregular;
        set
        {
            if (_normallyIrregular == value)
            {
                return;
            }

            _normallyIrregular = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ShowRegularModifier));
        }
    }

    public bool ShowRegularModifier => NormallyIrregular && !IsIrregular;
}


