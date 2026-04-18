namespace InfinityMercsApp.Domain.Models.Perks;

public static partial class CompanyPerkCatalog
{
    private static PerkNodeList GenerateMechaPerkNodeList()
    {
        var track1tier1 = new PerkNode
        {
            Id = "mecha-track-1-tier-1",
            Name = "S6, +1 ARM",
            Tier = 1,
            S = 6,
            ARM = 1
        };

        var track1tier2 = new PerkNode
        {
            Id = "mecha-track-1-tier-2",
            Name = "S7, +1 ARM, SR-1",
            Tier = 2,
            ParentId = track1tier1.Id,
            S = 7,
            ARM = 1,
            SkillsEquipmentGained = BuildSkillsEquipmentGained("SR-1")
        };

        // Mecha track 1 tier 3 is intentionally empty, so tier 4 depends on tier 2.
        var track1tier4 = new PerkNode
        {
            Id = "mecha-track-1-tier-4",
            Name = "+1 ARM",
            Tier = 4,
            ParentId = track1tier2.Id,
            ARM = 1
        };

        var track1tier5 = new PerkNode
        {
            Id = "mecha-track-1-tier-5",
            Name = "S8, +1 ARM, Baggage",
            Tier = 5,
            ParentId = track1tier4.Id,
            S = 8,
            ARM = 1,
            SkillsEquipmentGained = BuildSkillsEquipmentGained("Baggage")
        };

        track1tier1.AddChild(track1tier2);
        track1tier2.AddChild(track1tier4);
        track1tier4.AddChild(track1tier5);

        var track2tier1 = new PerkNode
        {
            Id = "mecha-track-2-tier-1",
            Name = "+3 BTS",
            Tier = 1,
            BTS = 3
        };

        var track2tier2 = new PerkNode
        {
            Id = "mecha-track-2-tier-2",
            Name = "ECM (Total Control -3)",
            Tier = 2,
            ParentId = track2tier1.Id,
            SkillsEquipmentGained = BuildSkillsEquipmentGained("ECM (Total Control -3)")
        };

        var track2tier4 = new PerkNode
        {
            Id = "mecha-track-2-tier-4",
            Name = "ECM (Hacker -3)",
            Tier = 4,
            ParentId = track2tier2.Id,
            SkillsEquipmentGained = BuildSkillsEquipmentGained("ECM (Hacker -3)")
        };

        var track2tier5 = new PerkNode
        {
            Id = "mecha-track-2-tier-5",
            Name = "+3 BTS",
            Tier = 5,
            ParentId = track2tier4.Id,
            BTS = 3
        };

        track2tier1.AddChild(track2tier2);
        track2tier2.AddChild(track2tier4);
        track2tier4.AddChild(track2tier5);

        var track3tier3 = new PerkNode
        {
            Id = "mecha-track-3-tier-3",
            Name = "Primary Weapon Gains Anti-Material Mode (B1 EXP)",
            Tier = 3,
            SkillsEquipmentGained = BuildSkillsEquipmentGained("Primary Weapon Gains Anti-Material Mode (B1 EXP)")
        };

        var track3tier4 = new PerkNode
        {
            Id = "mecha-track-3-tier-4",
            Name = "Buy HRMC for MULTI HMG Price",
            Tier = 4,
            ParentId = track3tier3.Id,
            SkillsEquipmentGained = BuildSkillsEquipmentGained("Buy HRMC for MULTI HMG Price")
        };

        track3tier3.AddChild(track3tier4);

        var track4tier1 = new PerkNode
        {
            Id = "mecha-track-4-tier-1",
            Name = "Aerial, No Cover, Super Jump (Jet Propulsion), Tech-Recovery",
            Tier = 1,
            SkillsEquipmentGained = BuildSkillsEquipmentGained("Aerial, No Cover, Super Jump (Jet Propulsion), Tech-Recovery")
        };

        var track4tier2 = new PerkNode
        {
            Id = "mecha-track-4-tier-2",
            Name = "Dodge (+3), Dodge (-3)",
            Tier = 2,
            ParentId = track4tier1.Id,
            SkillsEquipmentGained = BuildSkillsEquipmentGained("Dodge (+3), Dodge (-3)")
        };

        var track4tier3 = new PerkNode
        {
            Id = "mecha-track-4-tier-3",
            Name = "Super Jump (3\")",
            Tier = 3,
            ParentId = track4tier2.Id,
            SkillsEquipmentGained = BuildSkillsEquipmentGained("Super Jump (3\")")
        };

        var track4tier4 = new PerkNode
        {
            Id = "mecha-track-4-tier-4",
            Name = "Move 8-2",
            Tier = 4,
            ParentId = track4tier3.Id,
            MOV = "8-2"
        };

        track4tier1.AddChild(track4tier2);
        track4tier2.AddChild(track4tier3);
        track4tier3.AddChild(track4tier4);

        var track5tier2 = new PerkNode
        {
            Id = "mecha-track-5-tier-2",
            Name = "TAG may equip two primaries",
            Tier = 2,
            SkillsEquipmentGained = BuildSkillsEquipmentGained("TAG may equip two primaries")
        };

        var track5tier3 = new PerkNode
        {
            Id = "mecha-track-5-tier-3",
            Name = "Tactical Awareness",
            Tier = 3,
            SkillsEquipmentGained = BuildSkillsEquipmentGained("Tactical Awareness")
        };

        var track6tier1 = new PerkNode
        {
            Id = "mecha-track-6-tier-1",
            Name = "Ancillary: Gains any role TAG has",
            Tier = 1,
            SkillsEquipmentGained = BuildSkillsEquipmentGained("Ancillary: Gains any role TAG has")
        };

        var track6tier2 = new PerkNode
        {
            Id = "mecha-track-6-tier-2",
            Name = "Ancillary: +3 WIP",
            Tier = 2,
            ParentId = track6tier1.Id,
            WIP = 3,
            SkillsEquipmentGained = BuildSkillsEquipmentGained("Ancillary: +3 WIP")
        };

        var track6tier4 = new PerkNode
        {
            Id = "mecha-track-6-tier-4",
            Name = "Ancillary: Gains Explode, can self-trigger",
            Tier = 4,
            SkillsEquipmentGained = BuildSkillsEquipmentGained("Ancillary: Gains Explode, can self-trigger")
        };

        track6tier1.AddChild(track6tier2);

        var track7tier1 = new PerkNode
        {
            Id = "mecha-track-7-tier-1",
            Name = "+1 Wound Skill",
            Tier = 1,
            SkillsEquipmentGained = BuildSkillsEquipmentGained("+1 Wound Skill")
        };

        var track7tier4 = new PerkNode
        {
            Id = "mecha-track-7-tier-4",
            Name = "+1 Wound Skill",
            Tier = 4,
            ParentId = track7tier1.Id,
            SkillsEquipmentGained = BuildSkillsEquipmentGained("+1 Wound Skill")
        };

        track7tier1.AddChild(track7tier4);

        return new PerkNodeList
        {
            ListId = "mecha",
            ListName = "Mecha",
            IsRandomlyGenerated = false,
            ListRollRanges = [],
            Roots =
            [
                track1tier1,
                track2tier1,
                track3tier3,
                track4tier1,
                track5tier2,
                track5tier3,
                track6tier1,
                track6tier4,
                track7tier1
            ]
        };
    }
}
