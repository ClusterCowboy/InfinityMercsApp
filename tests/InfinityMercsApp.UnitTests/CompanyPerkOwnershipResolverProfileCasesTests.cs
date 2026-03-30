using System.Text.Json;
using InfinityMercsApp.Domain.Models.Perks;
using Microsoft.Data.Sqlite;
using Xunit.Abstractions;

namespace InfinityMercsApp.UnitTests;

public sealed class CompanyPerkOwnershipResolverProfileCasesTests
{
    private readonly ITestOutputHelper _output;

    public CompanyPerkOwnershipResolverProfileCasesTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void ResolveOwnedPerks_O12_Nimrod_APMachinegun()
    {
        var result = ResolveForUnitProfile(
            factionId: 1001,
            unitNameContains: "NIMROD",
            requiredWeaponNames: ["AP Submachine Gun"]);

        _output.WriteLine(result.Report);

        Assert.True(result.Found, "Could not find O-12 Nimrod option with AP Marksman Rifle in database.");
        Assert.Contains(result.OwnedPerkIds, x => x == "body-8-13-track-1-tier-3"); // Super Jump
        Assert.Contains(result.OwnedPerkIds, x => x == "cool-5-10-track-4-tier-2"); // Mimetism (-3)
        Assert.Contains(result.OwnedPerkIds, x => x == "initiative-1-7-track-3-tier-2"); // Forward Deployment (+8")

        Assert.DoesNotContain(result.OwnedPerkIds, x => x == "body-8-13-track-1-tier-1"); // Climbing Plus
        Assert.DoesNotContain(result.OwnedPerkIds, x => x == "body-8-13-track-2-tier-1"); // Immunity chain
        Assert.DoesNotContain(result.OwnedPerkIds, x => x == "body-8-13-track-2-tier-2");
        Assert.DoesNotContain(result.OwnedPerkIds, x => x == "body-8-13-track-2-tier-3");
        Assert.DoesNotContain(result.OwnedPerkIds, x => x == "cool-5-10-track-4-tier-3"); // Mimetism (-6)
        Assert.DoesNotContain(result.OwnedPerkIds, x => x == "reflex-11-16-track-7-tier-3"); // BS Attack (-3)
        Assert.DoesNotContain(result.OwnedPerkIds, x => x == "reflex-11-16-track-7-tier-4"); // BS Attack (+1 SD)
    }

    [Fact]
    public void ResolveOwnedPerks_Ariadna_Rokot_Rifle_LightShotgun()
    {
        var result = ResolveForUnitProfile(
            factionId: 301,
            unitNameContains: "ROKOT",
            requiredWeaponNames: ["Rifle", "Light Shotgun"]);

        _output.WriteLine(result.Report);

        Assert.True(result.Found, "Could not find Ariadna Rokot option with Rifle + Light Shotgun in database.");
    }

    [Fact]
    public void ResolveOwnedPerks_RamahTaskforce_Khawarijs_BoardingShotgun()
    {
        var result = ResolveForUnitProfile(
            factionId: 404,
            unitNameContains: "KHAWARIJ",
            requiredWeaponNames: ["Boarding Shotgun"]);

        _output.WriteLine(result.Report);

        Assert.True(result.Found, "Could not find Ramah Taskforce Khawarijs option with Boarding Shotgun in database.");
    }

    [Fact]
    public void ResolveOwnedPerks_RamahTaskforce_YaraHaddad_Lieutenant()
    {
        var result = ResolveForUnitProfile(
            factionId: 404,
            unitNameContains: "YARA HADDAD",
            requiredWeaponNames: [],
            requiredSkillNames: ["Lieutenant"]);

        _output.WriteLine(result.Report);

        Assert.True(result.Found, "Could not find Ramah Taskforce Yara Haddad Lieutenant option in database.");
    }

    [Fact]
    public void ResolveOwnedPerks_Shindenbutai_KurayamiNinjas_Minelayer()
    {
        var result = ResolveForUnitProfile(
            factionId: 1102,
            unitNameContains: "KURAYAMI",
            requiredWeaponNames: [],
            requiredSkillNames: ["Minelayer"]);

        _output.WriteLine(result.Report);

        Assert.True(result.Found, "Could not find Shindenbutai Kurayami Ninjas Minelayer option in database.");
    }

    [Fact]
    public void ResolveOwnedPerks_Shindenbutai_Nokizaru_ForwardObserver()
    {
        var result = ResolveForUnitProfile(
            factionId: 1102,
            unitNameContains: "NOKIZARU",
            requiredWeaponNames: [],
            requiredSkillNames: ["Forward Observer"]);

        _output.WriteLine(result.Report);

        Assert.True(result.Found, "Could not find Shindenbutai Nokizaru Unit Forward Observer option in database.");
    }

