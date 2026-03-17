namespace InfinityMercsApp.Views.Common.NewCompany;

internal sealed class CompanyTeamAggregate
{
    internal CompanyTeamAggregate(string name)
    {
        Name = name;
    }

    internal string Name { get; }
    internal int Duo { get; private set; }
    internal int Haris { get; private set; }
    internal int Core { get; private set; }
    internal Dictionary<string, (int Min, int Max, string? Slug, bool MinAsterisk)> UnitLimits { get; } = new(StringComparer.OrdinalIgnoreCase);

    internal void AddCounts(int duo, int haris, int core)
    {
        Duo += duo;
        Haris += haris;
        Core += core;
    }

    internal void MergeUnitLimit(string unitName, int min, int max, string? slug, bool minAsterisk)
    {
        if (UnitLimits.TryGetValue(unitName, out var existing))
        {
            UnitLimits[unitName] = (
                Math.Min(existing.Min, min),
                Math.Max(existing.Max, max),
                string.IsNullOrWhiteSpace(existing.Slug) ? slug : existing.Slug,
                existing.MinAsterisk || minAsterisk);
            return;
        }

        UnitLimits[unitName] = (min, max, slug, minAsterisk);
    }
}

