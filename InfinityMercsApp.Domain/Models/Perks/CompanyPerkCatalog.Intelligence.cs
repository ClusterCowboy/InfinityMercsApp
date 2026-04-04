namespace InfinityMercsApp.Domain.Models.Perks;

public static partial class CompanyPerkCatalog
{
    private static PerkNodeList GenerateIntelligencePerkNodeList()
    {
        var t1r3 = new PerkNode
        {
            Id = "intelligence-track-1-tier-3",
            Name = "Forward Observer (+1 SD) (Role), Flash Pulse (+1 SD)",
            Tier = 3,
            SkillsEquipmentGained = BuildSkillsEquipmentGained("Forward Observer (+1 SD) (Role), Flash Pulse (+1 SD)")
        };

        var t2r1 = new PerkNode
        {
            Id = "intelligence-track-2-tier-1",
            Name = "Medikit (no slot required)",
            Tier = 1,
            SkillsEquipmentGained = BuildSkillsEquipmentGained("Medikit (no slot required)")
        };
        var t2r3 = new PerkNode
        {
            Id = "intelligence-track-2-tier-3",
            Name = "Doctor (ReRoll -3) (Role)",
            Tier = 3,
            ParentId = t2r1.Id,
            SkillsEquipmentGained = BuildSkillsEquipmentGained("Doctor (ReRoll -3) (Role)")
        };
        var t2r4 = new PerkNode
        {
            Id = "intelligence-track-2-tier-4",
            Name = "Doctor (2W)",
            Tier = 4,
            ParentId = t2r3.Id,
            SkillsEquipmentGained = BuildSkillsEquipmentGained("Doctor (2W)")
        };
        t2r1.AddChild(t2r3);
        t2r3.AddChild(t2r4);

        var t3r1 = new PerkNode
        {
            Id = "intelligence-track-3-tier-1",
            Name = "Gizmokit (no slot required)",
            Tier = 1,
            SkillsEquipmentGained = BuildSkillsEquipmentGained("Gizmokit (no slot required)")
        };
        var t3r3 = new PerkNode
        {
            Id = "intelligence-track-3-tier-3",
            Name = "Engineer (ReRoll -3) (Role)",
            Tier = 3,
            ParentId = t3r1.Id,
            SkillsEquipmentGained = BuildSkillsEquipmentGained("Engineer (ReRoll -3) (Role)")
        };
        var t3r5 = new PerkNode
        {
            Id = "intelligence-track-3-tier-5",
            Name = "Engineer (2W)",
            Tier = 5,
            ParentId = t3r3.Id,
            SkillsEquipmentGained = BuildSkillsEquipmentGained("Engineer (2W)")
        };
        t3r1.AddChild(t3r3);
        t3r3.AddChild(t3r5);

        var t4r1 = new PerkNode
        {
            Id = "intelligence-track-4-tier-1",
            Name = "Hacker (Role) No device",
            Tier = 1,
            SkillsEquipmentGained = BuildSkillsEquipmentGained("Hacker (Role) No device")
        };
        var t4r2 = new PerkNode
        {
            Id = "intelligence-track-4-tier-2",
            Name = "Hacking Device",
            Tier = 2,
            ParentId = t4r1.Id,
            SkillsEquipmentGained = BuildSkillsEquipmentGained("Hacking Device")
        };
        var t4r3 = new PerkNode
        {
            Id = "intelligence-track-4-tier-3",
            Name = "Upgrade: White Noise",
            Tier = 3,
            ParentId = t4r2.Id,
            SkillsEquipmentGained = BuildSkillsEquipmentGained("Upgrade: White Noise")
        };
        t4r1.AddChild(t4r2);
        t4r2.AddChild(t4r3);

        var t5r2 = new PerkNode
        {
            Id = "intelligence-track-5-tier-2",
            Name = "Killer Hacking Device",
            Tier = 2,
            ParentId = t4r1.Id,
            SkillsEquipmentGained = BuildSkillsEquipmentGained("Killer Hacking Device")
        };
        var t5r3 = new PerkNode
        {
            Id = "intelligence-track-5-tier-3",
            Name = "Upgrade: Trinity (-3)",
            Tier = 3,
            ParentId = t5r2.Id,
            SkillsEquipmentGained = BuildSkillsEquipmentGained("Upgrade: Trinity (-3)")
        };
        t4r1.AddChild(t5r2);
        t5r2.AddChild(t5r3);

        var t6r2 = new PerkNode
        {
            Id = "intelligence-track-6-tier-2",
            Name = "EVO Hacking Device",
            Tier = 2,
            ParentId = t4r1.Id,
            SkillsEquipmentGained = BuildSkillsEquipmentGained("EVO Hacking Device")
        };
        var t6r3 = new PerkNode
        {
            Id = "intelligence-track-6-tier-3",
            Name = "Network Support",
            Tier = 3,
            ParentId = t6r2.Id,
            SkillsEquipmentGained = BuildSkillsEquipmentGained("Network Support")
        };
        t4r1.AddChild(t6r2);
        t6r2.AddChild(t6r3);

        var t7r3 = new PerkNode
        {
            Id = "intelligence-track-7-tier-3",
            Name = "+2 WIP",
            Tier = 3,
            SkillsEquipmentGained = BuildSkillsEquipmentGained("+2 WIP")
        };
        t7r3.WIP = 2;

        var t8r3 = new PerkNode
        {
            Id = "intelligence-track-8-tier-3",
            Name = "Non-Lethal (+1 SD)",
            Tier = 3,
            SkillsEquipmentGained = BuildSkillsEquipmentGained("Non-Lethal (+1 SD)")
        };

        return new PerkNodeList
        {
            ListId = "intelligence-14-19",
            ListName = "Intelligence",
            IsRandomlyGenerated = true,
            ListRollRanges = ParseRollRanges("14-19"),
            Roots = [t1r3, t2r1, t3r1, t4r1, t7r3, t8r3]
        };
    }
}
