using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using SkiaSharp;
using System.Globalization;
using System.Linq;

namespace InfinityMercsApp.Views.Controls;

/// <summary>
/// Shared selected-unit panel that forwards canvas/size events to hosting pages.
/// </summary>
public partial class UnitDisplayConfigurationsView : ContentView
{
    private const int MaxHeaderIcons = 6;
    private const float IconSize = 24f;
    private const float IconVerticalGap = 5f;
    private const float IconHorizontalGap = 6f;
    private const double DefaultUnitHeadingMaxFontSize = 24d;
    private const double DefaultUnitHeadingMinFontSize = 11d;
    private const double DefaultUnitHeadingFontStep = 0.5d;
    private const string DefaultStatValue = "-";
    private const string DefaultVitalityHeader = "VITA";
    public static readonly Color DefaultHeaderPrimaryColor = Color.FromArgb("#B91C1C");
    public static readonly Color DefaultHeaderSecondaryColor = Color.FromArgb("#7F1D1D");
    public static readonly Color EquipmentAccentOnDarkSecondary = Color.FromArgb("#67E8F9");
    public static readonly Color SkillsAccentOnDarkSecondary = Color.FromArgb("#FDE68A");
    public static readonly Color EquipmentAccentOnLightSecondary = Color.FromArgb("#0B5563");
    public static readonly Color SkillsAccentOnLightSecondary = Color.FromArgb("#7C2D12");
    private sealed record HeaderIconRenderItem(SKPicture Picture);
    private double _profilesPanLastTotalY;

