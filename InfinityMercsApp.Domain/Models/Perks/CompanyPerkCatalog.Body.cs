namespace InfinityMercsApp.Domain.Models.Perks;

public static partial class CompanyPerkCatalog
{
    private static PerkNodeList GenerateBodyPerkNodeList()
    {
        var t1r1 = new PerkNode
        {
            Id = "body-track-1-tier-1",
            Name = "Climbing Plus",
            Tier = 1,
            SkillsEquipmentGained = BuildSkillsEquipmentGained("Climbing Plus")
        };
        var t1r3 = new PerkNode
        {
            Id = "body-track-1-tier-3",
            Name = "Super Jump",
            Tier = 3,
            ParentId = t1r1.Id,
            SkillsEquipmentGained = BuildSkillsEquipmentGained("Super Jump")
        };
        t1r1.AddChild(t1r3);

        var t2r1 = new PerkNode
        {
            Id = "body-track-2-tier-1",
            Name = "Immunity (DA), Immunity (Shock)",
            Tier = 1,
            SkillsEquipmentGained = BuildSkillsEquipmentGained("Immunity (DA), Immunity (Shock)")
        };
        var t2r2 = new PerkNode
        {
            Id = "body-track-2-tier-2",
            Name = "Immunity (AP)",
            Tier = 2,
            ParentId = t2r1.Id,
            SkillsEquipmentGained = BuildSkillsEquipmentGained("Immunity (AP)")
        };
        var t2r3a = new PerkNode
        {
            Id = "body-track-2-tier-3a",
            Name = "Immunity (ARM)",
            Tier = 3,
            ParentId = t2r2.Id,
            SkillsEquipmentGained = BuildSkillsEquipmentGained("Immunity (ARM)")
        };
        var t2r3b = new PerkNode
        {
            Id = "body-track-2-tier-3b",
            Name = "Immunity (BTS)",
            Tier = 3,
            ParentId = t2r2.Id,
            SkillsEquipmentGained = BuildSkillsEquipmentGained("Immunity (BTS)")
        };
        t2r1.AddChild(t2r2);
        t2r2.AddChild(t2r3a);
        t2r2.AddChild(t2r3b);

        var t3r2 = new PerkNode
        {
            Id = "body-track-3-tier-2",
            Name = "Berserk",
            Tier = 2,
            SkillsEquipmentGained = BuildSkillsEquipmentGained("Berserk")
        };
        var t3r3 = new PerkNode
        {
            Id = "body-track-3-tier-3",
            Name = "Berserk (+3)",
            Tier = 3,
            ParentId = t3r2.Id,
            SkillsEquipmentGained = BuildSkillsEquipmentGained("Berserk (+3)")
        };
        t3r2.AddChild(t3r3);

        var t4r1 = new PerkNode
        {
            Id = "body-track-4-tier-1",
            Name = "Trade VITA for STR",
            Tier = 1,
            SkillsEquipmentGained = BuildSkillsEquipmentGained("Trade VITA for STR")
        };
        var t4r3 = new PerkNode
        {
            Id = "body-track-4-tier-3",
            Name = "REM, Remote Presence",
            Tier = 3,
            ParentId = t4r1.Id,
            SkillsEquipmentGained = BuildSkillsEquipmentGained("REM, Remote Presence")
        };
        t4r1.AddChild(t4r3);

        var t5r2 = new PerkNode
        {
            Id = "body-track-5-tier-2",
            Name = "Regeneration",
            Tier = 2,
            SkillsEquipmentGained = BuildSkillsEquipmentGained("Regeneration")
        };

        var t6r1 = new PerkNode
        {
            Id = "body-track-6-tier-1",
            Name = "Terrain (Total)",
            Tier = 1,
            SkillsEquipmentGained = BuildSkillsEquipmentGained("Terrain (Total)")
        };

        var t7r2 = new PerkNode
        {
            Id = "body-track-7-tier-2",
            Name = "+1 ARM",
            Tier = 2,
            SkillsEquipmentGained = BuildSkillsEquipmentGained("+1 ARM")
        };
        t7r2.ARM = 1;
        var t7r4 = new PerkNode
        {
            Id = "body-track-7-tier-4",
            Name = "+1 ARM",
            Tier = 4,
            ParentId = t7r2.Id,
            SkillsEquipmentGained = BuildSkillsEquipmentGained("+1 ARM")
        };
        t7r4.ARM = 1;
        t7r2.AddChild(t7r4);

        var t8r2 = new PerkNode
        {
            Id = "body-track-8-tier-2",
            Name = "Natural Born Warrior",
            Tier = 2,
            SkillsEquipmentGained = BuildSkillsEquipmentGained("Natural Born Warrior")
        };

        var t9r3 = new PerkNode
        {
            Id = "body-track-9-tier-3",
            Name = "+1 Wounds Skill",
            Tier = 3,
            SkillsEquipmentGained = BuildSkillsEquipmentGained("+1 Wounds Skill")
        };
        var t9r5 = new PerkNode
        {
            Id = "body-track-9-tier-5",
            Name = "+1 Wounds Skill",
            Tier = 5,
            ParentId = t9r3.Id,
            SkillsEquipmentGained = BuildSkillsEquipmentGained("+1 Wounds Skill")
        };
        t9r3.AddChild(t9r5);

        var t10r3 = new PerkNode
        {
            Id = "body-track-10-tier-3",
            Name = "+2 PH + CC Attack (SR-2)",
            Tier = 3,
            SkillsEquipmentGained = BuildSkillsEquipmentGained("+2 PH + CC Attack (SR-2)")
        };

        return new PerkNodeList
        {
            ListId = "body-8-13",
            ListName = "Body",
            IsRandomlyGenerated = true,
            ListRollRanges = ParseRollRanges("8-13"),
            Roots = [t1r1, t2r1, t3r2, t4r1, t5r2, t6r1, t7r2, t8r2, t9r3, t10r3]
        };
    }
}
