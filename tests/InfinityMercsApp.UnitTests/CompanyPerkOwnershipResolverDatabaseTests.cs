using System.Text.Json;
using InfinityMercsApp.Domain.Models.Perks;
using Microsoft.Data.Sqlite;

namespace InfinityMercsApp.UnitTests;

public sealed class CompanyPerkOwnershipResolverDatabaseTests
{
    [Fact]
    public void ResolveOwnedPerkNodeIds_GhulamDoctor_FromDatabase_ReturnsDoctorPerkNodes()
    {
        var dbPath = ResolveRepoFilePath("InfinityMercsApp/ReferenceData/infinitymercs.db3");
        using var connection = new SqliteConnection($"Data Source={dbPath};Mode=ReadOnly");
        connection.Open();

        var skillLookup = LoadLookup(connection, "skills", "Id");
        var (profile, option, unitFiltersJson, factionId) = FindFirstGhulamDoctorProfile(connection, skillLookup);
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

        Assert.Contains(ownedPerks, x => x.Id == "intelligence-track-2-tier-1");
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
            if (entry.ValueKind != JsonValueKind.Object || !entry.TryGetProperty("id", out var idEl))
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

    private static (JsonElement Profile, JsonElement Option, string? FiltersJson, int FactionId) FindFirstGhulamDoctorProfile(
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

            var profileGroupsJson = reader.GetString(0);
            var filtersJson = reader.IsDBNull(1) ? null : reader.GetString(1);
            var factionId = reader.IsDBNull(2) ? 0 : reader.GetInt32(2);
            if (TryExtractDoctorProfile(profileGroupsJson, doctorSkillIds, out var profile, out var option))
            {
                return (profile, option, filtersJson, factionId);
            }
        }

        throw new Xunit.Sdk.XunitException("Could not find a GHULAM Doctor profile in units table.");
    }

    private static bool TryExtractDoctorProfile(
        string profileGroupsJson,
        IReadOnlySet<int> doctorSkillIds,
        out JsonElement profile,
        out JsonElement option)
    {
        profile = default;
        option = default;

        using var doc = JsonDocument.Parse(profileGroupsJson);
        if (doc.RootElement.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        foreach (var group in doc.RootElement.EnumerateArray())
        {
            if (!group.TryGetProperty("options", out var options) || options.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var candidateOption in options.EnumerateArray())
            {
                if (!ContainsAnySkillId(candidateOption, doctorSkillIds))
                {
                    continue;
                }

                if (!group.TryGetProperty("profiles", out var profiles) ||
                    profiles.ValueKind != JsonValueKind.Array ||
                    profiles.GetArrayLength() == 0)
                {
                    continue;
                }

                using var profileDoc = JsonDocument.Parse(profiles[0].GetRawText());
                using var optionDoc = JsonDocument.Parse(candidateOption.GetRawText());
                profile = profileDoc.RootElement.Clone();
                option = optionDoc.RootElement.Clone();
                return true;
            }
        }

        return false;
    }

    private static bool ContainsAnySkillId(JsonElement container, IReadOnlySet<int> requiredIds)
    {
        if (!container.TryGetProperty("skills", out var skills) || skills.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        foreach (var skill in skills.EnumerateArray())
        {
            if (!skill.TryGetProperty("id", out var idEl) || idEl.ValueKind != JsonValueKind.Number)
            {
                continue;
            }

            if (requiredIds.Contains(idEl.GetInt32()))
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
}
