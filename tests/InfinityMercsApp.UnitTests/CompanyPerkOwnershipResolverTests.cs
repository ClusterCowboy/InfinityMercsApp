using System.Text.Json;
using InfinityMercsApp.Domain.Models.Perks;
using Microsoft.Data.Sqlite;

namespace InfinityMercsApp.UnitTests;

public sealed class CompanyPerkOwnershipResolverTests
{
    [Fact]
    public void ResolveOwnedPerkNodeIds_GhulamDoctorProfile_ReturnsDoctorPerkNodes()
    {
        var dbPath = ResolveRepoFilePath("InfinityMercsApp/ReferenceData/infinitymercs.db3");
        using var connection = new SqliteConnection($"Data Source={dbPath};Mode=ReadOnly");
        connection.Open();

        var skillLookup = LoadLookup(connection, "skills", "Id");
        var (profile, option, unitFiltersJson, factionId) = FindGhulamDoctorPlusThreeProfile(connection, skillLookup);
        var filtersJson = LoadFactionFiltersJson(connection, factionId) ?? unitFiltersJson;
        var equipLookup = BuildIdNameLookup(filtersJson, "equip");
        var weaponLookup = BuildIdNameLookup(filtersJson, "weapons");
        var charsLookup = BuildIdNameLookup(filtersJson, "chars");
        var extrasLookup = BuildIdNameLookup(filtersJson, "extras");

        var ownedPerks = CompanyPerkOwnershipResolver.ResolveOwnedPerksFromProfile(
            profile,
            option,
            skillLookup,
            equipLookup,
            weaponLookup,
            charsLookup,
            extrasLookup);

        var report = CompanyPerkOwnershipResolver.BuildOwnedPerkReport(ownedPerks);

        Assert.Contains(ownedPerks, x => x.Id == "intelligence-track-2-tier-1");
        Assert.Contains("Medikit", report, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ResolveOwnedPerkNodeIds_NoProfileData_ReturnsEmpty()
    {
        var emptyProfile = JsonDocument.Parse("{}").RootElement;
        var ownedPerks = CompanyPerkOwnershipResolver.ResolveOwnedPerksFromProfile(
            emptyProfile,
            option: null,
            skillsLookup: new Dictionary<int, string>());

        Assert.Empty(ownedPerks);
    }

    [Fact]
    public void ResolveOwnedPerkNodeIds_BurstBonus_DoesNotMatchBallisticSkillBonus()
    {
        var ownedIds = CompanyPerkOwnershipResolver.ResolveOwnedPerkNodeIds(
            skills: ["Lieutenant Roll (+1 B)"]);

        var ownedPerks = ownedIds
            .Select(CompanyPerkCatalog.FindById)
            .Where(x => x is not null)
            .Select(x => x!)
            .ToList();

        Assert.DoesNotContain(
            ownedPerks,
            x => x.Description.Contains("+1 BS", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ResolveOwnedPerkNodeIds_HackingDevice_AlsoGrantsBaseHacker()
    {
        var ownedIds = CompanyPerkOwnershipResolver.ResolveOwnedPerkNodeIds(
            skills: ["Killer Hacking Device"]);

        var descriptions = ResolveDescriptions(ownedIds);
        Assert.Contains(descriptions, x => x.Contains("Killer Hacking Device", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(descriptions, x => x.Contains("Hacker (Role) No device", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ResolveOwnedPerkNodeIds_MartialArtsL4_GrantsL1AndL3ButNotL5()
    {
        var ownedIds = CompanyPerkOwnershipResolver.ResolveOwnedPerkNodeIds(
            skills: ["Martial Arts L4"]);

        var descriptions = ResolveDescriptions(ownedIds);
        Assert.Contains(descriptions, x => x.Contains("Martial Arts L1", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(descriptions, x => x.Contains("Martial Arts L3", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(descriptions, x => x.Contains("Martial Arts L5", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ResolveOwnedPerkNodeIds_CcWeaponPs6_DoesNotGrantCcMinus6Perk()
    {
        var ownedIds = CompanyPerkOwnershipResolver.ResolveOwnedPerkNodeIds(
            skills: ["CC Weapon (PS=6)"]);

        var descriptions = ResolveDescriptions(ownedIds);
        Assert.DoesNotContain(descriptions, x => x.Contains("CC (-6)", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ResolveOwnedPerksFromProfile_MechaTrack_OnlyForTags()
    {
        using var nonTagProfileDoc = JsonDocument.Parse("""{ "type": 1, "skills": [ { "id": 1 } ] }""");
        using var tagProfileDoc = JsonDocument.Parse("""{ "type": 4, "skills": [ { "id": 1 } ] }""");
        var lookup = new Dictionary<int, string> { [1] = "Aerial, No Cover, Super Jump (Jet Propulsion), Tech-Recovery" };

        var nonTagOwned = CompanyPerkOwnershipResolver.ResolveOwnedPerksFromProfile(
            nonTagProfileDoc.RootElement,
            option: null,
            skillsLookup: lookup);
        var tagOwned = CompanyPerkOwnershipResolver.ResolveOwnedPerksFromProfile(
            tagProfileDoc.RootElement,
            option: null,
            skillsLookup: lookup);

        Assert.DoesNotContain(nonTagOwned, x => string.Equals(x.ListId, "mecha", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(tagOwned, x => string.Equals(x.ListId, "mecha", StringComparison.OrdinalIgnoreCase));
    }

    private static Dictionary<int, string> LoadLookup(SqliteConnection connection, string tableName, string idColumnName)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = $"SELECT {idColumnName}, Name FROM {tableName}";
        using var reader = cmd.ExecuteReader();

        var lookup = new Dictionary<int, string>();
        while (reader.Read())
        {
            var id = reader.GetInt32(0);
            var name = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
            lookup[id] = name;
        }

        return lookup;
    }

    private static Dictionary<int, string> BuildIdNameLookup(string? filtersJson, string sectionName)
    {
        var map = new Dictionary<int, string>();
        if (string.IsNullOrWhiteSpace(filtersJson))
        {
            return map;
        }

        using var doc = JsonDocument.Parse(filtersJson);
        if (!doc.RootElement.TryGetProperty(sectionName, out var section) || section.ValueKind != JsonValueKind.Array)
        {
            return map;
        }

        foreach (var entry in section.EnumerateArray())
        {
            if (entry.ValueKind != JsonValueKind.Object ||
                !entry.TryGetProperty("id", out var idEl))
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
                map[parsedId] = name;
            }
        }

        return map;
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
        return reader.Read() && !reader.IsDBNull(0) ? reader.GetString(0) : null;
    }

    private static (JsonElement Profile, JsonElement Option, string? FiltersJson, int FactionId) FindGhulamDoctorPlusThreeProfile(
        SqliteConnection connection,
        IReadOnlyDictionary<int, string> skillLookup)
    {
        var doctorSkillIds = skillLookup
            .Where(x => x.Value.Contains("Doctor", StringComparison.OrdinalIgnoreCase))
            .Select(x => x.Key)
            .ToHashSet();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
                          SELECT ProfileGroupsJson, FiltersJson, FactionId
                          FROM units
                          WHERE Name LIKE '%GHULAM%'
                            AND ProfileGroupsJson IS NOT NULL
                          """;
        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            if (reader.IsDBNull(0))
            {
                continue;
            }

            var json = reader.GetString(0);
            var filtersJson = reader.IsDBNull(1) ? null : reader.GetString(1);
            var factionId = reader.IsDBNull(2) ? 0 : reader.GetInt32(2);
            using var doc = JsonDocument.Parse(json);
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

                foreach (var option in options.EnumerateArray())
                {
                    if (!HasDoctorPlusThree(option, doctorSkillIds))
                    {
                        continue;
                    }

                    var profileRaw = profiles[0].GetRawText();
                    var optionRaw = option.GetRawText();
                    using var profileDoc = JsonDocument.Parse(profileRaw);
                    using var optionDoc = JsonDocument.Parse(optionRaw);
                    return (profileDoc.RootElement.Clone(), optionDoc.RootElement.Clone(), filtersJson, factionId);
                }
            }
        }

        throw new Xunit.Sdk.XunitException("Could not find GHULAM Infantry Doctor(+3) in database.");
    }

    private static bool HasDoctorPlusThree(JsonElement option, IReadOnlySet<int> doctorSkillIds)
    {
        if (!option.TryGetProperty("skills", out var skills) || skills.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        foreach (var skill in skills.EnumerateArray())
        {
            if (!skill.TryGetProperty("id", out var idEl) || idEl.ValueKind != JsonValueKind.Number)
            {
                continue;
            }

            var id = idEl.GetInt32();
            if (!doctorSkillIds.Contains(id))
            {
                continue;
            }

            if (skill.TryGetProperty("extra", out var extraEl) &&
                extraEl.ValueKind == JsonValueKind.Array &&
                extraEl.EnumerateArray().Any(x => x.ValueKind == JsonValueKind.Number && x.GetInt32() == 1))
            {
                return true;
            }
        }

        return false;
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

    private static List<string> ResolveDescriptions(IEnumerable<string> ownedIds)
    {
        return ownedIds
            .Select(CompanyPerkCatalog.FindById)
            .Where(x => x is not null && !string.IsNullOrWhiteSpace(x.Description))
            .Select(x => x!.Description)
            .ToList();
    }
}
