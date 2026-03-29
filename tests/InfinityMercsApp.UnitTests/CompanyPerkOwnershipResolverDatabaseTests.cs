using System.Text.Json;
using InfinityMercsApp.Views.Common;
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

        var skillLookup = LoadSkillLookup(connection);
        var ghulamDoctorSkills = FindFirstGhulamDoctorSkills(connection, skillLookup);

        var ownedIds = CompanyPerkOwnershipResolver.ResolveOwnedPerkNodeIds(ghulamDoctorSkills);

        Assert.Contains("intelligence-14-19-track-2-tier-3", ownedIds);
        Assert.Contains("intelligence-14-19-track-2-tier-4", ownedIds);
    }

    private static Dictionary<int, string> LoadSkillLookup(SqliteConnection connection)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT Id, Name FROM skills";
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

    private static List<string> FindFirstGhulamDoctorSkills(
        SqliteConnection connection,
        IReadOnlyDictionary<int, string> skillLookup)
    {
        var doctorSkillIds = skillLookup
            .Where(x => x.Value.Contains("Doctor", StringComparison.OrdinalIgnoreCase))
            .Select(x => x.Key)
            .ToHashSet();

        if (doctorSkillIds.Count == 0)
        {
            throw new Xunit.Sdk.XunitException("Could not find Doctor skill id(s) in skills table.");
        }

        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
                          SELECT ProfileGroupsJson
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
            if (TryExtractDoctorSkills(profileGroupsJson, doctorSkillIds, skillLookup, out var doctorSkills))
            {
                return doctorSkills;
            }
        }

        throw new Xunit.Sdk.XunitException("Could not find a GHULAM Doctor profile in units table.");
    }

    private static bool TryExtractDoctorSkills(
        string profileGroupsJson,
        IReadOnlySet<int> doctorSkillIds,
        IReadOnlyDictionary<int, string> skillLookup,
        out List<string> doctorSkills)
    {
        doctorSkills = [];
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

            foreach (var option in options.EnumerateArray())
            {
                if (!ContainsAnySkillId(option, doctorSkillIds))
                {
                    continue;
                }

                AddSkillNames(option, skillLookup, doctorSkills);

                if (group.TryGetProperty("profiles", out var profiles) &&
                    profiles.ValueKind == JsonValueKind.Array &&
                    profiles.GetArrayLength() > 0)
                {
                    AddSkillNames(profiles[0], skillLookup, doctorSkills);
                }

                doctorSkills = doctorSkills
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                return doctorSkills.Count > 0;
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

    private static void AddSkillNames(
        JsonElement container,
        IReadOnlyDictionary<int, string> skillLookup,
        ICollection<string> destination)
    {
        if (!container.TryGetProperty("skills", out var skills) || skills.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var skill in skills.EnumerateArray())
        {
            if (!skill.TryGetProperty("id", out var idEl) || idEl.ValueKind != JsonValueKind.Number)
            {
                continue;
            }

            var id = idEl.GetInt32();
            if (skillLookup.TryGetValue(id, out var name) && !string.IsNullOrWhiteSpace(name))
            {
                destination.Add(name);
            }
        }
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