    /// <summary>
    /// Unit MOV value shown in the primary statline.
    /// </summary>
    public static readonly BindableProperty UnitMovProperty =
        BindableProperty.Create(nameof(UnitMov), typeof(string), typeof(UnitDisplayConfigurationsView), DefaultStatValue);
    public static readonly BindableProperty UnitCcProperty =
        BindableProperty.Create(nameof(UnitCc), typeof(string), typeof(UnitDisplayConfigurationsView), DefaultStatValue);
    public static readonly BindableProperty UnitBsProperty =
        BindableProperty.Create(nameof(UnitBs), typeof(string), typeof(UnitDisplayConfigurationsView), DefaultStatValue);
    public static readonly BindableProperty UnitPhProperty =
        BindableProperty.Create(nameof(UnitPh), typeof(string), typeof(UnitDisplayConfigurationsView), DefaultStatValue);
    public static readonly BindableProperty UnitWipProperty =
        BindableProperty.Create(nameof(UnitWip), typeof(string), typeof(UnitDisplayConfigurationsView), DefaultStatValue);
    public static readonly BindableProperty UnitArmProperty =
        BindableProperty.Create(nameof(UnitArm), typeof(string), typeof(UnitDisplayConfigurationsView), DefaultStatValue);
    public static readonly BindableProperty UnitBtsProperty =
        BindableProperty.Create(nameof(UnitBts), typeof(string), typeof(UnitDisplayConfigurationsView), DefaultStatValue);
    public static readonly BindableProperty UnitVitalityHeaderProperty =
        BindableProperty.Create(nameof(UnitVitalityHeader), typeof(string), typeof(UnitDisplayConfigurationsView), DefaultVitalityHeader);
    public static readonly BindableProperty UnitVitalityProperty =
        BindableProperty.Create(nameof(UnitVitality), typeof(string), typeof(UnitDisplayConfigurationsView), DefaultStatValue);
    public static readonly BindableProperty UnitSProperty =
        BindableProperty.Create(nameof(UnitS), typeof(string), typeof(UnitDisplayConfigurationsView), DefaultStatValue);
    public static readonly BindableProperty UnitAvaProperty =
        BindableProperty.Create(nameof(UnitAva), typeof(string), typeof(UnitDisplayConfigurationsView), DefaultStatValue);
    public static readonly BindableProperty HasPeripheralStatBlockProperty =
        BindableProperty.Create(nameof(HasPeripheralStatBlock), typeof(bool), typeof(UnitDisplayConfigurationsView), false);
    public static readonly BindableProperty PeripheralNameHeadingProperty =
        BindableProperty.Create(nameof(PeripheralNameHeading), typeof(string), typeof(UnitDisplayConfigurationsView), string.Empty);
    public static readonly BindableProperty PeripheralMovProperty =
        BindableProperty.Create(nameof(PeripheralMov), typeof(string), typeof(UnitDisplayConfigurationsView), DefaultStatValue);
    public static readonly BindableProperty PeripheralCcProperty =
        BindableProperty.Create(nameof(PeripheralCc), typeof(string), typeof(UnitDisplayConfigurationsView), DefaultStatValue);
    public static readonly BindableProperty PeripheralBsProperty =
        BindableProperty.Create(nameof(PeripheralBs), typeof(string), typeof(UnitDisplayConfigurationsView), DefaultStatValue);
    public static readonly BindableProperty PeripheralPhProperty =
        BindableProperty.Create(nameof(PeripheralPh), typeof(string), typeof(UnitDisplayConfigurationsView), DefaultStatValue);
    public static readonly BindableProperty PeripheralWipProperty =
        BindableProperty.Create(nameof(PeripheralWip), typeof(string), typeof(UnitDisplayConfigurationsView), DefaultStatValue);
    public static readonly BindableProperty PeripheralArmProperty =
        BindableProperty.Create(nameof(PeripheralArm), typeof(string), typeof(UnitDisplayConfigurationsView), DefaultStatValue);
    public static readonly BindableProperty PeripheralBtsProperty =
        BindableProperty.Create(nameof(PeripheralBts), typeof(string), typeof(UnitDisplayConfigurationsView), DefaultStatValue);
    public static readonly BindableProperty PeripheralVitalityHeaderProperty =
        BindableProperty.Create(nameof(PeripheralVitalityHeader), typeof(string), typeof(UnitDisplayConfigurationsView), DefaultVitalityHeader);
    public static readonly BindableProperty PeripheralVitalityProperty =
        BindableProperty.Create(nameof(PeripheralVitality), typeof(string), typeof(UnitDisplayConfigurationsView), DefaultStatValue);
    public static readonly BindableProperty PeripheralSProperty =
        BindableProperty.Create(nameof(PeripheralS), typeof(string), typeof(UnitDisplayConfigurationsView), DefaultStatValue);
    public static readonly BindableProperty PeripheralAvaProperty =
        BindableProperty.Create(nameof(PeripheralAva), typeof(string), typeof(UnitDisplayConfigurationsView), DefaultStatValue);
    public static readonly BindableProperty SelectedUnitPictureProperty =
        BindableProperty.Create(nameof(SelectedUnitPicture), typeof(SKPicture), typeof(UnitDisplayConfigurationsView), null);
    public static readonly BindableProperty RegularOrderIconPictureProperty =
        BindableProperty.Create(nameof(RegularOrderIconPicture), typeof(SKPicture), typeof(UnitDisplayConfigurationsView), null);
    public static readonly BindableProperty IrregularOrderIconPictureProperty =
        BindableProperty.Create(nameof(IrregularOrderIconPicture), typeof(SKPicture), typeof(UnitDisplayConfigurationsView), null);
    public static readonly BindableProperty LieutenantIconPictureProperty =
        BindableProperty.Create(nameof(LieutenantIconPicture), typeof(SKPicture), typeof(UnitDisplayConfigurationsView), null);
    public static readonly BindableProperty ImpetuousIconPictureProperty =
        BindableProperty.Create(nameof(ImpetuousIconPicture), typeof(SKPicture), typeof(UnitDisplayConfigurationsView), null);
    public static readonly BindableProperty TacticalAwarenessIconPictureProperty =
        BindableProperty.Create(nameof(TacticalAwarenessIconPicture), typeof(SKPicture), typeof(UnitDisplayConfigurationsView), null);
    public static readonly BindableProperty CubeIconPictureProperty =
        BindableProperty.Create(nameof(CubeIconPicture), typeof(SKPicture), typeof(UnitDisplayConfigurationsView), null);
    public static readonly BindableProperty Cube2IconPictureProperty =
        BindableProperty.Create(nameof(Cube2IconPicture), typeof(SKPicture), typeof(UnitDisplayConfigurationsView), null);
    public static readonly BindableProperty HackableIconPictureProperty =
        BindableProperty.Create(nameof(HackableIconPicture), typeof(SKPicture), typeof(UnitDisplayConfigurationsView), null);
    public static readonly BindableProperty PeripheralIconPictureProperty =
        BindableProperty.Create(nameof(PeripheralIconPicture), typeof(SKPicture), typeof(UnitDisplayConfigurationsView), null);
    public static readonly BindableProperty ShowRegularOrderIconProperty =
        BindableProperty.Create(nameof(ShowRegularOrderIcon), typeof(bool), typeof(UnitDisplayConfigurationsView), false, propertyChanged: OnHeaderIconVisibilityChanged);
    public static readonly BindableProperty ShowIrregularOrderIconProperty =
        BindableProperty.Create(nameof(ShowIrregularOrderIcon), typeof(bool), typeof(UnitDisplayConfigurationsView), false, propertyChanged: OnHeaderIconVisibilityChanged);
    public static readonly BindableProperty ShowLieutenantIconProperty =
        BindableProperty.Create(nameof(ShowLieutenantIcon), typeof(bool), typeof(UnitDisplayConfigurationsView), false, propertyChanged: OnHeaderIconVisibilityChanged);
    public static readonly BindableProperty ShowPeripheralIconProperty =
        BindableProperty.Create(nameof(ShowPeripheralIcon), typeof(bool), typeof(UnitDisplayConfigurationsView), false, propertyChanged: OnHeaderIconVisibilityChanged);
    public static readonly BindableProperty LieutenantIconCountProperty =
        BindableProperty.Create(nameof(LieutenantIconCount), typeof(int), typeof(UnitDisplayConfigurationsView), 0, propertyChanged: OnHeaderIconVisibilityChanged);
    public static readonly BindableProperty ShowImpetuousIconProperty =
        BindableProperty.Create(nameof(ShowImpetuousIcon), typeof(bool), typeof(UnitDisplayConfigurationsView), false, propertyChanged: OnHeaderIconVisibilityChanged);
    public static readonly BindableProperty ShowTacticalAwarenessIconProperty =
        BindableProperty.Create(nameof(ShowTacticalAwarenessIcon), typeof(bool), typeof(UnitDisplayConfigurationsView), false, propertyChanged: OnHeaderIconVisibilityChanged);
    public static readonly BindableProperty ShowCubeIconProperty =
        BindableProperty.Create(nameof(ShowCubeIcon), typeof(bool), typeof(UnitDisplayConfigurationsView), false, propertyChanged: OnHeaderIconVisibilityChanged);
    public static readonly BindableProperty ShowCube2IconProperty =
        BindableProperty.Create(nameof(ShowCube2Icon), typeof(bool), typeof(UnitDisplayConfigurationsView), false, propertyChanged: OnHeaderIconVisibilityChanged);
    public static readonly BindableProperty ShowHackableIconProperty =
        BindableProperty.Create(nameof(ShowHackableIcon), typeof(bool), typeof(UnitDisplayConfigurationsView), false, propertyChanged: OnHeaderIconVisibilityChanged);
    public static readonly BindableProperty ShowUnitsInInchesProperty =
        BindableProperty.Create(nameof(ShowUnitsInInches), typeof(bool), typeof(UnitDisplayConfigurationsView), true);
    public static readonly BindableProperty UnitMoveFirstCmProperty =
        BindableProperty.Create(nameof(UnitMoveFirstCm), typeof(int?), typeof(UnitDisplayConfigurationsView), null);
    public static readonly BindableProperty UnitMoveSecondCmProperty =
        BindableProperty.Create(nameof(UnitMoveSecondCm), typeof(int?), typeof(UnitDisplayConfigurationsView), null);
    public static readonly BindableProperty PeripheralMoveFirstCmProperty =
        BindableProperty.Create(nameof(PeripheralMoveFirstCm), typeof(int?), typeof(UnitDisplayConfigurationsView), null);
    public static readonly BindableProperty PeripheralMoveSecondCmProperty =
        BindableProperty.Create(nameof(PeripheralMoveSecondCm), typeof(int?), typeof(UnitDisplayConfigurationsView), null);
    public static readonly BindableProperty UnitNameHeadingProperty =
        BindableProperty.Create(nameof(UnitNameHeading), typeof(string), typeof(UnitDisplayConfigurationsView), "Select a unit");
    public static readonly BindableProperty UnitNameHeadingFontSizeProperty =
        BindableProperty.Create(nameof(UnitNameHeadingFontSize), typeof(double), typeof(UnitDisplayConfigurationsView), 24d);
    public static readonly BindableProperty UnitHeaderPrimaryColorProperty =
        BindableProperty.Create(nameof(UnitHeaderPrimaryColor), typeof(Color), typeof(UnitDisplayConfigurationsView), DefaultHeaderPrimaryColor);
    public static readonly BindableProperty UnitHeaderSecondaryColorProperty =
        BindableProperty.Create(nameof(UnitHeaderSecondaryColor), typeof(Color), typeof(UnitDisplayConfigurationsView), DefaultHeaderSecondaryColor);
    public static readonly BindableProperty UnitHeaderPrimaryTextColorProperty =
        BindableProperty.Create(nameof(UnitHeaderPrimaryTextColor), typeof(Color), typeof(UnitDisplayConfigurationsView), Colors.White);
    public static readonly BindableProperty UnitHeaderSecondaryTextColorProperty =
        BindableProperty.Create(nameof(UnitHeaderSecondaryTextColor), typeof(Color), typeof(UnitDisplayConfigurationsView), Colors.White);
    public static readonly BindableProperty PeripheralEquipmentProperty =
        BindableProperty.Create(
            nameof(PeripheralEquipment),
            typeof(string),
            typeof(UnitDisplayConfigurationsView),
            "-",
            propertyChanged: (bindable, _, _) =>
            {
                if (bindable is UnitDisplayConfigurationsView view)
                {
                    view.OnPropertyChanged(nameof(HasPeripheralEquipment));
                }
            });
    public static readonly BindableProperty PeripheralSkillsProperty =
        BindableProperty.Create(
            nameof(PeripheralSkills),
            typeof(string),
            typeof(UnitDisplayConfigurationsView),
            "-",
            propertyChanged: (bindable, _, _) =>
            {
                if (bindable is UnitDisplayConfigurationsView view)
                {
                    view.OnPropertyChanged(nameof(HasPeripheralSkills));
                }
            });
    public static readonly BindableProperty EquipmentSummaryProperty =
        BindableProperty.Create(nameof(EquipmentSummary), typeof(string), typeof(UnitDisplayConfigurationsView), "Equipment: -");
    public static readonly BindableProperty SpecialSkillsSummaryProperty =
        BindableProperty.Create(nameof(SpecialSkillsSummary), typeof(string), typeof(UnitDisplayConfigurationsView), "Special Skills: -");
    public static readonly BindableProperty EquipmentSummaryFormattedProperty =
        BindableProperty.Create(nameof(EquipmentSummaryFormatted), typeof(FormattedString), typeof(UnitDisplayConfigurationsView), new FormattedString());
    public static readonly BindableProperty SpecialSkillsSummaryFormattedProperty =
        BindableProperty.Create(nameof(SpecialSkillsSummaryFormatted), typeof(FormattedString), typeof(UnitDisplayConfigurationsView), new FormattedString());
    public static readonly BindableProperty PeripheralEquipmentFormattedProperty =
        BindableProperty.Create(nameof(PeripheralEquipmentFormatted), typeof(FormattedString), typeof(UnitDisplayConfigurationsView), new FormattedString());
    public static readonly BindableProperty PeripheralSkillsFormattedProperty =
        BindableProperty.Create(nameof(PeripheralSkillsFormatted), typeof(FormattedString), typeof(UnitDisplayConfigurationsView), new FormattedString());
    public static readonly BindableProperty ShowConfigurationsSectionProperty =
        BindableProperty.Create(
            nameof(ShowConfigurationsSection),
            typeof(bool),
            typeof(UnitDisplayConfigurationsView),
            true,
            propertyChanged: OnShowConfigurationsSectionChanged);

