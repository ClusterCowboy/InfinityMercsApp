using InfinityMercsApp.Infrastructure.Providers;
using ArmyFactionRecord = InfinityMercsApp.Domain.Models.Army.Faction;
using ArmyResumeRecord = InfinityMercsApp.Domain.Models.Army.Resume;
using ArmyUnitRecord = InfinityMercsApp.Domain.Models.Army.Unit;
using MercsArmyListEntry = InfinityMercsApp.Domain.Models.Army.MercsArmyListEntry;

namespace InfinityMercsApp.Views.StandardCompany;

internal sealed class StandardCompanyDataCoordinator(
    IFactionProvider? factionProvider,
    ICohesiveCompanyFactionQueryProvider cohesiveCompanyFactionQueryProvider)
{
    public ArmyFactionRecord? GetFactionSnapshot(int factionId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (factionProvider is null || factionId <= 0)
        {
            return null;
        }

        return ToArmyFactionRecord(factionProvider.GetFactionSnapshot(factionId));
    }

    public IReadOnlyList<ArmyResumeRecord> GetResumeByFactionMercsOnly(int factionId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (factionProvider is null || factionId <= 0)
        {
            return [];
        }

        return factionProvider.GetResumeByFactionMercsOnly(factionId)
            .Select(ToArmyResumeRecord)
            .ToList();
    }

    public ArmyUnitRecord? GetUnit(int factionId, int unitId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (factionProvider is null || factionId <= 0 || unitId <= 0)
        {
            return null;
        }

        return ToArmyUnitRecord(factionProvider.GetUnit(factionId, unitId));
    }

    public async Task<IReadOnlyList<MercsArmyListEntry>> GetMergedMercsArmyListAsync(
        IReadOnlyCollection<int> factionIds,
        CancellationToken cancellationToken = default)
    {
        if (factionIds.Count == 0)
        {
            return [];
        }

        var queryResult = await cohesiveCompanyFactionQueryProvider.GetFilterQuerySourceAsync(factionIds, cancellationToken);
        return queryResult.MergedMercsListEntries;
    }

    private static ArmyFactionRecord? ToArmyFactionRecord(ArmyFactionRecord? faction)
    {
        if (faction is null)
        {
            return null;
        }

        return new ArmyFactionRecord
        {
            FactionId = faction.FactionId,
            Version = faction.Version,
            ImportedAtUnixSeconds = faction.ImportedAtUnixSeconds,
            ReinforcementsJson = faction.ReinforcementsJson,
            FiltersJson = faction.FiltersJson,
            FireteamsJson = faction.FireteamsJson,
            RelationsJson = faction.RelationsJson,
            SpecopsJson = faction.SpecopsJson,
            FireteamChartJson = faction.FireteamChartJson,
            RawJson = faction.RawJson
        };
    }

    private static ArmyResumeRecord ToArmyResumeRecord(ArmyResumeRecord resume)
    {
        return new ArmyResumeRecord
        {
            ResumeKey = resume.ResumeKey,
            FactionId = resume.FactionId,
            UnitId = resume.UnitId,
            IdArmy = resume.IdArmy,
            Isc = resume.Isc,
            Name = resume.Name,
            Slug = resume.Slug,
            Logo = resume.Logo,
            Type = resume.Type,
            Category = resume.Category
        };
    }

    private static ArmyUnitRecord? ToArmyUnitRecord(ArmyUnitRecord? unit)
    {
        if (unit is null)
        {
            return null;
        }

        return new ArmyUnitRecord
        {
            UnitKey = unit.UnitKey,
            FactionId = unit.FactionId,
            UnitId = unit.UnitId,
            IdArmy = unit.IdArmy,
            Canonical = unit.Canonical,
            Isc = unit.Isc,
            IscAbbr = unit.IscAbbr,
            Name = unit.Name,
            Slug = unit.Slug,
            ProfileGroupsJson = unit.ProfileGroupsJson,
            OptionsJson = unit.OptionsJson,
            FiltersJson = unit.FiltersJson,
            FactionsJson = unit.FactionsJson
        };
    }
}
