namespace InfinityMercsApp.Infrastructure.API.InfinityArmy;

using DomainArmyImportFaction = InfinityMercsApp.Domain.Models.Army.ArmyImportFaction;
using DomainArmyImportReinforcementsFaction = InfinityMercsApp.Domain.Models.Army.ArmyImportReinforcementsFaction;
using DomainArmyImportResume = InfinityMercsApp.Domain.Models.Army.ArmyImportResume;
using DomainArmyImportUnit = InfinityMercsApp.Domain.Models.Army.ArmyImportUnit;
using DomainMetadataAmmunition = InfinityMercsApp.Domain.Models.Metadata.Ammunition;
using DomainMetadataBooty = InfinityMercsApp.Domain.Models.Metadata.Booty;
using DomainMetadataDocument = InfinityMercsApp.Domain.Models.Metadata.MetadataDocument;
using DomainMetadataEquipment = InfinityMercsApp.Domain.Models.Metadata.Equipments;
using DomainMetadataFaction = InfinityMercsApp.Domain.Models.Metadata.Faction;
using DomainMetadataHackingProgram = InfinityMercsApp.Domain.Models.Metadata.HackingProgram;
using DomainMetadataMartialArt = InfinityMercsApp.Domain.Models.Metadata.MartialArt;
using DomainMetadataMetachemistry = InfinityMercsApp.Domain.Models.Metadata.Metachemistry;
using DomainMetadataSkill = InfinityMercsApp.Domain.Models.Metadata.Skill;
using DomainMetadataWeapon = InfinityMercsApp.Domain.Models.Metadata.Weapon;
using ApiMetadataDocument = InfinityMercsApp.Infrastructure.Models.API.Metadata.MetadataDocument;
using ApiMetadataWeapon = InfinityMercsApp.Infrastructure.Models.API.Metadata.Weapon;
using Microsoft.Extensions.Logging;
using System.IO.Compression;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <inheritdoc/>
public sealed class InfinityArmyAPI(HttpClient httpClient, ILogger<InfinityArmyAPI> logger) : IInfinityArmyAPI
{
    private const string ArmyUrlBase = "https://api.corvusbelli.com/army/units/en/";
    private const string MetadataUrl = "https://api.corvusbelli.com/army/infinity/en/metadata";
    private const string DebugDumpFolderName = "external-read-failures";

