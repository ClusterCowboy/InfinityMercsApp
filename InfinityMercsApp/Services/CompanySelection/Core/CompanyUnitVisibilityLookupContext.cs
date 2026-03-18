using ArmySpecopsUnitRecord = InfinityMercsApp.Domain.Models.Army.SpecopsUnit;

namespace InfinityMercsApp.Views.Common;

public sealed class CompanyUnitVisibilityLookupContext
{
    public Dictionary<int, IReadOnlyDictionary<int, string>> SkillsByFactionId { get; } = [];
    public Dictionary<int, IReadOnlyDictionary<int, string>> TypeByFactionId { get; } = [];
    public Dictionary<int, IReadOnlyDictionary<int, string>> CharsByFactionId { get; } = [];
    public Dictionary<int, IReadOnlyDictionary<int, string>> EquipByFactionId { get; } = [];
    public Dictionary<int, IReadOnlyDictionary<int, string>> WeaponsByFactionId { get; } = [];
    public Dictionary<int, IReadOnlyDictionary<int, string>> AmmoByFactionId { get; } = [];
    public Dictionary<int, Dictionary<int, ArmySpecopsUnitRecord>> SpecopsByFactionId { get; } = [];
}
