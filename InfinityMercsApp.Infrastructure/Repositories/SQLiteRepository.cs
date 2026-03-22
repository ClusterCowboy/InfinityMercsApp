
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
    public T? GetById<T>(int id) where T : new()
    {
        _connection.CreateTable<T>();

        var query = _connection.Table<T>();

        return _connection.Find<T>(id);
    }

    /// <inheritdoc/>
    public IReadOnlyList<T> GetAll<T>(Expression<Func<T, bool>> filter, Expression<Func<T, object>>? orderBy = null) where T : new()
    {
        _connection.CreateTable<T>();

        var query = _connection.Table<T>().Where(filter);

        if (orderBy is not null)
        {
            return query.OrderBy(orderBy).ToList();
        }

        return query.ToList();
    }

    /// <inheritdoc/>
    public IReadOnlyList<T> GetAll<T>(Expression<Func<T, bool>> filter, Expression<Func<T, object>>? orderBy, int limit) where T : new()
    {
        _connection.CreateTable<T>();

        var query = _connection.Table<T>().Where(filter);

        if (orderBy is not null)
        {
            query = query.OrderBy(orderBy);
        }

        return query.Take(limit).ToList();
    }

    /// <inheritdoc/>
    public T? FirstOrDefault<T>(Expression<Func<T, bool>> filter) where T : new()
    {
        _connection.CreateTable<T>();

        return _connection.Table<T>().Where(filter).FirstOrDefault();
    }

    /// <inheritdoc/>
    public int Count<T>(Expression<Func<T, bool>> filter) where T : new()
    {
        _connection.CreateTable<T>();

        return _connection.Table<T>().Where(filter).Count();
    }

    /// <inheritdoc/>
    public bool Exists<T>(Expression<Func<T, bool>> filter) where T : new()
    {
        _connection.CreateTable<T>();

        return _connection.Table<T>().Where(filter).Take(1).Count() > 0;
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
