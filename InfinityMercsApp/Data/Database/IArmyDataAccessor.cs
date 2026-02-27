namespace InfinityMercsApp.Data.Database;

public interface IArmyDataAccessor
{
    Task ImportFactionArmyFromJsonAsync(int factionId, string json, CancellationToken cancellationToken = default);

    Task ImportFactionArmyFromFileAsync(int factionId, string filePath, CancellationToken cancellationToken = default);

    Task<bool> HasFactionArmyAsync(int factionId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<int>> GetStoredFactionIdsAsync(CancellationToken cancellationToken = default);

    Task<ArmyFactionRecord?> GetFactionSnapshotAsync(int factionId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ArmyUnitRecord>> GetUnitsByFactionAsync(int factionId, CancellationToken cancellationToken = default);

    Task<ArmyUnitRecord?> GetUnitAsync(int factionId, int unitId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ArmyUnitRecord>> SearchUnitsAsync(string searchTerm, int? factionId = null, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ArmyResumeRecord>> GetResumeByFactionAsync(int factionId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ArmyResumeRecord>> GetResumeByFactionMercsOnlyAsync(int factionId, CancellationToken cancellationToken = default);
}
