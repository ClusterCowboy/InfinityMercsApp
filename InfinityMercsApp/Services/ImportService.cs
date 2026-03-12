using InfinityMercsApp.Domain.Models.DataImport;
using InfinityMercsApp.Infrastructure.API.InfinityArmy;
using InfinityMercsApp.Infrastructure.Models.App;
using InfinityMercsApp.Infrastructure.Providers;
using InfinityMercsApp.Infrastructure.Repositories;
using System.Globalization;

namespace InfinityMercsApp.Services;

/// <inheritdoc/>
internal class ImportService(
    IInfinityArmyAPI infinityArmyAPI,
    IFactionProvider factionProvider,
    IMetadataProvider metadataProvider,
    IArmyImportProvider armyImportProvider,
    ISQLiteRepository sqliteRepository,
    FactionLogoCacheService factionLogoCacheService) : IImportService
{
    private const string LastStartupUpdateAttemptKey = "startup_update_last_attempt_utc";
    private static readonly TimeSpan StartupUpdateInterval = TimeSpan.FromDays(7);

    /// <inheritdoc/>
    public async IAsyncEnumerable<SuccessWithStringResult> ImportFactionAsync(string factionIdAsString)
    {
        if (!int.TryParse(factionIdAsString, NumberStyles.Integer, CultureInfo.InvariantCulture, out var factionId) || factionId <= 0)
        {
            yield return new(false, "Enter a valid positive faction ID.");
            yield break;
        }

        yield return new(true, $"Downloading army data for faction {factionId}...");

        var latestArmy = await infinityArmyAPI.GetArmyDataAsync(factionId);

        if (latestArmy is null)
        {
            yield return new(false, "Call to Army API failed.");
            yield break;
        }

        var latestVersion = latestArmy.Version;

        if (string.IsNullOrWhiteSpace(latestVersion))
        {
            yield return new(false, "Downloaded army data has no version; skipped.");
            yield break;
        }

        var snapshot = factionProvider.GetFactionSnapshot(factionId);
        var storedVersion = snapshot?.Version;
        if (!string.IsNullOrWhiteSpace(storedVersion) && CompareVersions(latestVersion, storedVersion) <= 0)
        {
            yield return new(true, $"No update needed for faction {factionId}. Stored: {storedVersion}, Incoming: {latestVersion}.");
            yield break;
        }

        await armyImportProvider.ImportAsync(factionId, latestArmy);
        yield return new(true, $"Faction {factionId} updated to version {latestVersion}.");
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<SuccessWithStringResult> ImportMetadataAsync()
    {
        yield return new(true, "Downloading metadata...");

        var metadata = await infinityArmyAPI.GetMetaDataAsync();

        if (metadata is null || metadata.Factions.Count == 0)
        {
            yield return new(false, "Metadata imported to DB. No factions found.");
            yield break;
        }

        await factionLogoCacheService.CacheAllAsync(metadata.Factions);

        var factionIds = metadata.Factions
            .Select(f => f.Id)
            .Distinct()
            .OrderBy(x => x)
            .ToList();

        var updatedCount = 0;
        var skippedCount = 0;
        var errorCount = 0;
        foreach (var factionId in factionIds)
        {
            yield return new(true, $"Fetching faction data for {factionId}...");

            try
            {
                var latestArmy = await infinityArmyAPI.GetArmyDataAsync(factionId);

                if (latestArmy is null)
                {
                    skippedCount++;
                    continue;
                }

                var latestVersion = latestArmy.Version;

                if (latestArmy.Resume is not null)
                {
                    await factionLogoCacheService.CacheUnitLogosAsync(factionId, latestArmy.Resume);
                }

                if (string.IsNullOrWhiteSpace(latestVersion))
                {
                    skippedCount++;
                    continue;
                }

                var snapshot = factionProvider.GetFactionSnapshot(factionId);
                var storedVersion = snapshot?.Version;

                if (!string.IsNullOrWhiteSpace(storedVersion) && CompareVersions(latestVersion, storedVersion) <= 0)
                {
                    skippedCount++;
                    continue;
                }

                await armyImportProvider.ImportAsync(factionId, latestArmy);
                updatedCount++;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"ImportMetadataAsync faction {factionId} failed: {ex.Message}");
                errorCount++;
            }
        }

        yield return new(true, $"Metadata imported. Updated: {updatedCount}, Unchanged: {skippedCount}, Errors: {errorCount}.");
    }

    public async IAsyncEnumerable<SuccessWithStringResult> ImportAllDataAsync()
    {
        RecordStartupUpdateAttempt();

        yield return new(true, "Updating database: downloading metadata...");

        var metadata = await infinityArmyAPI.GetMetaDataAsync();
        yield return new(true, "Updating database: importing metadata...");

        if (metadata is null || metadata.Factions.Count == 0)
        {
            yield return new(false, "Metadata download succeeded but no factions were found.");
            yield break;
        }

        metadataProvider.Import(metadata);

        yield return new(true, "Updating SVGs: caching faction logos...");
        await factionLogoCacheService.CacheAllAsync(metadata.Factions);

        var factions = metadata.Factions
            .Select(f => new { f.Id, f.Name })
            .Distinct()
            .OrderBy(x => x.Id)
            .ToList();

        var updatedCount = 0;
        var skippedCount = 0;
        var errorCount = 0;

        foreach (var faction in factions)
        {
            yield return new(true, $"Updating factions: checking {faction.Name}...");

            try
            {
                var latestArmy = await infinityArmyAPI.GetArmyDataAsync(faction.Id);

                if (latestArmy is null)
                {
                    continue;
                }

                var latestVersion = latestArmy.Version;

                if (latestArmy.Resume is not null)
                {
                    await factionLogoCacheService.CacheUnitLogosAsync(faction.Id, latestArmy.Resume);
                }

                if (string.IsNullOrWhiteSpace(latestVersion))
                {
                    skippedCount++;
                    continue;
                }

                var snapshot = factionProvider.GetFactionSnapshot(faction.Id);
                var storedVersion = snapshot?.Version;

                if (!string.IsNullOrWhiteSpace(storedVersion) && CompareVersions(latestVersion, storedVersion) <= 0)
                {
                    skippedCount++;
                    continue;
                }

                await armyImportProvider.ImportAsync(faction.Id, latestArmy);
                updatedCount++;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"ImportAllDataAsync faction {faction.Id} ({faction.Name}) failed: {ex.Message}");
                errorCount++;
            }
        }

        yield return new(true, $"Update complete. Updated: {updatedCount}, Unchanged: {skippedCount}, Errors: {errorCount}.");
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
