
namespace InfinityMercsApp.Infrastructure.Repositories;

using InfinityMercsApp.Infrastructure.Options;
using SQLite;
using System.Linq.Expressions;

/// <inheritdoc/>
public sealed class SQLiteRepository : ISQLiteRepository
{
    private readonly SQLiteConnection _connection;

    // The single connection is not thread-safe, yet it is shared between the UI thread and the
    // background daily-sync. Every access is serialized through this gate.
    private readonly object _sync = new();

    // CreateTable issues a reflection scan plus a PRAGMA table_info migration check. It used to run
    // on every single repository call; now each table is created at most once per process.
    private readonly HashSet<Type> _createdTables = [];

    public SQLiteRepository(SQLIteConfiguration databasePath)
    {
        _connection = new SQLiteConnection(databasePath.DBPath);

        // WAL lets reads proceed without blocking on writes (e.g. UI reads during the daily sync),
        // synchronous=NORMAL drops a full fsync per transaction, and the busy timeout absorbs the
        // brief contention window instead of surfacing "database is locked".
        _connection.BusyTimeout = TimeSpan.FromSeconds(5);
        _connection.ExecuteScalar<string>("PRAGMA journal_mode=WAL");
        _connection.Execute("PRAGMA synchronous=NORMAL");
    }

    /// <inheritdoc/>
    public void Insert<T>(IEnumerable<T> recordsToInsert) where T : new()
    {
        EnsureCreated<T>();
        lock (_sync)
        {
            _connection.InsertAll(recordsToInsert);
        }
    }

    /// <inheritdoc/>
    public void Update<T>(T item) where T : new()
    {
        EnsureCreated<T>();
        lock (_sync)
        {
            _connection.Update(item);
        }
    }

    /// <inheritdoc/>
    public T? GetById<T>(int id) where T : new()
    {
        EnsureCreated<T>();
        lock (_sync)
        {
            return _connection.Find<T>(id);
        }
    }

    /// <inheritdoc/>
    public IReadOnlyList<T> GetAll<T>(Expression<Func<T, bool>> filter, Expression<Func<T, object>>? orderBy = null) where T : new()
    {
        EnsureCreated<T>();
        lock (_sync)
        {
            var query = _connection.Table<T>().Where(filter);

            if (orderBy is not null)
            {
                return query.OrderBy(orderBy).ToList();
            }

            return query.ToList();
        }
    }

    /// <inheritdoc/>
    public IReadOnlyList<T> GetAll<T>(Expression<Func<T, bool>> filter, Expression<Func<T, object>>? orderBy, int limit) where T : new()
    {
        EnsureCreated<T>();
        lock (_sync)
        {
            var query = _connection.Table<T>().Where(filter);

            if (orderBy is not null)
            {
                query = query.OrderBy(orderBy);
            }

            return query.Take(limit).ToList();
        }
    }

    /// <inheritdoc/>
    public T? FirstOrDefault<T>(Expression<Func<T, bool>> filter) where T : new()
    {
        EnsureCreated<T>();
        lock (_sync)
        {
            return _connection.Table<T>().Where(filter).FirstOrDefault();
        }
    }

    /// <inheritdoc/>
    public int Count<T>(Expression<Func<T, bool>> filter) where T : new()
    {
        EnsureCreated<T>();
        lock (_sync)
        {
            return _connection.Table<T>().Where(filter).Count();
        }
    }

    /// <inheritdoc/>
    public bool Exists<T>(Expression<Func<T, bool>> filter) where T : new()
    {
        EnsureCreated<T>();
        lock (_sync)
        {
            return _connection.Table<T>().Where(filter).Take(1).Count() > 0;
        }
    }

    /// <inheritdoc/>
    public bool Any<T>() where T : new()
    {
        EnsureCreated<T>();
        lock (_sync)
        {
            var table = _connection.GetMapping<T>().TableName;
            return _connection.ExecuteScalar<int>($"SELECT EXISTS(SELECT 1 FROM \"{table}\")") > 0;
        }
    }

    /// <inheritdoc/>
    public IReadOnlyList<TResult> QueryColumn<T, TResult>(string columnName) where T : new()
    {
        EnsureCreated<T>();
        lock (_sync)
        {
            var table = _connection.GetMapping<T>().TableName;
            return _connection.QueryScalars<TResult>($"SELECT \"{columnName}\" FROM \"{table}\"");
        }
    }

    /// <inheritdoc/>
    public void Delete<T>(Expression<Func<T, bool>> filter) where T : new()
    {
        EnsureCreated<T>();
        lock (_sync)
        {
            _connection.Table<T>().Delete(filter);
        }
    }

    /// <inheritdoc/>
    public void DeleteAll<T>() where T : new()
    {
        EnsureCreated<T>();
        lock (_sync)
        {
            _connection.Table<T>().Delete(x => true);
        }
    }

    /// <inheritdoc/>
    public void RunInTransaction(Action action)
    {
        // The lock is re-entrant (Monitor), so repository calls inside the action that also
        // take _sync are fine on this thread.
        lock (_sync)
        {
            _connection.RunInTransaction(action);
        }
    }

    private void EnsureCreated<T>() where T : new()
    {
        lock (_sync)
        {
            if (_createdTables.Add(typeof(T)))
            {
                _connection.CreateTable<T>();
            }
        }
    }
}
