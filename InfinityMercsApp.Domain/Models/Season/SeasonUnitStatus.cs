namespace InfinityMercsApp.Domain.Models.Season;

public class SeasonUnitStatus
{
    public string UnitName { get; set; } = string.Empty;
    public int TotalExperience { get; set; }
    public int Renown { get; set; }
    public int Notoriety { get; set; }
    public List<string> Perks { get; set; } = [];
    public List<SeasonUnitGear> Gear { get; set; } = [];
    public SeasonUnitStats? ModifiedStats { get; set; }
}
