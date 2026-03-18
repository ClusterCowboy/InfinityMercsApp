namespace InfinityMercsApp.Infrastructure.Providers;

using DomainMetadataDocument = InfinityMercsApp.Domain.Models.Metadata.MetadataDocument;
using DomainFaction = InfinityMercsApp.Domain.Models.Metadata.Faction;
using DomainSkill = InfinityMercsApp.Domain.Models.Metadata.Skill;
using DomainWeapon = InfinityMercsApp.Domain.Models.Metadata.Weapon;
using InfinityMercsApp.Infrastructure.Repositories;
using DbAmmunition = InfinityMercsApp.Infrastructure.Models.Database.Metadata.Ammunition;
using DbBooty = InfinityMercsApp.Infrastructure.Models.Database.Metadata.Booty;
using DbEquipment = InfinityMercsApp.Infrastructure.Models.Database.Metadata.Equipments;
using DbFaction = InfinityMercsApp.Infrastructure.Models.Database.Metadata.Faction;
using DbHackProgram = InfinityMercsApp.Infrastructure.Models.Database.Metadata.HackingProgram;
using DbMartialArt = InfinityMercsApp.Infrastructure.Models.Database.Metadata.MartialArt;
using DbMetachemistry = InfinityMercsApp.Infrastructure.Models.Database.Metadata.Metachemistry;
using DbSkill = InfinityMercsApp.Infrastructure.Models.Database.Metadata.Skill;
using DbWeapon = InfinityMercsApp.Infrastructure.Models.Database.Metadata.Weapon;

/// <inheritdoc/>
public sealed class MetadataProvider(ISQLiteRepository sqliteRepository) : IMetadataProvider
{
    /// <inheritdoc/>
    public void Import(DomainMetadataDocument metadata)
    {
        var factions = metadata.Factions.Select(x => new DbFaction
        {
            Id = x.Id,
            ParentId = x.ParentId,
            Name = x.Name,
            Slug = x.Slug,
            Discontinued = x.Discontinued,
            Logo = x.Logo
        }).ToList();

        var ammunitions = metadata.Ammunitions.Select(x => new DbAmmunition
        {
            Id = x.Id,
            Name = x.Name,
            Wiki = x.Wiki
        }).ToList();

        var weapons = metadata.Weapons.Select(x => new DbWeapon
        {
            WeaponKey = string.IsNullOrWhiteSpace(x.WeaponKey) ? BuildWeaponKey(x) : x.WeaponKey,
            WeaponId = x.WeaponId,
            Name = x.Name,
            Type = x.Type,
            Mode = x.Mode,
            Wiki = x.Wiki,
            AmmunitionId = x.AmmunitionId,
            Burst = x.Burst,
            Damage = x.Damage,
            Saving = x.Saving,
            SavingNum = x.SavingNum,
            Profile = x.Profile,
            PropertiesJson = x.PropertiesJson,
            DistanceJson = x.DistanceJson
        }).ToList();

        var skills = metadata.Skills.Select(x => new DbSkill
        {
            Id = x.Id,
            Name = x.Name,
            Wiki = x.Wiki
        }).ToList();

        var equips = metadata.Equips.Select(x => new DbEquipment
        {
            Id = x.Id,
            Name = x.Name,
            Wiki = x.Wiki
        }).ToList();

        var hacks = metadata.Hack.Select(x => new DbHackProgram
        {
            Name = x.Name,
            Opponent = x.Opponent,
            Special = x.Special,
            Damage = x.Damage,
            Attack = x.Attack,
            Burst = x.Burst,
            Extra = x.Extra,
            SkillTypeJson = x.SkillTypeJson,
            DevicesJson = x.DevicesJson,
            TargetJson = x.TargetJson
        }).ToList();

        var martialArts = metadata.MartialArts.Select(x => new DbMartialArt
        {
            Name = x.Name,
            Opponent = x.Opponent,
            Damage = x.Damage,
            Attack = x.Attack,
            Burst = x.Burst
        }).ToList();

        var metachemistry = metadata.Metachemistry.Select(x => new DbMetachemistry
        {
            Id = x.Id,
            Name = x.Name,
            Value = x.Value
        }).ToList();

        var booty = metadata.Booty.Select(x => new DbBooty
        {
            Id = x.Id,
            Name = x.Name,
            Value = x.Value
        }).ToList();

        sqliteRepository.DeleteAll<DbFaction>();
        sqliteRepository.DeleteAll<DbAmmunition>();
        sqliteRepository.DeleteAll<DbWeapon>();
        sqliteRepository.DeleteAll<DbSkill>();
        sqliteRepository.DeleteAll<DbEquipment>();
        sqliteRepository.DeleteAll<DbHackProgram>();
        sqliteRepository.DeleteAll<DbMartialArt>();
        sqliteRepository.DeleteAll<DbMetachemistry>();
        sqliteRepository.DeleteAll<DbBooty>();

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
        return sqliteRepository.GetAll<DbFaction>(x => true).Count() > 0;
    }