    private static readonly JsonSerializerOptions jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        Converters = { new RelaxedInt32Converter(), new RelaxedNullableInt32Converter() }
    };

    /// <inheritdoc/>
    public async Task<DomainMetadataDocument?> GetMetaDataAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Fetching metadata from: {Url}", MetadataUrl);
        var payload = await GetAsync(MetadataUrl, cancellationToken);
        if (!LooksLikeJsonObject(payload))
        {
            var dumpPath = await SaveFailedPayloadAsync("metadata-non-json-payload", MetadataUrl, payload);
            logger.LogError("Metadata response was not JSON. Dump saved to: {DumpPath}", dumpPath);
            throw new InvalidDataException($"Metadata response was not JSON. Dump: {dumpPath}");
        }

        try
        {
            var apiDocument = JsonSerializer.Deserialize<ApiMetadataDocument>(payload, jsonOptions);
            if (apiDocument is null)
            {
                var dumpPath = await SaveFailedPayloadAsync("metadata-deserialize-null", MetadataUrl, payload);
                logger.LogError("Metadata deserialization returned null. Dump saved to: {DumpPath}", dumpPath);
                return null;
            }

            return MapMetadataDocument(apiDocument);
        }
        catch (JsonException ex)
        {
            var dumpPath = await SaveFailedPayloadAsync("metadata-deserialize-error", MetadataUrl, payload);
            logger.LogError(ex, "Metadata deserialization failed. Dump saved to: {DumpPath}", dumpPath);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<DomainArmyImportFaction?> GetArmyDataAsync(int factionId, CancellationToken cancellationToken = default)
    {
        var fullUrl = $"{ArmyUrlBase}{factionId}";
        logger.LogInformation("Fetching army data from: {Url}", fullUrl);

        var payload = await GetAsync(fullUrl, cancellationToken);
        if (!LooksLikeJsonObject(payload))
        {
            var dumpPath = await SaveFailedPayloadAsync($"army-{factionId}-non-json-payload", fullUrl, payload);
            logger.LogError("Army response for faction {FactionId} was not JSON. Dump saved to: {DumpPath}", factionId, dumpPath);
            throw new InvalidDataException($"Army response for faction {factionId} was not JSON. Dump: {dumpPath}");
        }

        try
        {
            var apiFaction = JsonSerializer.Deserialize<Models.API.Army.Faction>(payload);
            if (apiFaction is null)
            {
                var dumpPath = await SaveFailedPayloadAsync($"army-{factionId}-deserialize-null", fullUrl, payload);
                logger.LogError("Army deserialization returned null for faction {FactionId}. Dump saved to: {DumpPath}", factionId, dumpPath);
                return null;
            }

            return MapArmyImportFaction(apiFaction, payload);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Primary army deserialization failed for faction {FactionId}. Attempting reinforcements fallback parser.", factionId);

            try
            {
                var reinforcementsFaction = ParseArmyImportReinforcementsFaction(payload);
                return MapArmyImportReinforcementsFaction(reinforcementsFaction);
            }
            catch (Exception fallbackEx)
            {
                var dumpPath = await SaveFailedPayloadAsync($"army-{factionId}-deserialize-error", fullUrl, payload);
                logger.LogError(
                    fallbackEx,
                    "Army deserialization failed for faction {FactionId} with both primary and reinforcements fallback parser. Dump saved to: {DumpPath}",
                    factionId,
                    dumpPath);
                throw;
            }
        }
    }

    private static DomainMetadataDocument MapMetadataDocument(ApiMetadataDocument source)
    {
        return new DomainMetadataDocument
        {
            Factions = source.Factions.Select(x => new DomainMetadataFaction
            {
                Id = x.Id,
                ParentId = x.Parent,
                Name = x.Name,
                Slug = x.Slug,
                Discontinued = x.Discontinued,
                Logo = x.Logo
            }).ToList(),
            Ammunitions = source.Ammunitions.Select(x => new DomainMetadataAmmunition
            {
                Id = x.Id,
                Name = x.Name,
                Wiki = x.Wiki
            }).ToList(),
            Weapons = source.Weapons.Select(x => new DomainMetadataWeapon
            {
                WeaponKey = BuildWeaponKey(x),
                WeaponId = x.Id,
                Name = x.Name,
                Type = x.Type,
                Mode = x.Mode,
                Wiki = x.Wiki,
                AmmunitionId = x.Ammunition,
                Burst = x.Burst,
                Damage = x.Damage,
                Saving = x.Saving,
                SavingNum = x.SavingNum,
                Profile = x.Profile,
                PropertiesJson = x.Properties is null ? null : JsonSerializer.Serialize(x.Properties),
                DistanceJson = x.Distance is null ? null : JsonSerializer.Serialize(x.Distance)
            }).ToList(),
            Skills = source.Skills.Select(x => new DomainMetadataSkill
            {
                Id = x.Id,
                Name = x.Name,
                Wiki = x.Wiki
            }).ToList(),
            Equips = source.Equips.Select(x => new DomainMetadataEquipment
            {
                Id = x.Id,
                Name = x.Name,
                Wiki = x.Wiki
            }).ToList(),
            Hack = source.Hack.Select(x => new DomainMetadataHackingProgram
            {
                Name = x.Name,
                Opponent = x.Opponent,
                Special = x.Special,
                Damage = x.Damage,
                Attack = x.Attack,
                Burst = x.Burst,
                Extra = x.Extra,
                SkillTypeJson = x.SkillType is null ? null : JsonSerializer.Serialize(x.SkillType),
                DevicesJson = x.Devices is null ? null : JsonSerializer.Serialize(x.Devices),
                TargetJson = x.Target is null ? null : JsonSerializer.Serialize(x.Target)
            }).ToList(),
            MartialArts = source.MartialArts.Select(x => new DomainMetadataMartialArt
            {
                Name = x.Name,
                Opponent = x.Opponent,
                Damage = x.Damage,
                Attack = x.Attack,
                Burst = x.Burst
            }).ToList(),
            Metachemistry = source.Metachemistry.Select(x => new DomainMetadataMetachemistry
            {
                Id = x.Id,
                Name = x.Name,
                Value = x.Value
            }).ToList(),
            Booty = source.Booty.Select(x => new DomainMetadataBooty
            {
                Id = x.Id,
                Name = x.Name,
                Value = x.Value
            }).ToList()
        };
    }

    private static DomainArmyImportFaction MapArmyImportFaction(Models.API.Army.Faction source, string rawJson)
    {
        return new DomainArmyImportFaction
        {
            Version = source.Version,
            Units = source.Units.Select(x => new DomainArmyImportUnit
            {
                Id = x.Id,
                IdArmy = x.IdArmy,
                Canonical = x.Canonical,
                Isc = x.Isc,
                IscAbbr = x.IscAbbr,
                Name = x.Name,
                Slug = x.Slug,
                ProfileGroupsJson = ToJsonOrNull(x.ProfileGroups),
                OptionsJson = ToJsonOrNull(x.Options),
                FiltersJson = ToJsonOrNull(x.Filters),
                FactionsJson = ToJsonOrNull(x.Factions)
            }).ToList(),
            Resume = source.Resume.Select(x => new DomainArmyImportResume
            {
                Id = x.Id,
                Isc = x.Isc,
                IdArmy = x.IdArmy,
                Name = x.Name,
                Slug = x.Slug,
                Logo = x.Logo,
                Type = x.Type,
                Category = x.Category
            }).ToList(),
            ReinforcementsJson = ToJsonOrNull(source.Reinforcements),
            FiltersJson = ToJsonOrNull(source.Filters),
            FireteamsJson = ToJsonOrNull(source.Fireteams),
            RelationsJson = ToJsonOrNull(source.Relations),
            SpecopsJson = ToJsonOrNull(source.Specops),
            FireteamChartJson = ToJsonOrNull(source.FireteamChart),
            RawJson = rawJson
        };
    }

    private static DomainArmyImportFaction MapArmyImportReinforcementsFaction(DomainArmyImportReinforcementsFaction source)
    {
        return new DomainArmyImportFaction
        {
            Version = source.Version,
            Units = source.Units,
            Resume = source.Resume,
            ReinforcementsJson = source.ReinforcementsJson,
            FiltersJson = source.FiltersJson,
            FireteamsJson = source.FireteamsJson,
            RelationsJson = source.RelationsJson,
            SpecopsJson = source.SpecopsJson,
            FireteamChartJson = source.FireteamChartJson,
            RawJson = source.RawJson
        };
    }

    private static string BuildWeaponKey(ApiMetadataWeapon weapon)
    {
        return $"{weapon.Id}:{weapon.Name}:{weapon.Mode ?? string.Empty}";
    }

    private static string? ToJsonOrNull(JsonElement element)
    {
        return element.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null
            ? null
            : element.GetRawText();
    }

    private static DomainArmyImportReinforcementsFaction ParseArmyImportReinforcementsFaction(string payload)
    {
        using var document = JsonDocument.Parse(payload);
        var root = document.RootElement;

        return new DomainArmyImportReinforcementsFaction
        {
            Version = ReadString(root, "version") ?? string.Empty,
            Units = ReadArray(root, "units").Select(ParseArmyImportUnit).ToList(),
            Resume = ReadArray(root, "resume").Select(ParseArmyImportResume).ToList(),
            ReinforcementsJson = ReadRawJson(root, "reinforcements"),
            FiltersJson = ReadRawJson(root, "filters"),
            FireteamsJson = ReadRawJson(root, "fireteams"),
            RelationsJson = ReadRawJson(root, "relations"),
            SpecopsJson = ReadRawJson(root, "specops"),
            FireteamChartJson = ReadRawJson(root, "fireteamChart"),
            RawJson = payload
        };
    }

    private static DomainArmyImportUnit ParseArmyImportUnit(JsonElement element)
    {
        return new DomainArmyImportUnit
        {
            Id = ReadInt(element, "id"),
            IdArmy = ReadNullableInt(element, "idArmy"),
            Canonical = ReadNullableInt(element, "canonical"),
            Isc = ReadString(element, "isc"),
            IscAbbr = ReadString(element, "iscAbbr"),
            Name = ReadString(element, "name") ?? string.Empty,
            Slug = ReadString(element, "slug"),
            ProfileGroupsJson = ReadRawJson(element, "profileGroups"),
            OptionsJson = ReadRawJson(element, "options"),
            FiltersJson = ReadRawJson(element, "filters"),
            FactionsJson = ReadRawJson(element, "factions")
        };
    }

    private static DomainArmyImportResume ParseArmyImportResume(JsonElement element)
    {
        return new DomainArmyImportResume
        {
            Id = ReadInt(element, "id"),
            Isc = ReadString(element, "isc"),
            IdArmy = ReadNullableInt(element, "idArmy"),
            Name = ReadString(element, "name") ?? string.Empty,
            Slug = ReadString(element, "slug"),
            Logo = ReadString(element, "logo"),
            Type = ReadNullableInt(element, "type"),
            Category = ReadNullableInt(element, "category")
        };
    }

    private static IEnumerable<JsonElement> ReadArray(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return property.EnumerateArray().ToArray();
    }

    private static string? ReadString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        return property.ValueKind switch
        {
            JsonValueKind.String => property.GetString(),
            JsonValueKind.Number => property.GetRawText(),
            JsonValueKind.True => bool.TrueString,
            JsonValueKind.False => bool.FalseString,
            _ => null
        };
    }

    private static string? ReadRawJson(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        return ToJsonOrNull(property);
    }

    private static int ReadInt(JsonElement element, string propertyName)
    {
        return ReadNullableInt(element, propertyName) ?? 0;
    }

    private static int? ReadNullableInt(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        if (property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out var numericValue))
        {
            return numericValue;
        }

        if (property.ValueKind == JsonValueKind.String &&
            int.TryParse(property.GetString(), out var stringValue))
        {
            return stringValue;
        }

        return null;
    }

    private async Task<string> GetAsync(string url, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.TryAddWithoutValidation("Origin", "https://infinitytheuniverse.com");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(80));

        using var response = await httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            timeoutCts.Token);

        if (!response.IsSuccessStatusCode)
        {
            var responseBody = await TryReadResponseBodyAsync(response, timeoutCts.Token);
            var dumpPath = await SaveFailedPayloadAsync($"http-{(int)response.StatusCode}", url, responseBody);
            logger.LogError(
                "Request failed for {Url}. Status={StatusCode}. Failure dump saved to: {DumpPath}",
                url,
                (int)response.StatusCode,
                dumpPath);

            throw new HttpRequestException($"Request failed. Status={(int)response.StatusCode}. Failure dump: {dumpPath}");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(timeoutCts.Token);
        var contentEncodings = response.Content.Headers.ContentEncoding;

        if (contentEncodings.Any(e => e.Equals("gzip", StringComparison.OrdinalIgnoreCase)))
        {
            await using var gzip = new GZipStream(stream, CompressionMode.Decompress);
            using var reader = new StreamReader(gzip);
            return await reader.ReadToEndAsync(timeoutCts.Token);
        }

        using var plainReader = new StreamReader(stream);
        return await plainReader.ReadToEndAsync(timeoutCts.Token);
    }

    private async Task<string> SaveFailedPayloadAsync(string category, string url, string payload)
    {
        try
        {
            var directory = GetDebugDumpDirectory();
            var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss-fff");
            var fileName = $"{SanitizeForFileName(category)}-{timestamp}-{Guid.NewGuid():N}.txt";
            var fullPath = Path.GetFullPath(Path.Combine(directory, fileName));

            var dump = $"UTC: {DateTimeOffset.UtcNow:O}{Environment.NewLine}URL: {url}{Environment.NewLine}{Environment.NewLine}{payload}";
            await File.WriteAllTextAsync(fullPath, dump);
            return fullPath;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to write external-read failure dump.");
            return "<failed-to-write-dump>";
        }
    }

    private static string GetDebugDumpDirectory()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var directory = Path.Combine(appData, "InfinityMercsApp", "debug", DebugDumpFolderName);
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static async Task<string> TryReadResponseBodyAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            return await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            return $"<failed-to-read-response-body: {ex.Message}>";
        }
    }

    private static string SanitizeForFileName(string value)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var buffer = value.ToCharArray();
        for (var i = 0; i < buffer.Length; i++)
        {
            if (invalidChars.Contains(buffer[i]))
            {
                buffer[i] = '_';
            }
        }

        return new string(buffer);
    }

    private static bool LooksLikeJsonObject(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return false;
        }

        foreach (var ch in payload)
        {
            if (!char.IsWhiteSpace(ch))
            {
                return ch == '{';
            }
        }

        return false;
    }
}
