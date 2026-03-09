using InfinityMercsApp.Infrastructure.API.InfinityArmy;
using InfinityMercsApp.Infrastructure.Models.App;
using InfinityMercsApp.Infrastructure.Providers;
using InfinityMercsApp.Infrastructure.Repositories;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace InfinityMercsApp.Services;

public class AppInitializationService(
    ISQLiteRepository sqliteRepository, 
    IMetadataProvider metadataProvider, 
    IFactionProvider factionProvider, 
    IArmyImportProvider armyImportProvider, 
    IInfinityArmyAPI infinityArmyAPI, 
    FactionLogoCacheService factionLogoCacheService)
{
    private const string LastStartupUpdateAttemptKey = "startup_update_last_attempt_utc";
    private static readonly TimeSpan StartupUpdateInterval = TimeSpan.FromDays(7);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        Converters = { new RelaxedInt32Converter(), new RelaxedNullableInt32Converter() }
    };

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (!ShouldAttemptStartupUpdate())
        {
            return;
        }

        RecordStartupUpdateAttempt();

        var metadataJson = await infinityArmyAPI.GetMetaDataAsync(cancellationToken);
        metadataProvider.ImportFromJson(metadataJson);

        var metadataDocument = JsonSerializer.Deserialize<Infrastructure.Models.API.Metadata.MetadataDocument>(metadataJson, JsonOptions);
        if (metadataDocument is not null)
        {
            await factionLogoCacheService.CacheAllAsync(metadataDocument.Factions);

            foreach (var factionId in metadataDocument.Factions.Select(x => x.Id).Distinct())
            {
                var armyJson = await infinityArmyAPI.GetArmyDataAsync(factionId, cancellationToken);
                var armyDocument = JsonSerializer.Deserialize<Infrastructure.Models.API.Army.Faction>(armyJson, JsonOptions);
                var latestVersion = armyDocument?.Version;
                if (string.IsNullOrWhiteSpace(latestVersion))
                {
                    continue;
                }

                if (armyDocument?.Resume is not null)
                {
                    await factionLogoCacheService.CacheUnitLogosAsync(factionId, armyDocument.Resume, cancellationToken);
                }

                var snapshot = factionProvider.GetFactionSnapshot(factionId);
                var storedVersion = snapshot?.Version;

                if (!string.IsNullOrWhiteSpace(storedVersion) && CompareVersions(latestVersion, storedVersion) <= 0)
                {
                    continue;
                }

                await armyImportProvider.ImportFactionArmyFromJsonAsync(factionId, armyJson, cancellationToken);
            }
        }
    }

    private static int CompareVersions(string left, string right)
    {
        var leftParts = left.Split('.', StringSplitOptions.RemoveEmptyEntries);
        var rightParts = right.Split('.', StringSplitOptions.RemoveEmptyEntries);
        var maxLength = Math.Max(leftParts.Length, rightParts.Length);

        for (var i = 0; i < maxLength; i++)
        {
            var leftPart = i < leftParts.Length && int.TryParse(leftParts[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out var l) ? l : 0;
            var rightPart = i < rightParts.Length && int.TryParse(rightParts[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out var r) ? r : 0;

            if (leftPart > rightPart)
            {
                return 1;
            }

            if (leftPart < rightPart)
            {
                return -1;
            }
        }

        return 0;
    }

    private bool ShouldAttemptStartupUpdate()
    {
        var setting = sqliteRepository.GetAll<AppSetting>(x => x.Key == LastStartupUpdateAttemptKey).FirstOrDefault();

        if (setting is null)
        {
            return true;
        }

        if (!DateTimeOffset.TryParse(
                setting.Value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var lastAttemptUtc))
        {
            return true;
        }

        var elapsed = DateTimeOffset.UtcNow - lastAttemptUtc;
        return elapsed >= StartupUpdateInterval;
    }

    private void RecordStartupUpdateAttempt()
    {
        var value = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture);
        var existingSetting = sqliteRepository.GetAll<AppSetting>(x => x.Key == LastStartupUpdateAttemptKey).FirstOrDefault();

        if (existingSetting is null)
        {
            sqliteRepository.Insert([new AppSetting
            {
                Key = LastStartupUpdateAttemptKey,
                Value = value
            }]);
            return;
        }

        existingSetting.Value = value;
        sqliteRepository.Update(existingSetting);
    }
}
