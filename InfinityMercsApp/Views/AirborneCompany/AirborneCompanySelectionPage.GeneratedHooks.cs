using InfinityMercsApp.Services;
using InfinityMercsApp.ViewModels;
using InfinityMercsApp.Views.Common;
using InfinityMercsApp.Views.Controls;
using AirborneGen = InfinityMercsApp.Infrastructure.Providers.AirborneCompanyFactionGenerator;

namespace InfinityMercsApp.Views.AirborneCompany;

public partial class AirborneCompanySelectionPage
{
    private const string ParachutistSkillName = "Parachutist";
    private const string NetworkSupportSkillName = "Network Support (Controlled Jump)";

    protected override int CompanyFactionId => AirborneGen.AirborneCompanyFactionId;

    protected override string CompanyLogoPath => AirborneCompanyLogoPath;

    protected override string CompanyTypeLabel => "Airborne Company";

    protected override SeasonStartPointsView SeasonStartPointsControl => SeasonStartPointsView;

    protected override CompanyUnitSelectionListPanelView UnitSelectionPanelControl => UnitSelectionPanel;

    protected override ContentView UnitFilterPopupHostControl => UnitFilterPopupHost;

    protected override Grid UnitFilterOverlayControl => UnitFilterOverlay;

    protected override IReadOnlyCollection<string> ApplyCaptainSkills(IReadOnlyCollection<string> commonSkills, ViewerProfileItem profile)
    {
        return IsLeftSlotUnit(_selectedUnit)
            ? BuildCaptainSkills(commonSkills, profile.UniqueSkills)
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

    private static List<string> BuildCaptainSkills(IReadOnlyCollection<string> commonSkills, string? uniqueSkills)
    {
        var allExisting = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var skill in commonSkills)
        {
            allExisting.Add(skill);
        }

        if (!string.IsNullOrWhiteSpace(uniqueSkills))
        {
            foreach (var part in CompanyProfileTextService.SplitDisplayLine(uniqueSkills))
            {
                allExisting.Add(part);
            }
        }

        var result = new List<string>(commonSkills);
        if (!allExisting.Contains(ParachutistSkillName))
        {
            result.Add(ParachutistSkillName);
        }

        if (!allExisting.Contains(NetworkSupportSkillName))
        {
            result.Add(NetworkSupportSkillName);
        }

        return result;
    }
}
