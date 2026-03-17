using ArmyResumeRecord = InfinityMercsApp.Domain.Models.Army.Resume;

namespace InfinityMercsApp.Views.Common;

public abstract partial class CompanySelectionPageBase
{
    protected static IEnumerable<string?> BuildUnitCachedPathCandidatesCore(
        CompanyUnitSelectionItemBase item,
        int? leftSlotFactionId,
        int? rightSlotFactionId,
        Func<int, int, string?>? getCachedUnitLogoPath,
        Func<int, string?>? getCachedFactionLogoPath)
    {
        if (getCachedUnitLogoPath is null || getCachedFactionLogoPath is null)
        {
            return [item.CachedLogoPath];
        }

        return CompanyUnitDetailsShared.BuildUnitCachedPathCandidates(
            item.CachedLogoPath,
            item.SourceFactionId,
            item.Id,
            leftSlotFactionId,
            rightSlotFactionId,
            getCachedUnitLogoPath,
            getCachedFactionLogoPath);
    }

    protected static IEnumerable<string?> BuildUnitPackagedPathCandidatesCore(
        CompanyUnitSelectionItemBase item,
        int? leftSlotFactionId,
        int? rightSlotFactionId,
        Func<int, int, string?>? getPackagedUnitLogoPath,
        Func<int, string?>? getPackagedFactionLogoPath)
    {
        return CompanyUnitDetailsShared.BuildUnitPackagedPathCandidates(
            item.PackagedLogoPath,
            item.SourceFactionId,
            item.Id,
            leftSlotFactionId,
            rightSlotFactionId,
            getPackagedUnitLogoPath,
            getPackagedFactionLogoPath);
    }

    protected static void MergeFireteamEntriesCore(
        string? fireteamChartJson,
        Dictionary<string, CompanyTeamAggregate> target)
    {
        CompanyUnitDetailsShared.MergeFireteamEntries(
            fireteamChartJson,
            entry =>
            {
                if (!target.TryGetValue(entry.Name, out var aggregate))
                {
                    aggregate = new CompanyTeamAggregate(entry.Name);
                    target[entry.Name] = aggregate;
                }

                aggregate.AddCounts(entry.Duo, entry.Haris, entry.Core);
                foreach (var limit in entry.UnitLimits)
                {
                    aggregate.MergeUnitLimit(limit.Name, limit.Min, limit.Max, limit.Slug, limit.MinAsterisk);
                }
            },
            Console.Error.WriteLine);
    }

    protected static string BuildUnitSubtitleCore(
        ArmyResumeRecord unit,
        IReadOnlyDictionary<int, string> typeLookup,
        IReadOnlyDictionary<int, string> categoryLookup)
    {
        return CompanyUnitDetailsShared.BuildUnitSubtitle(unit, typeLookup, categoryLookup);
    }

    protected static bool IsCharacterCategoryCore(ArmyResumeRecord unit, IReadOnlyDictionary<int, string> categoryLookup)
    {
        return CompanyUnitDetailsShared.IsCharacterCategory(unit, categoryLookup);
    }
}