    public string UnitMov
    {
        get => (string)GetValue(UnitMovProperty);
        set => SetValue(UnitMovProperty, value);
    }

    public string UnitCc
    {
        get => (string)GetValue(UnitCcProperty);
        set => SetValue(UnitCcProperty, value);
    }

    public string UnitBs
    {
        get => (string)GetValue(UnitBsProperty);
        set => SetValue(UnitBsProperty, value);
    }

    public string UnitPh
    {
        get => (string)GetValue(UnitPhProperty);
        set => SetValue(UnitPhProperty, value);
    }

    public string UnitWip
    {
        get => (string)GetValue(UnitWipProperty);
        set => SetValue(UnitWipProperty, value);
    }

    public string UnitArm
    {
        get => (string)GetValue(UnitArmProperty);
        set => SetValue(UnitArmProperty, value);
    }

    public string UnitBts
    {
        get => (string)GetValue(UnitBtsProperty);
        set => SetValue(UnitBtsProperty, value);
    }

    public string UnitVitalityHeader
    {
        get => (string)GetValue(UnitVitalityHeaderProperty);
        set => SetValue(UnitVitalityHeaderProperty, value);
    }

    public string UnitVitality
    {
        get => (string)GetValue(UnitVitalityProperty);
        set => SetValue(UnitVitalityProperty, value);
    }

