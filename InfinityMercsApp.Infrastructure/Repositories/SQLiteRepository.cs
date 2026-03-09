
namespace InfinityMercsApp.Infrastructure.Repositories;

using InfinityMercsApp.Infrastructure.Options;
using SQLite;
using System.Linq.Expressions;

/// <inheritdoc/>
public sealed class SQLiteRepository : ISQLiteRepository
{
    private SQLiteConnection _connection;

    public SQLiteRepository(SQLIteConfiguration databasePath)
    {
        //_databasePath = Path.Combine(FileSystem.Current.AppDataDirectory, "infinitymercs.db3");
        _connection = new SQLiteConnection(databasePath.DBPath);
    }

    /// <inheritdoc/>
    public void Insert<T>(IEnumerable<T> recordsToInsert) where T : new()
    {
        _connection.CreateTable<T>();
        
        var query = _connection.Table<T>();

        _connection.InsertAll(recordsToInsert);
    }

    /// <inheritdoc/>
    public void Update<T>(T item) where T : new()
    {
        _connection.CreateTable<T>();

        var query = _connection.Table<T>();

        _connection.Update(item);
    }

    /// <inheritdoc/>
    public T GetById<T>(int id) where T : new()
    {
        _connection.CreateTable<T>();

        var query = _connection.Table<T>();

        return _connection.Get<T>(id);
    }

    /// <inheritdoc/>
    public IReadOnlyList<T> GetAll<T>(Expression<Func<T, bool>> filter, Expression<Func<T, object>>? orderBy = null) where T : new()
    {
        _connection.CreateTable<T>();

        var query = _connection.Table<T>();

        if (orderBy is not null)
        {
            return query.Where(filter).OrderBy(orderBy).ToList();
        }

        return query.Where(filter).ToList();
    }

    /// <inheritdoc/>
    public void Delete<T>(Expression<Func<T, bool>> filter) where T : new()
    {
        _connection.CreateTable<T>();

        var query = _connection.Table<T>();

        query.Delete(filter);
    }

    /// <inheritdoc/>
    public void DeleteAll<T>() where T : new()
    {
        _connection.CreateTable<T>();

        var query = _connection.Table<T>();

        query.Delete(x => true);
    }
}
