using InfinityMercsApp.Services;
using InfinityMercsApp.Views.Controls;
using ArmyResumeRecord = InfinityMercsApp.Domain.Models.Army.Resume;

namespace InfinityMercsApp.Views.Common;

internal static class CompanySelectionUnitDetailsWorkflow
{
    internal static IEnumerable<string?> BuildUnitCachedPathCandidates(
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

    internal static IEnumerable<string?> BuildUnitPackagedPathCandidates(
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

    internal static void MergeFireteamEntries(
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

    internal static string BuildUnitSubtitle(
        ArmyResumeRecord unit,
        IReadOnlyDictionary<int, string> typeLookup,
        IReadOnlyDictionary<int, string> categoryLookup)
    {
        return CompanyUnitDetailsShared.BuildUnitSubtitle(unit, typeLookup, categoryLookup);
    }

    internal static bool IsCharacterCategory(ArmyResumeRecord unit, IReadOnlyDictionary<int, string> categoryLookup)
    {
        return CompanyUnitDetailsShared.IsCharacterCategory(unit, categoryLookup);
    }

    internal static void ClearSelectedUnitLogo(UnitDisplayConfigurationsView unitDisplayView, string? logMessage = null)
    {
        if (!string.IsNullOrWhiteSpace(logMessage))
        {
            Console.WriteLine(logMessage);
        }

        unitDisplayView.SelectedUnitPicture?.Dispose();
        unitDisplayView.SelectedUnitPicture = null;
        unitDisplayView.InvalidateSelectedUnitCanvas();
    }

    internal static async Task LoadSelectedUnitLogoAsync(
        CompanyUnitSelectionItemBase item,
        UnitDisplayConfigurationsView unitDisplayView,
        Func<Task<Stream?>> openBestUnitLogoStreamAsync)
    {
        ClearSelectedUnitLogo(unitDisplayView);
        unitDisplayView.SelectedUnitPicture = await CompanyUnitLogoWorkflowService.LoadSelectedUnitLogoAsync(
            item.Name,
            item.Id,
            item.SourceFactionId,
            openBestUnitLogoStreamAsync);
        unitDisplayView.InvalidateSelectedUnitCanvas();
    }
}