    public string UnitS
    {
        get => (string)GetValue(UnitSProperty);
        set => SetValue(UnitSProperty, value);
    }

    public string UnitAva
    {
        get => (string)GetValue(UnitAvaProperty);
        set => SetValue(UnitAvaProperty, value);
    }

    public bool HasPeripheralStatBlock
    {
        get => (bool)GetValue(HasPeripheralStatBlockProperty);
        set => SetValue(HasPeripheralStatBlockProperty, value);
    }

    public string PeripheralNameHeading
    {
        get => (string)GetValue(PeripheralNameHeadingProperty);
        set => SetValue(PeripheralNameHeadingProperty, value);
    }

    public string PeripheralMov
    {
        get => (string)GetValue(PeripheralMovProperty);
        set => SetValue(PeripheralMovProperty, value);
    }

    public string PeripheralCc
    {
        get => (string)GetValue(PeripheralCcProperty);
        set => SetValue(PeripheralCcProperty, value);
    }

    public string PeripheralBs
    {
        get => (string)GetValue(PeripheralBsProperty);
        set => SetValue(PeripheralBsProperty, value);
    }

    public string PeripheralPh
    {
        get => (string)GetValue(PeripheralPhProperty);
        set => SetValue(PeripheralPhProperty, value);
    }

    public string PeripheralWip
    {
        get => (string)GetValue(PeripheralWipProperty);
        set => SetValue(PeripheralWipProperty, value);
    }

    public string PeripheralArm
    {
        get => (string)GetValue(PeripheralArmProperty);
        set => SetValue(PeripheralArmProperty, value);
    }

    public string PeripheralBts
    {
        get => (string)GetValue(PeripheralBtsProperty);
        set => SetValue(PeripheralBtsProperty, value);
    }

    public string PeripheralVitalityHeader
    {
        get => (string)GetValue(PeripheralVitalityHeaderProperty);
        set => SetValue(PeripheralVitalityHeaderProperty, value);
    }

    public string PeripheralVitality
    {
        get => (string)GetValue(PeripheralVitalityProperty);
        set => SetValue(PeripheralVitalityProperty, value);
    }

    public string PeripheralS
    {
        get => (string)GetValue(PeripheralSProperty);
        set => SetValue(PeripheralSProperty, value);
    }

    public string PeripheralAva
    {
        get => (string)GetValue(PeripheralAvaProperty);
        set => SetValue(PeripheralAvaProperty, value);
    }

    public SKPicture? SelectedUnitPicture
    {
        get => (SKPicture?)GetValue(SelectedUnitPictureProperty);
        set => SetValue(SelectedUnitPictureProperty, value);
    }

    public SKPicture? RegularOrderIconPicture
    {
        get => (SKPicture?)GetValue(RegularOrderIconPictureProperty);
        set => SetValue(RegularOrderIconPictureProperty, value);
    }

    public SKPicture? IrregularOrderIconPicture
    {
        get => (SKPicture?)GetValue(IrregularOrderIconPictureProperty);
        set => SetValue(IrregularOrderIconPictureProperty, value);
    }

    public SKPicture? LieutenantIconPicture
    {
        get => (SKPicture?)GetValue(LieutenantIconPictureProperty);
        set => SetValue(LieutenantIconPictureProperty, value);
    }

    public SKPicture? ImpetuousIconPicture
    {
        get => (SKPicture?)GetValue(ImpetuousIconPictureProperty);
        set => SetValue(ImpetuousIconPictureProperty, value);
    }

    public SKPicture? TacticalAwarenessIconPicture
    {
        get => (SKPicture?)GetValue(TacticalAwarenessIconPictureProperty);
        set => SetValue(TacticalAwarenessIconPictureProperty, value);
    }

    public SKPicture? CubeIconPicture
    {
        get => (SKPicture?)GetValue(CubeIconPictureProperty);
        set => SetValue(CubeIconPictureProperty, value);
    }

    public SKPicture? Cube2IconPicture
    {
        get => (SKPicture?)GetValue(Cube2IconPictureProperty);
        set => SetValue(Cube2IconPictureProperty, value);
    }

    public SKPicture? HackableIconPicture
    {
        get => (SKPicture?)GetValue(HackableIconPictureProperty);
        set => SetValue(HackableIconPictureProperty, value);
    }

    public SKPicture? PeripheralIconPicture
    {
        get => (SKPicture?)GetValue(PeripheralIconPictureProperty);
        set => SetValue(PeripheralIconPictureProperty, value);
    }

    public bool ShowRegularOrderIcon
    {
        get => (bool)GetValue(ShowRegularOrderIconProperty);
        set => SetValue(ShowRegularOrderIconProperty, value);
    }

    public bool ShowIrregularOrderIcon
    {
        get => (bool)GetValue(ShowIrregularOrderIconProperty);
        set => SetValue(ShowIrregularOrderIconProperty, value);
    }

    public bool ShowLieutenantIcon
    {
        get => (bool)GetValue(ShowLieutenantIconProperty);
        set => SetValue(ShowLieutenantIconProperty, value);
    }

    public bool ShowPeripheralIcon
    {
        get => (bool)GetValue(ShowPeripheralIconProperty);
        set => SetValue(ShowPeripheralIconProperty, value);
    }

