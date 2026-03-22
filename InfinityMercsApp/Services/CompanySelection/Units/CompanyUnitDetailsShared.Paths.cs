namespace InfinityMercsApp.Views.Common;

internal static partial class CompanyUnitDetailsShared
{
    internal static IEnumerable<string?> BuildUnitCachedPathCandidates(
        string? itemCachedLogoPath,
        int sourceFactionId,
        int unitId,
        int? leftFactionId,
        int? rightFactionId,
        Func<int, int, string?> getCachedUnitLogoPath,
        Func<int, string?> getCachedLogoPath)
    {
        yield return itemCachedLogoPath;
        yield return getCachedUnitLogoPath(sourceFactionId, unitId);

        if (leftFactionId.HasValue)
        {
            yield return getCachedUnitLogoPath(leftFactionId.Value, unitId);
        }

        if (rightFactionId.HasValue)
        {
            yield return getCachedUnitLogoPath(rightFactionId.Value, unitId);
        }

        yield return getCachedLogoPath(sourceFactionId);
    }

    internal static IEnumerable<string?> BuildUnitPackagedPathCandidates(
        string? itemPackagedLogoPath,
        int sourceFactionId,
        int unitId,
        int? leftFactionId,
        int? rightFactionId,
        Func<int, int, string?>? getPackagedUnitLogoPath,
        Func<int, string?>? getPackagedFactionLogoPath)
    {
        yield return itemPackagedLogoPath;

        if (getPackagedUnitLogoPath is not null && getPackagedFactionLogoPath is not null)
        {
            yield return getPackagedUnitLogoPath(sourceFactionId, unitId);
            if (leftFactionId.HasValue)
            {
                yield return getPackagedUnitLogoPath(leftFactionId.Value, unitId);
            }

            if (rightFactionId.HasValue)
            {
                yield return getPackagedUnitLogoPath(rightFactionId.Value, unitId);
            }

            yield return getPackagedFactionLogoPath(sourceFactionId);
            yield break;
        }

        yield return $"SVGCache/units/{sourceFactionId}-{unitId}.svg";
        if (leftFactionId.HasValue)
        {
            yield return $"SVGCache/units/{leftFactionId.Value}-{unitId}.svg";
        }

        if (rightFactionId.HasValue)
        {
            yield return $"SVGCache/units/{rightFactionId.Value}-{unitId}.svg";
        }

        yield return $"SVGCache/factions/{sourceFactionId}.svg";
    }

    internal static List<TFaction> BuildUnitSourceFactions<TFaction>(
        bool showRightSelectionBox,
        TFaction? leftSlotFaction,
        TFaction? rightSlotFaction,
        Func<TFaction, int> readFactionId)
        where TFaction : class
    {
        if (!showRightSelectionBox)
        {
            return leftSlotFaction is null ? [] : [leftSlotFaction];
        }

        var list = new List<TFaction>(2);
        if (leftSlotFaction is not null)
        {
            list.Add(leftSlotFaction);
        }

        if (rightSlotFaction is not null &&
            (leftSlotFaction is null || readFactionId(rightSlotFaction) != readFactionId(leftSlotFaction)))
        {
            list.Add(rightSlotFaction);
        }

        return list;
    }

    /// <summary>
    /// Returns only the faction assigned to the currently active slot.
    /// When <paramref name="activeSlotIndex"/> is 0, the left slot faction is returned;
    /// when 1, the right slot faction is returned.
    /// This is used by company types where each slot displays its own independent unit list
    /// (e.g. Airborne Company), as opposed to company types that merge both slots' units together.
    /// </summary>
    internal static List<TFaction> BuildUnitSourceFactionsForActiveSlot<TFaction>(
        int activeSlotIndex,
        TFaction? leftSlotFaction,
        TFaction? rightSlotFaction)
        where TFaction : class
    {
        var faction = activeSlotIndex switch
        {
            0 => leftSlotFaction,
            1 => rightSlotFaction,
            _ => leftSlotFaction
        };

        return faction is null ? [] : [faction];
    }
}
