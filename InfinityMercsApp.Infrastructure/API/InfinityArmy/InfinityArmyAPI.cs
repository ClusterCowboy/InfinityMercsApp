namespace InfinityMercsApp.Infrastructure.API.InfinityArmy;

using DomainArmyImportFaction = InfinityMercsApp.Domain.Models.Army.ArmyImportFaction;
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
        var apiDocument = JsonSerializer.Deserialize<ApiMetadataDocument>(await GetAsync(MetadataUrl, cancellationToken), jsonOptions);
        return apiDocument is null ? null : MapMetadataDocument(apiDocument);
    }

    /// <inheritdoc/>
    public async Task<DomainArmyImportFaction?> GetArmyDataAsync(int factionId, CancellationToken cancellationToken = default)
    {
        var fullUrl = $"{ArmyUrlBase}{factionId}";
        logger.LogInformation("Fetching army data from: {Url}", fullUrl);

        var apiFaction = JsonSerializer.Deserialize<Models.API.Army.Faction>(await GetAsync(fullUrl, cancellationToken));
        return apiFaction is null ? null : MapArmyImportFaction(apiFaction);
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

    private static DomainArmyImportFaction MapArmyImportFaction(Models.API.Army.Faction source)
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
            RawJson = JsonSerializer.Serialize(source)
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
            throw new HttpRequestException($"Request failed. Status={(int)response.StatusCode}");
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
}
