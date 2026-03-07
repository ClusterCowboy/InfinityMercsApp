namespace InfinityMercsApp.Infrastructure.Repositories.Models.Army;

using SQLite;

[Table("armies")]
public class Faction
{
    private const int JsaFactionId = 1101;
    private const int JsaShindenbutaiFactionId = 1102;
    private const int JsaObanFactionId = 1103;

    [PrimaryKey]
    public int FactionId { get; set; }

    public string Version { get; set; } = string.Empty;

    public long ImportedAtUnixSeconds { get; set; }

    public string? ReinforcementsJson { get; set; }

    public string? FiltersJson { get; set; }

    public string? FireteamsJson { get; set; }

    public string? RelationsJson { get; set; }

    public string? SpecopsJson { get; set; }

    public string? FireteamChartJson { get; set; }

    public string RawJson { get; set; } = string.Empty;

    public bool IsJsaFaction => DetermineIsJsaFaction(FactionId);

    private static bool DetermineIsJsaFaction(int factionId)
    {
        return factionId is JsaFactionId or JsaShindenbutaiFactionId or JsaObanFactionId;
    }
}
