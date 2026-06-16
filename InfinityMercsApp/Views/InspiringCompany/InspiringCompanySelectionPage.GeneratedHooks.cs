using InfinityMercsApp.ViewModels;
using InfinityMercsApp.Views.Common;
using InfinityMercsApp.Views.Controls;
using InspiringGen = InfinityMercsApp.Infrastructure.Providers.InspiringCompanyFactionGenerator;

namespace InfinityMercsApp.Views.InspiringCompany;

public partial class InspiringCompanySelectionPage
{
    private const string InspiringLeadershipSkillName = "Inspiring Leadership";

    protected override int CompanyFactionId => InspiringGen.InspiringCompanyFactionId;

    protected override string CompanyLogoPath => InspiringCompanyLogoPath;

    protected override string CompanyTypeLabel => "Inspiring Leader";

    protected override SeasonStartPointsView SeasonStartPointsControl => SeasonStartPointsView;

    protected override CompanyUnitSelectionListPanelView UnitSelectionPanelControl => UnitSelectionPanel;

    protected override ContentView UnitFilterPopupHostControl => UnitFilterPopupHost;

    protected override Grid UnitFilterOverlayControl => UnitFilterOverlay;

    protected override IReadOnlyCollection<string> ApplyCaptainSkills(IReadOnlyCollection<string> commonSkills, ViewerProfileItem profile)
    {
        return profile.IsLieutenant
            ? EnsureInspiringLeadershipSkill(commonSkills)
            : commonSkills;
    }

    protected override async Task StartCompanyAsync()
    {
        await ExecuteStartCompanyAsync<ArmyFactionSelectionItem, MercsCompanyEntry, SavedImprovedCaptainStats>(
            CompanyName,
            MercsCompanyEntries,
            false,
            _factionSelectionState.LeftSlotFaction,
            null,
            Factions,
            SpecOpsProvider,
            ShowUnitsInInches,
            SelectedStartSeasonPoints,
            SeasonPointsCapText,
            factionId => ArmyDataService.GetMetadataFactionById(factionId)?.Name,
            stats => stats.CaptainName);
    }

    private static IReadOnlyCollection<string> EnsureInspiringLeadershipSkill(IReadOnlyCollection<string> skills)
    {
        if (skills.Any(x => string.Equals(x?.Trim(), InspiringLeadershipSkillName, StringComparison.OrdinalIgnoreCase)))
        {
            return skills;
        }

        var merged = skills
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .ToList();
        merged.Add(InspiringLeadershipSkillName);
        return merged;
    }
}
