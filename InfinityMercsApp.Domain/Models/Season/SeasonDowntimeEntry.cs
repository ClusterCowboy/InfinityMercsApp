namespace InfinityMercsApp.Domain.Models.Season;

public class SeasonDowntimeEntry
{
    public string EventId { get; set; } = string.Empty;
    public string ChosenPlan { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty;
    public int CrGain { get; set; }
    public int NotorietyGain { get; set; }
    public int XpGain { get; set; }
    public int SpentCr { get; set; }
    public string OtherEffects { get; set; } = string.Empty;
}
