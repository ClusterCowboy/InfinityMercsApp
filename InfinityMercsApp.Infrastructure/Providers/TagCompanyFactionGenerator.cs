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
    private const string TagCompanyVersion = "tag-company-empty-v1";

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

        var emptyFaction = new ArmyImportFaction
        {
            Version = TagCompanyVersion,
            Units = [],
            Resume = [],
            FiltersJson = BuildEmptyFiltersJson(),
            ReinforcementsJson = null,
            FireteamsJson = null,
            RelationsJson = null,
            SpecopsJson = null,
            FireteamChartJson = null,
            RawJson = string.Empty
        };

        await armyImportProvider.ImportAsync(TagCompanyFactionId, emptyFaction, cancellationToken);
        InsertMetadataFactionEntry();

        Console.WriteLine($"[TagGen] Synthetic faction {TagCompanyFactionId} imported with 0 units.");
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

    private static string BuildEmptyFiltersJson()
    {
        return """
               {
                 "type": [],
                 "chars": [],
                 "skills": [],
                 "equip": [],
                 "weapons": [],
                 "ammunition": [],
                 "category": [],
                 "peripheral": [],
                 "extras": []
               }
               """;
    }
}
