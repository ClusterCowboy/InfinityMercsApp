using InfinityMercsApp.Services;
using InfinityMercsApp.Views.Controls;
using ArmyResumeRecord = InfinityMercsApp.Domain.Models.Army.Resume;

namespace InfinityMercsApp.Views.Common;

public abstract partial class CompanySelectionPageBase
{
    /// <summary>
    /// Returns an ordered sequence of on-disk logo path candidates for the given unit,
    /// checking the unit's own cached path as well as both slot faction paths as fallbacks.
    /// </summary>
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

    /// <summary>
    /// Returns an ordered sequence of bundled app-package logo path candidates for the given unit,
    /// used as a fallback when the on-disk cache does not have a suitable image.
    /// </summary>
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

    /// <summary>
    /// Deserialises fireteam chart JSON and merges the entries into the provided dictionary,
    /// keyed by team name. Used to build the combined fireteam list from multiple faction sources.
    /// </summary>
    protected static void MergeFireteamEntriesCore(
        string? fireteamChartJson,
        Dictionary<string, CompanyTeamAggregate> target)
    {
        CompanySelectionUnitDetailsWorkflow.MergeFireteamEntries(
            fireteamChartJson,
            target);
    }

    /// <summary>
    /// Composes the unit subtitle string by resolving the unit type and category names
    /// from their respective lookup dictionaries.
    /// </summary>
    protected static string BuildUnitSubtitleCore(
        ArmyResumeRecord unit,
        IReadOnlyDictionary<int, string> typeLookup,
        IReadOnlyDictionary<int, string> categoryLookup)
    {
        return CompanySelectionUnitDetailsWorkflow.BuildUnitSubtitle(unit, typeLookup, categoryLookup);
    }

    /// <summary>
    /// Returns <c>true</c> when the unit belongs to the "Character" category,
    /// which affects display formatting and AVA rules.
    /// </summary>
    protected static bool IsCharacterCategoryCore(ArmyResumeRecord unit, IReadOnlyDictionary<int, string> categoryLookup)
    {
        return CompanySelectionUnitDetailsWorkflow.IsCharacterCategory(unit, categoryLookup);
    }

    /// <summary>
    /// Resets the unit logo canvas to a blank state.
    /// The optional <paramref name="logMessage"/> is written to stderr for diagnostics.
    /// </summary>
    protected static void ClearSelectedUnitLogoCore(UnitDisplayConfigurationsView unitDisplayView, string? logMessage = null)
    {
        CompanySelectionUnitDetailsWorkflow.ClearSelectedUnitLogo(unitDisplayView, logMessage);
    }

    /// <summary>
    /// Asynchronously loads the best available logo for the selected unit by invoking the supplied stream opener
    /// and rendering the SVG into the unit display canvas.
    /// </summary>
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
