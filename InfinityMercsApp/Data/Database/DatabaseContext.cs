using InfinityMercsApp.Models;
using Microsoft.Maui.Storage;
using SQLite;

namespace InfinityMercsApp.Data.Database;

public class DatabaseContext : IDatabaseContext
{
    public SQLiteAsyncConnection Connection { get; }
    private bool _isInitialized;

    public DatabaseContext()
    {
        DatabasePath = Path.Combine(FileSystem.Current.AppDataDirectory, "infinitymercs.db3");
        Connection = new SQLiteAsyncConnection(DatabasePath);
    }

    public string DatabasePath { get; }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_isInitialized)
        {
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();
        await Connection.CreateTableAsync<AppSetting>();
        await Connection.CreateTableAsync<FactionRecord>();
        await Connection.CreateTableAsync<AmmunitionRecord>();
        await Connection.CreateTableAsync<WeaponRecord>();
        await Connection.CreateTableAsync<SkillRecord>();
        await Connection.CreateTableAsync<EquipRecord>();
        await Connection.CreateTableAsync<HackProgramRecord>();
        await Connection.CreateTableAsync<MartialArtRecord>();
        await Connection.CreateTableAsync<MetachemistryRecord>();
        await Connection.CreateTableAsync<BootyRecord>();
        await Connection.CreateTableAsync<ArmyFactionRecord>();
        await Connection.CreateTableAsync<ArmyUnitRecord>();
        await Connection.CreateTableAsync<ArmyResumeRecord>();
        _isInitialized = true;
    }
}
