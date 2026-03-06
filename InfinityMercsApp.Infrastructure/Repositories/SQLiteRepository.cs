
namespace InfinityMercsApp.Infrastructure.Repositories;

using Microsoft.Extensions.Options;
using SQLite;
using System.Linq.Expressions;

/// <inheritdoc/>
public class SQLiteRepository : ISQLiteRepository
{
    private SQLiteConnection _connection;

    public SQLiteRepository(IOptions<string> databasePath)
    {
        //_databasePath = Path.Combine(FileSystem.Current.AppDataDirectory, "infinitymercs.db3");
        _connection = new SQLiteConnection(databasePath.Value);
    }

    /// <inheritdoc/>
    public T GetById<T>(int id) where T : new()
    {
        _connection.CreateTable<T>();

        var query = _connection.Table<T>();

        return _connection.Get<T>(id);
    }

    /// <inheritdoc/>
    public IReadOnlyList<T> GetAll<T>(Expression<Func<T, bool>> filter, Expression<Func<T, bool>> orderBy) where T : new()
    {
        _connection.CreateTable<T>();

        var query = _connection.Table<T>();

        return query.Where(filter).OrderBy(orderBy).ToList();
    }

    /// <inheritdoc/>
    public void DeleteAll<T>() where T : new()
    {
        _connection.CreateTable<T>();

        var query = _connection.Table<T>();

        query.Delete(x => true);
    }
}
