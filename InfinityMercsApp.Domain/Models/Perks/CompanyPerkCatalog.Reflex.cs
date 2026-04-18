namespace InfinityMercsApp.Domain.Models.Perks;

public static partial class CompanyPerkCatalog
{
    private static PerkNodeList GenerateReflexPerkNodeList()
    {
        var t1r1 = new PerkNode
        {
            Id = "reflex-track-1-tier-1",
            Name = "Triangluated Fire",
            Tier = 1,
            SkillsEquipmentGained = BuildSkillsEquipmentGained("Triangluated Fire")
        };
        var t1r4 = new PerkNode
        {
            Id = "reflex-track-1-tier-4",
            Name = "Marksmanship",
            Tier = 4,
            ParentId = t1r1.Id,
            SkillsEquipmentGained = BuildSkillsEquipmentGained("Marksmanship")
        };
        t1r1.AddChild(t1r4);

        var t2r1 = new PerkNode
        {
            Id = "reflex-track-2-tier-1",
            Name = "Sensor",
            Tier = 1,
            SkillsEquipmentGained = BuildSkillsEquipmentGained("Sensor")
        };

        var t3r1 = new PerkNode
        {
            Id = "reflex-track-3-tier-1",
            Name = "Dodge (+3), Dodge (+1\")",
            Tier = 1,
            SkillsEquipmentGained = BuildSkillsEquipmentGained("Dodge (+3), Dodge (+1\")")
        };
        var t3r2 = new PerkNode
        {
            Id = "reflex-track-3-tier-2",
            Name = "Dodge (+6), Dodge (+2\")",
            Tier = 2,
            ParentId = t3r1.Id,
            SkillsEquipmentGained = BuildSkillsEquipmentGained("Dodge (+6), Dodge (+2\")")
        };
        var t3r3 = new PerkNode
        {
            Id = "reflex-track-3-tier-3",
            Name = "Dodge (-3)",
            Tier = 3,
            ParentId = t3r2.Id,
            SkillsEquipmentGained = BuildSkillsEquipmentGained("Dodge (-3)")
        };
        t3r2.AddChild(t3r3);

        var t4r2 = new PerkNode
        {
            Id = "reflex-track-4-tier-2",
            Name = "Sixth Sense",
            Tier = 2,
            SkillsEquipmentGained = BuildSkillsEquipmentGained("Sixth Sense")
        };

        var t5r2 = new PerkNode
        {
            Id = "reflex-track-5-tier-2",
            Name = "360 Visor (Accessory)",
            Tier = 2,
            SkillsEquipmentGained = BuildSkillsEquipmentGained("360 Visor (Accessory)")
        };

        var t6r2 = new PerkNode
        {
            Id = "reflex-track-6-tier-2",
            Name = "Neurocinetics",
            Tier = 2,
            SkillsEquipmentGained = BuildSkillsEquipmentGained("Neurocinetics")
        };
        var t6r4 = new PerkNode
        {
            Id = "reflex-track-6-tier-4",
            Name = "Neurocinetics (B2 in Active)",
            Tier = 4,
            ParentId = t6r2.Id,
            SkillsEquipmentGained = BuildSkillsEquipmentGained("Neurocinetics (B2 in Active)")
        };
        var t6r5 = new PerkNode
        {
            Id = "reflex-track-6-tier-5",
            Name = "Total Reaction",
            Tier = 5,
            ParentId = t6r4.Id,
            SkillsEquipmentGained = BuildSkillsEquipmentGained("Total Reaction")
        };
        t6r2.AddChild(t6r4);
        t6r4.AddChild(t6r5);

        var t7r3 = new PerkNode
        {
            Id = "reflex-track-7-tier-3",
            Name = "BS Attack (-3)",
            Tier = 3,
            SkillsEquipmentGained = BuildSkillsEquipmentGained("BS Attack (-3)")
        };
        var t7r4 = new PerkNode
        {
            Id = "reflex-track-7-tier-4",
            Name = "BS Attack (+1 SD)",
            Tier = 4,
            ParentId = t7r3.Id,
            SkillsEquipmentGained = BuildSkillsEquipmentGained("BS Attack (+1 SD)")
        };
        t7r3.AddChild(t7r4);

        var t8r2 = new PerkNode
        {
            Id = "reflex-track-8-tier-2",
            Name = "+1 BS",
            Tier = 2,
            SkillsEquipmentGained = BuildSkillsEquipmentGained("+1 BS")
        };
        t8r2.BS = 1;
        var t8r4 = new PerkNode
        {
            Id = "reflex-track-8-tier-4",
            Name = "+1 BS",
            Tier = 4,
            ParentId = t8r2.Id,
            SkillsEquipmentGained = BuildSkillsEquipmentGained("+1 BS")
        };
        t8r4.BS = 1;
        t8r2.AddChild(t8r4);

        return new PerkNodeList
        {
            ListId = "reflex-11-16",
            ListName = "Reflex",
            IsRandomlyGenerated = true,
            ListRollRanges = ParseRollRanges("11-16"),
            Roots = [t1r1, t2r1, t3r1, t3r2, t4r2, t5r2, t6r2, t7r3, t8r2]
        };
    }
}
