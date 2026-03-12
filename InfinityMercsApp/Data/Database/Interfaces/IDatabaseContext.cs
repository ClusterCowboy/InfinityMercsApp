using SQLite;

namespace InfinityMercsApp.Data.Database;

public interface IDatabaseContext
{
    string DatabasePath { get; }

    SQLiteAsyncConnection Connection { get; }

    Task InitializeAsync(CancellationToken cancellationToken = default);
}