    [Fact]
    public void ResolveOwnedPerks_Shindenbutai_Hatamoto_NCO_PlasmaCarbine()
    {
        var result = ResolveForUnitProfile(
            factionId: 1102,
            unitNameContains: "HATAMOTO",
            requiredWeaponNames: ["Plasma Carbine"],
            requiredSkillNames: ["NCO"]);

        _output.WriteLine(result.Report);

        Assert.True(result.Found, "Could not find Hatamoto Imperial Guard NCO Plasma Carbine option in database.");
    }

    [Fact]
    public void ResolveOwnedPerks_YuJing_KuangShi_ChainRifle()
    {
        var result = ResolveForUnitProfile(
            factionId: 201,
            unitNameContains: "KUANG SHI",
            requiredWeaponNames: ["Chain Rifle"]);

        _output.WriteLine(result.Report);

        Assert.True(result.Found, "Could not find Yu Jing Kuang Shi with Chain Rifle option in database.");
    }

    [Fact]
    public void ResolveOwnedPerks_Bakunin_SinEater_Mk12()
    {
        var result = ResolveForUnitProfile(
            factionId: 503,
            unitNameContains: "SIN-EATER",
            requiredWeaponNames: ["Mk12"]);

        _output.WriteLine(result.Report);

        Assert.True(result.Found, "Could not find Sin-Eater Observants with Mk12 option in database.");
    }

    [Fact]
    public void ResolveOwnedPerks_Neoterran_Bolt_KillerHackerDevice()
    {
        var result = ResolveForUnitProfile(
            factionId: 104,
            unitNameContains: "BOLT",
            requiredWeaponNames: [],
            requiredEquipmentNames: ["Killer Hacking Device"]);

        _output.WriteLine(result.Report);

        Assert.True(result.Found, "Could not find Neoterran Capitaline Army Bolt with Killer Hacker Device option in database.");
    }

    private (bool Found, string Report, IReadOnlyList<string> OwnedPerkIds) ResolveForUnitProfile(
        int factionId,
        string unitNameContains,
        IReadOnlyCollection<string> requiredWeaponNames,
        IReadOnlyCollection<string>? requiredSkillNames = null,
        IReadOnlyCollection<string>? requiredEquipmentNames = null)
    {
        var dbPath = ResolveRepoFilePath("InfinityMercsApp/ReferenceData/infinitymercs.db3");
        using var connection = new SqliteConnection($"Data Source={dbPath};Mode=ReadOnly");
        connection.Open();

        var factionFiltersJson = LoadFactionFiltersJson(connection, factionId);
        var skillsLookup = BuildIdNameLookup(factionFiltersJson, "skills");
        var equipLookup = BuildIdNameLookup(factionFiltersJson, "equip");
        var weaponsLookup = BuildIdNameLookup(factionFiltersJson, "weapons");
        var charsLookup = BuildIdNameLookup(factionFiltersJson, "chars");
        var extrasLookup = BuildIdNameLookup(factionFiltersJson, "extras");

        var tableWeaponsLookup = LoadLookupFromTable(connection, "weapons", "WeaponId");

        if (!TryFindProfileOption(
                connection,
                factionId,
                unitNameContains,
                requiredWeaponNames,
                requiredSkillNames,
                requiredEquipmentNames,
                skillsLookup,
                equipLookup,
                tableWeaponsLookup,
                out var profile,
                out var option,
                out var unitName,
                out var optionName,
                out var filtersJson))
        {
            return (false, "Profile/option not found.", []);
        }

        var effectiveFiltersJson = factionFiltersJson ?? filtersJson;
        if (!ReferenceEquals(effectiveFiltersJson, factionFiltersJson))
        {
            skillsLookup = BuildIdNameLookup(effectiveFiltersJson, "skills");
            equipLookup = BuildIdNameLookup(effectiveFiltersJson, "equip");
            weaponsLookup = BuildIdNameLookup(effectiveFiltersJson, "weapons");
            charsLookup = BuildIdNameLookup(effectiveFiltersJson, "chars");
            extrasLookup = BuildIdNameLookup(effectiveFiltersJson, "extras");
        }

        var ownedPerks = CompanyPerkOwnershipResolver.ResolveOwnedPerksFromProfile(
            profile,
            option,
            skillsLookup,
            equipLookup,
            weaponsLookup,
            charsLookup,
            extrasLookup: extrasLookup);

        var resolvedSkills = ResolveNames(profile, option, "skills", skillsLookup, extrasLookup);
        var resolvedEquipment = ResolveNames(profile, option, "equip", equipLookup, extrasLookup);

        var header = $"Unit: {unitName} | Option: {optionName}";
        var report = string.Join(
            Environment.NewLine,
            header,
            $"Skills: {(resolvedSkills.Count == 0 ? "(none)" : string.Join(", ", resolvedSkills))}",
            $"Equipment: {(resolvedEquipment.Count == 0 ? "(none)" : string.Join(", ", resolvedEquipment))}",
            CompanyPerkOwnershipResolver.BuildOwnedPerkReport(ownedPerks));
        var ids = ownedPerks.Select(x => x.Id).ToList();
        return (true, report, ids);
    }

