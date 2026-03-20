namespace InfinityMercsApp.Domain.Models.Metadata;

public class MetadataDocument
{
    public List<Faction> Factions { get; set; } = [];

    public List<Ammunition> Ammunitions { get; set; } = [];

    public List<Weapon> Weapons { get; set; } = [];

    public List<Skill> Skills { get; set; } = [];

    public List<Equipments> Equips { get; set; } = [];

    public List<HackingProgram> Hack { get; set; } = [];

    public List<MartialArt> MartialArts { get; set; } = [];

    public List<Metachemistry> Metachemistry { get; set; } = [];

    public List<Booty> Booty { get; set; } = [];
}
