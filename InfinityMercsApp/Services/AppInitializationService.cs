using Microsoft.Maui.Storage;

namespace InfinityMercsApp.Services;

public class AppInitializationService
{
    private readonly Data.Database.IDatabaseContext _databaseContext;
    private readonly Data.Database.IMetadataAccessor _metadataAccessor;
    private readonly Data.Database.IArmyDataAccessor _armyDataAccessor;

    public AppInitializationService(
        Data.Database.IDatabaseContext databaseContext,
        Data.Database.IMetadataAccessor metadataAccessor,
        Data.Database.IArmyDataAccessor armyDataAccessor)
    {
        _databaseContext = databaseContext;
        _metadataAccessor = metadataAccessor;
        _armyDataAccessor = armyDataAccessor;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await _databaseContext.InitializeAsync(cancellationToken);

        var hasMetadata = await _metadataAccessor.HasMetadataAsync(cancellationToken);
        if (!hasMetadata)
        {
            await using var metadataStream = await FileSystem.Current.OpenAppPackageFileAsync("metadata.json");
            using var metadataReader = new StreamReader(metadataStream);
            var metadataJson = await metadataReader.ReadToEndAsync(cancellationToken);
            await _metadataAccessor.ImportFromJsonAsync(metadataJson, cancellationToken);
        }

        var hasArmy101 = await _armyDataAccessor.HasFactionArmyAsync(101, cancellationToken);
        if (!hasArmy101)
        {
            try
            {
                await using var armyStream = await FileSystem.Current.OpenAppPackageFileAsync("army-101.json");
                using var armyReader = new StreamReader(armyStream);
                var armyJson = await armyReader.ReadToEndAsync(cancellationToken);
                await _armyDataAccessor.ImportFactionArmyFromJsonAsync(101, armyJson, cancellationToken);
            }
            catch (FileNotFoundException)
            {
            }
        }
    }
}
