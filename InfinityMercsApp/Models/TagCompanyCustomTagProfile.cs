using System;

namespace InfinityMercsApp.Models;

public sealed record TagCompanyCustomTagProfile
{
    public string Name { get; init; } = string.Empty;
    public int Cost { get; init; }
    public string Statline { get; init; } = "-";
    public string Equipment { get; init; } = "-";
    public string Skills { get; init; } = "-";
    public string RangedWeapons { get; init; } = "-";
    public string CcWeapons { get; init; } = "-";
    public int ExperiencePoints { get; init; }

    public bool HasEquipmentLine => !string.Equals(Equipment, "-", StringComparison.Ordinal);
}