    /// <inheritdoc/>
    public IReadOnlyList<DomainFaction> GetFactions(bool includeDiscontinued = false)
    {
        var records = includeDiscontinued
            ? sqliteRepository.GetAll<DbFaction>(x => true, x => x.Name).ToList()
            : sqliteRepository.GetAll<DbFaction>(x => !x.Discontinued).OrderBy(x => x.Name).ToList();

        return records.Select(MapFaction).ToList();
    }

    /// <inheritdoc/>
    public DomainFaction? GetFactionById(int id)
    {
        var row = sqliteRepository.GetById<DbFaction>(id);
        return row is null ? null : MapFaction(row);
    }

    /// <inheritdoc/>
    public IReadOnlyList<DomainWeapon> SearchWeaponsByName(string searchTerm)
    {
        var records = string.IsNullOrWhiteSpace(searchTerm)
            ? sqliteRepository.GetAll<DbWeapon>(x => true, x => x.Name).Take(100).ToList()
            : sqliteRepository.GetAll<DbWeapon>(x => x.Name.Contains(searchTerm), x => x.Name).Take(100).ToList();
        return records.Select(MapWeapon).ToList();
    }

    /// <inheritdoc/>
    public IReadOnlyList<DomainSkill> GetSkills()
    {
        return sqliteRepository.GetAll<DbSkill>(x => true, x => x.Name)
            .Select(MapSkill)
            .ToList();
    }

    private static DomainFaction MapFaction(DbFaction source)
    {
        return new DomainFaction
        {
            Id = source.Id,
            ParentId = source.ParentId,
            Name = source.Name,
            Slug = source.Slug,
            Discontinued = source.Discontinued,
            Logo = source.Logo
        };
    }

    private static DomainWeapon MapWeapon(DbWeapon source)
    {
        return new DomainWeapon
        {
            WeaponKey = source.WeaponKey,
            WeaponId = source.WeaponId,
            Name = source.Name,
            Type = source.Type,
            Mode = source.Mode,
            Wiki = source.Wiki,
            AmmunitionId = source.AmmunitionId,
            Burst = source.Burst,
            Damage = source.Damage,
            Saving = source.Saving,
            SavingNum = source.SavingNum,
            Profile = source.Profile,
            PropertiesJson = source.PropertiesJson,
            DistanceJson = source.DistanceJson
        };
    }

    private static DomainSkill MapSkill(DbSkill source)
    {
        return new DomainSkill
        {
            Id = source.Id,
            Name = source.Name,
            Wiki = source.Wiki
        };
    }

    private static string BuildWeaponKey(DomainWeapon weapon)
    {
        return $"{weapon.WeaponId}:{weapon.Name}:{weapon.Mode ?? string.Empty}";
    }
}
