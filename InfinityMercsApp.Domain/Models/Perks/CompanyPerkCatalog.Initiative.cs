namespace InfinityMercsApp.Domain.Models.Perks;

public static partial class CompanyPerkCatalog
{
    private static PerkNodeList GenerateInitiativePerkNodeList()
    {
        var t1r1 = new PerkNode
        {
            Id = "initiative-track-1-tier-1",
            Name = "Minelayer",
            Tier = 1,
            SkillsEquipmentGained = BuildSkillsEquipmentGained("Minelayer")
        };
        var t1r2 = new PerkNode
        {
            Id = "initiative-track-1-tier-2",
            Name = "Minelayer (2)",
            Tier = 2,
            ParentId = t1r1.Id,
            SkillsEquipmentGained = BuildSkillsEquipmentGained("Minelayer (2)")
        };

        var t2r2 = new PerkNode
        {
            Id = "initiative-track-2-tier-2",
            Name = "Combat Jump",
            Tier = 2,
            SkillsEquipmentGained = BuildSkillsEquipmentGained("Combat Jump")
        };
        var t2r3 = new PerkNode
        {
            Id = "initiative-track-2-tier-3",
            Name = "Combat Jump +3",
            Tier = 3,
            ParentId = t2r2.Id,
            SkillsEquipmentGained = BuildSkillsEquipmentGained("Combat Jump +3")
        };
        t2r2.AddChild(t2r3);

        var t3r2 = new PerkNode
        {
            Id = "initiative-track-3-tier-2",
            Name = "Forward Deployment (+8\")",
            Tier = 2,
            SkillsEquipmentGained = BuildSkillsEquipmentGained("Forward Deployment (+8\")")
        };
        var t3r3 = new PerkNode
        {
            Id = "initiative-track-3-tier-3",
            Name = "Infiltration",
            Tier = 3,
            ParentId = t3r2.Id,
            SkillsEquipmentGained = BuildSkillsEquipmentGained("Infiltration")
        };
        var t3r4 = new PerkNode
        {
            Id = "initiative-track-3-tier-4",
            Name = "Infiltration (+3)",
            Tier = 4,
            ParentId = t3r3.Id,
            SkillsEquipmentGained = BuildSkillsEquipmentGained("Infiltration (+3)")
        };
        t3r2.AddChild(t3r3);
        t3r3.AddChild(t3r4);

        var t4r2 = new PerkNode
        {
            Id = "initiative-track-4-tier-2",
            Name = "Parachutist",
            Tier = 2,
            SkillsEquipmentGained = BuildSkillsEquipmentGained("Parachutist")
        };
        var t4r5 = new PerkNode
        {
            Id = "initiative-track-4-tier-5",
            Name = "Parachutist (Deployment Zone)",
            Tier = 5,
            ParentId = t4r2.Id,
            SkillsEquipmentGained = BuildSkillsEquipmentGained("Parachutist (Deployment Zone)")
        };
        t4r2.AddChild(t4r5);

        var t5r1 = new PerkNode
        {
            Id = "initiative-track-5-tier-1",
            Name = "Sapper",
            Tier = 1,
            SkillsEquipmentGained = BuildSkillsEquipmentGained("Sapper")
        };

        var t6r1 = new PerkNode
        {
            Id = "initiative-track-6-tier-1",
            Name = "MOV 6-2",
            Tier = 1,
            SkillsEquipmentGained = BuildSkillsEquipmentGained("MOV 6-2")
        };
        t6r1.MOV = "6-2";
        var t6r2 = new PerkNode
        {
            Id = "initiative-track-6-tier-2",
            Name = "MOV 6-4",
            Tier = 2,
            ParentId = t6r1.Id,
            SkillsEquipmentGained = BuildSkillsEquipmentGained("MOV 6-4")
        };
        t6r2.MOV = "6-4";
        var t6r4 = new PerkNode
        {
            Id = "initiative-track-6-tier-4",
            Name = "MOV 6-6",
            Tier = 4,
            ParentId = t6r2.Id,
            SkillsEquipmentGained = BuildSkillsEquipmentGained("MOV 6-6")
        };
        t6r4.MOV = "6-6";
        t6r1.AddChild(t6r2);
        t6r2.AddChild(t6r4);

        var t7r3 = new PerkNode
        {
            Id = "initiative-track-7-tier-3",
            Name = "Covering Fire",
            Tier = 3,
            SkillsEquipmentGained = BuildSkillsEquipmentGained("Covering Fire")
        };

        return new PerkNodeList
        {
            ListId = "initiative-1-7",
            ListName = "Initiative",
            IsRandomlyGenerated = true,
            ListRollRanges = ParseRollRanges("1-7"),
            Roots = [t1r1, t1r2, t2r2, t3r2, t4r2, t5r1, t6r1, t7r3]
        };
    }
}
