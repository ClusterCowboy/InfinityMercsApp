using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Maui.Controls;

namespace InfinityMercsApp.ViewModels;

public interface IViewerListItem
{
    string Name { get; }

    string? CachedLogoPath { get; }

    string? PackagedLogoPath { get; }

    string? Subtitle { get; }

    bool HasSubtitle { get; }

    bool IsSelected { get; set; }
}

public class ViewerFactionItem : BaseViewModel, IViewerListItem
{
    public int Id { get; init; }

    public int ParentId { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? Logo { get; init; }

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

public class ViewerUnitItem : BaseViewModel, IViewerListItem
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? Logo { get; init; }

    public string? CachedLogoPath { get; init; }

    public string? PackagedLogoPath { get; init; }

    public string? Subtitle { get; init; }

    public bool HasSubtitle => !string.IsNullOrWhiteSpace(Subtitle);

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

public class FireteamTeamItem
{
    public string Name { get; init; } = string.Empty;
    public string TeamTypes { get; init; } = "-";
    public IReadOnlyList<FireteamUnitLimitItem> UnitLimits { get; init; } = [];
}

public class FireteamUnitLimitItem
{
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string Min { get; init; } = "0";
    public string Max { get; init; } = "0";
}

public class ViewerProfileItem : BaseViewModel
{
    public string GroupName { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string ProfileKey { get; init; } = string.Empty;

    public bool IsLieutenant { get; init; }

    public FormattedString? NameFormatted { get; init; }

    public string RangedWeapons { get; init; } = "-";
    public FormattedString? RangedWeaponsFormatted { get; init; }

    public string MeleeWeapons { get; init; } = "-";
    public FormattedString? MeleeWeaponsFormatted { get; init; }

    public string UniqueEquipment { get; init; } = "-";
    public FormattedString? UniqueEquipmentFormatted { get; init; }

    public string UniqueSkills { get; init; } = "-";
    public FormattedString? UniqueSkillsFormatted { get; init; }

    public string Characteristics { get; init; } = "-";

    public string Peripherals { get; init; } = "-";
    public FormattedString? PeripheralsFormatted { get; init; }

    public bool HasPeripherals => !string.IsNullOrWhiteSpace(Peripherals) && Peripherals != "-";
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
    public string PeripheralSubtitle { get; init; } = "-";
    public FormattedString PeripheralEquipmentLineFormatted { get; init; } = new();
    public bool HasPeripheralEquipmentLine { get; init; }
    public FormattedString PeripheralSkillsLineFormatted { get; init; } = new();
    public bool HasPeripheralSkillsLine { get; init; }
    public bool PeripheralGrantsFtMaster { get; init; }
    public bool PeripheralIsIrregular { get; init; }
    public string PeripheralUnitName { get; init; } = string.Empty;

    public string Swc { get; init; } = "-";

    public string SwcDisplay { get; init; } = string.Empty;

    public string Cost { get; init; } = "-";
    public bool ShowProfileTacticalAwarenessIcon { get; init; }

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

    private bool _isLieutenantBlocked;
    public bool IsLieutenantBlocked
    {
        get => _isLieutenantBlocked;
        set
        {
            if (_isLieutenantBlocked == value)
            {
                return;
            }

            _isLieutenantBlocked = value;
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

