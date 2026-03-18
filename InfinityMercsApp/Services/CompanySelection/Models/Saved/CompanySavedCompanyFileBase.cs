namespace InfinityMercsApp.Views.Common;

public abstract class CompanySavedCompanyFileBase<TCaptainStats, TFaction, TEntry>
    where TCaptainStats : CompanySavedImprovedCaptainStatsBase, new()
    where TFaction : CompanySavedCompanyFactionBase, new()
    where TEntry : CompanySavedCompanyEntryBase, new()
{
    public string CompanyName { get; init; } = string.Empty;
    public string CompanyType { get; init; } = string.Empty;
    public string CompanyIdentifier { get; init; } = string.Empty;
    public int CompanyIndex { get; init; }
    public string CreatedUtc { get; init; } = string.Empty;
    public int StartSeasonPoints { get; init; }
    public int PointsLimit { get; init; }
    public int CurrentPoints { get; init; }
    public TCaptainStats ImprovedCaptainStats { get; init; } = new();
    public List<TFaction> SourceFactions { get; init; } = [];
    public List<TEntry> Entries { get; init; } = [];
}
