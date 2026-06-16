using System.Collections.ObjectModel;
using InfinityMercsApp.ViewModels;

namespace InfinityMercsApp.Views.Common;

public class ArmyTeamListItem : BaseViewModel
{
    public string Name { get; init; } = string.Empty;
    public string TeamCountsText { get; init; } = string.Empty;
    public bool HasTeamCounts => !string.IsNullOrWhiteSpace(TeamCountsText);
    public bool IsWildcardBucket { get; init; }
    public ObservableCollection<ArmyTeamUnitLimitItem> AllowedProfiles { get; init; } = [];

    public bool ShowTrackingRadioButton => !IsWildcardBucket;

    private bool _isTrackedTeam;
    public bool IsTrackedTeam
    {
        get => _isTrackedTeam;
        set
        {
            if (_isTrackedTeam == value)
            {
                return;
            }

            _isTrackedTeam = value;
            OnPropertyChanged();
        }
    }

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