    private static Dictionary<int, string> BuildIdNameLookup(string? filtersJson, string sectionName)
    {
        var lookup = new Dictionary<int, string>();
        if (string.IsNullOrWhiteSpace(filtersJson))
        {
            return lookup;
        }

        using var doc = JsonDocument.Parse(filtersJson);
        if (!doc.RootElement.TryGetProperty(sectionName, out var section) || section.ValueKind != JsonValueKind.Array)
        {
            return lookup;
        }

        foreach (var entry in section.EnumerateArray())
        {
            if (entry.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            if (!entry.TryGetProperty("id", out var idEl))
            {
                continue;
            }

            var parsedId = 0;
            var hasId = idEl.ValueKind == JsonValueKind.Number && idEl.TryGetInt32(out parsedId);
            if (!hasId &&
                !(idEl.ValueKind == JsonValueKind.String && int.TryParse(idEl.GetString(), out parsedId)))
            {
                continue;
            }

            if (!entry.TryGetProperty("name", out var nameEl) || nameEl.ValueKind != JsonValueKind.String)
            {
                continue;
            }

            var name = nameEl.GetString();
            if (!string.IsNullOrWhiteSpace(name))
            {
                lookup[parsedId] = name;
            }
        }

        return lookup;
    }

    private static Dictionary<int, string> LoadLookupFromTable(SqliteConnection connection, string tableName, string idColumnName)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = $"SELECT {idColumnName}, Name FROM {tableName}";
        using var reader = cmd.ExecuteReader();

        var lookup = new Dictionary<int, string>();
        while (reader.Read())
        {
            var id = reader.GetInt32(0);
            var name = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
            if (!string.IsNullOrWhiteSpace(name))
            {
                lookup[id] = name;
            }
        }

        return lookup;
    }

    private static string? LoadFactionFiltersJson(SqliteConnection connection, int factionId)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
                          SELECT FiltersJson
                          FROM armies
                          WHERE FactionId = $factionId
                          LIMIT 1
                          """;
        cmd.Parameters.AddWithValue("$factionId", factionId);
        using var reader = cmd.ExecuteReader();
        if (!reader.Read() || reader.IsDBNull(0))
        {
            return null;
        }

        return reader.GetString(0);
    }

    private static bool TryFindProfileOption(
        SqliteConnection connection,
        int factionId,
        string unitNameContains,
        IReadOnlyCollection<string> requiredWeaponNames,
        IReadOnlyCollection<string>? requiredSkillNames,
        IReadOnlyCollection<string>? requiredEquipmentNames,
        IReadOnlyDictionary<int, string> skillLookup,
        IReadOnlyDictionary<int, string> equipLookup,
        IReadOnlyDictionary<int, string> weaponLookup,
        out JsonElement profile,
        out JsonElement option,
        out string unitName,
        out string optionName,
        out string? filtersJson)
    {
        profile = default;
        option = default;
        unitName = string.Empty;
        optionName = string.Empty;
        filtersJson = null;

        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
                          SELECT Name, ProfileGroupsJson, FiltersJson
                          FROM units
                          WHERE FactionId = $factionId
                            AND Name LIKE $nameLike
                            AND ProfileGroupsJson IS NOT NULL
                          """;
        cmd.Parameters.AddWithValue("$factionId", factionId);
        cmd.Parameters.AddWithValue("$nameLike", $"%{unitNameContains}%");

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            if (reader.IsDBNull(0) || reader.IsDBNull(1))
            {
                continue;
            }

