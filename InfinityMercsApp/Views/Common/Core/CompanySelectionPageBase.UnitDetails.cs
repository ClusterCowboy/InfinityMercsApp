using InfinityMercsApp.Services;
using InfinityMercsApp.Views.Controls;
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
        return CompanySelectionUnitDetailsWorkflow.BuildUnitCachedPathCandidates(
            item,
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
        return CompanySelectionUnitDetailsWorkflow.BuildUnitPackagedPathCandidates(
            item,
            leftSlotFactionId,
            rightSlotFactionId,
            getPackagedUnitLogoPath,
            getPackagedFactionLogoPath);
    }

    protected static void MergeFireteamEntriesCore(
        string? fireteamChartJson,
        Dictionary<string, CompanyTeamAggregate> target)
    {
        CompanySelectionUnitDetailsWorkflow.MergeFireteamEntries(
            fireteamChartJson,
            target);
    }

    protected static string BuildUnitSubtitleCore(
        ArmyResumeRecord unit,
        IReadOnlyDictionary<int, string> typeLookup,
        IReadOnlyDictionary<int, string> categoryLookup)
    {
        return CompanySelectionUnitDetailsWorkflow.BuildUnitSubtitle(unit, typeLookup, categoryLookup);
    }

    protected static bool IsCharacterCategoryCore(ArmyResumeRecord unit, IReadOnlyDictionary<int, string> categoryLookup)
    {
        return CompanySelectionUnitDetailsWorkflow.IsCharacterCategory(unit, categoryLookup);
    }

    protected static void ClearSelectedUnitLogoCore(UnitDisplayConfigurationsView unitDisplayView, string? logMessage = null)
    {
        CompanySelectionUnitDetailsWorkflow.ClearSelectedUnitLogo(unitDisplayView, logMessage);
    }

    protected static async Task LoadSelectedUnitLogoCoreAsync(
        CompanyUnitSelectionItemBase item,
        UnitDisplayConfigurationsView unitDisplayView,
        Func<Task<Stream?>> openBestUnitLogoStreamAsync)
    {
        await CompanySelectionUnitDetailsWorkflow.LoadSelectedUnitLogoAsync(
            item,
            unitDisplayView,
            openBestUnitLogoStreamAsync);
    }
}
