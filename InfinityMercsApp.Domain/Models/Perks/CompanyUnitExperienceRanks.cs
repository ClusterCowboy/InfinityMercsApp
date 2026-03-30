namespace InfinityMercsApp.Domain.Models.Perks;

public static class CompanyUnitExperienceRanks
{
    private static readonly (int MinXp, string RankName)[] OrderedRanks =
    [
        (5, "Newbie"),
        (10, "Hired Gun"),
        (30, "Hitman"),
        (50, "Operative"),
        (75, "Veteran"),
        (105, "Officer"),
        (140, "Legend")
    ];

    public static string GetRankName(int experiencePoints)
    {
        var normalized = Math.Max(0, experiencePoints);
        for (var i = OrderedRanks.Length - 1; i >= 0; i--)
        {
            if (normalized >= OrderedRanks[i].MinXp)
            {
                return OrderedRanks[i].RankName;
            }
        }

        return "Unranked";
    }

    public static int GetRankLevel(int experiencePoints)
    {
        var normalized = Math.Max(0, experiencePoints);
        for (var i = OrderedRanks.Length - 1; i >= 0; i--)
        {
            if (normalized >= OrderedRanks[i].MinXp)
            {
                return i + 1;
            }
        }

        return 0;
    }
}
