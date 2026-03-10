namespace InfinityMercsApp.Infrastructure.Providers;

using InfinityMercsApp.Domain.Models.DataImport;
using InfinityMercsApp.Infrastructure.Models.Database.Metadata;
using InfinityMercsApp.Infrastructure.Repositories;
using System.Text.Json;

/// <inheritdoc/>
public sealed class MetadataProvider(ISQLiteRepository sqliteRepository) : IMetadataProvider
{
    /// <inheritdoc/>
    public void Import(Models.API.Metadata.MetadataDocument metadata)
    {
        var factions = metadata.Factions.Select(x => new Faction
        {
            Id = x.Id,
            ParentId = x.Parent,
            Name = x.Name,
            Slug = x.Slug,
            Discontinued = x.Discontinued,
            Logo = x.Logo
        }).ToList();

        var ammunitions = metadata.Ammunitions.Select(x => new Ammunition
        {
            Id = x.Id,
            Name = x.Name,
            Wiki = x.Wiki
        }).ToList();

        var weapons = metadata.Weapons.Select(x => new Weapon
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

        var skills = metadata.Skills.Select(x => new Skill
        {
            Id = x.Id,
            Name = x.Name,
            Wiki = x.Wiki
        }).ToList();

        var equips = metadata.Equips.Select(x => new Equipments
        {
            Id = x.Id,
            Name = x.Name,
            Wiki = x.Wiki
        }).ToList();

        var hacks = metadata.Hack.Select(x => new HackingProgram
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

        var martialArts = metadata.MartialArts.Select(x => new MartialArt
        {
            Name = x.Name,
            Opponent = x.Opponent,
            Damage = x.Damage,
            Attack = x.Attack,
            Burst = x.Burst
        }).ToList();

        var metachemistry = metadata.Metachemistry.Select(x => new Metachemistry
        {
            Id = x.Id,
            Name = x.Name,
            Value = x.Value
        }).ToList();

        var booty = metadata.Booty.Select(x => new Booty
        {
            Id = x.Id,
            Name = x.Name,
            Value = x.Value
        }).ToList();

        sqliteRepository.DeleteAll<Faction>();
        sqliteRepository.DeleteAll<Ammunition>();
        sqliteRepository.DeleteAll<Weapon>();
        sqliteRepository.DeleteAll<Skill>();
        sqliteRepository.DeleteAll<Equipments>();
        sqliteRepository.DeleteAll<HackingProgram>();
        sqliteRepository.DeleteAll<MartialArt>();
        sqliteRepository.DeleteAll<Metachemistry>();
        sqliteRepository.DeleteAll<Booty>();

        sqliteRepository.Insert(factions);
        sqliteRepository.Insert(ammunitions);
        sqliteRepository.Insert(weapons);
        sqliteRepository.Insert(skills);
        sqliteRepository.Insert(equips);
        sqliteRepository.Insert(hacks);
        sqliteRepository.Insert(martialArts);
        sqliteRepository.Insert(metachemistry);
        sqliteRepository.Insert(booty);
    }

    /// <inheritdoc/>
    public bool HasMetadata()
    {
        return sqliteRepository.GetAll<Faction>(x => true).Count() > 0;
    }

    /// <inheritdoc/>
    public IReadOnlyList<Faction> GetFactions(bool includeDiscontinued = false)
    {
        if (!includeDiscontinued)
        {
            return sqliteRepository.GetAll<Faction>(x => !x.Discontinued).OrderBy(x => x.Name).ToList();
        }

        return sqliteRepository.GetAll<Faction>(x => true, x => x.Name).ToList();
    }

    /// <inheritdoc/>
    public Faction? GetFactionById(int id)
    {
        return sqliteRepository.GetById<Faction>(id);
    }

    /// <inheritdoc/>
    public IReadOnlyList<Weapon> SearchWeaponsByName(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return sqliteRepository.GetAll<Weapon>(x => true, x => x.Name).Take(100).ToList();
        }

        return sqliteRepository.GetAll<Weapon>(x => x.Name.Contains(searchTerm), x => x.Name).Take(100).ToList();
    }

    /// <inheritdoc/>
    public IReadOnlyList<Skill> GetSkills()
    {
        return sqliteRepository.GetAll<Skill>(x => true, x => x.Name).ToList();
    }

    /// <inheritdoc/>
    private static string BuildWeaponKey(Models.API.Metadata.Weapon weapon)
    {
        return $"{weapon.Id}:{weapon.Name}:{weapon.Mode ?? string.Empty}";
    }
}
