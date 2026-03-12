namespace InfinityMercsApp.Domain.Sorting;

/// <summary>
/// Shared army unit ordering rules used by list query paths.
/// </summary>
public static class ArmyUnitSort
{
    private static readonly Dictionary<int, int> UnitTypeSortOrder = new()
    {
        [1] = 0, // LI
        [2] = 1, // MI
        [3] = 2, // HI
        [4] = 3, // TAG
        [5] = 4, // REM
        [6] = 5, // SK
        [7] = 6, // WB
        [8] = 7  // VH
    };

    /// <summary>
    /// Returns the stable sort index for a unit type.
    /// Unknown values are sorted after known types.
    /// </summary>
    public static int GetUnitTypeSortIndex(int? unitType)
    {
        if (!unitType.HasValue)
        {
            return int.MaxValue - 1;
        }

        return UnitTypeSortOrder.TryGetValue(unitType.Value, out var sortIndex)
            ? sortIndex
            : int.MaxValue;
    }

    /// <summary>
    /// Orders a sequence by unit type (using shared type sort) then by name.
    /// </summary>
    public static IEnumerable<T> OrderByUnitTypeAndName<T>(
        IEnumerable<T> source,
        Func<T, int?> typeSelector,
        Func<T, string?> nameSelector)
    {
        return source
            .OrderBy(x => GetUnitTypeSortIndex(typeSelector(x)))
            .ThenBy(x => nameSelector(x) ?? string.Empty, StringComparer.OrdinalIgnoreCase);
    }
}
