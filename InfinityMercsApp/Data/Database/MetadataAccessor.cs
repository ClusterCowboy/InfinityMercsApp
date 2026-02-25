using System.Text.Json;
using System.Text.Json.Serialization;
using SQLite;

namespace InfinityMercsApp.Data.Database;

public class MetadataAccessor : IMetadataAccessor
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        Converters = { new RelaxedInt32Converter(), new RelaxedNullableInt32Converter() }
    };

    private readonly IDatabaseContext _databaseContext;
    private readonly SQLiteAsyncConnection _connection;

    public MetadataAccessor(IDatabaseContext databaseContext)
    {
        _databaseContext = databaseContext;
        _connection = databaseContext.Connection;
    }

    public async Task ImportFromJsonAsync(string json, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _databaseContext.InitializeAsync(cancellationToken);

        var document = JsonSerializer.Deserialize<MetadataDocument>(json, JsonOptions)
            ?? throw new InvalidOperationException("Failed to deserialize metadata JSON.");

        var factions = document.Factions.Select(x => new FactionRecord
        {
            Id = x.Id,
            ParentId = x.Parent,
            Name = x.Name,
            Slug = x.Slug,
            Discontinued = x.Discontinued,
            Logo = x.Logo
        }).ToList();

        var ammunitions = document.Ammunitions.Select(x => new AmmunitionRecord
        {
            Id = x.Id,
            Name = x.Name,
            Wiki = x.Wiki
        }).ToList();

        var weapons = document.Weapons.Select(x => new WeaponRecord
        {
            WeaponKey = BuildWeaponKey(x),
            WeaponId = x.Id,
            Name = x.Name,
            Type = x.Type,
            Mode = x.Mode,
            Wiki = x.Wiki,
            AmmunitionId = x.Ammunition,
            Burst = x.Burst,
            Damage = x.Damage,
            Saving = x.Saving,
            SavingNum = x.SavingNum,
            Profile = x.Profile,
            PropertiesJson = x.Properties is null ? null : JsonSerializer.Serialize(x.Properties),
            DistanceJson = x.Distance is null ? null : JsonSerializer.Serialize(x.Distance)
        }).ToList();

        var skills = document.Skills.Select(x => new SkillRecord
        {
            Id = x.Id,
            Name = x.Name,
            Wiki = x.Wiki
        }).ToList();

        var equips = document.Equips.Select(x => new EquipRecord
        {
            Id = x.Id,
            Name = x.Name,
            Wiki = x.Wiki
        }).ToList();

        var hacks = document.Hack.Select(x => new HackProgramRecord
        {
            Name = x.Name,
            Opponent = x.Opponent,
            Special = x.Special,
            Damage = x.Damage,
            Attack = x.Attack,
            Burst = x.Burst,
            Extra = x.Extra,
            SkillTypeJson = x.SkillType is null ? null : JsonSerializer.Serialize(x.SkillType),
            DevicesJson = x.Devices is null ? null : JsonSerializer.Serialize(x.Devices),
            TargetJson = x.Target is null ? null : JsonSerializer.Serialize(x.Target)
        }).ToList();

        var martialArts = document.MartialArts.Select(x => new MartialArtRecord
        {
            Name = x.Name,
            Opponent = x.Opponent,
            Damage = x.Damage,
            Attack = x.Attack,
            Burst = x.Burst
        }).ToList();

        var metachemistry = document.Metachemistry.Select(x => new MetachemistryRecord
        {
            Id = x.Id,
            Name = x.Name,
            Value = x.Value
        }).ToList();

        var booty = document.Booty.Select(x => new BootyRecord
        {
            Id = x.Id,
            Name = x.Name,
            Value = x.Value
        }).ToList();

        await _connection.DeleteAllAsync<FactionRecord>();
        await _connection.DeleteAllAsync<AmmunitionRecord>();
        await _connection.DeleteAllAsync<WeaponRecord>();
        await _connection.DeleteAllAsync<SkillRecord>();
        await _connection.DeleteAllAsync<EquipRecord>();
        await _connection.DeleteAllAsync<HackProgramRecord>();
        await _connection.DeleteAllAsync<MartialArtRecord>();
        await _connection.DeleteAllAsync<MetachemistryRecord>();
        await _connection.DeleteAllAsync<BootyRecord>();

        if (factions.Count > 0) await _connection.InsertAllAsync(factions);
        if (ammunitions.Count > 0) await _connection.InsertAllAsync(ammunitions);
        if (weapons.Count > 0) await _connection.InsertAllAsync(weapons);
        if (skills.Count > 0) await _connection.InsertAllAsync(skills);
        if (equips.Count > 0) await _connection.InsertAllAsync(equips);
        if (hacks.Count > 0) await _connection.InsertAllAsync(hacks);
        if (martialArts.Count > 0) await _connection.InsertAllAsync(martialArts);
        if (metachemistry.Count > 0) await _connection.InsertAllAsync(metachemistry);
        if (booty.Count > 0) await _connection.InsertAllAsync(booty);
    }

    public async Task ImportFromFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var json = await File.ReadAllTextAsync(filePath, cancellationToken);
        await ImportFromJsonAsync(json, cancellationToken);
    }

    public async Task<bool> HasMetadataAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _databaseContext.InitializeAsync(cancellationToken);
        return await _connection.Table<FactionRecord>().CountAsync() > 0;
    }

    public async Task<IReadOnlyList<FactionRecord>> GetFactionsAsync(bool includeDiscontinued = false, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _databaseContext.InitializeAsync(cancellationToken);

        var query = _connection.Table<FactionRecord>();
        if (!includeDiscontinued)
        {
            query = query.Where(x => !x.Discontinued);
        }

        return await query.OrderBy(x => x.Name).ToListAsync();
    }

    public async Task<FactionRecord?> GetFactionByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _databaseContext.InitializeAsync(cancellationToken);
        return await _connection.FindAsync<FactionRecord>(id);
    }

    public async Task<IReadOnlyList<WeaponRecord>> SearchWeaponsByNameAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _databaseContext.InitializeAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return await _connection.Table<WeaponRecord>()
                .OrderBy(x => x.Name)
                .Take(100)
                .ToListAsync();
        }

        return await _connection.Table<WeaponRecord>()
            .Where(x => x.Name.Contains(searchTerm))
            .OrderBy(x => x.Name)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<SkillRecord>> GetSkillsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _databaseContext.InitializeAsync(cancellationToken);
        return await _connection.Table<SkillRecord>().OrderBy(x => x.Name).ToListAsync();
    }

    private static string BuildWeaponKey(WeaponDto weapon)
    {
        return $"{weapon.Id}:{weapon.Name}:{weapon.Mode ?? string.Empty}";
    }
}
