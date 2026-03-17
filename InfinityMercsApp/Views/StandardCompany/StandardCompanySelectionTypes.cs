using InfinityMercsApp.ViewModels;
using InfinityMercsApp.Views.Common;

namespace InfinityMercsApp.Views.StandardCompany;

public class ArmyFactionSelectionItem : CompanyFactionSelectionItemBase
{
}

public class ArmyUnitSelectionItem : CompanyUnitSelectionItemBase
{
}

public class ArmyTeamListItem : CompanyTeamListItemBase<ArmyTeamUnitLimitItem>
{
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


