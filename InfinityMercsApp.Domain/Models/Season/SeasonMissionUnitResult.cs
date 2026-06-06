namespace InfinityMercsApp.Domain.Models.Season;

public class SeasonMissionUnitResult
{
    public string UnitName { get; set; } = string.Empty;
    public string? Injury { get; set; }
    public bool TriedObjective { get; set; }
    public bool CompletedObjective { get; set; }
    public int AssistCount { get; set; }
    public int StatesInflicted { get; set; }
    public bool ScannedEnemy { get; set; }
    public bool ScannedEnemyWithFO { get; set; }
    public bool TagAndBag { get; set; }
    public bool ConsciousAtEnd { get; set; }
    public bool IsMvp { get; set; }
}
