namespace InfinityMercsApp.Views.Common;

public class ArmyTeamListItem : CompanyTeamListItemBase<ArmyTeamUnitLimitItem>
{
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
}
