using InfinityMercsApp.Domain.Models.Army;
using InfinityMercsApp.Infrastructure.Repositories;
using DbMetadataFaction = InfinityMercsApp.Infrastructure.Models.Database.Metadata.Faction;

namespace InfinityMercsApp.Infrastructure.Providers;

/// <inheritdoc/>
public sealed class TagCompanyFactionGenerator(
    ISQLiteRepository sqliteRepository,
    IFactionProvider factionProvider,
    IArmyImportProvider armyImportProvider) : ITagCompanyFactionGenerator
{
    public const int TagCompanyFactionId = 2003;
    private const int TagCompanyUnitId = 1;
    private const string TagCompanyDefaultUnitName = "Repurposed Mining Equipment";
    private const string TagCompanyVersion = "tag-company-repurposed-mining-equipment-v3";

    /// <inheritdoc/>
    public async Task GenerateAsync(CancellationToken cancellationToken = default)
    {
        Console.WriteLine("[TagGen] GenerateAsync started.");

        var existing = factionProvider.GetFactionSnapshot(TagCompanyFactionId);
        if (existing is not null && string.Equals(existing.Version, TagCompanyVersion, StringComparison.Ordinal))
        {
            InsertMetadataFactionEntry();
            Console.WriteLine($"[TagGen] No changes detected (version {TagCompanyVersion}), metadata refreshed.");
            return;
        }

        var tagFaction = BuildTagFaction();

        await armyImportProvider.ImportAsync(TagCompanyFactionId, tagFaction, cancellationToken);
        InsertMetadataFactionEntry();

        Console.WriteLine($"[TagGen] Synthetic faction {TagCompanyFactionId} imported with {tagFaction.Units.Count} unit.");
    }

    private void InsertMetadataFactionEntry()
    {
        sqliteRepository.Delete<DbMetadataFaction>(x => x.Id == TagCompanyFactionId);
        sqliteRepository.Insert(new[]
        {
            new DbMetadataFaction
            {
                Id = TagCompanyFactionId,
                ParentId = TagCompanyFactionId,
                Name = "TAG Company",
                Slug = "tag-company",
                Discontinued = false,
                Logo = "SVGCache/MercsIcons/noun-battle-mech-1731140.svg"
            }
        });
    }

    private static ArmyImportFaction BuildTagFaction()
    {
        var factionsJson = """
                           {
                             "sourceFactionId": 2003,
                             "sourceUnitId": 1,
                             "sourceFactionIds": [2003]
                           }
                           """;

        return new ArmyImportFaction
        {
            Version = TagCompanyVersion,
            Units =
            [
                new ArmyImportUnit
                {
                    Id = TagCompanyUnitId,
                    IdArmy = null,
                    Canonical = null,
                    Isc = TagCompanyDefaultUnitName,
                    IscAbbr = null,
                    Name = TagCompanyDefaultUnitName,
                    Slug = "repurposed-mining-equipment",
                    ProfileGroupsJson = BuildTagProfileGroupsJson(),
                    OptionsJson = "[]",
                    FiltersJson = BuildTagUnitFiltersJson(),
                    FactionsJson = factionsJson
                }
            ],
            Resume =
            [
                new ArmyImportResume
                {
                    Id = TagCompanyUnitId,
                    IdArmy = null,
                    Isc = TagCompanyDefaultUnitName,
                    Name = TagCompanyDefaultUnitName,
                    Slug = "repurposed-mining-equipment",
                    Logo = $"{TagCompanyFactionId}-{TagCompanyUnitId}",
                    Type = 4,
                    Category = 11
                }
            ],
            FiltersJson = BuildTagFactionFiltersJson(),
            ReinforcementsJson = null,
            FireteamsJson = null,
            RelationsJson = null,
            SpecopsJson = null,
            FireteamChartJson = null,
            RawJson = string.Empty
        };
    }

    private static string BuildTagProfileGroupsJson()
    {
        return """
               [
                 {
                   "notes": null,
                   "isc": "Repurposed Mining Equipment",
                   "profiles": [
                     {
                       "bts": 3,
                       "cc": 15,
                       "move": [15, 5],
                       "notes": null,
                       "includes": [],
                       "type": 4,
                       "ava": 1,
                       "str": true,
                       "bs": 12,
                       "s": 5,
                       "equip": [],
                       "w": 2,
                       "ph": 14,
                       "name": "Repurposed Mining Equipment",
                       "logo": "SVGCache/units/2003-1.svg",
                       "id": 1,
                       "arm": 5,
                       "weapons": [],
                       "chars": [21],
                       "wip": 12,
                       "skills": [
                         { "id": 86, "order": 1 },
                         { "id": 240, "extra": [293], "order": 2 },
                         { "id": 40, "extra": [27], "order": 3 },
                         { "id": 254, "extra": [27], "order": 4 },
                         { "id": 162, "extra": [30], "order": 5 },
                         { "id": 239, "extra": [31, 7], "order": 6 }
                       ],
                       "peripheral": [{ "id": 314 }]
                     }
                   ],
                   "options": [
                     {
                       "includes": [{ "q": 1, "group": 2, "option": 1 }],
                       "minis": 1,
                       "points": 40,
                       "equip": [],
                       "name": "REPURPOSED MINING EQUIPMENT",
                       "disabled": false,
                       "orders": [{ "type": "REGULAR", "list": 1, "total": 1 }],
                       "id": 1,
                       "weapons": [
                         { "id": 33, "order": 1 },
                         { "id": 69, "order": 2 },
                         { "id": 5, "extra": [303], "order": 3 }
                       ],
                       "chars": [],
                       "swc": "0",
                       "skills": [],
                       "peripheral": [{ "id": 314 }]
                     },
                     {
                       "includes": [{ "q": 1, "group": 2, "option": 1 }],
                       "minis": 1,
                       "points": 40,
                       "equip": [],
                       "name": "REPURPOSED MINING EQUIPMENT (LIEUTENANT)",
                       "disabled": false,
                       "orders": [
                         { "type": "REGULAR", "list": 1, "total": 1 },
                         { "type": "LIEUTENANT", "list": 1, "total": 1 }
                       ],
                       "id": 2,
                       "weapons": [
                         { "id": 33, "order": 1 },
                         { "id": 69, "order": 2 },
                         { "id": 5, "extra": [303], "order": 3 }
                       ],
                       "chars": [],
                       "swc": "0",
                       "skills": [
                         { "id": 119, "order": 1 }
                       ],
                       "peripheral": [{ "id": 314 }]
                     }
                   ],
                   "id": 1,
                   "category": 11
                 },
                 {
                   "notes": null,
                   "isc": "Turtlemek",
                   "profiles": [
                     {
                       "bts": 3,
                       "cc": "-",
                       "move": [15, 10],
                       "notes": null,
                       "includes": [],
                       "type": 5,
                       "ava": -1,
                       "str": true,
                       "bs": "-",
                       "s": 1,
                       "equip": [],
                       "w": 1,
                       "ph": 10,
                       "name": "Turtlemek",
                       "logo": "SVGCache/MercsIcons/tag_company.svg",
                       "id": 1,
                       "arm": 0,
                       "weapons": [],
                       "chars": [],
                       "wip": 12,
                       "skills": [
                         { "id": 84, "order": 1 },
                         { "id": 40, "extra": [1], "order": 2 },
                         { "id": 28, "extra": [6], "order": 3 },
                         { "id": 243, "extra": [322], "order": 4 },
                         { "id": 189, "order": 5 }
                       ],
                       "peripheral": []
                     }
                   ],
                   "options": [
                     {
                       "includes": [],
                       "minis": 1,
                       "points": 0,
                       "equip": [],
                       "name": "TURTLEMEK",
                       "disabled": true,
                       "orders": [],
                       "id": 1,
                       "weapons": [],
                       "chars": [],
                       "swc": "0",
                       "skills": [],
                       "peripheral": []
                     }
                   ],
                   "id": 2,
                   "category": 0
                 }
               ]
               """;
    }

    private static string BuildTagUnitFiltersJson()
    {
        return """
               {
                 "categories": [11],
                 "skills": [28, 40, 84, 86, 119, 162, 189, 239, 240, 243, 254],
                 "equip": [],
                 "chars": [21],
                 "types": [4, 5],
                 "weapons": [5, 33, 69],
                 "ammunition": [2]
               }
               """;
    }

    private static string BuildTagFactionFiltersJson()
    {
        return """
               {
                 "type": [
                   { "id": 4, "name": "TAG" },
                   { "id": 5, "name": "REM" }
                 ],
                 "chars": [
                   { "id": 21, "name": "Hackable" }
                 ],
                 "skills": [
                   { "id": 28, "name": "Mimetism" },
                   { "id": 40, "name": "Dodge" },
                   { "id": 84, "name": "Courage" },
                   { "id": 86, "name": "NWI" },
                   { "id": 119, "name": "Lieutenant" },
                   { "id": 162, "name": "Immunity" },
                   { "id": 189, "name": "Specialist Operative" },
                   { "id": 239, "name": "ECM" },
                   { "id": 240, "name": "CC Weapon" },
                   { "id": 243, "name": "Peripheral" },
                   { "id": 254, "name": "Gizmokit" }
                 ],
                 "equip": [],
                 "weapons": [
                   { "id": 5, "name": "CC Weapon" },
                   { "id": 33, "name": "Combi Rifle" },
                   { "id": 69, "name": "Pistol" }
                 ],
                 "ammunition": [
                   { "id": 2, "name": "Normal" }
                 ],
                 "category": [
                   { "id": 11, "name": "Mercenary Troops" }
                 ],
                 "peripheral": [
                   { "id": 314, "name": "Turtlemek" }
                 ],
                 "extras": [
                   { "id": 1, "name": "+3", "type": "TEXT" },
                   { "id": 6, "name": "-3", "type": "TEXT" },
                   { "id": 7, "name": "-6", "type": "TEXT" },
                   { "id": 27, "name": "PH=11", "type": "TEXT" },
                   { "id": 30, "name": "Shock", "type": "TEXT" },
                   { "id": 31, "name": "Guided", "type": "TEXT" },
                   { "id": 293, "name": "Antimaterial", "type": "TEXT" },
                   { "id": 303, "name": "PS=6", "type": "TEXT" },
                   { "id": 322, "name": "Ancillary", "type": "TEXT" }
                 ]
               }
               """;
    }
}
