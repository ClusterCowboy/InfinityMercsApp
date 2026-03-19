using System;
using System.Collections.Generic;
using System.Linq;

namespace InfinityMercsApp.Models;

public sealed class TagCompanyCustomTagModel
{
    public const int DefaultSourceFactionId = -2000;
    public const int DefaultSourceUnitId = -2000;
    public const int SpecOpsExperienceBudget = 20;

    public static TagCompanyCustomTagModel Default { get; } = new();

    public string DefaultName { get; } = "Custom TAG";
    public int Cost { get; } = 40;
    public string ProfileKey { get; } = "__tag-company-custom-tag__";
    public string IconPath { get; } = "SVGCache/MercsIcons/noun-battle-mech-1731140.svg";
    public string UnitTypeCode { get; } = "TAG";
    public static IReadOnlyList<string> RestrictedSpecOpsKeywords { get; } =
    [
        "Forward Deployment",
        "Strategic Deployment",
        "Infiltration",
        "Impersonation",
        "Parachutist",
        "Combat Jump",
        "Engineer"
    ];

    public string Mov { get; } = "6-2";
    public int Cc { get; } = 15;
    public int Bs { get; } = 12;
    public int Ph { get; } = 14;
    public int Wip { get; } = 12;
    public int Arm { get; } = 5;
    public int Bts { get; } = 3;
    public string VitalityHeader { get; } = "STR";
    public int Vitality { get; } = 2;
    public int Silhouette { get; } = 5;
    public int Availability { get; } = 1;

    public string BaseRangedWeapons { get; } = "Combi Rifle, Pistol";
    public string BaseCcWeapons { get; } = "CCW(PS=6)";
    public IReadOnlyList<string> BaseSkills { get; } =
    [
        "NWI",
        "CC Weapon (Antimaterial)",
        "Dodge(PH=11)",
        "Gizmokit(PH=11)",
        "Immunity(Shock)",
        "ECM(Guided -6)"
    ];

    public string BaseSkillsText => string.Join(", ", BaseSkills);

    public bool IsCustomTagProfile(string? profileKey)
    {
        return string.Equals(profileKey, ProfileKey, StringComparison.OrdinalIgnoreCase);
    }

    public string ResolveName(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? DefaultName
            : value.Trim();
    }

    public string ResolveSavedStatline(string? statline)
    {
        return string.IsNullOrWhiteSpace(statline) || statline.Trim() == "-"
            ? BuildStatline()
            : statline.Trim();
    }

    public string ResolvePackagedLogoPath(string? packagedLogoPath)
    {
        return string.IsNullOrWhiteSpace(packagedLogoPath)
            ? IconPath
            : packagedLogoPath.Trim();
    }

    public static string NormalizeProfileText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();
    }

    public TagCompanyCustomTagProfile BuildProfile(
        string? customName = null,
        IEnumerable<string?>? extraWeapons = null,
        IEnumerable<string?>? extraSkills = null,
        IEnumerable<string?>? extraEquipment = null,
        int ccBonus = 0,
        int bsBonus = 0,
        int phBonus = 0,
        int wipBonus = 0,
        int armBonus = 0,
        int btsBonus = 0,
        int vitalityBonus = 0,
        int spentExperience = 0)
    {
        var normalizedWeapons = NormalizeChoices(extraWeapons);
        var normalizedSkills = NormalizeChoices(extraSkills);
        var normalizedEquipment = NormalizeChoices(extraEquipment);

        return new TagCompanyCustomTagProfile
        {
            Name = ResolveName(customName),
            Cost = Cost,
            Statline = BuildStatline(
                ccBonus: ccBonus,
                bsBonus: bsBonus,
                phBonus: phBonus,
                wipBonus: wipBonus,
                armBonus: armBonus,
                btsBonus: btsBonus,
                vitalityBonus: vitalityBonus),
            Equipment = BuildEquipmentText(normalizedEquipment),
            Skills = BuildSkillsText(normalizedSkills),
            RangedWeapons = BuildRangedWeaponsText(normalizedWeapons),
            CcWeapons = BuildCcWeaponsText(),
            ExperiencePoints = Math.Max(0, spentExperience)
        };
    }

    public string BuildStatline(
        int ccBonus = 0,
        int bsBonus = 0,
        int phBonus = 0,
        int wipBonus = 0,
        int armBonus = 0,
        int btsBonus = 0,
        int vitalityBonus = 0)
    {
        return $"MOV {Mov} | CC {Cc + ccBonus} | BS {Bs + bsBonus} | PH {Ph + phBonus} | WIP {Wip + wipBonus} | ARM {Arm + armBonus} | BTS {Bts + btsBonus} | {VitalityHeader} {Vitality + vitalityBonus} | S {Silhouette}";
    }

    public string BuildRangedWeaponsText(IEnumerable<string>? extraWeapons)
    {
        return MergeBaseAndChoices(BaseRangedWeapons, extraWeapons, prependPlus: true);
    }

    public string BuildCcWeaponsText()
    {
        return BaseCcWeapons;
    }

    public string BuildSkillsText(IEnumerable<string>? extraSkills)
    {
        return MergeBaseAndChoices(BaseSkillsText, extraSkills, prependPlus: true);
    }

    public string BuildEquipmentText(IEnumerable<string>? extraEquipment)
    {
        return MergeBaseAndChoices("-", extraEquipment, prependPlus: true);
    }

    private static List<string> NormalizeChoices(IEnumerable<string?>? values)
    {
        if (values is null)
        {
            return [];
        }

        return values
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string MergeBaseAndChoices(string baseValue, IEnumerable<string>? extraValues, bool prependPlus)
    {
        var lines = SplitProfileLines(baseValue);
        if (extraValues is not null)
        {
            foreach (var value in extraValues
                         .Where(x => !string.IsNullOrWhiteSpace(x))
                         .Select(x => x.Trim())
                         .Distinct(StringComparer.OrdinalIgnoreCase))
            {
                lines.Add(prependPlus ? $"+ {value}" : value);
            }
        }

        return lines.Count == 0
            ? "-"
            : string.Join(Environment.NewLine, lines);
    }

    private static List<string> SplitProfileLines(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        return text
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x) && x != "-")
            .ToList();
    }
}
