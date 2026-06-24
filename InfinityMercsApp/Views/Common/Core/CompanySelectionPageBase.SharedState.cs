using System.Collections.ObjectModel;
using InfinityMercsApp.ViewModels;
using InfinityMercsApp.Views.Controls;

namespace InfinityMercsApp.Views.Common;

/// <summary>
/// Shared selection state previously duplicated across every concrete company page.
/// All company types use the same <c>Army*</c> item models, so this state lives on the
/// base and is consumed by the shared selection/visibility/detail workflows.
/// </summary>
public abstract partial class CompanySelectionPageBase
{
    /// <summary>Tracks the faction chosen for each selector slot.</summary>
    protected readonly FactionSlotSelectionState<ArmyFactionSelectionItem> _factionSelectionState = new();

    /// <summary>The unit currently shown in the detail pane, if any.</summary>
    protected ArmyUnitSelectionItem? _selectedUnit;

    /// <summary>When true, the equipment/skills summary bolds lieutenant entries.</summary>
    protected bool _summaryHighlightLieutenant;

    /// <summary>Index of the faction selector slot currently being edited.</summary>
    protected int _activeSlotIndex;

    /// <summary>Active unit search/filter criteria and prepared popup options.</summary>
    protected readonly CompanySelectionFilterState _filterState = new();

    public ObservableRangeCollection<ArmyFactionSelectionItem> Factions { get; } = [];
    public ObservableCollection<ArmyUnitSelectionItem> Units { get; } = [];
    public ObservableCollection<ArmyTeamListItem> TeamEntries { get; } = [];
    public ObservableCollection<ViewerProfileItem> Profiles { get; } = [];
    public ObservableCollection<MercsCompanyEntry> MercsCompanyEntries { get; } = [];
}
