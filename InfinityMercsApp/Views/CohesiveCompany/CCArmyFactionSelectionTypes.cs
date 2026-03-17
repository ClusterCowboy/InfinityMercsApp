using InfinityMercsApp.Views.Templates.NewCompany;

namespace InfinityMercsApp.Views.CohesiveCompany;

public class ArmyFactionSelectionItem : CompanyFactionSelectionItemBase
{
}

public class ArmyUnitSelectionItem : CompanyUnitSelectionItemBase
{
}

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

public class ArmyTeamUnitLimitItem : CompanyTeamUnitLimitItemBase
{
}

public class MercsCompanyEntry : CompanyMercsCompanyEntryBase
{
}

public sealed class SavedCompanyFile : CompanySavedCompanyFileBase<SavedImprovedCaptainStats, SavedCompanyFaction, SavedCompanyEntry>
{
}

public sealed class SavedImprovedCaptainStats : CompanySavedImprovedCaptainStatsBase
{
}

public sealed class SavedCompanyFaction : CompanySavedCompanyFactionBase
{
}

public sealed class SavedCompanyEntry : CompanySavedCompanyEntryBase
{
}

sealed class PeripheralMercsCompanyStats : CompanyPeripheralMercsCompanyStatsBase
{
}

public sealed class CaptainUpgradeOptionSet : CompanyCaptainUpgradeOptionSetBase
{
    public static CaptainUpgradeOptionSet Empty { get; } = new();
}
