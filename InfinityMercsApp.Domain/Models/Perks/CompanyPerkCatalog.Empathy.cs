namespace InfinityMercsApp.Domain.Models.Perks;

public static partial class CompanyPerkCatalog
{
    private static PerkNodeList GenerateEmpathyPerkNodeList()
    {
        var t1r1 = new PerkNode
        {
            Id = "empathy-track-1-tier-1",
            Name = "Logistician, Baggage",
            Tier = 1,
            SkillsEquipmentGained = BuildSkillsEquipmentGained("Logistician, Baggage")
        };

        var t2r1 = new PerkNode
        {
            Id = "empathy-track-2-tier-1",
            Name = "Strategos L1 (Reworked)",
            Tier = 1,
            SkillsEquipmentGained = BuildSkillsEquipmentGained("Strategos L1 (Reworked)")
        };
        var t2r3 = new PerkNode
        {
            Id = "empathy-track-2-tier-3",
            Name = "Strategos L2 (Reworked)",
            Tier = 3,
            ParentId = t2r1.Id,
            SkillsEquipmentGained = BuildSkillsEquipmentGained("Strategos L2 (Reworked)")
        };
        t2r1.AddChild(t2r3);

        var t3r2 = new PerkNode
        {
            Id = "empathy-track-3-tier-2",
            Name = "Counterintelligence",
            Tier = 2,
            SkillsEquipmentGained = BuildSkillsEquipmentGained("Counterintelligence")
        };

        var t4r3 = new PerkNode
        {
            Id = "empathy-track-4-tier-3",
            Name = "Lieutenant (+1 Order) Must be Captain",
            Tier = 3,
            SkillsEquipmentGained = BuildSkillsEquipmentGained("Lieutenant (+1 Order) Must be Captain")
        };
        var t5r3 = new PerkNode
        {
            Id = "empathy-track-5-tier-3",
            Name = "Lieutenant (+1 Command Token) Must be Captain",
            Tier = 3,
            SkillsEquipmentGained = BuildSkillsEquipmentGained("Lieutenant (+1 Command Token) Must be Captain")
        };
        var t6r3 = new PerkNode
        {
            Id = "empathy-track-6-tier-3",
            Name = "Lieutenant Roll (+1 B) Must be Captain",
            Tier = 3,
            SkillsEquipmentGained = BuildSkillsEquipmentGained("Lieutenant Roll (+1 B) Must be Captain")
        };

        var t7r3 = new PerkNode
        {
            Id = "empathy-track-7-tier-3",
            Name = "Holomask",
            Tier = 3,
            SkillsEquipmentGained = BuildSkillsEquipmentGained("Holomask")
        };

        var t8r5 = new PerkNode
        {
            Id = "empathy-track-8-tier-5",
            Name = "Tactical Awareness",
            Tier = 5,
            SkillsEquipmentGained = BuildSkillsEquipmentGained("Tactical Awareness")
        };

        var t9r1 = new PerkNode
        {
            Id = "empathy-track-9-tier-1",
            Name = "Discover (+3)",
            Tier = 1,
            SkillsEquipmentGained = BuildSkillsEquipmentGained("Discover (+3)")
        };
        var t9r2 = new PerkNode
        {
            Id = "empathy-track-9-tier-2",
            Name = "Discover (Reroll)",
            Tier = 2,
            ParentId = t9r1.Id,
            SkillsEquipmentGained = BuildSkillsEquipmentGained("Discover (Reroll)")
        };
        t9r1.AddChild(t9r2);

        return new PerkNodeList
        {
            ListId = "empathy-17-20-1-4",
            ListName = "Empathy",
            IsRandomlyGenerated = true,
            ListRollRanges = CompanyPerkDefinitionParser.ParseRollRanges("17-20/1-4"),
            Roots = [t1r1, t2r1, t3r2, t4r3, t5r3, t6r3, t7r3, t8r5, t9r1]
        };
    }
}
