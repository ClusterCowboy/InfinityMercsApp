namespace InfinityMercsApp.Infrastructure.Repositories;

using System.Linq.Expressions;

/// <summary>
/// An interface to handle interaction with SQLite.
/// This uses synchronous records because SQLite itself is synchronous.
/// </summary>
public interface ISQLiteRepository
{
    /// <summary>
    /// Inserts records into a SQLite table.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="recordsToInsert"></param>
    public void Insert<T>(IEnumerable<T> recordsToInsert) where T : new();

    /// <summary>
    /// Updates a record in a SQLite table.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="item"></param>
    public void Update<T>(T item) where T : new();

    /// <summary>
    /// Gets a single record from a SQLite table.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="id"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public T? GetById<T>(int id) where T: new();

    /// <summary>
    /// Gets all records from a SQLite table, optionally applying a filter and orderBy.
    /// </summary>
    public IReadOnlyList<T> GetAll<T>(Expression<Func<T, bool>> filter, Expression<Func<T, object>>? orderBy = null) where T: new();

    /// <summary>
    /// Gets up to <paramref name="limit"/> records from a SQLite table, applying a filter and optional orderBy.
    /// The LIMIT is applied at the SQL level.
    /// </summary>
    public IReadOnlyList<T> GetAll<T>(Expression<Func<T, bool>> filter, Expression<Func<T, object>>? orderBy, int limit) where T : new();

    /// <summary>
    /// Gets the first record matching the filter, or null. Uses SQL LIMIT 1.
    /// </summary>
    public T? FirstOrDefault<T>(Expression<Func<T, bool>> filter) where T : new();

    /// <summary>
    /// Returns the count of records matching the filter. Uses SQL COUNT.
    /// </summary>
    public int Count<T>(Expression<Func<T, bool>> filter) where T : new();

    /// <summary>
    /// Returns true if any record matches the filter. Uses SQL COUNT with LIMIT.
    /// </summary>
    public bool Exists<T>(Expression<Func<T, bool>> filter) where T : new();

    /// <summary>
    /// Deletes all records from a SQLite table by filter.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public void Delete<T>(Expression<Func<T, bool>> filter) where T : new();

    /// <summary>
    /// Deletes all records from a SQLite table.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public void DeleteAll<T>() where T : new();
}