using InfinityMercsApp.Models;
using Microsoft.Maui.Storage;
using SQLite;

namespace InfinityMercsApp.Data.Database;

public class DatabaseContext : IDatabaseContext
{
    private readonly SQLiteAsyncConnection _connection;
    private bool _isInitialized;

    public DatabaseContext()
    {
        DatabasePath = Path.Combine(FileSystem.Current.AppDataDirectory, "infinitymercs.db3");
        _connection = new SQLiteAsyncConnection(DatabasePath);
    }

    public string DatabasePath { get; }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_isInitialized)
        {
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();
        await _connection.CreateTableAsync<AppSetting>();
        _isInitialized = true;
    }
}
