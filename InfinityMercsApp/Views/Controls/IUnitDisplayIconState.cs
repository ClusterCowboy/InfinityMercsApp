namespace InfinityMercsApp.Views.Controls;

/// <summary>
/// Provides icon and heading-size state consumed by UnitDisplayConfigurationsView.
/// </summary>
public interface IUnitDisplayIconState
{
    bool ShowRegularOrderIcon { get; }
    bool ShowIrregularOrderIcon { get; }
    bool ShowImpetuousIcon { get; }
    bool ShowTacticalAwarenessIcon { get; }
    bool ShowCubeIcon { get; }
    bool ShowCube2Icon { get; }
    bool ShowHackableIcon { get; }

    double UnitHeadingMaxFontSize { get; }
    double UnitHeadingMinFontSize { get; }
    double UnitHeadingFontStep { get; }
    void ApplyUnitHeadingFontSize(double size);
}
