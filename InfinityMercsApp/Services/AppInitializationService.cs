using System.Text.Json;
using System.Text.Json.Serialization;
using System.Globalization;
using InfinityMercsApp.Data.Database;
using InfinityMercsApp.Data.WebAccess;

namespace InfinityMercsApp.Services;

public class AppInitializationService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        Converters = { new RelaxedInt32Converter(), new RelaxedNullableInt32Converter() }
    };

    private readonly IDatabaseContext _databaseContext;
    private readonly IMetadataAccessor _metadataAccessor;
    private readonly IArmyDataAccessor _armyDataAccessor;
    private readonly IWebAccessObject _webAccessObject;

    public AppInitializationService(
        IDatabaseContext databaseContext,
        IMetadataAccessor metadataAccessor,
        IArmyDataAccessor armyDataAccessor,
        IWebAccessObject webAccessObject)
    {
        _databaseContext = databaseContext;
        _metadataAccessor = metadataAccessor;
        _armyDataAccessor = armyDataAccessor;
        _webAccessObject = webAccessObject;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await _databaseContext.InitializeAsync(cancellationToken);

        var hasMetadata = await _metadataAccessor.HasMetadataAsync(cancellationToken);
        if (!hasMetadata)
        {
            var metadataJson = await _webAccessObject.GetMetaDataAsync(cancellationToken);
            await _metadataAccessor.ImportFromJsonAsync(metadataJson, cancellationToken);

            var metadataDocument = JsonSerializer.Deserialize<MetadataDocument>(metadataJson, JsonOptions);
            if (metadataDocument is not null)
            {
                foreach (var factionId in metadataDocument.Factions.Select(x => x.Id).Distinct())
                {
                    var armyJson = await _webAccessObject.GetArmyDataAsync(factionId, cancellationToken);
                    var armyDocument = JsonSerializer.Deserialize<ArmyDocument>(armyJson, JsonOptions);
                    var latestVersion = armyDocument?.Version;
                    if (string.IsNullOrWhiteSpace(latestVersion))
                    {
                        continue;
                    }

                    var snapshot = await _armyDataAccessor.GetFactionSnapshotAsync(factionId, cancellationToken);
                    var storedVersion = snapshot?.Version;

                    if (!string.IsNullOrWhiteSpace(storedVersion) && CompareVersions(latestVersion, storedVersion) <= 0)
                    {
                        continue;
                    }

                    await _armyDataAccessor.ImportFactionArmyFromJsonAsync(factionId, armyJson, cancellationToken);
                }
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
}
