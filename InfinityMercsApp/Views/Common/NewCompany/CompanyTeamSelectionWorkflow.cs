namespace InfinityMercsApp.Views.Common.NewCompany;

internal static class CompanyTeamSelectionWorkflow
{
    internal static TUnit? ResolveSelectedTeamUnit<TUnit>(
        IReadOnlyList<TUnit> units,
        int? resolvedUnitId,
        int? resolvedSourceFactionId,
        string? teamUnitSlug,
        string? teamUnitName,
        Func<TUnit, int> readUnitId,
        Func<TUnit, int> readSourceFactionId,
        Func<TUnit, string?> readSlug,
        Func<TUnit, string> readName)
        where TUnit : class
    {
        TUnit? resolved = null;
        if (resolvedUnitId.HasValue && resolvedSourceFactionId.HasValue)
        {
            resolved = units.FirstOrDefault(x =>
                readUnitId(x) == resolvedUnitId.Value &&
                readSourceFactionId(x) == resolvedSourceFactionId.Value);
        }

        resolved ??= units.FirstOrDefault(x =>
            !string.IsNullOrWhiteSpace(teamUnitSlug) &&
            !string.IsNullOrWhiteSpace(readSlug(x)) &&
            string.Equals(readSlug(x)?.Trim(), teamUnitSlug.Trim(), StringComparison.OrdinalIgnoreCase));

        resolved ??= units.FirstOrDefault(x =>
            string.Equals(readName(x), teamUnitName, StringComparison.OrdinalIgnoreCase));

        return resolved;
    }
}

