namespace InfinityMercsApp.Views.Controls;

/// <summary>
/// Provides move-statline state consumed and formatted by UnitDisplayConfigurationsView.
/// </summary>
public interface IUnitDisplayStatState
{
    bool ShowUnitsInInches { get; }
    int? UnitMoveFirstCm { get; }
    int? UnitMoveSecondCm { get; }
    int? PeripheralMoveFirstCm { get; }
    int? PeripheralMoveSecondCm { get; }

    void ApplyUnitMoveDisplay(string value);
    void ApplyPeripheralMoveDisplay(string value);
}