    public int LieutenantIconCount
    {
        get => (int)GetValue(LieutenantIconCountProperty);
        set => SetValue(LieutenantIconCountProperty, Math.Max(0, value));
    }

    public bool ShowImpetuousIcon
    {
        get => (bool)GetValue(ShowImpetuousIconProperty);
        set => SetValue(ShowImpetuousIconProperty, value);
    }

    public bool ShowTacticalAwarenessIcon
    {
        get => (bool)GetValue(ShowTacticalAwarenessIconProperty);
        set => SetValue(ShowTacticalAwarenessIconProperty, value);
    }

    public bool ShowCubeIcon
    {
        get => (bool)GetValue(ShowCubeIconProperty);
        set => SetValue(ShowCubeIconProperty, value);
    }

    public bool ShowCube2Icon
    {
        get => (bool)GetValue(ShowCube2IconProperty);
        set => SetValue(ShowCube2IconProperty, value);
    }

    public bool ShowHackableIcon
    {
        get => (bool)GetValue(ShowHackableIconProperty);
        set => SetValue(ShowHackableIconProperty, value);
    }

    public bool ShowUnitsInInches
    {
        get => (bool)GetValue(ShowUnitsInInchesProperty);
        set => SetValue(ShowUnitsInInchesProperty, value);
    }

    public int? UnitMoveFirstCm
    {
        get => (int?)GetValue(UnitMoveFirstCmProperty);
        set => SetValue(UnitMoveFirstCmProperty, value);
    }

    public int? UnitMoveSecondCm
    {
        get => (int?)GetValue(UnitMoveSecondCmProperty);
        set => SetValue(UnitMoveSecondCmProperty, value);
    }

    public int? PeripheralMoveFirstCm
    {
        get => (int?)GetValue(PeripheralMoveFirstCmProperty);
        set => SetValue(PeripheralMoveFirstCmProperty, value);
    }

    public int? PeripheralMoveSecondCm
    {
        get => (int?)GetValue(PeripheralMoveSecondCmProperty);
        set => SetValue(PeripheralMoveSecondCmProperty, value);
    }

    public string UnitNameHeading
    {
        get => (string)GetValue(UnitNameHeadingProperty);
        set => SetValue(UnitNameHeadingProperty, value);
    }

    public double UnitNameHeadingFontSize
    {
        get => (double)GetValue(UnitNameHeadingFontSizeProperty);
        set => SetValue(UnitNameHeadingFontSizeProperty, value);
    }

    public Color UnitHeaderPrimaryColor
    {
        get => (Color)GetValue(UnitHeaderPrimaryColorProperty);
        set => SetValue(UnitHeaderPrimaryColorProperty, value);
    }

    public Color UnitHeaderSecondaryColor
    {
        get => (Color)GetValue(UnitHeaderSecondaryColorProperty);
        set => SetValue(UnitHeaderSecondaryColorProperty, value);
    }

    public Color UnitHeaderPrimaryTextColor
    {
        get => (Color)GetValue(UnitHeaderPrimaryTextColorProperty);
        set => SetValue(UnitHeaderPrimaryTextColorProperty, value);
    }

    public Color UnitHeaderSecondaryTextColor
    {
        get => (Color)GetValue(UnitHeaderSecondaryTextColorProperty);
        set => SetValue(UnitHeaderSecondaryTextColorProperty, value);
    }

    public string PeripheralEquipment
    {
        get => (string)GetValue(PeripheralEquipmentProperty);
        set => SetValue(PeripheralEquipmentProperty, value);
    }

    public string PeripheralSkills
    {
        get => (string)GetValue(PeripheralSkillsProperty);
        set => SetValue(PeripheralSkillsProperty, value);
    }

    public string EquipmentSummary
    {
        get => (string)GetValue(EquipmentSummaryProperty);
        set => SetValue(EquipmentSummaryProperty, value);
    }

    public string SpecialSkillsSummary
    {
        get => (string)GetValue(SpecialSkillsSummaryProperty);
        set => SetValue(SpecialSkillsSummaryProperty, value);
    }

    public FormattedString EquipmentSummaryFormatted
    {
        get => (FormattedString)GetValue(EquipmentSummaryFormattedProperty);
        set => SetValue(EquipmentSummaryFormattedProperty, value);
    }

    public FormattedString SpecialSkillsSummaryFormatted
    {
        get => (FormattedString)GetValue(SpecialSkillsSummaryFormattedProperty);
        set => SetValue(SpecialSkillsSummaryFormattedProperty, value);
    }

    public FormattedString PeripheralEquipmentFormatted
    {
        get => (FormattedString)GetValue(PeripheralEquipmentFormattedProperty);
        set => SetValue(PeripheralEquipmentFormattedProperty, value);
    }

    public FormattedString PeripheralSkillsFormatted
    {
        get => (FormattedString)GetValue(PeripheralSkillsFormattedProperty);
        set => SetValue(PeripheralSkillsFormattedProperty, value);
    }

    public bool ShowConfigurationsSection
    {
        get => (bool)GetValue(ShowConfigurationsSectionProperty);
        set => SetValue(ShowConfigurationsSectionProperty, value);
    }

    public bool HasPeripheralEquipment => !string.IsNullOrWhiteSpace(PeripheralEquipment) && PeripheralEquipment != "-";
    public bool HasPeripheralSkills => !string.IsNullOrWhiteSpace(PeripheralSkills) && PeripheralSkills != "-";
    public bool HasAnyTopHeaderIcons => ShowRegularOrderIcon || ShowIrregularOrderIcon || ShowLieutenantIcon || LieutenantIconCount > 0 || ShowPeripheralIcon || ShowImpetuousIcon || ShowTacticalAwarenessIcon;
    public bool HasAnyBottomHeaderIcons => ShowCubeIcon || ShowCube2Icon || ShowHackableIcon;
    public bool HasAnyHeaderIcons => HasAnyTopHeaderIcons || HasAnyBottomHeaderIcons;

