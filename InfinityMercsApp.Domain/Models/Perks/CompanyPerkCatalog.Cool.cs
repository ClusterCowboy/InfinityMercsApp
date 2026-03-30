namespace InfinityMercsApp.Domain.Models.Perks;

public static partial class CompanyPerkCatalog
{
    private static PerkNodeList GenerateCoolPerkNodeList()
    {
        var t1r1 = new PerkNode
        {
            Id = "cool-track-1-tier-1",
            Name = "Decoy",
            Tier = 1,
            SkillsEquipmentGained = BuildSkillsEquipmentGained("Decoy")
        };

        var t2r1 = new PerkNode
        {
            Id = "cool-track-2-tier-1",
            Name = "Stealth",
            Tier = 1,
            SkillsEquipmentGained = BuildSkillsEquipmentGained("Stealth")
        };
        

        var t3r2 = new PerkNode
        {
            Id = "cool-track-3-tier-2",
            Name = "Hidden Deployment",
            Tier = 2,
            SkillsEquipmentGained = BuildSkillsEquipmentGained("Hidden Deployment")
        };

        var t4r2 = new PerkNode
        {
            Id = "cool-track-4-tier-2",
            Name = "Mimetism (-3)",
            Tier = 2,
            SkillsEquipmentGained = BuildSkillsEquipmentGained("Mimetism (-3)")
        };
        var t4r3 = new PerkNode
        {
            Id = "cool-track-4-tier-3",
            Name = "Mimetism (-6)",
            Tier = 3,
            SkillsEquipmentGained = BuildSkillsEquipmentGained("Mimetism (-6)")
        };

        var t5r1 = new PerkNode
        {
            Id = "cool-track-5-tier-1",
            Name = "CC (-3)",
            Tier = 1,
            SkillsEquipmentGained = BuildSkillsEquipmentGained("CC (-3)")
        };
        var t5r3 = new PerkNode
        {
            Id = "cool-track-5-tier-3",
            Name = "CC (-6)",
            Tier = 3,
            ParentId = t5r1.Id,
            SkillsEquipmentGained = BuildSkillsEquipmentGained("CC (-6)")
        };
        t5r1.AddChild(t5r3);

        var t6r1 = new PerkNode
        {
            Id = "cool-track-6-tier-1",
            Name = "Martial Arts L1",
            Tier = 1,
            SkillsEquipmentGained = BuildSkillsEquipmentGained("Martial Arts L1")
        };
        var t6r3 = new PerkNode
        {
            Id = "cool-track-6-tier-3",
            Name = "Martial Arts L3",
            Tier = 3,
            ParentId = t6r1.Id,
            SkillsEquipmentGained = BuildSkillsEquipmentGained("Martial Arts L3")
        };
        var t6r5 = new PerkNode
        {
            Id = "cool-track-6-tier-5",
            Name = "Martial Arts L5",
            Tier = 5,
            ParentId = t6r3.Id,
            SkillsEquipmentGained = BuildSkillsEquipmentGained("Martial Arts L5")
        };
        t6r1.AddChild(t6r3);
        t6r3.AddChild(t6r5);

        var t7r3 = new PerkNode
        {
            Id = "cool-track-7-tier-3",
            Name = "+3 CC",
            Tier = 3,
            SkillsEquipmentGained = BuildSkillsEquipmentGained("+3 CC")
        };
        t7r3.CC = 3;

        var t8r3 = new PerkNode
        {
            Id = "cool-track-8-tier-3",
            Name = "Camouflage, Surprise Attack (-3)",
            Tier = 3,
            SkillsEquipmentGained = BuildSkillsEquipmentGained("Camouflage, Surprise Attack (-3)")
        };

        var t9r5 = new PerkNode
        {
            Id = "cool-track-9-tier-5",
            Name = "Protheion",
            Tier = 5,
            SkillsEquipmentGained = BuildSkillsEquipmentGained("Protheion")
        };

        return new PerkNodeList
        {
            ListId = "cool-5-10",
            ListName = "Cool",
            IsRandomlyGenerated = true,
            ListRollRanges = CompanyPerkDefinitionParser.ParseRollRanges("5-10"),
            Roots = [t1r1, t2r1, t3r2, t4r2, t4r3, t5r1, t6r1, t7r3, t8r3, t9r5]
        };
    }
}
