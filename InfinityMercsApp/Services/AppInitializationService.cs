namespace InfinityMercsApp.Services;

public class AppInitializationService
{
    private readonly Data.Database.IDatabaseContext _databaseContext;

    public AppInitializationService(Data.Database.IDatabaseContext databaseContext)
    {
        _databaseContext = databaseContext;
    }

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        return _databaseContext.InitializeAsync(cancellationToken);
    }
}
