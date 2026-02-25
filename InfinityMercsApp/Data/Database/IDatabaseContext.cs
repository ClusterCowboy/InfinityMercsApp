namespace InfinityMercsApp.Data.Database;

public interface IDatabaseContext
{
    string DatabasePath { get; }

    Task InitializeAsync(CancellationToken cancellationToken = default);
}