    /// <summary>
    /// Serialized profile-groups payload for the currently selected unit.
    /// </summary>
    public string? SelectedUnitProfileGroupsJson { get; set; }

    /// <summary>
    /// Serialized filter payload associated with the currently selected unit.
    /// </summary>
    public string? SelectedUnitFiltersJson { get; set; }

    /// <summary>
    /// Canonical common equipment entries for the currently selected unit.
    /// </summary>
    public List<string> SelectedUnitCommonEquipment { get; set; } = [];

    /// <summary>
    /// Canonical common skills entries for the currently selected unit.
    /// </summary>
    public List<string> SelectedUnitCommonSkills { get; set; } = [];

    public event EventHandler<SKPaintSurfaceEventArgs>? HeaderIconsCanvasPaintSurface;
    public event EventHandler<SKPaintSurfaceEventArgs>? SelectedUnitCanvasPaintSurface;
    public event EventHandler<SKPaintSurfaceEventArgs>? PeripheralIconCanvasPaintSurface;
    public event EventHandler<SKPaintSurfaceEventArgs>? ProfileTacticalIconCanvasPaintSurface;
    public event EventHandler<EventArgs>? UnitNameHeadingSizeChanged;

    public UnitDisplayConfigurationsView()
    {
        InitializeComponent();
        UpdateConfigurationRows();
    }

    public Label UnitNameHeadingElement => UnitNameHeadingLabel;

    public void InvalidateHeaderIconsCanvas()
    {
        HeaderIconsCanvas.InvalidateSurface();
    }

    public void InvalidateSelectedUnitCanvas()
    {
        SelectedUnitCanvas.InvalidateSurface();
    }

    public void InvalidatePeripheralHeaderIconCanvas()
    {
        PeripheralHeaderIconCanvas.InvalidateSurface();
    }

    /// <summary>
    /// Recomputes and applies the heading font-size from the bound icon state.
    /// </summary>
    public void RefreshUnitHeadingFontSize()
    {
        if (BindingContext is IUnitDisplayIconState state)
        {
            UpdateUnitHeadingFontSize(
                state.UnitHeadingMaxFontSize,
                state.UnitHeadingMinFontSize,
                state.UnitHeadingFontStep);
            UnitNameHeadingSizeChanged?.Invoke(this, EventArgs.Empty);
            return;
        }

        UpdateUnitHeadingFontSize(
            DefaultUnitHeadingMaxFontSize,
            DefaultUnitHeadingMinFontSize,
            DefaultUnitHeadingFontStep);
        UnitNameHeadingSizeChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Recomputes and applies derived move strings from the bound stat state.
    /// </summary>
    public void RefreshMoveStatlines()
    {
        if (BindingContext is IUnitDisplayStatState state)
        {
            UnitMov = FormatMoveValue(state.UnitMoveFirstCm, state.UnitMoveSecondCm, state.ShowUnitsInInches);
            PeripheralMov = FormatMoveValue(state.PeripheralMoveFirstCm, state.PeripheralMoveSecondCm, state.ShowUnitsInInches);
            return;
        }

        UnitMov = FormatMoveValue(UnitMoveFirstCm, UnitMoveSecondCm, ShowUnitsInInches);
        PeripheralMov = FormatMoveValue(PeripheralMoveFirstCm, PeripheralMoveSecondCm, ShowUnitsInInches);
    }

    /// <summary>
    /// Formats movement values as inches or centimeters for display.
    /// </summary>
    public static string FormatMoveValue(int? firstCm, int? secondCm, bool showUnitsInInches)
    {
        if (firstCm is null || secondCm is null || firstCm <= 0 || secondCm <= 0)
        {
            return "-";
        }

        if (showUnitsInInches)
        {
            var firstInches = firstCm.Value / 2.54d;
            var secondInches = secondCm.Value / 2.54d;
            return $"{Math.Round(firstInches):0}-{Math.Round(secondInches):0}";
        }

        return $"{firstCm.Value.ToString(CultureInfo.InvariantCulture)}-{secondCm.Value.ToString(CultureInfo.InvariantCulture)}";
    }

    private void OnHeaderIconsCanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        if (BindingContext is IUnitDisplayIconState state)
        {
            DrawIconColumn(canvas, e.Info, BuildHeaderIconPictures(state));
            return;
        }

        DrawIconColumn(canvas, e.Info, BuildHeaderIconPictures());
        HeaderIconsCanvasPaintSurface?.Invoke(sender, e);
    }

    private void OnSelectedUnitCanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        if (SelectedUnitPicture is not null)
        {
            DrawSlotPicture(SelectedUnitPicture, e);
            return;
        }

