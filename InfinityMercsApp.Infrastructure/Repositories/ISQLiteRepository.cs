namespace InfinityMercsApp.Infrastructure.Repositories;

using System.Linq.Expressions;

/// <summary>
/// An interface to handle interaction with SQLite
/// </summary>
public interface ISQLiteRepository
{
    /// <summary>
    /// Gets a single record from a SQLite table.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="id"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public T GetById<T>(int id) where T: new();

    /// <summary>
    /// Gets all recrds from a SQLite table, optionally applying a filter and orderBy.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="filter"></param>
    /// <param name="orderBy"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public IReadOnlyList<T> GetAll<T>(Expression<Func<T, bool>> filter, Expression<Func<T, bool>> orderBy) where T: new();

    /// <summary>
    /// Deletes all records from a SQLite table.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public void DeleteAll<T>() where T : new();
}