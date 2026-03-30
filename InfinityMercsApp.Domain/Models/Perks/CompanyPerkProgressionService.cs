namespace InfinityMercsApp.Domain.Models.Perks;

/// <summary>
/// Backend helper for trooper perk progression tied to trooper experience.
/// </summary>
public static class CompanyPerkProgressionService
{
    /// <summary>
    /// Returns how many total perk slots are unlocked by current XP.
    /// Uses the existing rank progression table so perk unlocks scale with leveling.
    /// </summary>
    public static int GetUnlockedPerkSlots(int experiencePoints)
    {
        return CompanyUnitExperienceRanks.GetRankLevel(experiencePoints);
    }

    /// <summary>
    /// Returns how many perk ranks are currently spent.
    /// </summary>
    public static int GetSpentPerkSlots(IEnumerable<CompanyTrooperPerkState>? perks)
    {
        if (perks is null)
        {
            return 0;
        }

        var total = 0;
        foreach (var perk in perks)
        {
            if (perk is null)
            {
                continue;
            }

            total += Math.Max(0, perk.Rank);
        }

        return total;
    }

    /// <summary>
    /// Returns how many perk slots are still available at current XP.
    /// </summary>
    public static int GetAvailablePerkSlots(int experiencePoints, IEnumerable<CompanyTrooperPerkState>? perks)
    {
        var unlocked = GetUnlockedPerkSlots(experiencePoints);
        var spent = GetSpentPerkSlots(perks);
        return Math.Max(0, unlocked - spent);
    }

    /// <summary>
    /// Validates perk state by collapsing duplicates and clamping ranks to valid values.
    /// </summary>
    public static List<CompanyTrooperPerkState> Normalize(IEnumerable<CompanyTrooperPerkState>? perks)
    {
        var normalized = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        if (perks is null)
        {
            return [];
        }

        foreach (var perk in perks)
        {
            if (perk is null || string.IsNullOrWhiteSpace(perk.Id))
            {
                continue;
            }

            var id = perk.Id.Trim();
            var rank = Math.Max(0, perk.Rank);
            if (rank == 0)
            {
                continue;
            }

            if (normalized.TryGetValue(id, out var existing))
            {
                normalized[id] = Math.Max(existing, rank);
            }
            else
            {
                normalized[id] = rank;
            }
        }

        return normalized
            .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
            .Select(x => new CompanyTrooperPerkState { Id = x.Key, Rank = x.Value })
            .ToList();
    }

    /// <summary>
    /// Attempts to apply or increase a perk rank. Respects unlocked slots and perk max rank.
    /// </summary>
    public static bool TryApplyPerkRank(
        int experiencePoints,
        IList<CompanyTrooperPerkState> perks,
        string perkId,
        out string? error)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(perkId))
        {
            error = "Perk id is required.";
            return false;
        }

        var definition = CompanyPerkCatalog.FindById(perkId);
        if (definition is null)
        {
            error = "Unknown perk.";
            return false;
        }

        var normalizedPerks = Normalize(perks);
        var existing = normalizedPerks.FirstOrDefault(x =>
            string.Equals(x.Id, definition.Id, StringComparison.OrdinalIgnoreCase));
        var currentRank = existing?.Rank ?? 0;
        if (currentRank >= definition.MaxRank)
        {
            error = "Perk is already at max rank.";
            return false;
        }

        var available = GetAvailablePerkSlots(experiencePoints, normalizedPerks);
        if (available <= 0)
        {
            error = "No perk slots available.";
            return false;
        }

        var upgraded = new CompanyTrooperPerkState
        {
            Id = definition.Id,
            Rank = currentRank + 1
        };

        if (existing is not null)
        {
            normalizedPerks.Remove(existing);
        }

        normalizedPerks.Add(upgraded);
        normalizedPerks = Normalize(normalizedPerks);

        perks.Clear();
        foreach (var perk in normalizedPerks)
        {
            perks.Add(perk);
        }

        return true;
    }

    /// <summary>
    /// Validates whether a specific tier can be acquired based on requirement rules.
    /// </summary>
    public static bool CanAcquireTier(
        CompanyPerkTrackDefinition track,
        int tier,
        IReadOnlyCollection<int> alreadyOwnedTiers)
    {
        if (track is null || tier < 1 || tier > track.Tiers.Count)
        {
            return false;
        }

        var tierDefinition = track.Tiers[tier - 1];
        if (tierDefinition.IsEmpty)
        {
            return false;
        }

        var requiredTier = CompanyPerkCatalog.ResolveRequiredTier(track, tier);
        if (!requiredTier.HasValue)
        {
            return true;
        }

        return alreadyOwnedTiers.Contains(requiredTier.Value);
    }
}