        SelectedUnitCanvasPaintSurface?.Invoke(sender, e);
    }

    private void OnPeripheralIconCanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        if (PeripheralIconPicture is not null)
        {
            DrawSlotPicture(PeripheralIconPicture, e);
            return;
        }

        PeripheralIconCanvasPaintSurface?.Invoke(sender, e);
    }

    private void OnProfileTacticalIconCanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        if (TacticalAwarenessIconPicture is not null)
        {
            DrawSlotPicture(TacticalAwarenessIconPicture, e);
            return;
        }

        ProfileTacticalIconCanvasPaintSurface?.Invoke(sender, e);
    }

    private void OnUnitNameHeadingLabelSizeChanged(object? sender, EventArgs e)
    {
        if (BindingContext is IUnitDisplayIconState state)
        {
            UpdateUnitHeadingFontSize(
                state.UnitHeadingMaxFontSize,
                state.UnitHeadingMinFontSize,
                state.UnitHeadingFontStep);
            return;
        }

        UpdateUnitHeadingFontSize(
            DefaultUnitHeadingMaxFontSize,
            DefaultUnitHeadingMinFontSize,
            DefaultUnitHeadingFontStep);
    }

    private async void OnProfilesScrollPanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        if (sender is not ScrollView scrollView)
        {
            return;
        }

        switch (e.StatusType)
        {
            case GestureStatus.Started:
                _profilesPanLastTotalY = 0d;
                break;
            case GestureStatus.Running:
                var deltaY = e.TotalY - _profilesPanLastTotalY;
                _profilesPanLastTotalY = e.TotalY;
                var targetY = Math.Max(0d, scrollView.ScrollY - deltaY);
                await scrollView.ScrollToAsync(0d, targetY, false);
                break;
            case GestureStatus.Completed:
            case GestureStatus.Canceled:
                _profilesPanLastTotalY = 0d;
                break;
        }
    }

    private List<HeaderIconRenderItem> BuildHeaderIconPictures(IUnitDisplayIconState state)
    {
        var pictures = new List<HeaderIconRenderItem>(MaxHeaderIcons);
        var orderTypePicture = state.ShowIrregularOrderIcon ? IrregularOrderIconPicture : RegularOrderIconPicture;
        if ((state.ShowRegularOrderIcon || state.ShowIrregularOrderIcon) && orderTypePicture is not null)
        {
            pictures.Add(new HeaderIconRenderItem(orderTypePicture));
        }

        var lieutenantIconCount = Math.Max(ShowLieutenantIcon ? 1 : 0, LieutenantIconCount);
        for (var i = 0; i < lieutenantIconCount; i++)
        {
            if (LieutenantIconPicture is null)
            {
                break;
            }

            pictures.Add(new HeaderIconRenderItem(LieutenantIconPicture));
        }

        if (ShowPeripheralIcon && PeripheralIconPicture is not null)
        {
            pictures.Add(new HeaderIconRenderItem(PeripheralIconPicture));
        }

        if (state.ShowImpetuousIcon && ImpetuousIconPicture is not null)
        {
            pictures.Add(new HeaderIconRenderItem(ImpetuousIconPicture));
        }

        if (state.ShowTacticalAwarenessIcon && TacticalAwarenessIconPicture is not null)
        {
            pictures.Add(new HeaderIconRenderItem(TacticalAwarenessIconPicture));
        }

        if (state.ShowCubeIcon && CubeIconPicture is not null)
        {
            pictures.Add(new HeaderIconRenderItem(CubeIconPicture));
        }

        if (state.ShowCube2Icon && Cube2IconPicture is not null)
        {
            pictures.Add(new HeaderIconRenderItem(Cube2IconPicture));
        }

        if (state.ShowHackableIcon && HackableIconPicture is not null)
        {
            pictures.Add(new HeaderIconRenderItem(HackableIconPicture));
        }

        if (pictures.Count <= MaxHeaderIcons)
        {
            return pictures;
        }

        return pictures.Take(MaxHeaderIcons).ToList();
    }

    private List<HeaderIconRenderItem> BuildHeaderIconPictures()
    {
        var pictures = new List<HeaderIconRenderItem>(MaxHeaderIcons);
        var orderTypePicture = ShowIrregularOrderIcon ? IrregularOrderIconPicture : RegularOrderIconPicture;
        if ((ShowRegularOrderIcon || ShowIrregularOrderIcon) && orderTypePicture is not null)
        {
            pictures.Add(new HeaderIconRenderItem(orderTypePicture));
        }

        var lieutenantIconCount = Math.Max(ShowLieutenantIcon ? 1 : 0, LieutenantIconCount);
        for (var i = 0; i < lieutenantIconCount; i++)
        {
            if (LieutenantIconPicture is null)
            {
                break;
            }

            pictures.Add(new HeaderIconRenderItem(LieutenantIconPicture));
        }

        if (ShowPeripheralIcon && PeripheralIconPicture is not null)
        {
            pictures.Add(new HeaderIconRenderItem(PeripheralIconPicture));
        }

        if (ShowImpetuousIcon && ImpetuousIconPicture is not null)
        {
            pictures.Add(new HeaderIconRenderItem(ImpetuousIconPicture));
        }

        if (ShowTacticalAwarenessIcon && TacticalAwarenessIconPicture is not null)
        {
            pictures.Add(new HeaderIconRenderItem(TacticalAwarenessIconPicture));
        }

        if (ShowCubeIcon && CubeIconPicture is not null)
        {
            pictures.Add(new HeaderIconRenderItem(CubeIconPicture));
        }

        if (ShowCube2Icon && Cube2IconPicture is not null)
        {
            pictures.Add(new HeaderIconRenderItem(Cube2IconPicture));
        }

        if (ShowHackableIcon && HackableIconPicture is not null)
        {
            pictures.Add(new HeaderIconRenderItem(HackableIconPicture));
        }

        if (pictures.Count <= MaxHeaderIcons)
        {
            return pictures;
        }

        return pictures.Take(MaxHeaderIcons).ToList();
    }

    private static void DrawIconColumn(SKCanvas canvas, SKImageInfo info, IReadOnlyList<HeaderIconRenderItem> pictures)
    {
        if (pictures.Count == 0)
        {
            return;
        }

        var drawCount = pictures.Count;
        if (drawCount <= 3)
        {
            var totalGap = (drawCount - 1) * IconVerticalGap;
            var maxIconSizeFromHeight = (info.Height - totalGap) / drawCount;
            var maxIconSizeFromWidth = info.Width;
            var iconSize = Math.Max(1f, Math.Min(IconSize, Math.Min(maxIconSizeFromHeight, maxIconSizeFromWidth)));
            var totalHeight = (drawCount * iconSize) + totalGap;
            var startY = Math.Max(0f, (info.Height - totalHeight) / 2f);

            for (var i = 0; i < drawCount; i++)
            {
                var x = (info.Width - iconSize) / 2f;
                var y = startY + (i * (iconSize + IconVerticalGap));
                var destination = new SKRect(x, y, x + iconSize, y + iconSize);
                DrawPictureInRect(canvas, pictures[i].Picture, destination);
            }

            return;
        }

        var leftCount = Math.Min(3, drawCount);
        var rightCount = Math.Max(0, drawCount - leftCount);
        var rowsInTallerColumn = Math.Max(leftCount, rightCount);
        var totalVerticalGap = (rowsInTallerColumn - 1) * IconVerticalGap;
        var maxIconSizeFromTallestColumnHeight = (info.Height - totalVerticalGap) / rowsInTallerColumn;
        var maxIconSizeFromWidthForTwoColumns = (info.Width - IconHorizontalGap) / 2f;
        var twoColumnIconSize = Math.Max(
            1f,
            Math.Min(IconSize, Math.Min(maxIconSizeFromTallestColumnHeight, maxIconSizeFromWidthForTwoColumns)));

        var totalWidth = (twoColumnIconSize * 2f) + IconHorizontalGap;
        var leftX = Math.Max(0f, (info.Width - totalWidth) / 2f);
        var rightX = leftX + twoColumnIconSize + IconHorizontalGap;

        var leftTotalHeight = (leftCount * twoColumnIconSize) + ((leftCount - 1) * IconVerticalGap);
        var leftStartY = Math.Max(0f, (info.Height - leftTotalHeight) / 2f);
        for (var i = 0; i < leftCount; i++)
        {
            var y = leftStartY + (i * (twoColumnIconSize + IconVerticalGap));
            var destination = new SKRect(leftX, y, leftX + twoColumnIconSize, y + twoColumnIconSize);
            DrawPictureInRect(canvas, pictures[i].Picture, destination);
        }

        if (rightCount == 0)
        {
            return;
        }

        var rightTotalHeight = (rightCount * twoColumnIconSize) + ((rightCount - 1) * IconVerticalGap);
        var rightStartY = Math.Max(0f, (info.Height - rightTotalHeight) / 2f);
        for (var i = 0; i < rightCount; i++)
        {
            var pictureIndex = leftCount + i;
            var y = rightStartY + (i * (twoColumnIconSize + IconVerticalGap));
            var destination = new SKRect(rightX, y, rightX + twoColumnIconSize, y + twoColumnIconSize);
            DrawPictureInRect(canvas, pictures[pictureIndex].Picture, destination);
        }
    }

    private static void DrawPictureInRect(SKCanvas canvas, SKPicture picture, SKRect destination)
    {
        var bounds = picture.CullRect;
        if (bounds.Width <= 0 || bounds.Height <= 0 || destination.Width <= 0 || destination.Height <= 0)
        {
            return;
        }

        var scale = Math.Min(destination.Width / bounds.Width, destination.Height / bounds.Height);
        var scaledWidth = bounds.Width * scale;
        var scaledHeight = bounds.Height * scale;
        var left = destination.Left + ((destination.Width - scaledWidth) / 2f);
        var top = destination.Top + ((destination.Height - scaledHeight) / 2f);

        canvas.Save();
        canvas.Translate(left - (bounds.Left * scale), top - (bounds.Top * scale));
        canvas.Scale(scale);
        canvas.DrawPicture(picture);
        canvas.Restore();

    }

    private static void DrawSlotPicture(SKPicture? picture, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        if (picture is null)
        {
            return;
        }

        var destination = new SKRect(0, 0, e.Info.Width, e.Info.Height);
        DrawPictureInRect(canvas, picture, destination);
    }

    private void UpdateUnitHeadingFontSize(double maxFontSize, double minFontSize, double fontStep)
    {
        var availableWidth = UnitNameHeadingLabel.Width;
        if (availableWidth <= 0)
        {
            UnitNameHeadingFontSize = maxFontSize;
            return;
        }

        for (var size = maxFontSize; size >= minFontSize; size -= fontStep)
        {
            UnitNameHeadingFontSize = size;
            var measuredWidth = UnitNameHeadingLabel.Measure(double.PositiveInfinity, double.PositiveInfinity).Width;
            if (measuredWidth <= availableWidth)
            {
                return;
            }
        }

        UnitNameHeadingFontSize = minFontSize;
    }

    private static void OnHeaderIconVisibilityChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is not UnitDisplayConfigurationsView view)
        {
            return;
        }

        view.OnPropertyChanged(nameof(HasAnyTopHeaderIcons));
        view.OnPropertyChanged(nameof(HasAnyBottomHeaderIcons));
        view.OnPropertyChanged(nameof(HasAnyHeaderIcons));
        view.InvalidateHeaderIconsCanvas();
    }

    private static void OnShowConfigurationsSectionChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is not UnitDisplayConfigurationsView view)
        {
            return;
        }

        view.UpdateConfigurationRows();
    }

    private void UpdateConfigurationRows()
    {
        if (ConfigurationsRootGrid.RowDefinitions.Count < 8)
        {
            return;
        }

        if (ShowConfigurationsSection)
        {
            ConfigurationsRootGrid.RowDefinitions[6].Height = GridLength.Auto;
            ConfigurationsRootGrid.RowDefinitions[7].Height = new GridLength(1, GridUnitType.Star);
            return;
        }

        var hiddenHeight = new GridLength(0);
        ConfigurationsRootGrid.RowDefinitions[6].Height = hiddenHeight;
        ConfigurationsRootGrid.RowDefinitions[7].Height = hiddenHeight;
    }
}
