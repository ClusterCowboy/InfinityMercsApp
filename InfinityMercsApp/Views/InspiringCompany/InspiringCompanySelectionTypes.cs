using InfinityMercsApp.Views.Common;

namespace InfinityMercsApp.Views.InspiringCompany;

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

public sealed class CaptainUpgradeOptionSet : CompanyCaptainUpgradeOptionSetBase
{
    public static CaptainUpgradeOptionSet Empty { get; } = new();
}