            var dbUnitName = reader.GetString(0);
            var profileGroupsJson = reader.GetString(1);
            var dbFiltersJson = reader.IsDBNull(2) ? null : reader.GetString(2);
            using var doc = JsonDocument.Parse(profileGroupsJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var group in doc.RootElement.EnumerateArray())
            {
                if (!group.TryGetProperty("profiles", out var profiles) ||
                    profiles.ValueKind != JsonValueKind.Array ||
                    profiles.GetArrayLength() == 0 ||
                    !group.TryGetProperty("options", out var options) ||
                    options.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var candidateOption in options.EnumerateArray())
                {
                    var optionSkillNames = ResolveNames(profiles[0], candidateOption, "skills", skillLookup, new Dictionary<int, string>());
                    if (requiredSkillNames is not null && requiredSkillNames.Count > 0)
                    {
                        var hasAllRequiredSkills = requiredSkillNames.All(required =>
                            optionSkillNames.Any(actual => actual.Contains(required, StringComparison.OrdinalIgnoreCase)));
                        if (!hasAllRequiredSkills)
                        {
                            continue;
                        }
                    }

                    var optionEquipmentNames = ResolveNames(profiles[0], candidateOption, "equip", equipLookup, new Dictionary<int, string>());
                    if (requiredEquipmentNames is not null && requiredEquipmentNames.Count > 0)
                    {
                        var hasAllRequiredEquipment = requiredEquipmentNames.All(required =>
                            optionEquipmentNames.Any(actual => actual.Contains(required, StringComparison.OrdinalIgnoreCase)));
                        if (!hasAllRequiredEquipment)
                        {
                            continue;
                        }
                    }

                    var optionWeapons = ResolveWeaponNames(candidateOption, weaponLookup);
                    var hasAllRequired = requiredWeaponNames.All(required =>
                        optionWeapons.Any(actual => actual.Contains(required, StringComparison.OrdinalIgnoreCase)));

                    if (!hasAllRequired)
                    {
                        continue;
                    }

                    using var profileDoc = JsonDocument.Parse(profiles[0].GetRawText());
                    using var optionDoc = JsonDocument.Parse(candidateOption.GetRawText());
                    profile = profileDoc.RootElement.Clone();
                    option = optionDoc.RootElement.Clone();
                    unitName = dbUnitName;
                    filtersJson = dbFiltersJson;
                    optionName = candidateOption.TryGetProperty("name", out var optionNameEl) && optionNameEl.ValueKind == JsonValueKind.String
                        ? optionNameEl.GetString() ?? "-"
                        : "-";
                    return true;
                }
            }
        }

        return false;
    }

    private static List<string> ResolveWeaponNames(JsonElement option, IReadOnlyDictionary<int, string> weaponLookup)
    {
        var names = new List<string>();
        if (!option.TryGetProperty("weapons", out var weaponsEl) || weaponsEl.ValueKind != JsonValueKind.Array)
        {
            return names;
        }

        foreach (var weapon in weaponsEl.EnumerateArray())
        {
            if (!weapon.TryGetProperty("id", out var idEl) || idEl.ValueKind != JsonValueKind.Number)
            {
                continue;
            }

            var id = idEl.GetInt32();
            if (weaponLookup.TryGetValue(id, out var name) && !string.IsNullOrWhiteSpace(name))
            {
                names.Add(name);
            }
        }

        return names;
    }

    private static List<string> ResolveNames(
        JsonElement profile,
        JsonElement option,
        string propertyName,
        IReadOnlyDictionary<int, string> lookup,
        IReadOnlyDictionary<int, string> extrasLookup)
    {
        var values = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        CollectNames(profile, propertyName, lookup, extrasLookup, values);
        CollectNames(option, propertyName, lookup, extrasLookup, values);
        return values.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static void CollectNames(
        JsonElement container,
        string propertyName,
        IReadOnlyDictionary<int, string> lookup,
        IReadOnlyDictionary<int, string> extrasLookup,
        ISet<string> destination)
    {
        if (container.ValueKind != JsonValueKind.Object ||
            !container.TryGetProperty(propertyName, out var entries) ||
            entries.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var entry in entries.EnumerateArray())
        {
            if (entry.ValueKind != JsonValueKind.Object ||
                !entry.TryGetProperty("id", out var idEl) ||
                idEl.ValueKind != JsonValueKind.Number)
            {
                continue;
            }

            var id = idEl.GetInt32();
            if (lookup.TryGetValue(id, out var name) && !string.IsNullOrWhiteSpace(name))
            {
                destination.Add(FormatNameWithExtras(name, entry, extrasLookup));
            }
        }
    }

    private static string FormatNameWithExtras(
        string baseName,
        JsonElement entry,
        IReadOnlyDictionary<int, string> extrasLookup)
    {
        if (!entry.TryGetProperty("extra", out var extrasEl) || extrasEl.ValueKind != JsonValueKind.Array)
        {
            return baseName;
        }

        var extraNames = new List<string>();
        foreach (var extra in extrasEl.EnumerateArray())
        {
            if (extra.ValueKind != JsonValueKind.Number)
            {
                continue;
            }

            var id = extra.GetInt32();
            if (extrasLookup.TryGetValue(id, out var extraName) && !string.IsNullOrWhiteSpace(extraName))
            {
                extraNames.Add(extraName);
            }
        }

        return extraNames.Count == 0
            ? baseName
            : $"{baseName} ({string.Join(", ", extraNames.Distinct(StringComparer.OrdinalIgnoreCase))})";
    }

    private static string ResolveRepoFilePath(string relativePath)
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, relativePath.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        throw new FileNotFoundException($"Could not locate '{relativePath}' from test base directory.");
    }
}
