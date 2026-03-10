namespace InfinityMercsApp.Views.Controls;

/// <summary>
/// Holds selected faction and slot assignments for company selection pages.
/// </summary>
/// <typeparam name="TFaction">The faction item type used by the page.</typeparam>
public sealed class FactionSlotSelectionState<TFaction>
    where TFaction : class
{
    /// <summary>
    /// Currently selected faction from the source list.
    /// </summary>
    public TFaction? SelectedFaction { get; set; }

    /// <summary>
    /// Faction assigned to the left source slot.
    /// </summary>
    public TFaction? LeftSlotFaction { get; set; }

    /// <summary>
    /// Faction assigned to the right source slot.
    /// </summary>
    public TFaction? RightSlotFaction { get; set; }
}
